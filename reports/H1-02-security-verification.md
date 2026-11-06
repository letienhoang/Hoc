# H1-02D2 Browser Acceptance — 2026-11-06

## Conclusion

**ACCEPT — for the approved local Development / `hoc_dev` browser-acceptance
scope.** The evidence below is real local-host runtime, PostgreSQL, and
Chrome-browser evidence. It is not deployed-runtime or production evidence.

## Scope and target

- **Source:** `main` at `1564041342e2085540eb6165a849d14172583ea9`; the
  worktree was clean before this documentation update.
- **Host:** `Hoc.Web`, Development, content root
  `D:\job\OaiPhongLabs\HocTat\Hoc\src\Hoc.Web`, listening only on
  `http://127.0.0.1:51235`.
- **Database:** the Development overlay resolved to PostgreSQL database
  `hoc_dev` as `hoc_owner`; Npgsql completed a `VerifyFull` connection.
  `20261102145512_InitialCreate` was the sole recorded migration.
- **Database topology:** zero tenants and tenant connection strings before and
  after the run. The excluded historical local endpoint `127.0.0.1:55433`
  was not listening and was not used.
- **Asset/startup preflight:** the LeptonX package manifest, installed static
  library root, and Release Web output were present. The host logged
  Development and the intended content root before accepting requests.

`pg_stat_ssl` reported `false` for the Neon backend connection. This does not
contradict the Npgsql client-side `VerifyFull` result: with Neon proxying, that
backend field is not a measurement of the client-to-proxy TLS session.

## Browser evidence

An isolated local **HeadlessChrome 156** profile was used against the running
host, with Chrome DevTools Protocol browser and network observations. It did
not reuse a personal browser profile or an external service. Sanitized
browser/network observations are recorded below; no credentials, cookie
values, antiforgery values, or user-list contents were retained.

| Scenario | Sanitized browser / network evidence | Result |
| --- | --- | --- |
| Anonymous administrative MVC access | `/Identity/Users` rendered `/Account/Login?ReturnUrl=%2FIdentity%2FUsers`; no Identity authentication cookie. | PASS |
| Anonymous administrative API access | Browser request to `/api/identity/users` received `302` to `/Account/Login`, followed the Login page, and had no Identity authentication cookie. | PASS |
| Invalid Administrator sign-in | The browser remained on Login, displayed `Invalid username or password!`, and had zero Identity authentication cookies. | PASS |
| Administrator sign-in and user interface | Valid configured Administrator sign-in reached `/Identity/Users`; the Users grid was present. Browser `GET /api/identity/users` returned `200` with an `items` list. The session cookie was `HttpOnly`, `Lax`, path `/`; its value was not retained. | PASS |
| Administrator logout | Logout left zero Identity authentication cookies; subsequent protected navigation challenged at Login. | PASS |
| Self-registration UI | `/Account/Register` contained only an antiforgery field and the disabled-registration notice; it had no Username, Email, Password, or submit controls. | PASS |
| Direct Razor registration | Anonymous valid-antiforgery POST returned `200` with the disabled-registration policy message; PostgreSQL count for the proposed user was `0` before and after. | PASS |
| Direct registration API | Anonymous browser POST to `/api/account/register` returned `403`, `success=false`, and the disabled-registration policy message; PostgreSQL count for the proposed user was `0` before and after. | PASS |

## Reader authorization evidence

The Administrator browser session created exactly one scoped account,
`h1-02d2-reader-20261106-1825` (ID
`3a24c2cb-205b-4498-9b8e-2e936baebc9d`), through
`POST /api/identity/users` (`200`) and assigned only `Reader` through
`PUT /api/identity/users/{id}/roles` (`204`). PostgreSQL then showed exactly
one user row, exactly one `Reader` membership, zero user claims, and zero
`Reader` role permission grants.

All Reader mutation requests below first obtained a fresh valid antiforgery
token in the authenticated browser. They were authorization denials, not
CSRF, routing, or server-error results.

| Scenario | Sanitized browser / network evidence | PostgreSQL before / after | Result |
| --- | --- | --- | --- |
| Reader login | Browser reached `/` and held a new `HttpOnly` Identity session cookie. | Dedicated account was active with only `Reader`. | PASS |
| User-list access | `/Identity/Users` rendered `/Account/AccessDenied?ReturnUrl=%2FIdentity%2FUsers`; no Users grid was present. | No mutation. | PASS |
| Create Identity user | Browser POST to `/api/identity/users` received `302` to `/Account/AccessDenied` after a valid token. | Proposed username: `0` before, `0` after. | PASS |
| Assign an Admin role | Browser PUT to `/api/identity/users/{id}/roles` received `302` to `/Account/AccessDenied` after a valid token. | Existing Reader memberships: `Reader` before and after. | PASS |
| Permission management | Browser GET to `/api/permission-management/permissions?providerName=R&providerKey=Reader` received `302` to `/Account/AccessDenied`. | No mutation. | PASS |

## Cleanup and final database verification

The Administrator browser session deleted only the identified Reader test
account through `DELETE /api/identity/users/{id}` (`204`). This endpoint uses
ABP soft deletion: the physical row remains as one `IsDeleted=true` tombstone,
but the task account has no active identity.

Final read-only PostgreSQL checks:

| Check | Result |
| --- | --- |
| Active accounts named by this task (Reader and all denied-registration/create proposals) | `0` |
| Test Reader tombstone | `1`, `IsDeleted=true` |
| Active Administrator user count | `1` (same count as preflight) |
| `admin` role count | `1` |
| `Reader` role count | `1` |
| Reader role permission grants | `0` |
| Migration history | `20261102145512_InitialCreate` |
| Tenants / tenant connection strings | `0` / `0` |

No migration, seeding, DbMigrator execution, arbitrary SQL write, production
access, source change, or modification of a pre-existing account was
performed. The initial Administrator count, role counts, Reader grants,
migration history, and tenant topology agree with the runtime preflight. A
cryptographic before/after fingerprint of the pre-existing Administrator row
was not captured, so this report does not claim a byte-level row comparison.

## Evidence references and limitations

- Browser/network evidence is the sanitized Chrome DevTools Protocol capture
  summarized in the two tables above: route, status, redirect target, rendered
  path/control presence, and persistence result only.
- The task-specific browser-control host was unavailable. A visible Chrome
  instance available through a separate RDP machine could not reach this
  host's loopback address, so it was not used as evidence. The accepted
  browser session was instead the locally installed Chrome engine with an
  isolated headless profile bound to this host's loopback address.
- This verifies the local Development host plus the approved Neon development
  database. It does not prove a deployed endpoint, production configuration,
  production data, external identity-provider registration, or a human-visible
  interactive-browser run on the RDP machine.
- Prior H1-02D1 TestServer/SQLite evidence is a distinct evidence class and is
  not used to establish any browser or PostgreSQL conclusion here.

## Final recommendation

**ACCEPT** the H1-02D2 browser acceptance scope. Retain the limitations above
for any later deployment or production-readiness decision.
