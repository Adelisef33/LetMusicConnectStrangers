using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LetMusicConnectStrangers.Data;
using LetMusicConnectStrangers.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LetMusicConnectStrangers.Pages.Reviews
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly LetMusicConnectStrangersContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(LetMusicConnectStrangersContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public class InputModel
        {
            public int? ReviewId { get; set; }
            [Required]
            public string SpotifyTrackId { get; set; } = string.Empty;
            [Required]
            public string TrackName { get; set; } = string.Empty;
            [Required]
            public string ArtistName { get; set; } = string.Empty;
            [Required]
            [Range(1,5)]
            public int Rating { get; set; } = 3;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == user.Id);
            if (review == null) return NotFound();

            Input = new InputModel
            {
                ReviewId = review.ReviewId,
                SpotifyTrackId = review.SpotifyTrackId,
                TrackName = review.TrackName,
                ArtistName = review.ArtistName,
                Rating = review.Rating
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid) return Page();

            if (!Input.ReviewId.HasValue) return BadRequest();

            // Verify the id from the route matches the ReviewId in the form
            if (Id != Input.ReviewId.Value) return BadRequest();

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == Input.ReviewId.Value && r.UserId == user.Id);
            if (review == null) return NotFound();

            review.SpotifyTrackId = Input.SpotifyTrackId;
            review.TrackName = Input.TrackName;
            review.ArtistName = Input.ArtistName;
            review.Rating = Input.Rating;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Reviews");
        }
    }
}
