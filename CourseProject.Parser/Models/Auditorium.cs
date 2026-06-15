using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourseProject.Parser.Models
{
    [Table("auditoriums")]
    public class Auditorium
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("building")]
        [MaxLength(10)]
        public string Building { get; set; } = string.Empty;

        [Required]
        [Column("room_number")]
        [MaxLength(10)]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        [Column("capacity")]
        public int Capacity { get; set; }

        [Column("has_computers")]
        public bool HasComputers { get; set; } = false;

        [Column("has_projector")]
        public bool HasProjector { get; set; } = false;
    }
}