using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Hoc.Authorization;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Hoc.Identity;
using HtmlAgilityPack;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;
using Xunit;

namespace Hoc.DemoRecords.Tests;

public class DemoRecordHttpApiAuthorizationTests : RolePermissionWebTestBase
{
    private const string CollectionUrl = "/api/demo-records/demo-record";
    private const string SecurityContextProbeUrl = "/_test/demorecords-security-context";
    private readonly IRepository<DemoRecord, Guid> _repository;
    private readonly IPermissionManager _permissionManager;

    public DemoRecordHttpApiAuthorizationTests()
    {
        _repository = GetRequiredService<IRepository<DemoRecord, Guid>>();
        _permissionManager = GetRequiredService<IPermissionManager>();
    }

    [Fact]
    public async Task Anonymous_requests_should_be_challenged_for_all_operations()
    {
        var id = Guid.NewGuid();

        await AssertChallengeAsync(await Client.GetAsync(CollectionUrl));
        await AssertChallengeAsync(await Client.GetAsync($"{CollectionUrl}/{id}"));
        await AssertChallengeAsync(await Client.PostAsJsonAsync(CollectionUrl, new { name = "Anonymous" }));
        await AssertChallengeAsync(await Client.PutAsJsonAsync($"{CollectionUrl}/{id}", new { name = "Anonymous" }));
        await AssertChallengeAsync(await Client.DeleteAsync($"{CollectionUrl}/{id}"));
    }

