namespace EA17LinkMLExporter;

internal sealed class ModelSnapshot
{
    public required string Name { get; init; }
    public required string Notes { get; init; }
    public List<UmlClass> Classes { get; } = [];
    public List<UmlEnum> Enums { get; } = [];
    public List<UmlRelation> Relations { get; } = [];
}

internal sealed class UmlClass
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string QualifiedName { get; init; }
    public required string Notes { get; init; }
    public string? Version { get; init; }
    public bool Abstract { get; init; }
    public List<UmlProperty> Properties { get; } = [];
    public List<string> Parents { get; } = [];
}

internal sealed class UmlEnum
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Notes { get; init; }
    public List<string> Values { get; } = [];
}

internal sealed class UmlProperty
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Notes { get; init; }
    public required string Lower { get; init; }
    public required string Upper { get; init; }
    public bool Derived { get; init; }
    public bool ReadOnly { get; init; }
}

internal sealed class UmlRelation
{
    public required string Kind { get; init; }
    public required int SourceId { get; init; }
    public required int TargetId { get; init; }
    public required string SourceName { get; init; }
    public required string TargetName { get; init; }
    public required string SourceRole { get; init; }
    public required string TargetRole { get; init; }
    public required string SourceMultiplicity { get; init; }
    public required string TargetMultiplicity { get; init; }
    public required string Notes { get; init; }
    public bool Composition { get; init; }
}
