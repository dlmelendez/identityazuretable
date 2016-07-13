// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if !net45
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.WindowsAzure.Storage.Table;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using System.Threading;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public class RoleStore<TRole> : RoleStore<TRole, IdentityCloudContext>
    where TRole : Model.IdentityRole, new()
    {
        public RoleStore()
            : this(new IdentityCloudContext())
        {
        }

        public RoleStore(IdentityCloudContext context)
            : base(context)
        {
        }
    }

    public class RoleStore<TRole, TContext> : RoleStore<TRole, string, Model.IdentityUserRole, TContext> 
		//, IQueryableRoleStore<TRole>
		, IRoleStore<TRole>
		//TODO: New implementation, IRoleClaimStore<TRole>
		where TRole : Model.IdentityRole, new()
		where TContext : IdentityCloudContext, new()
	{
		
		public RoleStore(TContext context)
			: base(context)
		{ }

		//Fixing code analysis issue CA1063
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}

	public class RoleStore<TRole, TKey, TUserRole, TContext> : 
		//IQueryableRoleStore<TRole>, 
		IRoleStore<TRole>, IDisposable
		where TRole : Model.IdentityRole<TKey, TUserRole>, new()
		where TUserRole : Model.IdentityUserRole<TKey>, new()
		where TContext : IdentityCloudContext, new()
	{
		private bool _disposed;
		private CloudTable _roleTable;
        private IdentityErrorDescriber _errorDescriber = new IdentityErrorDescriber();

		public RoleStore(TContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			Context = context;
			_roleTable = context.RoleTable;
		}

		public async Task<bool> CreateTableIfNotExistsAsync()
		{
			return await Context.RoleTable.CreateIfNotExistsAsync();
		}

		public async virtual Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}

			((Model.IGenerateKeys)role).GenerateKeys();

			// Create the TableOperation that inserts the role entity.
			TableOperation insertOperation = TableOperation.Insert(role);

			// Execute the insert operation.
			await _roleTable.ExecuteAsync(insertOperation);
			return IdentityResult.Success;
		}

		public async virtual Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}
			// Create the TableOperation that deletes the role entity.
			TableOperation deleteOperation = TableOperation.Delete(role);

			// Execute the insert operation.
			await _roleTable.ExecuteAsync(deleteOperation);
			return IdentityResult.Success;
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
				this._roleTable = null;
				this.Context = null;
				this._disposed = true;
			}
		}

		public async Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();
			TableOperation getOperation = TableOperation.Retrieve<TRole>(
				KeyHelper.ParsePartitionKeyIdentityRoleFromRowKey(roleId.ToString()),
				roleId.ToString());

			TableResult tresult = await _roleTable.ExecuteAsync(getOperation);
			return tresult.Result == null ? null : (TRole)tresult.Result;

		}

		public async Task<TRole> FindByNameAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.ThrowIfDisposed();

			TableOperation getOperation = TableOperation.Retrieve<TRole>(
				KeyHelper.GeneratePartitionKeyIdentityRole(roleName),
				KeyHelper.GenerateRowKeyIdentityRole(roleName));
            
			TableResult tresult = await _roleTable.ExecuteAsync(getOperation);
			return tresult.Result == null ? null : (TRole)tresult.Result;
		}


		private void ThrowIfDisposed()
		{
			if (this._disposed)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
		}

		public async virtual Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}

			Model.IGenerateKeys g = role as Model.IGenerateKeys;
			if (!g.PeekRowKey().Equals(role.RowKey, StringComparison.Ordinal))
			{
				TableBatchOperation batch = new TableBatchOperation();
				DynamicTableEntity dRole = new DynamicTableEntity(role.PartitionKey, role.RowKey);
				dRole.ETag = Constants.ETagWildcard;
				dRole.Timestamp = role.Timestamp;
				g.GenerateKeys();
				//PartitionKey has to be the same to participate in a batch transaction.
				if (dRole.PartitionKey.Equals(role.PartitionKey))
				{
					batch.Add(TableOperation.Delete(dRole));
					batch.Add(TableOperation.Insert(role));
					await _roleTable.ExecuteBatchAsync(batch);
				}
				else
				{
					await Task.WhenAll(
					_roleTable.ExecuteAsync(TableOperation.Delete(dRole)),
					_roleTable.ExecuteAsync(TableOperation.Insert(role)));
				}

				return IdentityResult.Success;
			}

			return IdentityResult.Failed(_errorDescriber.InvalidRoleName(role.Name));
		}

		public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}
			return Task.FromResult(role.Id.ToString());
		}


		public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}
			return Task.FromResult(role.Name);
		}

		public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}
			role.Name = roleName;
			return Task.FromResult(0);
		}


		public virtual Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException(nameof(role));
			}
			return Task.FromResult(role.NormalizedName);
		}

		public virtual Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException(nameof(role));
			}
			role.NormalizedName = normalizedName;
			return Task.FromResult(0);
		}


		public TContext Context { get; private set; }

		//public IQueryable<TRole> Roles
		//{
		//	get
		//	{
		//		return _roleTable.CreateQuery<TRole>();
		//	}
		//}

	}
}
#endif