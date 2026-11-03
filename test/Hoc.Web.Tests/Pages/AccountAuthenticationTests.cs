using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Shouldly;
using Volo.Abp.Identity;
using Xunit;

namespace Hoc.Pages;

public class AccountAuthenticationTests : HocWebTestBase
{
    private const string ProtectedRouteUrl = "/_test/authentication-probe";

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
}
