using ProfileService.Models.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProfileService;

public class ApplicationContext: DbContext
{
    // Таблицы
    public DbSet<Persons> Persons { get; set; }
    public DbSet<Images> Images { get; set; }
    
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Persons>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<Persons>()
            .HasAlternateKey(p => p.PersonId);

        modelBuilder.Entity<Images>()
            .HasOne(i => i.Person)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PersonId)
            .HasPrincipalKey(p => p.PersonId);
        
        modelBuilder.Entity<Persons>()
            .HasIndex(p => p.PersonId)
            .IsUnique();
    }
}

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new ApplicationContext(optionsBuilder.Options);
    }
}