namespace SymbleSolver.Models;

public class GameState
{
    public List<Guess> Guesses { get; set; } = [];
    public SymbolMapping SymbolMapping { get; set; } = new();
    public SolverMode SolverMode { get; set; } = SolverMode.MappingUnknown;
    public RankerType RankerType { get; set; } = RankerType.Entropy;
}
