// IEngine.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SchackBot.Engine;

public class SearchParams
{
    public int? Depth;
    public int? MoveTimeMs;
    public long? WhiteTimeMs;
    public long? BlackTimeMs;
    // add fields if you want winc/binc/nodes constraint etc.
}

public class SearchInfo
{
    public int Depth;
    public int SelDepth;
    public int ScoreCp;   // centipawn score
    public long Nodes;
    public string Pv;     // principal variation as UCI moves separated by spaces
}

public class SearchResult
{
    public string BestMoveUci;
    public string? BestMovePonderUci;
}

public interface IEngine
{
    void NewGame();
    void SetPositionFromFen(string fen);
    void SetPositionStartpos();
    void MakeMovesFromUci(IEnumerable<string> uciMoves);
    Task<SearchResult> StartSearchAsync(SearchParams pars, CancellationToken token, IProgress<SearchInfo> progress);
    IEnumerable<(string name, string type, string defaultValue)> GetOptions();
    void SetOption(string name, string value);
    void PonderHit();
}
