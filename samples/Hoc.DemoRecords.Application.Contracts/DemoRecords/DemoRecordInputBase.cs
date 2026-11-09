using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hoc.DemoRecords.DemoRecords;

public abstract class DemoRecordInputBase : IValidatableObject
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Name is required and cannot consist only of whitespace.",
                new[] { nameof(Name) });
        }
    }
}
