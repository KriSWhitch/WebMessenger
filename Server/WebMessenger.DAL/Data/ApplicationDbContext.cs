using Microsoft.EntityFrameworkCore;
using WebMessenger.DAL.Entities;

namespace WebMessenger.DAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatMember> ChatMembers { get; set; }
        public DbSet<Contact> Contacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var currentTimestamp = "CURRENT_TIMESTAMP(6)";

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();

                entity.Property(u => u.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAdd();

                entity.Property(u => u.LastSeenAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(u => u.LastLoginAt)
                    .HasColumnType("datetime(6)");
            });

            modelBuilder.Entity<Chat>(entity =>
            {
                entity.Property(c => c.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAdd();

                entity.Property(c => c.Name)
                    .HasMaxLength(100);

                entity.Property(c => c.AvatarUrl)
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(m => m.Content)
                    .IsRequired()
                    .HasMaxLength(5000);

                entity.Property(m => m.SentAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAdd();

                entity.Property(m => m.EditedAt)
                    .HasColumnType("datetime(6)");

                entity.HasIndex(m => new { m.ChatId, m.SentAt });
            });

            modelBuilder.Entity<ChatMember>(entity =>
            {
                entity.Property(cm => cm.JoinedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAdd();

                entity.Property(cm => cm.LastReadAt)
                    .HasColumnType("datetime(6)");

                entity.HasIndex(cm => new { cm.UserId, cm.ChatId }).IsUnique();
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.Property(c => c.AddedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAdd();

                entity.Property(c => c.Nickname)
                    .HasMaxLength(50);

                entity.HasIndex(c => new { c.OwnerUserId, c.ContactUserId }).IsUnique();

                entity.HasOne(c => c.OwnerUser)
                    .WithMany(u => u.Contacts)
                    .HasForeignKey(c => c.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.ContactUser)
                    .WithMany()
                    .HasForeignKey(c => c.ContactUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Chat>()
                .HasIndex(c => c.IsGroup);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.IsOnline);
        }
    }
}