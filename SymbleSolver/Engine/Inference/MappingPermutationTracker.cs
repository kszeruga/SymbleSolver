using SymbleSolver.Engine.Filtering;
using SymbleSolver.Models;

namespace SymbleSolver.Engine.Inference;

/// <summary>
/// Tracks which symbol?meaning permutations are still logically viable given the
/// observed guesses. When only one permutation remains viable the mapping is inferred.
///
/// There are 3! = 6 permutations of assigning (Exact, Present, Absent) to the three
/// symbols (index 0, 1, 2). A permutation is viable as long as at least one dictionary
/// word is consistent with every observed guess under that assignment.
/// </summary>
public class MappingPermutationTracker
{
    // All 6 permutations: each int[3] maps symbol index ? (int)SymbolType
    private static readonly int[][] AllPermutations = BuildPermutations();

    private readonly ICandidateFilter _filter;
    private string[] _labels = ["A", "B", "C"];

    public MappingPermutationTracker(ICandidateFilter filter) => _filter = filter;

    public List<int[]> ViablePermutations { get; private set; } = [.. AllPermutations];

    public bool IsResolved => ViablePermutations.Count == 1;

    public SymbolMapping? GetResolvedMapping() =>
        IsResolved ? MappingFromPermutation(ViablePermutations[0]) : null;

    /// <summary>
    /// Re-evaluates which permutations are still viable given updated guesses.
    /// </summary>
    public void Update(IReadOnlySet<string> allWords, IEnumerable<Guess> guesses, string[] labels)
    {
        _labels = labels;
        var guessList = guesses.ToList();

        if (guessList.Count == 0)
        {
            ViablePermutations = [.. AllPermutations];
            return;
        }

        ViablePermutations = AllPermutations
            .Where(perm => _filter.Filter(allWords, guessList, MappingFromPermutation(perm)).Any())
            .ToList();
    }

    /// <summary>
    /// Returns the union of candidates across all still-viable permutations.
    /// A word is a possible answer if it is consistent under at least one viable mapping.
    /// </summary>
    public IReadOnlyList<string> GetCandidates(IReadOnlySet<string> allWords, IEnumerable<Guess> guesses)
    {
        var guessList = guesses.ToList();
        var result = new HashSet<string>();

        foreach (var perm in ViablePermutations)
        {
            foreach (var word in _filter.Filter(allWords, guessList, MappingFromPermutation(perm)))
                result.Add(word);
        }

        return [.. result.OrderBy(w => w)];
    }

    public IReadOnlyList<string> GetViableDescriptions() =>
        ViablePermutations
            .Select(p => $"{_labels[0]}={TypeLabel(p[0])}  {_labels[1]}={TypeLabel(p[1])}  {_labels[2]}={TypeLabel(p[2])}")
            .ToList();

    private SymbolMapping MappingFromPermutation(int[] perm) =>
        SymbolMapping.FromPermutation(perm, _labels);

    private static string TypeLabel(int t) => (SymbolType)t switch
    {
        SymbolType.Exact   => "EXACT",
        SymbolType.Present => "PRESENT",
        SymbolType.Absent  => "ABSENT",
        _                  => "?"
    };

    private static int[][] BuildPermutations()
    {
        var result = new List<int[]>();
        int[] types = [0, 1, 2];
        foreach (var a in types)
            foreach (var b in types)
                foreach (var c in types)
                    if (a != b && b != c && a != c)
                        result.Add([a, b, c]);
        return [.. result];
    }

    /// <summary>Resets to all 6 permutations viable (no guesses state).</summary>
    public void Reset(string[] labels)
    {
        _labels = labels;
        ViablePermutations = [.. AllPermutations];
    }

    /// <summary>
    /// Returns the set of SymbolType meanings that are still possible for
    /// <paramref name="symbolIndex"/> across all viable permutations.
    /// One entry  ? meaning is fully inferred for this symbol.
    /// Two entries ? two possibilities remain.
    /// Three entries ? fully ambiguous (no guesses yet, or no information).
    /// </summary>
    public IReadOnlySet<SymbolType> GetPossibleMeanings(int symbolIndex) =>
        ViablePermutations
            .Select(p => (SymbolType)p[symbolIndex])
            .ToHashSet();
}
