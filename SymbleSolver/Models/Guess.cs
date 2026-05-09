namespace SymbleSolver.Models;

public class Guess
{
    /// <summary>5 uppercase letters — the word the player typed into the game.</summary>
    public required string Word { get; set; }

    /// <summary>
    /// Symbol indices (0–2) for each of the 5 feedback positions.
    /// Index corresponds to answer position, matching the inverted feedback logic:
    /// feedback[i] describes how answer[i] relates to the guess.
    /// </summary>
    public required int[] Feedback { get; set; }
}
