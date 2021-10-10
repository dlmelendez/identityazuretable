// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using Azure.Data.Tables;
using Azure;
using System.Net;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public class UserStore<TUser, TContext> : UserStore<TUser, Model.IdentityRole, string, Model.IdentityUserLogin, Model.IdentityUserRole, Model.IdentityUserClaim, Model.IdentityUserToken, TContext>
       , IUserStore<TUser>
       where TUser : Model.IdentityUser<string>, new()
       where TContext : IdentityCloudContext
    {
        public UserStore(TContext context, Model.IKeyHelper keyHelper, Model.IdentityConfiguration config) : base(context, keyHelper, config) { }
    }
    /// <summary>
    /// Supports as slimmer, trimmer, IdentityUser 
    /// Use this for keep inline with v3 core identity base user model. 
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class UserStore<TUser, TRole, TContext> : UserStore<TUser, TRole, string, Model.IdentityUserLogin, Model.IdentityUserRole, Model.IdentityUserClaim, Model.IdentityUserToken, TContext>
        , IUserStore<TUser>
        where TUser : Model.IdentityUser<string>, new()
        where TRole : Model.IdentityRole<string, Model.IdentityUserRole>, new()
        where TContext : IdentityCloudContext
    {
        public UserStore(TContext context, Model.IKeyHelper keyHelper, Model.IdentityConfiguration config) : base(context, keyHelper, config) { }
    }

    public class UserStore<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim, TUserToken, TContext> :
        UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken>
        , IUserRoleStore<TUser>
        , IDisposable
        where TUser : Model.IdentityUser<TKey>, new()
        where TRole : Model.IdentityRole<TKey, TUserRole>, new()
        where TKey : IEquatable<TKey>
        where TUserLogin : Model.IdentityUserLogin<TKey>, new()
        where TUserRole : Model.IdentityUserRole<TKey>, new()
        where TUserClaim : Model.IdentityUserClaim<TKey>, new()
        where TUserToken : Model.IdentityUserToken<TKey>, new()
        where TContext : IdentityCloudContext
    {
        protected TableClient _roleTable;

        public UserStore(TContext context, Model.IKeyHelper keyHelper, Model.IdentityConfiguration config) : base(context, keyHelper, config) 
        {
            this._roleTable = context.RoleTable;
        }

        public override Task CreateTablesIfNotExistsAsync()
        {
            Task[] tasks = 
                new Task[]
                {
                    base.CreateTablesIfNotExistsAsync(),
                    _roleTable.CreateIfNotExistsAsync(),
                };
            return Task.WhenAll(tasks);
        }

        public virtual async Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            TRole roleT = new TRole();
            roleT.Name = roleName;
            ((Model.IGenerateKeys)roleT).GenerateKeys(_keyHelper);


            TUserRole userToRole = new TUserRole();
            userToRole.PartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            userToRole.RoleId = roleT.Id;
            userToRole.RoleName = roleT.Name;
            userToRole.UserId = user.Id;
            TUserRole item = userToRole;

            ((Model.IGenerateKeys)item).GenerateKeys(_keyHelper);

            List<Task> tasks = new List<Task>(2);

            tasks.Add(_userTable.AddEntityAsync(item));
            tasks.Add(_indexTable.UpsertEntityAsync(CreateRoleIndex(userToRole.PartitionKey, roleName)));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async virtual Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (EqualityComparer<TKey>.Default.Equals(user.Id, default(TKey)))
            {
                throw new ArgumentNullException(nameof(user.Id));
            }

            const string roleName = "RoleName";
            string userId = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            // Changing to a live query to mimic EF UserStore in Identity 3.0
            TableQuery tq = new TableQuery();

            string rowFilter =
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserRole);

            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId),
                TableOperators.And,
                rowFilter);
            tq.SelectColumns = new List<string>() { roleName };
            var userRoles =
                (await _userTable.ExecuteQueryAsync<TableEntity>(tq).ToListAsync().ConfigureAwait(false))
                .Where(w => w.ContainsKey(roleName))
                .Select(d => d.GetString(roleName))
                .Where(di => !string.IsNullOrWhiteSpace(di));

            int userRoleTotalCount = userRoles.Count();
            if (userRoleTotalCount > 0)
            {
                const double pageSize = 10d;
                double maxPages = Math.Ceiling((double)userRoleTotalCount / pageSize);

                List<Task<List<string>>> tasks = new List<Task<List<string>>>((int)maxPages);

                for (int iPageIndex = 0; iPageIndex < maxPages; iPageIndex++)
                {
                    int skip = (int)(iPageIndex * pageSize);
                    IEnumerable<string> userRolesTemp = skip > 0 ? userRoles.Skip(skip).Take((int)pageSize) :
                        userRoles.Take((int)pageSize); ;

                    string queryTemp = string.Empty;
                    int iRoleCounter = 0;
                    foreach (var urt in userRolesTemp)
                    {
                        if (iRoleCounter == 0)
                        {
                            queryTemp = BuildRoleQuery(urt);
                        }
                        else
                        {
                            queryTemp = TableQuery.CombineFilters(queryTemp, TableOperators.Or, BuildRoleQuery(urt));
                        }
                        iRoleCounter++;
                    }
                    TableQuery tqRoles = new TableQuery();
                    tqRoles.FilterString = queryTemp;
                    tqRoles.SelectColumns = new List<string>() { nameof(Model.IdentityRole.Name) };
                    tasks.Add(
                        _roleTable.ExecuteQueryAsync<Model.IdentityRole>(tqRoles).ToListAsync()
                        .ContinueWith((t) => {
                            return t.Result.Where(w => w.Name != null)
                            .Select(d => d.Name)
                            .Where(di => !string.IsNullOrWhiteSpace(di))
                            .ToList();
                        })
                    );
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                return tasks.Select(s => s.Result).SelectMany(m => m).ToList() as IList<string>;
            }

            return new List<string>() as IList<string>;
        }
        public string BuildRoleQuery(string normalizedRoleName)
        {
            string rowFilter =
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                _keyHelper.GenerateRowKeyIdentityRole(normalizedRoleName));

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal, _keyHelper.GeneratePartitionKeyIdentityRole(normalizedRoleName)),
                TableOperators.And,
                rowFilter);
        }

        public async virtual Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            if (await RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                Func<string, string> getTableQueryFilterByUserId = (userId) =>
                {
                    string rowFilter = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, userId),
                        TableOperators.Or,
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, _keyHelper.GenerateRowKeyIdentityUserRole(roleName)));

                    string tqFilter = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId), TableOperators.And,
                        rowFilter);
                    return tqFilter;
                };


                return (await this.GetUsersAggregateByIndexQueryAsync(GetUserByRoleQuery(roleName), (userId) => {
                    return GetUserAggregateQueryAsync(userId, setFilterByUserId: getTableQueryFilterByUserId, whereClaim: null, whereRole: (ur) =>
                    {
                        return ur.RowKey == _keyHelper.GenerateRowKeyIdentityUserRole(roleName);
                    });

                }).ConfigureAwait(false)).ToList();
            }

            return new List<TUser>();
        }

        public async virtual Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            string userId = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            // Changing to a live query to mimic EF UserStore in Identity 3.0
            TableQuery tq = new TableQuery();

            string rowFilter =
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, _keyHelper.GenerateRowKeyIdentityUserRole(roleName));

            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId),
                TableOperators.And,
                rowFilter);
            tq.SelectColumns = new List<string>() { nameof(TableEntity.RowKey) };
            tq.TakeCount = 1;
            var tasks = new Task<bool>[]
            {
                _userTable.ExecuteQueryAsync<TableEntity>(tq).AnyAsync(),
                RoleExistsAsync(roleName)
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return tasks.All(t => t.Result);
        }

        public Task<bool> RoleExistsAsync(string roleName)
        {
            TableQuery tqRoles = new TableQuery();
            tqRoles.FilterString = BuildRoleQuery(roleName);
            tqRoles.SelectColumns = new List<string>() { nameof(Model.IdentityRole.Name) };
            tqRoles.TakeCount = 1;
            return _roleTable.ExecuteQueryAsync<TableEntity>(tqRoles).AnyAsync();
        }

        public virtual async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));

            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            try
            {
                var item = await _userTable.GetEntityAsync<TUserRole>(userPartitionKey, _keyHelper.GenerateRowKeyIdentityRole(roleName)).ConfigureAwait(false);

                if (item.Value != null)
                {
                    var deleteRoleIndex = CreateRoleIndex(userPartitionKey, roleName);
                    await Task.WhenAll(
                       _userTable.DeleteEntityAsync(item.Value.PartitionKey, item.Value.RowKey,  TableConstants.ETagWildcard, cancellationToken),
                       _indexTable.DeleteEntityAsync(deleteRoleIndex.PartitionKey, deleteRoleIndex.RowKey,  TableConstants.ETagWildcard, cancellationToken)
                    ).ConfigureAwait(false);
                }
            }
            catch (RequestFailedException rfe)
            when (rfe.Status == (int)HttpStatusCode.NotFound)
            {
                //noop - if it isn't found, we can't delete
            }
        }

        protected async Task<IEnumerable<TUser>> GetUserAggregateQueryAsync(IEnumerable<string> userIds,
        Func<string, string> setFilterByUserId = null,
        Func<TUserRole, bool> whereRole = null,
        Func<TUserClaim, bool> whereClaim = null)
        {
            const double pageSize = 50.0;
            int pages = (int)Math.Ceiling(((double)userIds.Count() / pageSize));
            List<TableQuery> listTqs = new List<TableQuery>(pages);
            IEnumerable<string> tempUserIds = null;

            for (int currentPage = 1; currentPage <= pages; currentPage++)
            {
                if (currentPage > 1)
                {
                    tempUserIds = userIds.Skip(((currentPage - 1) * (int)pageSize)).Take((int)pageSize);
                }
                else
                {
                    tempUserIds = userIds.Take((int)pageSize);
                }

                TableQuery tq = new TableQuery();
                int i = 0;
                foreach (var tempUserId in tempUserIds)
                {

                    string temp = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, tempUserId);
                    if (setFilterByUserId != null)
                    {
                        temp = setFilterByUserId(tempUserId);
                    }

                    if (i > 0)
                    {
                        tq.FilterString = TableQuery.CombineFilters(tq.FilterString, TableOperators.Or, temp);
                    }
                    else
                    {
                        tq.FilterString = temp;
                    }
                    i++;
                }
                listTqs.Add(tq);

            }

            ConcurrentBag<TUser> bag = new ConcurrentBag<TUser>();
