#nullable disable

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using LetMusicConnectStrangers.Models;

namespace LetMusicConnectStrangers.Areas.Identity.Pages.Account
{
    [Authorize]
    public class SpotifyCallbackModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SpotifyCallbackModel> _logger;

        public SpotifyCallbackModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<SpotifyCallbackModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                _logger.LogWarning("Error loading external login information.");
                ModelState.AddModelError(string.Empty, "Error loading Spotify information.");
                return Page();
            }

            // Add the external login to the user
            var result = await _userManager.AddLoginAsync(user, info);
            if (result.Succeeded)
            {
                _logger.LogInformation("User linked their Spotify account successfully.");
                
                // Store Spotify ID and tokens
                var spotifyId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(spotifyId))
                {
                    user.SpotifyId = spotifyId;
                    user.SpotifyDisplayName = info.Principal.FindFirstValue(ClaimTypes.Name);
                    
                    if (info.AuthenticationTokens != null)
                    {
                        user.SpotifyAccessToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
                        user.SpotifyRefreshToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
                        
                        var expiresIn = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "expires_in")?.Value;
                        if (!string.IsNullOrEmpty(expiresIn) && int.TryParse(expiresIn, out int seconds))
                        {
                            user.SpotifyTokenExpiration = DateTime.UtcNow.AddSeconds(seconds);
                        }
                    }
                    
                    await _userManager.UpdateAsync(user);
                }

                // Get the return URL from TempData
                var returnUrl = TempData["PostSpotifyReturnUrl"] as string ?? Url.Content("~/");
                
                // Redirect back to localhost
                return RedirectToLocalhost(returnUrl);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                _logger.LogError("Failed to link Spotify account.");
                return Page();
            }
        }

        /// <summary>
        /// Redirects to localhost if currently on 127.0.0.1, otherwise does a LocalRedirect
        /// </summary>
        private IActionResult RedirectToLocalhost(string returnUrl)
        {
            // If we're on 127.0.0.1 (from Spotify callback), redirect to localhost
            if (string.Equals(Request.Host.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                var port = Request.Host.Port ?? 3000;
                
                // Handle both relative and absolute URLs
                string path;
                if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absUri))
                {
                    path = absUri.PathAndQuery;
                }
                else
                {
                    path = returnUrl.StartsWith("/") ? returnUrl : "/" + returnUrl;
                }
                
                var localhostUrl = $"https://localhost:{port}{path}";
                _logger.LogInformation("Redirecting from 127.0.0.1 back to localhost: {Url}", localhostUrl);
                return Redirect(localhostUrl);
            }

            return LocalRedirect(returnUrl);
        }
    }
}
