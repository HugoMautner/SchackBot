using UnityEngine;

public static class BoardCoord
{
    public static Vector3 Origin = Vector3.zero;
    public static float TileSize = 1f;
    public static bool WhiteIsBottom = true;

    public static bool TryWorldToSquare(Vector3 worldPos, out int square)
    {
        Vector3 localPos = worldPos - Origin;
        int file = Mathf.FloorToInt(localPos.x / TileSize);
        int rank = Mathf.FloorToInt(localPos.z / TileSize);
        if (!InBounds(file, rank))
        {
            square = -1; // Invalid square
            return false;
        }
        if (!WhiteIsBottom)
        {
            file = 7 - file;
            rank = 7 - rank;
        }
        square = rank * 8 + file;
        return true;
    }
    public static Vector3 SquareToWorld(int square)
    {
        int file = square % 8;
        int rank = square / 8;
        if (!WhiteIsBottom)
        {
            file = 7 - file;
            rank = 7 - rank;
        }

        float halfTile = TileSize * 0.5f;
        float cx = Origin.x + (file * TileSize) + halfTile;
        float cz = Origin.z + (rank * TileSize) + halfTile;
        return new Vector3(cx, Origin.y, cz);
    }

    public static bool TryRayToBoard(Ray ray, out Vector3 world)
    {
        Plane boardPlane = new Plane(Vector3.up, new Vector3(0f, Origin.y, 0f));
        if (boardPlane.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }
        else
        {
            world = Vector3.zero;
            return false;
        }
    }

    public static string SquareToAlgebraic(int square)
    {
        int file = square % 8;
        int rank = square / 8;
        char fileChar = (char)('a' + file);
        char rankChar = (char)('1' + rank);
        return $"{fileChar}{rankChar}";
    }
    public static bool TryAlgebraicToSquare(string algebraic, out int square)
    {
        square = -1;
        if (string.IsNullOrEmpty(algebraic) || algebraic.Length != 2) { return false; }

        char fileChar = char.ToLowerInvariant(algebraic[0]);
        char rankChar = algebraic[1];

        if (!IsValidAlgebraic(fileChar, rankChar)) { return false; }

        int file = fileChar - 'a';
        int rank = rankChar - '1';
        square = rank * 8 + file;
        return true;
    }

    public static void Configure(Vector3 origin, float tileSize, bool whiteIsBottom)
    {
        Origin = origin;
        TileSize = tileSize;
        WhiteIsBottom = whiteIsBottom;
    }

    public static bool InBounds(int file, int rank) =>
        (uint)file <= 7 && (uint)rank <= 7;
    public static bool IsValidSquareIndex(int square) =>
        (uint)square < 64;
    public static bool IsValidAlgebraic(char fileChar, char rankChar) =>
        fileChar >= 'a' && fileChar <= 'h' && rankChar >= '1' && rankChar <= '8';
}
