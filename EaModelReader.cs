using System.Net;
using System.Text.RegularExpressions;

namespace EA17LinkMLExporter;

internal static class EaModelReader
{
    public static ModelSnapshot Read(EA.Repository repository, EA.Package root)
    {
        // Try to get version from package tagged values
        string packageVersion = "";
        for (int i = 0; i < root.TaggedValues.Count; i++)
        {
            EA.TaggedValue tv = root.TaggedValues.GetAt(i);
            if (tv.Name.Equals("version", StringComparison.OrdinalIgnoreCase))
            {
                packageVersion = tv.Value ?? "";
                break;
            }
        }

        var model = new ModelSnapshot
        {
            Name = root.Name,
            Notes = CleanNotes(root.Notes),
            Version = packageVersion
        };
        ReadPackage(repository, root, root.Name, model);
        ReadRelations(repository, model);
        return model;
    }

    private static void ReadPackage(EA.Repository repository, EA.Package package, string path, ModelSnapshot model)
    {
        foreach (EA.Element element in package.Elements)
        {
            if (IsEnumeration(element))
            {
                var item = new UmlEnum 
                { 
                    Id = element.ElementID, 
                    Name = element.Name, 
                    Notes = CleanNotes(element.Notes),
                    Color = ExtractColor(element)
                };
                foreach (EA.Attribute attribute in element.Attributes)
                    item.Values.Add(attribute.Name);
                model.Enums.Add(item);
                continue;
            }

            if (!IsClass(element)) continue;
            
            // Try to get version from tagged values if element.Version is empty
            string version = element.Version ?? "";
            if (string.IsNullOrWhiteSpace(version))
            {
                // Try to get version from tagged values
                for (int i = 0; i < element.TaggedValues.Count; i++)
                {
                    EA.TaggedValue tv = element.TaggedValues.GetAt(i);
                    if (tv.Name.Equals("version", StringComparison.OrdinalIgnoreCase))
                    {
                        version = tv.Value ?? "";
                        break;
                    }
                }
            }
            
            var cls = new UmlClass
            {
                Id = element.ElementID,
                Name = element.Name,
                QualifiedName = path + "::" + element.Name,
                Notes = CleanNotes(element.Notes),
                Version = version,
                Abstract = element.Abstract == "1",
                Color = ExtractColor(element)
            };
            foreach (EA.Attribute attribute in element.Attributes)
            {
                var type = attribute.Type;
                if (attribute.ClassifierID > 0)
                {
                    try { type = repository.GetElementByID(attribute.ClassifierID).Name; } catch { /* retain EA type */ }
                }
                cls.Properties.Add(new UmlProperty
                {
                    Name = attribute.Name,
                    Type = type,
                    Notes = CleanNotes(attribute.Notes),
                    Lower = string.IsNullOrWhiteSpace(attribute.LowerBound) ? "0" : attribute.LowerBound,
                    Upper = string.IsNullOrWhiteSpace(attribute.UpperBound) ? "1" : attribute.UpperBound,
                    Derived = attribute.IsDerived,
                    ReadOnly = attribute.IsConst
                });
            }
            model.Classes.Add(cls);
        }

        foreach (EA.Package child in package.Packages)
            ReadPackage(repository, child, path + "::" + child.Name, model);
    }

    private static void ReadRelations(EA.Repository repository, ModelSnapshot model)
    {
        var ids = model.Classes.Select(x => x.Id).Concat(model.Enums.Select(x => x.Id)).ToHashSet();
        var seen = new HashSet<int>();
        foreach (var source in model.Classes)
        {
            EA.Element element = repository.GetElementByID(source.Id);
            foreach (EA.Connector connector in element.Connectors)
            {
                if (!seen.Add(connector.ConnectorID) || !ids.Contains(connector.ClientID) || !ids.Contains(connector.SupplierID))
                    continue;
                var client = repository.GetElementByID(connector.ClientID);
                var supplier = repository.GetElementByID(connector.SupplierID);
                if (connector.Type.Equals("Generalization", StringComparison.OrdinalIgnoreCase))
                {
                    var child = model.Classes.FirstOrDefault(x => x.Id == connector.ClientID);
                    if (child is not null) child.Parents.Add(supplier.Name);
                    continue;
                }

                if (!connector.Type.Equals("Association", StringComparison.OrdinalIgnoreCase) &&
                    !connector.Type.Equals("Aggregation", StringComparison.OrdinalIgnoreCase)) continue;

                model.Relations.Add(new UmlRelation
                {
                    Kind = connector.Type,
                    SourceId = connector.ClientID,
                    TargetId = connector.SupplierID,
                    SourceName = client.Name,
                    TargetName = supplier.Name,
                    SourceRole = connector.ClientEnd.Role,
                    TargetRole = connector.SupplierEnd.Role,
                    SourceMultiplicity = DefaultMultiplicity(connector.ClientEnd.Cardinality),
                    TargetMultiplicity = DefaultMultiplicity(connector.SupplierEnd.Cardinality),
                    Notes = CleanNotes(connector.Notes),
                    Composition = connector.ClientEnd.Aggregation == 2 || connector.SupplierEnd.Aggregation == 2
                });
            }
        }
    }

    private static bool IsClass(EA.Element element) =>
        element.Type.Equals("Class", StringComparison.OrdinalIgnoreCase) && !IsEnumeration(element);

    private static bool IsEnumeration(EA.Element element) =>
        element.Type.Equals("Enumeration", StringComparison.OrdinalIgnoreCase) ||
        element.Stereotype.Equals("enumeration", StringComparison.OrdinalIgnoreCase);

    private static string DefaultMultiplicity(string value) => string.IsNullOrWhiteSpace(value) ? "0..1" : value;

    private static string? ExtractColor(EA.Element element)
    {
        // EA stores colors as RGB hex in the FillColor property
        // Returns null if not set or converts to hex format
        try
        {
            if (!string.IsNullOrWhiteSpace(element.FillColor))
            {
                // FillColor is typically a hex string like "CCFFFF" or can be an RGB integer
                string color = element.FillColor.Trim();
                if (color.Length > 0 && !color.Equals("16777215", StringComparison.OrdinalIgnoreCase)) // 16777215 is white (default)
                {
                    // If it's numeric, convert to hex; otherwise assume it's already hex
                    if (int.TryParse(color, out int rgb))
                    {
                        return "#" + rgb.ToString("X6");
                    }
                    else if (color.Length == 6 || (color.StartsWith("#") && color.Length == 7))
                    {
                        return color.StartsWith("#") ? color : "#" + color;
                    }
                }
            }
        }
        catch { /* return null on error */ }
        return null;
    }

    internal static string CleanNotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var decoded = WebUtility.HtmlDecode(value.Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</p>", "\n", StringComparison.OrdinalIgnoreCase));
        return Regex.Replace(decoded, "<[^>]+>", "").Replace("\r", "").Trim();
    }
}
