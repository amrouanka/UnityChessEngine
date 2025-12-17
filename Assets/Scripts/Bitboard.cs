using System.Collections.Generic;
using static Board;
using static GameLogic;

public static class Bitboard
{
    public static ulong Bit(int sq) => 1UL << sq;
    public static bool TestBit(ulong bb, int sq) => (bb & Bit(sq)) != 0;
    public static ulong SetBit(ulong bb, int sq) => bb | Bit(sq);
    public static ulong ClearBit(ulong bb, int sq) => bb & ~Bit(sq);

    private static Stack<MoveStateBB> stateStack = new Stack<MoveStateBB>(256);

    public static int BitScanForward(ulong bb)
        {
            if (bb == 0) return -1;
            int count = 0;
            while ((bb & 1) == 0)
            {
                bb >>= 1;
                count++;
            }
            return count;
        }


    public static int PopLsb(ref ulong bb)
    {
        if (bb == 0) return -1;
        int idx = BitScanForward(bb);
        bb &= bb - 1UL;
        return idx;
    }

    public static int PopCount(ulong bb)
        {
            int count = 0;
            while (bb != 0)
            {
                bb &= bb - 1; // clear the least significant bit set
                count++;
            }
            return count;
        }

    public struct MoveStateBB
    {
        public ulong WhitePawns, WhiteKnights, WhiteBishops, WhiteRooks, WhiteQueens, WhiteKing;
        public ulong BlackPawns, BlackKnights, BlackBishops, BlackRooks, BlackQueens, BlackKing;
        public ulong WhitePiecesBB, BlackPiecesBB, OccupiedBB;

        // Game state
        public bool WhiteToMove;
        public bool WhiteKingsideCastlingRights;
        public bool WhiteQueensideCastlingRights;
        public bool BlackKingsideCastlingRights;
        public bool BlackQueensideCastlingRights;
        public int EnPassantSquare;
        public int WhiteKingSquare;
        public int BlackKingSquare;
    }

