using EA17LinkMLExporter;

Assert(EaDiagramSvgExporter.SafeFileName("Network: Overview") == "Network_ Overview", "safe diagram filename");
Assert(EaDiagramSvgExporter.SafeFileName("...") == "diagram", "empty diagram filename fallback");

var model = new ModelSnapshot { Name = "Network", Version = "1.0", Notes = "" };
var diagrams = new[]
{
    new ExportedDiagram("Network / Asset Health", "diagrams/001-Asset Health.svg"),
    new ExportedDiagram("Network / Load [Planning]", "diagrams/002-Load Planning.svg")
};
string nativeMarkdown = MarkdownWriter.Write(model, "Network.drawio", null, "Network.linkml.yaml", diagrams);
Assert(nativeMarkdown.Contains("## EA diagrams"), "EA diagram section");
Assert(nativeMarkdown.Contains("diagrams/001-Asset%20Health.svg"), "SVG path encoding");
Assert(nativeMarkdown.Contains("![Network / Load (Planning)]"), "Markdown alt text escaping");

string fallbackMarkdown = MarkdownWriter.Write(model, "Network.drawio", "Network.svg", "Network.linkml.yaml", []);
Assert(fallbackMarkdown.Contains("![Generated UML class diagram](Network.svg)"), "generated SVG fallback");

Console.WriteLine("All exporter tests passed.");

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("Failed: " + name);
}
