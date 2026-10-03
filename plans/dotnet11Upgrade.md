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
| 4 | `IKeyHelper` | **Updated with new passkey row properties** (revised — was "unchanged"): add `PreFixIdentityUserPasskey`, `PreFixIdentityUserPasskeyUpperBound`, `FormatterIdentityUserPasskey` to the interface, implemented in `BaseKeyHelper` (virtual, defaulting to `TableConstants` values) and inherited by `DefaultKeyHelper` / `SHA256KeyHelper`. Passkey **index** keys still reuse existing members (`GenerateRowKeyUserId`, `GeneratePartitionKeyIndexByLogin`, `GenerateRowKeyIdentityUserLogin`) — no new generation methods. `KeyVersion` bump to `11.0` (recommended — see 2.2). **Note:** this is a compile-time breaking change for custom `IKeyHelper` implementers — release-note it. |
| 5 | Passkey row prefix | `K_` (rows) / `O_` (upper bound) — confirmed non-colliding with `C_/D_/R_/S_/L_/M_/E_/T_/U_/V_/N_`. Default values live in `TableConstants.RowKeyConstants` (same pattern as all other prefixes); surfaced through the new `IKeyHelper` properties |
| 6 | Passkey index keys | Reuse `GeneratePartitionKeyIndexByLogin`/`GenerateRowKeyIdentityUserLogin` with literal provider `"Passkey"` + Base64Url(credentialId) — mirrors `CreateLoginIndex`, zero `IKeyHelper` impact |
| 7 | Timing | Build against RC now (`11.0-*`); switch to `11.0.*` at GA |
| 8 | C# 15 | Bump + targeted modernization only; union types / closed hierarchies excluded (net10 TFM compat) |
| 9 | Scope exclusions | Legacy samples (netcoreapp2.1/3.1, separate sln); `Azure.Data.Tables` stays 12.8.1; `runtime-async` not enabled (WASM consumers) |

**Identity 11 features that light up automatically (app-level, no store changes):** `TimeProvider` support throughout Identity, passkey display-name inference from AAGUID (works because `Aaguid` is preserved in the typed columns), experimental `AddPasskeyEndpoints`/`MapWellKnownPasskeyEndpoints`. DI registration of the store needs no changes — `UserManager.SupportsUserPasskey` checks `Store is IUserPasskeyStore<TUser>`.

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
- [x] **1.5** Build the solution (workspace `build` task: `dotnet build ElCamino.AspNetCore.Identity.AzureTable.sln`); confirm both `net10.0` and `net11.0` assets compile. Existing `#if NET10_0_OR_GREATER` / `#if NET9_0_OR_GREATER` guards already cover `net11.0`. Fix any C# 15 analyzer fallout. *(Completed: 0 errors/0 warnings; test-project TFM alignment from 3.1/3.2 pulled into this phase because the solution cannot build until test TFMs match the retargeted libraries. Identity 11.0.0-rc.1 resolved for net11.0.)*

---

## Phase 2 — Passkey store implementation (no JSON; new `IKeyHelper` row properties)

- [x] **2.1** **New file** `src/ElCamino.AspNetCore.Identity.AzureTable.Model/IdentityUserPasskey.cs` *(implemented: typed columns + lossless `FromPasskeyInfo`/`ToPasskeyInfo` converters with `#if NET11_0_OR_GREATER` Aaguid guard; `Base64Url.EncodeToString` (BCL, .NET 9+) for credentialId row keys; property-representation test deferred to Phase 3 with the other tests per plan structure)*
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
- [x] **2.2** `IKeyHelper` + key helpers + `TableConstants` — add new passkey row properties:
  - `src/ElCamino.AspNetCore.Identity.AzureTable.Model/IKeyHelper.cs`: add interface properties (following the existing prefix/formatter pattern):
    - `string PreFixIdentityUserPasskey { get; }`
    - `string PreFixIdentityUserPasskeyUpperBound { get; }`
    - `string FormatterIdentityUserPasskey { get; }`
  - `src/ElCamino.AspNetCore.Identity.AzureTable/TableConstants.cs` — nested `RowKeyConstants`: add default consts `PreFixIdentityUserPasskey = "K_"`, `PreFixIdentityUserPasskeyUpperBound = "O_"`, `FormatterIdentityUserPasskey = PreFixIdentityUserPasskey + "{0}"`
  - `src/ElCamino.AspNetCore.Identity.AzureTable/Helpers/BaseKeyHelper.cs`: implement the three new properties as `public virtual` returning the `TableConstants.RowKeyConstants` defaults (same pattern as every other prefix/formatter); bump `KeyVersion` `10.1` → `11.0`
  - `DefaultKeyHelper` / `SHA256KeyHelper`: inherit the new properties automatically — verify no overrides needed
  - Update `tests/ElCamino.AspNetCore.Identity.AzureTable.Tests/Fakes/HashTestKeyHelperFake.cs` for the new interface members (inherits `BaseKeyHelper` → likely compiles unchanged; verify)
  - **Breaking-change note for release notes:** custom `IKeyHelper` implementations must add the three new properties (interface additions are compile-time breaking)
