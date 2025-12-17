using System.Collections.Generic;
using static Board;
using static GameLogic;

public static class BitboardMoveGen
{
    // precomputed attack bitboards for knights and kings
    private static readonly ulong[] KnightAttacks = new ulong[64];
    private static readonly ulong[] KingAttacks = new ulong[64];

    static BitboardMoveGen()
    {
        for (int square = 0; square < 64; square++)
        {
            KnightAttacks[square] = GenerateKnightAttacks(square);
            KingAttacks[square] = GenerateKingAttacks(square);
        }
    }

    private static ulong GenerateKnightAttacks(int square)
    {
        int rank = square / 8;
        int file = square % 8;
        ulong attacks = 0UL;
        int[] offsets = { -17, -15, -10, -6, 6, 10, 15, 17 };
        foreach (int offset in offsets)
        {
            // to means target square
            int to = square + offset;
            if (to < 0 || to >= 64) continue;

            // tr/tf means target square rank/file
            int tr = to / 8; int tf = to % 8;

            // ensure knight move doesn't wrap around files
            if (System.Math.Abs(tr - rank) <= 2 && System.Math.Abs(tf - file) <= 2)
                attacks |= Bitboard.Bit(to);
        }
        return attacks;
    }

    private static ulong GenerateKingAttacks(int square)
    {
        int rank = square / 8;
        int file = square % 8;
        ulong attacks = 0UL;
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int df = -1; df <= 1; df++)
            {
                if (dr == 0 && df == 0) continue;
                int tr = rank + dr; int tf = file + df;
                if (tr >= 0 && tr < 8 && tf >= 0 && tf < 8)
                {
                    attacks |= Bitboard.Bit(tr * 8 + tf);
                }
            }
        }
        return attacks;
    }

    private static bool SameFile(int a, int b) => (a % 8) == (b % 8);
    private static bool SameRank(int a, int b) => (a / 8) == (b / 8);

    public static List<Move> GenerateMovesBitboard()
    {
        List<Move> moves = new List<Move>();
        ulong us = isWhiteTurn ? WhitePiecesBB : BlackPiecesBB;
        ulong them = isWhiteTurn ? BlackPiecesBB : WhitePiecesBB;
        ulong occ = OccupiedBB;

        // Pawns
        ulong pawns = isWhiteTurn ? WhitePawns : BlackPawns;
        int step = isWhiteTurn ? 8 : -8;
        int startingRank = isWhiteTurn ? 1 : 6;  // rank for double move
        int promotionRank = isWhiteTurn ? 7 : 0;

        ulong pawnsCopy = pawns;
        while (pawnsCopy != 0)
        {
            int from = Bitboard.PopLsb(ref pawnsCopy);
            int fromRank = from / 8;

            int one = from + step;
            if (one >= 0 && one < 64 && ((occ & Bitboard.Bit(one)) == 0))
            {
                if (one / 8 == promotionRank)
                {
                    moves.Add(new Move(from, one, Piece.Queen));
                }
                else moves.Add(new Move(from, one));

                int two = from + 2 * step;
                if (fromRank == startingRank && two >= 0 && two < 64 && ((occ & Bitboard.Bit(two)) == 0))
                {
                    // ensure the square between is also empty
                    if ((occ & Bitboard.Bit(one)) == 0)
                        moves.Add(new Move(from, two));
                }
            }

            // captures
            int left = from + (isWhiteTurn ? 7 : -9);
            int right = from + (isWhiteTurn ? 9 : -7);

            if (left >= 0 && left < 64 && !SameFile(from, left) && ((them & Bitboard.Bit(left)) != 0))
            {
                if (left / 8 == promotionRank) moves.Add(new Move(from, left, Piece.Queen)); else moves.Add(new Move(from, left));
            }
            if (right >= 0 && right < 64 && !SameFile(from, right) && ((them & Bitboard.Bit(right)) != 0))
            {
                if (right / 8 == promotionRank) moves.Add(new Move(from, right, Piece.Queen)); else moves.Add(new Move(from, right));
            }

            // en-passant
            if (enPassantTargetSquare >= 0)
            {
                if (left == enPassantTargetSquare || right == enPassantTargetSquare)
                {
                    moves.Add(new Move(from, enPassantTargetSquare));
                }
            }
        }

        // Knights
        ulong knights = isWhiteTurn ? WhiteKnights : BlackKnights;
        ulong knightsCopy = knights;
        while (knightsCopy != 0)
        {
            int from = Bitboard.PopLsb(ref knightsCopy);
            ulong attacks = KnightAttacks[from] & ~us;
            while (attacks != 0)
            {
                int to = Bitboard.PopLsb(ref attacks);
                moves.Add(new Move(from, to));
            }
        }

        // Kings
        ulong kings = isWhiteTurn ? WhiteKing : BlackKing;
        ulong kingsCopy = kings;
        while (kingsCopy != 0)
        {
            int from = Bitboard.PopLsb(ref kingsCopy);
            ulong attacks = KingAttacks[from] & ~us;
            while (attacks != 0)
            {
                int to = Bitboard.PopLsb(ref attacks);
                moves.Add(new Move(from, to));
            }
            // castling omitted in bitboard generator for simplicity
        }

        // Sliding pieces: rooks, bishops, queens (index using Square[] to determine type)
        // We'll iterate occupancy-aware rays using index arithmetic.
        int[] rookDirs = { 8, -8, -1, 1 };
        int[] bishopDirs = { 7, -7, 9, -9 };

        ulong sliders = isWhiteTurn ? (WhiteRooks | WhiteBishops | WhiteQueens) : (BlackRooks | BlackBishops | BlackQueens);
        ulong slidersCopy = sliders;
        while (slidersCopy != 0)
        {
            int from = Bitboard.PopLsb(ref slidersCopy);
            int type = Square[from] & Piece.TypeMask;
            if (type == Piece.Rook || type == Piece.Queen)
            {
                foreach (int dir in rookDirs)
                {
                    int to = from + dir;
                    while (to >= 0 && to < 64 && (dir == 1 || dir == -1 ? SameRank(from, to) : true))
                    {
                        moves.Add(new Move(from, to));
                        if ((OccupiedBB & Bitboard.Bit(to)) != 0) break;
                        to += dir;
                    }
                }
            }
            if (type == Piece.Bishop || type == Piece.Queen)
            {
                foreach (int dir in bishopDirs)
                {
                    int to = from + dir;
                    while (to >= 0 && to < 64)
                    {
                        // ensure diagonal didn't wrap files
                        if (System.Math.Abs((to % 8) - (from % 8)) > 2 && (System.Math.Abs(dir) == 7 || System.Math.Abs(dir) == 9)) break;
                        moves.Add(new Move(from, to));
                        if ((OccupiedBB & Bitboard.Bit(to)) != 0) break;
                        int next = to + dir;
                        if (next < 0 || next >= 64) break;
                        to = next;
                    }
                }
            }
        }

        return moves;
    }
}
