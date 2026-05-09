using SymbleSolver.Engine.Core;
using SymbleSolver.Engine.Dictionary;
using SymbleSolver.Models;

namespace SymbleSolver.Services.State;

public class GameStateService : IGameStateService
{
    private readonly IDictionaryService _dictionary;
    private readonly ISolverEngine _solver;

    public event Action? OnChanged;

    public GameState State { get; private set; } = new();
    public IReadOnlyList<string> Candidates => _solver.Candidates;
    public IReadOnlyList<GuessRanking> RankedGuesses => _solver.RankedGuesses;
    public IReadOnlyList<string> ViablePermutationDescriptions => _solver.ViablePermutationDescriptions;
    public int ViablePermutationCount => _solver.ViablePermutationCount;
    public bool IsMappingResolved => _solver.IsMappingResolved;
    public SymbolMapping? InferredMapping => _solver.InferredMapping;
    public bool IsLoading { get; private set; }

    public GameStateService(IDictionaryService dictionary, ISolverEngine solver)
    {
        _dictionary = dictionary;
        _solver = solver;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        Notify();

        await _dictionary.LoadAsync();
        Recompute();

        IsLoading = false;
        Notify();
    }

    public void AddGuess(Guess guess)
    {
        State.Guesses.Add(guess);
        Recompute();
        Notify();
    }

    public void RemoveLastGuess()
    {
        if (State.Guesses.Count > 0)
        {
            State.Guesses.RemoveAt(State.Guesses.Count - 1);
            Recompute();
            Notify();
        }
    }

    public void UpdateSymbolMapping(SymbolMapping mapping)
    {
        State.SymbolMapping = mapping;
        Recompute();
        Notify();
    }

    public void SetSolverMode(SolverMode mode)
    {
        State.SolverMode = mode;
        Recompute();
        Notify();
    }

    public void SetRankerType(RankerType type)
    {
        State.RankerType = type;
        Recompute();
        Notify();
    }

    public void Reset()
    {
        var mapping    = State.SymbolMapping.Clone();
        var mode       = State.SolverMode;
        var rankerType = State.RankerType;
        State = new GameState { SymbolMapping = mapping, SolverMode = mode, RankerType = rankerType };
        Recompute();
        Notify();
    }

    private void Recompute() => _solver.Recompute(State);
    private void Notify() => OnChanged?.Invoke();

    public IReadOnlySet<SymbolType> GetPossibleMeanings(int symbolIndex) =>
        _solver.GetPossibleMeanings(symbolIndex);
}
