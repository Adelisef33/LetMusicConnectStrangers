# Spotify Integration - Setup Checklist

## Pre-Setup Checklist
- [x] Code implementation complete
- [x] Build successful
- [ ] Read QUICKSTART.md
- [ ] Read IMPLEMENTATION_SUMMARY.md

## Spotify Developer Setup
- [ ] Create Spotify Developer account at https://developer.spotify.com/dashboard
- [ ] Create new app in Spotify Dashboard
- [ ] Copy Client ID
- [ ] Copy Client Secret
- [ ] Configure Redirect URIs:
  - [ ] Add `https://localhost:XXXX/signin-spotify` (your HTTPS port)
  - [ ] Add `http://localhost:XXXX/signin-spotify` (your HTTP port)
- [ ] Save settings in Spotify Dashboard

## Application Configuration
- [ ] Update `appsettings.json` with Client ID
- [ ] Update `appsettings.json` with Client Secret
- [ ] Consider using User Secrets for development
- [ ] Plan for production secret management (Azure Key Vault, etc.)

## Database Migration
- [ ] Open Package Manager Console in Visual Studio
- [ ] Run `Add-Migration AddSpotifyFieldsToApplicationUser`
- [ ] Review the migration file
- [ ] Run `Update-Database`
- [ ] Verify database tables updated (check AspNetUsers table for new columns)

## Testing
- [ ] Run the application
- [ ] Test Registration Flow:
  - [ ] Navigate to Register page
  - [ ] Click "Connect with Spotify"
  - [ ] Authorize the app on Spotify
  - [ ] Complete registration with email/password
  - [ ] Verify account created successfully
- [ ] Test Login Flow:
  - [ ] Log out
  - [ ] Try login with email/password
  - [ ] Log out
  - [ ] Try login with Spotify button
- [ ] Test Spotify Profile Page:
  - [ ] Navigate to `/SpotifyProfile`
  - [ ] Verify your Spotify name shows
  - [ ] Verify top tracks display
  - [ ] Verify top artists display

## Troubleshooting (if needed)
- [ ] Check redirect URI matches exactly (including port)
- [ ] Verify Spotify credentials are correct
- [ ] Check database migration ran successfully
- [ ] Review browser console for errors
- [ ] Check application logs

## Optional Enhancements
- [ ] Set up User Secrets for local development
- [ ] Add Spotify profile picture to user profile
- [ ] Implement token refresh logic
- [ ] Add error handling for API rate limits
- [ ] Create user matching based on music taste
- [ ] Add playlist creation features
- [ ] Implement recently played tracks feature

## Production Deployment
- [ ] Move secrets to Azure Key Vault or environment variables
- [ ] Update Redirect URIs in Spotify Dashboard for production URL
- [ ] Test OAuth flow in production environment
- [ ] Monitor token expiration and refresh
- [ ] Set up logging for Spotify API calls
- [ ] Consider implementing caching for API responses

## Documentation
- [ ] Document any custom changes made
- [ ] Update team on new Spotify features
- [ ] Create user guide for Spotify integration
- [ ] Document API rate limits and best practices

---

## Quick Reference

**Spotify Dashboard**: https://developer.spotify.com/dashboard

**Redirect URI Format**: `https://localhost:PORT/signin-spotify`

**Test Page**: `/SpotifyProfile` (after logging in)

**Configuration File**: `appsettings.json`

**Main Service**: `Services/SpotifyService.cs`

**User Model**: `Models/ApplicationUser.cs`

---

## Need Help?

- **Setup Issues**: See `QUICKSTART.md`
- **Code Examples**: See `IMPLEMENTATION_SUMMARY.md`
- **Spotify API Docs**: https://developer.spotify.com/documentation/web-api/
- **SpotifyAPI.Web Package**: https://github.com/JohnnyCrazy/SpotifyAPI-NET

---

**Status**: Ready to configure and test! ??
