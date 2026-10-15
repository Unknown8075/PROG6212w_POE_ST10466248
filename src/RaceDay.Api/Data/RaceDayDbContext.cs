using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.DateCreated)
                .HasDefaultValueSql("GETDATE()");

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Users_Role",
                "[Role] IN ('Organiser', 'Participant')"));
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.Events)
                .HasForeignKey(e => e.OrganiserID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.EventDate)
                .HasColumnType("date");

            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("GETDATE()");

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Events_EventType",
                "[EventType] IN ('Run', 'Walk', 'Cycle')"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Events_Distance",
                "[Distance] > 0"));
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.CategoryName).IsUnique();
        });

        modelBuilder.Entity<EventCategory>(entity =>
        {
            entity.HasIndex(ec => new
            {
                ec.EventID,
                ec.CategoryID
            }).IsUnique();

            entity.HasOne(ec => ec.Event)
                .WithMany(e => e.EventCategories)
                .HasForeignKey(ec => ec.EventID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ec => ec.Category)
                .WithMany(c => c.EventCategories)
                .HasForeignKey(ec => ec.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_EventCategories_EntryFee",
                "[EntryFee] >= 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_EventCategories_MaxParticipants",
                "[MaximumParticipants] > 0"));
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.EventCategory)
                .WithMany(ec => ec.Enrolments)
                .HasForeignKey(e => e.EventCategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.EnrolmentDate)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => new
            {
                e.ParticipantID,
                e.EventCategoryID
            }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Enrolments_Status",
                "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(r => r.FinishTime)
                .HasColumnType("time");

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Results_Status",
                "[ResultStatus] IN ('Finished', 'DNF', 'DNS', 'DQ')"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Results_Position",
                "[Position] IS NULL OR [Position] > 0"));
        });
    }
}
