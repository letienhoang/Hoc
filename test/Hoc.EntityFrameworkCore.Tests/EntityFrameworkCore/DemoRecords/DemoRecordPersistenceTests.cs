using System;
using System.Threading.Tasks;
using Hoc.DemoRecords.DemoRecords;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Hoc.EntityFrameworkCore.DemoRecords;

[Collection(HocTestConsts.CollectionDefinitionName)]
public class DemoRecordPersistenceTests : HocEntityFrameworkCoreTestBase
{
    private readonly HocDbContext _dbContext;
    private readonly IRepository<DemoRecord, Guid> _repository;

    public DemoRecordPersistenceTests()
    {
        _dbContext = GetRequiredService<HocDbContext>();
        _repository = GetRequiredService<IRepository<DemoRecord, Guid>>();
    }

    [Fact]
    public void Model_should_register_the_module_owned_mapping()
    {
        var entityType = _dbContext.Model.FindEntityType(typeof(DemoRecord));

        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("DemoRecords");
        entityType.FindProperty(nameof(DemoRecord.Name))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(DemoRecord.Name))!.GetMaxLength().ShouldBe(DemoRecord.MaxNameLength);
        entityType.FindProperty(nameof(DemoRecord.Description))!.IsNullable.ShouldBeTrue();
        entityType.FindProperty(nameof(DemoRecord.Description))!.GetMaxLength().ShouldBe(DemoRecord.MaxDescriptionLength);
        entityType.FindIndex(entityType.FindProperty(nameof(DemoRecord.Name))!).ShouldNotBeNull();
    }

    [Fact]
    public async Task Repository_should_round_trip_a_demo_record_through_disposable_sqlite()
    {
        var record = new DemoRecord(Guid.NewGuid(), "Persisted record", "Persisted description");

        await WithUnitOfWorkAsync(async () =>
        {
            await _repository.InsertAsync(record);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var persistedRecord = await _repository.GetAsync(record.Id);

            persistedRecord.Name.ShouldBe("Persisted record");
            persistedRecord.Description.ShouldBe("Persisted description");
        });
    }
}
