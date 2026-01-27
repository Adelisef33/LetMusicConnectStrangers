namespace LetMusicConnectStrangers.Models
{
    public class Recommendation
    {

        // ----- Attributes (nouns) -----
        private int userId;
        private int trackId;

        // ----- Constructor -----
        public Recommendation(int userId, int trackId)
        {
            this.userId = userId;
            this.trackId = trackId;
        }

        // ----- Methods (verbs) -----
        public int GetUserId()
        {
            // TODO: add GetUserID() logic
            return userId;
        }

        public int GetTrackId()
        {
            // TODO: add GetTrackID() logic
            return trackId;
        }

        public void RefreshRecommendation()
        {
            // TODO: add RefreshRecommendation() logic
        }

    }
}
