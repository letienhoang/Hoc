using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Hoc.DemoRecords.DemoRecords;

/// <summary>
/// A minimal DemoRecords aggregate used to prove module-owned persistence.
/// </summary>
public class DemoRecord : AggregateRoot<Guid>
{
    /// <summary>
    /// Maximum supported length for a record name.
    /// </summary>
    public const int MaxNameLength = 128;

    /// <summary>
    /// Maximum supported length for an optional record description.
    /// </summary>
    public const int MaxDescriptionLength = 1024;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    protected DemoRecord()
    {
    }

    public DemoRecord(Guid id, string name, string? description = null)
        : base(id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A demo record identifier is required.", nameof(id));
        }

        SetProperties(name, description);
    }

    public void Update(string name, string? description)
    {
        SetProperties(name, description);
    }

    private void SetProperties(string name, string? description)
    {
        var validatedName = Check.NotNullOrWhiteSpace(name, nameof(name), MaxNameLength);
        var validatedDescription = Check.Length(description, nameof(description), MaxDescriptionLength);

        Name = validatedName;
        Description = validatedDescription;
    }
}
