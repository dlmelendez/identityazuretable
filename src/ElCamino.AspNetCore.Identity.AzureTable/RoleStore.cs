// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Azure.Data.Tables;
using System.Net;
using Azure;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public class RoleStore<TRole> : RoleStore<TRole, IdentityCloudContext>
    where TRole : Model.IdentityRole, new()
    {
        public RoleStore()
            : this(new IdentityCloudContext(), new DefaultKeyHelper())
        {
        }

        public RoleStore(IdentityCloudContext context, IKeyHelper keyHelper)
            : base(context, keyHelper)
        {
        }
    }

    public class RoleStore<TRole, TContext> : RoleStore<TRole, string, Model.IdentityUserRole, Model.IdentityRoleClaim, TContext>
        where TRole : Model.IdentityRole, new()
        where TContext : IdentityCloudContext, new()
    {
        public RoleStore(TContext context, IKeyHelper keyHelper) : base(context, keyHelper) { }

        //Fixing code analysis issue CA1063
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public class RoleStore<TRole, TKey, TUserRole, TRoleClaim, TContext> :
        RoleStoreBase<TRole, TKey, TUserRole, TRoleClaim>
        where TRole : Model.IdentityRole<TKey, TUserRole>, new()
        where TUserRole : Model.IdentityUserRole<TKey>, new()
        where TRoleClaim : Model.IdentityRoleClaim<TKey>, new()
        where TContext : IdentityCloudContext, new()
        where TKey : IEquatable<TKey>
    {
        private bool _disposed;
        private TableClient _roleTable;
        private IdentityErrorDescriber _errorDescriber = new IdentityErrorDescriber();
        protected IKeyHelper _keyHelper;
        private readonly string FilterString;

        public RoleStore(TContext context, IKeyHelper keyHelper) : base(new IdentityErrorDescriber())
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _roleTable = context.RoleTable;
            _keyHelper = keyHelper;

            FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityRole),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityRoleUpperBound));

        }

        public Task CreateTableIfNotExistsAsync() => Context.RoleTable.CreateIfNotExistsAsync();

        public override async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null) throw new ArgumentNullException(nameof(role));

            ((Model.IGenerateKeys)role).GenerateKeys(_keyHelper);

            // Execute the insert operation.
            await _roleTable.AddEntityAsync(role).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null) throw new ArgumentNullException(nameof(role));

            // Execute the insert operation.
            await _roleTable.DeleteEntityAsync(role.PartitionKey, role.RowKey, ETag.All).ConfigureAwait(false);
            return IdentityResult.Success;
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
                this._roleTable = null;
                this.Context = null;
                this._disposed = true;
            }
        }

        public override async Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();

            try
            {
                return await _roleTable.GetEntityAsync<TRole>(_keyHelper.ParsePartitionKeyIdentityRoleFromRowKey(roleId),
                    roleId.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException rfe)
            when (rfe.Status == (int)HttpStatusCode.NotFound)
            {
                return default(TRole);
            }
        }

        public override async Task<TRole> FindByNameAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();
           
            try
            {
                return await _roleTable.GetEntityAsync<TRole>(_keyHelper.GeneratePartitionKeyIdentityRole(roleName),
                _keyHelper.GenerateRowKeyIdentityRole(roleName), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException rfe)
            when (rfe.Status == (int)HttpStatusCode.NotFound)
            {
                return default(TRole);
            }
        }
       

        public override async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            Model.IGenerateKeys g = role as Model.IGenerateKeys;
            if (!g.PeekRowKey(_keyHelper).Equals(role.RowKey, StringComparison.Ordinal))
            {
                var batch = _roleTable.CreateTransactionalBatch(role.PartitionKey);
                TableEntity dRole = new TableEntity(role.PartitionKey, role.RowKey);
                dRole.ETag = Constants.ETagWildcard;
                dRole.Timestamp = role.Timestamp;
                g.GenerateKeys(_keyHelper);
                //PartitionKey has to be the same to participate in a batch transaction.
                if (dRole.PartitionKey.Equals(role.PartitionKey))
                {
                    batch.DeleteEntity(dRole.PartitionKey, dRole.RowKey, ETag.All);
                    batch.AddEntity(role);
                    await batch.SubmitBatchAsync(cancellationToken:cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.WhenAll(
                    _roleTable.DeleteEntityAsync(dRole.PartitionKey, dRole.RowKey, ifMatch:ETag.All, cancellationToken:cancellationToken),
                    _roleTable.AddEntityAsync(role, cancellationToken)).ConfigureAwait(false);
                }

                return IdentityResult.Success;
            }

            return IdentityResult.Failed(_errorDescriber.InvalidRoleName(role.Name));
        }      


        public override async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            string partitionFilter = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, role.Id.ToString());

            string rowFilter1 = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserToken);
            string rowFilter2 = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserId);
            string rowFilter = TableQuery.CombineFilters(rowFilter1, TableOperators.Or, rowFilter2);

            string filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            return 

                (await _roleTable.QueryAsync<TRoleClaim>(filter, cancellationToken:cancellationToken ).ToListAsync().ConfigureAwait(false))                                
                .Select(w => new Claim(w.ClaimType, w.ClaimValue))
                .ToList() as IList<Claim>;
        }

        public override async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            TRoleClaim item = Activator.CreateInstance<TRoleClaim>();
            item.RoleId = role.Id;
            item.ClaimType = claim.Type;
            item.ClaimValue = claim.Value;
            ((Model.IGenerateKeys)item).GenerateKeys(_keyHelper);

            await _roleTable.AddEntityAsync(item).ConfigureAwait(false);
        }

        public override async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            if (string.IsNullOrWhiteSpace(claim.Type))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(claim.Type));
            }

            TRoleClaim item = Activator.CreateInstance<TRoleClaim>();
            item.RoleId = role.Id;
            item.ClaimType = claim.Type;
            item.ClaimValue = claim.Value;
            item.ETag = Constants.ETagWildcard;
            ((Model.IGenerateKeys)item).GenerateKeys(_keyHelper);

            await _roleTable.DeleteEntityAsync(item.PartitionKey, item.RowKey, ETag.All, cancellationToken).ConfigureAwait(false);
        }

        public TContext Context { get; private set; }

        /// <summary>
        /// Queries will be slow unless they include Partition and/or Row keys
        /// </summary>
        public override IQueryable<TRole> Roles
        {
            get
            {
                return _roleTable.Query<TRole>(FilterString).AsQueryable();
            }
        }

    }
}
