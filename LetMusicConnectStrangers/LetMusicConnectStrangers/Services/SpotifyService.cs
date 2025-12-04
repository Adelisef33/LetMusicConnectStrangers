using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SpotifyAPI.Web;
using LetMusicConnectStrangers.Models;

namespace LetMusicConnectStrangers.Services
{
    public class SpotifyService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public SpotifyService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Get a configured SpotifyClient for a user
        /// </summary>
        public async Task<SpotifyClient?> GetSpotifyClientForUser(ApplicationUser user)
        {
            if (string.IsNullOrEmpty(user.SpotifyAccessToken))
            {
                return null;
            }

            // Check if token is expired
            if (user.SpotifyTokenExpiration.HasValue && user.SpotifyTokenExpiration.Value <= DateTime.UtcNow)
            {
                // Token is expired - would need to implement refresh logic here
                return null;
            }

            var config = SpotifyClientConfig
                .CreateDefault()
                .WithToken(user.SpotifyAccessToken);

            return new SpotifyClient(config);
        }

        /// <summary>
        /// Get user's top tracks from Spotify
        /// </summary>
        public async Task<List<FullTrack>> GetUserTopTracks(ApplicationUser user, int limit = 20)
        {
            var spotify = await GetSpotifyClientForUser(user);
            if (spotify == null) return new List<FullTrack>();

            var request = new PersonalizationTopRequest
            {
                TimeRangeParam = PersonalizationTopRequest.TimeRange.MediumTerm,
                Limit = limit
            };

            var response = await spotify.Personalization.GetTopTracks(request);
            return response?.Items ?? new List<FullTrack>();
        }

        /// <summary>
        /// Get user's top artists from Spotify
        /// </summary>
        public async Task<List<FullArtist>> GetUserTopArtists(ApplicationUser user, int limit = 20)
        {
            var spotify = await GetSpotifyClientForUser(user);
            if (spotify == null) return new List<FullArtist>();

            var request = new PersonalizationTopRequest
            {
                TimeRangeParam = PersonalizationTopRequest.TimeRange.MediumTerm,
                Limit = limit
            };

            var response = await spotify.Personalization.GetTopArtists(request);
            return response?.Items ?? new List<FullArtist>();
        }

        /// <summary>
        /// Get user's recently played tracks
        /// </summary>
        public async Task<List<PlayHistoryItem>> GetRecentlyPlayed(ApplicationUser user, int limit = 20)
        {
            var spotify = await GetSpotifyClientForUser(user);
            if (spotify == null) return new List<PlayHistoryItem>();

            var request = new PlayerRecentlyPlayedRequest
            {
                Limit = limit
            };

            var response = await spotify.Player.GetRecentlyPlayed(request);
            return response?.Items ?? new List<PlayHistoryItem>();
        }

        /// <summary>
        /// Get the current user's Spotify profile
        /// </summary>
        public async Task<PrivateUser?> GetCurrentUserProfile(ApplicationUser user)
        {
            var spotify = await GetSpotifyClientForUser(user);
            if (spotify == null) return null;

            return await spotify.UserProfile.Current();
        }

        /// <summary>
        /// Check if user has a valid Spotify connection
        /// </summary>
        public bool HasValidSpotifyConnection(ApplicationUser user)
        {
            return !string.IsNullOrEmpty(user.SpotifyAccessToken) &&
                   user.SpotifyTokenExpiration.HasValue &&
                   user.SpotifyTokenExpiration.Value > DateTime.UtcNow;
        }
    }
}
