using ChatFlow.Models.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatFlow;

public class ApplicationContext: DbContext
{
    // Таблицы
    public DbSet<Persons> Persons { get; set; }
    public DbSet<DataPersons> DataPersons { get; set; }
    public DbSet<Sessions> Sessions { get; set; }
    public DbSet<Images> Images { get; set; }
    
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Persons>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<Persons>()
            .HasAlternateKey(p => p.PersonId);

        // Один к одному: Persons ↔ DataPersons (по строковому PersonId)
        modelBuilder.Entity<Persons>()
            .HasOne(p => p.DataPersons)
            .WithOne(dp => dp.Persons)
            .HasForeignKey<DataPersons>(dp => dp.PersonId)
            .HasPrincipalKey<Persons>(p => p.PersonId);

        // Один ко многим: DataPersons ↔ Images (по строковому PersonId)
        modelBuilder.Entity<DataPersons>()
            .HasMany(dp => dp.Images)
            .WithOne(img => img.DataPersons)
            .HasForeignKey(img => img.PersonId)
            .HasPrincipalKey(dp => dp.PersonId);
        
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