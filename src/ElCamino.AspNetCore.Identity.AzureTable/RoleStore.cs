// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if net45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Queryable;
using Microsoft.WindowsAzure.Storage;
using System.Data.Services.Client;
using System.Data.Services.Common;
using System.Net;
using System.Diagnostics;
using ElCamino.AspNet.Identity.AzureTable.Model;
using ElCamino.AspNet.Identity.AzureTable.Helpers;

namespace ElCamino.AspNet.Identity.AzureTable
{
	public class RoleStore<TRole> : RoleStore<TRole, string, IdentityUserRole>, IQueryableRoleStore<TRole>, IQueryableRoleStore<TRole, string>, IRoleStore<TRole, string> where TRole : IdentityRole, new()
	{
		public RoleStore()
			: this(new IdentityCloudContext())
		{

		}

		public RoleStore(IdentityCloudContext context)
			: base(context)
		{ }

		//Fixing code analysis issue CA1063
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}

	public class RoleStore<TRole, TKey, TUserRole> : IQueryableRoleStore<TRole, TKey>, IRoleStore<TRole, TKey>, IDisposable
		where TRole : IdentityRole<TKey, TUserRole>, new()
		where TUserRole : IdentityUserRole<TKey>, new()
	{
		private bool _disposed;
		private CloudTable _roleTable;

		public RoleStore(IdentityCloudContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			this.Context = context;
			_roleTable = context.RoleTable;
		}

		public async Task<bool> CreateTableIfNotExistsAsync()
		{
			return await Context.RoleTable.CreateIfNotExistsAsync();
		}

		public async virtual Task CreateAsync(TRole role)
		{
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}

			((IGenerateKeys)role).GenerateKeys();

			// Create the TableOperation that inserts the role entity.
			TableOperation insertOperation = TableOperation.Insert(role);

			// Execute the insert operation.
			await _roleTable.ExecuteAsync(insertOperation);
		}

		public async virtual Task DeleteAsync(TRole role)
		{
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}
			// Create the TableOperation that deletes the role entity.
			TableOperation deleteOperation = TableOperation.Delete(role);

			// Execute the insert operation.
			await _roleTable.ExecuteAsync(deleteOperation);
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
				this._roleTable = null;
				this.Context = null;
				this._disposed = true;
			}
		}

		public async Task<TRole> FindByIdAsync(TKey roleId)
		{
			this.ThrowIfDisposed();
			TableOperation getOperation = TableOperation.Retrieve<TRole>(
				KeyHelper.ParsePartitionKeyIdentityRoleFromRowKey(roleId.ToString()),
				roleId.ToString());

			TableResult tresult = await _roleTable.ExecuteAsync(getOperation);
			return tresult.Result == null ? null : (TRole)tresult.Result;

		}

		public async Task<TRole> FindByNameAsync(string roleName)
		{
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

		public async virtual Task UpdateAsync(TRole role)
		{
			ThrowIfDisposed();
			if (role == null)
			{
				throw new ArgumentNullException("role");
			}

			IGenerateKeys g = role as IGenerateKeys;
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
			}

		}

		public IdentityCloudContext Context { get; private set; }

		public IQueryable<TRole> Roles
		{
			get
			{
				return _roleTable.CreateQuery<TRole>();
			}
		}

	}
}
#endif