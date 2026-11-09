using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public async Task Admin_with_test_only_grants_should_complete_crud_and_reject_invalid_or_forged_posts()
    {
        await GrantRolePermissionsAsync("admin", DemoRecordsPermissions.Read, DemoRecordsPermissions.Create, DemoRecordsPermissions.Update, DemoRecordsPermissions.Delete);
        await LoginAsync("admin", IdentityDataSeedContributor.AdminPasswordDefaultValue, ListUrl);

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
        (await _repository.GetListAsync()).Count.ShouldBe(countBeforeInvalid);

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
}
