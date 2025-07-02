using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

using KXMapStudio.Core;

namespace KXMapStudio.App.Services;

public class PackService
{
    public async Task<PackLoadResult> LoadPackAsync(string path)
    {
        var originalRawContent = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(path))
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is ".taco" or ".zip")
            {
                using var archive = ZipFile.OpenRead(path);
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    var entryFullName = entry.FullName.Replace('\\', '/');
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    originalRawContent[entryFullName] = ms.ToArray();
                }
            }
            else if (extension == ".xml")
            {
                originalRawContent[Path.GetFileName(path)] = await File.ReadAllBytesAsync(path);
            }
        }
        else if (Directory.Exists(path))
        {
            var files = Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(path, file).Replace('\\', '/');
                originalRawContent[relativePath] = await File.ReadAllBytesAsync(file);
            }
        }
        else
        {
            throw new FileNotFoundException("The specified pack, file, or directory was not found.", path);
        }

        var xmlDocuments = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<PackLoadError>();

        var markersByFile = new Dictionary<string, List<Marker>>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileEntry in originalRawContent.Where(kvp => kvp.Key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var markersForThisFile = new List<Marker>();
                markersByFile[fileEntry.Key] = markersForThisFile;

                using var stream = new MemoryStream(fileEntry.Value);
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                var doc = await XDocument.LoadAsync(reader, LoadOptions.PreserveWhitespace, CancellationToken.None);
                xmlDocuments[fileEntry.Key] = doc;

                var poisNode = doc.Descendants(TacoXmlConstants.PoisElement).FirstOrDefault();
                if (poisNode != null)
                {
                    foreach (var poiNode in poisNode.Elements(TacoXmlConstants.PoiElement))
                    {
                        var marker = CreateMarkerFromNode(poiNode, fileEntry.Key);
                        if (marker != null)
                        {
                            markersForThisFile.Add(marker);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new PackLoadError { FileName = fileEntry.Key, ErrorMessage = $"XML Parse Error: {ex.Message}" });
            }
        }

        var rootCategory = new Category { InternalName = "root", DisplayName = Path.GetFileNameWithoutExtension(path) };

        // Build the category tree from all loaded XML documents.
        foreach (var docEntry in xmlDocuments)
        {
            var overlayData = docEntry.Value.Element(TacoXmlConstants.OverlayDataElement);
            if (overlayData == null)
            {
                continue;
            }

            foreach (var categoryNode in overlayData.Elements(TacoXmlConstants.MarkerCategoryElement))
            {
                MergeCategoryRecursive(categoryNode, rootCategory, docEntry.Key);
            }
        }

        // Assign markers to their respective categories.
        foreach (var fileMarkers in markersByFile.Values)
        {
            foreach (var marker in fileMarkers)
            {
                var destinationCategory = FindOrCreateCategoryByNamespace(rootCategory, marker.Type);
                destinationCategory.Markers.Add(marker);
                marker.EnableChangeTracking();
            }
        }

        var loadedPack = new LoadedMarkerPack
        {
            FilePath = path,
            RootCategory = rootCategory,
            OriginalRawContent = originalRawContent,
            XmlDocuments = xmlDocuments,
            MarkersByFile = markersByFile
        };

        var result = new PackLoadResult { LoadedPack = loadedPack };
        result.Errors.AddRange(errors);
        return result;
    }

    private Marker? CreateMarkerFromNode(XElement poiNode, string sourceFile)
    {
        var guidString = poiNode.Attribute(TacoXmlConstants.GuidAttribute)?.Value;
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

        return new Marker
        {
            Guid = markerGuid,
            MapId = int.TryParse(poiNode.Attribute(TacoXmlConstants.MapIdAttribute)?.Value, out var mid) ? mid : 0,
            X = double.TryParse(poiNode.Attribute(TacoXmlConstants.XPosAttribute)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : 0,
            Y = double.TryParse(poiNode.Attribute(TacoXmlConstants.YPosAttribute)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var y) ? y : 0,
            Z = double.TryParse(poiNode.Attribute(TacoXmlConstants.ZPosAttribute)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var z) ? z : 0,
            Type = poiNode.Attribute(TacoXmlConstants.TypeAttribute)?.Value ?? string.Empty,
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
