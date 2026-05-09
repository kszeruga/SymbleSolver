using SymbleSolver.Models;

namespace SymbleSolver.Services.State;

public interface IGameStateService
{
    event Action? OnChanged;

    GameState State { get; }
    IReadOnlyList<string> Candidates { get; }
    IReadOnlyList<GuessRanking> RankedGuesses { get; }
    IReadOnlyList<string> ViablePermutationDescriptions { get; }
    int ViablePermutationCount { get; }
    bool IsMappingResolved { get; }
    SymbolMapping? InferredMapping { get; }
    bool IsLoading { get; }

    Task InitializeAsync();
    void AddGuess(Guess guess);
    void RemoveLastGuess();
    void UpdateSymbolMapping(SymbolMapping mapping);
    void SetSolverMode(SolverMode mode);
    void SetRankerType(RankerType type);
    void Reset();
    IReadOnlySet<SymbolType> GetPossibleMeanings(int symbolIndex);
}
