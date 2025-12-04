using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LetMusicConnectStrangers.Data;
using LetMusicConnectStrangers.Models;
using LetMusicConnectStrangers.Services;
using AspNet.Security.OAuth.Spotify;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.OAuth;

var builder = WebApplication.CreateBuilder(args);

// Load mkcert certificate that covers localhost and 127.0.0.1
var certPath = Path.Combine(builder.Environment.ContentRootPath, "certificate.pfx");
var certPassword = builder.Configuration["HttpsCertificatePassword"] ?? "changeit";

X509Certificate2? httpsCert = null;
if (File.Exists(certPath))
{
    httpsCert = new X509Certificate2(certPath, certPassword);
    Console.WriteLine("[HTTPS] Loaded mkcert certificate");
}

// Configure Kestrel to listen on 127.0.0.1:3000
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Parse("127.0.0.1"), 3000, listenOptions =>
    {
        if (httpsCert != null)
            listenOptions.UseHttps(httpsCert);
        else
            listenOptions.UseHttps();
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

        options.Scope.Add("user-read-email");
        options.Scope.Add("user-read-private");
        options.Scope.Add("user-top-read");
        options.Scope.Add("user-library-read");
        options.Scope.Add("playlist-read-private");

        // Set correlation cookie settings
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

        // Force redirect_uri to use 127.0.0.1 - replace both encoded and non-encoded versions
        options.Events = new OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = ctx =>
            {
                var uri = ctx.RedirectUri;
                
                // Replace URL-encoded localhost (%3A%2F%2Flocalhost%3A)
                uri = uri.Replace("%3A%2F%2Flocalhost%3A", "%3A%2F%2F127.0.0.1%3A");
                
                // Also replace non-encoded version just in case
                uri = uri.Replace("://localhost:", "://127.0.0.1:");
                
                Console.WriteLine($"[SpotifyAuth] Original: {ctx.RedirectUri}");
                Console.WriteLine($"[SpotifyAuth] Modified: {uri}");
                
                ctx.Response.Redirect(uri);
                return Task.CompletedTask;
            }
        };
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
