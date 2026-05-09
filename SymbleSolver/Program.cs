using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SymbleSolver;
using SymbleSolver.Engine.Core;
using SymbleSolver.Engine.Dictionary;
using SymbleSolver.Engine.Filtering;
using SymbleSolver.Engine.Inference;
using SymbleSolver.Engine.Ranking;
using SymbleSolver.Services.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Engine
var baseAddress = builder.HostEnvironment.BaseAddress;
builder.Services.AddSingleton<IDictionaryService>(_ => new DictionaryService(baseAddress));
builder.Services.AddSingleton<IFeedbackEvaluator, FeedbackEvaluator>();
builder.Services.AddSingleton<ICandidateFilter, CandidateFilter>();
builder.Services.AddSingleton<MappingPermutationTracker>();
builder.Services.AddSingleton<FrequencyGuessRanker>();
builder.Services.AddSingleton<EntropyGuessRanker>();
builder.Services.AddSingleton<ISolverEngine, SolverEngine>();

// State
builder.Services.AddSingleton<IGameStateService, GameStateService>();

await builder.Build().RunAsync();
