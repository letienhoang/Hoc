using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Validation;

namespace Hoc.DemoRecords.DemoRecords;

public class DemoRecordAppService : ApplicationService, IDemoRecordAppService
{
    private readonly IRepository<DemoRecord, Guid> _repository;
    private readonly IGuidGenerator _guidGenerator;

    public DemoRecordAppService(
        IRepository<DemoRecord, Guid> repository,
        IGuidGenerator guidGenerator)
    {
        _repository = repository;
        _guidGenerator = guidGenerator;
    }

    [Authorize(Permissions.DemoRecordsPermissions.Read)]
    public virtual async Task<DemoRecordDto> GetAsync(Guid id)
    {
        EnsureHostAccess();
        EnsureValidId(id);

        return MapToDto(await _repository.GetAsync(id));
    }

    [Authorize(Permissions.DemoRecordsPermissions.Read)]
    public virtual async Task<PagedResultDto<DemoRecordDto>> GetListAsync(GetDemoRecordListInput input)
    {
        EnsureHostAccess();
        EnsureInput(input);

        var queryable = await _repository.GetQueryableAsync();
        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var records = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(record => record.Name)
                .ThenBy(record => record.Id)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<DemoRecordDto>(
            totalCount,
            records.Select(MapToDto).ToList());
    }

    [Authorize(Permissions.DemoRecordsPermissions.Create)]
    public virtual async Task<DemoRecordDto> CreateAsync(CreateDemoRecordDto input)
    {
        EnsureHostAccess();
        EnsureInput(input);

        var record = new DemoRecord(_guidGenerator.Create(), input.Name, input.Description);
        await _repository.InsertAsync(record, autoSave: true);

        return MapToDto(record);
    }

    [Authorize(Permissions.DemoRecordsPermissions.Update)]
    public virtual async Task<DemoRecordDto> UpdateAsync(Guid id, UpdateDemoRecordDto input)
    {
        EnsureHostAccess();
        EnsureValidId(id);
        EnsureInput(input);

        var record = await _repository.GetAsync(id);
        record.Update(input.Name, input.Description);
        await _repository.UpdateAsync(record, autoSave: true);

        return MapToDto(record);
    }

    [Authorize(Permissions.DemoRecordsPermissions.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        EnsureHostAccess();
        EnsureValidId(id);

        var record = await _repository.GetAsync(id);
        await _repository.DeleteAsync(record, autoSave: true);
    }

    private void EnsureHostAccess()
    {
        if (CurrentTenant.Id.HasValue || CurrentUser.TenantId.HasValue)
        {
            throw new AbpAuthorizationException("Demo records are available only in the host context.");
        }
    }

    private static void EnsureValidId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new AbpValidationException(new[]
            {
                new ValidationResult("A non-empty demo record identifier is required.", new[] { nameof(id) })
            });
        }
    }

    private static void EnsureInput(object? input)
    {
        if (input is null)
        {
            throw new AbpValidationException(new[]
            {
                new ValidationResult("A demo record request body is required.")
            });
        }
    }

    private static DemoRecordDto MapToDto(DemoRecord record)
    {
        return new DemoRecordDto
        {
            Id = record.Id,
            Name = record.Name,
            Description = record.Description
        };
    }
}
