using KXMapStudio.Core;
using KXMapStudio.Core.Utilities;

using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace KXMapStudio.App.Services.Pack;

public class PackLoader
{
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
                            var marker = CreateMarkerFromNode(element, fileEntry.Key);
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
                MergeCategoryRecursive(categoryNode, rootCategory, docEntry.Key);
            }
        }

        foreach (var fileMarkers in markersByFile.Values)
        {
            foreach (var marker in fileMarkers)
            {
                var destinationCategory = FindOrCreateCategoryByNamespace(rootCategory, marker.Type);
                destinationCategory.Markers.Add(marker);
                marker.EnableChangeTracking();
            }
        }

        return result;
    }

    private Marker? CreateMarkerFromNode(XElement poiNode, string sourceFile)
    {
        var guidString = poiNode.AttributeIgnoreCase(TacoXmlConstants.GuidAttribute)?.Value;
        Guid markerGuid = Guid.Empty;
        if (!string.IsNullOrEmpty(guidString))
        {
            try { markerGuid = new Guid(Convert.FromBase64String(guidString)); }
            catch (FormatException) { Guid.TryParse(guidString, out markerGuid); }
        }
        if (markerGuid == Guid.Empty)
        {
            markerGuid = Guid.NewGuid();
        }

        var xPosAttr = poiNode.AttributeIgnoreCase(TacoXmlConstants.XPosAttribute);
        var yPosAttr = poiNode.AttributeIgnoreCase(TacoXmlConstants.YPosAttribute);
        if (xPosAttr == null || yPosAttr == null)
        {
            return null; 
        }

        return new Marker
        {
            Guid = markerGuid,
            MapId = int.TryParse(poiNode.AttributeIgnoreCase(TacoXmlConstants.MapIdAttribute)?.Value, out var mid) ? mid : 0,
            X = double.TryParse(xPosAttr.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : 0,
            Y = double.TryParse(yPosAttr.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var y) ? y : 0,
            Z = double.TryParse(poiNode.AttributeIgnoreCase(TacoXmlConstants.ZPosAttribute)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var z) ? z : 0,
            Type = poiNode.AttributeIgnoreCase(TacoXmlConstants.TypeAttribute)?.Value ?? string.Empty,
            SourceFile = sourceFile,
            IsDirty = false
        };
    }

    private void MergeCategoryRecursive(XElement categoryNode, Category parent, string sourceFile)
    {
        var internalName = categoryNode.Attribute(TacoXmlConstants.NameAttribute)?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(internalName))
        {
            return;
        }

        var ourCategory = parent.SubCategories.FirstOrDefault(c => c.InternalName.Equals(internalName, StringComparison.OrdinalIgnoreCase));
        if (ourCategory == null)
        {
            ourCategory = new Category { InternalName = internalName, Parent = parent };
            parent.SubCategories.Add(ourCategory);
        }
        ourCategory.IsDefinition = true;
        ourCategory.SourceFile = sourceFile;
        ourCategory.DisplayName = categoryNode.Attribute(TacoXmlConstants.DisplayNameAttribute)?.Value ?? internalName;
        ourCategory.IsSeparator = categoryNode.Attribute(TacoXmlConstants.IsSeparatorAttribute)?.Value == "1";
        ourCategory.Attributes = categoryNode.Attributes().Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value)).ToList();
        foreach (var subNode in categoryNode.Elements(TacoXmlConstants.MarkerCategoryElement))
        {
            MergeCategoryRecursive(subNode, ourCategory, sourceFile);
        }
    }

    private Category FindOrCreateCategoryByNamespace(Category root, string fullNamespace)
    {
        if (string.IsNullOrEmpty(fullNamespace))
        {
            return root;
        }

        var pathParts = fullNamespace.Split('.');
        Category current = root;
        foreach (var part in pathParts)
        {
            var next = current.SubCategories.FirstOrDefault(c => c.InternalName.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (next == null)
            {
                next = new Category { InternalName = part, DisplayName = part, Parent = current };
                current.SubCategories.Add(next);
            }
            current = next;
        }
        return current;
    }
}
