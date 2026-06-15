using Microsoft.EntityFrameworkCore;
using CourseProject.Parser.Models;

namespace CourseProject.Parser.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<ScheduleRecord> ScheduleRecords { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Auditorium> Auditoriums { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // schedule_records
            modelBuilder.Entity<ScheduleRecord>(entity =>
            {
                entity.ToTable("schedule_records");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Date).HasColumnName("date");
                entity.Property(e => e.PairNumber).HasColumnName("pair_number");
                entity.Property(e => e.Cabinet).HasColumnName("cabinet");
                entity.Property(e => e.Building).HasColumnName("building");
            });

            // users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.MiddleName).HasColumnName("middle_name");
                entity.Property(e => e.Group).HasColumnName("group_name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            // auditoriums
            modelBuilder.Entity<Auditorium>(entity =>
            {
                entity.ToTable("auditoriums");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Building).HasColumnName("building");
                entity.Property(e => e.RoomNumber).HasColumnName("room_number");
                entity.Property(e => e.Capacity).HasColumnName("capacity");
                entity.Property(e => e.HasComputers).HasColumnName("has_computers");
                entity.Property(e => e.HasProjector).HasColumnName("has_projector");
            });

            // bookings
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("bookings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AuditoriumId).HasColumnName("auditorium_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.BookingDate).HasColumnName("booking_date");
                entity.Property(e => e.StartTime).HasColumnName("start_time");
                entity.Property(e => e.EndTime).HasColumnName("end_time");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });
        }
    }
}