using KXMapStudio.Core;
using KXMapStudio.Core.Utilities;
using System.Globalization;
using System.Xml.Linq;

namespace KXMapStudio.App.Services.Pack;

public class MarkerXmlParser
{
    public Marker? CreateMarkerFromNode(XElement poiNode, string sourceFile)
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
}
