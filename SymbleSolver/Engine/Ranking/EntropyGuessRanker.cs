using SymbleSolver.Engine.Core;
using SymbleSolver.Models;

namespace SymbleSolver.Engine.Ranking;

/// <summary>
/// Ranks guesses by Shannon entropy over the remaining candidate set.
///
/// For each candidate guess the algorithm:
///   1. Simulates what feedback pattern every remaining candidate would produce.
///   2. Counts how many candidates fall into each of the 3^5 = 243 pattern buckets.
///   3. Computes H = −Σ p·log₂(p) — the expected bits of information gained.
///
/// The guess with the highest entropy eliminates the most candidates on average,
/// minimising the expected number of guesses required to reach the answer.
///
/// Performance optimisations for WASM single-threaded budget:
///   • Zero-allocation inner loop: feedback pattern is computed inline as a base-3
///     integer without allocating arrays — the hot path is pure stack arithmetic.
///   • Bucket array is stack-allocated (stackalloc) to avoid GC pressure.
///   • Pre-computed log₂ lookup table eliminates per-bucket Math.Log2 calls.
///   • Adaptive pool/sample sizing keeps wall-clock time under ~100 ms.
/// </summary>
public sealed class EntropyGuessRanker : IGuessRanker
{
    // -- Tuning knobs --------------------------------------------------------
    // Evaluate the entire dictionary as the guess pool below this threshold.
    private const int FullEntropyThreshold = 200;
    // Size of candidate sub-sample used as entropy denominator for large sets.
    private const int LargeCandidateSample = 300;
    // Extra non-candidate "explorer" words added to the pool for large sets.
    private const int ExplorerPoolSize = 300;
    // Number of feedback pattern buckets: 3^5 = 243.
    private const int BucketCount = 243;

    // Pre-computed log₂ lookup: logTable[n] = n * log₂(n) for n in [1..MaxLogEntry].
    // Allows entropy to be computed as log₂(total) - (1/total) * Σ(nlog₂n).
    private const int MaxLogEntry = 2048;
    private static readonly double[] NLogNTable = BuildNLogNTable();

    private readonly IFeedbackEvaluator _evaluator;

    public EntropyGuessRanker(IFeedbackEvaluator evaluator) => _evaluator = evaluator;

    // ── Public API ──────────────────────────────────────────────────────────

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
        if (candidates.Count == 2)
            return candidates.Select(w => new GuessRanking(w, 1.0, true, "final two"));

        var candidateList = candidates as IList<string> ?? [.. candidates];
        var candidateSet  = candidates as IReadOnlySet<string>
                            ?? new HashSet<string>(candidates, StringComparer.Ordinal);

        // Sub-sample used as the entropy denominator for large sets.
        var entropyBase = candidateList.Count <= LargeCandidateSample
            ? candidateList
            : EvenlySampled(candidateList, LargeCandidateSample);

        var pool = BuildPool(candidateList, allWords, candidateSet);

        return pool
            .Select(word =>
            {
                double h           = Entropy(word, entropyBase);
                bool   isCandidate = candidateSet.Contains(word);
                return new GuessRanking(word, Math.Round(h, 4), isCandidate,
                                        isCandidate ? "candidate" : "explorer");
            })
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.IsCandidate)
            .Take(topN);
    }

    // ── Pool selection ──────────────────────────────────────────────────────

    private static IEnumerable<string> BuildPool(
        IList<string>        candidates,
        IReadOnlySet<string> allWords,
        IReadOnlySet<string> candidateSet)
    {
        // Few candidates → exhaustive search over the whole dictionary.
        if (candidates.Count <= FullEntropyThreshold)
            return allWords;

        // Many candidates → candidates themselves + a fixed explorer sample.
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

    // ── Entropy computation (zero-alloc hot path) ───────────────────────────

    private double Entropy(string guess, IList<string> candidates)
    {
        Span<int> buckets = stackalloc int[BucketCount];

        foreach (var candidate in candidates)
            buckets[EncodePatternInline(candidate, guess)]++;

        int total = candidates.Count;

        // H = log₂(total) - (1/total) * Σ(n·log₂(n)) for non-zero buckets.
        // This avoids dividing each bucket by total and calling log₂ per bucket.
        double sumNLogN = 0;
        foreach (var n in buckets)
        {
            if (n == 0) continue;
            sumNLogN += NLogN(n);
        }

        return Math.Log2(total) - sumNLogN / total;
    }

    /// <summary>
    /// Computes the feedback pattern as a base-3 integer [0..242] entirely on the
    /// stack — no array allocations. Implements the standard two-pass Wordle algorithm:
    ///   Pass 1: mark exact matches (answer[i] == guess[i]).
    ///   Pass 2: for remaining positions, check for present letters using letter counts.
    ///
    /// Encoding: Exact=0, Present=1, Absent=2 (matches SymbolType enum order).
    /// </summary>
    private static int EncodePatternInline(string answer, string guess)
    {
        // We track remaining (unconsumed) answer letter counts after exact matches.
        // Using a fixed 26-int span avoids dictionary/hashset overhead.
        Span<int> answerCounts = stackalloc int[26];

        // Also track which guess positions were exact (so we skip them in pass 2).
        // Pack into a single int as bit flags (only 5 bits needed).
        int exactMask = 0;

        // Pass 1: exact matches
        for (int i = 0; i < 5; i++)
        {
            if (answer[i] == guess[i])
            {
                exactMask |= (1 << i);
            }
            else
            {
                // Only count answer letters that are NOT exact-matched
                answerCounts[answer[i] - 'A']++;
            }
        }

        // Pass 2: build the base-3 code
        int code = 0;
        for (int i = 0; i < 5; i++)
        {
            int digit;
            if ((exactMask & (1 << i)) != 0)
            {
                digit = 0; // Exact
            }
            else
            {
                int letterIdx = guess[i] - 'A';
                if (answerCounts[letterIdx] > 0)
                {
                    digit = 1; // Present
                    answerCounts[letterIdx]--;
                }
                else
                {
                    digit = 2; // Absent
                }
            }
            code = code * 3 + digit;
        }

        return code;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns n·log₂(n), using a lookup table for small n to avoid Math.Log2 calls.
    /// </summary>
    private static double NLogN(int n)
    {
        if (n < MaxLogEntry) return NLogNTable[n];
        return n * Math.Log2(n);
    }

    private static double[] BuildNLogNTable()
    {
        var table = new double[MaxLogEntry];
        table[0] = 0;
        for (int i = 1; i < MaxLogEntry; i++)
            table[i] = i * Math.Log2(i);
        return table;
    }

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
