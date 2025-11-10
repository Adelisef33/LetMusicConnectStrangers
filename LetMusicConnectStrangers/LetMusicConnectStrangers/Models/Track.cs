using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMusicConnectStrangers.Models
{
    public class Track
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TrackId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Artist { get; set; } = string.Empty;

        [Required]
        public string Album { get; set; } = string.Empty;

        [Required]
        public string Genre { get; set; } = string.Empty;

        [Required]
        public TimeSpan Duration { get; set; }

        [Required]
        public string SpotifyId { get; set; } = string.Empty;

        public string? Reviews { get; set; }
    }
}
