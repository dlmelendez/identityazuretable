// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if !net45
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.WindowsAzure.Storage.Table;
using Model = ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using System.Threading;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public class UserStore<TUser> : UserStore<TUser, IdentityCloudContext>
        where TUser : Model.IdentityUser, new()
    {
        public UserStore()
            : this(new IdentityCloudContext())
        {
        }

        public UserStore(IdentityCloudContext context)
            : base(context)
        {
        }
    }

    public class UserStore<TUser, TContext> : UserStore<TUser, Model.IdentityRole, TContext>
		, IUserStore<TUser> 
		where TUser : Model.IdentityUser, new()
		where TContext : IdentityCloudContext, new()
    {
		public UserStore(TContext context)
			: base(context)
		{
		}

		//Fixing code analysis issue CA1063
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}

	public class UserStore<TUser, TRole, TContext> : UserStore<TUser, TRole, string, Model.IdentityUserLogin, Model.IdentityUserRole, Model.IdentityUserClaim, Model.IdentityUserToken, TContext>
		, IUserStore<TUser>
		where TUser : Model.IdentityUser, new()
		where TRole : Model.IdentityRole<string, Model.IdentityUserRole>, new()
		where TContext : IdentityCloudContext, new()
	{
		public UserStore(TContext context)
			: base(context)
		{
		}
	}

    public class UserStore<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim, TUserToken, TContext> : 
		IUserLoginStore<TUser>
		, IUserClaimStore<TUser>
		, IUserRoleStore<TUser>
		, IUserPasswordStore<TUser>
		, IUserSecurityStampStore<TUser>
		, IUserEmailStore<TUser>
		, IUserPhoneNumberStore<TUser>
		, IUserTwoFactorStore<TUser>
		, IUserLockoutStore<TUser>
		, IUserStore<TUser>
        , IUserAuthenticationTokenStore<TUser>
        , IDisposable
		where TUser : Model.IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>, new()
		where TRole : Model.IdentityRole<TKey, TUserRole>, new()
		where TKey : IEquatable<TKey>
		where TUserLogin : Model.IdentityUserLogin<TKey>, new()
		where TUserRole : Model.IdentityUserRole<TKey>, new()
		where TUserClaim : Model.IdentityUserClaim<TKey>, new()
        where TUserToken : Model.IdentityUserToken<TKey>, new()
        where TContext : IdentityCloudContext, new()
    {
		private bool _disposed;

		private CloudTable _userTable;
        private CloudTable _roleTable;
		private CloudTable _indexTable;

		public UserStore(TContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}
			this.Context = context;
			this._userTable = context.UserTable;
			this._indexTable = context.IndexTable;
            this._roleTable = context.RoleTable;

        }

		public async Task CreateTablesIfNotExists()
		{
			Task<bool>[] tasks = new Task<bool>[]
					{
						Context.RoleTable.CreateIfNotExistsAsync(),
						Context.UserTable.CreateIfNotExistsAsync(),
						Context.IndexTable.CreateIfNotExistsAsync(),
					};
			await Task.WhenAll(tasks);
		}

		public virtual async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (claims == null)
			{
				throw new ArgumentNullException("claims");
			}

			BatchOperationHelper bop = new BatchOperationHelper();
			foreach (Claim c in claims)
			{
				TUserClaim item = Activator.CreateInstance<TUserClaim>();
				item.UserId = user.Id;
				item.ClaimType = c.Type;
				item.ClaimValue = c.Value;
				((Model.IGenerateKeys)item).GenerateKeys();

				user.Claims.Add(item);

				bop.Add(TableOperation.Insert(item));
			}

			await bop.ExecuteBatchAsync(_userTable);
		}

        public virtual async Task AddClaimAsync(TUser user, Claim claim)
		{
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (claim == null)
			{
				throw new ArgumentNullException("claim");
			}
			TUserClaim item = Activator.CreateInstance<TUserClaim>();
			item.UserId = user.Id;
			item.ClaimType = claim.Type;
			item.ClaimValue = claim.Value;
			((Model.IGenerateKeys)item).GenerateKeys();

			user.Claims.Add(item);

			await _userTable.ExecuteAsync(TableOperation.Insert(item));
		}

		public virtual async Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (login == null)
			{
				throw new ArgumentNullException("login");
			}
			TUserLogin item = Activator.CreateInstance<TUserLogin>();
			item.UserId = user.Id;
			item.ProviderKey = login.ProviderKey;
			item.LoginProvider = login.LoginProvider;
			((Model.IGenerateKeys)item).GenerateKeys();

			user.Logins.Add(item);
			Model.IdentityUserIndex index = CreateLoginIndex(item.UserId.ToString(), item.LoginProvider, item.ProviderKey);

			await Task.WhenAll(_userTable.ExecuteAsync(TableOperation.Insert(item))
				, _indexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)));

		}

		public virtual async Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (string.IsNullOrWhiteSpace(roleName))
			{
				throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
			}

			TRole roleT = Activator.CreateInstance<TRole>();
			roleT.Name = roleName;
			((Model.IGenerateKeys)roleT).GenerateKeys();

			TUserRole userToRole = Activator.CreateInstance<TUserRole>();
			userToRole.UserId = user.Id;
			userToRole.RoleId = roleT.Id;
			userToRole.RoleName = roleT.Name;
			TUserRole item = userToRole;

			((Model.IGenerateKeys)item).GenerateKeys();

			user.Roles.Add(item);
			roleT.Users.Add(item);

			await _userTable.ExecuteAsync(TableOperation.Insert(item));

		}

		public async virtual Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();

			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			((Model.IGenerateKeys)user).GenerateKeys();

			try
			{

			    List<Task> tasks = new List<Task>(2);
			    tasks.Add(_userTable.ExecuteAsync(TableOperation.Insert(user)));

			    if (!string.IsNullOrWhiteSpace(user.Email))
			    {
				    Model.IdentityUserIndex index = CreateEmailIndex(user.Id.ToString(), user.Email);
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

		public async virtual Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}

			List<Task> tasks = new List<Task>(50);

			BatchOperationHelper userBatch = new BatchOperationHelper();

			userBatch.Add(TableOperation.Delete(user));
			//Don't use the BatchHelper for login index table, partition keys are likely not the same
			//since they are based on logonprovider and providerkey
			foreach (var userLogin in user.Logins)
			{
				userBatch.Add(TableOperation.Delete(userLogin));

				Model.IdentityUserIndex indexLogin = CreateLoginIndex(user.Id.ToString(), userLogin.LoginProvider, userLogin.ProviderKey);

				tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(indexLogin)));
			}

			foreach (var userRole in user.Roles)
			{
				userBatch.Add(TableOperation.Delete(userRole));
			}

			foreach (var userClaim in user.Claims)
			{
				userBatch.Add(TableOperation.Delete(userClaim));
			}

			tasks.Add(userBatch.ExecuteBatchAsync(_userTable));
			if (!string.IsNullOrWhiteSpace(user.Email))
			{
				Model.IdentityUserIndex indexEmail = CreateEmailIndex(user.Id.ToString(), user.Email);
				tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(indexEmail)));
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

		public void Dispose()
		{
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

		public async virtual Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
		{
            ThrowIfDisposed();

			string rowKey = KeyHelper.GenerateRowKeyUserLoginInfo(loginProvider, providerKey);
			string partitionKey = KeyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey);
			var loginQuery = GetUserIdByIndex(partitionKey, rowKey);

			return await GetUserAggregateAsync(loginQuery);
		}

		public async Task<TUser> FindByEmailAsync(string plainEmail, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			return await this.GetUserAggregateAsync(FindByEmailQuery(plainEmail));
		}

		public async Task<IEnumerable<TUser>> FindAllByEmailAsync(string plainEmail)
		{
			this.ThrowIfDisposed();
			return await this.GetUsersAggregateByIndexQueryAsync(FindByEmailQuery(plainEmail));
		}

		private TableQuery FindByEmailQuery(string plainEmail)
		{
			return GetUserIdsByIndex(KeyHelper.GenerateRowKeyUserEmail(plainEmail));
		}

		private TableQuery GetUserIdByIndex(string partitionkey, string rowkey)
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

        private TableQuery GetUserIdsByIndex(string partitionKey)
        {
            TableQuery tq = new TableQuery();
            tq.SelectColumns = new List<string>() { "Id" };
            tq.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            return tq;
        }

        public virtual Task<TUser> FindByIdAsync(TKey userId)
		{
			this.ThrowIfDisposed();
			return this.GetUserAggregateAsync(userId.ToString());
		}

		public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			return this.GetUserAggregateAsync(userId.ToString());
		}

		public Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			return this.GetUserAggregateAsync(KeyHelper.GenerateRowKeyUserName(normalizedUserName));
		}

		public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<int>(user.AccessFailedCount);
		}

		public virtual Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<IList<Claim>>(user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList());
		}

		public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.Email);
		}

		public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<bool>(user.EmailConfirmed);
		}

		public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<bool>(user.LockoutEnabled);
		}

		public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<DateTimeOffset?>(user.LockoutEndDateUtc);
		}

		public virtual Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<IList<UserLoginInfo>>((from l in user.Logins select new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList<UserLoginInfo>());
		}

		public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.PasswordHash);
		}

		public Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.PhoneNumber);
		}

		public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<bool>(user.PhoneNumberConfirmed);
		}

		public async virtual Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException(nameof(user));
			}
            if (user.Id == null)
            {
                throw new ArgumentNullException(nameof(user.Id));
            }

            const string roleName = "RoleName";
            string userId = user.Id.ToString();
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
                _userTable.ExecuteQuery(tq)
                .Where(w=> w.Properties[roleName] != null)
                .Select(d => d.Properties[roleName].StringValue)
                .Where(di=> !string.IsNullOrWhiteSpace(di))
                .ToList();

            int userRoleTotalCount = userRoles.Count;
            if (userRoleTotalCount > 0)
            {
                const double pageSize = 10d;
                double maxPages = Math.Ceiling((double)userRoleTotalCount / pageSize);

                List<Task<List<string>>> tasks = new List<Task<List<string>>>((int)maxPages);

                for(int iPageIndex = 0; iPageIndex < maxPages; iPageIndex++)
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
                    tasks.Add(Task.Run(() =>
                    {
                        return _roleTable.ExecuteQuery(tqRoles)
                            .Where(w => w.Properties[rName] != null)
                            .Select(d => d.Properties[rName].StringValue)
                            .Where(di => !string.IsNullOrWhiteSpace(di))
                            .ToList();
                    }));
                }
                await Task.WhenAll(tasks);
                return tasks.Select(s=> s.Result).SelectMany(m=> m).ToList() as IList<string>;
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

		public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.SecurityStamp);
		}

		public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<bool>(user.TwoFactorEnabled);
		}

		private Task<TUser> GetUserAggregateAsync(string userId)
		{
			var userResults = GetUserAggregateQuery(userId).ToList();
			return Task.FromResult<TUser>(GetUserAggregate(userId, userResults));
		}

		private IEnumerable<DynamicTableEntity> GetUserAggregateQuery(string userId)
		{
			TableQuery tq = new TableQuery();
			string partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId);

            string rowFilter1 = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, Constants.RowKeyConstants.PreFixIdentityUserToken);
            string rowFilter2 = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, "U_");
            string rowFilter = TableQuery.CombineFilters(rowFilter1, TableOperators.Or, rowFilter2);

            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            return _userTable.ExecuteQuery(tq);
		}

		protected IEnumerable<TUser> GetUserAggregateQuery(IList<string> userIds)
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
#if !netstandard
			Parallel.ForEach(listTqs, (q) =>
			{
				Parallel.ForEach(_userTable.ExecuteQuery(q)
					.ToList()
					.GroupBy(g => g.PartitionKey), (s) =>
					{
						bag.Add(GetUserAggregate(s.Key, s));
					});
			});
