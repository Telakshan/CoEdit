using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoEdit.Common.Infrastructure.Outbox;

[ExcludeFromCodeCoverage(Justification = "EF Core entity type configuration")]
public class OutboxMessageConfiguration: IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SequenceNumber)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Content).IsRequired();

        // Supports polling query: WHERE ProcessedAt IS NULL AND RetryCount < N ORDER BY SequenceNumber LIMIT batch
        builder.HasIndex(x => new { x.RetryCount, x.SequenceNumber })
            .HasDatabaseName("IX_OutboxMessages_Polling")
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}