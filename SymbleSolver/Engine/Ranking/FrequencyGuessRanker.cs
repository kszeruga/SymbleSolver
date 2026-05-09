using SymbleSolver.Models;

namespace SymbleSolver.Engine.Ranking;

/// <summary>
/// Ranks guesses by positional letter frequency among current candidates.
/// Words that cover the most common positional letters score highest.
/// Actual candidates receive a 20% bonus over pure explorer words.
/// </summary>
public class FrequencyGuessRanker : IGuessRanker
{
    public IEnumerable<GuessRanking> Rank(
        IReadOnlyCollection<string> candidates,
        IReadOnlySet<string> allWords,
        int topN = 10)
    {
        if (candidates.Count == 0) return [];

        // Ensure O(1) membership test.
        // IReadOnlySet is already O(1); List.Contains is O(n) ? convert once.
        var candidateSet = candidates as IReadOnlySet<string>
                           ?? new HashSet<string>(candidates, StringComparer.Ordinal);

        // Positional frequency table built from remaining candidates
        var posFreq = new int[5, 26];
        foreach (var word in candidates)
            for (int i = 0; i < 5; i++)
                posFreq[i, word[i] - 'A']++;

        // Score a word using a bitmask for seen-letter deduplication.
        // Avoids allocating a HashSet<char> per word (14 k × alloc was expensive).
        double Score(string word)
        {
            int seen = 0; // 26-bit mask, one bit per letter
            double score = 0;
            for (int i = 0; i < 5; i++)
            {
                int bit = 1 << (word[i] - 'A');
                if ((seen & bit) == 0)       // first occurrence of this letter
                {
                    seen  |= bit;
                    score += posFreq[i, word[i] - 'A'];
                }
            }
            if (candidateSet.Contains(word)) score *= 1.2;
            return score;
        }

        // When many candidates remain, explore the full dictionary so we can
        // surface high-value opener words; otherwise restrict to candidates.
        var pool = candidates.Count > 2 ? allWords : candidates;

        return pool
            .Select(w => new GuessRanking(w, Score(w), candidateSet.Contains(w),
                         candidateSet.Contains(w) ? "candidate" : "explorer"))
            .OrderByDescending(r => r.Score)
            .Take(topN);
    }
}
