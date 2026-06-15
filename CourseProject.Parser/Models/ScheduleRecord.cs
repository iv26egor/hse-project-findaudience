using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourseProject.Parser.Models
{
    [Table("schedule_records")]
    public class ScheduleRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("date", TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        [Column("pair_number")]
        public int PairNumber { get; set; }

        [Required]
        [Column("cabinet")]
        public string Cabinet { get; set; }

        [Required]
        [Column("building")]
        public string Building { get; set; }
    }
}