using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Account.Settings;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Xunit;

namespace Hoc.Pages;

public class AccountAuthenticationTests : HocWebTestBase
{
    private const string ProtectedRouteUrl = "/_test/authentication-probe";
    private const string RegistrationUserName = "public-registration-attempt";
    private const string RegistrationEmail = "public-registration-attempt@hoc.test";
    private const string AdminManagedUserName = "admin-managed-account";
    private const string AdminManagedEmail = "admin-managed-account@hoc.test";

    [Fact]
    public async Task Anonymous_request_to_protected_route_should_be_challenged()
    {
        var response = await Client.GetAsync(ProtectedRouteUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.AbsolutePath.ShouldBe("/Account/Login");
        response.Headers.Location.Query.ShouldContain("ReturnUrl=%2F_test%2Fauthentication-probe");
    }

    [Fact]
    public async Task Login_without_an_antiforgery_token_should_not_authenticate()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("LoginInput.UserNameOrEmailAddress", "admin"),
            new KeyValuePair<string, string>("LoginInput.Password", IdentityDataSeedContributor.AdminPasswordDefaultValue),
            new KeyValuePair<string, string>("LoginInput.RememberMe", "false"),
            new KeyValuePair<string, string>("ReturnUrl", ProtectedRouteUrl),
            new KeyValuePair<string, string>("ReturnUrlHash", string.Empty)
        });

        var response = await Client.PostAsync("/Account/Login", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await Client.GetAsync(ProtectedRouteUrl)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Login_and_logout_should_control_access_to_protected_route()
    {
        var invalidLoginResponse = await PostLoginAsync("admin", "invalid-password");

        invalidLoginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.GetAsync(ProtectedRouteUrl)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var validLoginResponse = await PostLoginAsync(
            "admin",
            IdentityDataSeedContributor.AdminPasswordDefaultValue);

        validLoginResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        validLoginResponse.Headers.Location.ShouldNotBeNull();
        validLoginResponse.Headers.Location!.OriginalString.ShouldBe(ProtectedRouteUrl);
        (await Client.GetAsync(ProtectedRouteUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var logoutResponse = await Client.GetAsync("/Account/Logout");

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await Client.GetAsync(ProtectedRouteUrl)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Self_registration_setting_should_be_disabled_and_reject_persisted_overrides()
    {
        var settingDefinitionManager = GetRequiredService<ISettingDefinitionManager>();
        var settingProvider = GetRequiredService<ISettingProvider>();
        var settingManager = GetRequiredService<ISettingManager>();
        var setting = await settingDefinitionManager.GetAsync(AccountSettingNames.IsSelfRegistrationEnabled);

        setting.DefaultValue.ShouldBe("false");
        setting.Providers.ShouldBe(new[] { DefaultValueSettingValueProvider.ProviderName });
        (await settingProvider.IsTrueAsync(AccountSettingNames.IsSelfRegistrationEnabled)).ShouldBeFalse();

        await Should.ThrowAsync<AbpException>(() => settingManager.SetAsync(
            AccountSettingNames.IsSelfRegistrationEnabled,
            "true",
            GlobalSettingValueProvider.ProviderName,
            null));
        await Should.ThrowAsync<AbpException>(() => settingManager.SetAsync(
            AccountSettingNames.IsSelfRegistrationEnabled,
            "true",
            TenantSettingValueProvider.ProviderName,
            "test-tenant"));
    }

    [Fact]
    public async Task Anonymous_registration_page_should_not_offer_a_local_registration_form()
    {
        var response = await Client.GetAsync("/Account/Register");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = new HtmlDocument();
        document.LoadHtml(await response.Content.ReadAsStringAsync());
        document.DocumentNode.SelectSingleNode("//input[@name='Input.UserName']").ShouldBeNull();
        document.DocumentNode.SelectSingleNode("//input[@name='Input.EmailAddress']").ShouldBeNull();
        document.DocumentNode.SelectSingleNode("//input[@name='Input.Password']").ShouldBeNull();
        document.DocumentNode.SelectSingleNode("//button[@type='submit']").ShouldBeNull();
    }

    [Fact]
    public async Task Anonymous_registration_post_should_be_denied_by_server_policy_without_creating_a_user()
    {
        var antiForgeryToken = await GetAntiForgeryTokenAsync("/Account/Register");
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            new KeyValuePair<string, string>("Input.UserName", RegistrationUserName),
            new KeyValuePair<string, string>("Input.EmailAddress", RegistrationEmail),
            new KeyValuePair<string, string>("Input.Password", "Public-registration-attempt-1!")
        });

        var response = await Client.PostAsync("/Account/Register", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Self-registration is disabled for this application");
        await UserShouldNotExistAsync();
    }

    [Fact]
    public async Task Direct_registration_api_should_be_denied_by_server_policy_without_creating_a_user()
    {
        var content = new StringContent(
            $"{{\"userName\":\"{RegistrationUserName}\",\"emailAddress\":\"{RegistrationEmail}\",\"password\":\"Public-registration-attempt-1!\",\"appName\":\"MVC\"}}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync("/api/account/register", content);

        response.IsSuccessStatusCode.ShouldBeFalse();
        (await response.Content.ReadAsStringAsync()).ShouldContain("Self-registration is disabled for this application");
        await UserShouldNotExistAsync();
    }

    [Fact]
    public async Task Admin_managed_account_creation_should_remain_available()
    {
        var identityUserAppService = GetRequiredService<IIdentityUserAppService>();

        var user = await identityUserAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = AdminManagedUserName,
            Email = AdminManagedEmail,
            Password = "Admin-managed-account-1!",
            IsActive = true
        });

        user.UserName.ShouldBe(AdminManagedUserName);
        user.Email.ShouldBe(AdminManagedEmail);
    }

    private async Task<HttpResponseMessage> PostLoginAsync(string userName, string password)
    {
        var antiForgeryToken = await GetAntiForgeryTokenAsync($"/Account/Login?ReturnUrl={Uri.EscapeDataString(ProtectedRouteUrl)}");
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            new KeyValuePair<string, string>("LoginInput.UserNameOrEmailAddress", userName),
            new KeyValuePair<string, string>("LoginInput.Password", password),
            new KeyValuePair<string, string>("LoginInput.RememberMe", "false"),
            new KeyValuePair<string, string>("ReturnUrl", ProtectedRouteUrl),
            new KeyValuePair<string, string>("ReturnUrlHash", string.Empty)
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

    private async Task UserShouldNotExistAsync()
    {
        var identityUserRepository = GetRequiredService<IIdentityUserRepository>();
        var user = await identityUserRepository.FindByNormalizedUserNameAsync(RegistrationUserName.ToUpperInvariant());

        user.ShouldBeNull();
    }
}
