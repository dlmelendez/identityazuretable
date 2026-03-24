// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    /// <inheritdoc/>
    public class UserStore<TUser, TContext> : UserStore<TUser, Model.IdentityRole, string, Model.IdentityUserLogin, Model.IdentityUserRole, Model.IdentityUserClaim, Model.IdentityUserToken, TContext>
       , IUserStore<TUser>
       where TUser : Model.IdentityUser<string>, new()
       where TContext : IdentityCloudContext
    {
        /// <inheritdoc/>
        public UserStore(TContext context, Model.IKeyHelper keyHelper) : base(context, keyHelper) { }
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
        /// <inheritdoc/>
        public UserStore(TContext context, Model.IKeyHelper keyHelper) : base(context, keyHelper) { }
    }

    /// <inheritdoc/>
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
        private static readonly List<string> UserRoleNameSelectColumns = [nameof(Model.IdentityUserRole<TKey>.RoleName)];
        private static readonly List<string> TableRowKeySelectColumns = [nameof(TableEntity.RowKey)];
        private static readonly List<string> RoleNameOnlySelectColumns = [nameof(Model.IdentityRole.Name)];

        /// <summary>
        /// Access to the Role Table
        /// </summary>
        protected TableClient _roleTable;

        /// <inheritdoc/>
        public UserStore(TContext context, Model.IKeyHelper keyHelper) : base(context, keyHelper)
        {
            _roleTable = context.RoleTable;
        }

        /// <summary>
        /// Create user and role tables
        /// </summary>
        /// <returns></returns>
        public override Task CreateTablesIfNotExistsAsync()
        {
            return Task.WhenAll(base.CreateTablesIfNotExistsAsync(), _roleTable.CreateIfNotExistsAsync());
        }

        /// <inheritdoc/>
        public virtual async Task AddToRoleAsync(TUser user, string? roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            TRole roleT = new TRole();
            roleT.Name = roleName;
            ((Model.IGenerateKeys)roleT).GenerateKeys(_keyHelper);


            TUserRole userToRole = new TUserRole();
            userToRole.PartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            userToRole.RoleId = roleT.Id;
            userToRole.RoleName = roleT.Name;
            userToRole.UserId = user.Id;
            TUserRole item = userToRole;

            ((Model.IGenerateKeys)item).GenerateKeys(_keyHelper);

            await Task.WhenAll(
                _userTable.AddEntityAsync(item, cancellationToken),
                _indexTable.UpsertEntityAsync(CreateRoleIndex(userToRole.PartitionKey, roleName), mode: TableUpdateMode.Replace, cancellationToken: cancellationToken)
            ).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public virtual async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConcurrentBag<string> bag = [];
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            if (EqualityComparer<TKey?>.Default.Equals(user!.Id, default))
            {
                throw new ArgumentNullException(nameof(user));
            }

            const string roleName = nameof(Model.IdentityUserRole<>.RoleName);
            var userId = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            // Changing to a live query to mimic EF UserStore in Identity 3.0

            var filterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserRole));
            List<string> userRoles = [];
            await foreach (var userRoleEntity in _userTable.QueryAsync<TableEntity>(filter: filterString.ToString(), select: UserRoleNameSelectColumns, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (userRoleEntity.ContainsKey(roleName))
                {
                    var role = userRoleEntity.GetString(roleName);
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        userRoles.Add(role);
                    }
                }
            }

            int userRoleTotalCount = userRoles.Count;
            if (userRoleTotalCount > 0)
            {
                const int pageSize = 10;
                int maxPages = (int)Math.Ceiling((double)userRoleTotalCount / pageSize);

                List<Task> tasks = new List<Task>(maxPages);

                for (int offset = 0; offset < userRoleTotalCount; offset += pageSize)
                {
                    int currentPageCount = Math.Min(pageSize, userRoleTotalCount - offset);
                    string? queryTemp = null;
                    for (int iRoleCounter = 0; iRoleCounter < currentPageCount; iRoleCounter++)
                    {
                        var urt = userRoles[offset + iRoleCounter];
                        var roleQuery = BuildRoleQuery(urt);
                        if (iRoleCounter == 0)
                        {
                            queryTemp = roleQuery;
                        }
                        else
                        {
                            queryTemp = TableQuery.CombineFilters(queryTemp!, TableOperators.Or, roleQuery).ToString();
                        }
                    }

                    if (queryTemp is null)
                    {
                        continue;
                    }

                    tasks.Add(
                        _roleTable.QueryAsync<Model.IdentityRole>(filter: queryTemp, select: RoleNameOnlySelectColumns, cancellationToken: cancellationToken)
                        .ForEachAsync((t) =>
                        {
                            if (!string.IsNullOrWhiteSpace(t?.Name))
                            {
                                bag.Add(t!.Name!);
                            }
                        }, cancellationToken)
                    );
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            return [.. bag];
        }

        /// <summary>
        /// Builds Odata role query
        /// </summary>
        /// <param name="normalizedRoleName"></param>
        /// <returns></returns>
        public string BuildRoleQuery(string normalizedRoleName)
        {
            var rowFilter =
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey),
                QueryComparisons.Equal,
                _keyHelper.GenerateRowKeyIdentityRole(normalizedRoleName));

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal, _keyHelper.GeneratePartitionKeyIdentityRole(normalizedRoleName)),
                TableOperators.And,
                rowFilter).ToString();
        }

        /// <inheritdoc/>
        public virtual async Task<IList<TUser>> GetUsersInRoleAsync(string? roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            if (await RoleExistsAsync(roleName!, cancellationToken).ConfigureAwait(false))
            {
                string getTableQueryFilterByUserId(string? userId)
                {
                    var rowFilter = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, userId),
                        TableOperators.Or,
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, _keyHelper.GenerateRowKeyIdentityUserRole(roleName)));

                    var tqFilter = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId), TableOperators.And,
                        rowFilter);
                    return tqFilter.ToString();
                }


                return [.. (await GetUsersByIndexQueryAsync(GetUserByRoleIndexQuery(roleName!).ToString(), (userId, cancellationToken) =>
                {
                    return GetUserAggregateQueryAsync(userId, setFilterByUserId: getTableQueryFilterByUserId, whereClaim: null, whereRole: (ur) =>
                    {
                        return ur.RowKey.AsSpan().Equals(_keyHelper.GenerateRowKeyIdentityUserRole(roleName), StringComparison.OrdinalIgnoreCase);
                    }, cancellationToken: cancellationToken);

                }, cancellationToken).ConfigureAwait(false))];
            }

            return [];
        }

        /// <inheritdoc/>
        public virtual async Task<bool> IsInRoleAsync(TUser user, string? roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            var userId = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            // Changing to a live query to mimic EF UserStore in Identity 3.0

            var filterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, _keyHelper.GenerateRowKeyIdentityUserRole(roleName)));
            var userRoleTask = _userTable.QueryAsync<TableEntity>(filter: filterString.ToString(), maxPerPage: 1, select: TableRowKeySelectColumns, cancellationToken).AnyAsync(cancellationToken)
