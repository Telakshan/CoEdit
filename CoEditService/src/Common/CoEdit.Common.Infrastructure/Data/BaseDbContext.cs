using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CoEdit.Common.Infrastructure.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoEdit.Common.Infrastructure.Data;

public abstract class BaseDbContext : DbContext, IUnitOfWork
{
    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<OutboxDeadLetter> OutboxDeadLetters { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        InsertOutboxMessages();

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    [ExcludeFromCodeCoverage(Justification = "EF Core ChangeTracker outbox pattern; tested via integration tests with domain event publishing")]
    private void InsertOutboxMessages()
    {
        var entries = ChangeTracker.Entries<Entity>().ToList();
        // Console.WriteLine($"Found {entries.Count} entries tracking Entity");

        var domainEvents = entries
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                // Snapshot domain events before clearing; otherwise we return an emptied backing list.
                var domainEvents = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .ToList();

        var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage
        {
            Id = domainEvent.EventId,
            OccurredAt = domainEvent.OccurredOn,
            Type = domainEvent.GetType().Name,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            AssemblyQualifiedName = domainEvent.GetType().AssemblyQualifiedName
        }).ToList();

        OutboxMessages.AddRange(outboxMessages);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxDeadLetterConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}