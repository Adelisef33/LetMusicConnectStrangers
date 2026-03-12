# Commonality and Variability Analysis

## Commonalities
- User must sign into their Spotify account
- User is able to leave a review
- User is able to recieve a recommended song
- User is able to search for a song to review

## Variabilities
- Rating System
  - Why it may change: The system may change to alloow a user to leave a review on a 10 point scale or something different like a thumbs up/down system instead of the 5 point scale.
  - How it is isolated: Creating seperate methods and classes that handle changing from a 5 point scale to a different format

- Track Selection 
  - Why it may change: The UI may change to allow a user view their playlist to find a song to review or one of the exsisting ways to retreive a song may be removed
  - How it is isolated: Creating different classes that handle a specific way to provide tracks (For example: Searching tracks would have its own class)

- Music Provider
  - Why it may change: Other music services like Apple music or Youtube Music could be added and spotify could be removed 
  - How it is isolated: Sepreate the logic for reviews and track search so the features can work without depending on Spotify's API
