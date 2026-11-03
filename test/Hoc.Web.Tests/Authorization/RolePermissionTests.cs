using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hoc.Identity;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;
using Xunit;

namespace Hoc.Authorization;

public class RolePermissionTests : HocWebTestBase
{
    private const string AdministrativeProbeUrl = "/_test/authorization/identity-users";
    private const string ReaderUserName = "reader-permission-test";
    private const string ReaderEmail = "reader-permission-test@hoc.test";
    private const string ReaderPassword = "Reader-permission-test-1!";
    private const string RolePermissionProviderName = "R";

    [Fact]
    public async Task Admin_and_Reader_roles_should_exist_once_with_expected_defaults()
    {
        GetRequiredService<IAuthorizationService>().ShouldBeOfType<DefaultAuthorizationService>();
        GetRequiredService<IPermissionChecker>().ShouldBeOfType<PermissionChecker>();

        var roleRepository = GetRequiredService<IIdentityRoleRepository>();
        var roles = await roleRepository.GetListAsync(includeDetails: true);

        roles.Count(role => role.NormalizedName == "ADMIN").ShouldBe(1);
        roles.Count(role => role.NormalizedName == "READER").ShouldBe(1);

        var readerRole = roles.Single(role => role.NormalizedName == "READER");
        readerRole.Name.ShouldBe(HocRoleNames.Reader);
        readerRole.IsDefault.ShouldBeFalse();
        readerRole.Claims.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reader_role_seeding_should_be_idempotent_and_preserve_existing_role_state()
    {
        var roleRepository = GetRequiredService<IIdentityRoleRepository>();
        var dataSeeder = GetRequiredService<IDataSeeder>();
        var originalReaderRole = await roleRepository.FindByNormalizedNameAsync("READER");

        originalReaderRole.ShouldNotBeNull();
        originalReaderRole!.IsPublic = true;
        await roleRepository.UpdateAsync(originalReaderRole, autoSave: true);

        await dataSeeder.SeedAsync();
        await dataSeeder.SeedAsync();

        var roles = await roleRepository.GetListAsync(includeDetails: true);
        roles.Count(role => role.NormalizedName == "READER").ShouldBe(1);

        var reseededReaderRole = roles.Single(role => role.NormalizedName == "READER");
        reseededReaderRole.Id.ShouldBe(originalReaderRole.Id);
        reseededReaderRole.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public async Task Reader_should_have_no_role_permission_grants_by_default()
    {
        var permissionGrantRepository = GetRequiredService<IPermissionGrantRepository>();

        var grants = await permissionGrantRepository.GetListAsync(
            RolePermissionProviderName,
            HocRoleNames.Reader);

        grants.ShouldBeEmpty();
    }

    [Fact]
    public async Task Anonymous_request_to_administrative_permission_endpoint_should_be_challenged()
    {
        var response = await Client.GetAsync(AdministrativeProbeUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.AbsolutePath.ShouldBe("/Account/Login");
    }

    [Fact]
    public async Task Authenticated_Reader_should_be_forbidden_from_administrative_permission_endpoint()
    {
        await CreateReaderUserAsync();
        var loginResponse = await PostLoginAsync(ReaderUserName, ReaderPassword);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        loginResponse.Headers.Location.ShouldNotBeNull();
        loginResponse.Headers.Location!.OriginalString.ShouldBe(AdministrativeProbeUrl);

        var response = await Client.GetAsync(AdministrativeProbeUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.AbsolutePath.ShouldBe("/Account/AccessDenied");
    }

    [Fact]
    public async Task Admin_should_be_authorized_for_administrative_permission_endpoint()
    {
        (await PostLoginAsync(
            "admin",
            IdentityDataSeedContributor.AdminPasswordDefaultValue)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        (await Client.GetAsync(AdministrativeProbeUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task CreateReaderUserAsync()
    {
        var unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        using (var unitOfWork = unitOfWorkManager.Begin())
        {
            var userManager = GetRequiredService<IdentityUserManager>();
            var guidGenerator = GetRequiredService<IGuidGenerator>();
            var reader = new IdentityUser(
                guidGenerator.Create(),
                ReaderUserName,
                ReaderEmail);

            (await userManager.CreateAsync(reader, ReaderPassword)).Succeeded.ShouldBeTrue();
            (await userManager.AddToRoleAsync(reader, HocRoleNames.Reader)).Succeeded.ShouldBeTrue();
            await unitOfWork.CompleteAsync();
        }
    }

    private async Task<HttpResponseMessage> PostLoginAsync(string userName, string password)
    {
        var antiForgeryToken = await GetAntiForgeryTokenAsync($"/Account/Login?ReturnUrl={Uri.EscapeDataString(AdministrativeProbeUrl)}");
        var content = new FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            new System.Collections.Generic.KeyValuePair<string, string>("LoginInput.UserNameOrEmailAddress", userName),
            new System.Collections.Generic.KeyValuePair<string, string>("LoginInput.Password", password),
            new System.Collections.Generic.KeyValuePair<string, string>("LoginInput.RememberMe", "false"),
            new System.Collections.Generic.KeyValuePair<string, string>("ReturnUrl", AdministrativeProbeUrl),
            new System.Collections.Generic.KeyValuePair<string, string>("ReturnUrlHash", string.Empty)
        });

        return await Client.PostAsync("/Account/Login", content);
    }

    private async Task<string> GetAntiForgeryTokenAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = new HtmlDocument();
        document.LoadHtml(await response.Content.ReadAsStringAsync());
        var token = document.DocumentNode
            .SelectSingleNode("//input[@name='__RequestVerificationToken']")?
            .GetAttributeValue("value", string.Empty);

        token.ShouldNotBeNullOrWhiteSpace();
        return token;
    }
}
