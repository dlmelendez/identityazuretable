// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    /// <summary>
    /// Supports IdentityUser with Roles, Claims, and Tokens as collection properties.
    /// Use this for backwards compat existing code. Otherwise, data is the same.
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class UserStore<TUser, TRole, TContext> : UserStoreV2<TUser, TRole, TContext>
        , IUserStore<TUser>
        where TUser : Model.IdentityUser, new()
        where TRole : Model.IdentityRole<string, Model.IdentityUserRole>, new()
        where TContext : IdentityCloudContext, new()
    {
        public UserStore(TContext context, IdentityConfiguration config) : base(context,config) { }

        //Fixing code analysis issue CA1063
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }


            return (await GetUsersAggregateByIndexQueryAsync(GetUserByClaimQuery(claim), (userId) => {
                return GetUserAggregateQueryAsync(userId, setFilterByUserId: null, whereRole: null, whereClaim: (uc) =>
                {
                    return uc.RowKey == KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value);
                });

            })).ToList();
        }

        public override async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            if (await RoleExistsAsync(roleName))
            {
                return (await this.GetUsersAggregateByIndexQueryAsync(GetUserByRoleQuery(roleName), (userId) => {
                    return GetUserAggregateQueryAsync(userId, setFilterByUserId: null, whereClaim: null, whereRole: (ur) =>
                    {
                        return ur.RowKey == KeyHelper.GenerateRowKeyIdentityUserRole(roleName);
                    });

                })).ToList();
            }

            return new List<TUser>();
        }

        protected override async Task<TUser> GetUserAggregateAsync(TableQuery queryUser) 
        {
            var user = (await _indexTable.ExecuteQueryAsync(queryUser)).FirstOrDefault();
            if (user != null)
            {
                string userId = user.Properties["Id"].StringValue;
                var userResults = (await GetUserAggregateQueryAsync(userId)).ToList();
                var result =  MapUserAggregate(userId, userResults);
                if(result.User != null)
                {
                    TUser u = result.User;
                    result.Claims.ToList().ForEach(c => u.Claims.Add(c));
                    result.Logins.ToList().ForEach(l => u.Logins.Add(l));
                    result.Roles.ToList().ForEach(r => u.Roles.Add(r));
                    result.Tokens.ToList().ForEach(t => u.Tokens.Add(t));
                    return u;
                }
            }

            return default(TUser);
        }

        private TUser MapUser(string userId, IEnumerable<DynamicTableEntity> userResults)
        {
            var result = MapUserAggregate(userId, userResults);
            if (result.User != null)
            {
                TUser u = result.User;
                result.Claims.ToList().ForEach(c => u.Claims.Add(c));
                result.Logins.ToList().ForEach(l => u.Logins.Add(l));
                result.Roles.ToList().ForEach(r => u.Roles.Add(r));
                result.Tokens.ToList().ForEach(t => u.Tokens.Add(t));
                return u;
            }

            return default(TUser);
        }

        protected override async Task<TUser> GetUserAsync(string userId)
        {
            return MapUser(userId, await GetUserAggregateQueryAsync(userId));
        }

        protected async override Task<IEnumerable<TUser>> GetUserQueryAsync(IList<string> userIds)
        {
            const double pageSize = 50.0;
            int pages = (int)Math.Ceiling(((double)userIds.Count / pageSize));
            List<TableQuery> listTqs = new List<TableQuery>(pages);
            List<string> tempUserIds = null;

            for (int currentPage = 1; currentPage <= pages; currentPage++)
            {
                if (currentPage > 1)
                {
                    tempUserIds = userIds.Skip(((currentPage - 1) * (int)pageSize)).Take((int)pageSize).ToList();
                }
                else
                {
                    tempUserIds = userIds.Take((int)pageSize).ToList();
                }

                TableQuery tq = new TableQuery();
                for (int i = 0; i < tempUserIds.Count; i++)
                {

                    string temp = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, tempUserIds[i]);

                    if (i > 0)
                    {
                        tq.FilterString = TableQuery.CombineFilters(tq.FilterString, TableOperators.Or, temp);
                    }
                    else
                    {
                        tq.FilterString = temp;
                    }

                }
                listTqs.Add(tq);

            }

            ConcurrentBag<TUser> bag = new ConcurrentBag<TUser>();
#if DEBUG
            DateTime startUserAggTotal = DateTime.UtcNow;
#endif
            List<Task> tasks = new List<Task>(listTqs.Count);
            listTqs.ForEach((q) =>
            {
                tasks.Add(Task.Run(async () =>
                {
                    (await _userTable.ExecuteQueryAsync(q))
                    .ToList()
                    .GroupBy(g => g.PartitionKey)
                    .ToList()
                    .ForEach((s) =>
                    {
                        bag.Add(MapUser(s.Key, s));
                    });
                }));
            });
            await Task.WhenAll(tasks);
#if DEBUG
            Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
