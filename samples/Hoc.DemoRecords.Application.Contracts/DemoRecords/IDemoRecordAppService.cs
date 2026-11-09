using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Hoc.DemoRecords.DemoRecords;

public interface IDemoRecordAppService : IApplicationService
{
    Task<DemoRecordDto> GetAsync(Guid id);

    Task<PagedResultDto<DemoRecordDto>> GetListAsync(GetDemoRecordListInput input);

    Task<DemoRecordDto> CreateAsync(CreateDemoRecordDto input);

    Task<DemoRecordDto> UpdateAsync(Guid id, UpdateDemoRecordDto input);

    Task DeleteAsync(Guid id);
}
