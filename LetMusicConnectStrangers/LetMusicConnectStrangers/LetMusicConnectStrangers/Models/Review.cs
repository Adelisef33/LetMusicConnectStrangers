using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMusicConnectStrangers.Models
{
    public class Review
    {
     [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewId { get; set; }

        
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

      [ForeignKey("User")]
        public int UserId { get; set; }
  public required User User { get; set; }
    }
}