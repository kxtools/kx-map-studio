namespace KXMapStudio.Core
{
    /// <summary>
    /// Defines XML constants for TacO marker pack files.
    /// </summary>
    public static class TacoXmlConstants
    {
        // Root and Container Elements
        public const string OverlayDataElement = "OverlayData";
        public const string PoisElement = "POIs";
        public const string MarkerCategoryElement = "MarkerCategory";
        public const string PoiElement = "POI";

        // Category Attributes
        public const string NameAttribute = "name";
        public const string DisplayNameAttribute = "DisplayName";

        public const string IsSeparatorAttribute = "IsSeparator";

        // POI Attributes
        public const string GuidAttribute = "GUID";
        public const string MapIdAttribute = "MapID";
        public const string XPosAttribute = "xpos";
        public const string YPosAttribute = "ypos";
        public const string ZPosAttribute = "zpos";
        public const string TypeAttribute = "type";
    }
}
