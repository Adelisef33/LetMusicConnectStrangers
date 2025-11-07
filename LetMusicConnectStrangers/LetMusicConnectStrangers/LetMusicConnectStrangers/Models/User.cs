using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMusicConnectStrangers.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string SpotifyToken { get; set; }

        // Optional collection of reviews
        public ICollection<Review>? Reviews { get; set; }

        // Optional collection of preferences
        public ICollection<Preference>? Preferences { get; set; }
    }
}