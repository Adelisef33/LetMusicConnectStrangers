using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMusicConnectStrangers.Models
{
    public class Recommendation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RecommendationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int TrackId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
