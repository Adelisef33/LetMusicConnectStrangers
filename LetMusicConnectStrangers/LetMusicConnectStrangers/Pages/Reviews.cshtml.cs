using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LetMusicConnectStrangers.Data;
using LetMusicConnectStrangers.Models;
using LetMusicConnectStrangers.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace LetMusicConnectStrangers.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ReviewsModel : PageModel
    {
        private readonly LetMusicConnectStrangersContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SpotifyService _spotifyService;
        private readonly ILogger<ReviewsModel> _logger;

        public ReviewsModel(
            LetMusicConnectStrangersContext context, 
            UserManager<ApplicationUser> userManager,
            SpotifyService spotifyService,
            ILogger<ReviewsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _spotifyService = spotifyService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowCreateForm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ActiveTab { get; set; }

        // Initialize lists properly instead of using invalid array literal
        public List<Review> Reviews { get; set; } = new List<Review>();
        public List<TrackSearchResult> SearchResults { get; set; } = new List<TrackSearchResult>();
        public List<TrackSearchResult> RecentlyPlayed { get; set; } = new List<TrackSearchResult>();
        public List<TrackSearchResult> TopTracks { get; set; } = new List<TrackSearchResult>();
        public Review? EditingReview { get; set; }
        public string? CurrentUserId { get; set; }
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            public int? ReviewId { get; set; }

            [Required(ErrorMessage = "Please select a song")]
            public string SpotifyTrackId { get; set; } = string.Empty;

            public string TrackName { get; set; } = string.Empty;
            public string ArtistName { get; set; } = string.Empty;
            public string? AlbumName { get; set; }
            public string? AlbumImageUrl { get; set; }

            [Required(ErrorMessage = "Rating is required")]
            [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5")]
            public int Rating { get; set; } = 3;
        }

        public class TrackSearchResult
        {
            public string SpotifyId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Artist { get; set; } = string.Empty;
            public string Album { get; set; } = string.Empty;
            public string? ImageUrl { get; set; }
        }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            CurrentUserId = user?.Id;

            _logger.LogInformation("OnGetAsync called with EditId: {EditId}, ShowCreateForm: {ShowCreateForm}", EditId, ShowCreateForm);

            if (string.IsNullOrEmpty(ActiveTab))
            {
                ActiveTab = "recent";
            }

            Reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // If editing, load the review and show modal
            if (EditId.HasValue && user != null)
            {
                _logger.LogInformation("Loading review for edit. EditId: {EditId}, UserId: {UserId}", EditId.Value, user.Id);
                
                EditingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ReviewId == EditId.Value && r.UserId == user.Id);

                if (EditingReview != null)
                {
                    _logger.LogInformation("Review loaded: {ReviewId} - {TrackName}", EditingReview.ReviewId, EditingReview.TrackName);
                    ShowCreateForm = true;
                    Input = new InputModel
                    {
                        ReviewId = EditingReview.ReviewId,
                        SpotifyTrackId = EditingReview.SpotifyTrackId,
                        TrackName = EditingReview.TrackName,
                        ArtistName = EditingReview.ArtistName,
                        AlbumName = EditingReview.AlbumName,
                        AlbumImageUrl = EditingReview.AlbumImageUrl,
                        Rating = EditingReview.Rating
                    };
                    await LoadSpotifyTracksAsync(user);
                }
                else
                {
                    _logger.LogWarning("Review not found for EditId: {EditId}", EditId.Value);
                }
            }
            // If ShowCreateModal is requested via query string
            else if (ShowCreateForm && user != null)
            {
                await LoadSpotifyTracksAsync(user);
            }
        }

        public async Task<IActionResult> OnPostOpenFormAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            CurrentUserId = user?.Id;
            ShowCreateForm = true;
            ActiveTab = "recent";

            Reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (user != null)
            {
                await LoadSpotifyTracksAsync(user);
            }

            return Page();
        }

        private async Task LoadSpotifyTracksAsync(ApplicationUser user)
        {
            try
            {
                Console.WriteLine($"Loading Spotify tracks for user: {user.UserName}");
                Console.WriteLine($"Access Token exists: {!string.IsNullOrEmpty(user.SpotifyAccessToken)}");
                Console.WriteLine($"Refresh Token exists: {!string.IsNullOrEmpty(user.SpotifyRefreshToken)}");
                Console.WriteLine($"Token Expiration: {user.SpotifyTokenExpiration}");

                var recentTracks = await _spotifyService.GetRecentlyPlayed(user, 10);
                Console.WriteLine($"Recent tracks count: {recentTracks.Count}");
                
                RecentlyPlayed = recentTracks.Select(t => new TrackSearchResult
                {
                    SpotifyId = t.Track.Id,
                    Name = t.Track.Name,
                    Artist = string.Join(", ", t.Track.Artists.Select(a => a.Name)),
                    Album = t.Track.Album?.Name ?? "",
                    ImageUrl = t.Track.Album?.Images?.FirstOrDefault()?.Url
                }).DistinctBy(t => t.SpotifyId).ToList();

                var topTracks = await _spotifyService.GetUserTopTracks(user, 10);
                Console.WriteLine($"Top tracks count: {topTracks.Count}");
                
                TopTracks = topTracks.Select(t => new TrackSearchResult
                {
                    SpotifyId = t.Id,
                    Name = t.Name,
                    Artist = string.Join(", ", t.Artists.Select(a => a.Name)),
                    Album = t.Album?.Name ?? "",
                    ImageUrl = t.Album?.Images?.FirstOrDefault()?.Url
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Spotify tracks: {ex.Message}");
                ErrorMessage = "Unable to load your Spotify tracks. Please try logging out and back in.";
            }
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("=== OnPostSearchAsync called ===");
            Console.WriteLine($"SearchQuery from binding: '{SearchQuery}'");
            Console.WriteLine($"Request.Form['SearchQuery']: '{Request.Form["SearchQuery"]}'");
            
            var user = await _userManager.GetUserAsync(User);
            CurrentUserId = user?.Id;
            ShowCreateForm = true;
            ActiveTab = "search";

            Reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (user == null)
            {
                Console.WriteLine("ERROR: User is null!");
                ErrorMessage = "User not found. Please log in again.";
                Console.WriteLine("===========================================");
                return Page();
            }

            // Load recent and top tracks first
            await LoadSpotifyTracksAsync(user);

            // Check SearchQuery from both binding and form
            var searchTerm = SearchQuery ?? Request.Form["SearchQuery"].ToString();
            searchTerm = searchTerm?.Trim();
            Console.WriteLine($"Final search term after trim: '{searchTerm}'");
            Console.WriteLine($"Search term length: {searchTerm?.Length ?? 0}");
            Console.WriteLine($"Search term is null or whitespace: {string.IsNullOrWhiteSpace(searchTerm)}");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                try
                {
                    Console.WriteLine($"===> CALLING SPOTIFY SEARCH");
                    Console.WriteLine($"     Query: '{searchTerm}'");
                    Console.WriteLine($"     Limit: 20");
                    
                    var tracks = await _spotifyService.SearchTracks(user, searchTerm, 20);
                    
                    Console.WriteLine($"<===SPOTIFY RETURNED");
                    Console.WriteLine($"     Track count: {tracks.Count}");
                    
                    if (tracks.Count > 0)
                    {
                        Console.WriteLine($"     First 3 results:");
                        for (int i = 0; i < Math.Min(3, tracks.Count); i++)
                        {
                            Console.WriteLine($"       {i + 1}. {tracks[i].Name} by {string.Join(", ", tracks[i].Artists.Select(a => a.Name))}");
                        }
                        
                        SearchResults = tracks.Select(t => new TrackSearchResult
                        {
                            SpotifyId = t.Id,
                            Name = t.Name,
                            Artist = string.Join(", ", t.Artists.Select(a => a.Name)),
                            Album = t.Album?.Name ?? "",
                            ImageUrl = t.Album?.Images?.FirstOrDefault()?.Url
                        }).ToList();
                        
                        Console.WriteLine($"SearchResults populated with {SearchResults.Count} TrackSearchResult objects");
                    }
                    else
                    {
                        Console.WriteLine("WARNING: Spotify returned 0 tracks");
                        Console.WriteLine("This could mean:");
                        Console.WriteLine("  1. The search term has no matches");
                        Console.WriteLine("  2. Spotify API error");
                        Console.WriteLine("  3. Token/permission issue");
                        SearchResults = new List<TrackSearchResult>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("!!!!! EXCEPTION in search !!!!");
                    Console.WriteLine($"Type: {ex.GetType().Name}");
                    Console.WriteLine($"Message: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    ErrorMessage = $"Search error: {ex.Message}";
                    SearchResults = new List<TrackSearchResult>();
                }
            }
            else
            {
                Console.WriteLine("Search term is empty or whitespace - not searching");
                SearchResults = new List<TrackSearchResult>();
            }

            Console.WriteLine("=== OnPostSearchAsync complete ===");
            Console.WriteLine($"    SearchResults.Count = {SearchResults.Count}");
            Console.WriteLine($"    ActiveTab = {ActiveTab}");
            Console.WriteLine($"    SearchQuery = '{SearchQuery}'");
            Console.WriteLine("===========================================");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            CurrentUserId = user.Id;

            _logger.LogInformation("=== OnPostAsync called ===");
            _logger.LogInformation("Input.ReviewId: {ReviewId}", Input.ReviewId);
            _logger.LogInformation("Input.SpotifyTrackId: {TrackId}", Input.SpotifyTrackId);
            _logger.LogInformation("Input.Rating: {Rating}", Input.Rating);
            _logger.LogInformation("Input.TrackName: {Track}", Input.TrackName);
            _logger.LogInformation("Input.ArtistName: {Artist}", Input.ArtistName);
            _logger.LogInformation("ModelState.IsValid: {Valid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid when posting review. Errors:");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning("  Error: {ErrorMessage}", error.ErrorMessage);
                    }
                }

                ShowCreateForm = true;
                Reviews = await _context.Reviews
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                await LoadSpotifyTracksAsync(user);
                return Page();
            }

            if (Input.ReviewId.HasValue)
            {
                _logger.LogInformation("Attempting to update review with ID: {ReviewId}", Input.ReviewId.Value);
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ReviewId == Input.ReviewId.Value && r.UserId == user.Id);

                if (existingReview == null)
                {
                    _logger.LogWarning("Review not found for update: ID={ReviewId}, UserId={UserId}", Input.ReviewId.Value, user.Id);
                    return NotFound();
                }

                _logger.LogInformation("Found review to update, updating fields...");
                
                existingReview.SpotifyTrackId = Input.SpotifyTrackId;
                existingReview.TrackName = Input.TrackName;
                existingReview.ArtistName = Input.ArtistName;
                existingReview.AlbumName = Input.AlbumName;
                existingReview.AlbumImageUrl = Input.AlbumImageUrl;
                existingReview.Rating = Input.Rating;
                existingReview.UpdatedAt = DateTime.UtcNow;
                
                // Explicitly mark the entity as modified to ensure EF detects changes
                _context.Entry(existingReview).State = EntityState.Modified;

                var updated = await _context.SaveChangesAsync();
                _logger.LogInformation("Updated review {Id}, SaveChanges returned {Count}", existingReview.ReviewId, updated);
                if (updated <= 0)
                {
                    ErrorMessage = "Unable to update review. Please try again.";
                    ShowCreateForm = true;
                    Reviews = await _context.Reviews
                        .Include(r => r.User)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
                    await LoadSpotifyTracksAsync(user);
                    return Page();
                }
            }
            else
            {
                _logger.LogInformation("Creating new review...");
                    
                var review = new Review
                {
                    UserId = user.Id,
                    SpotifyTrackId = Input.SpotifyTrackId,
                    TrackName = Input.TrackName,
                    ArtistName = Input.ArtistName,
                    AlbumName = Input.AlbumName,
                    AlbumImageUrl = Input.AlbumImageUrl,
                    Rating = Input.Rating,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                var saved = await _context.SaveChangesAsync();
                _logger.LogInformation("Attempted to save new review for user {UserId}, SaveChanges returned {Count}", user.Id, saved);
                if (saved <= 0)
                {
                    ErrorMessage = "Unable to save your review. Please try again.";
                    ShowCreateForm = true;
                    Reviews = await _context.Reviews
                        .Include(r => r.User)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
                    await LoadSpotifyTracksAsync(user);
                    return Page();
                }

                _logger.LogInformation("New review saved with id {ReviewId}", review.ReviewId);
            }

            _logger.LogInformation("=== Review saved successfully ===");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            _logger.LogInformation("=== OnPostDeleteAsync called with id: {Id} ===", id);

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == user.Id);

            if (review == null)
            {
                _logger.LogWarning("Review not found for deletion: ID={Id}, UserId={UserId}", id, user.Id);
                return NotFound();
            }

            _logger.LogInformation("Found review to delete: ID={Id}, Title={Title}", review.ReviewId, review.TrackName);
            _context.Reviews.Remove(review);
            var deleted = await _context.SaveChangesAsync();
            _logger.LogInformation("Delete completed, SaveChanges returned {Count}", deleted);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            CurrentUserId = user.Id;
            _logger.LogInformation("OnPostEditAsync called for id {Id} by user {UserId}", id, user.Id);

            // Load reviews for display
            Reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Load the review to edit (only allow owner)
            EditingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == user.Id);

            if (EditingReview == null)
            {
                _logger.LogWarning("Edit requested but review not found or not owned by user. Id: {Id}", id);
                return NotFound();
            }

            // Populate the Input model so the form shows values
            Input = new InputModel
            {
                ReviewId = EditingReview.ReviewId,
                SpotifyTrackId = EditingReview.SpotifyTrackId,
                TrackName = EditingReview.TrackName,
                ArtistName = EditingReview.ArtistName,
                AlbumName = EditingReview.AlbumName,
                AlbumImageUrl = EditingReview.AlbumImageUrl,
                Rating = EditingReview.Rating
            };

            ShowCreateForm = true;
            ActiveTab = "recent";

            await LoadSpotifyTracksAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            CurrentUserId = user.Id;
            _logger.LogInformation("OnGetEditAsync called for id {Id} by user {UserId}", id, user.Id);

            // Load reviews for display
            Reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Load the review to edit (only allow owner)
            EditingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == user.Id);

            if (EditingReview == null)
            {
                _logger.LogWarning("Edit requested but review not found or not owned by user. Id: {Id}", id);
                return NotFound();
            }

            // Populate the Input model so the form shows values
            Input = new InputModel
            {
                ReviewId = EditingReview.ReviewId,
                SpotifyTrackId = EditingReview.SpotifyTrackId,
                TrackName = EditingReview.TrackName,
                ArtistName = EditingReview.ArtistName,
                AlbumName = EditingReview.AlbumName,
                AlbumImageUrl = EditingReview.AlbumImageUrl,
                Rating = EditingReview.Rating
            };

            ShowCreateForm = true;
            ActiveTab = "recent";

            await LoadSpotifyTracksAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnGetSearchTracksAsync(string query)
        {
            _logger.LogInformation("OnGetSearchTracksAsync called with query: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return new JsonResult(new List<object>());
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for search");
                return Unauthorized();
            }

            try
            {
                var tracks = await _spotifyService.SearchTracks(user, query, 20);
                
                var results = tracks.Select(t => new
                {
                    spotifyId = t.Id,
                    name = t.Name,
                    artist = string.Join(", ", t.Artists.Select(a => a.Name)),
                    album = t.Album?.Name ?? "",
                    imageUrl = t.Album?.Images?.FirstOrDefault()?.Url
                }).ToList();

                _logger.LogInformation("Search completed. Found {Count} tracks", results.Count);
                return new JsonResult(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Spotify tracks");
                return new JsonResult(new List<object>());
            }
        }
    }
}