    public static void MakeMoveBB(Move m)
    {
        // Save state
        stateStack.Push(new MoveStateBB
        {
            WhitePawns = WhitePawns,
            WhiteKnights = WhiteKnights,
            WhiteBishops = WhiteBishops,
            WhiteRooks = WhiteRooks,
            WhiteQueens = WhiteQueens,
            WhiteKing = WhiteKing,

            BlackPawns = BlackPawns,
            BlackKnights = BlackKnights,
            BlackBishops = BlackBishops,
            BlackRooks = BlackRooks,
            BlackQueens = BlackQueens,
            BlackKing = BlackKing,

            WhitePiecesBB = WhitePiecesBB,
            BlackPiecesBB = BlackPiecesBB,
            OccupiedBB = OccupiedBB,

            WhiteToMove = isWhiteTurn,
            WhiteKingsideCastlingRights = whiteKingsideCastlingRights,
            WhiteQueensideCastlingRights = whiteQueensideCastlingRights,
            BlackKingsideCastlingRights = blackKingsideCastlingRights,
            BlackQueensideCastlingRights = blackQueensideCastlingRights,
            EnPassantSquare = enPassantTargetSquare,
            WhiteKingSquare = WhiteKingSquare,
            BlackKingSquare = BlackKingSquare
        });

        int from = m.StartSquare;
        int to = m.TargetSquare;
        int piece = Square[from];
        bool isWhitePiece = (piece & Piece.White) != 0;
        int type = piece & Piece.TypeMask;

        int step = isWhitePiece ? 8 : -8;

        // Handle captures (including en-passant)
        int capturedIndex = to;
        int capturedPiece = Square[to];
        if (type == Piece.Pawn && to == enPassantTargetSquare)
        {
            // captured pawn sits one step behind target
            capturedIndex = to - step;
            capturedPiece = Square[capturedIndex];
            Square[capturedIndex] = Piece.None;
            // clear captured pawn bitboard
            if ((capturedPiece & Piece.White) != 0) WhitePawns &= ~Bit(capturedIndex); else BlackPawns &= ~Bit(capturedIndex);
        }
        else
        {
            if (capturedPiece != Piece.None)
            {
                // clear captured piece bitboard
                bool capturedWhite = (capturedPiece & Piece.White) != 0;
                int capturedType = capturedPiece & Piece.TypeMask;
                ulong mask = ~Bit(capturedIndex);
                if (capturedWhite)
                {
                    switch (capturedType)
                    {
                        case Piece.Pawn: WhitePawns &= mask; break;
                        case Piece.Knight: WhiteKnights &= mask; break;
                        case Piece.Bishop: WhiteBishops &= mask; break;
                        case Piece.Rook: WhiteRooks &= mask; break;
                        case Piece.Queen: WhiteQueens &= mask; break;
                        case Piece.King: WhiteKing &= mask; break;
                    }
                }
                else
                {
                    switch (capturedType)
                    {
                        case Piece.Pawn: BlackPawns &= mask; break;
                        case Piece.Knight: BlackKnights &= mask; break;
                        case Piece.Bishop: BlackBishops &= mask; break;
                        case Piece.Rook: BlackRooks &= mask; break;
                        case Piece.Queen: BlackQueens &= mask; break;
                        case Piece.King: BlackKing &= mask; break;
                    }
                }
            }
        }

        // Clear from-square on appropriate bitboard
        ulong fromMask = ~Bit(from);
        if (isWhitePiece)
        {
            switch (type)
            {
                case Piece.Pawn: WhitePawns &= fromMask; break;
                case Piece.Knight: WhiteKnights &= fromMask; break;
                case Piece.Bishop: WhiteBishops &= fromMask; break;
                case Piece.Rook: WhiteRooks &= fromMask; break;
                case Piece.Queen: WhiteQueens &= fromMask; break;
                case Piece.King: WhiteKing &= fromMask; break;
            }
        }
        else
        {
            switch (type)
            {
                case Piece.Pawn: BlackPawns &= fromMask; break;
                case Piece.Knight: BlackKnights &= fromMask; break;
                case Piece.Bishop: BlackBishops &= fromMask; break;
                case Piece.Rook: BlackRooks &= fromMask; break;
                case Piece.Queen: BlackQueens &= fromMask; break;
                case Piece.King: BlackKing &= fromMask; break;
            }
        }

        // Handle promotion
        int movedPiece = piece;
        if (type == Piece.Pawn && m.PromotionPiece != Piece.None)
        {
            movedPiece = (isWhitePiece ? Piece.White : Piece.Black) | m.PromotionPiece;
            // set destination bit on promoted piece
            int promotedType = m.PromotionPiece & Piece.TypeMask;
            ulong toBit = Bit(to);
            if (isWhitePiece)
            {
                switch (promotedType)
                {
                    case Piece.Queen: WhiteQueens |= toBit; break;
                    case Piece.Rook: WhiteRooks |= toBit; break;
                    case Piece.Bishop: WhiteBishops |= toBit; break;
                    case Piece.Knight: WhiteKnights |= toBit; break;
                }
            }
            else
            {
                switch (promotedType)
                {
                    case Piece.Queen: BlackQueens |= toBit; break;
                    case Piece.Rook: BlackRooks |= toBit; break;
                    case Piece.Bishop: BlackBishops |= toBit; break;
                    case Piece.Knight: BlackKnights |= toBit; break;
                }
            }
        }
        else
        {
            // Move the piece bit to destination
            ulong toBit = Bit(to);
            if (isWhitePiece)
            {
                switch (type)
                {
                    case Piece.Pawn: WhitePawns |= toBit; break;
                    case Piece.Knight: WhiteKnights |= toBit; break;
                    case Piece.Bishop: WhiteBishops |= toBit; break;
                    case Piece.Rook: WhiteRooks |= toBit; break;
                    case Piece.Queen: WhiteQueens |= toBit; break;
                    case Piece.King: WhiteKing |= toBit; WhiteKingSquare = to; break;
                }
            }
            else
            {
                switch (type)
                {
                    case Piece.Pawn: BlackPawns |= toBit; break;
                    case Piece.Knight: BlackKnights |= toBit; break;
                    case Piece.Bishop: BlackBishops |= toBit; break;
                    case Piece.Rook: BlackRooks |= toBit; break;
                    case Piece.Queen: BlackQueens |= toBit; break;
                    case Piece.King: BlackKing |= toBit; BlackKingSquare = to; break;
                }
            }
        }

        // Update aggregates
        WhitePiecesBB = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        BlackPiecesBB = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        OccupiedBB = WhitePiecesBB | BlackPiecesBB;

        // Update Board.Square array (keep array and bitboards in sync)
        Square[to] = movedPiece;
        Square[from] = Piece.None;

        // Update en-passant target
        if (type == Piece.Pawn && System.Math.Abs(to - from) == 16)
        {
            enPassantTargetSquare = from + (step / 2);
        }
        else
        {
            enPassantTargetSquare = -1;
        }

        // Update castling rights and rook moves if necessary
        if (type == Piece.King)
        {
            if (isWhitePiece) { whiteKingsideCastlingRights = whiteQueensideCastlingRights = false; WhiteKingSquare = to; }
            else { blackKingsideCastlingRights = blackQueensideCastlingRights = false; BlackKingSquare = to; }
            // handle rook when castling (move rook)
            if (to == from + 2)
            {
                // kingside
                int rookFrom = from + 3;
                int rookTo = from + 1;
                Square[rookTo] = Square[rookFrom];
                Square[rookFrom] = Piece.None;
                // update rook bitboards
                if (isWhitePiece)
                {
                    WhiteRooks &= ~Bit(rookFrom);
                    WhiteRooks |= Bit(rookTo);
                }
                else
                {
                    BlackRooks &= ~Bit(rookFrom);
                    BlackRooks |= Bit(rookTo);
                }
            }
            else if (to == from - 2)
            {
                int rookFrom = from - 4;
                int rookTo = from - 1;
                Square[rookTo] = Square[rookFrom];
                Square[rookFrom] = Piece.None;
                if (isWhitePiece)
                {
                    WhiteRooks &= ~Bit(rookFrom);
                    WhiteRooks |= Bit(rookTo);
                }
                else
                {
                    BlackRooks &= ~Bit(rookFrom);
                    BlackRooks |= Bit(rookTo);
                }
            }
        }

        if (type == Piece.Rook)
        {
            if (isWhitePiece)
            {
                if (from == 0) whiteQueensideCastlingRights = false;
                else if (from == 7) whiteKingsideCastlingRights = false;
            }
            else
            {
                if (from == 56) blackQueensideCastlingRights = false;
                else if (from == 63) blackKingsideCastlingRights = false;
            }
        }

        // Flip side to move
        isWhiteTurn = !isWhiteTurn;
    }

