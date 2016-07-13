// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

#if net45
using ElCamino.AspNet.Identity.AzureTable.Model;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.AzureTable.Helpers
{
    internal sealed class TableQueryHelper<t> : IQueryable<t> where t : IdentityUser, new()
    {
        private readonly TableQuery<t> _tableQuery;
        private readonly Func<IList<string>, IEnumerable<t>> _userEntityFunc;
        private readonly TableQueryProviderHelper<t> _provider;

        internal TableQueryHelper(TableQuery<t> tableQuery, Func<IList<string>, IEnumerable<t>> userEntityFunc)
        {
            _tableQuery = tableQuery;
            _userEntityFunc = userEntityFunc;           
            _provider = new TableQueryProviderHelper<t>(_tableQuery, _userEntityFunc);
        }
       
        public IEnumerator<t> GetEnumerator()
        {
#if net45
			var userIds = _tableQuery.Select(u => u.RowKey).ToList();
            var result = _userEntityFunc(userIds);
#else
			var userIds = _tableQuery.Select(new List<string>() { "RowKey" });
			var result = _userEntityFunc(userIds);

#endif
			return result.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public Type ElementType
        {
            get { return _tableQuery.ElementType; }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get 
            {
                return _tableQuery.Expression; 
            }
        }

        public IQueryProvider Provider
        {
            get { return _provider; }
        }

    }

    internal class TableQueryProviderHelper<TElement> : IQueryProvider where TElement : IdentityUser, new()
    {
        private readonly TableQuery<TElement> _tableQuery;
        private readonly Func<IList<string>, IEnumerable<TElement>> _userEntityFunc;

        internal TableQueryProviderHelper(TableQuery<TElement> tableQuery, Func<IList<string>, 
            IEnumerable<TElement>> userEntityFunc)
        {
            _tableQuery = tableQuery;
            _userEntityFunc = userEntityFunc;
        }

        public IQueryable<t> CreateQuery<t>(System.Linq.Expressions.Expression expression) 
        {
            MethodCallExpression mc = expression as MethodCallExpression;

            if (typeof(TElement) == typeof(t))
            {
                if (mc.Method.Name.Equals("Skip", StringComparison.OrdinalIgnoreCase))
                {
                    var skipToUserIds = _tableQuery.Provider.CreateQuery<TElement>(mc.Arguments[0]).Select(u => u.RowKey).ToList();
                    ConstantExpression c = mc.Arguments[1] as ConstantExpression;
                    var skipList = skipToUserIds.Skip((int)c.Value).ToList();
                    return skipList.SelectMany(s =>
                        {
                            return _userEntityFunc(new List<string>() { s }).Cast<t>();
                        }).AsQueryable();
                }

                //Force the query here to populate roles, claims and logins
                var userIds = _tableQuery.Provider.CreateQuery<TElement>(expression).Select(u => u.RowKey).ToList();

                var result = _userEntityFunc(userIds);
                return result.Cast<t>().AsQueryable();
            }

            return _tableQuery.Provider.CreateQuery<t>(expression);
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            return this.CreateQuery<TElement>(expression);
        }

        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        {
            MethodCallExpression mc = expression as MethodCallExpression;

            if( mc.Method.Name.Equals("Count", StringComparison.OrdinalIgnoreCase))
            {
                Expression temp = mc.Arguments[0];
                var list = _tableQuery.Provider.CreateQuery<TElement>(temp).Select(u => u.RowKey).ToList();
                return (TResult)(object)list.Count;
            }

            //Force the query here to populate roles, claims and logins
            var user = _tableQuery.Provider.Execute<TElement>(expression);

            var result = _userEntityFunc(new List<string>() { user.RowKey });
            return result.Cast<TResult>().FirstOrDefault();
        }

        public object Execute(System.Linq.Expressions.Expression expression)
        {
            throw new NotSupportedException();
        }
    }

}
#endif