#else
			List<Task> tasks = new List<Task>(listTqs.Count);
			listTqs.ForEach((q) =>
		   {
			   tasks.Add(Task.Run(new Action(() =>
			   {
				   _userTable.ExecuteQuery(q)
				   .ToList()
				   .GroupBy(g => g.PartitionKey)
				   .ToList().ForEach((s) =>
				   {
					   bag.Add(GetUserAggregate(s.Key, s));
				   });
			   })));
			});
			Task.WaitAll(tasks.ToArray());
#endif
#if DEBUG
			Debug.WriteLine("GetUserAggregateQuery (GetUserAggregateTotal): {0} seconds", (DateTime.UtcNow - startUserAggTotal).TotalSeconds);
#endif
			return bag;
		}

		private TUser GetUserAggregate(string userId, IEnumerable<DynamicTableEntity> userResults)
		{
			TUser user = default(TUser);
			var vUser = userResults.Where(u => u.RowKey.Equals(userId) && u.PartitionKey.Equals(userId)).SingleOrDefault();
			var op = new OperationContext();

			if (vUser != null)
			{
				//User
				user = Activator.CreateInstance<TUser>();
				user.ReadEntity(vUser.Properties, op);
				user.PartitionKey = vUser.PartitionKey;
				user.RowKey = vUser.RowKey;
				user.ETag = vUser.ETag;
				user.Timestamp = vUser.Timestamp;

				//Roles                            
				foreach (var log in userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserRole)
					 && u.PartitionKey.Equals(userId)))
				{
					TUserRole trole = Activator.CreateInstance<TUserRole>();
					trole.ReadEntity(log.Properties, op);
					trole.PartitionKey = log.PartitionKey;
					trole.RowKey = log.RowKey;
					trole.ETag = log.ETag;
					trole.Timestamp = log.Timestamp;
					user.Roles.Add(trole);
				}
				foreach (var log in userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserClaim)
					 && u.PartitionKey.Equals(userId)))
				{
					TUserClaim tclaim = Activator.CreateInstance<TUserClaim>();
					tclaim.ReadEntity(log.Properties, op);
					tclaim.PartitionKey = log.PartitionKey;
					tclaim.RowKey = log.RowKey;
					tclaim.ETag = log.ETag;
					tclaim.Timestamp = log.Timestamp;
					user.Claims.Add(tclaim);
				}
				//Logins
				foreach (var log in userResults.Where(u => u.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserLogin)
					 && u.PartitionKey.Equals(userId)))
				{
					TUserLogin tlogin = Activator.CreateInstance<TUserLogin>();
					tlogin.ReadEntity(log.Properties, op);
					tlogin.PartitionKey = log.PartitionKey;
					tlogin.RowKey = log.RowKey;
					tlogin.ETag = log.ETag;
					tlogin.Timestamp = log.Timestamp;
					user.Logins.Add(tlogin);
				}
			}
			return user;
		}

		private async Task<TUser> GetUserAggregateAsync(TableQuery queryUser)
		{
			return await Task.Run(() =>
			{
				var user = _indexTable.ExecuteQuery(queryUser).FirstOrDefault();
				if (user != null)
				{
					string userId = user.Properties["Id"].StringValue;
					var userResults = GetUserAggregateQuery(userId).ToList();
					return GetUserAggregate(userId, userResults);
				}

				return default(TUser);
			});
		}

		private async Task<IEnumerable<TUser>> GetUsersAggregateByIndexQueryAsync(TableQuery queryUser)
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
            Action<List<string>> getUsers = new Action<List<string>>((ids) =>
            {
                lUsers.Add(GetUserAggregateQuery(ids).ToList());
            });
            while (token != null)
            {
                var response = await _indexTable.ExecuteQuerySegmentedAsync(queryUser, token);
                var tempUserIds = response.ToList().Select(u => u.Properties["Id"].StringValue).Distinct().ToList();
                taskBatch.Add(Task.Run(() => { getUsers(tempUserIds); }));
                if(taskBatch.Count % taskMax == 0)
                {
                    await Task.WhenAll(taskBatch.ToArray());
                    taskBatch.Clear();
                }
                token = response.ContinuationToken;
            }

            if (taskBatch.Count > 0)
            {
                await Task.WhenAll(taskBatch.ToArray());
                taskBatch.Clear();
            }
