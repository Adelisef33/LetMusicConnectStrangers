#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using LetMusicConnectStrangers.Models;

namespace LetMusicConnectStrangers.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl },
                protocol: "https",
                host: "127.0.0.1:3000");

            _logger.LogInformation("Generated redirectUrl for external provider {Provider}: {RedirectUrl}", provider, redirectUrl);

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Try to sign in with existing external login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                
                // Update tokens for existing user
                if (info.LoginProvider == "Spotify")
                {
                    var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                    if (user != null)
                    {
                        await UpdateSpotifyTokens(user, info);
                    }
                }
                return LocalRedirect(returnUrl);
            }
            
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            // Handle Spotify login - auto-create or link account
            if (info.LoginProvider == "Spotify")
            {
                return await HandleSpotifyLoginAsync(info, returnUrl);
            }

            // For other providers, show confirmation page
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                Input = new InputModel { Email = info.Principal.FindFirstValue(ClaimTypes.Email) };
            }
            return Page();
        }

        private async Task<IActionResult> HandleSpotifyLoginAsync(ExternalLoginInfo info, string returnUrl)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var displayName = info.Principal.FindFirstValue(ClaimTypes.Name);
            var spotifyId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(email))
            {
                ErrorMessage = "Email not provided by Spotify. Please ensure your Spotify account has a verified email.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Check if user with this email exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            
            if (existingUser != null)
            {
                // User exists - link Spotify login if not already linked
                var logins = await _userManager.GetLoginsAsync(existingUser);
                var hasSpotifyLogin = logins.Any(l => l.LoginProvider == "Spotify");

                if (!hasSpotifyLogin)
                {
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (!addLoginResult.Succeeded)
                    {
                        _logger.LogError("Failed to link Spotify login: {Errors}", 
                            string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                    }
                }

                // Update Spotify fields
                existingUser.SpotifyId = spotifyId;
                existingUser.SpotifyDisplayName = displayName;
                existingUser.EmailConfirmed = true; // Ensure email is confirmed
                await UpdateSpotifyTokens(existingUser, info);

                // Sign in the user
                await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                _logger.LogInformation("User {Email} signed in via Spotify.", email);
                return LocalRedirect(returnUrl);
            }

            // Create new user
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                SpotifyId = spotifyId,
                SpotifyDisplayName = displayName
            };

            if (info.AuthenticationTokens != null)
            {
                newUser.SpotifyAccessToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
                newUser.SpotifyRefreshToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
                var expiresIn = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "expires_in")?.Value;
                if (!string.IsNullOrEmpty(expiresIn) && int.TryParse(expiresIn, out int seconds))
                {
                    newUser.SpotifyTokenExpiration = DateTime.UtcNow.AddSeconds(seconds);
                }
            }

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create user: {Errors}", 
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                ErrorMessage = "Unable to create account. Please try again.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            await _userManager.AddLoginAsync(newUser, info);
            await _signInManager.SignInAsync(newUser, isPersistent: false, info.LoginProvider);
            _logger.LogInformation("Created new user {Email} via Spotify.", email);
            return LocalRedirect(returnUrl);
        }

        private async Task UpdateSpotifyTokens(ApplicationUser user, ExternalLoginInfo info)
        {
            if (info.AuthenticationTokens == null) return;
            
            user.SpotifyAccessToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
            user.SpotifyRefreshToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
            
            var expiresIn = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "expires_in")?.Value;
            if (!string.IsNullOrEmpty(expiresIn) && int.TryParse(expiresIn, out int seconds))
            {
                user.SpotifyTokenExpiration = DateTime.UtcNow.AddSeconds(seconds);
            }
            
            await _userManager.UpdateAsync(user);
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("./RegisterConfirmation", new { Email = Input.Email });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
