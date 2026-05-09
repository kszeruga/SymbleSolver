namespace SymbleSolver.Models;

public class GameState
{
    public List<Guess> Guesses { get; set; } = [];
    public SymbolMapping SymbolMapping { get; set; } = new();
    public SolverMode SolverMode { get; set; } = SolverMode.MappingUnknown;
    public RankerType RankerType { get; set; } = RankerType.Entropy;

    // ?? Auto-grading mode ????????????????????????????????????????????????
    /// <summary>The secret answer word for auto-grading (5 uppercase letters).</summary>
    public string? SecretAnswer { get; set; }

    /// <summary>
    /// Random symbol-to-meaning assignment active for this game session.
    /// Generated once when SecretAnswer is set, persists until New Game.
    /// Map: symbol index (0,1,2) ? SymbolType.
    /// </summary>
    public int[]? SecretSymbolAssignment { get; set; }

    public bool IsAutoGradeMode => !string.IsNullOrEmpty(SecretAnswer);
}
