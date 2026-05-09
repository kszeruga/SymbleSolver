using SymbleSolver.Engine.Dictionary;
using SymbleSolver.Engine.Filtering;
using SymbleSolver.Engine.Inference;
using SymbleSolver.Engine.Ranking;
using SymbleSolver.Models;

namespace SymbleSolver.Engine.Core;

public class SolverEngine : ISolverEngine
{
    private readonly IDictionaryService _dictionary;
    private readonly ICandidateFilter _filter;
    private readonly MappingPermutationTracker _permTracker;
    private readonly IGuessRanker _ranker;

    public SolverEngine(
        IDictionaryService dictionary,
        ICandidateFilter filter,
        MappingPermutationTracker permTracker,
        IGuessRanker ranker)
    {
        _dictionary = dictionary;
        _filter = filter;
        _permTracker = permTracker;
        _ranker = ranker;
    }

    public IReadOnlyList<string> Candidates { get; private set; } = [];
    public IReadOnlyList<GuessRanking> RankedGuesses { get; private set; } = [];
    public IReadOnlyList<string> ViablePermutationDescriptions => _permTracker.GetViableDescriptions();
    public int ViablePermutationCount => _permTracker.ViablePermutations.Count;
    public bool IsMappingResolved => _permTracker.IsResolved;
    public SymbolMapping? InferredMapping => _permTracker.GetResolvedMapping();

    public void Recompute(GameState state)
    {
        if (!_dictionary.IsLoaded)
        {
            Candidates = [];
            RankedGuesses = [];
            return;
        }

        var answerWords = _dictionary.AnswerWords; // candidates must come from here
        var allWords    = _dictionary.Words;        // full set — valid for guesses & ranker pool
        var guesses     = state.Guesses;
        var labels      = state.SymbolMapping.Symbols.Select(s => s.Label).ToArray();

        if (guesses.Count == 0)
        {
            _permTracker.Reset(labels);
            Candidates = [];   // nothing filtered yet — UI shows a prompt instead

            // Rank using the full answer set so Best Next Guesses shows good openers
            RankedGuesses = [.. _ranker.Rank(answerWords, allWords, 10)];
            return;
        }
        else if (state.SolverMode == SolverMode.MappingUnknown)
        {
            _permTracker.Update(answerWords, guesses, labels);
            Candidates = _permTracker.GetCandidates(answerWords, guesses);
        }
        else
        {
            _permTracker.Update(answerWords, guesses, labels);
            Candidates = [.. _filter.Filter(answerWords, guesses, state.SymbolMapping).OrderBy(w => w)];
        }

        RankedGuesses = [.. _ranker.Rank(Candidates, allWords, 10)];
    }

    public IReadOnlySet<SymbolType> GetPossibleMeanings(int symbolIndex) =>
        _permTracker.GetPossibleMeanings(symbolIndex);
}