#if NET10_0_OR_GREATER
                .AsTask()
#endif
                ;
            var roleExistsTask = RoleExistsAsync(roleName!, cancellationToken)
#if NET10_0_OR_GREATER
                .AsTask()
#endif
                ;

            await Task.WhenAll(userRoleTask, roleExistsTask).ConfigureAwait(false);

            return userRoleTask.Result && roleExistsTask.Result;
        }

#if NET10_0_OR_GREATER
        /// <inheritdoc/>
        public ValueTask<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
        {
            return _roleTable.QueryAsync<TableEntity>(filter: BuildRoleQuery(roleName), maxPerPage: 1, select: [nameof(Model.IdentityRole.Name)], cancellationToken: cancellationToken).AnyAsync(cancellationToken);
        }
#else

        /// <inheritdoc/>
        public Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
        {
            return _roleTable.QueryAsync<TableEntity>(filter: BuildRoleQuery(roleName), maxPerPage: 1, select: [nameof(Model.IdentityRole.Name)], cancellationToken: cancellationToken).AnyAsync(cancellationToken);
        }

#endif
        /// <inheritdoc/>
        public virtual async Task RemoveFromRoleAsync(TUser user, string? roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));

            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            try
            {
                var item = await _userTable.GetEntityOrDefaultAsync<TUserRole>(userPartitionKey, _keyHelper.GenerateRowKeyIdentityRole(roleName).ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);

                if (item is not null)
                {
                    var deleteRoleIndex = CreateRoleIndex(userPartitionKey, roleName);
                    await Task.WhenAll(
                       _userTable.DeleteEntityAsync(item.PartitionKey, item.RowKey, TableConstants.ETagWildcard, cancellationToken),
                       _indexTable.DeleteEntityAsync(deleteRoleIndex.PartitionKey, deleteRoleIndex.RowKey, TableConstants.ETagWildcard, cancellationToken)
                    ).ConfigureAwait(false);
                }
            }
            catch (RequestFailedException rfe)
                when (rfe.ErrorCode == TableErrorCode.ResourceNotFound)
            {
                //noop - if it isn't found, we can't delete
            }
        }

        /// <summary>
        /// Executes a user aggregate query, that can filter by userid, roles and/or claims
        /// </summary>
        /// <param name="userIds"></param>
        /// <param name="setFilterByUserId"></param>
        /// <param name="whereRole"></param>
        /// <param name="whereClaim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<IEnumerable<TUser>> GetUserAggregateQueryAsync(IEnumerable<string> userIds,
        Func<string, string>? setFilterByUserId = null,
        Func<TUserRole, bool>? whereRole = null,
        Func<TUserClaim, bool>? whereClaim = null,
        CancellationToken cancellationToken = default)
        {
            string[] userIdArray = userIds as string[] ?? [.. userIds];
            const int pageSize = 50;
            int pages = (int)Math.Ceiling((double)userIdArray.Length / pageSize);
            List<string> listTqs = new List<string>(pages);

            for (int offset = 0; offset < userIdArray.Length; offset += pageSize)
            {
                string filterString = string.Empty;
                int currentPageCount = Math.Min(pageSize, userIdArray.Length - offset);
                for (int i = 0; i < currentPageCount; i++)
                {
                    var tempUserId = userIdArray[offset + i];

                    string temp = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, tempUserId).ToString();
                    if (setFilterByUserId is not null)
                    {
                        temp = setFilterByUserId(tempUserId);
                    }

                    if (i > 0)
                    {
                        filterString = TableQuery.CombineFilters(filterString, TableOperators.Or, temp).ToString();
                    }
                    else
                    {
                        filterString = temp;
                    }
                }
                if (!string.IsNullOrWhiteSpace(filterString))
                {
                    listTqs.Add(filterString);
                }

            }

            ConcurrentBag<TUser> bag = [];
