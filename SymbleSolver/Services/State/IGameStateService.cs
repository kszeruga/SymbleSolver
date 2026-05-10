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
    bool IsComputing { get; }

    Task InitializeAsync();
    Task AddGuessAsync(Guess guess);
    Task AddGuessWithWordAsync(string word);   // auto-grade: generates feedback automatically
    Task RemoveLastGuessAsync();
    Task UpdateSymbolMappingAsync(SymbolMapping mapping);
    Task SetSolverModeAsync(SolverMode mode);
    Task SetRankerTypeAsync(RankerType rankerType);
    Task SetSecretAnswerAsync(string? answer);
    Task ResetAsync();
    IReadOnlySet<SymbolType> GetPossibleMeanings(int symbolIndex);
}
