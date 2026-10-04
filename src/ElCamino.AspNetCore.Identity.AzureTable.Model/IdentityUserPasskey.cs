// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Buffers.Text;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <summary>
    /// Entity that represents a user passkey credential stored as typed table columns.
    /// Holds the fields of <see cref="UserPasskeyInfo"/> as individual typed properties
    /// for lossless storage and provides two-way conversion to/from <see cref="UserPasskeyInfo"/>.
    /// </summary>
    public class IdentityUserPasskey : IGenerateKeys, ITableEntity
    {
        /// <summary>
        /// Separator used to pack <see cref="UserPasskeyInfo.Transports"/> into the <see cref="Transports"/> string column.
        /// </summary>
        public const string TransportsSeparator = ";";

        /// <summary>
        /// The largest credential id, in bytes, that can be stored. WebAuthn allows up to 1023 bytes, but the credential id
        /// is Base64Url encoded into the row key. 382 bytes keeps the row key within 512 characters, the key size the
        /// Azurite emulator accepts. Azure Table Storage documents a limit of 1024 characters.
        /// </summary>
        public const int MaxCredentialIdLength = 382;

        /// <summary>
        /// The user id (hashed) that owns this passkey. Same value as the user row's partition key.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The credential id for this passkey.
        /// </summary>
        public byte[] CredentialId { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The public key associated with this passkey.
        /// </summary>
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The friendly name for this passkey.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The time this passkey was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// The signature counter for this passkey. Stored as <see cref="long"/>; the
        /// <see cref="UserPasskeyInfo.SignCount"/> is a <see cref="uint"/> and is converted on mapping.
        /// </summary>
        public long SignCount { get; set; }

        /// <summary>
        /// The transports supported by this passkey, joined with <see cref="TransportsSeparator"/>.
        /// Stored as a string because Azure Table Storage has no collection column type.
        /// </summary>
        public string? Transports { get; set; }

        /// <summary>
        /// Whether the passkey has a verified user.
        /// </summary>
        public bool IsUserVerified { get; set; }

        /// <summary>
        /// Whether the passkey is eligible for backup.
        /// </summary>
        public bool IsBackupEligible { get; set; }

        /// <summary>
        /// Whether the passkey is currently backed up.
        /// </summary>
        public bool IsBackedUp { get; set; }

        /// <summary>
        /// The attestation object associated with this passkey.
        /// </summary>
        public byte[] AttestationObject { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The collected client data JSON associated with this passkey.
        /// </summary>
        public byte[] ClientDataJson { get; set; } = Array.Empty<byte>();

#if NET11_0_OR_GREATER
        /// <summary>
        /// The AAGUID of the authenticator that created this passkey.
        /// Available when targeting .NET 11+ (Identity 11).
        /// </summary>
        public byte[]? Aaguid { get; set; }
#endif

        /// <inheritdoc/>
        public string PartitionKey { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string RowKey { get; set; } = string.Empty;

        /// <inheritdoc/>
        public DateTimeOffset? Timestamp { get; set; }

        /// <inheritdoc/>
        public ETag ETag { get; set; } = ETag.All;

        /// <inheritdoc/>
        public double KeyVersion { get; set; }

        /// <summary>
        /// Generates Row and Partition keys.
        /// RowKey is the passkey prefix + Base64Url(CredentialId). PartitionKey is the hashed user id.
        /// </summary>
        /// <param name="keyHelper"></param>
        public void GenerateKeys(IKeyHelper keyHelper)
        {
            RowKey = PeekRowKey(keyHelper);
            PartitionKey = UserId;
            KeyVersion = keyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <param name="keyHelper"></param>
        /// <returns></returns>
        public string PeekRowKey(IKeyHelper keyHelper)
        {
            return GenerateRowKey(keyHelper, CredentialId);
        }

        /// <summary>
        /// Generates the RowKey for a credential id: the passkey prefix + Base64Url(credentialId).
        /// The other keys are hashed from upper cased text. A credential id is a case sensitive binary value,
        /// so it is encoded instead to keep the key exact.
        /// </summary>
        /// <param name="keyHelper"></param>
        /// <param name="credentialId">The passkey credential id.</param>
        /// <returns></returns>
        public static string GenerateRowKey(IKeyHelper keyHelper, ReadOnlySpan<byte> credentialId)
        {
            return string.Format(keyHelper.FormatterIdentityUserPasskey, Base64Url.EncodeToString(credentialId));
        }

        /// <summary>
        /// Creates a new <see cref="IdentityUserPasskey"/> from a <see cref="UserPasskeyInfo"/>
        /// for the specified hashed user id.
        /// </summary>
        /// <param name="passkey">The passkey info to convert.</param>
        /// <param name="hashedUserId">The hashed user id used as the partition key.</param>
        /// <returns>A new <see cref="IdentityUserPasskey"/> with all fields mapped.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The credential id is longer than <see cref="MaxCredentialIdLength"/>.</exception>
        public static IdentityUserPasskey FromPasskeyInfo(UserPasskeyInfo passkey, string hashedUserId)
        {
            ArgumentNullException.ThrowIfNull(passkey);
            ArgumentException.ThrowIfNullOrEmpty(hashedUserId);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(passkey.CredentialId.Length, MaxCredentialIdLength);

            return new IdentityUserPasskey
            {
                UserId = hashedUserId,
                CredentialId = passkey.CredentialId,
                PublicKey = passkey.PublicKey,
                Name = passkey.Name,
                CreatedAt = passkey.CreatedAt,
                SignCount = passkey.SignCount,
                Transports = passkey.Transports is { Length: > 0 } transports ? string.Join(TransportsSeparator, transports) : null,
                IsUserVerified = passkey.IsUserVerified,
                IsBackupEligible = passkey.IsBackupEligible,
                IsBackedUp = passkey.IsBackedUp,
                AttestationObject = passkey.AttestationObject,
                ClientDataJson = passkey.ClientDataJson,
#if NET11_0_OR_GREATER
                Aaguid = passkey.Aaguid,
#endif
            };
        }

        /// <summary>
        /// Converts this entity to a <see cref="UserPasskeyInfo"/> with all fields mapped losslessly.
        /// </summary>
        /// <returns>A new <see cref="UserPasskeyInfo"/> populated from the stored columns.</returns>
        public UserPasskeyInfo ToPasskeyInfo()
        {
            return new UserPasskeyInfo(
                credentialId: CredentialId,
                publicKey: PublicKey,
                createdAt: CreatedAt,
                signCount: checked((uint)SignCount),
                transports: string.IsNullOrEmpty(Transports) ? null : Transports.Split(TransportsSeparator),
                isUserVerified: IsUserVerified,
                isBackupEligible: IsBackupEligible,
                isBackedUp: IsBackedUp,
                attestationObject: AttestationObject,
                clientDataJson: ClientDataJson)
            {
                Name = Name,
#if NET11_0_OR_GREATER
                Aaguid = Aaguid,
#endif
            };
        }
    }
}
