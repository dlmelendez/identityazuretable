// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Buffers.Text;
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
            Assert.Equal(expected.CredentialId, actual.CredentialId);
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
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);

            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId, signCount: 0);
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            // Update sign count and name on the same credential id. The sign count is stored as a long: use the largest uint.
            passkey.SignCount = uint.MaxValue;
            passkey.Name = "Updated passkey";
            var updateResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(updateResult.Succeeded, string.Concat(updateResult.Errors));

            var passkeys = await manager.GetPasskeysAsync(user).ConfigureAwait(false);
            var stored = Assert.Single(passkeys);
            Assert.Equal(uint.MaxValue, stored.SignCount);
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
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);

            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId);
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            // Passwordless lookup by credential id alone
            var foundUser = await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
            Assert.NotNull(foundUser);
            Assert.Equal(user.Id, foundUser.Id);

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
        /// DeleteAsync cleans up passkey index rows: the index row is deleted, not left pointing at the deleted user.
        /// </summary>
        public virtual async Task DeleteUserPasskeyIndexCleanup()
        {
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

            var foundAfterDelete = await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
            Assert.Null(foundAfterDelete);

            // An orphaned index row also gives null above, so prove the row itself is gone:
            // a new user with the deleted user's id must not be found by the deleted user's credential id
            var recreatedUser = GenTestUser();
            recreatedUser.Id = user.Id;
            var recreateResult = await manager.CreateAsync(recreatedUser).ConfigureAwait(false);
            Assert.True(recreateResult.Succeeded, string.Concat(recreateResult.Errors));
            Assert.Null(await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false));
        }

        /// <summary>
        /// GetPasskeysAsync returns only passkey rows: other rows in the user's partition are not passkeys.
        /// </summary>
        public virtual async Task GetUserPasskeysExcludesOtherUserRows()
        {
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);

            // Login row keys sort directly after passkey row keys
            var loginResult = await manager.AddLoginAsync(user, new UserLoginInfo("Google", Guid.NewGuid().ToString("N"), "Google")).ConfigureAwait(false);
            Assert.True(loginResult.Succeeded, string.Concat(loginResult.Errors));
            var claimResult = await manager.AddClaimAsync(user, GenUserClaim()).ConfigureAwait(false);
            Assert.True(claimResult.Succeeded, string.Concat(claimResult.Errors));
            var tokenResult = await manager.SetAuthenticationTokenAsync(user, "Google", "access_token", Guid.NewGuid().ToString("N")).ConfigureAwait(false);
            Assert.True(tokenResult.Succeeded, string.Concat(tokenResult.Errors));

            var passkey = GenTestPasskey(GenCredentialId());
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            var stored = Assert.Single(await manager.GetPasskeysAsync(user).ConfigureAwait(false));
            AssertPasskeyEqual(passkey, stored);
        }

        /// <summary>
        /// Removing a credential id the user does not own must not touch the owner's passkey or its index row.
        /// </summary>
        public virtual async Task RemoveUserPasskeyRequiresOwnership()
        {
            using var manager = userFixture.CreateUserManager();
            var owner = await CreateTestUserLiteAsync().ConfigureAwait(false);
            var otherUser = await CreateTestUserLiteAsync().ConfigureAwait(false);

            var credentialId = GenCredentialId();
            var passkey = GenTestPasskey(credentialId);
            var addResult = await manager.AddOrUpdatePasskeyAsync(owner, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            var removeResult = await manager.RemovePasskeyAsync(otherUser, credentialId).ConfigureAwait(false);
            Assert.True(removeResult.Succeeded, string.Concat(removeResult.Errors));

            var foundUser = await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
            Assert.Equal(owner.Id, foundUser?.Id);
            AssertPasskeyEqual(passkey, await manager.GetPasskeyAsync(owner, credentialId).ConfigureAwait(false));
        }

        /// <summary>
        /// Credential ids are binary: ids whose Base64Url encodings differ only by letter casing are different credentials.
        /// </summary>
        public virtual async Task UserPasskeyCredentialIdIsCaseSensitive()
        {
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);
            var otherUser = await CreateTestUserLiteAsync().ConfigureAwait(false);

            // 48 bytes encode to 64 chars without a partial block, so every case variant decodes
            var credentialId = RandomNumberGenerator.GetBytes(48);
            var caseVariantId = Base64Url.DecodeFromChars(string.Concat(
                Base64Url.EncodeToString(credentialId).Select(c => char.IsLower(c) ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c))));
            Assert.NotEqual(credentialId, caseVariantId);

            var passkey = GenTestPasskey(credentialId);
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            Assert.Null(await manager.FindByPasskeyIdAsync(caseVariantId).ConfigureAwait(false));
            Assert.Null(await manager.GetPasskeyAsync(user, caseVariantId).ConfigureAwait(false));

            // The case variant belongs to whoever registers it and never takes over the original's index row
            var caseVariantPasskey = GenTestPasskey(caseVariantId);
            var addVariantResult = await manager.AddOrUpdatePasskeyAsync(otherUser, caseVariantPasskey).ConfigureAwait(false);
            Assert.True(addVariantResult.Succeeded, string.Concat(addVariantResult.Errors));
            Assert.Equal(user.Id, (await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false))?.Id);
            Assert.Equal(otherUser.Id, (await manager.FindByPasskeyIdAsync(caseVariantId).ConfigureAwait(false))?.Id);

            var removeVariantResult = await manager.RemovePasskeyAsync(otherUser, caseVariantId).ConfigureAwait(false);
            Assert.True(removeVariantResult.Succeeded, string.Concat(removeVariantResult.Errors));
            Assert.Equal(user.Id, (await manager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false))?.Id);
            AssertPasskeyEqual(passkey, await manager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false));
        }

        /// <summary>
        /// Passkey index rows share the index table with external login index rows but never resolve as a login.
        /// </summary>
        public virtual async Task UserPasskeyIndexIsNotALoginIndex()
        {
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);

            var credentialId = GenCredentialId();
            var addResult = await manager.AddOrUpdatePasskeyAsync(user, GenTestPasskey(credentialId)).ConfigureAwait(false);
            Assert.True(addResult.Succeeded, string.Concat(addResult.Errors));

            // Guards against keying the passkey index with the login key generation under a passkey provider name
            string encodedCredentialId = Base64Url.EncodeToString(credentialId);
            Assert.Null(await manager.FindByLoginAsync("Passkey", encodedCredentialId).ConfigureAwait(false));
            Assert.Null(await manager.FindByLoginAsync(nameof(Model.IdentityUserPasskey), encodedCredentialId).ConfigureAwait(false));
        }

        /// <summary>
        /// The keys hold a hash of the credential id, so the largest credential id WebAuthn allows (1023 bytes) round-trips.
        /// Credential ids are caller supplied at sign in: one of any size is looked up, and not found, without error.
        /// </summary>
        public virtual async Task UserPasskeyLargeCredentialId()
        {
            using var store = userFixture.CreateUserStore();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);
            var cancellationToken = new CancellationToken();

            var largestId = RandomNumberGenerator.GetBytes(1023);
            var passkey = GenTestPasskey(largestId);
            await store.AddOrUpdatePasskeyAsync(user, passkey, cancellationToken).ConfigureAwait(false);
            AssertPasskeyEqual(passkey, await store.FindPasskeyAsync(user, largestId, cancellationToken).ConfigureAwait(false));
            Assert.Equal(user.Id, (await store.FindByPasskeyIdAsync(largestId, cancellationToken).ConfigureAwait(false))?.Id);
            await store.RemovePasskeyAsync(user, largestId, cancellationToken).ConfigureAwait(false);
            Assert.Null(await store.FindPasskeyAsync(user, largestId, cancellationToken).ConfigureAwait(false));

            var oversizedId = new byte[4 * 1024 * 1024];
            Assert.Null(await store.FindPasskeyAsync(user, oversizedId, cancellationToken).ConfigureAwait(false));
            Assert.Null(await store.FindByPasskeyIdAsync(oversizedId, cancellationToken).ConfigureAwait(false));
            await store.RemovePasskeyAsync(user, oversizedId, cancellationToken).ConfigureAwait(false);
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
