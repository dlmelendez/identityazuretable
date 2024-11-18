// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Identity;


namespace ElCamino.AspNetCore.Identity.AzureTable
{
    /// <inheritdoc/>
    public class UserOnlyStore<TUser>
        : UserOnlyStore<TUser, IdentityCloudContext> where TUser : Model.IdentityUser<string>, new()
    {
        /// <inheritdoc/>
        public UserOnlyStore(IdentityCloudContext context, IKeyHelper keyHelper) : base(context, keyHelper) { }

    }

    /// <inheritdoc/>
    public class UserOnlyStore<TUser, TContext>
        : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken>
        where TUser : Model.IdentityUser<string>, new()
        where TContext : IdentityCloudContext
    {
        /// <inheritdoc/>
        public UserOnlyStore(TContext context, IKeyHelper keyHelper) : base(context, keyHelper) { }
    }

    /// <inheritdoc/>
    public class UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken> :
        UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
        , IDisposable
        where TUser : Model.IdentityUser<TKey>, new()
        where TKey : IEquatable<TKey>
        where TUserLogin : Model.IdentityUserLogin<TKey>, new()
        where TUserClaim : Model.IdentityUserClaim<TKey>, new()
        where TUserToken : Model.IdentityUserToken<TKey>, new()
        where TContext : IdentityCloudContext
    {
        /// <summary>
        /// User Table
        /// </summary>
        protected readonly TableClient _userTable;

        /// <summary>
        /// Index Table
        /// </summary>
        protected readonly TableClient _indexTable;

        /// <summary>
        /// Current Key Helper
        /// </summary>
        protected readonly IKeyHelper _keyHelper;

        private readonly TContext _context;

        private readonly string FilterString;
        private static readonly List<string> IndexUserIdSelectColumns = [nameof(IdentityUserIndex.Id)];

        /// <inheritdoc/>
        public UserOnlyStore(TContext context, IKeyHelper keyHelper) : base(new IdentityErrorDescriber())
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userTable = context.UserTable;
            _indexTable = context.IndexTable;
            _keyHelper = keyHelper;

            var partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            var rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter).ToString();
        }

        /// <summary>
        /// Queries will be slow unless they include Partition and/or Row keys
        /// </summary>
        public override IQueryable<TUser> Users
        {
            get
            {
                return _userTable.Query<TUser>(FilterString).AsQueryable();
            }
        }

        /// <summary>
        /// Create tables for users and indexes
        /// </summary>
        /// <returns></returns>
        public virtual Task CreateTablesIfNotExistsAsync()
        {
            Task[] tasks =
                    [
                        _userTable.CreateIfNotExistsAsync(),
                        _indexTable.CreateIfNotExistsAsync(),
                    ];
            return Task.WhenAll(tasks);
        }

        /// <inheritdoc/>
        public override async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claims);

            BatchOperationHelper bHelper = new BatchOperationHelper(_userTable);

            List<Task> tasks = [];
            var userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            foreach (Claim c in claims)
            {
                bHelper.AddEntity(CreateUserClaim(user, c));
                tasks.Add(_indexTable.UpsertEntityAsync(CreateClaimIndex(userPartitionKey, c.Type, c.Value), TableUpdateMode.Replace, cancellationToken));
            }

            tasks.Add(bHelper.SubmitBatchAsync(cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public virtual async Task AddClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claim);

            List<Task> tasks =
            [
                _userTable.AddEntityAsync(CreateUserClaim(user, claim)),
                _indexTable.UpsertEntityAsync(CreateClaimIndex(_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)), claim.Type, claim.Value), mode: TableUpdateMode.Replace)
            ];

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(login);

            TUserLogin item = CreateUserLogin(user, login);

            Model.IdentityUserIndex index = CreateLoginIndex(_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString(), item.LoginProvider, item.ProviderKey);

            return Task.WhenAll(_userTable.AddEntityAsync(item, cancellationToken: cancellationToken)
                , _indexTable.UpsertEntityAsync(index, mode: TableUpdateMode.Replace, cancellationToken: cancellationToken));
        }

        /// <inheritdoc/>
        public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            ((Model.IGenerateKeys)user).GenerateKeys(_keyHelper);

            try
            {
                var userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
                List<Task> tasks = new List<Task>(3)
                {
                    _userTable.AddEntityAsync(user, cancellationToken)
                };

                if (!string.IsNullOrWhiteSpace(user?.UserName))
                {
                    tasks.Add(_indexTable.AddEntityAsync(CreateUserNameIndex(userPartitionKey, user!.UserName!), cancellationToken));
                }

                if (!string.IsNullOrWhiteSpace(user?.Email))
                {
                    Model.IdentityUserIndex index = CreateEmailIndex(userPartitionKey, user!.Email!);
                    tasks.Add(_indexTable.UpsertEntityAsync(index, mode: TableUpdateMode.Replace, cancellationToken: cancellationToken));
                }

                await Task.WhenAll([.. tasks]).ConfigureAwait(false);
                return IdentityResult.Success;
            }
            catch (AggregateException aggex)
            {
                aggex.Flatten();
                return IdentityResult.Failed(new IdentityError() { Code = "001", Description = "User Creation Failed." });
            }
        }

        /// <inheritdoc/>
        public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            List<Task> tasks = new List<Task>(50);
            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            var userRows = await GetUserAggregateQueryAsync(userPartitionKey, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);

            tasks.Add(DeleteAllUserRowsAsync(userPartitionKey, userRows));

            var deleteUserIndex = CreateUserNameIndex(userPartitionKey, user.UserName);
            tasks.Add(_indexTable.DeleteEntityAsync(deleteUserIndex.PartitionKey, deleteUserIndex.RowKey, TableConstants.ETagWildcard, cancellationToken));

            var userAgg = MapUserAggregate(userPartitionKey.ToString(), userRows);

            //Don't use the BatchHelper for login index table, partition keys are likely not the same
            //since they are based on logonprovider and providerkey
            foreach (var userLogin in userAgg.Logins)
            {
                var deleteLoginIndex = CreateLoginIndex(userPartitionKey, userLogin.LoginProvider, userLogin.ProviderKey);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteLoginIndex.PartitionKey, deleteLoginIndex.RowKey, TableConstants.ETagWildcard, cancellationToken));
            }

            foreach (var userClaim in userAgg.Claims)
            {
                var deleteClaimIndex = CreateClaimIndex(userPartitionKey, userClaim.ClaimType, userClaim.ClaimValue);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteClaimIndex.PartitionKey, deleteClaimIndex.RowKey, TableConstants.ETagWildcard, cancellationToken));
            }

            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                var deleteEmailIndex = CreateEmailIndex(userPartitionKey, user!.Email!);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteEmailIndex.PartitionKey, deleteEmailIndex.RowKey, TableConstants.ETagWildcard, cancellationToken));
            }

            try
            {
                await Task.WhenAll([.. tasks]).ConfigureAwait(false);
                return IdentityResult.Success;
            }
            catch (AggregateException aggex)
            {
                aggex.Flatten();
                return IdentityResult.Failed(new IdentityError() { Code = "003", Description = "Delete user failed." });
            }
        }

        /// <inheritdoc/>
        protected override Task<TUserLogin?> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return FindUserLoginAsync(ConvertIdToString(userId), loginProvider, providerKey);
        }

        /// <inheritdoc/>
        protected async Task<TUserLogin?> FindUserLoginAsync(string? userId, string loginProvider, string providerKey)
        {
            if (userId is not null)
            {
                var rowKey = _keyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);

                return await _userTable.GetEntityOrDefaultAsync<TUserLogin>(userId!, rowKey.ToString()).ConfigureAwait(false);
            }
            return default;
        }

        /// <inheritdoc/>
        protected override async Task<TUserLogin?> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var rowKey = _keyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
            var partitionKey = _keyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey);

            IdentityUserIndex? indexInfo = await _indexTable.GetEntityOrDefaultAsync<IdentityUserIndex>(partitionKey.ToString(), rowKey.ToString(), IndexUserIdSelectColumns, cancellationToken)
                .ConfigureAwait(false);

            if (indexInfo is not null && indexInfo.Id is not null)
            {
                return await FindUserLoginAsync(indexInfo.Id, loginProvider, providerKey).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Overridden for better performance.
        /// </summary>
        /// <param name="loginProvider"></param>
        /// <param name="providerKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var rowKey = _keyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
            var partitionKey = _keyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey);

            return GetUserFromIndexQueryAsync(GetUserIdByIndexQuery(partitionKey.ToString(), rowKey.ToString()).ToString(), cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<TUser?> FindByEmailAsync(string plainEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return GetUserFromIndexQueryAsync(FindByEmailIndexQuery(plainEmail).ToString(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TUser>> FindAllByEmailAsync(string plainEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            IEnumerable<TUser> users = await GetUsersByIndexQueryAsync(FindByEmailIndexQuery(plainEmail).ToString(), GetUserQueryAsync, cancellationToken).ConfigureAwait(false);
            return users.Where(user => _keyHelper.GenerateRowKeyUserEmail(plainEmail).Equals(_keyHelper.GenerateRowKeyUserEmail(user.Email), StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        protected override Task<TUser?> FindUserAsync(TKey userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return GetUserAsync(ConvertIdToString(userId), cancellationToken);
        }

        /// <summary>
        /// GetUserByRoleIndexQuery
        /// </summary>
        /// <param name="plainRoleName"></param>
        /// <returns>Odata filter query</returns>
        protected ReadOnlySpan<char> GetUserByRoleIndexQuery(string plainRoleName)
             => GetUserIdsByIndexQuery(_keyHelper.GenerateRowKeyIdentityUserRole(plainRoleName));

        /// <summary>
        /// GetUserByClaimIndexQuery
        /// </summary>
        /// <param name="claim"></param>
        /// <returns>Odata filter query</returns>
        protected ReadOnlySpan<char> GetUserByClaimIndexQuery(Claim claim)
         => GetUserIdsByIndexQuery(_keyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value));

        /// <summary>
        /// FindByEmailIndexQuery
        /// </summary>
        /// <param name="plainEmail"></param>
        /// <returns>Odata filter query</returns>
        protected ReadOnlySpan<char> FindByEmailIndexQuery(string plainEmail)
         => GetUserIdsByIndexQuery(_keyHelper.GenerateRowKeyUserEmail(plainEmail));

        /// <summary>
        /// FindByUserNameIndexQuery
        /// </summary>
        /// <param name="userName"></param>
        /// <returns>Odata filter query</returns>
        protected ReadOnlySpan<char> FindByUserNameIndexQuery(string userName)
            => GetUserIdsByIndexQuery(_keyHelper.GenerateRowKeyUserName(userName));

        /// <summary>
        /// GetUserIdByIndexQuery
        /// </summary>
        /// <param name="partitionkey"></param>
        /// <param name="rowkey"></param>
        /// <returns>Odata filter query</returns>
        protected ReadOnlySpan<char> GetUserIdByIndexQuery(string partitionkey, string rowkey)
        {
            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, partitionkey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, rowkey));
        }

        /// <summary>
        /// GetUserIdsByIndexQuery
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <returns>Odata filter query</returns>
        protected ReadOnlySpan<char> GetUserIdsByIndexQuery(ReadOnlySpan<char> partitionKey)
        {
            return TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, partitionKey);
        }

        /// <inheritdoc/>
        public override Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return GetUserAsync(_keyHelper.GenerateRowKeyUserId(userId).ToString(), cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            TUser? user = await GetUserFromIndexQueryAsync(FindByUserNameIndexQuery(normalizedUserName).ToString(), cancellationToken).ConfigureAwait(false);
            //Make sure the index lookup matches the user record.
            if (user != default(TUser)
                && user.NormalizedUserName is not null
                && user.NormalizedUserName.Equals(normalizedUserName, StringComparison.OrdinalIgnoreCase))
            {
                return user;
            }
            return default;
        }

        /// <inheritdoc/>
        protected async Task<TUserClaim?> GetUserClaimAsync(TUser user, Claim claim)
        {
            return await _userTable.GetEntityOrDefaultAsync<TUserClaim>(_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString(),
                _keyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value).ToString()).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            List<Claim> rClaims = [];

            var partitionFilter =
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, user.PartitionKey);
            var rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserClaim),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserClaimUpperBound));
            var filterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            await foreach (var tclaim in _userTable.QueryAsync<TUserClaim>(filter: filterString.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                //1.7 Claim rowkey migration 
                if (_keyHelper.GenerateRowKeyIdentityUserClaim(tclaim.ClaimType, tclaim.ClaimValue).Equals(tclaim.RowKey.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    rClaims.Add(tclaim.ToClaim());
                }
            }

            return rClaims;
        }

        /// <inheritdoc/>
        public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            List<UserLoginInfo> rLogins = [];

            var partitionFilter =
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, user.PartitionKey);
            var rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserLogin),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserLoginUpperBound));
            var filterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            await foreach (var tul in _userTable.QueryAsync<TUserLogin>(filter: filterString.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                rLogins.Add(new UserLoginInfo(tul.LoginProvider, tul.ProviderKey, tul.ProviderDisplayName));
            }

            return rLogins;
        }

        /// <inheritdoc/>
        protected virtual async Task<TUser?> GetUserAsync(string? userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (userId is not null)
            {
                return await _userTable.GetEntityOrDefaultAsync<TUser>(partitionKey: userId, rowKey: userId, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            return default;
        }

        /// <summary>
        /// Retrieves User rows by partitionkey UserId
        /// </summary>
        /// <param name="userIdPartitionKey">Must be formatted as UserId PartitionKey</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected IAsyncEnumerable<TableEntity> GetUserAggregateQueryAsync(string userIdPartitionKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filterString = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userIdPartitionKey);

            return _userTable.QueryAsync<TableEntity>(filter: filterString.ToString(), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Used for complex queries across userids with a filter
        /// </summary>
        /// <param name="userIds"></param>
        /// <param name="setFilterByUserId"></param>
        /// <param name="whereClaim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<IEnumerable<TUser>> GetUserAggregateQueryAsync(IEnumerable<string> userIds,
                Func<string, string>? setFilterByUserId = null,
                Func<TUserClaim, bool>? whereClaim = null,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            const double pageSize = 50.0;
            int pages = (int)Math.Ceiling(((double)userIds.Count() / pageSize));
            List<string> listTqs = new List<string>(pages);
            IEnumerable<string>? tempUserIds = null;

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

                string filterString = string.Empty;
                int i = 0;
                foreach (var tempUserId in tempUserIds)
                {

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
                    i++;
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
            var tasks = listTqs.Select((q) =>
            {
                return _userTable.QueryAsync<TableEntity>(filter: q, cancellationToken:cancellationToken).ToListAsync(cancellationToken)
                     .ContinueWith((taskResults) =>
                     {
                         //ContinueWith returns completed task. Calling .Result is safe here.

                         foreach (var s in taskResults.Result.GroupBy(g => g.PartitionKey))
                         {
                             var userAgg = MapUserAggregate(s.Key, s);
                             bool addUser = true;
                             if (whereClaim is not null)
                             {
                                 if (!userAgg.Claims.Any(whereClaim))
                                 {
                                     addUser = false;
                                 }
                             }
                             if (userAgg.User is not null && addUser)
                             {
                                 bag.Add(userAgg.User);
                             }
                         }
                     });

            });
            await Task.WhenAll(tasks).ConfigureAwait(false);
#if DEBUG
            Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
            Debug.WriteLine("GetUserAggregateQuery (Return Count): {0} userIds", bag.Count);
#endif
            return bag;
        }

        /// <summary>
        /// Gets <see cref="IEnumerable{TUser}"/> by userids
        /// </summary>
        /// <param name="userIds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="IEnumerable{TUser}"/> by userids </returns>
        protected virtual async Task<IEnumerable<TUser>> GetUserQueryAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            const double pageSize = 50.0;
            int pages = (int)Math.Ceiling(((double)userIds.Count() / pageSize));
            List<string> listTqs = new List<string>(pages);
            IEnumerable<string> tempUserIds = [];

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

                string filterString = string.Empty;
                int tempUserCounter = 0;
                foreach (string tempUserId in tempUserIds)
                {

                    var temp = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, tempUserId), TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, tempUserId));
                    if (tempUserCounter > 0)
                    {
                        filterString = TableQuery.CombineFilters(filterString, TableOperators.Or, temp).ToString();
                    }
                    else
                    {
                        filterString = temp.ToString();
                    }
                    tempUserCounter++;
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
            IEnumerable<Task> tasks = listTqs.Select((q) =>
            {
                return _userTable.QueryAsync<TUser>(filter: q, cancellationToken: cancellationToken)
                    .ForEachAsync((user) => { bag.Add(user); }, cancellationToken);
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);
#if DEBUG
            Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
#endif
            return bag;
        }

        /// <summary>
        /// Maps table entities from a query result to strongly typed identity entities
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userResults"></param>
        /// <returns>Identity Entities</returns>
        protected (TUser? User,
            IEnumerable<TUserClaim> Claims,
            IEnumerable<TUserLogin> Logins,
            IEnumerable<TUserToken> Tokens)
        MapUserAggregate(string userId,
            IEnumerable<TableEntity> userResults)
        {

            TUser? user = default;

            var vUser = userResults.Where(u => u.RowKey.Equals(userId) && u.PartitionKey.Equals(userId)).SingleOrDefault();

            if (vUser is not null)
            {
                //User
                user = vUser.MapTableEntity<TUser>();

                //Claims
                IEnumerable<TUserClaim> claims = userResults.Where(u => u.RowKey.StartsWith(_keyHelper.PreFixIdentityUserClaim)
                     && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return log.MapTableEntity<TUserClaim>();
                    });
                //Logins
                IEnumerable<TUserLogin> logins = userResults.Where(u => u.RowKey.StartsWith(_keyHelper.PreFixIdentityUserLogin)
                    && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return log.MapTableEntity<TUserLogin>();
                    });

                //Tokens
                IEnumerable<TUserToken> tokens = userResults.Where(u => u.RowKey.StartsWith(_keyHelper.PreFixIdentityUserToken)
                     && u.PartitionKey.Equals(userId))
                    .Select((log) =>
                    {
                        return log.MapTableEntity<TUserToken>();
                    });
                return (user, claims, logins, tokens);
            }
            return (user, Enumerable.Empty<TUserClaim>(), Enumerable.Empty<TUserLogin>(), Enumerable.Empty<TUserToken>());
        }

        /// <summary>
        /// Executes an index query and then gets a TUser by userId 
        /// </summary>
        /// <param name="indexQuery">Odata index filter query</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Nullable{TUser}</returns>
        protected virtual async Task<TUser?> GetUserFromIndexQueryAsync(string indexQuery, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _indexTable.QueryAsync<IdentityUserIndex>(filter: indexQuery, maxPerPage: 1, select: IndexUserIdSelectColumns, cancellationToken).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (user is not null && user.Id is not null)
            {
                string? userId = user.Id;
                return await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
            }

            return default;
        }

        /// <summary>
        /// Executes an index query and then gets a IEnumerable{TUser} by userIds
        /// </summary>
        /// <param name="indexQuery">Odata index filter query</param>
        /// <param name="getUserFunc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<IEnumerable<TUser>> GetUsersByIndexQueryAsync(string indexQuery, Func<IEnumerable<string>, CancellationToken, Task<IEnumerable<TUser>>> getUserFunc, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if DEBUG
            DateTime startIndex = DateTime.UtcNow;
