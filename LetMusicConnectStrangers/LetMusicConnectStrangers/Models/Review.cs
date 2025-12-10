using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMusicConnectStrangers.Models
{
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        // Spotify track information
        [Required]
        public string SpotifyTrackId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TrackName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ArtistName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AlbumName { get; set; }

        [StringLength(500)]
        public string? AlbumImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
