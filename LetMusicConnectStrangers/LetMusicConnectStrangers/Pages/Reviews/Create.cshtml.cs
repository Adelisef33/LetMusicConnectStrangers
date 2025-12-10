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

        public class InputModel
        {
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

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid) return Page();

            var review = new Review
            {
                UserId = user.Id,
                SpotifyTrackId = Input.SpotifyTrackId,
                TrackName = Input.TrackName,
                ArtistName = Input.ArtistName,
                Rating = Input.Rating,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Reviews");
        }
    }
}
