// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Xunit;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class BaseUserStoreTests<TUser, TContext, TUserStore, TKeyHelper>
    {
        private static byte[] GenCredentialId()
        {
            // WebAuthn credential ids are at least 16 bytes
            return RandomNumberGenerator.GetBytes(32);
        }

        private static byte[] GenPublicKey()
        {
            return RandomNumberGenerator.GetBytes(64);
        }

        private static UserPasskeyInfo GenTestPasskey(byte[] credentialId, uint signCount = 0, string[] transports = null)
        {
            return new UserPasskeyInfo(
                credentialId: credentialId,
                publicKey: GenPublicKey(),
                createdAt: DateTimeOffset.UtcNow,
                signCount: signCount,
                transports: transports,
                isUserVerified: true,
                isBackupEligible: true,
                isBackedUp: true,
                attestationObject: RandomNumberGenerator.GetBytes(96),
                clientDataJson: RandomNumberGenerator.GetBytes(128))
            {
                Name = "Test passkey",
#if NET11_0_OR_GREATER
                Aaguid = RandomNumberGenerator.GetBytes(16),
#endif
            };
        }

        private static void AssertPasskeyEqual(UserPasskeyInfo expected, UserPasskeyInfo actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.CredentialId, actual!.CredentialId);
            Assert.Equal(expected.PublicKey, actual.PublicKey);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.CreatedAt, actual.CreatedAt);
            Assert.Equal(expected.SignCount, actual.SignCount);
            Assert.Equal(expected.Transports, actual.Transports);
            Assert.Equal(expected.IsUserVerified, actual.IsUserVerified);
            Assert.Equal(expected.IsBackupEligible, actual.IsBackupEligible);
            Assert.Equal(expected.IsBackedUp, actual.IsBackedUp);
            Assert.Equal(expected.AttestationObject, actual.AttestationObject);
            Assert.Equal(expected.ClientDataJson, actual.ClientDataJson);
#if NET11_0_OR_GREATER
            Assert.Equal(expected.Aaguid, actual.Aaguid);
#endif
        }

        /// <summary>
        /// AddOrUpdate -> Get -> Find -> Remove round-trip: all UserPasskeyInfo fields survive losslessly.
        /// </summary>
        public virtual async Task AddGetFindRemoveUserPasskey()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);
            WriteLineObject<IdentityUser>(user);

            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId, signCount: 5, transports: ["internal", "hybrid"]);

            Assert.True(manager.SupportsUserPasskey, "Store does not support passkeys");

            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            // GetPasskeys
            var passkeys = await manager.GetPasskeysAsync(user).ConfigureAwait(false);
            Assert.NotNull(passkeys);
            var stored = Assert.Single(passkeys);
            AssertPasskeyEqual(passkey, stored);

            // GetPasskeyAsync by credential id
            var found = await manager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false);
            AssertPasskeyEqual(passkey, found);

            // Remove and verify
            var removeResult = await manager.RemovePasskeyAsync(user, credentialId).ConfigureAwait(false);
            Assert.True(removeResult.Succeeded, string.Concat(removeResult.Errors));

            var passkeysAfterRemove = await manager.GetPasskeysAsync(user).ConfigureAwait(false);
            Assert.Empty(passkeysAfterRemove);

            var foundAfterRemove = await manager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false);
            Assert.Null(foundAfterRemove);
        }

        /// <summary>
        /// AddOrUpdate with the same credential id updates the stored passkey.
        /// </summary>
        public virtual async Task UpdateUserPasskey()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);

            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId, signCount: 0);
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            // Update sign count and name on the same credential id
            passkey.SignCount = 42;
            passkey.Name = "Updated passkey";
            var updateResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(updateResult.Succeeded, string.Concat(updateResult.Errors));

            var passkeys = await manager.GetPasskeysAsync(user).ConfigureAwait(false);
            var stored = Assert.Single(passkeys);
            Assert.Equal(42u, stored.SignCount);
            Assert.Equal("Updated passkey", stored.Name);

            // Remove via manager
            var removeResult = await manager.RemovePasskeyAsync(user, credentialId).ConfigureAwait(false);
            Assert.True(removeResult.Succeeded, string.Concat(removeResult.Errors));
            var passkeysAfterRemove = await manager.GetPasskeysAsync(user).ConfigureAwait(false);
            Assert.Empty(passkeysAfterRemove);
        }

        /// <summary>
        /// FindByPasskeyIdAsync index lookup: user found from credential id alone (passwordless flow).
        /// </summary>
        public virtual async Task FindUserByPasskeyId()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);

            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId);
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            // Passwordless lookup by credential id alone
            var foundUser = await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
            Assert.NotNull(foundUser);
            Assert.Equal(user.Id, foundUser!.Id);

            // Negative: unknown credential id
            var notFound = await manager.FindByPasskeyIdAsync(GenCredentialId()).ConfigureAwait(false);
            Assert.Null(notFound);

            // Remove and verify the index row is gone
            var removeResult = await manager.RemovePasskeyAsync(user, credentialId).ConfigureAwait(false);
            Assert.True(removeResult.Succeeded, string.Concat(removeResult.Errors));
            var foundAfterRemove = await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
            Assert.Null(foundAfterRemove);
        }

        /// <summary>
        /// DeleteAsync cleans up passkey index rows: lookup by credential id returns null after user delete.
        /// </summary>
        public virtual async Task DeleteUserPasskeyIndexCleanup()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);

            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId);
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            // Verify the index lookup works before delete
            var foundUser = await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
            Assert.NotNull(foundUser);

            var userDeletionResult = await manager.DeleteAsync(user).ConfigureAwait(false);
            Assert.True(userDeletionResult.Succeeded, string.Concat(userDeletionResult.Errors));

            // Index row must be gone
            var foundAfterDelete = await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
            Assert.Null(foundAfterDelete);
        }

        /// <summary>
        /// Negative cases: null arguments throw ArgumentNullException for all passkey store methods.
        /// </summary>
        public virtual async Task UserPasskeyArgumentNullChecks()
        {
            using var store = userFixture.CreateUserStore();
            var user = GenTestUser();
            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId);
            var cancellationToken = new CancellationToken();

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddOrUpdatePasskeyAsync(null, passkey, cancellationToken)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddOrUpdatePasskeyAsync(user, null, cancellationToken)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPasskeysAsync(null, cancellationToken)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindPasskeyAsync(null, credentialId, cancellationToken)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindPasskeyAsync(user, null, cancellationToken)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByPasskeyIdAsync(null, cancellationToken)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemovePasskeyAsync(null, credentialId, cancellationToken)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemovePasskeyAsync(user, null, cancellationToken)).ConfigureAwait(false);
        }
    }
}