# H1-02 Security Verification

## Scope and evidence

- **Main baseline:** `24db5df1252b06812d10adac269a6f30b8e2529a`.
- **Verified security-test source revision:** `c9547ef7b2ab6db5da176d7dbca38d3cbe41721a`.
- **Environment:** local Windows TestServer, ABP 10.7.0, .NET SDK 10.0.403, isolated in-memory SQLite supplied by `HocEntityFrameworkCoreTestModule`.
- **Out of scope:** DbMigrator, PostgreSQL connection or writes, production access, real credentials, browser automation, deployment, and merge.
- **Test authorization integrity:** `RolePermissionWebTestBase` replaces the permissive test registrations with ABP's `AbpAuthorizationService`, `MethodInvocationAuthorizationService`, `PermissionChecker`, and `HttpContextCurrentPrincipalAccessor`. Assertions also verify these implementations at runtime.

## Verification matrix

| Control | Evidence | Result |
| --- | --- | --- |
| Anonymous protected-route access | `Anonymous_request_to_protected_route_should_be_challenged` checks the login challenge and return URL. | PASS |
| Login and antiforgery | Invalid credentials remain unauthenticated; a login POST without a token cannot authenticate; a valid token/login reaches the protected probe. | PASS |
| Logout/session access | The authenticated client reaches the probe, then logout returns it to a challenge. | PASS |
| Registration UI and direct API | The Razor page exposes no local-registration inputs; both Razor and `/api/account/register` denial paths assert no user is persisted. | PASS |
| Registration with a valid antiforgery token | The Razor registration POST includes a token and is denied by server policy before a user exists. | PASS |
| Registration override prevention | The setting is `false`, accepts only its default provider, and Global/Tenant override attempts throw. | PASS |
| Admin-managed creation | The existing service-level application-service test verifies identity creation in the isolated fixture. It does not independently prove an authenticated Admin can create a user through the actual HTTP endpoint. | PASS — service-level evidence only |
| Reader baseline | Reader is non-default, has no claims or role grants, and its seed is idempotent. | PASS |
| Anonymous and Reader identity-list access | The real `/api/identity/users` endpoint challenges anonymous requests, denies Reader, and returns a valid list to Admin. | PASS |
| Reader user creation | Reader POST to `/api/identity/users` carries an antiforgery token, is authorization-denied, and the proposed user is absent from SQLite. | PASS |
| Reader role escalation | Reader PUT to `/api/identity/users/{id}/roles` carries an antiforgery token, is authorization-denied, and the target user has no roles. | PASS |
| Reader permission-management access | Reader GET to `/api/permission-management/permissions` is authorization-denied; the existing role-grant test separately confirms Reader has no permission grants by default. | PASS |

## Existing and added verification

Existing H1-02A/B/C coverage is retained: protected-route challenge, invalid/valid login, antiforgery login rejection, logout, public-registration Razor/API denial, immutable registration settings, role seed/idempotency/no-grants, and identity-list authorization. The registration tests verify the local-registration behavior only. ABP also has a separate external-login registration flow, but no external identity-provider configuration or exploitable external-registration path has been established for Học. Any future external authentication integration requires a separate registration-policy review; it is not classified here as a confirmed vulnerability.

This change adds only three negative TestServer/SQLite tests: Reader user creation, Reader role assignment, and Reader permission-management access. The two mutation requests carry a freshly issued antiforgery token, assert an authorization-denial response rather than 404/500/validation failure, and check that the targeted persisted state did not change.

## Local validation

| Command | Result |
| --- | --- |
| `./.tools/dotnet/dotnet.exe test test/Hoc.Web.Tests/Hoc.Web.Tests.csproj -c Release -m:1 /nr:false --no-build --no-restore --logger "console;verbosity=minimal"` | PASS — 18 passed, 0 failed, 0 skipped; isolated TestServer/SQLite. |
| `./.tools/dotnet/dotnet.exe build Hoc.slnx -c Release -m:1 /nr:false --no-restore` | PASS — 0 warnings, 0 errors. |
| `git diff --check` | PASS before each commit. |

The Web-project build emitted one pre-existing external test-host warning, `CS7022` from `Microsoft.NET.Test.Sdk.Program.cs`; the full solution Release build reported 0 warnings. No tests were skipped or failed. Local validation is not independent CI verification.

## Security findings and remaining risks

- **Future hardening concern — missing setting definition:** `HocAccountSettingDefinitionProvider` silently returns if the underlying ABP `IsSelfRegistrationEnabled` definition is absent. In the reviewed ABP 10.7.0 path, a missing definition makes `SettingProvider.GetOrNullAsync()` return `null`; `IsTrueAsync()` is true only for the string `true`; and local registration is rejected when the setting evaluates false. The missing-definition path therefore evaluates false for local registration and does not, by itself, establish a fail-open registration bypass. A fail-fast startup validation could be considered later to detect configuration drift, but it is optional hardening rather than urgent vulnerability remediation.
- **LIMITATION — Admin HTTP creation not independently verified:** The Admin-managed creation result is an application-service test in an isolated fixture. The real HTTP endpoint is verified for Admin/Reader identity-list access, but this report does not independently establish an authenticated Admin HTTP create-user operation.
- **NOT VERIFIED — external registration policy:** No external identity provider or valid external-login context has been demonstrated in Học. The ABP external-login registration path needs separate policy review if such authentication is introduced.
- **NOT VERIFIED — browser evidence:** TestServer HTTP assertions do not prove browser cookie handling, rendered error UX, CSRF behavior in a real browser, or session invalidation across concurrent tabs.
- **NOT VERIFIED — PostgreSQL/runtime evidence:** SQLite/TestServer does not establish PostgreSQL behavior, deployed configuration, production database state, or production authorization.
- **NOT VERIFIED — H1-01 runtime limitation:** H1-01’s isolated local runtime proof does not establish a deployed, browser-verified, PostgreSQL-backed service. H1-02 inherits that limitation.

## D2 execution checklist (Owner approval required)

1. Approve a disposable, named PostgreSQL target and its ownership; do not use production or an unrelated service.
2. Authorize a dedicated non-administrative Reader test account and an administrator-operated test account; keep credentials in an approved secret mechanism.
3. Start the approved Web host only, with the approved target and configuration, without running DbMigrator unless separately authorized.
4. In a real browser, record anonymous, Reader, and Admin outcomes for the tested identity and permission routes; include logout/session behavior and registration denial with a valid antiforgery submission.
5. Query only the approved disposable target to confirm denied mutations created no users, role memberships, or permission grants; preserve redacted evidence and clean up only under explicit authorization.

## Technical Lead recommendation

**H1-02D1 automated security evidence remains valid, subject to the stated limitations. H1-02 is NOT ACCEPTED.** Local TestServer/SQLite authorization evidence is suitable to advance the Draft PR and request review after validation. Required D2 browser and approved PostgreSQL runtime evidence remain gates for H1-02 acceptance; unavailable runtime evidence is not a PASS.
