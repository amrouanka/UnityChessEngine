using System;
using static GameLogic;

public static class Board
{
    public static int[] Square;
    public static bool whiteKingsideCastlingRights;
    public static bool whiteQueensideCastlingRights;
    public static bool blackKingsideCastlingRights;
    public static bool blackQueensideCastlingRights;
    public static int enPassantTargetSquare;
    // Bitboards (incremental migration alongside Square[])
    public static ulong WhitePawns, WhiteKnights, WhiteBishops, WhiteRooks, WhiteQueens, WhiteKing;
    public static ulong BlackPawns, BlackKnights, BlackBishops, BlackRooks, BlackQueens, BlackKing;
    public static ulong WhitePiecesBB, BlackPiecesBB, OccupiedBB;
    public static string startFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    static Board()
    {
        Square = new int[64];
        whiteKingsideCastlingRights = true;
        whiteQueensideCastlingRights = true;
        blackKingsideCastlingRights = true;
        blackQueensideCastlingRights = true;
        enPassantTargetSquare = -1;
        LoadPositionFromFen(startFEN);
    }
    public struct MoveState
    {
        public int PieceMoved;
        public int CapturedPiece;
        public int CapturedPieceIndex;
        public bool WhiteKingsideCastlingRights;
        public bool WhiteQueensideCastlingRights;
        public bool BlackKingsideCastlingRights;
        public bool BlackQueensideCastlingRights;
        public int EnPassantTargetSquare;
        public int WhiteKingSquare;
        public int BlackKingSquare;
        public MoveState(int pieceMoved, int capturedPiece, int capturedPieceIndex, bool whiteKingsideCastlingRights, bool whiteQueensideCastlingRights,
                         bool blackKingsideCastlingRights, bool blackQueensideCastlingRights, int enPassantTargetSquare,
                         int whiteKingSquare, int blackKingSquare)
        {
            PieceMoved = pieceMoved;
            CapturedPiece = capturedPiece;
            CapturedPieceIndex = capturedPieceIndex;
            WhiteKingsideCastlingRights = whiteKingsideCastlingRights;
            WhiteQueensideCastlingRights = whiteQueensideCastlingRights;
            BlackKingsideCastlingRights = blackKingsideCastlingRights;
            BlackQueensideCastlingRights = blackQueensideCastlingRights;
            EnPassantTargetSquare = enPassantTargetSquare;
            WhiteKingSquare = whiteKingSquare;
            BlackKingSquare = blackKingSquare;
        }
    }

    public static void LoadPositionFromFen(string fen)
    {
        string[] parts = fen.Split(' ');
        if (parts.Length < 6)
            throw new ArgumentException("Invalid FEN string");

        string piecePlacement = parts[0];
        string activeColor = parts[1];
        string castlingRights = parts[2];
        string enPassant = parts[3];

        int file = 0;
        int rank = 7;
        foreach (char c in piecePlacement)
        {
            if (c == '/')
            {
                rank--;
                file = 0;
            }
            else if (char.IsDigit(c))
            {
                file += c - '0';
            }
            else
            {
                int piece = PieceFromChar(c);
                Square[rank * 8 + file] = piece;
                file++;
            }
        }
        // After loading the array-based board, populate bitboards for incremental migration.
        UpdateBitboards();
    }

    public static void UpdateBitboards()
    {
        WhitePawns = WhiteKnights = WhiteBishops = WhiteRooks = WhiteQueens = WhiteKing = 0UL;
        BlackPawns = BlackKnights = BlackBishops = BlackRooks = BlackQueens = BlackKing = 0UL;
        WhitePiecesBB = BlackPiecesBB = OccupiedBB = 0UL;

        for (int i = 0; i < 64; i++)
        {
            int p = Square[i];
            if (p == Piece.None) continue;
            ulong bit = 1UL << i;
            bool white = (p & Piece.White) != 0;
            int type = p & Piece.TypeMask;
            if (white)
            {
                switch (type)
                {
                    case Piece.Pawn: WhitePawns |= bit; break;
                    case Piece.Knight: WhiteKnights |= bit; break;
                    case Piece.Bishop: WhiteBishops |= bit; break;
                    case Piece.Rook: WhiteRooks |= bit; break;
                    case Piece.Queen: WhiteQueens |= bit; break;
                    case Piece.King: WhiteKing |= bit; break;
                }
            }
            else
            {
                switch (type)
                {
                    case Piece.Pawn: BlackPawns |= bit; break;
                    case Piece.Knight: BlackKnights |= bit; break;
                    case Piece.Bishop: BlackBishops |= bit; break;
                    case Piece.Rook: BlackRooks |= bit; break;
                    case Piece.Queen: BlackQueens |= bit; break;
                    case Piece.King: BlackKing |= bit; break;
                }
            }
        }

        WhitePiecesBB = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        BlackPiecesBB = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        OccupiedBB = WhitePiecesBB | BlackPiecesBB;
    }

        private static int PieceFromChar(char c)
    {
        switch (char.ToLower(c))
        {
            case 'p': return c == 'p' ? Piece.Black | Piece.Pawn : Piece.White | Piece.Pawn;
            case 'n': return c == 'n' ? Piece.Black | Piece.Knight : Piece.White | Piece.Knight;
            case 'b': return c == 'b' ? Piece.Black | Piece.Bishop : Piece.White | Piece.Bishop;
            case 'r': return c == 'r' ? Piece.Black | Piece.Rook : Piece.White | Piece.Rook;
            case 'q': return c == 'q' ? Piece.Black | Piece.Queen : Piece.White | Piece.Queen;
            case 'k': return c == 'k' ? Piece.Black | Piece.King : Piece.White | Piece.King;
            default: return Piece.None;
        }
    }

