using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;

namespace KXMapStudio.Core;

public partial class Category : ObservableObject
{
    public string InternalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Category? Parent { get; set; }
    public bool IsDefinition { get; set; }
    public bool IsSeparator { get; set; }

    public string SourceFile { get; set; } = string.Empty;

    public List<KeyValuePair<string, string>> Attributes { get; set; } = new();

    

    public string FullName
    {
        get
        {
            if (Parent?.Parent == null)
            {
                return InternalName;
            }
            return $"{Parent.FullName}.{InternalName}";
        }
    }

    public ObservableCollection<Marker> Markers { get; set; } = new();
    public ObservableCollection<Category> SubCategories { get; set; } = new();

    public IEnumerable<Marker> GetAllMarkersRecursively()
    {
        foreach (var marker in Markers)
        {
            yield return marker;
        }
        foreach (var subCategory in SubCategories)
        {
            foreach (var marker in subCategory.GetAllMarkersRecursively())
            {
                yield return marker;
            }
        }
    }

    public IEnumerable<Category> GetAllCategoriesRecursively()
    {
        yield return this;
        foreach (var subCategory in SubCategories)
        {
            foreach (var cat in subCategory.GetAllCategoriesRecursively())
            {
                yield return cat;
            }
        }
    }
}