#if DEBUG
            Debug.WriteLine("GetUsersAggregateByIndexQueryAsync (Index query): {0} seconds", (DateTime.UtcNow - startIndex).TotalSeconds);
#endif

			return lUsers.SelectMany(u=> u);
		}

		protected async Task<IEnumerable<TUser>> GetUsersAggregateByIdsAsync(IList<string> userIds)
		{
			return await Task.Run(() =>
			{
				return GetUserAggregateQuery(userIds);
			});
		}

		public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult<bool>(user.PasswordHash != null);
		}

		public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.AccessFailedCount++;
			return Task.FromResult<int>(user.AccessFailedCount);
		}

		public async virtual Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (string.IsNullOrWhiteSpace(roleName))
			{
				throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
			}

            List<Task<bool>> tasks = new List<Task<bool>>(2);

            string userId = user.Id.ToString();
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
            tasks.Add(Task.Run(() => { return _userTable.ExecuteQuery(tq).Any(); }));

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
            return await Task.FromResult(_roleTable.ExecuteQuery(tqRoles).Any());
        }

		public virtual async Task RemoveClaimAsync(TUser user, Claim claim)
		{
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (claim == null)
			{
				throw new ArgumentNullException("claim");
			}

			if (string.IsNullOrWhiteSpace(claim.Type))
			{
				throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "claim.Type");
			}

			// Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.

			TUserClaim local = (from uc in user.Claims
								where uc.RowKey == KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value)
								select uc).FirstOrDefault();
			{
				user.Claims.Remove(local);
				TableOperation deleteOperation = TableOperation.Delete(local);
				await _userTable.ExecuteAsync(deleteOperation);
			}

		}
		public async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (claim == null)
			{
				throw new ArgumentNullException("claim");
			}
			if (newClaim == null)
			{
				throw new ArgumentNullException("newClaim");
			}

			// Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.
			BatchOperationHelper bop = new BatchOperationHelper();

			TUserClaim local = (from uc in user.Claims
								where uc.RowKey == KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value)
								select uc).FirstOrDefault();
			if (local != null)
			{
				user.Claims.Remove(local);
				TableOperation deleteOperation = TableOperation.Delete(local);
				bop.Add(deleteOperation);
			}
			TUserClaim item = Activator.CreateInstance<TUserClaim>();
			item.UserId = user.Id;
			item.ClaimType = newClaim.Type;
			item.ClaimValue = newClaim.Value;
			((Model.IGenerateKeys)item).GenerateKeys();

			user.Claims.Add(item);

			bop.Add(TableOperation.Insert(item));
			await bop.ExecuteBatchAsync(_userTable);
		}

		public async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (claims == null)
			{
				throw new ArgumentNullException("claims");
			}

			// Claim ctor doesn't allow Claim.Value to be null. Need to allow string.empty.
			BatchOperationHelper bop = new BatchOperationHelper();

			foreach (Claim claim in claims)
			{
				TUserClaim local = (from uc in user.Claims
									where uc.RowKey == KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value)
									select uc).FirstOrDefault();
				{
					user.Claims.Remove(local);
					TableOperation deleteOperation = TableOperation.Delete(local);
					bop.Add(deleteOperation);
				}
			}
			await bop.ExecuteBatchAsync(_userTable);
		}


		public virtual async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (string.IsNullOrWhiteSpace(roleName))
			{
				throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
			}

			TUserRole item = user.Roles.FirstOrDefault<TUserRole>(r => r.RowKey == KeyHelper.GenerateRowKeyIdentityRole(roleName));
			if (item != null)
			{
				user.Roles.Remove(item);
				TableOperation deleteOperation = TableOperation.Delete(item);

				await _userTable.ExecuteAsync(deleteOperation);
			}
		}

		public virtual async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}

            TUserLogin item = user.Logins.SingleOrDefault<TUserLogin>(l => l.RowKey == KeyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey));
			if (item != null)
			{
				user.Logins.Remove(item);
				Model.IdentityUserIndex index = CreateLoginIndex(item.UserId.ToString(), item.LoginProvider, item.ProviderKey);
				await Task.WhenAll(_indexTable.ExecuteAsync(TableOperation.Delete(index)),
									_userTable.ExecuteAsync(TableOperation.Delete(item)));
			}
		}

		public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.AccessFailedCount = 0;
			return TaskCacheHelper.CompletedTask;
		}

		public async Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}

			//Only remove the email if different
			//The UserManager calls UpdateAsync which will generate the new email index record
			if (!string.IsNullOrWhiteSpace(user.Email) && user.Email != email)
			{
				await DeleteEmailIndexAsync(user.Id.ToString(), user.Email);
			}
			user.Email = email;
		}

		//Fixes deletes for non-unique emails for users.
		private async Task DeleteEmailIndexAsync(string userId, string plainEmail)
		{
			TableQuery tq = new TableQuery();
            tq.FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, KeyHelper.GenerateRowKeyUserEmail(plainEmail)),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, userId));
            tq.SelectColumns = new List<string>() { "Id" };

			var indexes = _indexTable.ExecuteQuery(tq);

			foreach (DynamicTableEntity de in indexes)
			{
				if (de.Properties["Id"].StringValue.Equals(userId, StringComparison.OrdinalIgnoreCase))
				{
					await _indexTable.ExecuteAsync(TableOperation.Delete(de));
				}
			}
		}

		public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.EmailConfirmed = confirmed;
			return TaskCacheHelper.CompletedTask;
		}

		public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.LockoutEnabled = enabled;
			return TaskCacheHelper.CompletedTask;
		}

		public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
            user.LockoutEndDateUtc = lockoutEnd.HasValue ? new DateTime?(lockoutEnd.Value.DateTime.ToUniversalTime()) : null;
			return TaskCacheHelper.CompletedTask;
		}

		public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();

			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.PasswordHash = passwordHash;
			return TaskCacheHelper.CompletedTask;
		}

		public Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.PhoneNumber = phoneNumber;
			return TaskCacheHelper.CompletedTask;
		}

		public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.PhoneNumberConfirmed = confirmed;
			return TaskCacheHelper.CompletedTask;
		}

		public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.SecurityStamp = stamp;
			return TaskCacheHelper.CompletedTask;
		}

		public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.TwoFactorEnabled = enabled;
			return TaskCacheHelper.CompletedTask;
		}

		private void ThrowIfDisposed()
		{
			if (this._disposed)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
		}

		private TUser ChangeUserName(TUser user)
		{
			List<Task> taskList = new List<Task>(50);
			string userNameKey = KeyHelper.GenerateRowKeyUserName(user.UserName);

			Debug.WriteLine("Old User.Id: {0}", user.Id);
			string oldUserId = user.Id.ToString();
			Debug.WriteLine(string.Format("New User.Id: {0}", KeyHelper.GenerateRowKeyUserName(user.UserName)));
			//Get the old user
			var userRows = GetUserAggregateQuery(user.Id.ToString()).ToList();
			//Insert the new user name rows
			BatchOperationHelper insertBatchHelper = new BatchOperationHelper();
			foreach (DynamicTableEntity oldUserRow in userRows)
			{
				ITableEntity dte = null;
				if (oldUserRow.RowKey == user.Id.ToString())
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
			BatchOperationHelper deleteBatchHelper = new BatchOperationHelper();
			foreach (DynamicTableEntity delUserRow in userRows)
			{
				deleteBatchHelper.Add(TableOperation.Delete(delUserRow));
			}
			taskList.Add(deleteBatchHelper.ExecuteBatchAsync(_userTable));

			// Create the new email index
			if (!string.IsNullOrWhiteSpace(user.Email))
			{
				taskList.Add(DeleteEmailIndexAsync(oldUserId, user.Email));

				Model.IdentityUserIndex indexEmail = CreateEmailIndex(userNameKey, user.Email);

				taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexEmail)));
			}

			// Update the external logins
			foreach (var login in user.Logins)
			{
				Model.IdentityUserIndex indexLogin = CreateLoginIndex(userNameKey, login.LoginProvider, login.ProviderKey);
				taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexLogin)));
				login.PartitionKey = userNameKey;
			}

			// Update the claims partitionkeys
			foreach (var claim in user.Claims)
			{
				claim.PartitionKey = userNameKey;
			}

			// Update the roles partitionkeys
			foreach (var role in user.Roles)
			{
				role.PartitionKey = userNameKey;
			}

			Task.WaitAll(taskList.ToArray());
			return user;
		}


		public async virtual Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}

			List<Task> tasks = new List<Task>(2);

			string userNameKey = KeyHelper.GenerateRowKeyUserName(user.UserName);
			if (user.Id.ToString() != userNameKey)
			{
				tasks.Add(Task.FromResult<TUser>(ChangeUserName(user)));
			}
			else
			{
				tasks.Add(_userTable.ExecuteAsync(TableOperation.Replace(user)));

				if (!string.IsNullOrWhiteSpace(user.Email))
				{
					Model.IdentityUserIndex indexEmail = CreateEmailIndex(user.Id.ToString(), user.Email);

					tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexEmail)));
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



        /// <summary>
        /// Creates an email index suitable for a crud operation
        /// </summary>
        /// <param name="userid">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
        /// <param name="email">Plain email address.</param>
        /// <returns></returns>
        private Model.IdentityUserIndex CreateEmailIndex(string userid, string email)
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

        private Model.IdentityUserIndex CreateLoginIndex(string userid, string loginProvider, string providerKey)
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

        protected async Task<IList<TUser>> GetUsersByRowkeyAsync(string rowKey)
        {
#if DEBUG
            DateTime startIndex = DateTime.UtcNow;
#endif
            ConcurrentBag<List<TUser>> lUsers = new ConcurrentBag<List<TUser>>();
            TableContinuationToken token = new TableContinuationToken();
            const int takeCount = 100;
            const int taskMax = 10;
            TableQuery tq = new TableQuery();
            tq.FilterString = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal,
                rowKey);
            tq.SelectColumns = new List<string>() { "PartitionKey" };
            tq.TakeCount = takeCount;
            List<Task> taskBatch = new List<Task>(taskMax);
            Action<List<string>> getUsers = new Action<List<string>>((ids) =>
            {
                lUsers.Add(GetUserAggregateQuery(ids).ToList());
            });
            while (token != null)
            {
                var response = await _userTable.ExecuteQuerySegmentedAsync(tq, token);
                var tempUserIds = response.ToList().Select(u => u.PartitionKey).Distinct().ToList();
                taskBatch.Add(Task.Run(() => { getUsers(tempUserIds); }));
                if (taskBatch.Count % taskMax == 0)
                {
                    await Task.WhenAll(taskBatch.ToArray());
                    taskBatch.Clear();
                }
                token = response.ContinuationToken;
            }

            if (taskBatch.Count > 0)
            {
                await Task.WhenAll(taskBatch.ToArray());
                taskBatch.Clear();
            }
