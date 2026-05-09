using SymbleSolver.Engine.Core;
using SymbleSolver.Models;

namespace SymbleSolver.Engine.Ranking;

/// <summary>
/// Ranks guesses by Shannon entropy over the remaining candidate set.
///
/// For each candidate guess the algorithm:
///   1. Simulates what feedback pattern every remaining candidate would produce.
///   2. Counts how many candidates fall into each of the 3^5 = 243 pattern buckets.
///   3. Computes H = -? p·log?(p) — the expected bits of information gained.
///
/// The guess with the highest entropy eliminates the most candidates on average,
/// minimising the expected number of guesses required to reach the answer.
///
/// Performance strategy (WASM single-threaded budget):
///   · candidates ? FullEntropyLimit : evaluate ALL dictionary words as potential guesses.
///   · candidates  > FullEntropyLimit: evaluate candidates + a fixed explorer sample.
///   · When candidates are numerous a random sub-sample is used for the entropy
///     denominator so the O(pool × candidates) work stays within ~50–100 ms.
/// </summary>
public sealed class EntropyGuessRanker : IGuessRanker
{
    // -- Tuning knobs ----------------------------------------------------
    // Evaluate the entire dictionary as the guess pool below this threshold.
    private const int FullEntropyThreshold = 100;
    // Size of candidate sub-sample used for entropy when the set is large.
    private const int LargeCandidateSample  = 150;
    // Extra non-candidate "explorer" words added to the pool for large sets.
    private const int ExplorerPoolSize      = 150;

    private readonly IFeedbackEvaluator _evaluator;

    public EntropyGuessRanker(IFeedbackEvaluator evaluator) => _evaluator = evaluator;

    // ?? Public API ????????????????????????????????????????????????????????

    public IEnumerable<GuessRanking> Rank(
        IReadOnlyCollection<string> candidates,
        IReadOnlySet<string>        allWords,
        int topN = 10)
    {
        if (candidates.Count == 0) return [];

        // Only one possibility left — it must be the answer.
        if (candidates.Count == 1)
            return [new GuessRanking(candidates.First(),
                                     double.PositiveInfinity, true, "only candidate")];

        // Special case: exactly 2 candidates.
        // Any word that produces different feedback for the two will have entropy = 1 bit.
        // Hundreds of dictionary words tie at 1.00 bits, flooding the results.
        // Solution: just return the 2 candidates themselves — one of them is the answer.
        if (candidates.Count == 2)
        {
            return candidates.Select(w => new GuessRanking(w, 1.0, true, "final two"));
        }

        var candidateList = candidates as IList<string> ?? [.. candidates];
        var candidateSet  = candidates as IReadOnlySet<string>
                            ?? new HashSet<string>(candidates, StringComparer.Ordinal);

        // Sub-sample used as the entropy denominator for large sets.
        var entropyBase = candidateList.Count <= FullEntropyThreshold
            ? candidateList
            : EvenlySampled(candidateList, LargeCandidateSample);

        var pool = BuildPool(candidateList, allWords, candidateSet);

        return pool
            .Select(word =>
            {
                double h          = Entropy(word, entropyBase);
                bool   isCandidate = candidateSet.Contains(word);
                // Tiny tie-break: prefer a word that could itself be the answer.
                double score = h + (isCandidate ? 1e-9 : 0);
                return new GuessRanking(word, Math.Round(h, 4), isCandidate,
                                        isCandidate ? "candidate" : "explorer");
            })
            .OrderByDescending(r => r.Score)
            .Take(topN);
    }

    // ?? Pool selection ????????????????????????????????????????????????????

    private IEnumerable<string> BuildPool(
        IList<string>        candidates,
        IReadOnlySet<string> allWords,
        IReadOnlySet<string> candidateSet)
    {
        // Few candidates ? exhaustive search over the whole dictionary.
        if (candidates.Count <= FullEntropyThreshold)
            return allWords;

        // Many candidates ? candidates themselves + a fixed explorer sample.
        var pool = new HashSet<string>(candidates, StringComparer.Ordinal);
        int added = 0;
        foreach (var word in allWords)
        {
            if (!candidateSet.Contains(word))
            {
                pool.Add(word);
                if (++added >= ExplorerPoolSize) break;
            }
        }
        return pool;
    }

    // ?? Entropy computation ???????????????????????????????????????????????

    private double Entropy(string guess, IList<string> candidates)
    {
        // 3^5 = 243 possible feedback patterns; use stack memory to avoid GC.
        Span<int> buckets = stackalloc int[243];

        foreach (var candidate in candidates)
            buckets[EncodePattern(candidate, guess)]++;

        double h     = 0;
        double total = candidates.Count;
        foreach (var count in buckets)
        {
            if (count == 0) continue;
            double p = count / total;
            h -= p * Math.Log2(p);
        }
        return h;
    }

    /// <summary>
    /// Encodes the 5-position feedback of (answer, guess) as a base-3 integer
    /// in [0, 242]: Exact=0, Present=1, Absent=2.
    /// </summary>
    private int EncodePattern(string answer, string guess)
    {
        var feedback = _evaluator.ComputeFeedback(answer, guess);
        int code = 0;
        for (int i = 0; i < 5; i++)
            code = code * 3 + (int)feedback[i];
        return code;
    }

    // ?? Helpers ???????????????????????????????????????????????????????????

    /// <summary>
    /// Returns <paramref name="n"/> elements spread evenly through
    /// <paramref name="source"/> without random seed (deterministic).
    /// </summary>
    private static IList<string> EvenlySampled(IList<string> source, int n)
    {
        if (source.Count <= n) return source;
        int step   = source.Count / n;
        var result = new List<string>(n);
        for (int i = 0; i < n; i++)
            result.Add(source[i * step]);
        return result;
    }
}
