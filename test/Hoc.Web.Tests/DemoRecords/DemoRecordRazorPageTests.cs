using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Hoc.Authorization;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Hoc.Identity;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Routing;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;
using Xunit;

namespace Hoc.DemoRecords.Tests;

public class DemoRecordRazorPageTests : RolePermissionWebTestBase
{
    private const string ListUrl = "/DemoRecords";
    private const string MenuProbeUrl = "/_test/demorecords-menu";
    private const string SecurityContextProbeUrl = "/_test/demorecords-security-context";
    private readonly IRepository<DemoRecord, Guid> _repository;
    private readonly IPermissionManager _permissionManager;

    public DemoRecordRazorPageTests()
    {
        _repository = GetRequiredService<IRepository<DemoRecord, Guid>>();
        _permissionManager = GetRequiredService<IPermissionManager>();
    }

    [Fact]
    public async Task Razor_page_routes_should_be_discoverable_and_anonymous_requests_challenged()
    {
        var endpoints = GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
        endpoints.ShouldContain(endpoint => endpoint.RoutePattern.RawText == "DemoRecords");
        endpoints.ShouldContain(endpoint => endpoint.RoutePattern.RawText == "DemoRecords/Create");
        endpoints.ShouldContain(endpoint => endpoint.RoutePattern.RawText == "DemoRecords/Details/{id:guid}");
        endpoints.ShouldContain(endpoint => endpoint.RoutePattern.RawText == "DemoRecords/Edit/{id:guid}");
        endpoints.ShouldContain(endpoint => endpoint.RoutePattern.RawText == "DemoRecords/Delete/{id:guid}");

        foreach (var url in new[] { ListUrl, "/DemoRecords/Create", $"/DemoRecords/Details/{Guid.NewGuid()}", $"/DemoRecords/Edit/{Guid.NewGuid()}", $"/DemoRecords/Delete/{Guid.NewGuid()}" })
        {
            await AssertChallengeAsync(await Client.GetAsync(url));
        }
    }

