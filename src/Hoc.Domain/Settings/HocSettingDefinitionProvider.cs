using Volo.Abp.Settings;

namespace Hoc.Settings;

public class HocSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(HocSettings.MySetting1));
    }
}
