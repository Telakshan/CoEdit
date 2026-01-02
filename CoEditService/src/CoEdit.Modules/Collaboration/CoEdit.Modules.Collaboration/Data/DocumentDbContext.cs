using CoEdit.Modules.Collaboration.Models;
using CoEdit.Modules.History;
using Microsoft.EntityFrameworkCore;

namespace CoEdit.Modules.Collaboration.Data;

public class DocumentDbContext: DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options)
    {
    }
    
    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}