#endif
            ConcurrentBag<IEnumerable<TUser>> lUsers = [];
            string? token = string.Empty;
            const int takeCount = 30;
            const int taskMax = 10;
            List<Task> taskBatch = new List<Task>(taskMax);
            async Task getUsers(IEnumerable<string> ids, CancellationToken ct)
            {
                lUsers.Add((await getUserFunc(ids, ct).ConfigureAwait(false)));
            }
            while (token is not null)
            {
                IAsyncEnumerable<Page<IdentityUserIndex>> pages = _indexTable.QueryAsync<IdentityUserIndex>(indexQuery, takeCount, IndexUserIdSelectColumns, cancellationToken).AsPages(continuationToken: token);
                Page<IdentityUserIndex>? page = await pages.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                IEnumerable<string> tempUserIds = page?.Values.Where(w => !string.IsNullOrWhiteSpace(w.Id)).Select(u => u.Id!).Distinct() ?? [];
                taskBatch.Add(getUsers(tempUserIds, cancellationToken));
                if (taskBatch.Count % taskMax == 0)
                {
                    await Task.WhenAll(taskBatch).ConfigureAwait(false);
                    taskBatch.Clear();
                }
                token = page?.ContinuationToken;
            }

            if (taskBatch.Count > 0)
            {
                await Task.WhenAll(taskBatch).ConfigureAwait(false);
                taskBatch.Clear();
            }
