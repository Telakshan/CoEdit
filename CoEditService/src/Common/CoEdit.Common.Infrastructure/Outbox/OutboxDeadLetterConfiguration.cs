using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoEdit.Common.Infrastructure.Outbox;

[ExcludeFromCodeCoverage(Justification = "EF Core entity type configuration")]
public class OutboxDeadLetterConfiguration: IEntityTypeConfiguration<OutboxDeadLetter>
{
    public void Configure(EntityTypeBuilder<OutboxDeadLetter> builder)
    {
        builder.ToTable("OutboxDeadLetters");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.SourceSchema).IsRequired().HasMaxLength(128);

        builder.HasIndex(x => new { x.SourceSchema, x.QuarantinedAt })
            .HasDatabaseName("IX_OutboxDeadLetters_Schema_Quarantined");
    }
}