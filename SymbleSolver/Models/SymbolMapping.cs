namespace SymbleSolver.Models;

public class SymbolMapping
{
    public SymbolDefinition[] Symbols { get; set; } =
    [
        new SymbolDefinition { Index = 0, Label = "[1]" },
        new SymbolDefinition { Index = 1, Label = "[2]" },
        new SymbolDefinition { Index = 2, Label = "[3]" }
    ];

    public bool IsFullyMapped => Symbols.All(s => s.MappedType.HasValue);

    public SymbolType? GetMeaning(int symbolIndex) =>
        symbolIndex is >= 0 and < 3 ? Symbols[symbolIndex].MappedType : null;

    public int? GetSymbolIndex(SymbolType type) =>
        Array.Find(Symbols, s => s.MappedType == type)?.Index;

    public SymbolMapping Clone() => new()
    {
        Symbols = Symbols.Select(s => s.Clone()).ToArray()
    };

    public static SymbolMapping FromPermutation(int[] perm, string[] labels) => new()
    {
        Symbols =
        [
            new SymbolDefinition { Index = 0, Label = labels[0], MappedType = (SymbolType)perm[0] },
            new SymbolDefinition { Index = 1, Label = labels[1], MappedType = (SymbolType)perm[1] },
            new SymbolDefinition { Index = 2, Label = labels[2], MappedType = (SymbolType)perm[2] }
        ]
    };
}
