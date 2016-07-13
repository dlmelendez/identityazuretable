// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if net45
using ElCamino.AspNet.Identity.AzureTable.Model;
using Microsoft.AspNet.Identity;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Queryable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNet.Identity.AzureTable.Helpers;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace ElCamino.AspNet.Identity.AzureTable
{
	public class UserStore<TUser> : UserStore<TUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>, IUserStore<TUser>, IUserStore<TUser, string> where TUser : IdentityUser, new()
	{
		public UserStore()
			: this(new IdentityCloudContext())
		{

		}

		public UserStore(IdentityCloudContext context)
			: base(context)
		{
		}

		/// <summary>
		/// Simple table queries allowed. Projections are only allowed for TUser types. 
		/// </summary>
		public override IQueryable<TUser> Users
		{
			get
			{
				TableQueryHelper<TUser> helper = new TableQueryHelper<TUser>(
					(from t in base.Users
					 where t.RowKey.CompareTo(Constants.RowKeyConstants.PreFixIdentityUserName) > 0
					 select t).AsTableQuery()
					, base.GetUserAggregateQuery);

				return helper;

			}
		}
		//Fixing code analysis issue CA1063
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}

	public class UserStore<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> : IUserLoginStore<TUser, TKey>
		, IUserClaimStore<TUser, TKey>
		, IUserRoleStore<TUser, TKey>, IUserPasswordStore<TUser, TKey>
		, IUserSecurityStampStore<TUser, TKey>, IQueryableUserStore<TUser, TKey>
		, IUserEmailStore<TUser, TKey>, IUserPhoneNumberStore<TUser, TKey>
		, IUserTwoFactorStore<TUser, TKey>
		, IUserLockoutStore<TUser, TKey>
		, IUserStore<TUser, TKey>
		, IDisposable
		where TUser : IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>, new()
		where TRole : IdentityRole<TKey, TUserRole>, new()
		where TKey : IEquatable<TKey>
		where TUserLogin : IdentityUserLogin<TKey>, new()
		where TUserRole : IdentityUserRole<TKey>, new()
		where TUserClaim : IdentityUserClaim<TKey>, new()
	{
		private bool _disposed;

		private CloudTable _userTable;
		private CloudTable _indexTable;

		public UserStore(IdentityCloudContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			this.Context = context;
			this._userTable = context.UserTable;
			this._indexTable = context.IndexTable;
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
			((IGenerateKeys)item).GenerateKeys();

			user.Claims.Add(item);

			await _userTable.ExecuteAsync(TableOperation.Insert(item));
		}

		public virtual async Task AddLoginAsync(TUser user, UserLoginInfo login)
		{
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
			((IGenerateKeys)item).GenerateKeys();

			user.Logins.Add(item);
			IdentityUserIndex index = CreateLoginIndex(item.UserId.ToString(), item);

			await Task.WhenAll(_userTable.ExecuteAsync(TableOperation.Insert(item))
				, _indexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)));

		}

		public virtual async Task AddToRoleAsync(TUser user, string roleName)
		{
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
			((IGenerateKeys)roleT).GenerateKeys();

			TUserRole userToRole = Activator.CreateInstance<TUserRole>();
			userToRole.UserId = user.Id;
			userToRole.RoleId = roleT.Id;
			userToRole.RoleName = roleT.Name;
			TUserRole item = userToRole;

			((IGenerateKeys)item).GenerateKeys();

			user.Roles.Add(item);
			roleT.Users.Add(item);

			await _userTable.ExecuteAsync(TableOperation.Insert(item));

		}

		public async virtual Task CreateAsync(TUser user)
		{
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			((IGenerateKeys)user).GenerateKeys();

			List<Task> tasks = new List<Task>(2);
			tasks.Add(_userTable.ExecuteAsync(TableOperation.Insert(user)));

			if (!string.IsNullOrWhiteSpace(user.Email))
			{
				IdentityUserIndex index = CreateEmailIndex(user.Id.ToString(), user.Email);
				tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)));
			}

			await Task.WhenAll(tasks.ToArray());
		}

		public async virtual Task DeleteAsync(TUser user)
		{
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}

			List<Task> tasks = new List<Task>(50);

			BatchOperationHelper userBatch = new BatchOperationHelper();

			userBatch.Add(TableOperation.Delete(user));
			//Don't use the BatchHelper for login index table, partition keys are likely not the same
			//since they are based on provider
			foreach (var userLogin in user.Logins)
			{
				userBatch.Add(TableOperation.Delete(userLogin));

				IdentityUserIndex indexLogin = CreateLoginIndex(user.Id.ToString(), userLogin);

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
				IdentityUserIndex indexEmail = CreateEmailIndex(user.Id.ToString(), user.Email);
				tasks.Add(_indexTable.ExecuteAsync(TableOperation.Delete(indexEmail)));
			}

			await Task.WhenAll(tasks.ToArray());

		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
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
				this.Context = null;
				this._disposed = true;
			}
		}

		public async virtual Task<TUser> FindAsync(UserLoginInfo login)
		{
			ThrowIfDisposed();
			if (login == null)
			{
				throw new ArgumentNullException("login");
			}

			string rowKey = login.GenerateRowKeyUserLoginInfo();
			string partitionKey = KeyHelper.GeneratePartitionKeyIndexByLogin(login.LoginProvider);
			var loginQuery = GetUserIdByIndex(partitionKey, rowKey);

			return await GetUserAggregateAsync(loginQuery);
		}

		public async Task<TUser> FindByEmailAsync(string plainEmail)
		{
			this.ThrowIfDisposed();
			return await this.GetUserAggregateAsync(FindByEmailQuery(plainEmail));
		}

		public async Task<IEnumerable<TUser>> FindAllByEmailAsync(string plainEmail)
		{
			this.ThrowIfDisposed();
			return await this.GetUsersAggregateAsync(FindByEmailQuery(plainEmail));
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

		private TableQuery GetUserIdsByIndex(string rowkey)
		{
			TableQuery tq = new TableQuery();
			tq.SelectColumns = new List<string>() { "Id" };
			tq.FilterString = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowkey);
			return tq;
		}

		public virtual Task<TUser> FindByIdAsync(TKey userId)
		{
			this.ThrowIfDisposed();
			return this.GetUserAggregateAsync(userId.ToString());
		}

		public virtual Task<TUser> FindByNameAsync(string userName)
		{
			this.ThrowIfDisposed();
			return this.GetUserAggregateAsync(KeyHelper.GenerateRowKeyUserName(userName));
		}

		public Task<int> GetAccessFailedCountAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<int>(user.AccessFailedCount);
		}

		public virtual Task<IList<Claim>> GetClaimsAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<IList<Claim>>(user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList());
		}

		public Task<string> GetEmailAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.Email);
		}

		public Task<bool> GetEmailConfirmedAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<bool>(user.EmailConfirmed);
		}

		public Task<bool> GetLockoutEnabledAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<bool>(user.LockoutEnabled);
		}

		public Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<DateTimeOffset>(user.LockoutEndDateUtc.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(user.LockoutEndDateUtc.Value, DateTimeKind.Utc)) : new DateTimeOffset());
		}

		public virtual Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<IList<UserLoginInfo>>((from l in user.Logins select new UserLoginInfo(l.LoginProvider, l.ProviderKey)).ToList<UserLoginInfo>());
		}

		public Task<string> GetPasswordHashAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.PasswordHash);
		}

		public Task<string> GetPhoneNumberAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.PhoneNumber);
		}

		public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<bool>(user.PhoneNumberConfirmed);
		}

		public virtual Task<IList<string>> GetRolesAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}

			return Task.FromResult<IList<string>>(user.Roles.ToList().Select(r => r.RoleName).ToList());
		}

		public Task<string> GetSecurityStampAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			return Task.FromResult<string>(user.SecurityStamp);
		}

		public Task<bool> GetTwoFactorEnabledAsync(TUser user)
		{
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
			tq.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId);
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
			Parallel.ForEach(listTqs, (q) =>
			{
				Parallel.ForEach(_userTable.ExecuteQuery(q)
					.ToList()
					.GroupBy(g => g.PartitionKey), (s) =>
					{
						bag.Add(GetUserAggregate(s.Key, s));
					});
			});
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
			return await new TaskFactory<TUser>().StartNew(() =>
			{
				var user = _indexTable.ExecuteQuery(queryUser).FirstOrDefault();
				if (user != null)
				{
					string userId = user["Id"].StringValue;
					var userResults = GetUserAggregateQuery(userId).ToList();
					return GetUserAggregate(userId, userResults);
				}

				return default(TUser);
			});
		}

		private async Task<IEnumerable<TUser>> GetUsersAggregateAsync(TableQuery queryUser)
		{
#if DEBUG
			DateTime startIndex = DateTime.UtcNow;
#endif
			var userIds = _indexTable.ExecuteQuery(queryUser).ToList().Select(u => u["Id"].StringValue).Distinct().ToList();
#if DEBUG
			Debug.WriteLine("GetUsersAggregateAsync (Index query): {0} seconds", (DateTime.UtcNow - startIndex).TotalSeconds);
#endif
			List<TUser> list = new List<TUser>(userIds.Count);

			return await GetUsersAggregateByIdsAsync(userIds);
		}

		protected async Task<IEnumerable<TUser>> GetUsersAggregateByIdsAsync(IList<string> userIds)
		{
			return await new TaskFactory<IEnumerable<TUser>>().StartNew(() =>
			{
				return GetUserAggregateQuery(userIds);
			});
		}

		public Task<bool> HasPasswordAsync(TUser user)
		{
			return Task.FromResult<bool>(user.PasswordHash != null);
		}

		public Task<int> IncrementAccessFailedCountAsync(TUser user)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.AccessFailedCount++;
			return Task.FromResult<int>(user.AccessFailedCount);
		}

		public virtual Task<bool> IsInRoleAsync(TUser user, string roleName)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (string.IsNullOrWhiteSpace(roleName))
			{
				throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
			}

			//Removing the live query. UserManager calls FindById to hydrate the user object first.
			//No need to go to the table again.
			return Task.FromResult<bool>(user.Roles.Any(r => r.RowKey == KeyHelper.GenerateRowKeyIdentityRole(roleName)));
		}

		public virtual async Task RemoveClaimAsync(TUser user, Claim claim)
		{
			this.ThrowIfDisposed();
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

		public virtual async Task RemoveFromRoleAsync(TUser user, string roleName)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (string.IsNullOrWhiteSpace(roleName))
			{
				throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
			}

			TUserRole item = user.Roles.FirstOrDefault<TUserRole>(r => r.RowKey == KeyHelper.GenerateRowKeyIdentityRole(r.RoleName));
			if (item != null)
			{
				user.Roles.Remove(item);
				TableOperation deleteOperation = TableOperation.Delete(item);

				await _userTable.ExecuteAsync(deleteOperation);
			}
		}

		public virtual async Task RemoveLoginAsync(TUser user, UserLoginInfo login)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			if (login == null)
			{
				throw new ArgumentNullException("login");
			}
			string provider = login.LoginProvider;
			string key = login.ProviderKey;
			TUserLogin item = user.Logins.SingleOrDefault<TUserLogin>(l => l.RowKey == KeyHelper.GenerateRowKeyIdentityUserLogin(provider, key));
			if (item != null)
			{
				user.Logins.Remove(item);
				IdentityUserIndex index = CreateLoginIndex(item.UserId.ToString(), item);
				await Task.WhenAll(_indexTable.ExecuteAsync(TableOperation.Delete(index)),
									_userTable.ExecuteAsync(TableOperation.Delete(item)));
			}
		}

		public Task ResetAccessFailedCountAsync(TUser user)
		{
			ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.AccessFailedCount = 0;
			return Task.FromResult<int>(0);
		}

		public async Task SetEmailAsync(TUser user, string email)
		{
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
			//Only query by email rowkey to pickup old partitionkeys from 1.2.9.2 and lower
			var indexes = (from index in _indexTable.CreateQuery<IdentityUserIndex>()
						   where index.RowKey.Equals(KeyHelper.GenerateRowKeyUserEmail(plainEmail))
						   select index).ToList();
			foreach (IdentityUserIndex de in indexes)
			{
				if (de.Id == userId)
				{
					await _indexTable.ExecuteAsync(TableOperation.Delete(de));
				}
			}
		}

		public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.EmailConfirmed = confirmed;
			return Task.FromResult<int>(0);
		}

		public Task SetLockoutEnabledAsync(TUser user, bool enabled)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.LockoutEnabled = enabled;
			return Task.FromResult<int>(0);
		}

		public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.LockoutEndDateUtc = (lockoutEnd == DateTimeOffset.MinValue) ? null : new DateTime?(lockoutEnd.UtcDateTime);
			return Task.FromResult<int>(0);
		}

		public Task SetPasswordHashAsync(TUser user, string passwordHash)
		{
			this.ThrowIfDisposed();

			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.PasswordHash = passwordHash;
			return Task.FromResult<int>(0);
		}

		public Task SetPhoneNumberAsync(TUser user, string phoneNumber)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.PhoneNumber = phoneNumber;
			return Task.FromResult<int>(0);
		}

		public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.PhoneNumberConfirmed = confirmed;
			return Task.FromResult<int>(0);
		}

		public Task SetSecurityStampAsync(TUser user, string stamp)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.SecurityStamp = stamp;
			return Task.FromResult<int>(0);
		}

		public Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
		{
			this.ThrowIfDisposed();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			user.TwoFactorEnabled = enabled;
			return Task.FromResult<int>(0);
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
					IGenerateKeys ikey = (IGenerateKeys)user;
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

				IdentityUserIndex indexEmail = CreateEmailIndex(userNameKey, user.Email);

				taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexEmail)));
			}

			// Update the external logins
			foreach (var login in user.Logins)
			{
				IdentityUserIndex indexLogin = CreateLoginIndex(userNameKey, login);
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


		public async virtual Task UpdateAsync(TUser user)
		{
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
					IdentityUserIndex indexEmail = CreateEmailIndex(user.Id.ToString(), user.Email);

					tasks.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexEmail)));
				}
			}

			await Task.WhenAll(tasks.ToArray());
		}

		public IdentityCloudContext Context { get; private set; }


		public virtual IQueryable<TUser> Users
		{
			get
			{
				ThrowIfDisposed();
				return _userTable.CreateQuery<TUser>();
			}
		}

		/// <summary>
		/// Creates an email index suitable for a crud operation
		/// </summary>
		/// <param name="userid">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
		/// <param name="email">Plain email address.</param>
		/// <returns></returns>
		private IdentityUserIndex CreateEmailIndex(string userid, string email)
		{
			return new IdentityUserIndex()
			{
				Id = userid,
				PartitionKey = userid,
				RowKey = KeyHelper.GenerateRowKeyUserEmail(email),
				KeyVersion = KeyHelper.KeyVersion,
				ETag = Constants.ETagWildcard
			};
		}

		private IdentityUserIndex CreateLoginIndex(string userid, TUserLogin login)
		{
			return new IdentityUserIndex()
			{
				Id = userid,
				PartitionKey = KeyHelper.GeneratePartitionKeyIndexByLogin(login.LoginProvider),
				RowKey = login.RowKey,
				KeyVersion = KeyHelper.KeyVersion,
				ETag = Constants.ETagWildcard
			};

		}
	}
}
#endif