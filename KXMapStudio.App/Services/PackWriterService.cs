using System.Globalization;
using System.Xml.Linq;

using KXMapStudio.Core;

namespace KXMapStudio.App.Services;

public class PackWriterService
{
    /// <summary>
    /// Rewrites the POIs section of an XML document with a new set of markers,
    /// preserving any unmanaged elements like trails.
    /// </summary>
    /// <param name="doc">The XML document to modify.</param>
    /// <param name="markersInOrder">The ordered list of markers to write.</param>
    /// <param name="unmanagedElements">A list of other elements (e.g. trails) to preserve.</param>
    public void RewritePoisSection(XDocument doc, IEnumerable<Marker> markersInOrder, IEnumerable<XElement> unmanagedElements)
    {
        var overlayData = doc.Element(TacoXmlConstants.OverlayDataElement);
        if (overlayData == null)
        {
            // If the root is lowercase, find it that way.
            overlayData = doc.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(TacoXmlConstants.OverlayDataElement, StringComparison.OrdinalIgnoreCase));
            if (overlayData == null)
            {
                overlayData = new XElement(TacoXmlConstants.OverlayDataElement);
                doc.Add(overlayData);
            }
        }

        var poisNode = overlayData.Element(TacoXmlConstants.PoisElement);
        if (poisNode == null)
        {
            // Case-insensitive search
            poisNode = overlayData.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(TacoXmlConstants.PoisElement, StringComparison.OrdinalIgnoreCase));
            if (poisNode == null)
            {
                poisNode = new XElement(TacoXmlConstants.PoisElement);
                overlayData.Add(poisNode);
            }
        }

        poisNode.RemoveNodes();

        string parentIndent = (poisNode.PreviousNode as XText)?.Value ?? "\n  ";
        string childIndent = parentIndent.Contains('\n') ? parentIndent + "  " : "  ";

        // First, write all the markers we manage.
        foreach (var marker in markersInOrder)
        {
            var newPoiElement = new XElement(TacoXmlConstants.PoiElement);

            newPoiElement.Add(new XAttribute(TacoXmlConstants.MapIdAttribute, marker.MapId.ToString()));
            newPoiElement.Add(new XAttribute(TacoXmlConstants.XPosAttribute, marker.X.ToString("F4", CultureInfo.InvariantCulture)));
            newPoiElement.Add(new XAttribute(TacoXmlConstants.YPosAttribute, marker.Y.ToString("F4", CultureInfo.InvariantCulture)));
            newPoiElement.Add(new XAttribute(TacoXmlConstants.ZPosAttribute, marker.Z.ToString("F4", CultureInfo.InvariantCulture)));

            if (!string.IsNullOrEmpty(marker.Type))
            {
                newPoiElement.Add(new XAttribute(TacoXmlConstants.TypeAttribute, marker.Type));
            }

            newPoiElement.Add(new XAttribute(TacoXmlConstants.GuidAttribute, Convert.ToBase64String(marker.Guid.ToByteArray())));

            poisNode.Add(new XText(childIndent), newPoiElement);
        }

        // Next, write back all the unmanaged elements we preserved.
        foreach (var unmanagedElement in unmanagedElements)
        {
            // Add a copy of the element to avoid any shared object issues.
            poisNode.Add(new XText(childIndent), new XElement(unmanagedElement));
        }

        if (markersInOrder.Any() || unmanagedElements.Any())
        {
            poisNode.Add(new XText(parentIndent));
        }
    }
}
