using UnityEngine;

[DisallowMultipleComponent]
public class BoardPieceRegistry : MonoBehaviour
{
    private MovablePiece[] _bySquare = new MovablePiece[64];

    public MovablePiece GetAt(int square) =>
        (uint)square <= 63 ? _bySquare[square] : null;

    public void Register(MovablePiece piece, int square)
    {
        if ((uint)square > 63) return;
        _bySquare[square] = piece;
    }

    public void Move(int fromSquare, int toSquare)
    {
        if ((uint)fromSquare > 63 || (uint)toSquare > 63) return;
        _bySquare[toSquare] = _bySquare[fromSquare];
        _bySquare[fromSquare] = null;
    }

    public void RemoveAt(int square)
    {
        if ((uint)square > 63) return;
        _bySquare[square] = null;
    }
}
