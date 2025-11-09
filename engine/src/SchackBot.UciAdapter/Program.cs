// Program.cs - UCI adapter main loop (async, warning-cleaned)
using SchackBot.Engine;

namespace SchackBot.UciAdapter;

class Program
{
    static EngineImpl engine = null!;
    static CancellationTokenSource? searchCts;
    static Task? runningSearch;

    static readonly string UciLogPath = Path.Combine(AppContext.BaseDirectory, "uci_debug.log");

    static Task LogInAsync(string line) =>
        File.AppendAllTextAsync(UciLogPath, $"{DateTime.UtcNow:O} IN  : {line}\n");

    static Task LogOutAsync(string line) =>
        File.AppendAllTextAsync(UciLogPath, $"{DateTime.UtcNow:O} OUT : {line}\n");

    static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string[] args)
    {
        engine = new EngineImpl();

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            // log incoming (best-effort)
            _ = LogInAsync(line); // fire-and-forget logging (do not block the stdin loop)

            if (string.IsNullOrWhiteSpace(line)) continue;
            line = line.Trim();

            if (line == "uci")
            {
                // respond to uci
                await Console.Out.WriteLineAsync($"id name SchackBot").ConfigureAwait(false);
                await Console.Out.WriteLineAsync($"id author Hugo Mautner").ConfigureAwait(false);
                foreach (var opt in engine.GetOptions() ?? Enumerable.Empty<(string, string, string)>())
                {
                    await Console.Out.WriteLineAsync($"option name {opt.name} type {opt.type} default {opt.defaultValue}").ConfigureAwait(false);
                }
                await Console.Out.WriteLineAsync("uciok").ConfigureAwait(false);
                await Console.Out.FlushAsync().ConfigureAwait(false);
            }
            else if (line == "isready")
            {
                await Console.Out.WriteLineAsync("readyok").ConfigureAwait(false);
                await Console.Out.FlushAsync().ConfigureAwait(false);
            }
            else if (line.StartsWith("setoption ", StringComparison.Ordinal))
            {
                var parts = SplitArgs(line);
                var nameIndex = Array.IndexOf(parts, "name");
                var valueIndex = Array.IndexOf(parts, "value");
                if (nameIndex >= 0 && nameIndex + 1 < parts.Length)
                {
                    var name = parts[nameIndex + 1];
                    var value = (valueIndex >= 0) ? string.Join(" ", parts.Skip(valueIndex + 1)) : "";
                    engine.SetOption(name, value);
                }
            }
            else if (line.StartsWith("ucinewgame", StringComparison.Ordinal))
            {
                engine.NewGame();
            }
            else if (line.StartsWith("position ", StringComparison.Ordinal))
            {
                HandlePosition(line);
            }
            else if (line.StartsWith("go", StringComparison.Ordinal))
            {
                var sp = ParseGo(line);
                // cancel previous
                searchCts?.Cancel();
                searchCts = new CancellationTokenSource();
                var token = searchCts.Token;
                var progress = new Progress<SearchInfo>(info => EmitInfo(info));

                runningSearch = Task.Run(async () =>
                {
                    try
                    {
                        var result = await engine.StartSearchAsync(sp, token, progress).ConfigureAwait(false);
                        if (!token.IsCancellationRequested && result != null)
                        {
                            var outLine = $"bestmove {result.BestMoveUci}";
                            if (!string.IsNullOrEmpty(result.BestMovePonderUci))
                                outLine += $" ponder {result.BestMovePonderUci}";

                            _ = LogOutAsync(outLine); // non-blocking log
                            await Console.Out.WriteLineAsync(outLine).ConfigureAwait(false);
                            await Console.Out.FlushAsync().ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // normal stop
                    }
                    catch (Exception ex)
                    {
                        // log to a file (do not write to stdout)
                        _ = File.AppendAllTextAsync(Path.Combine(AppContext.BaseDirectory, "uci_adapter_error.log"), ex + Environment.NewLine);
                    }
                }, token);
            }
            else if (line == "stop")
            {
                searchCts?.Cancel();
                if (runningSearch != null) await runningSearch.ConfigureAwait(false);
            }
            else if (line == "ponderhit")
            {
                engine.PonderHit();
            }
            else if (line == "quit")
            {
                searchCts?.Cancel();
                if (runningSearch != null) await runningSearch.ConfigureAwait(false);
                break;
            }
        }
    }

    static async void EmitInfo(SearchInfo info)
    {
        // best-effort non-blocking: emit info line
        try
        {
            var line = $"info depth {info.Depth} seldepth {info.SelDepth} score cp {info.ScoreCp} nodes {info.Nodes} pv {info.Pv}";
            _ = LogOutAsync(line);
            await Console.Out.WriteLineAsync(line).ConfigureAwait(false);
            await Console.Out.FlushAsync().ConfigureAwait(false);
        }
        catch
        {
            // swallow logging errors
        }
    }

    static void HandlePosition(string line)
    {
        var parts = SplitArgs(line);
        if (parts.Length >= 2 && parts[1] == "startpos")
        {
            engine.SetPositionStartpos();
            var movesIndex = Array.IndexOf(parts, "moves");
            if (movesIndex >= 0)
            {
                var moves = parts.Skip(movesIndex + 1).ToArray();
                engine.MakeMovesFromUci(moves);
            }
        }
        else
        {
            var fenIndex = Array.IndexOf(parts, "fen");
            if (fenIndex >= 0)
            {
                var movesIndex = Array.IndexOf(parts, "moves");
                string fen;
                if (movesIndex >= 0)
                {
                    fen = string.Join(" ", parts.Skip(fenIndex + 1).Take(movesIndex - fenIndex - 1));
                }
                else
                {
                    fen = string.Join(" ", parts.Skip(fenIndex + 1));
                }
                engine.SetPositionFromFen(fen);
                if (movesIndex >= 0)
                {
                    var moves = parts.Skip(movesIndex + 1).ToArray();
                    engine.MakeMovesFromUci(moves);
                }
            }
        }
    }

    static SearchParams ParseGo(string line)
    {
        var parts = SplitArgs(line);
        var sp = new SearchParams();
        for (int i = 1; i < parts.Length; i++)
        {
            string p = parts[i];
            if (p == "depth" && i + 1 < parts.Length) { if (int.TryParse(parts[++i], out var d)) sp.Depth = d; }
            else if (p == "movetime" && i + 1 < parts.Length) { if (int.TryParse(parts[++i], out var t)) sp.MoveTimeMs = t; }
            else if (p == "wtime" && i + 1 < parts.Length) { if (long.TryParse(parts[++i], out var t)) sp.WhiteTimeMs = t; }
            else if (p == "btime" && i + 1 < parts.Length) { if (long.TryParse(parts[++i], out var t)) sp.BlackTimeMs = t; }
        }
        return sp;
    }

    static string[] SplitArgs(string line)
    {
        return line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
    }
}
