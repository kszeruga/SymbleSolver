using SymbleSolver.Engine.Core;
using SymbleSolver.Engine.Dictionary;
using SymbleSolver.Models;

namespace SymbleSolver.Services.State;

public class GameStateService : IGameStateService
{
    private readonly IDictionaryService _dictionary;
    private readonly ISolverEngine _solver;
    private readonly IFeedbackEvaluator _evaluator;
    private readonly Random _rng = new();

    public event Action? OnChanged;

    public GameState State { get; private set; } = new();
    public IReadOnlyList<string> Candidates => _solver.Candidates;
    public IReadOnlyList<GuessRanking> RankedGuesses => _solver.RankedGuesses;
    public IReadOnlyList<string> ViablePermutationDescriptions => _solver.ViablePermutationDescriptions;
    public int ViablePermutationCount => _solver.ViablePermutationCount;
    public bool IsMappingResolved => _solver.IsMappingResolved;
    public SymbolMapping? InferredMapping => _solver.InferredMapping;
    public bool IsLoading { get; private set; }
    public bool IsComputing { get; private set; }

    public GameStateService(IDictionaryService dictionary, ISolverEngine solver, IFeedbackEvaluator evaluator)
    {
        _dictionary = dictionary;
        _solver = solver;
        _evaluator = evaluator;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        Notify();

        await _dictionary.LoadAsync();

        // Precompute initial best guesses while still showing loading state.
        // This is the expensive part — runs entropy over the full dictionary once.
        _solver.PrecomputeInitialGuesses();

        IsLoading = false;
        await RecomputeAsync();
    }

    public async Task AddGuessAsync(Guess guess)
    {
        State.Guesses.Add(guess);
        await RecomputeAsync();
    }

    public async Task AddGuessWithWordAsync(string word)
    {
        if (!State.IsAutoGradeMode || State.SecretAnswer == null)
            throw new InvalidOperationException("Auto-grade mode not active");

        word = word.ToUpperInvariant();
        var feedback = _evaluator.ComputeFeedback(State.SecretAnswer, word);
        var feedbackIndices = new int[5];
        for (int i = 0; i < 5; i++)
            feedbackIndices[i] = Array.IndexOf(State.SecretSymbolAssignment!, (int)feedback[i]);

        await AddGuessAsync(new Guess { Word = word, Feedback = feedbackIndices });
    }

    public async Task RemoveLastGuessAsync()
    {
        if (State.Guesses.Count > 0)
        {
            State.Guesses.RemoveAt(State.Guesses.Count - 1);
            await RecomputeAsync();
        }
    }

    public async Task UpdateSymbolMappingAsync(SymbolMapping mapping)
    {
        State.SymbolMapping = mapping;
        await RecomputeAsync();
    }

    public async Task SetSolverModeAsync(SolverMode mode)
    {
        State.SolverMode = mode;
        await RecomputeAsync();
    }

    public async Task SetRankerTypeAsync(RankerType rankerType)
    {
        State.RankerType = rankerType;
        await RecomputeAsync();
    }

    public async Task SetSecretAnswerAsync(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            State.SecretAnswer = null;
            State.SecretSymbolAssignment = null;
        }
        else
        {
            answer = answer.Trim().ToUpperInvariant();
            if (answer.Length != 5 || !answer.All(char.IsLetter))
                throw new ArgumentException("Answer must be exactly 5 letters");

            State.SecretAnswer = answer;
            // Generate random permutation of [0,1,2] mapping symbol indices to SymbolType values
            State.SecretSymbolAssignment = [0, 1, 2];
            for (int i = 2; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (State.SecretSymbolAssignment[i], State.SecretSymbolAssignment[j]) =
                    (State.SecretSymbolAssignment[j], State.SecretSymbolAssignment[i]);
            }
        }
        await RecomputeAsync();
    }

    public async Task ResetAsync()
    {
        var mapping = State.SymbolMapping.Clone();
        var mode = State.SolverMode;
        var rankerType = State.RankerType;
        State = new GameState
        {
            SymbolMapping = mapping,
            SolverMode = mode,
            RankerType = rankerType
        };
        await RecomputeAsync();
    }

    /// <summary>
    /// Yields to the UI thread so the "Computing…" indicator renders,
    /// then performs the heavy solver work synchronously.
    /// </summary>
    private async Task RecomputeAsync()
    {
        IsComputing = true;
        Notify();

        // Yield so Blazor can render the computing state before we block.
        await Task.Delay(1);

        _solver.Recompute(State);

        IsComputing = false;
        Notify();
    }

    private void Notify() => OnChanged?.Invoke();

    public IReadOnlySet<SymbolType> GetPossibleMeanings(int symbolIndex) =>
        _solver.GetPossibleMeanings(symbolIndex);
}
