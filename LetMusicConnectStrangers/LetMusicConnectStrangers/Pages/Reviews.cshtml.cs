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
            // Use 0 so there's no silent default; user must actively pick a rating
            public int Rating { get; set; } = 0;
            
            
            public string? Comment { get; set; }
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
                        Rating = EditingReview.Rating,
                        Comment = EditingReview.Comment ?? string.Empty
                    };
                    await LoadSpotifyTracksAsync(user);
                }
                else
                {
                    _logger.LogWarning("Review not found for EditId: {EditId}", EditId.Value);
                }
            }
            // If ShowCreateModal is requested via query string (for creating NEW review)
            else if (ShowCreateForm && user != null)
            {
                // Ensure Input is completely fresh for creating a new review
                Input = new InputModel
                {
                    ReviewId = null,
                    SpotifyTrackId = string.Empty,
                    TrackName = string.Empty,
                    ArtistName = string.Empty,
                    AlbumName = null,
                    AlbumImageUrl = null,
                    Rating = 0,
                    Comment = string.Empty
                };
                EditingReview = null; // Explicitly null to indicate this is create, not edit
                await LoadSpotifyTracksAsync(user);
            }
        }

        public async Task<IActionResult> OnPostOpenFormAsync()
        {
            // Redirect to GET with ShowCreateForm=true to ensure clean form state
            // This prevents browser from keeping old POST data in the form fields
            return RedirectToPage(new { ShowCreateForm = true, ActiveTab = "recent" });
        }

        private async Task LoadSpotifyTracksAsync(ApplicationUser user)
        {
            try
            {
                // Fallback: sometimes the ReviewId isn't bound correctly from the form.
                // Try to populate it from the posted form values using both Razor-generated
                // name and a plain id-based name used by some client-side code.
                // IMPORTANT: Only access Request.Form on POST requests
                if (!Input.ReviewId.HasValue && Request.Method == "POST" && Request.HasFormContentType)
                {
                    if (Request.Form.TryGetValue("Input.ReviewId", out var rid) && !string.IsNullOrWhiteSpace(rid))
                    {
                        if (int.TryParse(rid.ToString(), out var parsedRid))
                        {
                            Input.ReviewId = parsedRid;
                        }
                    }
                    else if (Request.Form.TryGetValue("inputReviewId", out var rid2) && !string.IsNullOrWhiteSpace(rid2))
                    {
                        if (int.TryParse(rid2.ToString(), out var parsedRid2))
                        {
                            Input.ReviewId = parsedRid2;
                        }
                    }
                }
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
            // Fallback: sometimes client-side JS may not populate hidden inputs (rating/track id).
            // Attempt to read values directly from the request form and populate Input before validation.
            try
            {
                if (string.IsNullOrWhiteSpace(Input.SpotifyTrackId) && Request.Form.TryGetValue("Input.SpotifyTrackId", out var fv) && !string.IsNullOrWhiteSpace(fv))
                {
                    Input.SpotifyTrackId = fv.ToString();
                }

                if ((Input.Rating == 0) && Request.Form.TryGetValue("Input.Rating", out var rv) && int.TryParse(rv.ToString(), out var parsedRating) && parsedRating > 0)
                {
                    Input.Rating = parsedRating;
                }

                if ((Input.Rating == 0) && Request.Form.TryGetValue("inputRating", out var rv2) && int.TryParse(rv2.ToString(), out var parsedRating2) && parsedRating2 > 0)
                {
                    Input.Rating = parsedRating2;
                }

                if (string.IsNullOrWhiteSpace(Input.TrackName) && Request.Form.TryGetValue("Input.TrackName", out var tn) && !string.IsNullOrWhiteSpace(tn))
                {
                    Input.TrackName = tn.ToString();
                }
                
                // Always ensure Comment is an empty string, never null
                if (Input.Comment == null)
                {
                    Input.Comment = string.Empty;
                }
                
                // Try to populate comment from form if it's currently empty
                if (string.IsNullOrWhiteSpace(Input.Comment))
                {
                    if (Request.Form.TryGetValue("Input.Comment", out var cv))
                    {
                        Input.Comment = cv.ToString() ?? string.Empty;
                    }
                    else if (Request.Form.TryGetValue("inputComment", out var cv2))
                    {
                        Input.Comment = cv2.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading fallback form values");
            }

            _logger.LogInformation("Input.ReviewId: {ReviewId}", Input.ReviewId);
            _logger.LogInformation("Input.SpotifyTrackId: {TrackId}", Input.SpotifyTrackId);
            _logger.LogInformation("Input.Rating: {Rating}", Input.Rating);
            _logger.LogInformation("Input.Comment: '{Comment}'", Input.Comment ?? "(null)");
            _logger.LogInformation("Input.Comment length: {Length}", Input.Comment?.Length ?? 0);
            _logger.LogInformation("Input.TrackName: {Track}", Input.TrackName);
            _logger.LogInformation("Input.ArtistName: {Artist}", Input.ArtistName);
            _logger.LogInformation("ModelState.IsValid (before validate): {Valid}", ModelState.IsValid);

            // Re-validate the Input model now that we've populated fallback values
            ModelState.Clear();
            TryValidateModel(Input);
            _logger.LogInformation("ModelState.IsValid (after validate): {Valid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid when posting review. Errors:");
                var errorMessages = new List<string>();
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning("  Error: {ErrorMessage}", error.ErrorMessage);
                        errorMessages.Add(error.ErrorMessage);
                    }
                }

                ErrorMessage = $"Validation failed: {string.Join("; ", errorMessages)}";
                ShowCreateForm = true;
                Reviews = await _context.Reviews
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                
                // If we were editing a review, reload it so the form shows the existing data
                if (Input.ReviewId.HasValue)
                {
                    EditingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.ReviewId == Input.ReviewId.Value && r.UserId == user.Id);
                    _logger.LogInformation("Reloaded EditingReview for failed validation: {ReviewId}", Input.ReviewId);
                }
                
                await LoadSpotifyTracksAsync(user);
                return Page();
            }

            _logger.LogInformation("=== Checking if create or update ===");
            _logger.LogInformation("Input.ReviewId.HasValue: {HasValue}", Input.ReviewId.HasValue);
            _logger.LogInformation("Input.ReviewId value: {Value}", Input.ReviewId);
            
            if (Input.ReviewId.HasValue)
            {
                _logger.LogInformation("*** TAKING UPDATE PATH *** Attempting to update review with ID: {ReviewId}", Input.ReviewId.Value);
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
                // Save comment (null when empty) so edits persist and existing reviews display comments
                existingReview.Comment = string.IsNullOrWhiteSpace(Input.Comment) ? null : Input.Comment;
                existingReview.UpdatedAt = DateTime.UtcNow;
                
                // Explicitly mark the entity as modified to ensure EF detects changes
                _context.Entry(existingReview).State = EntityState.Modified;

                var updated = await _context.SaveChangesAsync();
                _logger.LogInformation("Updated review {Id}, SaveChanges returned {Count}", existingReview.ReviewId, updated);
                
                // Log the updated values for debugging
                _logger.LogInformation("Review after save: TrackName={Track}, Rating={Rating}, Comment={Comment}", 
                    existingReview.TrackName, existingReview.Rating, existingReview.Comment);
                
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
                
                _logger.LogInformation("Redirecting to Reviews page after successful update");
                return RedirectToPage();
            }
            else
            {
                _logger.LogInformation("*** TAKING CREATE PATH *** Creating new review...");
                _logger.LogInformation("CREATE - SpotifyTrackId: {TrackId}", Input.SpotifyTrackId);
                _logger.LogInformation("CREATE - TrackName: {TrackName}", Input.TrackName);
                _logger.LogInformation("CREATE - ArtistName: {ArtistName}", Input.ArtistName);
                _logger.LogInformation("CREATE - Rating: {Rating}", Input.Rating);
                    
                    
                var review = new Review
                {
                    UserId = user.Id,
                    SpotifyTrackId = Input.SpotifyTrackId,
                    TrackName = Input.TrackName,
                    ArtistName = Input.ArtistName,
                    AlbumName = Input.AlbumName,
                    AlbumImageUrl = Input.AlbumImageUrl,
                    Rating = Input.Rating,
                    Comment = string.IsNullOrWhiteSpace(Input.Comment) ? null : Input.Comment,
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
                        Rating = EditingReview.Rating,
                        Comment = EditingReview.Comment ?? string.Empty
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
                        Rating = EditingReview.Rating,
                        Comment = EditingReview.Comment ?? string.Empty
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
