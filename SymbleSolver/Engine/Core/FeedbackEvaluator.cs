using SymbleSolver.Models;

namespace SymbleSolver.Engine.Core;

public class FeedbackEvaluator : IFeedbackEvaluator
{
    public SymbolType[] ComputeFeedback(string answer, string guess)
    {
        // Must explicitly fill with Absent — default(SymbolType) is Exact (value 0),
        // NOT Absent (value 2), so leaving the array zero-initialised is wrong.
        var result   = new SymbolType[5];
        Array.Fill(result, SymbolType.Absent);
        var consumed = new bool[5];          // tracks which guess positions are used

        // Pass 1 — EXACT matches; consume the guess letter so it can't be reused.
        for (int i = 0; i < 5; i++)
        {
            if (answer[i] == guess[i])
            {
                result[i]   = SymbolType.Exact;
                consumed[i] = true;
            }
        }

        // Pass 2 — PRESENT check for remaining answer positions.
        // Scan unconsumed guess letters left-to-right; take the first match.
        for (int i = 0; i < 5; i++)
        {
            if (result[i] == SymbolType.Exact) continue;

            for (int j = 0; j < 5; j++)
            {
                if (!consumed[j] && guess[j] == answer[i])
                {
                    result[i]   = SymbolType.Present;
                    consumed[j] = true;   // this guess letter is now used up
                    break;
                }
            }
            // result[i] stays Absent if no unconsumed match was found
        }

        return result;
    }

    public bool IsConsistent(string candidate, Guess guess, SymbolMapping mapping)
    {
        var expected = ComputeFeedback(candidate, guess.Word);
        for (int i = 0; i < 5; i++)
        {
            var meaning = mapping.GetMeaning(guess.Feedback[i]);
            if (!meaning.HasValue) continue;          // symbol not yet mapped — skip
            if (expected[i] != meaning.Value) return false;
        }
        return true;
    }
}
