using System;
using System.Linq;
using System.Threading.Tasks;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Shouldly;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using Xunit;

namespace Hoc.EntityFrameworkCore.DemoRecords;

[Collection(HocTestConsts.CollectionDefinitionName)]
public class DemoRecordAppServiceTests : DemoRecordApplicationAuthorizationTestBase
{
    private readonly IDemoRecordAppService _service;
    private readonly IRepository<DemoRecord, Guid> _repository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IPermissionManager _permissionManager;
    private readonly ICurrentUser _currentUser;

    public DemoRecordAppServiceTests()
    {
        _service = GetRequiredService<IDemoRecordAppService>();
        _repository = GetRequiredService<IRepository<DemoRecord, Guid>>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
        _permissionManager = GetRequiredService<IPermissionManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
    }

    [Fact]
    public async Task Should_create_update_list_and_delete_through_the_application_service()
    {
        await GrantAllPermissionsAsync();
        var created = await _service.CreateAsync(new CreateDemoRecordDto
        {
            Name = "Bravo",
            Description = "Initial description"
        });
        var second = await _service.CreateAsync(new CreateDemoRecordDto
        {
            Name = "Alpha"
        });

        var retrieved = await _service.GetAsync(created.Id);
        retrieved.Name.ShouldBe("Bravo");
        retrieved.Description.ShouldBe("Initial description");

        var updated = await _service.UpdateAsync(created.Id, new UpdateDemoRecordDto
        {
            Name = "Charlie",
            Description = "Updated description"
        });
        updated.Name.ShouldBe("Charlie");

        var list = await _service.GetListAsync(new GetDemoRecordListInput
        {
            SkipCount = 0,
            MaxResultCount = 1
        });
        list.TotalCount.ShouldBe(2);
        list.Items.Count.ShouldBe(1);
        list.Items.Single().Id.ShouldBe(second.Id);

        await _service.DeleteAsync(created.Id);
        await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(created.Id));
        (await _repository.FindAsync(created.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_reject_invalid_input_without_persisting_a_record()
    {
        await GrantAllPermissionsAsync();
        var countBefore = (await _repository.GetListAsync()).Count;
        await Should.ThrowAsync<AbpValidationException>(() => _service.CreateAsync(new CreateDemoRecordDto
        {
            Name = "   "
        }));
        await Should.ThrowAsync<AbpValidationException>(() => _service.CreateAsync(new CreateDemoRecordDto
        {
            Name = new string('n', DemoRecord.MaxNameLength + 1)
        }));
        await Should.ThrowAsync<AbpValidationException>(() => _service.CreateAsync(new CreateDemoRecordDto
        {
            Name = "Valid name",
            Description = new string('d', DemoRecord.MaxDescriptionLength + 1)
        }));

        (await _repository.GetListAsync()).Count.ShouldBe(countBefore);
    }

    [Fact]
    public async Task Should_reject_tenant_context_before_repository_access()
    {
        await GrantAllPermissionsAsync();
        var record = new DemoRecord(Guid.NewGuid(), "Host record");
        await _repository.InsertAsync(record, autoSave: true);

        using (_currentTenant.Change(Guid.NewGuid()))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(() => _service.GetListAsync(new GetDemoRecordListInput()));
            await Should.ThrowAsync<AbpAuthorizationException>(() => _service.UpdateAsync(record.Id, new UpdateDemoRecordDto
            {
                Name = "Tenant mutation"
            }));
        }

        var persisted = await _repository.GetAsync(record.Id);
        persisted.Name.ShouldBe("Host record");
    }

    [Fact]
    public async Task Should_enforce_permissions_for_direct_application_service_calls()
    {
        var userId = _currentUser.Id!.Value.ToString();
        await _permissionManager.SetAsync(DemoRecordsPermissions.Create, "U", userId, false);
        var countBefore = (await _repository.GetListAsync()).Count;

        await Should.ThrowAsync<AbpAuthorizationException>(() => _service.CreateAsync(new CreateDemoRecordDto
        {
            Name = "Unauthorized direct call"
        }));

        (await _repository.GetListAsync()).Count.ShouldBe(countBefore);
    }

    private async Task GrantAllPermissionsAsync()
    {
        var userId = _currentUser.Id!.Value.ToString();
        foreach (var permissionName in new[]
                 {
                     DemoRecordsPermissions.Read,
                     DemoRecordsPermissions.Create,
                     DemoRecordsPermissions.Update,
                     DemoRecordsPermissions.Delete
                 })
        {
            await _permissionManager.SetAsync(permissionName, "U", userId, true);
        }
    }
}
