using SymbleSolver.Models;

namespace SymbleSolver.Engine.Filtering;

public interface ICandidateFilter
{
    IEnumerable<string> Filter(IEnumerable<string> words, IEnumerable<Guess> guesses, SymbolMapping mapping);
}
