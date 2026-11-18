
using System.Collections.Generic;

namespace SchackBot.Engine;

public interface IEngine
{
    void NewGame();
    void SetPositionStartpos();
    void SetPositionFromFen(string fen);
    string BestLegalMove();
    void MakeMovesFromUci(IEnumerable<string> uciMoves);
    string CurrentFen();
}
