using SymbleSolver.Engine.Core;
using SymbleSolver.Models;

namespace SymbleSolver.Engine.Filtering;

public class CandidateFilter : ICandidateFilter
{
    private readonly IFeedbackEvaluator _evaluator;

    public CandidateFilter(IFeedbackEvaluator evaluator) => _evaluator = evaluator;

    public IEnumerable<string> Filter(IEnumerable<string> words, IEnumerable<Guess> guesses, SymbolMapping mapping)
    {
        var guessList = guesses.ToList();
        if (guessList.Count == 0) return words;
        return words.Where(w => guessList.All(g => _evaluator.IsConsistent(w, g, mapping)));
    }
}
