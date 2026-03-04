


## Review 

*Responsibilities*
- Allows a user to leave their feedback on a track ID
- Store's the user's rating and if they left a comment
- Tracks the time and date the review was poasted

*Collaborators*
ApplicationUser



## SpotifyService 

*Responsibilities*
- Refreshes the Spotify token (This grants the user a new token if needed)
- Retrieves a users profile data
- Allows the user to interact with the API (Users top songs or recently played tracks)


*Collaborators*
ApplicationUser
Review



## ApplicationUser

*Responsibilities*
-Links a users Spotify profile data
- Maintains a users access to the API (This determines if the user had a valid token)
- Stores a users information

*Collaborators*
SpotifyService



## Reccomendation 

*Responsibilities*
- Stores track information (listening history, genre, reviewed songs)
- Shares track ID's a user may like
- Tracks a user's action towards the recommended song

*Collaborators*
-ApplicationUser
-SpotifyService
