using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoEdit.Common.Infrastructure.Data;

public abstract class BaseDbContext(DbContextOptions options, IPublisher publisher) : DbContext(options), IUnitOfWork
{
    private readonly IPublisher _publisher = publisher;

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        InsertOutboxMessages();

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    private void InsertOutboxMessages()
    {
        var entries = ChangeTracker.Entries<Entity>().ToList();
        //Serilog here $"Found {entries.Count} entries tracking Entity"
        
        var domainEvents = entries
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.DomainEvents;
                // Console.WriteLine($"Entity {entity.GetType().Name} has {domainEvents.Count} events");
                // Replace with Serilog here
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .ToList();

        var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage
        {
            Id = domainEvent.Id,
            OccurredAt = domainEvent.OccuredOnUtc,
            Type = domainEvent.GetType().Name,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), (JsonSerializerOptions?)null),
            AssemblyQualifiedName = domainEvent.GetType().AssemblyQualifiedName
        }).ToList();

        OutboxMessages.AddRange(outboxMessages);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}