using System.Xml.Linq;

namespace KXMapStudio.Core
{
    public class LoadedMarkerPack
    {
        public required string FilePath { get; set; }

        public bool IsArchive { get; set; }

        public required Category RootCategory { get; init; }
        public required Dictionary<string, byte[]> OriginalRawContent { get; init; }
        public required Dictionary<string, XDocument> XmlDocuments { get; init; }

        public required Dictionary<string, List<Marker>> MarkersByFile { get; init; }

        public Dictionary<string, List<XElement>> UnmanagedPois { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<Marker> AddedMarkers { get; } = new();
        public HashSet<Marker> DeletedMarkers { get; } = new();

        public bool HasUnsavedChangesFor(string? documentPath)
        {
            if (string.IsNullOrEmpty(documentPath))
            {
                return false;
            }

            bool hasAdded = AddedMarkers.Any(m => m.SourceFile.Equals(documentPath, StringComparison.OrdinalIgnoreCase));
            if (hasAdded)
            {
                return true;
            }

            bool hasDeleted = DeletedMarkers.Any(m => m.SourceFile.Equals(documentPath, StringComparison.OrdinalIgnoreCase));
            if (hasDeleted)
            {
                return true;
            }

            if (MarkersByFile.TryGetValue(documentPath, out var markers) && markers.Any(m => m.IsDirty))
            {
                return true;
            }

            return false;
        }
    }
}
