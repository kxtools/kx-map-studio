using KXMapStudio.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KXMapStudio.App.Services.Pack;

public class PackLoader
{
    private readonly MarkerXmlParser _markerXmlParser;
    private readonly CategoryBuilder _categoryBuilder;

    public PackLoader(MarkerXmlParser markerXmlParser, CategoryBuilder categoryBuilder)
    {
        _markerXmlParser = markerXmlParser;
        _categoryBuilder = categoryBuilder;
    }

    public async Task<PackLoadResult> LoadPackFromMemoryAsync(Dictionary<string, byte[]> originalRawContent, string path, bool isArchive)
    {
        var xmlDocuments = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<PackLoadError>();
        var markersByFile = new Dictionary<string, List<Marker>>(StringComparer.OrdinalIgnoreCase);

        var loadedPack = new LoadedMarkerPack
        {
            FilePath = path,
            IsArchive = isArchive,
            RootCategory = new Category { InternalName = "root", DisplayName = Path.GetFileNameWithoutExtension(path) },
            OriginalRawContent = originalRawContent,
            XmlDocuments = xmlDocuments,
            MarkersByFile = markersByFile
        };
        var result = new PackLoadResult { LoadedPack = loadedPack };
        result.Errors.AddRange(errors);

        foreach (var fileEntry in originalRawContent.Where(kvp => kvp.Key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var markersForThisFile = new List<Marker>();
                markersByFile[fileEntry.Key] = markersForThisFile;

                var unmanagedElementsForThisFile = new List<XElement>();
                loadedPack.UnmanagedPois[fileEntry.Key] = unmanagedElementsForThisFile;

                using var stream = new MemoryStream(fileEntry.Value);
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                var doc = await XDocument.LoadAsync(reader, LoadOptions.PreserveWhitespace, CancellationToken.None);
                xmlDocuments[fileEntry.Key] = doc;

                var poisNode = doc.Descendants()
                                  .FirstOrDefault(e => e.Name.LocalName.Equals(TacoXmlConstants.PoisElement, StringComparison.OrdinalIgnoreCase));

                if (poisNode != null)
                {
                    foreach (var element in poisNode.Elements())
                    {
                        if (element.Name.LocalName.Equals(TacoXmlConstants.PoiElement, StringComparison.OrdinalIgnoreCase))
                        {
                            var marker = _markerXmlParser.CreateMarkerFromNode(element, fileEntry.Key);
                            if (marker != null)
                            {
                                markersForThisFile.Add(marker);
                            }
                            else
                            {
                                unmanagedElementsForThisFile.Add(element);
                            }
                        }
                        else
                        {
                            unmanagedElementsForThisFile.Add(element);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new PackLoadError { FileName = fileEntry.Key, ErrorMessage = $"XML Parse Error: {ex.Message}" });
            }
        }

        var rootCategory = loadedPack.RootCategory;

        foreach (var docEntry in xmlDocuments)
        {
            var overlayData = docEntry.Value.Elements()
                                      .FirstOrDefault(e => e.Name.LocalName.Equals(TacoXmlConstants.OverlayDataElement, StringComparison.OrdinalIgnoreCase));
            if (overlayData == null)
            {
                continue;
            }

            foreach (var categoryNode in overlayData.Elements(TacoXmlConstants.MarkerCategoryElement))
            {
                _categoryBuilder.MergeCategoryRecursive(categoryNode, rootCategory, docEntry.Key);
            }
        }

        foreach (var fileMarkers in markersByFile.Values)
        {
            foreach (var marker in fileMarkers)
            {
                var destinationCategory = _categoryBuilder.FindOrCreateCategoryByNamespace(rootCategory, marker.Type);
                destinationCategory.Markers.Add(marker);
                marker.EnableChangeTracking();
            }
        }

        return result;
    }
}
