using System.Collections.Generic;
using UnityEngine;
using static GameLogic;
using static Board;

public static class Search
{
    public static int MaxDepth = 3;

    private const int INF = 1000000;

    private static int AlphaBeta(int depth, int alpha, int beta)
    {
        if (depth == 0)
        {
            int eval = Evaluation.EvaluateBitboards();
            return isWhiteTurn ? eval : -eval;
        }

        List<Move> moves = BitboardMoveGen.GenerateMovesBitboard();
        if (moves.Count == 0)
        {
            // no legal moves: return large negative/positive value depending on side to move
            return isWhiteTurn ? -INF + (MaxDepth - depth) : INF - (MaxDepth - depth);
        }

        int value = -INF;
        foreach (Move m in moves)
        {
            // use bitboard make/unmake (saves/restores state internally)
            Bitboard.MakeMoveBB(m);
            int score = -AlphaBeta(depth - 1, -beta, -alpha);
            Bitboard.UnmakeMoveBB();

            if (score >= beta) return beta;
            if (score > value) value = score;
            if (score > alpha) alpha = score;
        }

        return value;
    }

    public static Move ChooseComputerMove(List<Move> moves)
    {
        if (moves == null || moves.Count == 0) return default;

        Move bestMove = moves[0];
        int bestScore = -INF;

        foreach (Move m in moves)
        {
            Bitboard.MakeMoveBB(m);
            int score = -AlphaBeta(MaxDepth - 1, -INF, INF);
            Bitboard.UnmakeMoveBB();

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = m;
            }
        }

        return bestMove;
    }
}
