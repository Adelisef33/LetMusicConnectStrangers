using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LetMusicConnectStrangers.Data;
using LetMusicConnectStrangers.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using LetMusicConnectStrangers.Models.Validation;

namespace LetMusicConnectStrangers.Pages.Reviews
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly LetMusicConnectStrangersContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(LetMusicConnectStrangersContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string SpotifyTrackId { get; set; } = string.Empty;
            [Required]
            public string TrackName { get; set; } = string.Empty;
            [Required]
            public string ArtistName { get; set; } = string.Empty;

            [StringLength(200)]
            public string? AlbumName { get; set; }

            [StringLength(500)]
            public string? AlbumImageUrl { get; set; }

            // Rating from 1-5
            [Required]
            [Range(1, 5)]
            public int Rating { get; set; } = 3;

            // Allows users to add an optional comment with a 300-word maximum
            [DataType(DataType.MultilineText)]
            [MaxWords(300, ErrorMessage = "Comment must be 300 words or fewer.")]
            public string? Comment { get; set; }
        }

        public void OnGet()
        {
            // Ensure Input is initialized with default values
            Input = new InputModel
            {
                Rating = 3 // Default rating value
            };
            StatusMessage = "Page loaded via GET request. Ready to submit form.";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            StatusMessage = "Step 1: POST handler called";
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                StatusMessage = "Error: User not authenticated.";
                return Challenge();
            }

            StatusMessage = $"Step 2: User authenticated: {user.UserName}";

            if (!ModelState.IsValid)
            {
                // Collect all validation errors for debugging
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                var errorDetails = string.Join("; ", errors);
                StatusMessage = $"VALIDATION FAILED: {errorDetails}";
                
                // Also log ModelState keys
                var keys = string.Join(", ", ModelState.Keys);
                StatusMessage += $" | Fields: {keys}";
                
                return Page();
            }

            StatusMessage = "Step 3: Validation passed, creating review...";

            try
            {
                var review = new Review
                {
                    UserId = user.Id,
                    SpotifyTrackId = Input.SpotifyTrackId,
                    TrackName = Input.TrackName,
                    ArtistName = Input.ArtistName,
                    AlbumName = Input.AlbumName,
                    AlbumImageUrl = Input.AlbumImageUrl,
                    Rating = Input.Rating,
                    Comment = Input.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                StatusMessage = "Step 4: Review object created, adding to context...";
                
                _context.Reviews.Add(review);
                
                StatusMessage = "Step 5: Saving changes...";
                
                await _context.SaveChangesAsync();

                StatusMessage = $"SUCCESS! Review created with ID: {review.ReviewId}";
                
                // Use TempData for success message since we're redirecting
                TempData["SuccessMessage"] = $"Review created successfully! ID: {review.ReviewId}";
                return RedirectToPage("/Reviews");
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR at database save: {ex.Message}";
                if (ex.InnerException != null)
                {
                    StatusMessage += $" | Inner: {ex.InnerException.Message}";
                }
                if (ex.StackTrace != null)
                {
                    StatusMessage += $" | Stack: {ex.StackTrace.Substring(0, Math.Min(200, ex.StackTrace.Length))}";
                }
                return Page();
            }
        }
    }
}
