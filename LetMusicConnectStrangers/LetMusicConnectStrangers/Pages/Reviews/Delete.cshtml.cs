using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LetMusicConnectStrangers.Data;
using LetMusicConnectStrangers.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LetMusicConnectStrangers.Pages.Reviews
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly LetMusicConnectStrangersContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(LetMusicConnectStrangersContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Review Review { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        [FromRoute]
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == user.Id);
            if (review == null) return NotFound();

            Review = review;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == Id && r.UserId == user.Id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Reviews");
        }
    }
}