#if DEBUG
            DateTime startUserAggTotal = DateTime.UtcNow;
#endif
            var tasks = listTqs.Select((q) =>
            {
                return
                _userTable.ExecuteQueryAsync<TableEntity>(q).ToListAsync()
                     .ContinueWith((taskResults) =>
                     {
                         //ContinueWith returns completed task. Calling .Result is safe here.

                         foreach (var s in taskResults.Result.GroupBy(g => g.PartitionKey))
                         {
                             var userAgg = MapUserAggregate(s.Key, s);
                             bool addUser = true;
                             if (whereClaim != null)
                             {
                                 if (!userAgg.Claims.Any(whereClaim))
                                 {
                                     addUser = false;
                                 }
                             }
                             if (whereRole != null)
                             {
                                 if (!userAgg.Roles.Any(whereRole))
                                 {
                                     addUser = false;
                                 }
                             }
                             if (addUser)
                             {
                                 bag.Add(userAgg.User);
                             }
                         }
                     });

            });
            await Task.WhenAll(tasks).ConfigureAwait(false);
#if DEBUG
            Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
            Debug.WriteLine("GetUserAggregateQuery (Return Count): {0} userIds", bag.Count());
#endif
            return bag;
        }

        protected (TUser User,
            IEnumerable<TUserRole> Roles,
            IEnumerable<TUserClaim> Claims,
            IEnumerable<TUserLogin> Logins,
            IEnumerable<TUserToken> Tokens)
        MapUserAggregate(string userId, 
            IEnumerable<TableEntity> userResults,
            Func<TUserRole, bool> whereRole = null,
            Func<TUserClaim, bool> whereClaim = null)
        {

            TUser user = default(TUser);
            IEnumerable<TUserRole> roles = Enumerable.Empty<TUserRole>();
            IEnumerable<TUserClaim> claims = Enumerable.Empty<TUserClaim>();
            IEnumerable<TUserLogin> logins = Enumerable.Empty<TUserLogin>();
            IEnumerable<TUserToken> tokens = Enumerable.Empty<TUserToken>();

            var vUser = userResults.Where(u => u.RowKey.Equals(userId) && u.PartitionKey.Equals(userId)).SingleOrDefault();

            if (vUser != null)
            {
                //User
                user = MapTableEntity<TUser>(vUser);

                //Roles
                roles = userResults.Where(u => u.RowKey.StartsWith(_keyHelper.PreFixIdentityUserRole)
                    && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return MapTableEntity<TUserRole>(log);
                    });
                //Claims
                claims = userResults.Where(u => u.RowKey.StartsWith(_keyHelper.PreFixIdentityUserClaim)
                     && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return MapTableEntity<TUserClaim>(log);
                    });
                //Logins
                logins = userResults.Where(u => u.RowKey.StartsWith(_keyHelper.PreFixIdentityUserLogin)
                    && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return MapTableEntity<TUserLogin>(log);
                    });

                //Tokens
                tokens = userResults.Where(u => u.RowKey.StartsWith(_keyHelper.PreFixIdentityUserToken)
                     && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return MapTableEntity<TUserToken>(log);
                    });
            }
            return (user, roles, claims, logins, tokens);
        }

        public async override Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            List<Task> tasks = new List<Task>(50);
            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            var userRows = await GetUserAggregateQueryAsync(userPartitionKey).ToListAsync().ConfigureAwait(false);
            tasks.Add(DeleteAllUserRows(userPartitionKey, userRows));

            var deleteUserNameIndex = CreateUserNameIndex(userPartitionKey, user.UserName);
            tasks.Add(_indexTable.DeleteEntityAsync(deleteUserNameIndex.PartitionKey, deleteUserNameIndex.RowKey,  TableConstants.ETagWildcard, cancellationToken: cancellationToken));

            var userAgg = MapUserAggregate(userPartitionKey, userRows);

            //Don't use the BatchHelper for login index table, partition keys are likely not the same
            //since they are based on logonprovider and providerkey
            foreach (var userLogin in userAgg.Logins)
            {
                var deleteIndex = CreateLoginIndex(userPartitionKey, userLogin.LoginProvider, userLogin.ProviderKey);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey,  TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            foreach (var userRole in userAgg.Roles)
            {
                var deleteIndex = CreateRoleIndex(userPartitionKey, userRole.RoleName);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey,  TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            foreach (var userClaim in userAgg.Claims)
            {
                var deleteIndex = CreateClaimIndex(userPartitionKey, userClaim.ClaimType, userClaim.ClaimValue);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey,  TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var deleteIndex = CreateEmailIndex(userPartitionKey, user.Email);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey,  TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            try
            {
                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
                return IdentityResult.Success;
            }
            catch (AggregateException aggex)
            {
                aggex.Flatten();
                return IdentityResult.Failed(new IdentityError() { Code = "003", Description = "Delete user failed." });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                this._roleTable = null;
            }
            base.Dispose(disposing);
        }
    }

}
