# SchackBot (project tree)

This README provides a quick visual overview of the C# solution layout. It is intended to help navigation and planned refactors. The tree below reflects the current repository structure under the solution root.

```
Directory.Build.props
SchackBot.sln

src/
    SchackBot.Engine/
        EngineImpl.cs
        IEngine.cs
        SchackBot.Engine.csproj
        Core/
            Color.cs
            Piece.cs
            PieceType.cs
            Squares.cs
            Move.cs
            MoveFlags.cs
        Utilities/
            MoveUtility.cs
        MoveGeneration/
            MoveGenerator.cs
            PrecomputedMoveData.cs
        Board/
            BoardArray.cs
            Fen.cs
            FenPositionInfo.cs
            Position.cs
        Search/
            //TODO
        Evaluation/
            //TODO

    SchackBot.UciAdapter/
        Program.cs
        SchackBot.UciAdapter.csproj

tests/
    SchackBot.Engine.Tests/
        SchackBot.Engine.Tests.csproj
        Usings.cs
        Core/
            PieceTests.cs
            SquaresTests.cs
        MoveGeneration/
            ContractTests.cs
            StrongerTests.cs
        Positioning/
            PositionFromFen_FieldsAndErrorsTests.cs
            PositionFromFen_StartAndBasicsTests.cs
            PositionMakeMoveTests.cs
            StartPositionTests.cs
```

Notes and quick pointers

- The `src/SchackBot.Engine` project contains the engine implementation and domain model (Core, Positioning, MoveGeneration, Helpers).
- The `src/SchackBot.UciAdapter` project is the adapter that interacts with UCI and should ideally depend only on the engine's public API.
- Tests live under `tests/SchackBot.Engine.Tests` and exercise the engine core and move-generation logic.
