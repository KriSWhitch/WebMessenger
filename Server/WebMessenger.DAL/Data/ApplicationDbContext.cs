using Microsoft.EntityFrameworkCore;
using WebMessenger.DAL.Entities;

namespace WebMessenger.DAL.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
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
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(m => m.SentAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAdd();

                entity.Property(m => m.EditedAt)
                    .HasColumnType("datetime(6)");

                // Composite index for efficient chat history queries
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

                // Composite unique index — a user can only be a member of a chat once
                entity.HasIndex(cm => new { cm.UserId, cm.ChatId }).IsUnique();
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.Property(c => c.AddedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql(currentTimestamp)
                    .ValueGeneratedOnAdd();

                // Composite unique index — a user can only add another user as a contact once
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
        }
    }
}