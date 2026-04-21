using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Member A - Auth & Users
        public DbSet<User> Users { get; set; }
        public DbSet<EmailConfirmation> EmailConfirmations { get; set; }

        // Member B - Hotels & Rooms
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<Amenity> Amenities { get; set; }

        // Member C - Bookings, Payments, Promotions, Loyalty
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<LoyaltyReward> LoyaltyRewards { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //    OnModelCreatingPartial(modelBuilder);
        //}

        //partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}