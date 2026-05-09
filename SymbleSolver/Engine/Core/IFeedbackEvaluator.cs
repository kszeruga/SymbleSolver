using SymbleSolver.Models;

namespace SymbleSolver.Engine.Core;

public interface IFeedbackEvaluator
{
    /// <summary>
    /// Computes the full 5-position feedback array for a given answer/guess pair,
    /// correctly handling duplicate letters using a two-pass consume algorithm:
    ///
    ///   Pass 1 — mark EXACT positions (answer[i] == guess[i]).
    ///            Each matched guess letter is marked as consumed.
    ///
    ///   Pass 2 — for each remaining answer position, scan left-to-right through
    ///            unconsumed guess letters. First match ? PRESENT (consume it).
    ///            No match ? ABSENT.
    ///
    /// This matches the standard Wordle duplicate-letter semantics and is required
    /// for correct filtering when the answer or guess contains repeated letters.
    /// </summary>
    SymbolType[] ComputeFeedback(string answer, string guess);

    /// <summary>
    /// Returns true when <paramref name="candidate"/> is consistent with the observed
    /// <paramref name="guess"/> feedback under the given <paramref name="mapping"/>.
    /// Positions whose symbol has no mapped meaning are skipped (treated as unknown).
    /// </summary>
    bool IsConsistent(string candidate, Guess guess, SymbolMapping mapping);
}
