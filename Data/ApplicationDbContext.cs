using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Data
{
    /// <summary>
    /// Primary DbContext. Member A owns this file and the base configuration.
    /// Members B and C add their DbSets and OnModelCreating sections below the
    /// clearly marked boundaries — do NOT move or delete A's section.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── Member A ──────────────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<EmailConfirmation> EmailConfirmations { get; set; }

        // ── Member B (add your DbSets here) ──────────────────────────────────
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<Amenity> Amenities { get; set; }

        // ── Member C (add your DbSets here) ──────────────────────────────────
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<LoyaltyReward> LoyaltyRewards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ════════════════════════════════════════════════════════════════
            //  MEMBER A — User & EmailConfirmation
            // ════════════════════════════════════════════════════════════════

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(u => u.PasswordHash)
                      .IsRequired();

                entity.Property(u => u.FirstName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.LastName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.PhoneNumber)
                      .HasMaxLength(20);

                entity.Property(u => u.Role)
                      .IsRequired()
                      .HasDefaultValue("Customer");

                entity.Property(u => u.LoyaltyTier)
                      .HasDefaultValue("Bronze");

                entity.Property(u => u.LoyaltyPoints)
                      .HasDefaultValue(0);

                entity.Property(u => u.IsActive)
                      .HasDefaultValue(true);

                entity.Property(u => u.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<EmailConfirmation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ConfirmationNumber)
                      .IsUnique();

                entity.Property(e => e.RecipientEmail)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.ConfirmationNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.EmailType)
                      .HasDefaultValue("BookingConfirmation");

                entity.Property(e => e.SentAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // EmailConfirmation → User (optional; welcome emails before a booking)
                entity.HasOne(e => e.User)
                      .WithMany(u => u.EmailConfirmations)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // EmailConfirmation → Booking (optional for password-reset emails)
                entity.HasOne(e => e.Booking)
                      .WithMany(b => b.EmailConfirmations)
                      .HasForeignKey(e => e.BookingId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ════════════════════════════════════════════════════════════════
            //  MEMBER B — Hotel, Room, RoomCategory, Amenity
            //  (B: add your detailed Fluent API config here)
            // ════════════════════════════════════════════════════════════════

            modelBuilder.Entity<Hotel>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.Name).IsRequired().HasMaxLength(200);
                entity.Property(h => h.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(h => h.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Many-to-many Hotel ↔ Amenity
                entity.HasMany(h => h.Amenities)
                      .WithMany(a => a.Hotels)
                      .UsingEntity(j => j.ToTable("HotelAmenities"));
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.BasePrice).HasColumnType("decimal(18,2)");
                entity.Property(r => r.Status).HasDefaultValue("Available");

                entity.HasOne(r => r.Hotel)
                      .WithMany(h => h.Rooms)
                      .HasForeignKey(r => r.HotelId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.RoomCategory)
                      .WithMany(rc => rc.Rooms)
                      .HasForeignKey(r => r.RoomCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RoomCategory>(entity =>
            {
                entity.HasKey(rc => rc.Id);
                entity.Property(rc => rc.Name).IsRequired().HasMaxLength(100);
                entity.Property(rc => rc.PriceMultiplier).HasColumnType("decimal(5,2)");
            });

            modelBuilder.Entity<Amenity>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
                entity.Property(a => a.AdditionalCost).HasColumnType("decimal(18,2)");
            });

            // ════════════════════════════════════════════════════════════════
            //  MEMBER C — Booking, Payment, Promotion, LoyaltyReward
            //  (C: add your detailed Fluent API config here)
            // ════════════════════════════════════════════════════════════════

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.HasIndex(b => b.BookingReference).IsUnique();

                entity.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(b => b.FinalPrice).HasColumnType("decimal(18,2)");
                entity.Property(b => b.Status).HasDefaultValue("Pending");
                entity.Property(b => b.BookingDate).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(b => b.User)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Room)
                      .WithMany(r => r.Bookings)
                      .HasForeignKey(b => b.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Promotion)
                      .WithMany(p => p.Bookings)
                      .HasForeignKey(b => b.PromotionId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.TransactionId).IsUnique();
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.Property(p => p.PaymentStatus).HasDefaultValue("Pending");
                entity.Property(p => p.PaymentDate).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(p => p.Booking)
                      .WithMany(b => b.Payments)
                      .HasForeignKey(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.Code).IsUnique();
                entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
                entity.Property(p => p.DiscountPercentage).HasColumnType("decimal(5,2)");
                entity.Property(p => p.MaxDiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(p => p.MinimumBookingAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<LoyaltyReward>(entity =>
            {
                entity.HasKey(lr => lr.Id);
                entity.Property(lr => lr.DiscountRate).HasColumnType("decimal(5,2)");
                entity.Property(lr => lr.CashbackPercentage).HasColumnType("decimal(5,2)");
            });

            // ── Seed: Loyalty tiers ───────────────────────────────────────
            modelBuilder.Entity<LoyaltyReward>().HasData(
                new LoyaltyReward { Id = 1, TierName = "Bronze", MinimumPoints = 0, DiscountRate = 0, PointsPerDollar = 1, Benefits = "Basic rewards", FreeUpgradeAfterBookings = 0, CashbackPercentage = 0 },
                new LoyaltyReward { Id = 2, TierName = "Silver", MinimumPoints = 1000, DiscountRate = 5, PointsPerDollar = 2, Benefits = "5% discount, priority check-in", FreeUpgradeAfterBookings = 5, CashbackPercentage = 1 },
                new LoyaltyReward { Id = 3, TierName = "Gold", MinimumPoints = 5000, DiscountRate = 10, PointsPerDollar = 3, Benefits = "10% discount, free breakfast", FreeUpgradeAfterBookings = 3, CashbackPercentage = 2 },
                new LoyaltyReward { Id = 4, TierName = "Platinum", MinimumPoints = 15000, DiscountRate = 20, PointsPerDollar = 5, Benefits = "20% discount, airport transfer, lounge", FreeUpgradeAfterBookings = 1, CashbackPercentage = 5 }
            );
        }
    }
}