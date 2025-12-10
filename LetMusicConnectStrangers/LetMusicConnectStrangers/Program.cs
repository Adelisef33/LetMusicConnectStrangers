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

var builder = WebApplication.CreateBuilder(args);

// Load mkcert certificate that covers 127.0.0.1
var certPath = Path.Combine(builder.Environment.ContentRootPath, "certificate.pfx");
var certPassword = builder.Configuration["CertificatePassword"] ?? "changeit";

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Parse("127.0.0.1"), 3000, listenOptions =>
    {
        if (File.Exists(certPath))
        {
            var cert = new X509Certificate2(certPath, certPassword);
            listenOptions.UseHttps(cert);
        }
        else
        {
            throw new FileNotFoundException($"Certificate not found at {certPath}. Run: mkcert -pkcs12 -p12-file certificate.pfx 127.0.0.1 localhost");
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