    [Fact]
    public async Task Authenticated_principal_without_permissions_should_be_denied_without_mutation()
    {
        var userName = $"demorecords-none-{Guid.NewGuid():N}";
        await CreateUserAsync(userName, $"{userName}@hoc.test", HocRoleNames.Reader);
        var antiForgeryToken = await LoginAsync(userName, "Reader-permission-test-1!");
        var existing = new DemoRecord(Guid.NewGuid(), "Existing host record");
        await InsertAsync(existing);

        await AssertDeniedAsync(await Client.GetAsync(CollectionUrl));
        await AssertDeniedAsync(await Client.GetAsync($"{CollectionUrl}/{existing.Id}"));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Post, CollectionUrl, new { name = "Denied create" }, antiForgeryToken));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Put, $"{CollectionUrl}/{existing.Id}", new { name = "Denied update" }, antiForgeryToken));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Delete, $"{CollectionUrl}/{existing.Id}", null, antiForgeryToken));

        (await _repository.FindAsync(existing.Id)).ShouldNotBeNull();
        (await _repository.GetAsync(existing.Id)).Name.ShouldBe("Existing host record");
        (await _repository.GetListAsync()).Any(record => record.Name == "Denied create").ShouldBeFalse();
    }

    [Fact]
    public async Task Reader_with_only_read_permission_should_read_but_not_mutate()
    {
        await GrantRolePermissionsAsync(HocRoleNames.Reader, DemoRecordsPermissions.Read);
        var userName = $"demorecords-reader-{Guid.NewGuid():N}";
        await CreateUserAsync(userName, $"{userName}@hoc.test", HocRoleNames.Reader);
        var antiForgeryToken = await LoginAsync(userName, "Reader-permission-test-1!");
        var existing = new DemoRecord(Guid.NewGuid(), "Reader visible record");
        await InsertAsync(existing);

        (await Client.GetAsync(CollectionUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.GetAsync($"{CollectionUrl}/{existing.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Post, CollectionUrl, new { name = "Reader denied create" }, antiForgeryToken));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Put, $"{CollectionUrl}/{existing.Id}", new { name = "Reader denied update" }, antiForgeryToken));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Delete, $"{CollectionUrl}/{existing.Id}", null, antiForgeryToken));

        (await _repository.GetAsync(existing.Id)).Name.ShouldBe("Reader visible record");
        (await _repository.GetListAsync()).Any(record => record.Name == "Reader denied create").ShouldBeFalse();
    }

    [Fact]
    public async Task Admin_with_test_only_crud_grants_should_complete_crud_and_receive_validation_errors()
    {
        await GrantRolePermissionsAsync("admin",
            DemoRecordsPermissions.Read,
            DemoRecordsPermissions.Create,
            DemoRecordsPermissions.Update,
            DemoRecordsPermissions.Delete);
        var antiForgeryToken = await LoginAsync("admin", IdentityDataSeedContributor.AdminPasswordDefaultValue);

        var createResponse = await SendJsonAsync(HttpMethod.Post, CollectionUrl, new
        {
            name = "HTTP created",
            description = "Created through TestServer"
        }, antiForgeryToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await ReadDtoAsync(createResponse);

        var updateResponse = await SendJsonAsync(HttpMethod.Put, $"{CollectionUrl}/{created.Id}", new
        {
            name = "HTTP updated",
            description = "Updated through TestServer"
        }, antiForgeryToken);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadDtoAsync(updateResponse)).Name.ShouldBe("HTTP updated");

        var listResponse = await Client.GetAsync($"{CollectionUrl}?skipCount=0&maxResultCount=1");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using (var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()))
        {
            listDocument.RootElement.GetProperty("totalCount").GetInt64().ShouldBeGreaterThan(0);
            listDocument.RootElement.GetProperty("items").GetArrayLength().ShouldBe(1);
        }

        (await SendJsonAsync(HttpMethod.Post, CollectionUrl, new { name = "   " }, antiForgeryToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SendJsonAsync(HttpMethod.Post, CollectionUrl, new { name = new string('n', 129) }, antiForgeryToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SendJsonAsync(HttpMethod.Post, CollectionUrl, new { name = "Valid", description = new string('d', 1025) }, antiForgeryToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SendRawJsonAsync(HttpMethod.Post, CollectionUrl, "null", antiForgeryToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await Client.GetAsync($"{CollectionUrl}?skipCount=-1&maxResultCount=101")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await Client.GetAsync($"{CollectionUrl}/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SendJsonAsync(HttpMethod.Put, $"{CollectionUrl}/{Guid.NewGuid()}", new { name = "Missing" }, antiForgeryToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SendJsonAsync(HttpMethod.Delete, $"{CollectionUrl}/{Guid.NewGuid()}", null, antiForgeryToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Client.GetAsync($"{CollectionUrl}/{Guid.Empty}")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await SendJsonAsync(HttpMethod.Delete, $"{CollectionUrl}/{created.Id}", null, antiForgeryToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _repository.FindAsync(created.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Tenant_context_should_be_denied_despite_host_admin_crud_grants()
    {
        await GrantRolePermissionsAsync("admin",
            DemoRecordsPermissions.Read,
            DemoRecordsPermissions.Create,
            DemoRecordsPermissions.Update,
            DemoRecordsPermissions.Delete);
        var tenantName = $"tenant-{Guid.NewGuid():N}";
        var tenantId = await CreateTenantAsync(tenantName);
        var tenantUserName = $"demorecords-tenant-{Guid.NewGuid():N}";
        Guid tenantUserId;
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            tenantUserId = await CreateUserAsync(tenantUserName, $"{tenantUserName}@hoc.test");
            foreach (var permissionName in new[]
                     {
                         DemoRecordsPermissions.Read,
                         DemoRecordsPermissions.Create,
                         DemoRecordsPermissions.Update,
                         DemoRecordsPermissions.Delete
                     })
            {
                await _permissionManager.SetAsync(permissionName, "U", tenantUserId.ToString(), true);
            }
        }
        var antiForgeryToken = await LoginAsync(tenantUserName, "Reader-permission-test-1!", tenantName);
        var existing = new DemoRecord(Guid.NewGuid(), "Host-only record");
        await InsertAsync(existing);

        var tenantQuery = $"?__tenant={Uri.EscapeDataString(tenantName)}";
        await AssertTenantSecurityContextAsync(tenantUserId, tenantId, tenantQuery, expectTenantContext: true);
        await AssertDeniedAsync(await Client.GetAsync($"{CollectionUrl}{tenantQuery}"));
        await AssertDeniedAsync(await Client.GetAsync($"{CollectionUrl}/{existing.Id}{tenantQuery}"));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Post, $"{CollectionUrl}{tenantQuery}", new { name = "Tenant denied create" }, antiForgeryToken));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Put, $"{CollectionUrl}/{existing.Id}{tenantQuery}", new { name = "Tenant denied update" }, antiForgeryToken));
        await AssertDeniedAsync(await SendJsonAsync(HttpMethod.Delete, $"{CollectionUrl}/{existing.Id}{tenantQuery}", null, antiForgeryToken));

        await AssertTenantSecurityContextAsync(tenantUserId, tenantId, string.Empty, expectTenantContext: false);
        await AssertDeniedAsync(await Client.GetAsync($"{CollectionUrl}/{existing.Id}"));

        (await _repository.GetAsync(existing.Id)).Name.ShouldBe("Host-only record");
        (await _repository.GetListAsync()).Any(record => record.Name == "Tenant denied create").ShouldBeFalse();
    }

    private async Task GrantRolePermissionsAsync(string roleName, params string[] permissionNames)
    {
        foreach (var permissionName in permissionNames)
        {
            await _permissionManager.SetAsync(permissionName, "R", roleName, true);
        }
    }

    private async Task<Guid> CreateUserAsync(string userName, string email, params string[] roleNames)
    {
        Guid userId = Guid.Empty;
        await UseUnitOfWorkAsync(async () =>
        {
            var userManager = GetRequiredService<IdentityUserManager>();
            var guidGenerator = GetRequiredService<IGuidGenerator>();
            var user = new IdentityUser(
                guidGenerator.Create(),
                userName,
                email,
                tenantId: GetRequiredService<ICurrentTenant>().Id);

            (await userManager.CreateAsync(user, "Reader-permission-test-1!")).Succeeded.ShouldBeTrue();
            foreach (var roleName in roleNames)
            {
                (await userManager.AddToRoleAsync(user, roleName)).Succeeded.ShouldBeTrue();
            }
            userId = user.Id;
        });
        return userId;
    }

    private async Task<Guid> CreateTenantAsync(string name)
    {
        var tenantId = Guid.NewGuid();
        await UseUnitOfWorkAsync(async () =>
        {
            var tenantManager = GetRequiredService<TenantManager>();
            var tenantRepository = GetRequiredService<ITenantRepository>();
            var tenant = await tenantManager.CreateAsync(name);
            tenantId = tenant.Id;
            await tenantRepository.InsertAsync(tenant, autoSave: true);
        });
        return tenantId;
    }

    private Task InsertAsync(DemoRecord record)
    {
        return UseUnitOfWorkAsync(() => _repository.InsertAsync(record, autoSave: true));
    }

    private async Task UseUnitOfWorkAsync(Func<Task> action)
    {
        var unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        using var unitOfWork = unitOfWorkManager.Begin();
        await action();
        await unitOfWork.CompleteAsync();
    }

    private async Task<string> LoginAsync(string userName, string password, string? tenantName = null)
    {
        var tenantQuery = tenantName is null ? string.Empty : $"&__tenant={Uri.EscapeDataString(tenantName)}";
        var antiForgeryToken = await GetAntiForgeryTokenAsync($"/Account/Login?ReturnUrl={Uri.EscapeDataString(CollectionUrl)}{tenantQuery}");
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            new KeyValuePair<string, string>("LoginInput.UserNameOrEmailAddress", userName),
            new KeyValuePair<string, string>("LoginInput.Password", password),
            new KeyValuePair<string, string>("LoginInput.RememberMe", "false"),
            new KeyValuePair<string, string>("ReturnUrl", CollectionUrl),
            new KeyValuePair<string, string>("ReturnUrlHash", string.Empty)
        });
        var response = await Client.PostAsync($"/Account/Login?__tenant={Uri.EscapeDataString(tenantName ?? string.Empty)}", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        return await GetAntiForgeryTokenAsync($"/Account/Register?__tenant={Uri.EscapeDataString(tenantName ?? string.Empty)}");
    }

    private async Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string url, object? body, string antiForgeryToken)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        request.Headers.Add("RequestVerificationToken", antiForgeryToken);
        return await Client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendRawJsonAsync(HttpMethod method, string url, string json, string antiForgeryToken)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("RequestVerificationToken", antiForgeryToken);
        return await Client.SendAsync(request);
    }

    private async Task<string> GetAntiForgeryTokenAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = new HtmlDocument();
        document.LoadHtml(await response.Content.ReadAsStringAsync());
        var token = document.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']")?
            .GetAttributeValue("value", string.Empty);
        token.ShouldNotBeNullOrWhiteSpace();
        return token;
    }

    private async Task AssertTenantSecurityContextAsync(
        Guid expectedUserId,
        Guid expectedTenantId,
        string query,
        bool expectTenantContext)
    {
        var response = await Client.GetAsync($"{SecurityContextProbeUrl}{query}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var currentUser = root.GetProperty("currentUser");
        currentUser.GetProperty("id").GetGuid().ShouldBe(expectedUserId);
        currentUser.GetProperty("tenantId").GetGuid().ShouldBe(expectedTenantId);
        currentUser.GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();

        if (expectTenantContext)
        {
            var currentTenant = root.GetProperty("currentTenant");
            currentTenant.GetProperty("id").GetGuid().ShouldBe(expectedTenantId);
            currentTenant.GetProperty("isAvailable").GetBoolean().ShouldBeTrue();

            var permissions = root.GetProperty("permissions");
            permissions.GetProperty(DemoRecordsPermissions.Read).GetBoolean().ShouldBeTrue();
            permissions.GetProperty(DemoRecordsPermissions.Create).GetBoolean().ShouldBeTrue();
            permissions.GetProperty(DemoRecordsPermissions.Update).GetBoolean().ShouldBeTrue();
            permissions.GetProperty(DemoRecordsPermissions.Delete).GetBoolean().ShouldBeTrue();
        }
    }

    private static async Task<DemoRecordDto> ReadDtoAsync(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<DemoRecordDto>();
        result.ShouldNotBeNull();
        return result!;
    }

    private static Task AssertChallengeAsync(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        var locationPath = location is null
            ? null
            : location.IsAbsoluteUri
                ? location.AbsolutePath
                : location.OriginalString.Split('?', 2)[0];
        var isChallenge = response.StatusCode == HttpStatusCode.Unauthorized ||
            (response.StatusCode == HttpStatusCode.Redirect &&
             locationPath == "/Account/Login" &&
             location?.OriginalString.Contains("ReturnUrl=", StringComparison.Ordinal) == true);
        isChallenge.ShouldBeTrue();
        return Task.CompletedTask;
    }

    private static Task AssertDeniedAsync(HttpResponseMessage response)
    {
        var path = response.Headers.Location is null
            ? null
            : response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location.AbsolutePath
                : response.Headers.Location.OriginalString.Split('?', 2)[0];
        var isDenied = response.StatusCode == HttpStatusCode.Forbidden ||
            (response.StatusCode == HttpStatusCode.Redirect &&
             path == "/Account/AccessDenied");
        isDenied.ShouldBeTrue();
        return Task.CompletedTask;
    }
}
