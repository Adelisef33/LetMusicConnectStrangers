# Quick Start Guide - Spotify Integration

## ? What's Already Done

All the code has been implemented! The following is ready:
- ? Custom `ApplicationUser` model with Spotify fields
- ? Database context updated
- ? Spotify OAuth configured
- ? Registration page requires Spotify
- ? Login page supports Spotify
- ? `SpotifyService` for API calls
- ? Example `SpotifyProfile` page
- ? All code compiles successfully

## ?? 3 Steps to Get Running

### Step 1: Get Spotify API Credentials (5 minutes)

1. Go to https://developer.spotify.com/dashboard
2. Log in with Spotify
3. Click **"Create an App"**
4. Fill in:
   - App name: "LetMusicConnectStrangers"
   - App description: "Connect people through music"
5. Click **"Edit Settings"**
6. Add to **Redirect URIs**:
   - `https://localhost:7XXX/signin-spotify` (replace XXX with your HTTPS port)
   - `http://localhost:5XXX/signin-spotify` (replace XXX with your HTTP port)
7. Click **Save**
8. Copy your **Client ID** and **Client Secret**

### Step 2: Configure Your App (2 minutes)

Update `appsettings.json`:
```json
"Authentication": {
  "Spotify": {
    "ClientId": "PASTE_YOUR_CLIENT_ID_HERE",
    "ClientSecret": "PASTE_YOUR_CLIENT_SECRET_HERE"
  }
}
```

**Security Note**: For production, use User Secrets or Azure Key Vault instead!

### Step 3: Update Database (1 minute)

Open **Package Manager Console** in Visual Studio and run:
```powershell
Add-Migration AddSpotifyFieldsToApplicationUser
Update-Database
```

## ?? You're Done! Test It

1. **Run the application** (F5)
2. Navigate to **Register** page
3. Click **"Connect with Spotify"**
4. Authorize your app
5. Complete registration
6. Visit `/SpotifyProfile` to see your music data!

## ?? Find Your Port Numbers

Your port numbers are in `Properties/launchSettings.json`:
```json
"applicationUrl": "https://localhost:7XXX;http://localhost:5XXX"
```

The redirect URI should match these ports exactly.

## ?? Troubleshooting

### "Invalid redirect URI"
- Make sure the redirect URI in Spotify Dashboard matches your port numbers exactly
- Format: `https://localhost:PORTNUMBER/signin-spotify`

### "ClientId not configured"
- Check that you updated `appsettings.json` with your actual credentials
- Make sure the JSON is valid (no trailing commas)

### "Error loading external login information"
- Make sure you ran the database migration
- Verify Spotify credentials are correct

### Database errors
- Run `Update-Database` in Package Manager Console
- Check connection string in `appsettings.json`

## ?? Next Steps

See `IMPLEMENTATION_SUMMARY.md` for:
- Complete feature list
- How to use `SpotifyService` in your pages
- Code examples
- Security best practices
- Ideas for additional features

## ?? Example: Using Spotify Data

```csharp
// In any PageModel or Controller
var user = await _userManager.GetUserAsync(User);
var topTracks = await _spotifyService.GetUserTopTracks(user, 20);
var topArtists = await _spotifyService.GetUserTopArtists(user, 20);
```

Visit `/SpotifyProfile` after logging in to see a working example!
