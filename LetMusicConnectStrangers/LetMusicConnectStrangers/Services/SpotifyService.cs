using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;
using LetMusicConnectStrangers.Models;

namespace LetMusicConnectStrangers.Services
{
    public class SpotifyService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public SpotifyService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<SpotifyClient?> GetSpotifyClientForUser(ApplicationUser user)
        {
            Console.WriteLine($"GetSpotifyClientForUser called");
            Console.WriteLine($"  Access Token: {(string.IsNullOrEmpty(user.SpotifyAccessToken) ? "MISSING" : "EXISTS")}");
            Console.WriteLine($"  Refresh Token: {(string.IsNullOrEmpty(user.SpotifyRefreshToken) ? "MISSING" : "EXISTS")}");
            Console.WriteLine($"  Token Expiration: {user.SpotifyTokenExpiration}");
            Console.WriteLine($"  Current UTC Time: {DateTime.UtcNow}");

            if (string.IsNullOrEmpty(user.SpotifyAccessToken))
            {
                Console.WriteLine("  -> Returning null: No access token");
                return null;
            }

            // Check if token is expired or about to expire (within 5 minutes)
            if (user.SpotifyTokenExpiration.HasValue && 
                user.SpotifyTokenExpiration.Value <= DateTime.UtcNow.AddMinutes(5))
            {
                Console.WriteLine("  -> Token expired or expiring soon, attempting refresh...");
                var refreshed = await RefreshAccessTokenAsync(user);
                if (!refreshed)
                {
                    Console.WriteLine("  -> Refresh failed, returning null");
                    return null;
                }
                Console.WriteLine("  -> Token refreshed successfully");
            }

            var config = SpotifyClientConfig
                .CreateDefault()
                .WithToken(user.SpotifyAccessToken);

            Console.WriteLine("  -> Returning SpotifyClient");
            return new SpotifyClient(config);
        }

