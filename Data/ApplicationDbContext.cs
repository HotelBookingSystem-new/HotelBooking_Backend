using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    modelBuilder.Entity<Room>()
        //        .HasOne(r => r.Hotel)
        //        .WithMany(h => h.Rooms)
        //        .HasForeignKey(r => r.HotelId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    modelBuilder.Entity<Room>()
        //        .HasOne(r => r.RoomCategory)
        //        .WithMany(rc => rc.Rooms)
        //        .HasForeignKey(r => r.RoomCategoryId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<Hotel>()
        //        .HasMany(h => h.Amenities)
        //        .WithMany(a => a.Hotels)
        //        .UsingEntity(j => j.ToTable("HotelAmenities"));
        //}
    }
}