namespace SymbleSolver.Models;

public class SymbolDefinition
{
    public int Index { get; set; }
    public string Label { get; set; } = "";
    public SymbolType? MappedType { get; set; }

    public SymbolDefinition Clone() => new()
    {
        Index = Index,
        Label = Label,
        MappedType = MappedType
    };
}
