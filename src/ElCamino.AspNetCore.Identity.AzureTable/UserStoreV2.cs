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
using Microsoft.Azure.Cosmos.Table;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public class UserStoreV2<TUser, TContext> : UserStoreV2<TUser, Model.IdentityRole, string, Model.IdentityUserLogin, Model.IdentityUserRole, Model.IdentityUserClaim, Model.IdentityUserToken, TContext>
       , IUserStore<TUser>
       where TUser : Model.IdentityUser<string>, new()
       where TContext : IdentityCloudContext, new()
    {
        public UserStoreV2(TContext context, IdentityConfiguration config) : base(context, config) { }
    }
    /// <summary>
    /// Supports as slimmer, trimmer, IdentityUserV2 with NO Roles, Claims, and Tokens as collection properties.
    /// Use this for keep inline with v2 core identity base user model. Otherwise, data is the same, queries load a smaller user object.
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class UserStoreV2<TUser, TRole, TContext> : UserStoreV2<TUser, TRole, string, Model.IdentityUserLogin, Model.IdentityUserRole, Model.IdentityUserClaim, Model.IdentityUserToken, TContext>
        , IUserStore<TUser>
        where TUser : Model.IdentityUser<string>, new()
        where TRole : Model.IdentityRole<string, Model.IdentityUserRole>, new()
        where TContext : IdentityCloudContext, new()
    {
        public UserStoreV2(TContext context,IdentityConfiguration config) : base(context,config) { }
    }

    public class UserStoreV2<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim, TUserToken, TContext> :
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
        where TContext : IdentityCloudContext, new()
    {
        protected CloudTable _roleTable;

        public UserStoreV2(TContext context, IdentityConfiguration config) : base(context, config) 
        {
            this._roleTable = context.RoleTable;
        }

        public override async Task<bool> CreateTablesIfNotExistsAsync()
        {
            Task<bool>[] tasks = 
                new Task<bool>[]
                {
                    base.CreateTablesIfNotExistsAsync(),
                    _roleTable.CreateIfNotExistsAsync(),
                };
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return tasks.All(t => t.Result);
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
            ((Model.IGenerateKeys)roleT).GenerateKeys();


            TUserRole userToRole = new TUserRole();
            userToRole.PartitionKey = KeyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            userToRole.RoleId = roleT.Id;
            userToRole.RoleName = roleT.Name;
            userToRole.UserId = user.Id;
            TUserRole item = userToRole;

            ((Model.IGenerateKeys)item).GenerateKeys();

            roleT.Users.Add(item);

            List<Task> tasks = new List<Task>(2);

            tasks.Add(_userTable.ExecuteAsync(TableOperation.Insert(item)));
            tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateRoleIndex(userToRole.PartitionKey, roleName))));

            await Task.WhenAll(tasks);
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
            string userId = KeyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            // Changing to a live query to mimic EF UserStore in Identity 3.0
            TableQuery tq = new TableQuery();

            string rowFilter =
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserRole);

            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId),
                TableOperators.And,
                rowFilter);
            tq.SelectColumns = new List<string>() { roleName };
            var userRoles =
                (await _userTable.ExecuteQueryAsync(tq).ToListAsync())
                .Where(w => w.Properties[roleName] != null)
                .Select(d => d.Properties[roleName].StringValue)
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
                    const string rName = "Name";
                    TableQuery tqRoles = new TableQuery();
                    tqRoles.FilterString = queryTemp;
                    tqRoles.SelectColumns = new List<string>() { rName };
                    tasks.Add(_roleTable.ExecuteQueryAsync(tqRoles).ToListAsync()
                        .ContinueWith((t) => {
                            return t.Result.Where(w => w.Properties[rName] != null)
                            .Select(d => d.Properties[rName].StringValue)
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
                TableQuery.GenerateFilterCondition("RowKey",
                QueryComparisons.Equal,
                KeyHelper.GenerateRowKeyIdentityRole(normalizedRoleName));

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey",
                QueryComparisons.Equal, KeyHelper.GeneratePartitionKeyIdentityRole(normalizedRoleName)),
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

            if (await RoleExistsAsync(roleName))
            {
                Func<string, string> getTableQueryFilterByUserId = (userId) =>
                {
                    string rowFilter = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, userId),
                        TableOperators.Or,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, KeyHelper.GenerateRowKeyIdentityUserRole(roleName)));

                    string tqFilter = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId), TableOperators.And,
                        rowFilter);
                    return tqFilter;
                };


                return (await this.GetUsersAggregateByIndexQueryAsync(GetUserByRoleQuery(roleName), (userId) => {
                    return GetUserAggregateQueryAsync(userId, setFilterByUserId: getTableQueryFilterByUserId, whereClaim: null, whereRole: (ur) =>
                    {
                        return ur.RowKey == KeyHelper.GenerateRowKeyIdentityUserRole(roleName);
                    });

                })).ToList();
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

            string userId = KeyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            // Changing to a live query to mimic EF UserStore in Identity 3.0
            TableQuery tq = new TableQuery();

            string rowFilter =
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, KeyHelper.GenerateRowKeyIdentityUserRole(roleName));

            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId),
                TableOperators.And,
                rowFilter);
            tq.SelectColumns = new List<string>() { "RowKey" };
            tq.TakeCount = 1;
            var tasks = new Task<bool>[]
            {
                _userTable.ExecuteQueryAsync(tq).AnyAsync(),
                RoleExistsAsync(roleName)
            };

            await Task.WhenAll(tasks);

            return tasks.All(t => t.Result);
        }

        public Task<bool> RoleExistsAsync(string roleName)
        {
            TableQuery tqRoles = new TableQuery();
            tqRoles.FilterString = BuildRoleQuery(roleName);
            tqRoles.SelectColumns = new List<string>() { "Name" };
            tqRoles.TakeCount = 1;
            return _roleTable.ExecuteQueryAsync(tqRoles).AnyAsync();
        }

        public virtual async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));

            string userPartitionKey = KeyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            TUserRole item = null;
            var tresult = await _userTable.ExecuteAsync(TableOperation.Retrieve<TUserRole>(userPartitionKey, KeyHelper.GenerateRowKeyIdentityRole(roleName))).ConfigureAwait(false);
            item = tresult.Result as TUserRole;

            if (item != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(item);

                await Task.WhenAll(
                   _userTable.ExecuteAsync(deleteOperation),
                    _indexTable.ExecuteAsync(TableOperation.Delete(CreateRoleIndex(userPartitionKey, roleName)))
                );

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

                    string temp = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, tempUserId);
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
                return _userTable.ExecuteQueryAsync(q).ToListAsync()
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
            IEnumerable<DynamicTableEntity> userResults,
            Func<TUserRole, bool> whereRole = null,
            Func<TUserClaim, bool> whereClaim = null)
        {

            TUser user = default(TUser);
            IEnumerable<TUserRole> roles = Enumerable.Empty<TUserRole>();
            IEnumerable<TUserClaim> claims = Enumerable.Empty<TUserClaim>();
            IEnumerable<TUserLogin> logins = Enumerable.Empty<TUserLogin>();
            IEnumerable<TUserToken> tokens = Enumerable.Empty<TUserToken>();

            var vUser = userResults.Where(u => u.RowKey.Equals(userId) && u.PartitionKey.Equals(userId)).SingleOrDefault();
            var op = new OperationContext();

            if (vUser != null)
            {
                //User
                user = MapTableEntity<TUser>(vUser);

                //Roles
                roles = userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserRole)
                    && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return MapTableEntity<TUserRole>(log);
                    });
                //Claims
                claims = userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserClaim)
                     && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return MapTableEntity<TUserClaim>(log);
                    });
                //Logins
                logins = userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserLogin)
                    && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return MapTableEntity<TUserLogin>(log);
                    });

                //Tokens
                tokens = userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserToken)
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
            string userPartitionKey = KeyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            var userRows = await GetUserAggregateQueryAsync(userPartitionKey).ToListAsync().ConfigureAwait(false);

            tasks.Add(DeleteAllUserRows(userPartitionKey, userRows));

            tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateUserNameIndex(userPartitionKey, user.UserName))));

            var userAgg = MapUserAggregate(userPartitionKey, userRows);

            //Don't use the BatchHelper for login index table, partition keys are likely not the same
            //since they are based on logonprovider and providerkey
            foreach (var userLogin in userAgg.Logins)
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateLoginIndex(userPartitionKey, userLogin.LoginProvider, userLogin.ProviderKey))));
            }

            foreach (var userRole in userAgg.Roles)
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateRoleIndex(userPartitionKey, userRole.RoleName))));
            }

            foreach (var userClaim in userAgg.Claims)
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateClaimIndex(userPartitionKey, userClaim.ClaimType, userClaim.ClaimValue))));
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateEmailIndex(userPartitionKey, user.Email))));
            }

            try
            {
                await Task.WhenAll(tasks.ToArray());
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