    [Fact]
    public async Task Host_users_should_receive_only_the_pages_permitted_by_their_grants()
    {
        var noPermissionUser = $"demorecords-none-{Guid.NewGuid():N}";
        await CreateUserAsync(noPermissionUser, $"{noPermissionUser}@hoc.test", HocRoleNames.Reader);
        await LoginAsync(noPermissionUser, "Reader-permission-test-1!", ListUrl);
        await AssertDeniedAsync(await Client.GetAsync(ListUrl));

        await GrantRolePermissionsAsync(HocRoleNames.Reader, DemoRecordsPermissions.Read);
        var reader = $"demorecords-reader-{Guid.NewGuid():N}";
        await CreateUserAsync(reader, $"{reader}@hoc.test", HocRoleNames.Reader);
        await LoginAsync(reader, "Reader-permission-test-1!", ListUrl);
        var existing = new DemoRecord(Guid.NewGuid(), "Reader record");
        await InsertAsync(existing);

        (await Client.GetAsync(ListUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.GetAsync($"/DemoRecords/Details/{existing.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertDeniedAsync(await Client.GetAsync("/DemoRecords/Create"));
        await AssertDeniedAsync(await Client.GetAsync($"/DemoRecords/Edit/{existing.Id}"));
        await AssertDeniedAsync(await Client.GetAsync($"/DemoRecords/Delete/{existing.Id}"));
    }

    [Fact]
    public async Task Main_menu_should_be_visible_only_to_host_users_with_read_permission()
    {
        (await Client.GetAsync(MenuProbeUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertMenuItemCountAsync(0);

        var noPermissionUser = $"demorecords-menu-none-{Guid.NewGuid():N}";
        await CreateUserAsync(noPermissionUser, $"{noPermissionUser}@hoc.test", HocRoleNames.Reader);
        await LoginAsync(noPermissionUser, "Reader-permission-test-1!", MenuProbeUrl);
        await AssertMenuItemCountAsync(0);

        await GrantRolePermissionsAsync(HocRoleNames.Reader, DemoRecordsPermissions.Read);
        var reader = $"demorecords-menu-reader-{Guid.NewGuid():N}";
        await CreateUserAsync(reader, $"{reader}@hoc.test", HocRoleNames.Reader);
        await LoginAsync(reader, "Reader-permission-test-1!", MenuProbeUrl);
        await AssertMenuItemCountAsync(1);
        (await Client.GetAsync("/Account/Logout")).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var tenantName = $"tenant-menu-{Guid.NewGuid():N}";
        var tenantId = await CreateTenantAsync(tenantName);
        var tenantUser = $"demorecords-menu-tenant-{Guid.NewGuid():N}";
        Guid tenantUserId;
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            tenantUserId = await CreateUserAsync(tenantUser, $"{tenantUser}@hoc.test");
            await _permissionManager.SetAsync(DemoRecordsPermissions.Read, "U", tenantUserId.ToString(), true);
        }

        await LoginAsync(tenantUser, "Reader-permission-test-1!", $"{MenuProbeUrl}?__tenant={Uri.EscapeDataString(tenantName)}", tenantName);
        await AssertTenantPreconditionAsync(tenantUserId, tenantId, tenantName);
        await AssertMenuItemCountAsync(0, $"?__tenant={Uri.EscapeDataString(tenantName)}");
        await AssertTenantMenuContextAsync(tenantUserId, tenantId);
    }

    [Fact]
    public async Task Reader_and_tenant_posts_should_be_denied_without_mutating_records()
    {
        await GrantRolePermissionsAsync(HocRoleNames.Reader, DemoRecordsPermissions.Read);
        var reader = $"demorecords-post-reader-{Guid.NewGuid():N}";
        await CreateUserAsync(reader, $"{reader}@hoc.test", HocRoleNames.Reader);
        await LoginAsync(reader, "Reader-permission-test-1!", ListUrl);
        var record = new DemoRecord(Guid.NewGuid(), "Protected host record");
        await InsertAsync(record);
        var readerToken = await GetAntiForgeryTokenAsync("/Account/Register");

        await AssertDeniedAsync(await Client.PostAsync("/DemoRecords/Create", Form(readerToken, "Reader create", null)));
        await AssertDeniedAsync(await Client.PostAsync($"/DemoRecords/Edit/{record.Id}", Form(readerToken, "Reader update", null)));
        await AssertDeniedAsync(await Client.PostAsync($"/DemoRecords/Delete/{record.Id}", Form(readerToken, null, null)));
        (await _repository.GetAsync(record.Id)).Name.ShouldBe("Protected host record");
        (await _repository.GetListAsync()).Any(item => item.Name == "Reader create").ShouldBeFalse();
        (await Client.GetAsync("/Account/Logout")).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var tenantName = $"tenant-post-{Guid.NewGuid():N}";
        var tenantId = await CreateTenantAsync(tenantName);
        var tenantUser = $"demorecords-post-tenant-{Guid.NewGuid():N}";
        Guid tenantUserId;
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            tenantUserId = await CreateUserAsync(tenantUser, $"{tenantUser}@hoc.test");
            foreach (var permission in new[] { DemoRecordsPermissions.Create, DemoRecordsPermissions.Update, DemoRecordsPermissions.Delete })
            {
                await _permissionManager.SetAsync(permission, "U", tenantUserId.ToString(), true);
            }
        }

        var tenantQuery = $"?__tenant={Uri.EscapeDataString(tenantName)}";
        await LoginAsync(tenantUser, "Reader-permission-test-1!", $"{ListUrl}{tenantQuery}", tenantName);
        await AssertTenantMutationPreconditionAsync(tenantUserId, tenantId, tenantName);
        var tenantToken = await GetAntiForgeryTokenAsync($"/Account/Register{tenantQuery}");
        await AssertDeniedAsync(await Client.PostAsync($"/DemoRecords/Create{tenantQuery}", Form(tenantToken, "Tenant create", null)));
        await AssertDeniedAsync(await Client.PostAsync($"/DemoRecords/Edit/{record.Id}{tenantQuery}", Form(tenantToken, "Tenant update", null)));
        await AssertDeniedAsync(await Client.PostAsync($"/DemoRecords/Delete/{record.Id}{tenantQuery}", Form(tenantToken, null, null)));
        (await _repository.GetAsync(record.Id)).Name.ShouldBe("Protected host record");
        (await _repository.GetListAsync()).Any(item => item.Name == "Tenant create").ShouldBeFalse();
    }

    [Fact]
    public async Task Admin_with_test_only_grants_should_complete_crud_and_reject_invalid_or_forged_posts()
    {
        await GrantRolePermissionsAsync("admin", DemoRecordsPermissions.Read, DemoRecordsPermissions.Create, DemoRecordsPermissions.Update, DemoRecordsPermissions.Delete);
        await LoginAsync("admin", IdentityDataSeedContributor.AdminPasswordDefaultValue, ListUrl);
        await AssertAdminMutationPreconditionAsync();

        var createToken = await GetAntiForgeryTokenAsync("/DemoRecords/Create");
        var create = await Client.PostAsync("/DemoRecords/Create", Form(createToken, "Created in Razor Pages", "Created through TestServer"));
        create.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var created = (await _repository.GetListAsync()).Single(record => record.Name == "Created in Razor Pages");
        (await Client.GetAsync($"/DemoRecords/Details/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var editToken = await GetAntiForgeryTokenAsync($"/DemoRecords/Edit/{created.Id}");
        var update = await Client.PostAsync($"/DemoRecords/Edit/{created.Id}", Form(editToken, "Updated in Razor Pages", "Updated through TestServer"));
        update.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await _repository.GetAsync(created.Id)).Name.ShouldBe("Updated in Razor Pages");

        var countBeforeInvalid = (await _repository.GetListAsync()).Count;
        var invalidToken = await GetAntiForgeryTokenAsync("/DemoRecords/Create");
        var invalid = await Client.PostAsync("/DemoRecords/Create", Form(invalidToken, "   ", null));
        invalid.StatusCode.ShouldBe(HttpStatusCode.OK);
        var invalidBody = await invalid.Content.ReadAsStringAsync();
        invalidBody.ShouldContain("Input.Name");
        invalidBody.ShouldContain("text-danger");
        (await _repository.GetListAsync()).Count.ShouldBe(countBeforeInvalid);

        var csrfCount = (await _repository.GetListAsync()).Count;
        using var rawAdminClient = await CreateRawAdminClientAsync();
        (await rawAdminClient.GetAsync("/DemoRecords/Create")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await rawAdminClient.GetAsync($"/DemoRecords/Edit/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await rawAdminClient.GetAsync($"/DemoRecords/Delete/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        await AssertAntiforgeryRejectedAsync(await rawAdminClient.PostAsync(
            "/DemoRecords/Create",
            Form(null, "CSRF missing create", "Valid form input")));
        await AssertAntiforgeryRejectedAsync(await rawAdminClient.PostAsync(
            "/DemoRecords/Create",
            Form("invalid-antiforgery-token", "CSRF invalid create", "Valid form input")));
        await AssertAntiforgeryRejectedAsync(await rawAdminClient.PostAsync(
            $"/DemoRecords/Edit/{created.Id}",
            Form(null, "CSRF missing edit", "Valid form input")));
        await AssertAntiforgeryRejectedAsync(await rawAdminClient.PostAsync(
            $"/DemoRecords/Edit/{created.Id}",
            Form("invalid-antiforgery-token", "CSRF invalid edit", "Valid form input")));
        await AssertAntiforgeryRejectedAsync(await rawAdminClient.PostAsync(
            $"/DemoRecords/Delete/{created.Id}",
            Form(null, null, null)));
        await AssertAntiforgeryRejectedAsync(await rawAdminClient.PostAsync(
            $"/DemoRecords/Delete/{created.Id}",
            Form("invalid-antiforgery-token", null, null)));
        (await _repository.GetListAsync()).Count.ShouldBe(csrfCount);
        (await _repository.GetAsync(created.Id)).Name.ShouldBe("Updated in Razor Pages");

        var deleteToken = await GetAntiForgeryTokenAsync($"/DemoRecords/Delete/{created.Id}");
        var deletion = await Client.PostAsync($"/DemoRecords/Delete/{created.Id}", Form(deleteToken, null, null));
        deletion.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await _repository.FindAsync(created.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Tenant_principal_with_effective_grants_should_be_denied_by_the_host_only_boundary()
    {
        var tenantName = $"tenant-{Guid.NewGuid():N}";
        var tenantId = await CreateTenantAsync(tenantName);
        var userName = $"demorecords-tenant-{Guid.NewGuid():N}";
        Guid userId;
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            userId = await CreateUserAsync(userName, $"{userName}@hoc.test");
            foreach (var permission in new[] { DemoRecordsPermissions.Read, DemoRecordsPermissions.Create, DemoRecordsPermissions.Update, DemoRecordsPermissions.Delete })
            {
                await _permissionManager.SetAsync(permission, "U", userId.ToString(), true);
            }
        }

        var tenantQuery = $"?__tenant={Uri.EscapeDataString(tenantName)}";
        await LoginAsync(userName, "Reader-permission-test-1!", $"{ListUrl}{tenantQuery}", tenantName);
        foreach (var url in new[] { $"{ListUrl}{tenantQuery}", $"/DemoRecords/Create{tenantQuery}", $"/DemoRecords/Details/{Guid.NewGuid()}{tenantQuery}", $"/DemoRecords/Edit/{Guid.NewGuid()}{tenantQuery}", $"/DemoRecords/Delete/{Guid.NewGuid()}{tenantQuery}" })
        {
            await AssertDeniedAsync(await Client.GetAsync(url));
        }
    }

    private async Task<Guid> CreateTenantAsync(string name)
    {
        using var unitOfWork = GetRequiredService<IUnitOfWorkManager>().Begin();
        var tenant = await GetRequiredService<TenantManager>().CreateAsync(name);
        await GetRequiredService<ITenantRepository>().InsertAsync(tenant, autoSave: true);
        await unitOfWork.CompleteAsync();
        return tenant.Id;
    }

    private async Task<Guid> CreateUserAsync(string userName, string email, params string[] roles)
    {
        using var unitOfWork = GetRequiredService<IUnitOfWorkManager>().Begin();
        var user = new IdentityUser(GetRequiredService<IGuidGenerator>().Create(), userName, email, GetRequiredService<ICurrentTenant>().Id);
        (await GetRequiredService<IdentityUserManager>().CreateAsync(user, "Reader-permission-test-1!")).Succeeded.ShouldBeTrue();
        foreach (var role in roles)
        {
            (await GetRequiredService<IdentityUserManager>().AddToRoleAsync(user, role)).Succeeded.ShouldBeTrue();
        }
        await unitOfWork.CompleteAsync();
        return user.Id;
    }

    private async Task InsertAsync(DemoRecord record)
    {
        await _repository.InsertAsync(record, autoSave: true);
    }

    private async Task GrantRolePermissionsAsync(string role, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            await _permissionManager.SetAsync(permission, "R", role, true);
        }
    }

    private async Task LoginAsync(string userName, string password, string returnUrl, string? tenantName = null)
    {
        var tenantQuery = Uri.EscapeDataString(tenantName ?? string.Empty);
        var token = await GetAntiForgeryTokenAsync($"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&__tenant={tenantQuery}");
        var response = await Client.PostAsync($"/Account/Login?__tenant={tenantQuery}", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("LoginInput.UserNameOrEmailAddress", userName),
            new KeyValuePair<string, string>("LoginInput.Password", password),
            new KeyValuePair<string, string>("LoginInput.RememberMe", "false"),
            new KeyValuePair<string, string>("ReturnUrl", returnUrl),
            new KeyValuePair<string, string>("ReturnUrlHash", string.Empty)
        }));
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    private async Task<string> GetAntiForgeryTokenAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = new HtmlDocument();
        document.LoadHtml(await response.Content.ReadAsStringAsync());
        var token = document.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']")?.GetAttributeValue("value", string.Empty);
        token.ShouldNotBeNullOrWhiteSpace();
        return token;
    }

    private async Task<HttpClient> CreateRawAdminClientAsync()
    {
        var client = new HttpClient(Server.CreateHandler()) { BaseAddress = Client.BaseAddress };
        var loginUrl = $"/Account/Login?ReturnUrl={Uri.EscapeDataString(ListUrl)}";
        var loginPage = await client.GetAsync(loginUrl);
        loginPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cookieHeader = GetCookieHeader(loginPage);
        var document = new HtmlDocument();
        document.LoadHtml(await loginPage.Content.ReadAsStringAsync());
        var token = document.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']")?.GetAttributeValue("value", string.Empty);
        token.ShouldNotBeNullOrWhiteSpace();

        using var login = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("LoginInput.UserNameOrEmailAddress", "admin"),
                new KeyValuePair<string, string>("LoginInput.Password", IdentityDataSeedContributor.AdminPasswordDefaultValue),
                new KeyValuePair<string, string>("LoginInput.RememberMe", "false"),
                new KeyValuePair<string, string>("ReturnUrl", ListUrl),
                new KeyValuePair<string, string>("ReturnUrlHash", string.Empty)
            })
        };
        login.Headers.Add("Cookie", cookieHeader);
        var loginResponse = await client.SendAsync(login);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        client.DefaultRequestHeaders.Add("Cookie", JoinCookies(cookieHeader, GetCookieHeader(loginResponse)));
        return client;
    }

    private static string GetCookieHeader(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? string.Join("; ", values.Select(value => value.Split(';', 2)[0]))
            : string.Empty;
    }

    private static string JoinCookies(string first, string second)
    {
        return string.Join("; ", new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private async Task AssertMenuItemCountAsync(int expectedCount, string query = "")
    {
        var response = await Client.GetAsync($"{MenuProbeUrl}{query}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBe(expectedCount);
        if (expectedCount == 1)
        {
            items[0].GetProperty("name").GetString().ShouldBe("DemoRecords.DemoRecords");
            items[0].GetProperty("url").GetString().ShouldBe(ListUrl);
        }
    }

    private async Task AssertTenantPreconditionAsync(Guid userId, Guid tenantId, string tenantName)
    {
        var response = await Client.GetAsync($"{SecurityContextProbeUrl}?__tenant={Uri.EscapeDataString(tenantName)}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("currentUser").GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();
        root.GetProperty("currentUser").GetProperty("id").GetGuid().ShouldBe(userId);
        root.GetProperty("currentUser").GetProperty("tenantId").GetGuid().ShouldBe(tenantId);
        root.GetProperty("currentTenant").GetProperty("id").GetGuid().ShouldBe(tenantId);
        root.GetProperty("permissions").GetProperty(DemoRecordsPermissions.Read).GetBoolean().ShouldBeTrue();
    }

    private async Task AssertTenantMutationPreconditionAsync(Guid userId, Guid tenantId, string tenantName)
    {
        var response = await Client.GetAsync($"{SecurityContextProbeUrl}?__tenant={Uri.EscapeDataString(tenantName)}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("currentUser").GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();
        root.GetProperty("currentUser").GetProperty("id").GetGuid().ShouldBe(userId);
        root.GetProperty("currentUser").GetProperty("tenantId").GetGuid().ShouldBe(tenantId);
        root.GetProperty("currentTenant").GetProperty("id").GetGuid().ShouldBe(tenantId);
        var permissions = root.GetProperty("permissions");
        permissions.GetProperty(DemoRecordsPermissions.Create).GetBoolean().ShouldBeTrue();
        permissions.GetProperty(DemoRecordsPermissions.Update).GetBoolean().ShouldBeTrue();
        permissions.GetProperty(DemoRecordsPermissions.Delete).GetBoolean().ShouldBeTrue();
    }

    private async Task AssertAdminMutationPreconditionAsync()
    {
        var response = await Client.GetAsync(SecurityContextProbeUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("currentUser").GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();
        var permissions = root.GetProperty("permissions");
        permissions.GetProperty(DemoRecordsPermissions.Create).GetBoolean().ShouldBeTrue();
        permissions.GetProperty(DemoRecordsPermissions.Update).GetBoolean().ShouldBeTrue();
        permissions.GetProperty(DemoRecordsPermissions.Delete).GetBoolean().ShouldBeTrue();
    }

    private async Task AssertTenantMenuContextAsync(Guid userId, Guid tenantId)
    {
        var response = await Client.GetAsync(MenuProbeUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("items").GetArrayLength().ShouldBe(0);
        root.GetProperty("currentUser").GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();
        root.GetProperty("currentUser").GetProperty("id").GetGuid().ShouldBe(userId);
        root.GetProperty("currentUser").GetProperty("tenantId").GetGuid().ShouldBe(tenantId);
    }

    private static FormUrlEncodedContent Form(string? token, string? name, string? description)
    {
        var values = new List<KeyValuePair<string, string>>();
        if (token is not null) values.Add(new("__RequestVerificationToken", token));
        if (name is not null) values.Add(new("Input.Name", name));
        if (description is not null) values.Add(new("Input.Description", description));
        return new FormUrlEncodedContent(values);
    }

    private static Task AssertChallengeAsync(HttpResponseMessage response)
    {
        var path = response.Headers.Location?.AbsolutePath;
        (response.StatusCode == HttpStatusCode.Unauthorized || (response.StatusCode == HttpStatusCode.Redirect && path == "/Account/Login")).ShouldBeTrue();
        return Task.CompletedTask;
    }

    private static Task AssertDeniedAsync(HttpResponseMessage response)
    {
        var path = response.Headers.Location?.AbsolutePath;
        (response.StatusCode == HttpStatusCode.Forbidden || (response.StatusCode == HttpStatusCode.Redirect && path == "/Account/AccessDenied")).ShouldBeTrue();
        return Task.CompletedTask;
    }

    private static Task AssertAntiforgeryRejectedAsync(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        (response.StatusCode == HttpStatusCode.BadRequest ||
         (response.StatusCode == HttpStatusCode.Redirect &&
          IsExactBadRequestErrorRedirect(location))).ShouldBeTrue();
        return Task.CompletedTask;
    }

    private static bool IsExactBadRequestErrorRedirect(Uri? location)
    {
        if (location is null)
        {
            return false;
        }

        var path = location.IsAbsoluteUri
            ? location.AbsolutePath
            : location.OriginalString.Split('?', 2)[0];
        var query = location.IsAbsoluteUri ? location.Query : location.OriginalString.Contains('?') ? location.OriginalString[(location.OriginalString.IndexOf('?') + 1)..] : string.Empty;
        return path == "/Error" && query.Split('&').Contains("httpStatusCode=400", StringComparer.Ordinal);
    }
}
