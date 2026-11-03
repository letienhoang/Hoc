using Volo.Abp.Account.Settings;
using Volo.Abp.Settings;

namespace Hoc.Settings;

public class HocAccountSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        // This runs after the Account module's provider because HocApplicationModule depends on it.
        var selfRegistration = context.GetOrNull(AccountSettingNames.IsSelfRegistrationEnabled);

        if (selfRegistration is null)
        {
            return;
        }

        selfRegistration.DefaultValue = "false";
        selfRegistration.Providers.Clear();
        selfRegistration.WithProviders(DefaultValueSettingValueProvider.ProviderName);
    }
}
