using SymbleSolver.Models;

namespace SymbleSolver.Engine.Ranking;

public interface IGuessRanker
{
    IEnumerable<GuessRanking> Rank(
        IReadOnlyCollection<string> candidates,
        IReadOnlySet<string> allWords,
        int topN = 10);
}
