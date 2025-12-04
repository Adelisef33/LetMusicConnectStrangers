using Microsoft.AspNetCore.Identity;

namespace LetMusicConnectStrangers.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? SpotifyId { get; set; }
        public string? SpotifyAccessToken { get; set; }
        public string? SpotifyRefreshToken { get; set; }
        public DateTime? SpotifyTokenExpiration { get; set; }
        public string? SpotifyDisplayName { get; set; }
    }
}
