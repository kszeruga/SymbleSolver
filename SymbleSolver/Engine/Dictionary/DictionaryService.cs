namespace SymbleSolver.Engine.Dictionary;

public class DictionaryService : IDictionaryService
{
    // S-ending words that are legitimate answers despite the general exclusion rule.
    private static readonly HashSet<string> SEndingAnswerWhitelist = new(StringComparer.Ordinal)
    {
        "ABYSS", "AMASS", "AMISS", "BASIS", "BLESS", "BLISS", "BONUS", "BRASS",
        "CHAOS", "CHESS", "CLASS", "CRASS", "CRESS", "CROSS", "DRESS", "DROSS",
        "ETHOS", "FETUS", "FICUS", "FLOSS", "FOCUS", "GLASS", "GLOSS", "GRASS",
        "GROSS", "GUESS", "HUMUS", "LOCUS", "LUPUS", "MINUS", "MUCUS", "PRESS",
        "REBUS", "TORUS", "TRUSS", "VIRUS"
    };

    private readonly string _baseAddress;
    private HashSet<string> _words       = [];
    private HashSet<string> _answerWords = [];

    public DictionaryService(string baseAddress) => _baseAddress = baseAddress;

    public bool IsLoaded { get; private set; }
    public IReadOnlySet<string> Words       => _words;
    public IReadOnlySet<string> AnswerWords => _answerWords;

    public async Task LoadAsync()
    {
        if (IsLoaded) return;

        using var http = new HttpClient { BaseAddress = new Uri(_baseAddress) };
        var text = await http.GetStringAsync("data/word_list.txt");

        _words = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToUpperInvariant())
            .Where(w => w.Length == 5 && w.All(char.IsLetter))
            .ToHashSet(StringComparer.Ordinal);

        // Answer candidates = all words that don't end in S,
        // plus the explicitly whitelisted S-ending words.
        _answerWords = _words
            .Where(w => !w.EndsWith('S') || SEndingAnswerWhitelist.Contains(w))
            .ToHashSet(StringComparer.Ordinal);

        IsLoaded = true;
    }

    public bool IsValidWord(string word) =>
        _words.Contains(word.Trim().ToUpperInvariant());
}