#endif
            return bag;
        }

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
        UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
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
        protected bool _disposed;

        protected CloudTable _userTable;
        protected CloudTable _roleTable;
        protected CloudTable _indexTable;
        
        private IdentityConfiguration _config = null;
        public UserStoreV2(TContext context,IdentityConfiguration config) : base(new IdentityErrorDescriber())
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this._userTable = context.UserTable;
            this._indexTable = context.IndexTable;
            this._roleTable = context.RoleTable;
            this._config = config;
        }

        public override IQueryable<TUser> Users => throw new NotImplementedException();

        public Task CreateTablesIfNotExists()
        {
            Task<bool>[] tasks = new Task<bool>[]
                    {
                        Context.RoleTable.CreateIfNotExistsAsync(),
                        Context.UserTable.CreateIfNotExistsAsync(),
                        Context.IndexTable.CreateIfNotExistsAsync(),
                    };
            return Task.WhenAll(tasks);
        }

        public override async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claims == null) throw new ArgumentNullException("claims");

            BatchOperationHelper bop = new BatchOperationHelper();

            List<Task> tasks = new List<Task>();

            foreach (Claim c in claims)
            {
                bop.Add(TableOperation.Insert(CreateUserClaim(user, c)));
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateClaimIndex(ConvertIdToString(user.Id), c.Type, c.Value))));
            }

            tasks.Add(bop.ExecuteBatchAsync(_userTable));
            await Task.WhenAll(tasks);
        }

        public virtual async Task AddClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claim == null) throw new ArgumentNullException(nameof(claim));

            List<Task> tasks = new List<Task>(2);

            tasks.Add(_userTable.ExecuteAsync(TableOperation.Insert(CreateUserClaim(user, claim))));
            tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateClaimIndex(ConvertIdToString(user.Id), claim.Type, claim.Value))));

            await Task.WhenAll(tasks);
        }

        public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (login == null) throw new ArgumentNullException(nameof(login));

            TUserLogin item = CreateUserLogin(user, login);

            Model.IdentityUserIndex index = CreateLoginIndex(ConvertIdToString(user.Id), item.LoginProvider, item.ProviderKey);

            return Task.WhenAll(_userTable.ExecuteAsync(TableOperation.Insert(item))
                , _indexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)));
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
            userToRole.UserId = user.Id;
            userToRole.RoleId = roleT.Id;
            userToRole.RoleName = roleT.Name;
            TUserRole item = userToRole;

            ((Model.IGenerateKeys)item).GenerateKeys();

            roleT.Users.Add(item);

            List<Task> tasks = new List<Task>(2);

            tasks.Add(_userTable.ExecuteAsync(TableOperation.Insert(item)));
            tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateRoleIndex(ConvertIdToString(user.Id), roleName))));

            await Task.WhenAll(tasks);
        }

        public async override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            ((Model.IGenerateKeys)user).GenerateKeys();

            try
            {
                List<Task> tasks = new List<Task>(2);
                tasks.Add(_userTable.ExecuteAsync(TableOperation.Insert(user)));
                if (_config.EnableImmutableUserId)
                {
                    tasks.Add(_indexTable.ExecuteAsync(TableOperation.Insert(CreateUserNameIndex(ConvertIdToString(user.Id), user.UserName))));
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    Model.IdentityUserIndex index = CreateEmailIndex(ConvertIdToString(user.Id), user.Email);
                    tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)));
                }

                await Task.WhenAll(tasks.ToArray());
                return IdentityResult.Success;
            }
            catch (AggregateException aggex)
            {
                aggex.Flatten();
                return IdentityResult.Failed(new IdentityError() { Code = "001", Description = "User Creation Failed." });
            }
        }

        public async override Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            List<Task> tasks = new List<Task>(50);
            string userId = ConvertIdToString(user.Id);
            var userRows = (await GetUserAggregateQueryAsync(userId)).ToList();

            tasks.Add(DeleteAllUserRows(userId, userRows));

            var userAgg = MapUserAggregate(userId, userRows);

            //Don't use the BatchHelper for login index table, partition keys are likely not the same
            //since they are based on logonprovider and providerkey
            foreach (var userLogin in userAgg.Logins)
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateLoginIndex(ConvertIdToString(user.Id), userLogin.LoginProvider, userLogin.ProviderKey))));
            }

            foreach (var userRole in userAgg.Roles)
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateRoleIndex(ConvertIdToString(user.Id), userRole.RoleName))));
            }

            foreach (var userClaim in userAgg.Claims)
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateClaimIndex(ConvertIdToString(user.Id), userClaim.ClaimType, userClaim.ClaimValue))));
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateEmailIndex(ConvertIdToString(user.Id), user.Email))));
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


        public new void Dispose()
        {
            base.Dispose();
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (this.Context != null)
                {
                    this.Context.Dispose();
                }
                this._indexTable = null;
                this._userTable = null;
                this._roleTable = null;
                this.Context = null;
                this._disposed = true;
            }
        }

        protected override async Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await FindUserLoginAsync(ConvertIdToString(userId), loginProvider, providerKey);
        }

        protected async Task<TUserLogin> FindUserLoginAsync(string userId, string loginProvider, string providerKey)
        {
            string rowKey = KeyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);

            TableQuery tq = new TableQuery();
            tq.TakeCount = 1;
            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));

            var result = await _userTable.ExecuteQueryAsync(tq);
            var log = result.FirstOrDefault();
            if (log != null)
            {
                TUserLogin tlogin = MapTableEntity<TUserLogin>(log);
                return tlogin;
            }

            return null;
        }

        protected override async Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            string rowKey = KeyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
            string partitionKey = KeyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey);
            var loginQuery = GetUserIdByIndex(partitionKey, rowKey);

            var indexInfo = (await _indexTable.ExecuteQueryAsync(loginQuery)).FirstOrDefault();
            if (indexInfo != null)
            {
                string userId = indexInfo.Properties["Id"].StringValue;
                return await FindUserLoginAsync(userId, loginProvider, providerKey);
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
        public override Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            string rowKey = KeyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
            string partitionKey = KeyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey);
            var loginQuery = GetUserIdByIndex(partitionKey, rowKey);

            return GetUserAggregateAsync(loginQuery);
        }

        public override Task<TUser> FindByEmailAsync(string plainEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            return this.GetUserAggregateAsync(FindByEmailQuery(plainEmail));
        }

     

        public async Task<IEnumerable<TUser>> FindAllByEmailAsync(string plainEmail)
        {
            List<TUser> result = new List<TUser>();
            this.ThrowIfDisposed();
            var users = await GetUsersAggregateByIndexQueryAsync(FindByEmailQuery(plainEmail), GetUserQueryAsync);
            //Double check the index with the actual user
            foreach(TUser user in users)
            {
                if(KeyHelper.GenerateRowKeyUserEmail(plainEmail) == KeyHelper.GenerateRowKeyUserEmail(user.Email))
                {
                    result.Add(user);
                }
            }
            return result;
        }

        protected override Task<TUser> FindUserAsync(TKey userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return GetUserAsync(ConvertIdToString(userId));
        }

        protected TableQuery GetUserByRoleQuery(string plainRoleName)
             => GetUserIdsByIndex(KeyHelper.GenerateRowKeyIdentityUserRole(plainRoleName));

        protected TableQuery GetUserByClaimQuery(Claim claim)
         => GetUserIdsByIndex(KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value));

        protected TableQuery FindByEmailQuery(string plainEmail)
         => GetUserIdsByIndex(KeyHelper.GenerateRowKeyUserEmail(plainEmail));

        protected TableQuery FindByUserNameQuery(string userName)
            => GetUserIdsByIndex(KeyHelper.GenerateRowKeyUserName(userName));

        protected TableQuery GetUserIdByIndex(string partitionkey, string rowkey)
        {
            TableQuery tq = new TableQuery();
            tq.TakeCount = 1;
            tq.SelectColumns = new List<string>() { "Id" };
            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionkey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowkey));
            return tq;
        }

        protected TableQuery GetUserIdsByIndex(string partitionKey)
        {
            TableQuery tq = new TableQuery();
            tq.SelectColumns = new List<string>() { "Id" };
            tq.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            return tq;
        }

        public override Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            return GetUserAsync(userId);
        }

        public override Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (!_config.EnableImmutableUserId)
            {
                return GetUserAsync(KeyHelper.GenerateRowKeyUserName(normalizedUserName));
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                var userId = FindByUserNameQuery(normalizedUserName);
                return GetUserAggregateAsync(userId);
            }
        }

        protected async Task<TUserClaim> GetUserClaimAsync(TUser user, Claim claim)
        {
            var tr = await _userTable.ExecuteAsync(TableOperation.Retrieve<TUserClaim>(ConvertIdToString(user.Id),
                KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value)));
            if (tr.Result != null)
            {
                return (TUserClaim)tr.Result;
            }
            return null;
        }


        public override async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            List<Claim> rClaims = new List<Claim>();

            TableQuery tq = new TableQuery();
            string partitionFilter =
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, user.PartitionKey);
            string rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserClaim),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "D_"));
            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            var results = await _userTable.ExecuteQueryAsync(tq);
            foreach (var de in results)
            {
                TUserClaim tclaim = MapTableEntity<TUserClaim>(de);
                //1.7 Claim rowkey migration 
                if (KeyHelper.GenerateRowKeyIdentityUserClaim(tclaim.ClaimType, tclaim.ClaimValue) == tclaim.RowKey)
                {
                    rClaims.Add(new Claim(tclaim.ClaimType, tclaim.ClaimValue));
                }
            }

            return rClaims;
        }

        protected T MapTableEntity<T>(DynamicTableEntity dte) where T : ITableEntity, new()
        {
            T t = new T()
            {
                ETag = dte.ETag,
                PartitionKey = dte.PartitionKey,
                RowKey = dte.RowKey,
                Timestamp = dte.Timestamp
            };
            t.ReadEntity(dte.Properties, new OperationContext());
            return t;
        }

        public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            List<UserLoginInfo> rLogins = new List<UserLoginInfo>();

            TableQuery tq = new TableQuery();
            string partitionFilter =
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, user.PartitionKey);
            string rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserLogin),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "M_"));
            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            var results = await _userTable.ExecuteQueryAsync(tq);
            foreach (var de in results)
            {
                TUserLogin tul = MapTableEntity<TUserLogin>(de);
                rLogins.Add(new UserLoginInfo(tul.LoginProvider, tul.ProviderKey, tul.ProviderDisplayName));
            }

            return rLogins;
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
            string userId = ConvertIdToString(user.Id);
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
                (await _userTable.ExecuteQueryAsync(tq))
                .Where(w => w.Properties[roleName] != null)
                .Select(d => d.Properties[roleName].StringValue)
                .Where(di => !string.IsNullOrWhiteSpace(di))
                .ToList();

            int userRoleTotalCount = userRoles.Count;
            if (userRoleTotalCount > 0)
            {
                const double pageSize = 10d;
                double maxPages = Math.Ceiling((double)userRoleTotalCount / pageSize);

                List<Task<List<string>>> tasks = new List<Task<List<string>>>((int)maxPages);

                for (int iPageIndex = 0; iPageIndex < maxPages; iPageIndex++)
                {
                    int skip = (int)(iPageIndex * pageSize);
                    List<string> userRolesTemp = skip > 0 ? userRoles.Skip(skip).Take((int)pageSize).ToList() :
                        userRoles.Take((int)pageSize).ToList();
                    int userRolesTempCount = userRolesTemp.Count;

                    string queryTemp = string.Empty;
                    for (int iRoleCounter = 0; iRoleCounter < userRolesTempCount; iRoleCounter++)
                    {
                        if (iRoleCounter == 0)
                        {
                            queryTemp = BuildRoleQuery(userRolesTemp[iRoleCounter]);
                        }
                        else
                        {
                            queryTemp = TableQuery.CombineFilters(queryTemp, TableOperators.Or, BuildRoleQuery(userRolesTemp[iRoleCounter]));
                        }
                    }
                    const string rName = "Name";
                    TableQuery tqRoles = new TableQuery();
                    tqRoles.FilterString = queryTemp;
                    tqRoles.SelectColumns = new List<string>() { rName };
                    tasks.Add(Task.Run(async () =>
                    {
                        return (await _roleTable.ExecuteQueryAsync(tqRoles))
                            .Where(w => w.Properties[rName] != null)
                            .Select(d => d.Properties[rName].StringValue)
                            .Where(di => !string.IsNullOrWhiteSpace(di))
                            .ToList();
                    }));
                }
                await Task.WhenAll(tasks);
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


        protected async virtual Task<TUser> GetUserAsync(string userId)
        {
            var tr = await _userTable.ExecuteAsync(TableOperation.Retrieve<TUser>(userId, userId));
            if(tr.Result != null)
            {
                return (TUser)tr.Result;
            }
            return null;
        }

        protected Task<IEnumerable<DynamicTableEntity>> GetUserAggregateQueryAsync(string userId)
        {
            TableQuery tq = new TableQuery();
            tq.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId);

            return _userTable.ExecuteQueryAsync(tq);
        }

        protected async Task<IEnumerable<TUser>> GetUserAggregateQueryAsync(IList<string> userIds,
                Func<string, string> setFilterByUserId = null,
                Func<TUserRole, bool> whereRole = null,
                Func<TUserClaim, bool> whereClaim = null)
        {
            const double pageSize = 50.0;
            int pages = (int)Math.Ceiling(((double)userIds.Count / pageSize));
            List<TableQuery> listTqs = new List<TableQuery>(pages);
            List<string> tempUserIds = null;

            for (int currentPage = 1; currentPage <= pages; currentPage++)
            {
                if (currentPage > 1)
                {
                    tempUserIds = userIds.Skip(((currentPage - 1) * (int)pageSize)).Take((int)pageSize).ToList();
                }
                else
                {
                    tempUserIds = userIds.Take((int)pageSize).ToList();
                }

                TableQuery tq = new TableQuery();
                for (int i = 0; i < tempUserIds.Count; i++)
                {

                    string temp = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, tempUserIds[i]);
                    if(setFilterByUserId != null)
                    {
                        temp = setFilterByUserId(tempUserIds[i]);
                    }

                    if (i > 0)
                    {
                        tq.FilterString = TableQuery.CombineFilters(tq.FilterString, TableOperators.Or, temp);
                    }
                    else
                    {
                        tq.FilterString = temp;
                    }

                }
                listTqs.Add(tq);

            }

            ConcurrentBag<TUser> bag = new ConcurrentBag<TUser>();
