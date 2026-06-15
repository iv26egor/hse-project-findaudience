using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourseProject.Parser.Models
{
    [Table("bookings")]
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("auditorium_id")]
        public int AuditoriumId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("booking_date")]
        public DateTime BookingDate { get; set; }

        [Required]
        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AuditoriumId")]
        public virtual Auditorium? Auditorium { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}