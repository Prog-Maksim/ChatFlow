using API.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace API;

public class ApplicationContext: DbContext
{
    public DbSet<Person> person { get; set; } = null!;
    public DbSet<Session> session { get; set; } = null!;
    public DbSet<Chat> chat { get; set; } = null!;
    public DbSet<PersonChat> personChat { get; set; } = null!;
    
    public ApplicationContext(DbContextOptions<ApplicationContext> options): base(options)
    {
        
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PersonChat>()
            .HasOne(pc => pc.Chat)           // Один PersonChat связан с одним Chat
            .WithMany(c => c.Users)    // Один Chat может иметь много PersonChat
            .HasForeignKey(pc => pc.ChatId)  // Внешний ключ — поле ChatId
            .HasPrincipalKey(c => c.ChatId) // Ссылается на ключ Chat.ChatId
            .OnDelete(DeleteBehavior.Cascade); // Указываем, что при удалении Chat удаляются все связанные PersonChat

        base.OnModelCreating(modelBuilder);
    }
}