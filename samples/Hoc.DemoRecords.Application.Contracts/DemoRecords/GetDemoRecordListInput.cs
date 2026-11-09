using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Hoc.DemoRecords.DemoRecords;

public class GetDemoRecordListInput : IPagedResultRequest
{
    public const int DefaultMaxResultCount = 10;
    public const int MaxMaxResultCount = 100;

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, MaxMaxResultCount)]
    public int MaxResultCount { get; set; } = DefaultMaxResultCount;
}
