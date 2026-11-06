using Hoc.DemoRecords.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Hoc.DemoRecords.Permissions;

public class DemoRecordsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(DemoRecordsPermissions.GroupName, L("Permission:DemoRecords"));

        group.AddPermission(DemoRecordsPermissions.Read, L("Permission:DemoRecords.Read"));
        group.AddPermission(DemoRecordsPermissions.Create, L("Permission:DemoRecords.Create"));
        group.AddPermission(DemoRecordsPermissions.Update, L("Permission:DemoRecords.Update"));
        group.AddPermission(DemoRecordsPermissions.Delete, L("Permission:DemoRecords.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<DemoRecordsResource>(name);
    }
}
