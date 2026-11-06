using System;
using Hoc.DemoRecords.DemoRecords;
using Shouldly;
using Xunit;

namespace Hoc.DemoRecords;

public class DemoRecordTests
{
    [Fact]
    public void Constructor_should_set_valid_values()
    {
        var id = Guid.NewGuid();

        var record = new DemoRecord(id, "Initial record", "Initial description");

        record.Id.ShouldBe(id);
        record.Name.ShouldBe("Initial record");
        record.Description.ShouldBe("Initial description");
    }

    [Fact]
    public void Update_should_replace_both_mutable_values_without_changing_identity()
    {
        var id = Guid.NewGuid();
        var record = new DemoRecord(id, "Initial record", "Initial description");

        record.Update("Updated record", null);

        record.Id.ShouldBe(id);
        record.Name.ShouldBe("Updated record");
        record.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_should_reject_blank_names(string name)
    {
        Should.Throw<ArgumentException>(() => new DemoRecord(Guid.NewGuid(), name));
    }

    [Fact]
    public void Constructor_should_reject_an_empty_identifier()
    {
        Should.Throw<ArgumentException>(() => new DemoRecord(Guid.Empty, "Valid name"));
    }

    [Fact]
    public void Constructor_should_accept_boundary_lengths()
    {
        var name = new string('n', DemoRecord.MaxNameLength);
        var description = new string('d', DemoRecord.MaxDescriptionLength);

        var record = new DemoRecord(Guid.NewGuid(), name, description);

        record.Name.ShouldBe(name);
        record.Description.ShouldBe(description);
    }

    [Fact]
    public void Update_should_reject_values_longer_than_the_documented_limits_without_partial_update()
    {
        var record = new DemoRecord(Guid.NewGuid(), "Initial record", "Initial description");
        var tooLongName = new string('n', DemoRecord.MaxNameLength + 1);
        var tooLongDescription = new string('d', DemoRecord.MaxDescriptionLength + 1);

        Should.Throw<ArgumentException>(() => record.Update(tooLongName, "Changed description"));
        Should.Throw<ArgumentException>(() => record.Update("Changed name", tooLongDescription));

        record.Name.ShouldBe("Initial record");
        record.Description.ShouldBe("Initial description");
    }
}
