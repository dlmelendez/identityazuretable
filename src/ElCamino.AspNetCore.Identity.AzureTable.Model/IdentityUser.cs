// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.


using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;


namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <inheritdoc/>
    public class IdentityUser : IdentityUser<string>, IGenerateKeys
    {
        /// <inheritdoc/>
        public IdentityUser() : base() { }

        /// <inheritdoc/>
        public IdentityUser(string userName)
            : this()
        {
            UserName = userName;
        }

        /// <summary>
        /// Generates Row, Partition and Id keys.
        /// All are the same in this case
        /// </summary>
        public void GenerateKeys(IKeyHelper keyHelper)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                Id = keyHelper.GenerateUserId().ToString();
            }
            RowKey = PeekRowKey(keyHelper);
            PartitionKey = RowKey;
            KeyVersion = keyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// In this case, just returns a key based on username
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey(IKeyHelper keyHelper)
        {
            return keyHelper.GenerateRowKeyUserId(Id);
        }

        /// <inheritdoc/>
        public double KeyVersion { get; set; }


        /// <inheritdoc/>
        public override string? UserName
        {
            get => base.UserName;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    base.UserName = value!.Trim();
                }
            }
        }
    }

    /// <inheritdoc/>
    public class IdentityUser<TKey> : Microsoft.AspNetCore.Identity.IdentityUser<TKey>, ITableEntity
        where TKey : IEquatable<TKey>
    {
        /// <inheritdoc/>
        public IdentityUser()
        {
        }

        /// <summary>
        /// Stores the LockoutEnd
        /// </summary>
        public virtual DateTime? LockoutEndDateUtc { get; set; }

        /// <summary>
        /// LockoutEnd is stored as LockoutEndDateUtc for backwards compat.
        /// </summary>
        [IgnoreDataMember]
        public override DateTimeOffset? LockoutEnd
        {
            get
            {
                if (LockoutEndDateUtc.HasValue)
                {
                    return new DateTimeOffset?(new DateTimeOffset(LockoutEndDateUtc.Value));
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    LockoutEndDateUtc = value.Value.UtcDateTime;
                }
                else
                {
                    LockoutEndDateUtc = null;
                }
            }
        }

        /// <inheritdoc/>
        public string PartitionKey { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string RowKey { get; set; } = string.Empty;

        /// <inheritdoc/>
        public DateTimeOffset? Timestamp { get; set; }

        /// <inheritdoc/>
        public ETag ETag { get; set; } = ETag.All;

    }


}
