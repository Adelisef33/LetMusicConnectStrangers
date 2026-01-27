using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LetMusicConnectStrangers.Data;
using LetMusicConnectStrangers.Models;
using LetMusicConnectStrangers.Services;
using AspNet.Security.OAuth.Spotify;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Prefer mkcert.pfx, fall back to certificate.pfx, then to dev-certs
var mkcertPath = Path.Combine(builder.Environment.ContentRootPath, "mkcert.pfx");
var certPath = Path.Combine(builder.Environment.ContentRootPath, "certificate.pfx");
var certPassword = builder.Configuration["CertificatePassword"] ?? "changeit";

// Create temporary logger to report certificate used
using var tempLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var certLogger = tempLoggerFactory.CreateLogger("KestrelCert");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Parse("127.0.0.1"), 3000, listenOptions =>
    {
        string usedPath = null;
        X509Certificate2? cert = null;

        if (File.Exists(mkcertPath))
        {
            usedPath = mkcertPath;
        }
        else if (File.Exists(certPath))
        {
            usedPath = certPath;
        }

        if (usedPath != null)
        {
            // Try with provided password first, then try without password if that fails
            try
            {
                try
                {
                    cert = new X509Certificate2(usedPath, certPassword);
                    certLogger.LogInformation("Loaded PFX with password from {Path}.", usedPath);
                }
                catch (System.Security.Cryptography.CryptographicException pwEx)
                {
                    certLogger.LogWarning(pwEx, "Failed to load PFX with password; attempting without password for {Path}.", usedPath);
                    // Try without password
                    cert = new X509Certificate2(usedPath);
                    certLogger.LogInformation("Loaded PFX without password from {Path}.", usedPath);
                }

                // Log certificate details for diagnosis
                if (cert != null)
                {
                    certLogger.LogInformation("Using certificate from {Path} - Subject: {Subject}, Issuer: {Issuer}, Thumbprint: {Thumbprint}, ValidFrom: {From}, ValidTo: {To}",
                        usedPath, cert.Subject, cert.Issuer, cert.Thumbprint, cert.NotBefore, cert.NotAfter);

                    listenOptions.UseHttps(cert);
                    return;
                }
            }
            catch (Exception ex)
            {
                certLogger.LogError(ex, "Failed to load certificate at {Path}", usedPath);
                // fall through to dev cert if available
            }
        }

        if (builder.Environment.IsDevelopment())
        {
            certLogger.LogWarning("No valid PFX found or failed to load; requesting Kestrel use the default development certificate.");
            listenOptions.UseHttps();
        }
        else
        {
            throw new FileNotFoundException($"Certificate not found at {certPath} (or mkcert.pfx). Run: mkcert -pkcs12 -p12-file mkcert.pfx 127.0.0.1 localhost");
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("LetMusicConnectStrangersContextConnection") ?? throw new InvalidOperationException("Connection string 'LetMusicConnectStrangersContextConnection' not found.");

builder.Services.AddDbContext<LetMusicConnectStrangersContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<LetMusicConnectStrangersContext>();

builder.Services.AddAuthentication()
    .AddSpotify(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Spotify:ClientId"] ?? throw new InvalidOperationException("Spotify ClientId not configured");
        options.ClientSecret = builder.Configuration["Authentication:Spotify:ClientSecret"] ?? throw new InvalidOperationException("Spotify ClientSecret not configured");
        options.SaveTokens = true;
        options.CallbackPath = "/signin-spotify";

        // Required scopes for Spotify API access
        options.Scope.Add("user-read-email");
        options.Scope.Add("user-read-private");
        options.Scope.Add("user-top-read");           // For top tracks/artists
        options.Scope.Add("user-read-recently-played"); // For recently played tracks
        options.Scope.Add("user-library-read");       // For saved tracks
        options.Scope.Add("playlist-read-private");   // For playlists

        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddScoped<SpotifyService>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new RequireHttpsAttribute());
});
builder.Services.AddRazorPages();

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 3000;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.MapRazorPages();
app.Run();
