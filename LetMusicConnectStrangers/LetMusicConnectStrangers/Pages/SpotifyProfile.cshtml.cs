using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LetMusicConnectStrangers.Models;
using LetMusicConnectStrangers.Services;
using SpotifyAPI.Web;
using System.Collections.Generic;

namespace LetMusicConnectStrangers.Pages
{
    [Authorize]
    public class SpotifyProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SpotifyService _spotifyService;

        public SpotifyProfileModel(UserManager<ApplicationUser> userManager, SpotifyService spotifyService)
        {
            _userManager = userManager;
            _spotifyService = spotifyService;
        }

        public string? SpotifyDisplayName { get; set; }
        public string? SpotifyId { get; set; }
        public bool HasSpotifyConnection { get; set; }
        public List<FullTrack> TopTracks { get; set; } = new();
        public List<FullArtist> TopArtists { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            SpotifyDisplayName = user.SpotifyDisplayName;
            SpotifyId = user.SpotifyId;
            HasSpotifyConnection = _spotifyService.HasValidSpotifyConnection(user);

            if (!HasSpotifyConnection)
            {
                ErrorMessage = "You haven't connected your Spotify account or your connection has expired.";
                return Page();
            }

            try
            {
                // Get user's top tracks
                TopTracks = await _spotifyService.GetUserTopTracks(user, 10);

                // Get user's top artists
                TopArtists = await _spotifyService.GetUserTopArtists(user, 10);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error fetching Spotify data: {ex.Message}";
            }

            return Page();
        }
    }
}
