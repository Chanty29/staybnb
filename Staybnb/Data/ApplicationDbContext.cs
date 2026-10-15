using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data;

/// <summary>
/// The EF Core database context. It holds the Identity tables (users, roles, user-roles)
/// plus every Staybnb table from the entity diagram.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ----- Tables -----
    public DbSet<HostProperty> HostProperties => Set<HostProperty>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<HostApplication> HostApplications => Set<HostApplication>();
    public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
    public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
    public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // NOTE: SQL Server does not allow two "cascade delete" paths to the same table,
        // so anything that points at ApplicationUser from more than one place uses
        // DeleteBehavior.Restrict (it blocks deleting a user who still has data).

        // ApplicationUser 1 -> many HostProperty
        builder.Entity<HostProperty>(e =>
        {
            e.HasOne(p => p.Host)
                .WithMany(u => u.HostedProperties)
                .HasForeignKey(p => p.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            // HostProperty many <-> many Amenity (EF creates the join table for us)
            e.HasMany(p => p.Amenities)
                .WithMany(a => a.Properties);

            // The gallery filters by city and type, so index them.
            e.HasIndex(p => p.City);
            e.HasIndex(p => p.PropertyType);
        });

        // HostProperty 1 -> many PropertyImage
        builder.Entity<PropertyImage>(e =>
        {
            e.HasOne(i => i.Property)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Booking: many -> 1 HostProperty, many -> 1 ApplicationUser (the guest)
        builder.Entity<Booking>(e =>
        {
            e.HasOne(b => b.Property)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Guest)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking 1 -> 1 Payment
            e.HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking 1 -> 1 GuestCheckIn
            e.HasOne(b => b.GuestCheckIn)
                .WithOne(g => g.Booking)
                .HasForeignKey<GuestCheckIn>(g => g.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Review: many -> 1 HostProperty, many -> 1 ApplicationUser (the reviewer)
        builder.Entity<Review>(e =>
        {
            e.HasOne(r => r.Property)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Reviewer)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WishlistItem: a user can only save a property once
        builder.Entity<WishlistItem>(e =>
        {
            e.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(w => w.Property)
                .WithMany()
                .HasForeignKey(w => w.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(w => new { w.UserId, w.PropertyId }).IsUnique();
        });

        // Message has TWO links to ApplicationUser (sender and receiver)
        builder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // The inbox loads "my messages, newest first".
            e.HasIndex(m => new { m.ReceiverId, m.Timestamp });
        });

        // ActivityLog: the user is optional (system events), keep the log if a user is removed
        builder.Entity<ActivityLog>(e =>
        {
            e.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(l => l.CreatedAt);
        });

        // Notification: deleted together with the user
        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(n => new { n.UserId, n.IsRead });
        });

        // HostApplication: points at the applicant and at the property they submitted
        builder.Entity<HostApplication>(e =>
        {
            e.HasOne(a => a.ApplicationUser)
                .WithMany()
                .HasForeignKey(a => a.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Property)
                .WithMany(p => p.Applications)
                .HasForeignKey(a => a.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HostProperty 1 -> 1 CheckInProcess
        builder.Entity<CheckInProcess>(e =>
        {
            e.HasOne(c => c.Property)
                .WithOne(p => p.CheckInProcess)
                .HasForeignKey<CheckInProcess>(c => c.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GuestCheckIn many -> 1 CheckInProcess (a process can be used by many bookings)
        builder.Entity<GuestCheckIn>(e =>
        {
            e.HasOne(g => g.CheckInProcess)
                .WithMany(c => c.GuestCheckIns)
                .HasForeignKey(g => g.CheckInProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // GuestCheckIn 1 -> many GuestDocument
        builder.Entity<GuestDocument>(e =>
        {
            e.HasOne(d => d.GuestCheckIn)
                .WithMany(g => g.Documents)
                .HasForeignKey(d => d.GuestCheckInId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