#if DEBUG
            Debug.WriteLine("GetUsersByRowkeyAsync (Threaded query): {0} seconds", (DateTime.UtcNow - startIndex).TotalSeconds);
#endif

            return lUsers.SelectMany(u => u).ToList();
        }

        public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (string.IsNullOrWhiteSpace(claim.Type))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(claim.Type));
            }
            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(claim.Value));
            }
            return await GetUsersByRowkeyAsync(KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value));

        }

		public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
            if (await RoleExistsAsync(roleName))
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(roleName));
                }
                return await GetUsersByRowkeyAsync(KeyHelper.GenerateRowKeyIdentityUserRole(roleName));
            }

            return new List<TUser>();
        }

        public virtual Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException(nameof(user));
			}

            return Task.FromResult(user.Id != null ? user.Id.ToString() : string.Empty);
		}

		public virtual Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult(user.UserName);
		}

		public virtual Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.UserName = userName;
			return TaskCacheHelper.CompletedTask;
		}

		public virtual Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult(user.NormalizedUserName);
		}

		public virtual Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.NormalizedUserName = normalizedName;
			return TaskCacheHelper.CompletedTask;
		}

		public virtual Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult(user.NormalizedEmail);
		}

		public virtual Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.NormalizedEmail = normalizedEmail;
			return TaskCacheHelper.CompletedTask;
		}

        private async Task<TUserToken> FindUserTokenAsync(TUser user, string loginProvider, string tokenName)
        {
            var tableOp = TableOperation.Retrieve<TUserToken>(user.Id.ToString(), 
                KeyHelper.GenerateRowKeyIdentityUserToken(loginProvider, tokenName));

            var result = await _userTable.ExecuteAsync(tableOp);

            if(result.Result != null)
            {
                return (TUserToken)result.Result;
            }

            return default(TUserToken);
        }

        public async Task SetTokenAsync(TUser user, string loginProvider, string tokenName, string tokenValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(tokenName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(tokenName));
            }
            if (string.IsNullOrWhiteSpace(loginProvider))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(loginProvider));
            }
            if (tokenValue == null)
            {
                tokenValue = string.Empty;
            }

            var item = await FindUserTokenAsync(user, loginProvider, tokenName);
            if(item == null)
            {
                item = Activator.CreateInstance<TUserToken>();
                item.TokenName = tokenName;
                item.TokenValue = tokenValue;
                item.PartitionKey = user.Id.ToString();
                item.LoginProvider = loginProvider;
                ((Model.IGenerateKeys)item).GenerateKeys();
            }
            else
            {
                item.TokenValue = tokenValue;
            }

            await _userTable.ExecuteAsync(TableOperation.InsertOrReplace(item as TableEntity));
        }

        public async Task RemoveTokenAsync(TUser user, string loginProvider, string tokenName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(tokenName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(tokenName));
            }
            if (string.IsNullOrWhiteSpace(loginProvider))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(loginProvider));
            }

            var item = await FindUserTokenAsync(user, loginProvider, tokenName);
            if (item != null)
            {
                await _userTable.ExecuteAsync(TableOperation.Delete(item as TableEntity));
            }

        }

        public async Task<string> GetTokenAsync(TUser user, string loginProvider, string tokenName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(tokenName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(tokenName));
            }
            if (string.IsNullOrWhiteSpace(loginProvider))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(loginProvider));
            }

            var item = await FindUserTokenAsync(user, loginProvider, tokenName);
            return item?.TokenValue;
        }
    }
}
#endif