    public static void MakeMove(Move move)
    {
        int piece = Square[move.StartSquare];
        bool isWhitePiece = (piece & Piece.White) != 0;
        int step = isWhitePiece ? 8 : -8;

        if ((piece & 0b111) == Piece.Pawn)
        {
            if (move.PromotionPiece != Piece.None)
            {
                piece = (isWhitePiece ? Piece.White : Piece.Black) | move.PromotionPiece;
            }
            if (move.TargetSquare == enPassantTargetSquare && (move.StartSquare + step + 1 == move.TargetSquare || move.StartSquare + step - 1 == move.TargetSquare))
            {
                // remove the captured pawn (it sits one step behind the target)
                Square[move.TargetSquare - step] = Piece.None;
            }
            if (move.TargetSquare == move.StartSquare + 2 * step)
            {
                // set en-passant target square to the square the pawn passed over
                enPassantTargetSquare = move.StartSquare + step;
            }
            else
            {
                enPassantTargetSquare = -1;
            }
        }
        else if ((piece & 0b111) == Piece.King)
        {
            if (isWhitePiece)
            {
                WhiteKingSquare = move.TargetSquare;
                whiteKingsideCastlingRights = whiteQueensideCastlingRights = false;
            }
            else
            {
                BlackKingSquare = move.TargetSquare;
                blackKingsideCastlingRights = blackQueensideCastlingRights = false;
            }

            if (move.TargetSquare == move.StartSquare + 2)
            {
                Square[move.TargetSquare - 1] = Square[move.StartSquare + 3];
                Square[move.StartSquare + 3] = Piece.None;
            }
            else if (move.TargetSquare == move.StartSquare - 2)
            {
                Square[move.TargetSquare + 1] = Square[move.StartSquare - 4];
                Square[move.StartSquare - 4] = Piece.None;
            }
        }
        else if ((piece & 0b111) == Piece.Rook)
        {
            if (isWhitePiece)
            {
                if (move.StartSquare == 0) whiteQueensideCastlingRights = false;
                else if (move.StartSquare == 7) whiteKingsideCastlingRights = false;
            }
            else
            {
                if (move.StartSquare == 56) blackQueensideCastlingRights = false;
                else if (move.StartSquare == 63) blackKingsideCastlingRights = false;
            }
        }
        Square[move.TargetSquare] = piece;
        Square[move.StartSquare] = Piece.None;
        isWhiteTurn = !isWhiteTurn;
        // keep bitboards in sync (simple but correct approach)
        UpdateBitboards();
    }

    public static MoveState SaveState(Move move)
    {
        int pieceMoved = Square[move.StartSquare];
        int capturedIndex = move.TargetSquare;
        int capturedPiece = Square[move.TargetSquare];

        // Special-case en-passant captures: the captured pawn is not on the target square
        if ((pieceMoved & 0b111) == Piece.Pawn && move.TargetSquare == enPassantTargetSquare)
        {
            int step = (pieceMoved & Piece.White) != 0 ? 8 : -8;
            capturedIndex = move.TargetSquare - step;
            capturedPiece = Square[capturedIndex];
        }

        return new MoveState(
            pieceMoved,
            capturedPiece,
            capturedIndex,
            whiteKingsideCastlingRights,
            whiteQueensideCastlingRights,
            blackKingsideCastlingRights,
            blackQueensideCastlingRights,
            enPassantTargetSquare,
            WhiteKingSquare,
            BlackKingSquare
        );
    }

    public static void RestoreState(MoveState state)
    {
        whiteKingsideCastlingRights = state.WhiteKingsideCastlingRights;
        whiteQueensideCastlingRights = state.WhiteQueensideCastlingRights;
        blackKingsideCastlingRights = state.BlackKingsideCastlingRights;
        blackQueensideCastlingRights = state.BlackQueensideCastlingRights;
        enPassantTargetSquare = state.EnPassantTargetSquare;
        WhiteKingSquare = state.WhiteKingSquare;
        BlackKingSquare = state.BlackKingSquare;
    }

    public static void UnmakeMove(Move move, MoveState prevState)
    {
        // Restore the moved piece to its start square
        Square[move.StartSquare] = prevState.PieceMoved;

        // Restore captured piece: if capture index equals the target square, the captured piece was on the target.
        // Otherwise (en-passant), the captured piece was on a different square (prevState.CapturedPieceIndex).
        if (prevState.CapturedPieceIndex == move.TargetSquare || prevState.CapturedPieceIndex < 0)
        {
            Square[move.TargetSquare] = prevState.CapturedPiece;
        }
        else
        {
            Square[move.TargetSquare] = Piece.None;
            Square[prevState.CapturedPieceIndex] = prevState.CapturedPiece;
        }

        // Restore other state information
        RestoreState(prevState);

        // Handle undoing castling rook moves
        if ((prevState.PieceMoved & 0b111) == Piece.King)
        {
            if (move.TargetSquare == move.StartSquare + 2)
            {
                Square[move.StartSquare + 3] = Square[move.TargetSquare - 1];
                Square[move.TargetSquare - 1] = Piece.None;
            }
            else if (move.TargetSquare == move.StartSquare - 2)
            {
                Square[move.StartSquare - 4] = Square[move.TargetSquare + 1];
                Square[move.TargetSquare + 1] = Piece.None;
            }
        }

        isWhiteTurn = !isWhiteTurn;
        // restore bitboards after undo
        UpdateBitboards();
    }

    public static void MakeMoveAndUnmake(Move move)
    {
        MoveState prevState = SaveState(move);
        MakeMove(move);
        UnmakeMove(move, prevState);
    }
}
