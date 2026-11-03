# H1-02B: Disable public registration

Học disables `Abp.Account.IsSelfRegistrationEnabled` at the application
setting-definition layer. Its effective default is `false`, and only ABP's
default-value (`D`) setting provider may resolve it.

ABP Account 10.7.0 enforces this setting in both the `/Account/Register` Razor
PageModel and `AccountAppService.RegisterAsync`, which backs the conventional
account registration API. Therefore the setting rejects local public
registration before user creation through either entry point.

The provider restriction means global (`G`), tenant (`T`), and user (`U`)
setting values are neither read nor accepted for this setting. Existing
persisted `G` or `T` values cannot re-enable registration. This does not change
the separate `Abp.Account.EnableLocalLogin` setting, so local Admin login and
logout remain available. Admin-managed account creation uses Identity
management services and is not the Account self-registration flow.

No database migration or persisted-setting cleanup is required: legacy values
are ignored by the effective setting definition.
