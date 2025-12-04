@echo off
echo ============================================
echo Spotify User Secrets Setup
echo ============================================
echo.
echo This script will help you configure your Spotify API credentials.
echo.
echo Before running this script, make sure you have:
echo 1. Created a Spotify app at https://developer.spotify.com/dashboard
echo 2. Set the Redirect URI to: https://localhost:7096/signin-spotify
echo 3. Copied your Client ID and Client Secret
echo.
echo ============================================
echo.

set /p clientId="Enter your Spotify Client ID: "
set /p clientSecret="Enter your Spotify Client Secret: "

echo.
echo Setting user secrets...
echo.

dotnet user-secrets set "Spotify:ClientId" "%clientId%" --project LetMusicConnectStrangers.csproj
dotnet user-secrets set "Spotify:ClientSecret" "%clientSecret%" --project LetMusicConnectStrangers.csproj

echo.
echo ============================================
echo User secrets configured successfully!
echo ============================================
echo.
echo Next steps:
echo 1. Restart your application if it's running
echo 2. Navigate to the Register page
echo 3. Test the Spotify login flow
echo.
pause