    public static void UnmakeMoveBB()
    {
        MoveStateBB s = stateStack.Pop();

        WhitePawns = s.WhitePawns;
        WhiteKnights = s.WhiteKnights;
        WhiteBishops = s.WhiteBishops;
        WhiteRooks = s.WhiteRooks;
        WhiteQueens = s.WhiteQueens;
        WhiteKing = s.WhiteKing;

        BlackPawns = s.BlackPawns;
        BlackKnights = s.BlackKnights;
        BlackBishops = s.BlackBishops;
        BlackRooks = s.BlackRooks;
        BlackQueens = s.BlackQueens;
        BlackKing = s.BlackKing;

        WhitePiecesBB = s.WhitePiecesBB;
        BlackPiecesBB = s.BlackPiecesBB;
        OccupiedBB = s.OccupiedBB;

        isWhiteTurn = s.WhiteToMove;
        whiteKingsideCastlingRights = s.WhiteKingsideCastlingRights;
        whiteQueensideCastlingRights = s.WhiteQueensideCastlingRights;
        blackKingsideCastlingRights = s.BlackKingsideCastlingRights;
        blackQueensideCastlingRights = s.BlackQueensideCastlingRights;
        enPassantTargetSquare = s.EnPassantSquare;
        WhiteKingSquare = s.WhiteKingSquare;
        BlackKingSquare = s.BlackKingSquare;

        // Rebuild Square[] from bitboards to keep array in sync
        for (int i = 0; i < 64; i++) Square[i] = Piece.None;
        // white pieces
        ulong tmp = WhitePawns;
        while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.White | Piece.Pawn; }
        tmp = WhiteKnights; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.White | Piece.Knight; }
        tmp = WhiteBishops; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.White | Piece.Bishop; }
        tmp = WhiteRooks; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.White | Piece.Rook; }
        tmp = WhiteQueens; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.White | Piece.Queen; }
        tmp = WhiteKing; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.White | Piece.King; }
        // black pieces
        tmp = BlackPawns; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.Black | Piece.Pawn; }
        tmp = BlackKnights; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.Black | Piece.Knight; }
        tmp = BlackBishops; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.Black | Piece.Bishop; }
        tmp = BlackRooks; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.Black | Piece.Rook; }
        tmp = BlackQueens; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.Black | Piece.Queen; }
        tmp = BlackKing; while (tmp != 0) { int sq = PopLsb(ref tmp); Square[sq] = Piece.Black | Piece.King; }
    }
}