#if DEBUG
            DateTime startUserAggTotal = DateTime.UtcNow;
#endif
            List<Task> tasks = new List<Task>(listTqs.Count);
            foreach (var q in listTqs)
            {
                tasks.Add(ProcessAggregateQueryAsync(q, cancellationToken));
            }

            async Task ProcessAggregateQueryAsync(string query, CancellationToken ct)
            {
                var queryResults = await _userTable.QueryAsync<TableEntity>(filter: query, cancellationToken: ct).ToListAsync(ct)
#if NET10_0_OR_GREATER
                    .AsTask()
#endif
                    .ConfigureAwait(false);

                foreach (var s in queryResults.GroupBy(g => g.PartitionKey))
                {
                    var userAgg = MapUserAggregate(s.Key, s);
                    bool addUser = true;
                    if (whereClaim is not null && !userAgg.Claims.Any(whereClaim))
                    {
                        addUser = false;
                    }

                    if (whereRole is not null && !userAgg.Roles.Any(whereRole))
                    {
                        addUser = false;
                    }

                    if (userAgg.User is not null && addUser)
                    {
                        bag.Add(userAgg.User);
                    }
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
#if DEBUG
            Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
            Debug.WriteLine("GetUserAggregateQuery (Return Count): {0} userIds", bag.Count);
#endif
            return bag;
        }

        /// <summary>
        /// Maps table entities to strongly typed identity entities by userid
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userResults"></param>
        /// <returns></returns>
        protected new (TUser? User,
            IEnumerable<TUserRole> Roles,
            IEnumerable<TUserClaim> Claims,
            IEnumerable<TUserLogin> Logins,
            IEnumerable<TUserToken> Tokens)
        MapUserAggregate(string? userId,
            IEnumerable<TableEntity> userResults)
        {

            TUser? user = default;
            List<TUserRole> roles = [];
            List<TUserClaim> claims = [];
            List<TUserLogin> logins = [];
            List<TUserToken> tokens = [];

            foreach (var userResult in userResults)
            {
                if (!userResult.PartitionKey.Equals(userId))
                {
                    continue;
                }

                if (userResult.RowKey.Equals(userId))
                {
                    user = userResult.MapTableEntity<TUser>();
                    continue;
                }

                if (userResult.RowKey.StartsWith(_keyHelper.PreFixIdentityUserRole))
                {
                    roles.Add(userResult.MapTableEntity<TUserRole>());
                    continue;
                }

                if (userResult.RowKey.StartsWith(_keyHelper.PreFixIdentityUserClaim))
                {
                    claims.Add(userResult.MapTableEntity<TUserClaim>());
                    continue;
                }

                if (userResult.RowKey.StartsWith(_keyHelper.PreFixIdentityUserLogin))
                {
                    logins.Add(userResult.MapTableEntity<TUserLogin>());
                    continue;
                }

                if (userResult.RowKey.StartsWith(_keyHelper.PreFixIdentityUserToken))
                {
                    tokens.Add(userResult.MapTableEntity<TUserToken>());
                }
            }

            return (user, roles, claims, logins, tokens);
        }

        /// <inheritdoc/>
        public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            List<Task> tasks = new List<Task>(50);
            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            var userRows = await GetUserAggregateQueryAsync(userPartitionKey.ToString(), cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(DeleteAllUserRowsAsync(userPartitionKey, userRows));

            var deleteUserNameIndex = CreateUserNameIndex(userPartitionKey, user.UserName);
            tasks.Add(_indexTable.DeleteEntityAsync(deleteUserNameIndex.PartitionKey, deleteUserNameIndex.RowKey, TableConstants.ETagWildcard, cancellationToken: cancellationToken));

            var userAgg = MapUserAggregate(userPartitionKey, userRows);

            //Don't use the BatchHelper for login index table, partition keys are likely not the same
            //since they are based on logonprovider and providerkey
            foreach (var userLogin in userAgg.Logins)
            {
                var deleteIndex = CreateLoginIndex(userPartitionKey, userLogin.LoginProvider, userLogin.ProviderKey);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey, TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            foreach (var userRole in userAgg.Roles)
            {
                var deleteIndex = CreateRoleIndex(userPartitionKey, userRole.RoleName);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey, TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            foreach (var userClaim in userAgg.Claims)
            {
                var deleteIndex = CreateClaimIndex(userPartitionKey, userClaim.ClaimType, userClaim.ClaimValue);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey, TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                var deleteIndex = CreateEmailIndex(userPartitionKey, user!.Email!);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteIndex.PartitionKey, deleteIndex.RowKey, TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
                return IdentityResult.Success;
            }
            catch (AggregateException aggex)
            {
                aggex.Flatten();
                return IdentityResult.Failed(new IdentityError() { Code = "003", Description = "Delete user failed." });
            }
        }
    }

}
