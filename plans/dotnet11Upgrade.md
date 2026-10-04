# .NET 11 Upgrade Plan — identityazuretable v11.0

**Branch:** `rel/11.0`
**Ship version:** 11.0.0
**Target frameworks:** `net10.0;net11.0` (dropping `net8.0`, `net9.0`)
**Language:** C# 15 (`LangVersion 15.0`)
**Identity packages:** `11.0-*` floating prerelease (RC build now; switch to `11.0.*` at GA ~Nov 2026)

---

## Locked Decisions

| # | Decision | Detail |
|---|----------|--------|
| 1 | TFMs | `net10.0;net11.0` only (net8 EOL Nov 2026, net9 already EOL) |
| 2 | Passkeys | In scope — implement `IUserPasskeyStore<TUser>` (the store-level gap vs. Identity 10/11) |
| 3 | Passkey storage | **No JSON serialization.** New `Model.IdentityUserPasskey` holds `UserPasskeyInfo` fields as **typed columns** with lossless two-way conversion (`FromPasskeyInfo` / `ToPasskeyInfo`). Note: `UserPasskeyInfo` is `sealed`, so — like Microsoft's own `IdentityUserPasskey<TKey>` — the new class is a standalone POCO with converters, mirroring how `Model.IdentityUser` wraps `IdentityUser` for lossless storage. |
| 4 | `IKeyHelper` | **Updated with new passkey row properties** (revised — was "unchanged"): add `PreFixIdentityUserPasskey`, `PreFixIdentityUserPasskeyUpperBound`, `FormatterIdentityUserPasskey` to the interface, implemented in `BaseKeyHelper` (virtual, defaulting to `TableConstants` values) and inherited by `DefaultKeyHelper` / `SHA256KeyHelper`. Passkey **index** keys need no new generation methods either (see #6 — revised in review). `KeyVersion` bump to `11.0` (recommended — see 2.2). **Note:** this is a compile-time breaking change for custom `IKeyHelper` implementers — release-note it. |
| 5 | Passkey row prefix | `K_` (rows) / `L_` (upper bound) — **revised in review, was `O_`**. The upper bound is the *exclusive end of the row-key range scan*, so like every other pair (`C_`/`D_`, `L_`/`M_`, `R_`/`S_`, `U_`/`V_`) it must be the next prefix in sort order; it is allowed to equal another type's prefix. `K_`..`O_` spanned `L_`, so `GetPasskeysAsync` returned a user's external-login rows as passkeys. Default values live in `TableConstants.RowKeyConstants` (same pattern as all other prefixes); surfaced through the new `IKeyHelper` properties |
| 6 | Passkey index keys | **Revised in review.** Index row `PartitionKey` = `RowKey` = the passkey row key (`K_` + Base64Url(credentialId)), mirroring the login index shape (both keys equal) with zero `IKeyHelper` impact. The original design (reuse `GeneratePartitionKeyIndexByLogin`/`GenerateRowKeyIdentityUserLogin` with a literal provider) was unsafe: those hash **upper-cased** text, but Base64Url is case-sensitive, so distinct credential ids shared an index row (and `ToUpper()` is culture-sensitive); and sharing the login key space let `FindByLoginAsync(provider, Base64Url(credentialId))` resolve a passkey's owner |
| 7 | Timing | Build against RC now (`11.0-*`); switch to `11.0.*` at GA |
| 8 | C# 15 | Bump + targeted modernization only; union types / closed hierarchies excluded (net10 TFM compat) |
| 9 | Scope exclusions | Legacy samples (netcoreapp2.1/3.1, separate sln); `Azure.Data.Tables` stays 12.8.1; `runtime-async` not enabled (WASM consumers) |

**Identity 11 features that light up automatically (app-level, no store changes):** `TimeProvider` support throughout Identity, passkey display-name inference from AAGUID (works because `Aaguid` is preserved in the typed columns), experimental `AddPasskeyEndpoints`/`MapWellKnownPasskeyEndpoints`. DI registration of the store needs no changes — `UserManager.SupportsUserPasskey` checks `Store is IUserPasskeyStore<TUser>`.

*(Review: only the last sentence and the `Aaguid` round trip are verified. `AddPasskeyEndpoints` / `MapWellKnownPasskeyEndpoints` do not exist anywhere in the 11.0.0-rc.1 ASP.NET Core shared framework or `Identity.UI`, and no AAGUID display-name inference API was found there either — treat that sentence as unconfirmed and do not quote it in release notes.)*

---

## Phase 1 — Foundation: TFMs, packages, language version

- [x] **1.1** `src/ElCamino.AspNetCore.Identity.AzureTable/ElCamino.AspNetCore.Identity.AzureTable.csproj`
  - `<TargetFrameworks>` → `net10.0;net11.0`
  - `<LangVersion>` → `15.0`
  - Keep the `net10.0` conditional ItemGroup (`Microsoft.Extensions.Identity.Core` / `Microsoft.Extensions.Identity.Stores` / `Microsoft.Extensions.DependencyInjection` at `10.0.*`)
  - Add `net11.0` conditional ItemGroup → same three packages at `11.0-*` (floating prerelease; `11.0.*` at GA)
  - Delete the `net9.0` and `net8.0` conditional ItemGroups
  - Delete the dead `netstandard2.0` ItemGroup (`Microsoft.CSharp` 4.7.0)
- [x] **1.2** `src/ElCamino.AspNetCore.Identity.AzureTable.Model/ElCamino.AspNetCore.Identity.AzureTable.Model.csproj`
  - `<TargetFrameworks>` → `net10.0;net11.0`; `<LangVersion>` → `15.0`
  - Keep `net10.0` → `Microsoft.Extensions.Identity.Stores` `10.0.*`; add `net11.0` → `11.0-*`
  - Delete `net9.0` / `net8.0` conditional ItemGroups
- [x] **1.3** `src/ElCamino.Azure.Data.Tables/ElCamino.Azure.Data.Tables.csproj`
  - `<TargetFrameworks>` → `net10.0;net11.0`; `<LangVersion>` → `15.0`
- [x] **1.4** `src/ElCamino.Identity.AzureTable.DataUtility/ElCamino.Identity.AzureTable.DataUtility.csproj`
  - `<TargetFramework>` → `net11.0`; `<LangVersion>` → `15.0`
- [x] **1.5** Build the solution (workspace `build` task: `dotnet build ElCamino.AspNetCore.Identity.AzureTable.sln`); confirm both `net10.0` and `net11.0` assets compile. Existing `#if NET10_0_OR_GREATER` / `#if NET9_0_OR_GREATER` guards already cover `net11.0`. Fix any C# 15 analyzer fallout. *(Completed: 0 errors/0 compiler warnings; test-project TFM alignment from 3.1/3.2 pulled into this phase because the solution cannot build until test TFMs match the retargeted libraries. Identity 11.0.0-rc.1 resolved for net11.0.)*
  - *(Review correction: the build is **not** warning-free — pack emits 4× `NU5104` (stable `11.0` package version with prerelease `11.0.0-rc.1` Identity/DI dependencies: 1 in Model, 3 in the main lib). Expected while on `11.0-*`; do not suppress — it is the guard against publishing a stable package that floors consumers on RC Identity bits. Must be gone before release — see 6.6. No vulnerable packages on either TFM (`dotnet list package --vulnerable --include-transitive`); net10.0 resolves Identity 10.0.12.)*

---

## Phase 2 — Passkey store implementation (no JSON; new `IKeyHelper` row properties)

- [x] **2.1** **New file** `src/ElCamino.AspNetCore.Identity.AzureTable.Model/IdentityUserPasskey.cs` *(implemented: typed columns + lossless `FromPasskeyInfo`/`ToPasskeyInfo` converters with `#if NET11_0_OR_GREATER` Aaguid guard; `Base64Url.EncodeToString` (BCL, .NET 9+) for credentialId row keys; property-representation test deferred to Phase 3 with the other tests per plan structure)*
  - *(Review: added `static GenerateRowKey(IKeyHelper, ReadOnlySpan<byte>)` — the single row-key formula, used by `PeekRowKey` and the store (it was duplicated three times) — and `MaxCredentialIdLength = 382`: the row key is prefix + Base64Url(credentialId), and 382 bytes keeps it within 512 characters. Azure documents a 1024-character key limit, but Azurite (3.22.0 and 3.37.0 both verified) rejects keys over 512 with `400 InvalidInput`, so 512 is the size that works everywhere. `FromPasskeyInfo` throws `ArgumentOutOfRangeException` above it. WebAuthn allows 1023 bytes; real authenticators use ≤ ~255. Converters simplified to direct returns. Known, accepted limits: an empty `Transports` array reads back as `null`, a transport containing `;` would split, and a net10.0 writer drops an `Aaguid` column written by a net11.0 writer (Replace upsert).)*
  - `public class IdentityUserPasskey : IGenerateKeys, ITableEntity` (mirrors `IdentityUser.cs` shape)
  - `IGenerateKeys` members: `GenerateKeys(IKeyHelper)` (RowKey = `string.Format(keyHelper.FormatterIdentityUserPasskey, Base64Url(CredentialId))`; PartitionKey = hashed user id), `PeekRowKey(IKeyHelper)`, `KeyVersion`
  - `ITableEntity` members: `PartitionKey`, `RowKey`, `Timestamp`, `ETag` (defaults: `string.Empty` / `ETag.All`)
  - Typed columns (all Azure Tables-supported types):
    - `string UserId` — hashed user id (same value as the user row's partition key)
    - `byte[] CredentialId`, `byte[] PublicKey`, `byte[] AttestationObject`, `byte[] ClientDataJson`, `byte[]? Aaguid`
    - `string? Name`, `DateTimeOffset CreatedAt`, `long SignCount` (`uint` in `UserPasskeyInfo` — checked cast on conversion), `bool IsUserVerified`, `bool IsBackupEligible`, `bool IsBackedUp`
    - `string? Transports` — joined `;`-separated (Azure Tables has no `string[]` column type)
  - **Lossless converters:**
    - `static IdentityUserPasskey FromPasskeyInfo(UserPasskeyInfo passkey, string hashedUserId)` — packs `Transports` (`string.Join(";", ...)`), `SignCount` (`uint`→`long`); reads `Aaguid` guarded by `#if NET11_0_OR_GREATER` (Identity 10 lacks `UserPasskeyInfo.Aaguid`)
    - `UserPasskeyInfo ToPasskeyInfo()` — unpacks `Transports` (`Split(';')`, null when empty), `SignCount` (`checked` cast to `uint`); sets `Aaguid` (same `#if NET11_0_OR_GREATER` guard) so .NET 11 AAGUID display-name inference works
  - XML doc comments (Model csproj has `GenerateDocumentationFile=true`)
  - **Property-representation test** — new file `tests/ElCamino.AspNetCore.Identity.AzureTable.Tests/ModelTests/IdentityUserPasskeyTests.cs` (follows the existing `IdentityUserTests.cs` conventions: `[Fact]` + `[Trait("IdentityCore.Azure.Model", "")]`):
    - Pure reflection, like-for-like property name/type check — **no new dependencies** (no AutoMapper or similar)
    - Algorithm:
      1. Enumerate public instance properties of `Microsoft.AspNetCore.Identity.UserPasskeyInfo`
      2. Build a name-keyed lookup of `IdentityUserPasskey`'s public instance properties
      3. For each `UserPasskeyInfo` property: assert a same-named property exists on `IdentityUserPasskey` — a missing property **fails the test** (assertion message names the missing property and its type)
      4. Type check: `PropertyType` must match exactly (like for like), **except an explicit allowlist for the two documented storage-type adaptations** — `Transports` (`string[]`→`string`) and `SignCount` (`uint`→`long`); any other type mismatch fails with expected-vs-actual in the message
    - Additional properties on `IdentityUserPasskey` (`UserId`, `PartitionKey`, `RowKey`, `Timestamp`, `ETag`, `KeyVersion`) are **allowed** — no reverse check
    - Test project compiles with `<Nullable>disable</Nullable>`, so compare CLR types (`PropertyType`); nullability annotations are not compared
    - **TFM adaptation:** `UserPasskeyInfo`'s property set differs per Identity version — Identity 10 (net10.0 TFM) lacks `Aaguid`; Identity 11 (net11.0 TFM) adds it. Because the test reflects over the compiled-against Identity type, it adapts automatically on each TFM with no `#if` in the test — and a future Identity property addition will fail the test until the matching column is added (nothing can be silently unrepresented)
- [x] **2.2** `IKeyHelper` + key helpers + `TableConstants` — add new passkey row properties: *(Review: upper bound corrected `O_` → `L_`, see decision #5.)*
  - `src/ElCamino.AspNetCore.Identity.AzureTable.Model/IKeyHelper.cs`: add interface properties (following the existing prefix/formatter pattern):
    - `string PreFixIdentityUserPasskey { get; }`
    - `string PreFixIdentityUserPasskeyUpperBound { get; }`
    - `string FormatterIdentityUserPasskey { get; }`
  - `src/ElCamino.AspNetCore.Identity.AzureTable/TableConstants.cs` — nested `RowKeyConstants`: add default consts `PreFixIdentityUserPasskey = "K_"`, `PreFixIdentityUserPasskeyUpperBound = "L_"`, `FormatterIdentityUserPasskey = PreFixIdentityUserPasskey + "{0}"`
  - `src/ElCamino.AspNetCore.Identity.AzureTable/Helpers/BaseKeyHelper.cs`: implement the three new properties as `public virtual` returning the `TableConstants.RowKeyConstants` defaults (same pattern as every other prefix/formatter); bump `KeyVersion` `10.1` → `11.0`
  - `DefaultKeyHelper` / `SHA256KeyHelper`: inherit the new properties automatically — verify no overrides needed
  - Update `tests/ElCamino.AspNetCore.Identity.AzureTable.Tests/Fakes/HashTestKeyHelperFake.cs` for the new interface members (inherits `BaseKeyHelper` → likely compiles unchanged; verify)
  - **Breaking-change note for release notes:** custom `IKeyHelper` implementations must add the three new properties (interface additions are compile-time breaking)
- [x] **2.3** `src/ElCamino.AspNetCore.Identity.AzureTable/UserOnlyStore.cs` — declare `IUserPasskeyStore<TUser>` (no new generic type params → store signatures and DI wiring unchanged) and implement the 5 methods following existing store patterns:
  - *(Review — security fixes, each reproduced by a failing test first, in all four store variants:*
    - ***Cross-user index deletion:** `RemovePasskeyAsync` deleted the index row for any credential id, so any signed-in user could break another user's passkey lookup by "removing" that user's credential id. It now deletes only when the passkey row exists in the caller's partition (the `RemoveLoginAsync` pattern).*
    - ***Index keys:** revised per decision #6 — case-variant credential ids collided and `FindByLoginAsync` resolved passkey owners.*
    - ***Unbounded credential ids:** Identity's assertion flow passes the request's credential id to `FindPasskeyAsync` with no length check (only attestation enforces 1023 bytes). Ids over `MaxCredentialIdLength` now short-circuit (`Find*` → `null`, `Remove` → no-op, `AddOrUpdate` → `ArgumentOutOfRangeException`) before any key or query is built.*
    - *Not changed, by design: the store does not enforce "one credential id → one user" on add (index upsert, like logins); Identity's attestation check (`FindByPasskeyIdAsync` must be null) is the guard. The user row and index row are written in parallel, like logins/claims, so a partial failure can leave them out of step.)*
  - `AddOrUpdatePasskeyAsync(TUser, UserPasskeyInfo, CancellationToken)`:
    - `FromPasskeyInfo(passkey, hashedUserId)` → upsert user-table row (partition = `_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id))`, row = `string.Format(_keyHelper.FormatterIdentityUserPasskey, Base64Url(credentialId))`) via `BatchOperationHelper`/upsert
    - Upsert index-table row (partition = row = the passkey row key, `Id` = hashed user id — **revised**, see decision #6) — enables lookup by credential id alone
  - `GetPasskeysAsync(TUser, CancellationToken)`: partition-scope query between `_keyHelper.PreFixIdentityUserPasskey` / `_keyHelper.PreFixIdentityUserPasskeyUpperBound` row-key bounds (copy `GetClaimsAsync` prefix-range pattern) → `ToPasskeyInfo()` per row
  - `FindPasskeyAsync(TUser, byte[] credentialId, CancellationToken)`: `GetEntityOrDefaultAsync<IdentityUserPasskey>` → `ToPasskeyInfo()`
  - `FindByPasskeyIdAsync(byte[] credentialId, CancellationToken)`: index lookup → `GetUserFromIndexQueryAsync` pattern (mirrors `FindByLoginAsync`)
  - `RemovePasskeyAsync(TUser, byte[] credentialId, CancellationToken)`: **only if the passkey row exists for this user** (revised), delete user-table row + matching index row (`TableConstants.ETagWildcard`) in parallel
  - XML doc comments on all new public members
- [x] **2.4** `src/ElCamino.AspNetCore.Identity.AzureTable/UserOnlyStore.cs` — `DeleteAsync`: extend the existing login/claim index-deletion loops with passkey-index cleanup. User-table passkey rows are already removed by the partition-scope row delete.
  - *(Review: the implementation queried the passkeys **after** starting `DeleteAllUserRowsAsync`, so the partition delete won the race and the index rows were orphaned — and the test could not see it, because a lookup through an orphaned index row also returns null. Now `DeletePasskeyIndexesAsync(userRows, …)` derives the index deletes from the rows `DeleteAsync` already loaded: no second query, no race.)*
- [x] **2.5** `src/ElCamino.AspNetCore.Identity.AzureTable/UserStore.cs` — role store variant inherits passkey support from `UserOnlyStore`. Verify no collision with existing `IUserRoleStore<TUser>` declaration.
  - *(Review: "no edits needed" was wrong — `UserStore` **overrides** `DeleteAsync`, so the role-enabled store (the one most apps register) never cleaned up passkey index rows. It now calls the same `DeletePasskeyIndexesAsync`.)*
- [x] **2.6** Confirm `MapUserAggregate` untouched: passkey rows live in the user partition but are excluded from the claims/logins/tokens prefix filters — no collision with `C_/D_/R_/S_/L_/M_/E_/T_/U_/V_/N_`. *(verified: aggregate code untouched in both stores; `K_` matches none of the `StartsWith` prefix filters)*
- [x] **2.7** Verify the remaining untouched files: `Helpers/DefaultKeyHelper.cs` / `Helpers/SHA256KeyHelper.cs` (inherit new properties from `BaseKeyHelper` — no overrides), `IdentityAzureTableBuilderExtensions.cs` (DI registration unchanged). Update `Fakes/HashTestKeyHelperFake.cs` if needed per 2.2. *(verified: `HashTestKeyHelperFake` inherits virtual properties from `BaseKeyHelper`)*
  - *(Review — the key helpers and `TableQuery` did need a change: `ConvertKeyToHash` (both helpers) and `TableQuery.GenerateValueOperand` sized a `stackalloc` from their input, so a large enough key or filter value **overflowed the stack and killed the process** (reproduced: 2M chars). Passkeys make this reachable without authentication — the assertion's credential id and user handle reach `FindPasskeyAsync` / `FindByIdAsync` unbounded — but the code is pre-existing (shipped in v9.0 through v10.1) and reachable there wherever caller-supplied text reaches `FindByNameAsync` / `FindByEmailAsync`, e.g. a login or registration form. Now stack only up to 1024 bytes, heap above; hash output unchanged (backward-compat tests extended to 4M chars). **Consider backporting to 10.x.**)*

---

## Phase 3 — Tests

- [x] **3.1** `tests/ElCamino.AspNetCore.Identity.AzureTable.Tests/ElCamino.AspNetCore.Identity.AzureTable.Tests.csproj`: `<TargetFrameworks>` → `net10.0;net11.0` *(TFM/LangVersion done in Phase 1 so the solution builds; passkey test code remains in Phase 3)*
- [x] **3.2** `tests/ElCamino.Azure.Data.Tables.Tests/ElCamino.Azure.Data.Tables.Tests.csproj`: `<TargetFrameworks>` `net8.0;net10.0` → `net10.0;net11.0`; `Microsoft.Extensions.*` refs bumped to `11.0-*` *(csproj changes done in Phase 1; test code unchanged)*
- [x] **3.3** `tests/ElCamino.AspNetCore.Identity.AzureTable.Tests/BaseUserStoreTests.Passkeys.partial.cs` — add passkey tests following existing `CreateTestUserAsync`/fixture style (Azurite locally via `UseDevelopmentStorage=true`); overrides registered in all 4 concrete test classes:
  - *(Review: the original tests used a fresh user with one passkey and nothing else, so they could not see the Phase 2 defects. Added, in all 4 classes: `GetUserPasskeysExcludesOtherUserRows` (user with a login, claim and token), `RemoveUserPasskeyRequiresOwnership`, `UserPasskeyCredentialIdIsCaseSensitive`, `UserPasskeyIndexIsNotALoginIndex`, `UserPasskeyCredentialIdLengthLimit` (incl. a 4 MB credential id and a round trip at the 382-byte maximum). `DeleteUserPasskeyIndexCleanup` now proves the index row is gone by re-creating a user with the same id — a lookup through an orphaned row also returns null. `UpdateUserPasskey` uses `uint.MaxValue` for the sign count. Also: `KeyHelperTests.LargeKeyFormatBackwardCompat` and `TableQueryTests.QueryPropertyStringLargeValue` for the stack-overflow fix. Removed unused `store` instances and no-op `!` operators.)*
  - AddOrUpdate → Get → Find → Remove round-trip (assert all `UserPasskeyInfo` fields survive losslessly: `CredentialId`, `PublicKey`, `Name`, `CreatedAt`, `SignCount`, `Transports`, `IsUserVerified`, `IsBackupEligible`, `IsBackedUp`, `AttestationObject`, `ClientDataJson`, `Aaguid` — with `Aaguid` asserted only under `#if NET11_0_OR_GREATER`, matching the converter guard in 2.1)
  - Update-same-credentialId path (e.g. `SignCount`/`Name` update via second `AddOrUpdatePasskeyAsync`)
  - `FindByPasskeyIdAsync` index lookup (passwordless flow) — user found from credential id alone
  - `DeleteAsync` cleans up passkey index rows (index lookup returns null after user delete)
- [x] **3.4** Verify `ModelTests/IdentityUserPasskeyTests.cs` (property-representation test from 2.1) runs green on **both TFMs**: on net10.0 `Aaguid` isn't in Identity 10's `UserPasskeyInfo` so it isn't required; on net11.0 it is — proven by 239/239 passing on net10.0 and net11.0 (test adapts automatically via reflection). *(Review: `UserPasskeyInfo` 10.0.12 vs 11.0.0-rc.1 confirmed by decompiling both assemblies — `Aaguid` is the only difference.)*
- [x] **3.5** Optional: UserManager-level integration test through the existing `CreateUserManager` DI fixture (`AddIdentityCore` + `AddAzureTableStores` + default token providers): all 3.3 tests run through `CreateUserManager` and assert `SupportsUserPasskey == true` plus full `AddOrUpdatePasskeyAsync` → `GetPasskeysAsync` → `RemovePasskeyAsync` flows end-to-end (DI wiring proven unchanged).
- [x] **3.6** ~~Fixed two stale assertions~~ — **reverted in review.** The two `ne` assertions in `tests/ElCamino.Azure.Data.Tables.Tests/` (`TableQueryTests.QueryPropertyDate`, `TableQueryBuilderTests.QueryBuilderPropertyDate`) are back to `Assert.Equal(0, …)`, as set by commit `81af096` ("fixing test to pass on actual storage account"). The claim that real Azure and current Azurite return the entity was wrong: a `ne` filter on a property the entity lacks returns **0** rows on **Azurite 3.37.0** (verified) and, per `81af096`, on a real storage account; only the outdated **Azurite 3.22.0** installed on the dev machine returns 1. Asserting 1 would have failed CI, which runs against real storage. Locally these two tests fail on Azurite 3.22.0 until it is upgraded (`npm i -g azurite`). `.gitignore`: Azurite entries kept, widened to `__azurite_db_*__.json`.

---

## Phase 4 — Ancillary projects, CI, versioning (parallel with Phases 2–3 after Phase 1)

- [x] **4.1** Templates: `templates/templates/StarterWebMvc-CSharp/` + `templates/templates/StarterWebRazorPages-CSharp/`
  - *(Review: the templates are not in the solution, so nothing had ever compiled them. Verified now: both build on net11.0 with 0 warnings / 0 errors against the locally built 11.0.0 packages (Identity.UI 11.0.0-rc.1, CodeGeneration.Design 11.0.0-preview.1); the MVC app starts, serves its pages and registers a user. No EF leftovers, no secrets in the template settings, no vulnerable or deprecated packages after the two fixes below.)*
  - **Remove the direct EF Core package references** from both template csprojs (`samplemvccore.csproj` + `samplerazorpagescore.csproj`) — the apps use Azure Table stores, not EF:
    - Delete `<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" ... />`
    - Delete `<PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" ... />`
  - **Required `Program.cs` cleanup** (both APIs removed with the packages come from `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`):
    - Delete `builder.Services.AddDatabaseDeveloperPageExceptionFilter();`
    - Delete `app.UseMigrationsEndPoint();` and restructure the environment check to `if (!app.Environment.IsDevelopment()) { ... }` keeping `UseExceptionHandler` + `UseHsts` in the branch (the developer exception page is added automatically in Development by `WebApplicationBuilder`)
    - `samplemvccore/Program.cs`: delete the now-unused `using Microsoft.EntityFrameworkCore;` (samplerazorpagescore has no such using — verified)
    - Verify no other EF usage exists in the templates: `ApplicationDbContext` extends `IdentityCloudContext` (not EF `DbContext`), Areas/Identity pages use `UserManager`/`SignInManager` only — confirmed by search
  - `<TargetFramework>` → `net11.0`
  - `ElCamino.AspNetCore.Identity.AzureTable` ref → `11.0-*` (GA: `11.0.*`)
  - `Microsoft.AspNetCore.Identity.UI` ref → `11.0-*` (GA: `11.0.*`) — not an EF package, keep it
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design` ref → `11.0-*` (GA: `11.0.*`) — design-time scaffolding tool, keep it (it is not a *direct* EF reference; its transitive design-time deps are acceptable)
  - `Microsoft.Extensions.Azure` → `1.14.1` — **revised in review** (was "stays `1.11.0`, bump only if a net11-compatible newer version exists"). One exists, and NuGet marks every version up to 1.13.0 deprecated: *"takes a dependency on a deprecated version of Azure.Identity"*.
  - *(Review: `StarterWebRazorPages` `DownloadPersonalData` used `Response.Headers.Add` (analyzer warning ASP0019 in every generated app); now `TryAdd`, as the MVC template already had.)*
- [x] **4.2** `templates/ElCamino.AspNetCore.Identity.AzureTable.Templates.csproj`: `<Version>` → `11.0` (TFM already irrelevant — `IncludeBuildOutput=false`, but bump for consistency)
  - *(Review: the pack glob `templates\**\*` excluded only `bin`/`obj`, so a package built on a dev machine shipped that machine's git-ignored IDE state: 36 files / 5.4 MB in the local 11.0.0 package, including `.vs` Copilot chat sessions and indices, IIS Express `applicationhost.config`, document layouts with local paths, and `*.user` files. A clean CI checkout is not affected. Now also excludes `templates\**\.vs\**` and `templates\**\*.user`; the package is 1.9 MB instead of 3.4 MB and builds without the three `NU5119` warnings.)*
- [x] **4.3** `perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.csproj`: TFM → `net11.0`; verify BenchmarkDotNet 0.14.0 runs on a net11 host (bump BDN if it needs an update for the new runtime); `BenchmarkPackageVersion` default → `11.0.*`
- [x] **4.4** `perf/compare.ps1`: default `-BenchmarkPackageVersion` → `11.0.*` (package-comparison mode)
  - *(Review: the script failed whenever `-Filter` matched a single benchmark class — one CSV comes back as a single object and `.Count` throws under `Set-StrictMode`. The two `Get-BenchmarkCsvs` calls are now wrapped in `@( )`. Found while running it for 6.3.)*
- [x] **4.5** `azure-pipelines.yml`: *(Review: change is as planned. Not changed, pre-existing: the `$(StorageConnection)` secret is expanded into the inline script text; mapping it through `env:` is the safer pattern.)*
  - Replace the `UseDotNet@2` SDK `8.x` step with `11.0.x` **+ `includePreviewVersions: true`** (until GA)
  - Keep the SDK `10.0.x` step (tests build/run on both TFMs via `dotnet test`)
  - No other pipeline changes: tests run against real Azure storage via `$(StorageConnection)` config-rewrite step; `GeneratePackageOnBuild` still produces nupkgs during build
- [x] **4.6** `<Version>` `10.1` → `11.0` in all carrying csprojs: main lib, Model, Data.Tables, DataUtility, both test projects, templates pack, benchmark (8 files). *(Review: all 8 verified. This stable version on RC dependencies is what raises `NU5104` — see 1.5 and 6.6. CI overrides it with `-p:Version=$(BuildVersion)`, so the pipeline variable decides what actually ships.)*

---

## Phase 5 — C# 15 targeted modernization (parallel with Phases 2–3)

- [x] **5.1** Collection expression arguments `[with(capacity: n), ..source]` in hot aggregate-mapping paths:
  - `UserOnlyStore.MapUserAggregate` + `UserStore.MapUserAggregate` (result lists sized from known counts)
  - `EntityMapExtensions.MapTableEntity<T>` reflection-mapped entity construction
  - Audit other `new List<T>(capacity)` sites in `UserOnlyStore` / `UserStore` / `BatchOperationHelper` for the same pattern
  - *(verified: `MapUserAggregate` result lists already use `[]` collection expressions; remaining `new List<T>(capacity)` sites are `Task`-batching buffers where `with(capacity:)` adds no value — left as-is for clarity)*
  - *(Review: agreed, no change needed — the `MapUserAggregate` lists are filled while classifying rows, so there is no known count to pass. Correction: those `[]` expressions pre-date this plan, they were not introduced in Phase 1/2. Net effect of 5.1 + 5.2: **no C# 15 feature is used anywhere yet**; `LangVersion 15.0` is set but the phase's only code change is the 5.3 TFM cleanup.)*
- [x] **5.2** Labeled `break` / `continue` replacing Boolean-flag / `goto` patterns in nested loops (audit `DataUtility/Program.cs`, `BatchOperationHelper`, `GetUsersByIndexQueryAsync` pagination loops)
  - *(verified: no `goto`, no boolean-flag loop exits, and no label statements exist in the audited files — nothing to replace)*
- [x] **5.3** TFM-simplification cleanup (all TFMs ≥ net10 now):
  - *(Review: two leftovers cleaned up. (1) After `ToListAsync(ct)` the unconditional `.AsTask()` was redundant — `ValueTask` has `ConfigureAwait`, and the three other `ToListAsync` call sites already await it directly — so it is removed in both `ProcessAggregateQueryAsync` local functions; it stays in `IsInRoleAsync`, where `Task.WhenAll` needs tasks. (2) `TableQuery.GenerateFilterConditionForGuidNull` still had a `#if NET9_0_OR_GREATER` / `#else` pair, dead now that every TFM is ≥ net10; it now uses `Guid.AllBitsSet` directly. `RoleExistsAsync` is not an API change for anyone still supported: the net10.0 build of 10.x already returned `ValueTask<bool>`, only the dropped net8/net9 builds returned `Task<bool>`. Same for the removed LINQ operators, which were already compiled out of the net10.0 build.)*
  - Drop `#if NET10_0_OR_GREATER` around `.AsTask()` after `ToListAsync(ct)`/`AnyAsync(ct)` chains in `UserStore.cs` (lines ~270–299, ~393–395) and `UserOnlyStore.cs` (~608–610) — make the `.AsTask()` unconditional
  - `RoleExistsAsync` (`UserStore.cs` ~285–299): return type unconditionally `ValueTask<bool>` (minor public API change — **release-note it**)
  - Delete the `#if !NET10_0_OR_GREATER` custom LINQ operator block (`FirstOrDefaultAsync`/`ToListAsync`/`AnyAsync`/`CountAsync`) in `src/ElCamino.Azure.Data.Tables/IAsyncEnumerableExtensions.cs` — BCL provides them on net10+; **keep** `ForEachAsync` (not in BCL)
- [x] **5.4** EXCLUDED from library surface (document why): union types + extension blocks using them (net11-only runtime types would break the net10 TFM), `closed` hierarchies on public store types, memory-safety preview rules, `runtime-async`.

---

## Phase 6 — Verification, docs, GA checklist

- [x] **6.1** Build: workspace `build` task green; `net10.0` + `net11.0` assets confirmed in `bin/` for all shipped projects *(Review: re-verified with a full rebuild after the review changes — 0 errors, 0 compiler warnings; the only warnings are the 4 expected `NU5104`, see 1.5. The 3 `NU5119` template-pack warnings are gone, see 4.2.)*
- [x] **6.2** Tests: both test projects × both TFMs green — **239 + 30 per TFM, 0 failures, on Azurite 3.37.0** (was 214 + 28 before the review's tests). Passkey round-trip, index lookup, DeleteAsync cleanup, and property-representation tests all pass on net10.0 and net11.0. **Combined line coverage = 91.04%** (2043/2244; main 92.1%, Model 98.0%, Data.Tables 84.6%) — above the 85% target
  - *(Review: "green against Azurite" originally held only because 3.6 had bent two assertions to an outdated emulator. On the dev machine's Azurite 3.22.0 the result is 239 + 28: `QueryPropertyDate` and `QueryBuilderPropertyDate` fail there because of that version's `ne` bug — upgrade Azurite rather than change the tests. Not run here: real Azure storage, which is what CI uses.)*
- [ ] **6.3** Perf: **GA-gated** — the *package-comparison* mode of `perf/compare.ps1` compares local build vs. a published `11.0.*` NuGet package, but `11.0.*` is not published yet (latest on NuGet is `10.1.0`). The local benchmark harness was smoke-tested on a net11 host in Phase 4 (BDN 0.14.0 runs, 8 Dry benchmarks executed). Re-run `perf/compare.ps1` after the 11.0.x package is published and record results in `perf/history/`
  - *(Review — interim data point, not a substitute for the GA run: `perf/compare.ps1 -PackageVersion 10.1.0` (local build with the review changes vs. the published 10.1.0, net11 host). All 11 cases are within ±2.5% with identical allocations, except `UserOnlyStore_DeleteAsync`: about +7% averaged over four runs each (72–85 μs local vs 70–77 μs published, +20 B). That benchmark measures one call per iteration and its standard deviation is 5–16 μs, so the difference is inside its noise; the only added work is the passkey index cleanup, a few microseconds against the storage round trips a real delete makes. Not recorded in `perf/history/`.)*
- [x] **6.4** Pack inspection: `ElCamino.AspNetCore.Identity.AzureTable.11.0.0.nupkg`, `.Model.11.0.0.nupkg` and `ElCamino.Azure.Data.Tables.11.0.0.nupkg` contain `net10.0` + `net11.0` lib folders, XML docs, README + icon, and have a `.snupkg` symbols package; `SignAssembly=true` with `tools/key.snk` (strong-name intact)
  - *(Review: re-inspected. The original line listed the Templates package as having lib folders and symbols — it is content-only and has neither — and left out `ElCamino.Azure.Data.Tables`. Strong name verified: `PublicKeyToken=dccd6cad85dcd411`, identical to the published 10.1.0. Dependency groups: net10.0 → Identity/DI `10.0.12`; net11.0 → `11.0.0-rc.1.26425.128` (the `NU5104` source; `Data.Tables` has no Identity dependency and is unaffected). The Templates package inspection is what found the leaked IDE files, see 4.2.)*
- [x] **6.5** API-diff review: changes are additive only (new `IdentityUserPasskey` class, 5 `IUserPasskeyStore` methods, 3 `IKeyHelper` properties) + TFM removals. READMEs use auto-updating NuGet version badges (no hardcoded framework matrix); release notes live on GitHub Releases. Breaking-change note for the release notes: **custom `IKeyHelper` implementers must add `PreFixIdentityUserPasskey`, `PreFixIdentityUserPasskeyUpperBound`, `FormatterIdentityUserPasskey`**; `KeyVersion` bumped 10.1 → 11.0
  - *(Review: confirmed by diffing the public + protected surface of the published 10.1.0 net10.0 assemblies against the new net10.0 builds — nothing removed, `ElCamino.Azure.Data.Tables` unchanged, and `RoleExistsAsync` is **not** a change (see 5.3). Additions beyond the original list: `IdentityUserPasskey.GenerateRowKey` / `MaxCredentialIdLength`, protected `UserOnlyStore.CreatePasskeyIndex` / `DeletePasskeyIndexesAsync`, protected `BaseKeyHelper.MaxStackallocBytes`. The `IKeyHelper` change is **binary**-breaking as well as source-breaking: an implementation that does not derive from `BaseKeyHelper` fails to load against 11.0 until it is recompiled with the three properties.)*
  - *(Release notes should also carry, from this review: passkey credential ids are limited to 382 bytes; and a **security fix** — `ConvertKeyToHash` / `TableQuery` stack overflow on large input, which lets one unauthenticated request with a multi-megabyte username or email terminate the process in v9.0 through v10.1 (confirmed against the published 10.1.0 package with the MVC template's login form). That one deserves a 10.x patch release and an advisory, independent of 11.0.)*
- [ ] **6.6** **GA checklist (Nov 2026) — PENDING, do at .NET 11 GA:**
  - Swap `11.0-*` → `11.0.*` in all csprojs + templates
  - Drop `includePreviewVersions: true` from `azure-pipelines.yml`
  - Full re-test both TFMs; re-run perf comparison
  - Release 11.0.x
  - *(Review additions — gates before publishing:)*
    - *The build must be free of `NU5104`. A stable package that still declares `>= 11.0.0-rc.1` would leave consumers on RC Identity bits permanently (package versions cannot be replaced). Any package published **before** GA should get a prerelease version through `$(BuildVersion)`, as was done for `10.0.0-rc.1.*`.*
    - *Run the pipeline on this branch: CI uses real storage, which this review could not — it validates the restored 3.6 assertions and the passkey tests there (case-sensitive keys, the 382-byte credential id).*
    - *Build both templates from the published packages (they are not in the solution).*
    - *Publish the template package from a clean checkout / CI, not from a dev machine `bin` folder.*

---

## Files Touched (summary)

| Area | Files |
|------|-------|
| csproj | 4 `src/`, 2 `tests/`, `templates/` pack + 2 template apps, `perf/` benchmark |
| Templates | Both template `Program.cs` (EF API cleanup: remove `AddDatabaseDeveloperPageExceptionFilter`, `UseMigrationsEndPoint`, unused EF using); *review:* `Microsoft.Extensions.Azure` 1.14.1 in both csprojs, Razor Pages `DownloadPersonalData.cshtml.cs`, pack glob in the Templates csproj |
| New | `src/ElCamino.AspNetCore.Identity.AzureTable.Model/IdentityUserPasskey.cs`, `tests/...Tests/ModelTests/IdentityUserPasskeyTests.cs` (property-representation test), `tests/...Tests/BaseUserStoreTests.Passkeys.partial.cs`, `plans/dotnet11Upgrade.md` |
| Store | `UserOnlyStore.cs` (interface + 5 methods + `DeleteAsync` + passkey index helpers), `UserStore.cs` (TFM directives; *review:* `DeleteAsync` passkey index cleanup) |
| Constants | `TableConstants.cs` (`K_`/`L_` consts), `Model/IKeyHelper.cs` (3 new prefix/formatter properties), `Helpers/BaseKeyHelper.cs` (implementations + `KeyVersion` 11.0) |
| Key hashing / queries | *review:* `Helpers/DefaultKeyHelper.cs`, `Helpers/SHA256KeyHelper.cs`, `ElCamino.Azure.Data.Tables/TableQuery.cs` (stack-overflow fix; dead `#if`); `IAsyncEnumerableExtensions.cs` (5.3) |
| Tests | `BaseUserStoreTests.Passkeys.partial.cs` + overrides in the 4 concrete store test classes, `KeyHelperTests.cs`, `ElCamino.Azure.Data.Tables.Tests/TableQueryTests.cs` + `TableQueryBuilderTests.cs` |
| CI/perf | `azure-pipelines.yml`, `perf/compare.ps1`, `.gitignore` |
| Untouched | `IdentityAzureTableBuilderExtensions.cs`, `RoleStore.cs`, `MapUserAggregate`, `Fakes/HashTestKeyHelperFake.cs`, legacy samples |