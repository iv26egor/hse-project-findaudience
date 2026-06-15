using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourseProject.Parser.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("first_name")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Column("last_name")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Column("middle_name")]
        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [Required]
        [Column("group_name")]
        [MaxLength(20)]
        public string Group { get; set; } = string.Empty;

        [Required]
        [Column("email")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        [MaxLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}