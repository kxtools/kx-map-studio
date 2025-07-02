using System.Globalization;
using System.Xml.Linq;

using KXMapStudio.Core;

namespace KXMapStudio.App.Services;

public class PackWriterService
{
    /// <summary>
    /// Rewrites the POIs section of an XML document with a new set of markers.
    /// </summary>
    /// <param name="doc">The XML document to modify.</param>
    /// <param name="markersInOrder">The ordered list of markers to write.</param>
    public void RewritePoisSection(XDocument doc, IEnumerable<Marker> markersInOrder)
    {
        var overlayData = doc.Element(TacoXmlConstants.OverlayDataElement);
        if (overlayData == null)
        {
            overlayData = new XElement(TacoXmlConstants.OverlayDataElement);
            doc.Add(overlayData);
        }

        var poisNode = overlayData.Element(TacoXmlConstants.PoisElement);
        if (poisNode == null)
        {
            poisNode = new XElement(TacoXmlConstants.PoisElement);
            overlayData.Add(poisNode);
        }

        poisNode.RemoveNodes();

        string parentIndent = (poisNode.PreviousNode as XText)?.Value ?? "\n  ";
        string childIndent = parentIndent.Contains('\n') ? parentIndent + "  " : "  ";

        foreach (var marker in markersInOrder)
        {
            // Create the element and add attributes in a consistent order for clean output.
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

        if (markersInOrder.Any())
        {
            poisNode.Add(new XText(parentIndent));
        }
    }
}
