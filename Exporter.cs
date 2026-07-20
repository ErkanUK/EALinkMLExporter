using System.Text;

namespace EA17LinkMLExporter;

internal static class Exporter
{
    public static string Export(EA.Repository repository, EA.Package package, string parentDirectory)
    {
        var model = EaModelReader.Read(repository, package);
        var stem = SafeFileName(package.Name);
        var directory = Path.Combine(parentDirectory, stem);
        Directory.CreateDirectory(directory);
        var yamlName = stem + ".linkml.yaml";
        var mdName = stem + ".md";
        var drawIoName = stem + ".drawio";
        File.WriteAllText(Path.Combine(directory, yamlName), LinkMlWriter.Write(model), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, drawIoName), DiagramWriter.WriteDrawIo(model), new UTF8Encoding(false));
        var eaDiagrams = EaDiagramSvgExporter.Export(repository, package, directory);
        string? fallbackSvgName = null;
        if (eaDiagrams.Count == 0)
        {
            fallbackSvgName = stem + ".svg";
            File.WriteAllText(Path.Combine(directory, fallbackSvgName), DiagramWriter.WriteSvg(model), new UTF8Encoding(false));
        }
        File.WriteAllText(Path.Combine(directory, mdName),
            MarkdownWriter.Write(model, drawIoName, fallbackSvgName, yamlName, eaDiagrams), new UTF8Encoding(false));
        return directory;
    }

    private static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "uml-model" : name.Trim();
    }
}
