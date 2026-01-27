# Nouns and Verbs for LetMusicConnectStrangers

## Entities 
- Review
  - verbs: create, edit, update, delete, list, view
  
- ApplicationUser 
  - verbs: register, login, logout, spotify link, refresh token, get profile
	
- Track 
  - verbs: search, select, preview
 
- RecentlyPlayed / TopTracks
  - verbs: load, display
	
- ReviewCollection / Reviews page
  - verbs: load, paginate, filter, sort
	
- Recommendation
  - verbs: generate, fetch, display, accept, dismiss, save, refresh


## Roles / Actors
- User 
  - verbs: signup, signin, create review, edit review, delete review, search tracks, view reviews
- Admin 
  - verbs: manage users, moderate reviews, view logs

## Attributes 
- SpotifyTrackId 
- ArtistName 
- AlbumName
- AlbumImageUrl
- Rating 
- CreatedAt 
- UpdatedAt 
- UserId 
- RecommendationId
- RecommendedTrackIds 
- Score 
- Genre


## System / Technical 
- Database 
- SpotifyService
- Identity 
- Reviews 
- RecommendationService
