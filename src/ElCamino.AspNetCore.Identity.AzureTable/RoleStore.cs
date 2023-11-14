// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    /// <inheritdoc/>
    public class RoleStore<TRole> : RoleStore<TRole, IdentityCloudContext>
    where TRole : Model.IdentityRole, new()
    {

        /// <inheritdoc/>
        public RoleStore(IdentityCloudContext context, IKeyHelper keyHelper)
            : base(context, keyHelper)
        {
        }
    }

    /// <inheritdoc/>
    public class RoleStore<TRole, TContext> : RoleStore<TRole, string, Model.IdentityUserRole, Model.IdentityRoleClaim, TContext>
        where TRole : Model.IdentityRole, new()
        where TContext : IdentityCloudContext
    {
        /// <inheritdoc/>
        public RoleStore(TContext context, IKeyHelper keyHelper) : base(context, keyHelper) { }

        //Fixing code analysis issue CA1063
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    /// <inheritdoc/>
    public class RoleStore<TRole, TKey, TUserRole, TRoleClaim, TContext> :
        RoleStoreBase<TRole, TKey, TUserRole, TRoleClaim>
        where TRole : Model.IdentityRole<TKey, TUserRole>, new()
        where TUserRole : Model.IdentityUserRole<TKey>, new()
        where TRoleClaim : Model.IdentityRoleClaim<TKey>, new()
        where TContext : IdentityCloudContext
        where TKey : IEquatable<TKey>
    {
        private bool _disposed;
        private readonly TableClient _roleTable;
        private readonly TContext _context;
        private readonly IdentityErrorDescriber _errorDescriber = new();
        /// <summary>
        /// Key Helper
        /// </summary>
        protected readonly IKeyHelper _keyHelper;
        private readonly string FilterString;

        /// <inheritdoc/>
        public RoleStore(TContext context, IKeyHelper keyHelper) : base(new IdentityErrorDescriber())
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _roleTable = context.RoleTable;
            _keyHelper = keyHelper;

            FilterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityRole),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityRoleUpperBound));

        }

        /// <summary>
        /// Create table is not exists
        /// </summary>
        /// <returns>Task</returns>
        public Task CreateTableIfNotExistsAsync() => Context.RoleTable.CreateIfNotExistsAsync();

        /// <inheritdoc/>
        public override async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(role);
#else
            if (role is null) throw new ArgumentNullException(nameof(role));
#endif
            ((Model.IGenerateKeys)role).GenerateKeys(_keyHelper);

            // Execute the insert operation.
            _ = await _roleTable.AddEntityAsync(role, cancellationToken).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public override async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(role);
#else
            if (role is null) throw new ArgumentNullException(nameof(role));
#endif
            // Execute the insert operation.
            _ = await _roleTable.DeleteEntityAsync(role.PartitionKey, role.RowKey, TableConstants.ETagWildcard, cancellationToken: cancellationToken).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public new void Dispose()
        {
            base.Dispose();
            Dispose(true);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
            }
        }

#pragma warning disable CS8609 // Nullability of reference types in return type doesn't match overridden member.
        /// <inheritdoc/>
        public override async Task<TRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken = default)
#pragma warning restore CS8609 // Nullability of reference types in return type doesn't match overridden member.
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            return await _roleTable!.GetEntityOrDefaultAsync<TRole>(_keyHelper.ParsePartitionKeyIdentityRoleFromRowKey(roleId),
                roleId.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

#pragma warning disable CS8609 // Nullability of reference types in return type doesn't match overridden member.
        /// <inheritdoc/>
        public override async Task<TRole?> FindByNameAsync(string roleName, CancellationToken cancellationToken = default)
#pragma warning restore CS8609 // Nullability of reference types in return type doesn't match overridden member.
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            return await _roleTable.GetEntityOrDefaultAsync<TRole>(_keyHelper.GeneratePartitionKeyIdentityRole(roleName),
            _keyHelper.GenerateRowKeyIdentityRole(roleName), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(role);
#else
            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }
#endif
            Model.IGenerateKeys? g = role as Model.IGenerateKeys;
            if (g is not null && !g.PeekRowKey(_keyHelper).Equals(role.RowKey, StringComparison.Ordinal))
            {
                BatchOperationHelper bHelper = new BatchOperationHelper(_roleTable);
                TableEntity dRole = new TableEntity(role.PartitionKey, role.RowKey);
                dRole.ETag = TableConstants.ETagWildcard;
                dRole.Timestamp = role.Timestamp;
                g.GenerateKeys(_keyHelper);
                //PartitionKey has to be the same to participate in a batch transaction.
                if (dRole.PartitionKey.Equals(role.PartitionKey))
                {
                    bHelper.DeleteEntity(dRole.PartitionKey, dRole.RowKey, TableConstants.ETagWildcard);
                    bHelper.AddEntity(role);
                    _ = await bHelper.SubmitBatchAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _ = await Task.WhenAll(
                    _roleTable.DeleteEntityAsync(dRole.PartitionKey, dRole.RowKey, ifMatch: TableConstants.ETagWildcard, cancellationToken: cancellationToken),
                    _roleTable.AddEntityAsync(role, cancellationToken)).ConfigureAwait(false);
                }

                return IdentityResult.Success;
            }

            return IdentityResult.Failed(_errorDescriber.InvalidRoleName(role.Name));
        }

        /// <inheritdoc/>
        public override async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(role);
#else
            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }
#endif
            string partitionFilter = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, role.Id.ToString() ?? string.Empty);

            string rowFilter1 = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserToken);
            string rowFilter2 = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserId);
            string rowFilter = TableQuery.CombineFilters(rowFilter1, TableOperators.Or, rowFilter2);

            string filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            return

                (await _roleTable.QueryAsync<TRoleClaim>(filter, cancellationToken: cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false))
                .Select(w => w.ToClaim())
                .ToList() as IList<Claim>;
        }

        /// <inheritdoc/>
        public override async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(role);
            ArgumentNullException.ThrowIfNull(claim);
#else

            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim is null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
#endif
            TRoleClaim item = Activator.CreateInstance<TRoleClaim>();
            item.RoleId = role.Id;
            item.ClaimType = claim.Type;
            item.ClaimValue = claim.Value;
            ((Model.IGenerateKeys)item).GenerateKeys(_keyHelper);

            _ = await _roleTable.AddEntityAsync(item, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(role);
            ArgumentNullException.ThrowIfNull(claim);
#else
            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim is null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
#endif
            if (string.IsNullOrWhiteSpace(claim.Type))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, nameof(claim));
            }

            TRoleClaim item = Activator.CreateInstance<TRoleClaim>();
            item.RoleId = role.Id;
            item.ClaimType = claim.Type;
            item.ClaimValue = claim.Value;
            item.ETag = TableConstants.ETagWildcard;
            ((Model.IGenerateKeys)item).GenerateKeys(_keyHelper);

            _ = await _roleTable.DeleteEntityAsync(item.PartitionKey, item.RowKey, TableConstants.ETagWildcard, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Table Storage context access
        /// </summary>
        public TContext Context => _context;

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
