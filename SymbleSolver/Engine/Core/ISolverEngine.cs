using SymbleSolver.Models;

namespace SymbleSolver.Engine.Core;

public interface ISolverEngine
{
    IReadOnlyList<string> Candidates { get; }
    IReadOnlyList<GuessRanking> RankedGuesses { get; }
    IReadOnlyList<string> ViablePermutationDescriptions { get; }
    int ViablePermutationCount { get; }
    bool IsMappingResolved { get; }
    SymbolMapping? InferredMapping { get; }

    /// <summary>
    /// Returns the set of meanings still possible for the given symbol index
    /// based on all viable permutations.
    /// </summary>
    IReadOnlySet<SymbolType> GetPossibleMeanings(int symbolIndex);

    /// <summary>
    /// Precomputes and caches the initial best guesses for both ranker types.
    /// Should be called once after the dictionary is loaded.
    /// </summary>
    void PrecomputeInitialGuesses();

    void Recompute(GameState state);
}
