using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;

namespace Hoc.EntityFrameworkCore.DemoRecords;

[DependsOn(typeof(HocEntityFrameworkCoreTestModule))]
public class DemoRecordApplicationAuthorizationTestModule : AbpModule
{
    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Replace(ServiceDescriptor.Transient<IAuthorizationService, AbpAuthorizationService>());
        context.Services.Replace(ServiceDescriptor.Transient<IAbpAuthorizationService, AbpAuthorizationService>());
        context.Services.Replace(ServiceDescriptor.Transient<IMethodInvocationAuthorizationService, MethodInvocationAuthorizationService>());
        context.Services.Replace(ServiceDescriptor.Transient<IPermissionChecker, PermissionChecker>());
    }
}

public abstract class DemoRecordApplicationAuthorizationTestBase
    : AbpIntegratedTest<DemoRecordApplicationAuthorizationTestModule>
{
    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }
}
