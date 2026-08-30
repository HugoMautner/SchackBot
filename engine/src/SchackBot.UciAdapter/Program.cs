using SchackBot.Engine;
namespace SchackBot.UciAdapter;

internal static class Program
{
    private static EngineImpl _engine;
    private const string EngineName = "SchackBot";
    private const string Author = "Hugo Mautner";
    private const string LogFilePath = @"d:\Projects\schack\log.txt";

    private const string inPrefix = $" IN  : ";
    private const string outPrefix = $" OUT : ";

    public static void Main(string[] args)
    {
        if (!File.Exists(LogFilePath))
        {
            File.WriteAllText(LogFilePath, null);
        }
        File.WriteAllText(LogFilePath, string.Empty);

        _engine = new EngineImpl();

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            File.AppendAllText(LogFilePath, TimeStamp() + inPrefix + line + Environment.NewLine);

            if (string.IsNullOrWhiteSpace(line)) continue;
            line = line.Trim();

            if (line == "uci")
            {
                string[] output =
                [
                    $"id name {EngineName}",
                    $"id author {Author}",
                    "uciok"
                ];

                foreach (string outputLine in output)
                {
                    Console.WriteLine(outputLine);
                    File.AppendAllText(LogFilePath, TimeStamp() + outPrefix + outputLine + Environment.NewLine);
                }
                Console.Out.Flush();
            }
            else if (line == "isready")
            {
                const string output = "readyok";
                Console.Out.WriteLine(output);
                File.AppendAllText(LogFilePath, TimeStamp() + outPrefix + output + Environment.NewLine);

                Console.Out.Flush();
            }
            else if (line.StartsWith("ucinewgame", StringComparison.Ordinal))
            {
                _engine.NewGame();
            }
            else if (line.StartsWith("position ", StringComparison.Ordinal))
            {
                HandlePosition(line);
            }
            else if (line.StartsWith("go", StringComparison.Ordinal))
            {
                string bestMove = _engine.BestLegalMove();
                string outputLine = $"bestmove {bestMove}";
                Console.Out.WriteLine(outputLine);
                File.AppendAllText(LogFilePath, TimeStamp() + outPrefix + outputLine + Environment.NewLine);

                Console.Out.Flush();
            }
            else if (line == "quit")
            {
                break;
            }
        }
    }

    private static void HandlePosition(string line)
    {
        string[] parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        int movesIndex = Array.IndexOf(parts, "moves");
        if (movesIndex == -1)
        {
            movesIndex = parts.Length;
        }
        string[] positionParts = parts[2..movesIndex];

        string positionType = parts[1];
        switch (positionType)
        {
            case "startpos":
                _engine.SetPositionStartpos();
                break;
            case "fen":
                string fen = string.Join(" ", positionParts);
                _engine.SetPositionFromFen(fen);
                break;
        }

        string[] moves = [.. parts.Skip(movesIndex + 1)];
        if (moves.Length >= 1)
        {
            _engine.MakeMovesFromUci(moves);
        }
    }

    private static string TimeStamp()
    {
        return DateTime.Now.ToLongTimeString();
    }
}
