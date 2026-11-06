using Hoc.DemoRecords.DemoRecords;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hoc.DemoRecords.EntityFrameworkCore;

public static class DemoRecordsDbContextModelCreatingExtensions
{
    public const string TableName = "DemoRecords";

    public static void ConfigureDemoRecords(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<DemoRecord>(entity =>
        {
            entity.ToTable(TableName);

            entity.ConfigureByConvention();

            entity.Property(record => record.Name)
                .IsRequired()
                .HasMaxLength(DemoRecord.MaxNameLength);

            entity.Property(record => record.Description)
                .HasMaxLength(DemoRecord.MaxDescriptionLength);

            entity.HasIndex(record => record.Name);
        });
    }
}
