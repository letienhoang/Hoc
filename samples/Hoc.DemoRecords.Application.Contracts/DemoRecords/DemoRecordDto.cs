using System;

namespace Hoc.DemoRecords.DemoRecords;

public class DemoRecordDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