#if DEBUG
            DateTime startUserAggTotal = DateTime.UtcNow;
#endif
            List<Task> tasks = new List<Task>(listTqs.Count);
            listTqs.ForEach((q) =>
            {
                tasks.Add(
                     _userTable.ExecuteQueryAsync(q)
                     .ContinueWith((taskResults) =>
                     {
                         //ContinueWith returns completed task. Calling .Result is safe here.
                         var r = taskResults.Result;
                         r.ToList()
                         .GroupBy(g => g.PartitionKey)
                         .ToList().ForEach((s) =>
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
                         });
                     }, TaskContinuationOptions.ExecuteSynchronously)                   
                    );
            });
            await Task.WhenAll(tasks);
#if DEBUG
            Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
            Debug.WriteLine("GetUserAggregateQuery (Return Count): {0} userIds", bag.Count());
#endif
            return bag;
        }

        protected async virtual Task<IEnumerable<TUser>> GetUserQueryAsync(IList<string> userIds)
        {
            const double pageSize = 50.0;
            int pages = (int)Math.Ceiling(((double)userIds.Count / pageSize));
            List<TableQuery> listTqs = new List<TableQuery>(pages);
            List<string> tempUserIds = null;

            for (int currentPage = 1; currentPage <= pages; currentPage++)
            {
                if (currentPage > 1)
                {
                    tempUserIds = userIds.Skip(((currentPage - 1) * (int)pageSize)).Take((int)pageSize).ToList();
                }
                else
                {
                    tempUserIds = userIds.Take((int)pageSize).ToList();
                }

                TableQuery tq = new TableQuery();
                for (int i = 0; i < tempUserIds.Count; i++)
                {

                    string temp = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, tempUserIds[i]), TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, tempUserIds[i]));
                    if (i > 0)
                    {
                        tq.FilterString = TableQuery.CombineFilters(tq.FilterString, TableOperators.Or, temp);
                    }
                    else
                    {
                        tq.FilterString = temp;
                    }

                }
                listTqs.Add(tq);

            }

            ConcurrentBag<TUser> bag = new ConcurrentBag<TUser>();
