namespace SoundAds.Internal
{
    /// <summary>Everything an <see cref="IAdNetwork"/> needs to fetch one ad.</summary>
    internal readonly struct AdRequest
    {
        public readonly string PlacementId;
        public readonly AdFormat Format;
        public readonly AdMoment Moment;
        public readonly bool Personalized;

        public AdRequest(string placementId, AdFormat format, AdMoment moment, bool personalized)
        {
            PlacementId = placementId;
            Format = format;
            Moment = moment;
            Personalized = personalized;
        }

        public override string ToString() =>
            $"{PlacementId} ({Format}, moment={Moment}, personalized={Personalized})";
    }
}
