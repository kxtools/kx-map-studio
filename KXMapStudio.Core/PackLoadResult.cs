namespace KXMapStudio.Core
{
    public class PackLoadResult
    {
        public required LoadedMarkerPack LoadedPack { get; init; }
        public List<PackLoadError> Errors { get; } = new();
        public bool HasErrors => Errors.Any();
    }
}
