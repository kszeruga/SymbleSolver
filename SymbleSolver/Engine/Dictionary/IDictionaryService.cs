namespace SymbleSolver.Engine.Dictionary;

public interface IDictionaryService
{
    Task LoadAsync();
    bool IsLoaded { get; }

    /// <summary>All valid 5-letter guesses (the full word list).</summary>
    IReadOnlySet<string> Words { get; }

    /// <summary>
    /// Valid answer candidates — the full word list minus words ending in S,
    /// except for a curated whitelist of S-ending words that are common answers.
    /// </summary>
    IReadOnlySet<string> AnswerWords { get; }

    bool IsValidWord(string word);
}