- [x] **2.3** `src/ElCamino.AspNetCore.Identity.AzureTable/UserOnlyStore.cs` — declare `IUserPasskeyStore<TUser>` (no new generic type params → store signatures and DI wiring unchanged) and implement the 5 methods following existing store patterns:
  - `AddOrUpdatePasskeyAsync(TUser, UserPasskeyInfo, CancellationToken)`:
    - `FromPasskeyInfo(passkey, hashedUserId)` → upsert user-table row (partition = `_keyHelper.GenerateRowKeyUserId(ConvertIdToString(user.Id))`, row = `string.Format(_keyHelper.FormatterIdentityUserPasskey, Base64Url(credentialId))`) via `BatchOperationHelper`/upsert
    - Upsert index-table row (partition = `GeneratePartitionKeyIndexByLogin("Passkey", Base64Url(credentialId))`, row = `GenerateRowKeyIdentityUserLogin("Passkey", Base64Url(credentialId))`, `Id` = hashed user id) — enables username-less sign-in
  - `GetPasskeysAsync(TUser, CancellationToken)`: partition-scope query between `_keyHelper.PreFixIdentityUserPasskey` / `_keyHelper.PreFixIdentityUserPasskeyUpperBound` row-key bounds (copy `GetClaimsAsync` prefix-range pattern) → `ToPasskeyInfo()` per row
  - `FindPasskeyAsync(TUser, byte[] credentialId, CancellationToken)`: `GetEntityOrDefaultAsync<IdentityUserPasskey>` → `ToPasskeyInfo()`
  - `FindByPasskeyIdAsync(byte[] credentialId, CancellationToken)`: index lookup → `GetUserFromIndexQueryAsync` pattern (mirrors `FindByLoginAsync`)
  - `RemovePasskeyAsync(TUser, byte[] credentialId, CancellationToken)`: delete user-table row + matching index row (`TableConstants.ETagWildcard`) in parallel
  - XML doc comments on all new public members
