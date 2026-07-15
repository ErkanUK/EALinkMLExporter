using System.Net;
using System.Text.RegularExpressions;

namespace EA17LinkMLExporter;

internal static class EaModelReader
{
    public static ModelSnapshot Read(EA.Repository repository, EA.Package root)
    {
        var model = new ModelSnapshot
        {
            Name = root.Name,
            Notes = CleanNotes(root.Notes)
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
                    Notes = CleanNotes(element.Notes)
                };
                foreach (EA.Attribute attribute in element.Attributes)
                    item.Values.Add(attribute.Name);
                model.Enums.Add(item);
                continue;
            }

            if (!IsClass(element)) continue;
            
            var cls = new UmlClass
            {
                Id = element.ElementID,
                Name = element.Name,
                QualifiedName = path + "::" + element.Name,
                Notes = CleanNotes(element.Notes),
                Version = element.Version,
                Abstract = element.Abstract == "1"
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

    internal static string CleanNotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var decoded = WebUtility.HtmlDecode(value.Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</p>", "\n", StringComparison.OrdinalIgnoreCase));
        return Regex.Replace(decoded, "<[^>]+>", "").Replace("\r", "").Trim();
    }
}
