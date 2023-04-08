using System.Collections.Concurrent;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


var nextGameId = 0;
var gameDict = new ConcurrentDictionary<int, Game> { };

app.MapGet("/games/all", () => gameDict.Values);

app.MapGet("/games", () =>
{
    var id = Interlocked.Increment(ref nextGameId);

    var game = new MeltingSnowman.Logic.MeltingSnowmanGame();
    var gameToAdd = new Game
    {
        id = id,
        correctWord = game.WordToGuess,
        progress = game.Word,
        numberOfGuesses = 0
    };


    gameDict.TryAdd(gameToAdd.id, gameToAdd);
    return Results.Ok($"/games/{gameToAdd.id}");
});

app.MapGet("/games/{gameId}", (int gameId) =>
{
    if (gameDict.Keys.Contains(gameId))
    {
        var game = new DisplayGame(
            GameId: gameDict[gameId].id,
            Progress: gameDict[gameId].progress,
            NumberOfGuesses: gameDict[gameId].numberOfGuesses
        );
        return Results.Ok(game);
    }
    return Results.NotFound($"Game with id {gameId} not found");
});

app.MapPost("/games/{gameId}", (int gameId, GuessedLetterDTO guessedLetter) =>
{
    if (gameDict.ContainsKey(gameId))
    {
        var game = gameDict.GetValueOrDefault(gameId);

        var gameToPlay = new MeltingSnowman.Logic.MeltingSnowmanGame(game.correctWord);
        gameToPlay.Guess(guessedLetter.GuessedLetter);

        for(int i = 0; i < gameToPlay.Word.Length; i++)
        {
            if (gameToPlay.Word[i] != '.')
            {
                game.progress = game.progress.Remove(i, 1).Insert(i, guessedLetter.GuessedLetter);
            }
        }

        game.numberOfGuesses++;

        var gameToShow = new DisplayGame(
            GameId: game.id,
            Progress: game.progress,
            NumberOfGuesses: game.numberOfGuesses
        );

        gameDict.TryUpdate(gameId, game, game);

        return Results.Ok(gameToShow);

}
    return Results.NotFound($"Game with Id: {gameId} not found");
});

app.Run();

class Game
{
    public int id { get; set; }
    public string correctWord { get; set; } = "";
    public string progress { get; set; } = "";
    public int numberOfGuesses { get; set; }

}

record CreateGameDTO(int GameId, string CorrectWord, string GuessedWord, int NumberOfGuesses) { }

record DisplayGame(int GameId, string Progress, int NumberOfGuesses) { }

record GuessedLetterDTO(string GuessedLetter) { }