- [x] **2.4** `src/ElCamino.AspNetCore.Identity.AzureTable/UserOnlyStore.cs` — `DeleteAsync`: extend the existing login/claim index-deletion loops with passkey-index cleanup (iterate the user's passkeys via the `PreFixIdentityUserPasskey`/`PreFixIdentityUserPasskeyUpperBound` range before `DeleteAllUserRowsAsync`, add `DeleteEntityAsync` tasks for each passkey index row). User-table passkey rows are already removed by the partition-scope row delete.
- [x] **2.5** `src/ElCamino.AspNetCore.Identity.AzureTable/UserStore.cs` — **no edits needed**: role store variant inherits passkey support from `UserOnlyStore`. Verify no collision with existing `IUserRoleStore<TUser>` declaration. *(verified: role variant compiles unchanged on both TFMs)*
- [x] **2.6** Confirm `MapUserAggregate` untouched: passkey rows live in the user partition but are excluded from the claims/logins/tokens prefix filters — no collision with `C_/D_/R_/S_/L_/M_/E_/T_/U_/V_/N_/K_/O_`. *(verified: aggregate code untouched; passkey rows enumerated via separate range query)*
- [x] **2.7** Verify the remaining untouched files: `Helpers/DefaultKeyHelper.cs` / `Helpers/SHA256KeyHelper.cs` (inherit new properties from `BaseKeyHelper` — no overrides), `IdentityAzureTableBuilderExtensions.cs` (DI registration unchanged). Update `Fakes/HashTestKeyHelperFake.cs` if needed per 2.2. *(verified: both compile unchanged — `HashTestKeyHelperFake` inherits virtual properties from `BaseKeyHelper`)*

---

## Phase 3 — Tests

- [x] **3.1** `tests/ElCamino.AspNetCore.Identity.AzureTable.Tests/ElCamino.AspNetCore.Identity.AzureTable.Tests.csproj`: `<TargetFrameworks>` → `net10.0;net11.0` *(TFM/LangVersion done in Phase 1 so the solution builds; passkey test code remains in Phase 3)*
- [x] **3.2** `tests/ElCamino.Azure.Data.Tables.Tests/ElCamino.Azure.Data.Tables.Tests.csproj`: `<TargetFrameworks>` `net8.0;net10.0` → `net10.0;net11.0`; `Microsoft.Extensions.*` refs bumped to `11.0-*` *(csproj changes done in Phase 1; test code unchanged)*
- [x] **3.3** `tests/ElCamino.AspNetCore.Identity.AzureTable.Tests/BaseUserStoreTests.Passkeys.partial.cs` — add passkey tests following existing `CreateTestUserAsync`/fixture style (Azurite locally via `UseDevelopmentStorage=true`); overrides registered in all 4 concrete test classes:
  - AddOrUpdate → Get → Find → Remove round-trip (assert all `UserPasskeyInfo` fields survive losslessly: `CredentialId`, `PublicKey`, `Name`, `CreatedAt`, `SignCount`, `Transports`, `IsUserVerified`, `IsBackupEligible`, `IsBackedUp`, `AttestationObject`, `ClientDataJson`, `Aaguid` — with `Aaguid` asserted only under `#if NET11_0_OR_GREATER`, matching the converter guard in 2.1)
  - Update-same-credentialId path (e.g. `SignCount`/`Name` update via second `AddOrUpdatePasskeyAsync`)
  - `FindByPasskeyIdAsync` index lookup (passwordless flow) — user found from credential id alone
  - `DeleteAsync` cleans up passkey index rows (index lookup returns null after user delete)
- [x] **3.4** Verify `ModelTests/IdentityUserPasskeyTests.cs` (property-representation test from 2.1) runs green on **both TFMs**: on net10.0 `Aaguid` isn't in Identity 10's `UserPasskeyInfo` so it isn't required; on net11.0 it is — proven by 214/214 passing on net10.0 and net11.0 (test adapts automatically via reflection).
- [x] **3.5** Optional: UserManager-level integration test through the existing `CreateUserManager` DI fixture (`AddIdentityCore` + `AddAzureTableStores` + default token providers): all 3.3 tests run through `CreateUserManager` and assert `SupportsUserPasskey == true` plus full `AddOrUpdatePasskeyAsync` → `GetPasskeysAsync` → `RemovePasskeyAsync` flows end-to-end (DI wiring proven unchanged).
- [x] **3.6** *(completed during Phase 3)*: Fixed two stale assertions in `tests/ElCamino.Azure.Data.Tables.Tests/` (`TableQueryTests.cs`, `TableQueryBuilderTests.cs`) — current Azurite (matching real Azure behavior) makes `ne` filters match entities where the property is *absent*, so the `Assert.Equal(0, ...)` for the `ne` query now correctly asserts 1. The old comments claimed an emulator bug; real Azure always matched this way. Also added Azurite state files to `.gitignore`.

---

## Phase 4 — Ancillary projects, CI, versioning (parallel with Phases 2–3 after Phase 1)

- [ ] **4.1** Templates: `templates/templates/StarterWebMvc-CSharp/` + `templates/templates/StarterWebRazorPages-CSharp/`
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
  - `Microsoft.Extensions.Azure` stays `1.11.0` (bump only if a net11-compatible newer version exists)
- [ ] **4.2** `templates/ElCamino.AspNetCore.Identity.AzureTable.Templates.csproj`: `<Version>` → `11.0` (TFM already irrelevant — `IncludeBuildOutput=false`, but bump for consistency)
- [ ] **4.3** `perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.csproj`: TFM → `net11.0`; verify BenchmarkDotNet 0.14.0 runs on a net11 host (bump BDN if it needs an update for the new runtime); `BenchmarkPackageVersion` default → `11.0.*`
- [ ] **4.4** `perf/compare.ps1`: default `-BenchmarkPackageVersion` → `11.0.*` (package-comparison mode)
- [ ] **4.5** `azure-pipelines.yml`:
  - Replace the `UseDotNet@2` SDK `8.x` step with `11.0.x` **+ `includePreviewVersions: true`** (until GA)
  - Keep the SDK `10.0.x` step (tests build/run on both TFMs via `dotnet test`)
  - No other pipeline changes: tests run against real Azure storage via `$(StorageConnection)` config-rewrite step; `GeneratePackageOnBuild` still produces nupkgs during build
- [ ] **4.6** `<Version>` `10.1` → `11.0` in all carrying csprojs: main lib, Model, Data.Tables, DataUtility, both test projects, templates pack, benchmark (8 files).

---

## Phase 5 — C# 15 targeted modernization (parallel with Phases 2–3)

- [ ] **5.1** Collection expression arguments `[with(capacity: n), ..source]` in hot aggregate-mapping paths:
  - `UserOnlyStore.MapUserAggregate` + `UserStore.MapUserAggregate` (result lists sized from known counts)
  - `EntityMapExtensions.MapTableEntity<T>` reflection-mapped entity construction
  - Audit other `new List<T>(capacity)` sites in `UserOnlyStore` / `UserStore` / `BatchOperationHelper` for the same pattern
- [ ] **5.2** Labeled `break` / `continue` replacing Boolean-flag / `goto` patterns in nested loops (audit `DataUtility/Program.cs`, `BatchOperationHelper`, `GetUsersByIndexQueryAsync` pagination loops)
- [ ] **5.3** TFM-simplification cleanup (all TFMs ≥ net10 now):
  - Drop `#if NET10_0_OR_GREATER` around `.AsTask()` after `ToListAsync(ct)`/`AnyAsync(ct)` chains in `UserStore.cs` (lines ~270–299, ~393–395) and `UserOnlyStore.cs` (~608–610) — make the `.AsTask()` unconditional
  - `RoleExistsAsync` (`UserStore.cs` ~285–299): return type unconditionally `ValueTask<bool>` (minor public API change — **release-note it**)
  - Delete the `#if !NET10_0_OR_GREATER` custom LINQ operator block (`FirstOrDefaultAsync`/`ToListAsync`/`AnyAsync`/`CountAsync`) in `src/ElCamino.Azure.Data.Tables/IAsyncEnumerableExtensions.cs` — BCL provides them on net10+; **keep** `ForEachAsync` (not in BCL)
- [ ] **5.4** EXCLUDED from library surface (document why): union types + extension blocks using them (net11-only runtime types would break the net10 TFM), `closed` hierarchies on public store types, memory-safety preview rules, `runtime-async`.

---

## Phase 6 — Verification, docs, GA checklist

- [ ] **6.1** Build: workspace `build` task green; confirm `net10.0` and `net11.0` assets in `bin/`
- [ ] **6.2** Tests: `dotnet test` on both test projects × both TFMs against Azurite (table endpoint 10002); all passkey tests + existing suite green
- [ ] **6.3** Perf: run `perf/compare.ps1` (local project vs. NuGet `11.0.*` package mode) — no significant regressions vs. `perf/history/comparison_v10.0.0` baseline in `MapUserAggregate` / `GetUserQueryAsync`; record results in `perf/history/`
- [ ] **6.4** Pack inspection: `GeneratePackageOnBuild` nupkgs contain `net10.0` + `net11.0` lib folders, snupkg symbols, strong-name signature intact, XML docs present
- [ ] **6.5** API-diff review vs. 10.1 nupkg: changes must be additive only (passkey class + 5 store methods + 3 `IKeyHelper` properties) + TFM removals + `RoleExistsAsync` `ValueTask` note. Update README support matrix, template READMEs, and release notes (net8/9 drop, passkey support, **`IKeyHelper` additions — breaking for custom implementers**, `KeyVersion` 11.0)
- [ ] **6.6** **GA checklist (Nov 2026):**
  - Swap `11.0-*` → `11.0.*` in all csprojs + templates
  - Drop `includePreviewVersions: true` from `azure-pipelines.yml`
  - Full re-test both TFMs; re-run perf comparison
  - Release 11.0.x

---

## Files Touched (summary)

| Area | Files |
|------|-------|
| csproj | 4 `src/`, 2 `tests/`, `templates/` pack + 2 template apps, `perf/` benchmark |
| Templates | Both template `Program.cs` (EF API cleanup: remove `AddDatabaseDeveloperPageExceptionFilter`, `UseMigrationsEndPoint`, unused EF using) |
| New | `src/ElCamino.AspNetCore.Identity.AzureTable.Model/IdentityUserPasskey.cs`, `tests/...Tests/ModelTests/IdentityUserPasskeyTests.cs` (property-representation test), `plans/dotnet11Upgrade.md` |
| Store | `UserOnlyStore.cs` (interface + 5 methods + `DeleteAsync`), `UserStore.cs` (directives only) |
| Constants | `TableConstants.cs` (`K_`/`O_` consts), `Model/IKeyHelper.cs` (3 new prefix/formatter properties), `Helpers/BaseKeyHelper.cs` (implementations + `KeyVersion` 11.0) |
| Tests | `BaseUserStoreTests.cs`, `BaseUserStoreTests.Properties.partial.cs`, `Fakes/HashTestKeyHelperFake.cs` (verify/adjust for new interface members) |
| CI/perf | `azure-pipelines.yml`, `perf/compare.ps1` |
| Untouched | `Helpers/DefaultKeyHelper.cs` / `SHA256KeyHelper.cs` (inherit), `IdentityAzureTableBuilderExtensions.cs`, `RoleStore.cs`, `MapUserAggregate`, legacy samples |