#if DEBUG
            DateTime startUserAggTotal = DateTime.UtcNow;
#endif
            List<Task> tasks = new List<Task>(listTqs.Count);
            listTqs.ForEach((q) =>
            {
                tasks.Add(Task.Run(async () =>
                {
                    (await _userTable.ExecuteQueryAsync(q))
                    .ToList()
                    .ForEach((s) =>
                    {
                        bag.Add(MapTableEntity<TUser>(s));
                    });
                }));
            });
            await Task.WhenAll(tasks);
#if DEBUG
            Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
#endif
            return bag;
        }

        protected (TUser User, 
            IEnumerable<TUserRole> Roles, 
            IEnumerable<TUserClaim> Claims, 
            IEnumerable<TUserLogin> Logins, 
            IEnumerable<TUserToken> Tokens) 
            MapUserAggregate(string userId, IEnumerable<DynamicTableEntity> userResults, 
                Func<TUserRole, bool> whereRole = null,
                Func<TUserClaim, bool> whereClaim = null)
        {
                            
            TUser user = default(TUser);
            List<TUserRole> roles = new List<TUserRole>();
            List<TUserClaim> claims = new List<TUserClaim>();
            List<TUserLogin> logins = new List<TUserLogin>();
            List<TUserToken> tokens = new List<TUserToken>();

            var vUser = userResults.Where(u => u.RowKey.Equals(userId) && u.PartitionKey.Equals(userId)).SingleOrDefault();
            var op = new OperationContext();

            if (vUser != null)
            {
                //User
                user = MapTableEntity<TUser>(vUser);

                //Roles
                foreach (var log in userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserRole)
                     && u.PartitionKey.Equals(userId)))
                {
                    TUserRole trole = MapTableEntity<TUserRole>(log);
                    roles.Add(trole);
                }
                //Claims
                foreach (var log in userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserClaim)
                     && u.PartitionKey.Equals(userId)))
                {
                    TUserClaim tclaim = MapTableEntity<TUserClaim>(log);
                    //Added for 1.7 rowkey change
                    if (KeyHelper.GenerateRowKeyIdentityUserClaim(tclaim.ClaimType, tclaim.ClaimValue).Equals(tclaim.RowKey))
                    {
                        claims.Add(tclaim);
                    }
#if DEBUG
                    else
                    {
                        Debug.WriteLine("Claim partition and row keys not added to user: " + log.PartitionKey + " " + log.RowKey);
                    }
#endif
                }
                //Logins
                foreach (var log in userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserLogin)
                 && u.PartitionKey.Equals(userId)))
                {
                    TUserLogin tlogin = MapTableEntity<TUserLogin> (log);
                    logins.Add(tlogin);
                }

                //Tokens
                foreach (var log in userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserToken)
                 && u.PartitionKey.Equals(userId)))
                {
                    TUserToken token = MapTableEntity<TUserToken>(log);
                    tokens.Add(token);
                }
            }
            return (user, roles, claims, logins, tokens);
        }

        protected virtual async Task<TUser> GetUserAggregateAsync(TableQuery queryUser) 
        {
            var user = (await _indexTable.ExecuteQueryAsync(queryUser)).FirstOrDefault();
            if (user != null)
            {
                string userId = user.Properties["Id"].StringValue;
                return await GetUserAsync(userId);
            }

            return default(TUser);
        }

        protected async Task<IEnumerable<TUser>> GetUsersAggregateByIndexQueryAsync(TableQuery queryUser, Func<List<string>, Task<IEnumerable<TUser>>> getUserFunc)
        {
#if DEBUG
            DateTime startIndex = DateTime.UtcNow;
#endif
            ConcurrentBag<List<TUser>> lUsers = new ConcurrentBag<List<TUser>>();
            TableContinuationToken token = new TableContinuationToken();
            const int takeCount = 100;
            const int taskMax = 10;
            queryUser.TakeCount = takeCount;
            List<Task> taskBatch = new List<Task>(taskMax);
            Func<List<string>, Task> getUsers = async (ids) =>
            {
                lUsers.Add((await getUserFunc(ids)).ToList());
            };
            while (token != null)
            {
                var response = await _indexTable.ExecuteQuerySegmentedAsync(queryUser, token);
                var tempUserIds = response.ToList().Select(u => u.Properties["Id"].StringValue).Distinct().ToList();
                taskBatch.Add(getUsers(tempUserIds));
                if (taskBatch.Count % taskMax == 0)
                {
                    await Task.WhenAll(taskBatch);
                    taskBatch.Clear();
                }
                token = response.ContinuationToken;
            }

            if (taskBatch.Count > 0)
            {
                await Task.WhenAll(taskBatch);
                taskBatch.Clear();
            }
#if DEBUG
            Debug.WriteLine("GetUsersAggregateByIndexQueryAsync (Index query): {0} seconds", (DateTime.UtcNow - startIndex).TotalSeconds);
#endif

            return lUsers.SelectMany(u => u);
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

            List<Task<bool>> tasks = new List<Task<bool>>(2);

            string userId = ConvertIdToString(user.Id);
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
            tasks.Add(Task.Run(async () => (await _userTable.ExecuteQueryAsync(tq)).Any()));

            tasks.Add(RoleExistsAsync(roleName));

            await Task.WhenAll(tasks);

            return tasks.All(t => t.Result);
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            TableQuery tqRoles = new TableQuery();
            tqRoles.FilterString = BuildRoleQuery(roleName);
            tqRoles.SelectColumns = new List<string>() { "Name" };
            tqRoles.TakeCount = 1;
            return (await _roleTable.ExecuteQueryAsync(tqRoles)).Any();
        }

        public virtual async Task RemoveClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claim == null) throw new ArgumentNullException(nameof(claim));

            // Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.

            List<Task> tasks = new List<Task>(2);

            TUserClaim local = await GetUserClaimAsync(user, claim);
            if (local != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(CreateUserClaim(user, claim));
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateClaimIndex(ConvertIdToString(user.Id), claim.Type, claim.Value))));
                tasks.Add(_userTable.ExecuteAsync(deleteOperation));

                await Task.WhenAll(tasks);
            }

        }

        public override async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claim == null) throw new ArgumentNullException(nameof(claim));
            if (newClaim == null) throw new ArgumentNullException(nameof(newClaim));

            // Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.
            BatchOperationHelper bop = new BatchOperationHelper();

            TUserClaim local = await GetUserClaimAsync(user, claim);
            List<Task> tasks = new List<Task>(3);

            if (local != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(CreateUserClaim(user, claim));
                bop.Add(deleteOperation);
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateClaimIndex(ConvertIdToString(user.Id), claim.Type, claim.Value))));
            }
            TUserClaim item = CreateUserClaim(user, newClaim);

            bop.Add(TableOperation.Insert(item));
            tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateClaimIndex(ConvertIdToString(user.Id), newClaim.Type, newClaim.Value))));

            tasks.Add(bop.ExecuteBatchAsync(_userTable));

            await Task.WhenAll(tasks);
        }

        public override async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claims == null) throw new ArgumentNullException(nameof(claims));

            // Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.
            BatchOperationHelper bop = new BatchOperationHelper();
            List<Task> tasks = new List<Task>();

            var userClaims = await this.GetClaimsAsync(user);
            foreach (Claim claim in claims)
            {
                Claim local = (from uc in userClaims
                               where uc.Type == claim.Type && uc.Value == claim.Value
                               select uc).FirstOrDefault();
                if (local != null)
                {
                    TableOperation deleteOperation = TableOperation.Delete(CreateUserClaim(user, local));
                    bop.Add(deleteOperation);
                    tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateClaimIndex(ConvertIdToString(user.Id), local.Type, local.Value))));
                }
            }
            tasks.Add(bop.ExecuteBatchAsync(_userTable));
            await Task.WhenAll(tasks);
        }

        protected override TUserClaim CreateUserClaim(TUser user, Claim claim)
        {
            TUserClaim uc = base.CreateUserClaim(user, claim);
            ((IGenerateKeys)uc).GenerateKeys();
            uc.ETag = Constants.ETagWildcard;
            return uc;
        }

        protected override TUserLogin CreateUserLogin(TUser user, UserLoginInfo login)
        {
            TUserLogin ul = base.CreateUserLogin(user, login);
            ((IGenerateKeys)ul).GenerateKeys();
            ul.ETag = Constants.ETagWildcard;
            return ul;
        }

        public virtual async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));

            TUserRole item = null; 
            var tresult = await _userTable.ExecuteAsync(TableOperation.Retrieve<TUserRole>(user.PartitionKey, KeyHelper.GenerateRowKeyIdentityRole(roleName)));
            item = tresult.Result as TUserRole; 

            if (item != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(item);

                List<Task> tasks = new List<Task>(2);

                tasks.Add(_userTable.ExecuteAsync(deleteOperation));
                tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateRoleIndex(ConvertIdToString(user.Id), roleName))));

                await Task.WhenAll(tasks);
            }
        }

        public override async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            TUserLogin item = await FindUserLoginAsync(ConvertIdToString(user.Id), loginProvider, providerKey);

            if (item != null)
            {
                Model.IdentityUserIndex index = CreateLoginIndex(ConvertIdToString(user.Id), item.LoginProvider, item.ProviderKey);
                await Task.WhenAll(_indexTable.ExecuteAsync(TableOperation.Delete(index)),
                                    _userTable.ExecuteAsync(TableOperation.Delete(item)));
            }
        }

        public override async Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            //Only remove the email if different
            //The UserManager calls UpdateAsync which will generate the new email index record
            if (!string.IsNullOrWhiteSpace(user.Email) && user.Email != email)
            {
                await DeleteEmailIndexAsync(ConvertIdToString(user.Id), user.Email);
            }
            user.Email = email;
        }

        //Fixes deletes for non-unique emails for users.
        protected async Task DeleteEmailIndexAsync(string userId, string plainEmail)
        {
            TableQuery tq = new TableQuery();
            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, KeyHelper.GenerateRowKeyUserEmail(plainEmail)),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, userId));
            tq.SelectColumns = new List<string>() { "Id" };

            var indexes = await _indexTable.ExecuteQueryAsync(tq);

            foreach (DynamicTableEntity de in indexes)
            {
                if (de.Properties["Id"].StringValue.Equals(userId, StringComparison.OrdinalIgnoreCase))
                {
                    await _indexTable.ExecuteAsync(TableOperation.Delete(de));
                }
            }
        }

       
        protected async Task DeleteAllUserRows(string userId, IEnumerable<DynamicTableEntity> userRows)
        {
            BatchOperationHelper deleteBatchHelper = new BatchOperationHelper();
            foreach (DynamicTableEntity delUserRow in userRows)
            {
                if (userId == delUserRow.PartitionKey)
                {
                    deleteBatchHelper.Add(TableOperation.Delete(delUserRow));
                }
            }

            await deleteBatchHelper.ExecuteBatchAsync(_userTable);
        }


        protected async Task<TUser> ChangeUserNameAsync(TUser user)
        {
            List<Task> taskList = new List<Task>(50);
            string userNameKey = KeyHelper.GenerateRowKeyUserName(user.UserName);

            Debug.WriteLine("Old User.Id: {0}", user.Id);
            string oldUserId = ConvertIdToString(user.Id);
            Debug.WriteLine(string.Format("New User.Id: {0}", KeyHelper.GenerateRowKeyUserName(user.UserName)));
            //Get the old user
            var userRows = (await GetUserAggregateQueryAsync(ConvertIdToString(user.Id))).ToList();
            //Insert the new user name rows
            BatchOperationHelper insertBatchHelper = new BatchOperationHelper();
            foreach (DynamicTableEntity oldUserRow in userRows)
            {
                ITableEntity dte = null;
                if (oldUserRow.RowKey == ConvertIdToString(user.Id))
                {
                    Model.IGenerateKeys ikey = (Model.IGenerateKeys)user;
                    ikey.GenerateKeys();
                    dte = user;
                }
                else
                {
                    dte = new DynamicTableEntity(userNameKey, oldUserRow.RowKey,
                        Constants.ETagWildcard,
                        oldUserRow.Properties);
                }
                insertBatchHelper.Add(TableOperation.Insert(dte));
            }
            taskList.Add(insertBatchHelper.ExecuteBatchAsync(_userTable));
            //Delete the old user
            taskList.Add(DeleteAllUserRows(oldUserId, userRows));

            // Create the new email index
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                taskList.Add(DeleteEmailIndexAsync(oldUserId, user.Email));

                Model.IdentityUserIndex indexEmail = CreateEmailIndex(userNameKey, user.Email);

                taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexEmail)));
            }

            var userAgg = MapUserAggregate(oldUserId, userRows);

            // Update the external login indexes
            foreach (var login in userAgg.Logins)                                    
            {
                taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateLoginIndex(userNameKey, login.LoginProvider, login.ProviderKey))));
            }

            // Update the claims indexes
            foreach (var claim in userAgg.Claims)
            {
                taskList.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateClaimIndex(oldUserId, claim.ClaimType, claim.ClaimValue))));
                taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateClaimIndex(userNameKey, claim.ClaimType, claim.ClaimValue))));
            }

            // Update the roles indexes
            foreach (var role in userAgg.Roles)
            {
                taskList.Add(_indexTable.ExecuteAsync(TableOperation.Delete(CreateRoleIndex(oldUserId, role.RoleName))));
                taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateRoleIndex(userNameKey, role.RoleName))));
            }

            await Task.WhenAll(taskList.ToArray());
            return user;
        }


        public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            List<Task> tasks = new List<Task>(2);
            if (!_config.EnableImmutableUserId && 
                ConvertIdToString(user.Id) != KeyHelper.GenerateRowKeyUserName(user.UserName))
            {
                tasks.Add(ChangeUserNameAsync(user));
            }
            else
            {
                tasks.Add(_userTable.ExecuteAsync(TableOperation.Replace(user)));

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    Model.IdentityUserIndex indexEmail = CreateEmailIndex(ConvertIdToString(user.Id), user.Email);

                    tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexEmail)));
                }
                if (_config.EnableImmutableUserId)
                {
                    tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(CreateUserNameIndex(ConvertIdToString(user.Id), user.UserName))));
                }
            }

            try
            {
                await Task.WhenAll(tasks.ToArray());
                return IdentityResult.Success;
            }
            catch (AggregateException aggex)
            {
                aggex.Flatten();
                return IdentityResult.Failed(new IdentityError() { Code = "002", Description = "User Update Failed." });
            }

        }

        public TContext Context { get; private set; }

        protected Model.IdentityUserIndex CreateClaimIndex(string userid, string claimType, string claimValue)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = KeyHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue),
                RowKey = userid,
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };
        }


        /// <summary>
        ///  Creates an role -> users index suitable for a crud operation
        /// </summary>
        /// <param name="userid">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
        /// <param name="plainRoleName">Plain role name</param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateRoleIndex(string userid, string plainRoleName)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = KeyHelper.GenerateRowKeyIdentityUserRole(plainRoleName),
                RowKey = userid,
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };
        }

        /// <summary>
        /// Creates an email index suitable for a crud operation
        /// </summary>
        /// <param name="userid">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
        /// <param name="email">Plain email address.</param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateEmailIndex(string userid, string email)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = KeyHelper.GenerateRowKeyUserEmail(email),
                RowKey = userid,
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };
        }

        /// <summary>
        /// Create an index for getting the user id based on his user name,
        /// Only used if EnableImmutableUserId = true
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        protected Model.IdentityUserIndex CreateUserNameIndex(string userid, string userName)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = KeyHelper.GenerateRowKeyUserName(userName),
                RowKey = userid,
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };
        }

        protected Model.IdentityUserIndex CreateLoginIndex(string userid, string loginProvider, string providerKey)
        {
            return new Model.IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = KeyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey),
                RowKey = KeyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey),
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };

        }

        public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            string getTableQueryFilterByUserId(string userId)
            {
                string rowFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, userId),
                    TableOperators.Or,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value)));

                string tqFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId), TableOperators.And,
                    rowFilter);
                return tqFilter;
            }

            return (await this.GetUsersAggregateByIndexQueryAsync(GetUserByClaimQuery(claim), (userId) => {
                return GetUserAggregateQueryAsync(userId, setFilterByUserId: getTableQueryFilterByUserId, whereRole: null, whereClaim: (uc) => 
                {
                    return uc.RowKey == KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value);
                });

            })).ToList();
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

        /// <summary>
        /// Base returns null as default, override returns string.empty as default
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Id != null ? user.Id.ToString() : string.Empty);
        }       

        protected override async Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var tableOp = TableOperation.Retrieve<TUserToken>(ConvertIdToString(user.Id),
                KeyHelper.GenerateRowKeyIdentityUserToken(loginProvider, name));

            var result = await _userTable.ExecuteAsync(tableOp);

            if (result.Result != null)
            {
                return (TUserToken)result.Result;
            }

            return default(TUserToken);
        }

        protected override TUserToken CreateUserToken(TUser user, string loginProvider, string name, string value)
        {
            TUserToken item = base.CreateUserToken(user, loginProvider, name, value);
            ((Model.IGenerateKeys)item).GenerateKeys();
            item.PartitionKey = ConvertIdToString(user.Id);
            return item;
        }

        protected override async Task AddUserTokenAsync(TUserToken token)
        {
            await _userTable.ExecuteAsync(TableOperation.InsertOrReplace(token as ITableEntity));
        }


        protected override async Task RemoveUserTokenAsync(TUserToken token)
        {
            await _userTable.ExecuteAsync(TableOperation.Delete(token as ITableEntity));
        }
        

        
    }
}
