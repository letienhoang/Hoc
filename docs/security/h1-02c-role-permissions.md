# H1-02C: Admin and Reader role foundation

Học keeps ABP Identity's existing `admin` role. The H1-02C seed contributor
does not create, rename, assign, or grant permissions to that role.

It creates `Reader` only when no `READER` role exists in the current ABP data
seed context. The contributor changes to that context's tenant before lookup
and creation, so a host seed cannot see or modify tenant roles (and vice
versa). It uses ABP's `IIdentityRoleRepository` and `IdentityRoleManager`; it
does not use SQL or migrations.

`Reader` is deliberately not a default role, is not assigned to any user, and
receives no permission grants or role claims. No custom Học permission has been
introduced: business-module permissions remain out of scope until an actual
protected resource exists.

The focused Web test suite uses ABP's built-in `AbpIdentity.Users` permission
on an authenticated HTTP probe. It verifies an anonymous login challenge, an
authenticated Reader redirect to `/Account/AccessDenied`, and an Admin `200
OK`. This is isolated SQLite/TestServer evidence only. It is not PostgreSQL,
browser, deployed runtime, tenant-provisioning, or production authorization
evidence.