        /// <summary>
        /// Refresh the Spotify access token using the refresh token
        /// </summary>
        public async Task<bool> RefreshAccessTokenAsync(ApplicationUser user)
        {
            if (string.IsNullOrEmpty(user.SpotifyRefreshToken))
            {
                Console.WriteLine("RefreshAccessTokenAsync: No refresh token available");
                return false;
            }

            var clientId = _configuration["Authentication:Spotify:ClientId"];
            var clientSecret = _configuration["Authentication:Spotify:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("RefreshAccessTokenAsync: Missing client credentials");
                return false;
            }

            try
            {
                Console.WriteLine("RefreshAccessTokenAsync: Requesting new token...");
                var response = await new OAuthClient().RequestToken(
                    new AuthorizationCodeRefreshRequest(clientId, clientSecret, user.SpotifyRefreshToken)
                );

                user.SpotifyAccessToken = response.AccessToken;
                user.SpotifyTokenExpiration = DateTime.UtcNow.AddSeconds(response.ExpiresIn);

                // Spotify may return a new refresh token
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    user.SpotifyRefreshToken = response.RefreshToken;
                }

                await _userManager.UpdateAsync(user);
                Console.WriteLine($"RefreshAccessTokenAsync: Success! New expiration: {user.SpotifyTokenExpiration}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshAccessTokenAsync: Failed - {ex.Message}");
                return false;
            }
        }

        public async Task<List<FullTrack>> SearchTracks(ApplicationUser user, string query, int limit = 10)
        {
            Console.WriteLine($"========================================");
            Console.WriteLine($"SearchTracks called");
            Console.WriteLine($"  Query: '{query}'");
            Console.WriteLine($"  Limit: {limit}");
            Console.WriteLine($"========================================");
            
            try
            {
                var spotify = await GetSpotifyClientForUser(user);
                if (spotify == null)
                {
                    Console.WriteLine("ERROR: No Spotify client available");
                    return new List<FullTrack>();
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    Console.WriteLine("ERROR: Empty query");
                    return new List<FullTrack>();
                }

                var searchRequest = new SearchRequest(SearchRequest.Types.Track, query)
                {
                    Limit = limit
                };

                Console.WriteLine("Making Spotify API search call...");
                var response = await spotify.Search.Item(searchRequest);
                
                Console.WriteLine($"Response received:");
                Console.WriteLine($"  Tracks object: {(response?.Tracks != null ? "EXISTS" : "NULL")}");
                Console.WriteLine($"  Items: {(response?.Tracks?.Items != null ? "EXISTS" : "NULL")}");
                
                var results = response?.Tracks?.Items ?? new List<FullTrack>();
                Console.WriteLine($"  Results count: {results.Count}");
                
                if (results.Count > 0)
                {
                    Console.WriteLine($"Sample results:");
                    for (int i = 0; i < Math.Min(3, results.Count); i++)
                    {
                        var track = results[i];
                        Console.WriteLine($"    {i + 1}. {track.Name} by {string.Join(", ", track.Artists.Select(a => a.Name))}");
                    }
                }
                
                Console.WriteLine($"========================================");
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"========================================");
                Console.WriteLine($"EXCEPTION in SearchTracks:");
                Console.WriteLine($"  Message: {ex.Message}");
                Console.WriteLine($"  Type: {ex.GetType().Name}");
                Console.WriteLine($"  Stack: {ex.StackTrace}");
                Console.WriteLine($"========================================");
                return new List<FullTrack>();
            }
        }

        public async Task<FullTrack?> GetTrack(ApplicationUser user, string trackId)
        {
            try
            {
                var spotify = await GetSpotifyClientForUser(user);
                if (spotify == null || string.IsNullOrWhiteSpace(trackId)) 
                    return null;

                return await spotify.Tracks.Get(trackId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTrack ERROR: {ex.Message}");
                return null;
            }
        }

        public async Task<List<FullTrack>> GetUserTopTracks(ApplicationUser user, int limit = 20)
        {
            Console.WriteLine($"GetUserTopTracks called");
            try
            {
                var spotify = await GetSpotifyClientForUser(user);
                if (spotify == null)
                {
                    Console.WriteLine("GetUserTopTracks: No Spotify client available");
                    return new List<FullTrack>();
                }

                var request = new PersonalizationTopRequest
                {
                    TimeRangeParam = PersonalizationTopRequest.TimeRange.MediumTerm,
                    Limit = limit
                };

                Console.WriteLine("GetUserTopTracks: Making API call...");
                var response = await spotify.Personalization.GetTopTracks(request);
                var results = response?.Items ?? new List<FullTrack>();
                Console.WriteLine($"GetUserTopTracks: Got {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserTopTracks ERROR: {ex.Message}");
                return new List<FullTrack>();
            }
        }

        public async Task<List<FullArtist>> GetUserTopArtists(ApplicationUser user, int limit = 20)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserTopArtists ERROR: {ex.Message}");
                return new List<FullArtist>();
            }
        }

        public async Task<List<PlayHistoryItem>> GetRecentlyPlayed(ApplicationUser user, int limit = 20)
        {
            Console.WriteLine($"GetRecentlyPlayed called");
            try
            {
                var spotify = await GetSpotifyClientForUser(user);
                if (spotify == null)
                {
                    Console.WriteLine("GetRecentlyPlayed: No Spotify client available");
                    return new List<PlayHistoryItem>();
                }

                var request = new PlayerRecentlyPlayedRequest
                {
                    Limit = limit
                };

                Console.WriteLine("GetRecentlyPlayed: Making API call...");
                var response = await spotify.Player.GetRecentlyPlayed(request);
                var results = response?.Items ?? new List<PlayHistoryItem>();
                Console.WriteLine($"GetRecentlyPlayed: Got {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRecentlyPlayed ERROR: {ex.Message}");
                return new List<PlayHistoryItem>();
            }
        }

        public async Task<PrivateUser?> GetCurrentUserProfile(ApplicationUser user)
        {
            try
            {
                var spotify = await GetSpotifyClientForUser(user);
                if (spotify == null) return null;

                return await spotify.UserProfile.Current();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCurrentUserProfile ERROR: {ex.Message}");
                return null;
            }
        }

        public bool HasValidSpotifyConnection(ApplicationUser user)
        {
            return !string.IsNullOrEmpty(user.SpotifyAccessToken) &&
                   !string.IsNullOrEmpty(user.SpotifyRefreshToken);
        }
    }
}
