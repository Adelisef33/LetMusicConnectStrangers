# Spotify API Integration Setup Instructions

## 1. Get Spotify API Credentials

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Log in with your Spotify account
3. Click "Create an App"
4. Fill in the app name and description
5. After creating, you'll see your **Client ID** and **Client Secret**
6. Click "Edit Settings"
7. Add the following to **Redirect URIs**:
   - `https://localhost:7XXX/signin-spotify` (replace XXX with your port)
   - `http://localhost:5XXX/signin-spotify` (replace XXX with your port)
8. Save the settings

## 2. Update appsettings.json

Replace the placeholder values in `appsettings.json` with your actual credentials:
```json
"Authentication": {
  "Spotify": {
    "ClientId": "your-actual-client-id-here",
    "ClientSecret": "your-actual-client-secret-here"
  }
}
```

## 3. Create Database Migration

Run the following commands in the Package Manager Console:

```powershell
Add-Migration AddSpotifyFieldsToApplicationUser
Update-Database
```

Or using .NET CLI:

```bash
dotnet ef migrations add AddSpotifyFieldsToApplicationUser
dotnet ef database update
```

## 4. Test the Integration

1. Run the application
2. Navigate to the Register page
3. Click "Connect with Spotify"
4. Authorize the application
5. Complete the registration form
6. You should now be registered with Spotify integration!

## Features Implemented

### Registration Flow:
- **Step 1**: Users must authenticate with Spotify
- **Step 2**: After Spotify authentication, users complete registration with email/password
- Spotify data (ID, tokens, display name) is stored in the database

### Login Flow:
- Users can log in with **email/password** OR **Spotify account**
- Spotify tokens are refreshed on each login
- Existing users can link their Spotify account

### Data Stored:
- SpotifyId
- SpotifyAccessToken
- SpotifyRefreshToken
- SpotifyTokenExpiration
- SpotifyDisplayName

## Next Steps

You can now use the stored Spotify tokens to:
- Fetch user's top tracks
- Get user's playlists
- Access user's library
- Make recommendations based on listening history

Use the `SpotifyAPI.Web` package that's already installed to interact with Spotify's API using the stored access tokens.
