using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Evaluation
{
    public static readonly int Queen = 900;
    public static readonly int Rook = 500;
    public static readonly int Bishop = 320;
    public static readonly int Knight = 300;
    public static readonly int Pawn = 100;

    private static Dictionary<int, int> pieceValues = new Dictionary<int, int>
        {
            { Piece.White | Piece.Pawn, Pawn},
            { Piece.White | Piece.Knight, Knight},
            { Piece.White | Piece.Bishop, Bishop},
            { Piece.White | Piece.Rook, Rook},
            { Piece.White | Piece.Queen, Queen},
            { Piece.Black | Piece.Pawn, -Pawn},
            { Piece.Black | Piece.Knight, -Knight},
            { Piece.Black | Piece.Bishop, -Bishop},
            { Piece.Black | Piece.Rook, -Rook},
            { Piece.Black | Piece.Queen, -Queen}
        };

    public static int Evaluate(int[] position)
    {
        int materialBalance = 0;
        foreach (int piece in position)
        {
            if (piece == Piece.None) continue;
            materialBalance += pieceValues[piece];
        }

        return materialBalance;
    }
}
