using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StayBnb.Models;

namespace StayBnb.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
        public DbSet<HostProperty> HostProperties { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Amenity> Amenities { get; set; } = null!;
        public DbSet<PropertyImage> PropertyImages { get; set; } = null!;
        public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<HostApplication> HostApplications { get; set; } = null!;
        public DbSet<CheckInProcess> CheckInProcesses { get; set; } = null!;
        public DbSet<GuestCheckIn> GuestCheckIns { get; set; } = null!;
        public DbSet<GuestDocument> GuestDocuments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // USER PROFILE AND IDENTITY ACCOUNT
            builder.Entity<ApplicationUser>()
                .ToTable("ApplicationUsers");

            builder.Entity<ApplicationUser>()
                .Property(user => user.Id)
                .ValueGeneratedNever();

            builder.Entity<ApplicationUser>()
                .HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<ApplicationUser>(user => user.Id)
                .OnDelete(DeleteBehavior.Restrict);

            // PROPERTY OWNER
            builder.Entity<HostProperty>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(property => property.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            // BOOKINGS
            builder.Entity<Booking>()
                .HasOne<HostProperty>()
                .WithMany()
                .HasForeignKey(booking => booking.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(booking => booking.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            // ONE PAYMENT RECORD PER BOOKING
            builder.Entity<Payment>()
                .HasOne<Booking>()
                .WithOne()
                .HasForeignKey<Payment>(payment => payment.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // REVIEWS
            builder.Entity<Review>()
                .HasOne<HostProperty>()
                .WithMany()
                .HasForeignKey(review => review.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(review => review.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // PROPERTY IMAGES
            builder.Entity<PropertyImage>()
                .HasOne<HostProperty>()
                .WithMany()
                .HasForeignKey(image => image.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // WISHLIST ITEMS
            builder.Entity<WishlistItem>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WishlistItem>()
                .HasOne<HostProperty>()
                .WithMany()
                .HasForeignKey(item => item.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent saving the same property twice for one user.
            builder.Entity<WishlistItem>()
                .HasIndex(item => new { item.UserId, item.PropertyId })
                .IsUnique();

            // MESSAGE SENDER AND RECEIVER
            builder.Entity<Message>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(message => message.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // ACTIVITY LOGS
            builder.Entity<ActivityLog>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // NOTIFICATIONS
            builder.Entity<Notification>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // HOST APPLICATIONS
            builder.Entity<HostApplication>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(application => application.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<HostApplication>()
                .HasOne<HostProperty>()
                .WithMany()
                .HasForeignKey(application => application.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // ONE CHECK-IN PROCESS PER PROPERTY
            builder.Entity<CheckInProcess>()
                .HasOne<HostProperty>()
                .WithOne()
                .HasForeignKey<CheckInProcess>(process => process.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // ONE GUEST CHECK-IN RECORD PER BOOKING
            builder.Entity<GuestCheckIn>()
                .HasOne<Booking>()
                .WithOne()
                .HasForeignKey<GuestCheckIn>(checkIn => checkIn.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // MANY GUEST CHECK-INS CAN USE THE SAME PROPERTY PROCESS
            builder.Entity<GuestCheckIn>()
                .HasOne<CheckInProcess>()
                .WithMany()
                .HasForeignKey(checkIn => checkIn.CheckInProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            // CHECK-IN DOCUMENTS
            builder.Entity<GuestDocument>()
                .HasOne<GuestCheckIn>()
                .WithMany()
                .HasForeignKey(document => document.GuestCheckInId)
                .OnDelete(DeleteBehavior.Restrict);

            // PROPERTY-AMENITY LINKING TABLE
            builder.SharedTypeEntity<Dictionary<string, object>>(
                "PropertyAmenities",
                join =>
                {
                    join.ToTable("PropertyAmenities");

                    join.IndexerProperty<int>("PropertyId");
                    join.IndexerProperty<int>("AmenityId");

                    join.HasKey("PropertyId", "AmenityId");

                    join.HasOne<HostProperty>()
                        .WithMany()
                        .HasForeignKey("PropertyId")
                        .OnDelete(DeleteBehavior.Restrict);

                    join.HasOne<Amenity>()
                        .WithMany()
                        .HasForeignKey("AmenityId")
                        .OnDelete(DeleteBehavior.Restrict);
                });
        }
    }
}