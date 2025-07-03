using KXMapStudio.Core;
using KXMapStudio.Core.Utilities;
using System.Xml.Linq;

namespace KXMapStudio.App.Services.Pack;

public class CategoryBuilder
{
    public void MergeCategoryRecursive(XElement categoryNode, Category parent, string sourceFile)
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

    public Category FindOrCreateCategoryByNamespace(Category root, string fullNamespace)
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
