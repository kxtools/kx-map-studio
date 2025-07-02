using System.Xml.Linq;

namespace KXMapStudio.Core.Utilities
{
    public static class XElementExtensions
    {
        /// <summary>
        /// Gets an XAttribute from an XElement, ignoring the case of the attribute name.
        /// </summary>
        /// <param name="element">The element to search in.</param>
        /// <param name="attributeName">The case-insensitive name of the attribute.</param>
        /// <returns>The XAttribute if found; otherwise, null.</returns>
        public static XAttribute? AttributeIgnoreCase(this XElement element, string attributeName)
        {
            return element.Attributes()
                .FirstOrDefault(a => a.Name.LocalName.Equals(attributeName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
