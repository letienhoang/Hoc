using Microsoft.AspNetCore.Builder;
using Hoc;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();

builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("Hoc.Web.csproj");
await builder.RunAbpModuleAsync<HocWebTestModule>(applicationName: "Hoc.Web" );

public partial class Program
{
}