#if DEBUG
            Debug.WriteLine("GetUsersAggregateByIndexQueryAsync (Index query): {0} seconds", (DateTime.UtcNow - startIndex).TotalSeconds);
#endif

            return lUsers.SelectMany(u => u);
        }

        /// <inheritdoc/>
        public virtual async Task RemoveClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claim);

            // Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.


            TUserClaim? local = await GetUserClaimAsync(user, claim).ConfigureAwait(false);
            if (local is not null)
            {
                TUserClaim deleteUserClaim = CreateUserClaim(user, claim);
                IdentityUserIndex deleteUserClaimIndex = CreateClaimIndex(_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)), claim.Type, claim.Value);

                var tasks = new Task[]
                {
                    _indexTable.DeleteEntityAsync(deleteUserClaimIndex.PartitionKey, deleteUserClaimIndex.RowKey,  TableConstants.ETagWildcard),
                    _userTable.DeleteEntityAsync(deleteUserClaim.PartitionKey, deleteUserClaim.RowKey,  TableConstants.ETagWildcard)
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

        }

        /// <inheritdoc/>
        public override async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claim);
            ArgumentNullException.ThrowIfNull(newClaim);

            // Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.
            BatchOperationHelper bHelper = new BatchOperationHelper(_userTable);

            TUserClaim? local = await GetUserClaimAsync(user, claim).ConfigureAwait(false);
            List<Task> tasks = new List<Task>(3);
            var userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id));
            if (local is not null)
            {
                TUserClaim deleteClaim = CreateUserClaim(user, claim);
                bHelper.DeleteEntity(deleteClaim.PartitionKey!, deleteClaim.RowKey!, TableConstants.ETagWildcard);
                var deleteClaimIndex = CreateClaimIndex(userPartitionKey, claim.Type, claim.Value);
                tasks.Add(_indexTable.DeleteEntityAsync(deleteClaimIndex.PartitionKey, deleteClaimIndex.RowKey, ifMatch: TableConstants.ETagWildcard, cancellationToken: cancellationToken));
            }
            TUserClaim item = CreateUserClaim(user, newClaim);

            bHelper.AddEntity(item);
            tasks.Add(_indexTable.UpsertEntityAsync(CreateClaimIndex(userPartitionKey, newClaim.Type, newClaim.Value), mode: TableUpdateMode.Replace, cancellationToken: cancellationToken));

            tasks.Add(bHelper.SubmitBatchAsync(cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claims);

            // Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.
            List<Task> tasks = [];
            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            BatchOperationHelper bHelper = new BatchOperationHelper(_userTable);
            var userClaims = await GetClaimsAsync(user, cancellationToken).ConfigureAwait(false);
            foreach (Claim claim in claims)
            {
                Claim? local = (from uc in userClaims
                               where uc.Type == claim.Type && uc.Value == claim.Value
                               select uc).FirstOrDefault();
                if (local is not null)
                {
                    var deleteUserClaim = CreateUserClaim(user, local);
                    bHelper.DeleteEntity(deleteUserClaim.PartitionKey!, deleteUserClaim.RowKey!, TableConstants.ETagWildcard);
                    var deleteClaimIndex = CreateClaimIndex(userPartitionKey, local.Type, local.Value);
                    tasks.Add(_indexTable.DeleteEntityAsync(deleteClaimIndex.PartitionKey, deleteClaimIndex.RowKey, ifMatch: TableConstants.ETagWildcard, cancellationToken: cancellationToken));
                }
            }
            tasks.Add(bHelper.SubmitBatchAsync(cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override TUserClaim CreateUserClaim(TUser user, Claim claim)
        {
            TUserClaim uc = base.CreateUserClaim(user, claim);
            ((IGenerateKeys)uc).GenerateKeys(_keyHelper);
            uc.PartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            uc.UserId = user.Id;
            uc.ETag = TableConstants.ETagWildcard;
            return uc;
        }

        /// <inheritdoc/>
        protected override TUserLogin CreateUserLogin(TUser user, UserLoginInfo login)
        {
            TUserLogin ul = base.CreateUserLogin(user, login);
            ((IGenerateKeys)ul).GenerateKeys(_keyHelper);
            ul.PartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            ul.UserId = user.Id;
            ul.ETag = TableConstants.ETagWildcard;
            return ul;
        }

        /// <inheritdoc/>
        public override async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            TUserLogin? item = await FindUserLoginAsync(userPartitionKey, loginProvider, providerKey).ConfigureAwait(false);

            if (item is not null)
            {
                Model.IdentityUserIndex index = CreateLoginIndex(userPartitionKey, item.LoginProvider, item.ProviderKey);
                await Task.WhenAll(_indexTable.DeleteEntityAsync(index.PartitionKey, index.RowKey, TableConstants.ETagWildcard, cancellationToken),
                                    _userTable.DeleteEntityAsync(item.PartitionKey, item.RowKey, TableConstants.ETagWildcard, cancellationToken)).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public override async Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            //Only remove the username if different
            //The UserManager calls UpdateAsync which will generate the new username index record
            if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName != userName)
            {
                await DeleteUserNameIndexAsync(ConvertIdToString(user.Id), user.UserName).ConfigureAwait(false);
            }
            user.UserName = userName;
        }

        /// <inheritdoc/>
        public override async Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            //Only remove the email if different
            //The UserManager calls UpdateAsync which will generate the new email index record
            if (!string.IsNullOrWhiteSpace(user?.Email) && user!.Email != email)
            {
                await DeleteEmailIndexAsync(_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user!.Id)).ToString(), user!.Email!).ConfigureAwait(false);
            }
            user!.Email = email;
        }

        /// <summary>
        /// Tracking with issue #104
        /// </summary>
        /// <param name="user"></param>
        /// <param name="loginProvider"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task SetTokenAsync(TUser user, string loginProvider, string name, string? value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(user);

            var token = await FindTokenAsync(user, loginProvider, name, cancellationToken);

            token ??= CreateUserToken(user, loginProvider, name, value);
            token.Value = value;

            await AddUserTokenAsync(token);
        }

        /// <summary>
        /// Fixes deletes for non-unique emails for users.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="plainEmail"></param>
        /// <returns></returns>
        protected async Task DeleteEmailIndexAsync(string userId, string plainEmail)
        {
            var filterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, _keyHelper.GenerateRowKeyUserEmail(plainEmail)),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, userId));

            await foreach (IdentityUserIndex de in _indexTable.QueryAsync<IdentityUserIndex>(filter: filterString.ToString()).ConfigureAwait(false))
            {
                if (de?.Id is not null && de.Id.Equals(userId, StringComparison.OrdinalIgnoreCase))
                {
                    await _indexTable.DeleteEntityAsync(de.PartitionKey, de.RowKey, TableConstants.ETagWildcard).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Deletes UserName Index
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        protected async Task DeleteUserNameIndexAsync(string? userId, string? userName)
        {
            if (!string.IsNullOrWhiteSpace(userName) &&
                !string.IsNullOrWhiteSpace(userId))
            {
                var userPartitionKey = _keyHelper.GenerateRowKeyUserId(userId);
                var result = await _indexTable.GetEntityOrDefaultAsync<IdentityUserIndex>(_keyHelper.GenerateRowKeyUserName(userName).ToString(), userPartitionKey.ToString()).ConfigureAwait(false);
                if (result is not null)
                {
                    _ = await _indexTable.DeleteEntityAsync(result.PartitionKey, result.RowKey, TableConstants.ETagWildcard).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Deletes all user table information by userId
        /// </summary>
        /// <param name="userId">UserId in PartitionKey format</param>
        /// <param name="userRows"></param>
        /// <returns></returns>
        protected Task DeleteAllUserRowsAsync(string userId, IEnumerable<TableEntity> userRows)
        {
            var deleteBatchHelper = new BatchOperationHelper(_userTable);
            foreach (TableEntity delUserRow in userRows)
            {
                if (userId == delUserRow.PartitionKey)
                {
                    deleteBatchHelper.DeleteEntity(delUserRow.PartitionKey, delUserRow.RowKey, TableConstants.ETagWildcard);
                }
            }

            return deleteBatchHelper.SubmitBatchAsync();
        }

        /// <inheritdoc/>
        public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            string userPartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            List<Task> tasks = new List<Task>(3)
            {
                _userTable.UpdateEntityAsync(user, TableConstants.ETagWildcard, mode: TableUpdateMode.Replace, cancellationToken),

                _indexTable.UpsertEntityAsync(CreateUserNameIndex(userPartitionKey, user.UserName), mode: TableUpdateMode.Replace, cancellationToken: cancellationToken)
            };
            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                Model.IdentityUserIndex indexEmail = CreateEmailIndex(userPartitionKey, user!.Email!);

                tasks.Add(_indexTable.UpsertEntityAsync(indexEmail, mode: TableUpdateMode.Replace, cancellationToken: cancellationToken));
            }

            try
            {
                await Task.WhenAll([.. tasks]).ConfigureAwait(false);
                return IdentityResult.Success;
            }
            catch (AggregateException aggex)
            {
                aggex.Flatten();
                return IdentityResult.Failed(new IdentityError() { Code = "002", Description = "User Update Failed." });
            }

        }

        /// <summary>
        /// Table Storage Context access
        /// </summary>
        public TContext Context => _context;

        /// <summary>
        /// Generates new IdentityUserIndex for a user claim - suitable for a crud operation
        /// </summary>
        /// <param name="userPartitionKey"></param>
        /// <param name="claimType"></param>
        /// <param name="claimValue"></param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateClaimIndex(ReadOnlySpan<char> userPartitionKey, string? claimType, string? claimValue)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userPartitionKey.ToString(),
                PartitionKey = _keyHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue).ToString(),
                RowKey = userPartitionKey.ToString(),
                KeyVersion = _keyHelper.KeyVersion,
                ETag = TableConstants.ETagWildcard
            };
        }


        /// <summary>
        ///  Creates an role -> users index suitable for a crud operation
        /// </summary>
        /// <param name="userPartitionKey">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
        /// <param name="plainRoleName">Plain role name</param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateRoleIndex(string userPartitionKey, string? plainRoleName)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userPartitionKey,
                PartitionKey = _keyHelper.GenerateRowKeyIdentityUserRole(plainRoleName).ToString(),
                RowKey = userPartitionKey,
                KeyVersion = _keyHelper.KeyVersion,
                ETag = TableConstants.ETagWildcard
            };
        }

        /// <summary>
        /// Creates an email index suitable for a crud operation
        /// </summary>
        /// <param name="userid">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
        /// <param name="email">Plain email address.</param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateEmailIndex(ReadOnlySpan<char> userid, string email)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userid.ToString(),
                PartitionKey = _keyHelper.GenerateRowKeyUserEmail(email).ToString(),
                RowKey = userid.ToString(),
                KeyVersion = _keyHelper.KeyVersion,
                ETag = TableConstants.ETagWildcard
            };
        }

        /// <summary>
        /// Create an index for getting the user id based on his user name,
        /// </summary>
        /// <param name="userPartitionKey"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateUserNameIndex(ReadOnlySpan<char> userPartitionKey, string? userName)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userPartitionKey.ToString(),
                PartitionKey = _keyHelper.GenerateRowKeyUserName(userName).ToString(),
                RowKey = userPartitionKey.ToString(),
                KeyVersion = _keyHelper.KeyVersion,
                ETag = TableConstants.ETagWildcard
            };
        }

        /// <summary>
        /// Creates an IdentityUserIndex for Login suitable for a crud operation
        /// </summary>
        /// <param name="userPartitionKey"></param>
        /// <param name="loginProvider"></param>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateLoginIndex(string userPartitionKey, string loginProvider, string providerKey)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userPartitionKey,
                PartitionKey = _keyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey).ToString(),
                RowKey = _keyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey).ToString(),
                KeyVersion = _keyHelper.KeyVersion,
                ETag = TableConstants.ETagWildcard
            };

        }

        /// <inheritdoc/>
        public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(claim);

            string getTableQueryFilterByUserId(string userId)
            {
                var rowFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, userId),
                    TableOperators.Or,
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, _keyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value)));

                var tqFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId), TableOperators.And,
                    rowFilter);
                return tqFilter.ToString();
            }

            return (await GetUsersByIndexQueryAsync(GetUserByClaimIndexQuery(claim).ToString(), (userId, ct) =>
            {
                return GetUserAggregateQueryAsync(userId, setFilterByUserId: getTableQueryFilterByUserId, whereClaim: (uc) =>
                {
                    return uc.RowKey.AsSpan().Equals(_keyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value), StringComparison.OrdinalIgnoreCase);
                }, cancellationToken);

            }, cancellationToken).ConfigureAwait(false)).ToList();
        }

        /// <summary>
        /// Base returns null as default, override returns string.empty as default
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            return Task.FromResult(user.Id is not null ? user.Id.ToString()??string.Empty : string.Empty);
        }

        /// <inheritdoc/>
        protected override async Task<TUserToken?> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            return await _userTable.GetEntityOrDefaultAsync<TUserToken>(_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString(),
                    _keyHelper.GenerateRowKeyIdentityUserToken(loginProvider, name).ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates an IdentityUserIndex for UserToken suitable for a crud operation
        /// </summary>
        /// <param name="user"></param>
        /// <param name="loginProvider"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override TUserToken CreateUserToken(TUser user, string loginProvider, string name, string? value)
        {
            TUserToken item = base.CreateUserToken(user, loginProvider, name, value);
            ((Model.IGenerateKeys)item).GenerateKeys(_keyHelper);
            item.PartitionKey = _keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id)).ToString();
            item.UserId = user.Id;
            item.ETag = TableConstants.ETagWildcard;
            return item;
        }

        /// <inheritdoc/>
        protected override Task AddUserTokenAsync(TUserToken token)
        {
            return _userTable.UpsertEntityAsync(token, TableUpdateMode.Replace);
        }

        /// <inheritdoc/>
        protected override Task RemoveUserTokenAsync(TUserToken token)
        {
            return _userTable.DeleteEntityAsync(token.PartitionKey, token.RowKey, TableConstants.ETagWildcard);
        }
    }
}
