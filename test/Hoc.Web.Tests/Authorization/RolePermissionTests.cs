using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Hoc.Identity;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using Volo.Abp.Authorization;
using Volo.Abp.AspNetCore.Security.Claims;
using Volo.Abp.Data;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Hoc.Authorization;

public class RolePermissionTests : RolePermissionWebTestBase
{
    private const string AdministrativeIdentityUsersUrl = "/api/identity/users";
    private const string AdministrativeIdentityRoleAssignmentsUrl = "/api/identity/users/{0}/roles";
    private const string PermissionManagementUrl = "/api/permission-management/permissions";
    private const string ReaderUserName = "reader-permission-test";
    private const string ReaderEmail = "reader-permission-test@hoc.test";
    private const string ReaderPassword = "Reader-permission-test-1!";
    private const string UnauthorizedCreatedUserName = "reader-unauthorized-create";
    private const string UnauthorizedCreatedUserEmail = "reader-unauthorized-create@hoc.test";
    private const string RoleTargetUserName = "reader-role-target";
    private const string RoleTargetUserEmail = "reader-role-target@hoc.test";
    private const string RolePermissionProviderName = "R";
    private readonly ITestOutputHelper _output;

    public RolePermissionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Admin_and_Reader_roles_should_exist_once_with_expected_defaults()
    {
        GetRequiredService<IAuthorizationService>().ShouldBeOfType<AbpAuthorizationService>();
        GetRequiredService<IAbpAuthorizationService>().ShouldBeOfType<AbpAuthorizationService>();
        GetRequiredService<IPermissionChecker>().ShouldBeOfType<PermissionChecker>();
        GetRequiredService<IMethodInvocationAuthorizationService>().ShouldBeOfType<MethodInvocationAuthorizationService>();
        GetRequiredService<ICurrentPrincipalAccessor>().ShouldBeOfType<HttpContextCurrentPrincipalAccessor>();

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
    public async Task Anonymous_request_to_administrative_identity_users_endpoint_should_be_challenged_without_user_data()
    {
        var response = await Client.GetAsync(AdministrativeIdentityUsersUrl);

        var isChallenge = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect;
        isChallenge.ShouldBeTrue();

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            response.Headers.Location.ShouldNotBeNull();
            response.Headers.Location!.AbsolutePath.ShouldBe("/Account/Login");
        }

        await AssertResponseDoesNotContainUserListAsync(response);
        _output.WriteLine($"Anonymous {AdministrativeIdentityUsersUrl}: {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task Authenticated_Reader_should_be_denied_from_administrative_identity_users_endpoint()
    {
        await CreateAndLoginReaderAsync();

        var response = await Client.GetAsync(AdministrativeIdentityUsersUrl);

        await AssertPermissionDeniedAsync(response);
        await AssertResponseDoesNotContainUserListAsync(response);
        _output.WriteLine($"Reader {AdministrativeIdentityUsersUrl}: {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task Admin_should_receive_a_valid_user_list_from_administrative_identity_users_endpoint()
    {
        (await PostLoginAsync(
            "admin",
            IdentityDataSeedContributor.AdminPasswordDefaultValue)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var response = await Client.GetAsync(AdministrativeIdentityUsersUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertValidUserListResponseAsync(response);
        _output.WriteLine($"Admin {AdministrativeIdentityUsersUrl}: {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task Authenticated_Reader_cannot_create_an_Identity_user_and_the_user_is_not_persisted()
    {
        var antiForgeryToken = await CreateAndLoginReaderAsync();
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            AdministrativeIdentityUsersUrl,
            new
            {
                userName = UnauthorizedCreatedUserName,
                email = UnauthorizedCreatedUserEmail,
                password = "Reader-unauthorized-create-1!",
                isActive = true
            },
            antiForgeryToken);

        var response = await Client.SendAsync(request);

        await AssertPermissionDeniedAsync(response);
        var identityUserRepository = GetRequiredService<IIdentityUserRepository>();
        (await identityUserRepository.FindByNormalizedUserNameAsync(
            UnauthorizedCreatedUserName.ToUpperInvariant())).ShouldBeNull();
        _output.WriteLine($"Reader POST {AdministrativeIdentityUsersUrl}: {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task Authenticated_Reader_cannot_assign_an_administrative_role_to_another_user()
    {
        var targetUser = await CreateUserAsync(RoleTargetUserName, RoleTargetUserEmail);
        var antiForgeryToken = await CreateAndLoginReaderAsync();
        using var request = CreateJsonRequest(
            HttpMethod.Put,
            string.Format(AdministrativeIdentityRoleAssignmentsUrl, targetUser.Id),
            new { roleNames = new[] { "admin" } },
            antiForgeryToken);

        var response = await Client.SendAsync(request);

        await AssertPermissionDeniedAsync(response);
        var userManager = GetRequiredService<IdentityUserManager>();
        (await userManager.GetRolesAsync(targetUser)).ShouldBeEmpty();
        _output.WriteLine($"Reader PUT role assignment: {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task Authenticated_Reader_cannot_access_permission_management_operations()
    {
        await CreateAndLoginReaderAsync();
        var response = await Client.GetAsync(
            $"{PermissionManagementUrl}?providerName={RolePermissionProviderName}&providerKey={HocRoleNames.Reader}");

        await AssertPermissionDeniedAsync(response);
        _output.WriteLine($"Reader GET {PermissionManagementUrl}: {(int)response.StatusCode} {response.StatusCode}");
    }

    private async Task CreateReaderUserAsync()
    {
        await CreateUserAsync(ReaderUserName, ReaderEmail, HocRoleNames.Reader);
    }

    private async Task<IdentityUser> CreateUserAsync(string userName, string email, params string[] roleNames)
    {
        var unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        using (var unitOfWork = unitOfWorkManager.Begin())
        {
            var userManager = GetRequiredService<IdentityUserManager>();
            var guidGenerator = GetRequiredService<IGuidGenerator>();
            var user = new IdentityUser(
                guidGenerator.Create(),
                userName,
                email);

            (await userManager.CreateAsync(user, ReaderPassword)).Succeeded.ShouldBeTrue();
            foreach (var roleName in roleNames)
            {
                (await userManager.AddToRoleAsync(user, roleName)).Succeeded.ShouldBeTrue();
            }

            await unitOfWork.CompleteAsync();
            return user;
        }
    }

    private async Task<string> CreateAndLoginReaderAsync()
    {
        await CreateReaderUserAsync();
        var antiForgeryToken = await GetAntiForgeryTokenAsync(
            $"/Account/Login?ReturnUrl={Uri.EscapeDataString(AdministrativeIdentityUsersUrl)}");
        var loginResponse = await PostLoginAsync(ReaderUserName, ReaderPassword, antiForgeryToken);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        loginResponse.Headers.Location.ShouldNotBeNull();
        loginResponse.Headers.Location!.OriginalString.ShouldBe(AdministrativeIdentityUsersUrl);
        return await GetAntiForgeryTokenAsync("/Account/Register");
    }

    private async Task<HttpResponseMessage> PostLoginAsync(
        string userName,
        string password,
        string? antiForgeryToken = null)
    {
        antiForgeryToken ??= await GetAntiForgeryTokenAsync(
            $"/Account/Login?ReturnUrl={Uri.EscapeDataString(AdministrativeIdentityUsersUrl)}");
        var content = new FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            new System.Collections.Generic.KeyValuePair<string, string>("LoginInput.UserNameOrEmailAddress", userName),
            new System.Collections.Generic.KeyValuePair<string, string>("LoginInput.Password", password),
            new System.Collections.Generic.KeyValuePair<string, string>("LoginInput.RememberMe", "false"),
            new System.Collections.Generic.KeyValuePair<string, string>("ReturnUrl", AdministrativeIdentityUsersUrl),
            new System.Collections.Generic.KeyValuePair<string, string>("ReturnUrlHash", string.Empty)
        });

        return await Client.PostAsync("/Account/Login", content);
    }

    private static HttpRequestMessage CreateJsonRequest(
        HttpMethod method,
        string requestUri,
        object content,
        string antiForgeryToken)
    {
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(content)
        };
        request.Headers.Add("RequestVerificationToken", antiForgeryToken);
        return request;
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

    private static async Task AssertResponseDoesNotContainUserListAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        body.Contains("\"items\"", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    private async Task AssertPermissionDeniedAsync(HttpResponseMessage response)
    {
        var isPermissionDenied = response.StatusCode == HttpStatusCode.Forbidden ||
            (response.StatusCode == HttpStatusCode.Redirect &&
             IsAccessDeniedRedirect(response.Headers.Location));

        isPermissionDenied.ShouldBeTrue();
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("validationErrors");
    }

    private static bool IsAccessDeniedRedirect(Uri? location)
    {
        if (location is null)
        {
            return false;
        }

        var path = location.IsAbsoluteUri
            ? location.AbsolutePath
            : location.OriginalString.Split('?', 2)[0];

        return path == "/Account/AccessDenied";
    }

    private static async Task AssertValidUserListResponseAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        document.RootElement.TryGetProperty("items", out var items).ShouldBeTrue();
        items.ValueKind.ShouldBe(JsonValueKind.Array);
        document.RootElement.TryGetProperty("totalCount", out var totalCount).ShouldBeTrue();
        totalCount.ValueKind.ShouldBe(JsonValueKind.Number);
    }
}
