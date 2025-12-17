using System.Collections.Generic;
using System;
using static Board;

public static class GameLogic
{
    public static bool isWhiteTurn = true;
    public static int BlackKingSquare = 60;
    public static int WhiteKingSquare = 4;

    public struct Move
    {
        public readonly int StartSquare;
        public readonly int TargetSquare;
        public readonly int PromotionPiece;

        public Move(int startSquare, int targetSquare, int promotionPiece = Piece.None)
        {
            StartSquare = startSquare;
            TargetSquare = targetSquare;
            PromotionPiece = promotionPiece;
        }
    }


    private static readonly int[] knightOffsets = { -17, 17, -15, 15, -10, 10, 6, -6 };
    private static readonly int[] directionOffsets = { 8, -8, -1, 1, -7, 7, 9, -9 }; // first 4 are for the rook
    private static readonly int[][] numSquaresToEdge = new int[64][];

    public static List<Move> moves = new List<Move>();
    public static List<int> WhiteAttackedSquares = new List<int>();
    public static List<int> BlackAttackedSquares = new List<int>();

    public static List<Move> GenerateLegalMoves()
    {
        List<Move> pseudoLegalMoves = GenerateMoves();  // Get all possible moves for the side to move
        List<Move> legalMoves = new List<Move>();

        foreach (Move moveToVerify in pseudoLegalMoves)
        {
            bool movingIsWhite = isWhiteTurn;
            MoveState prevState = SaveState(moveToVerify);
            MakeMove(moveToVerify); // toggles isWhiteTurn

            // After making the move, generate opponent pseudo-legal moves and collect attacked squares
            List<Move> opponentMoves = GenerateMoves();
            var opponentAttacked = new System.Collections.Generic.HashSet<int>();
            foreach (var move in opponentMoves) opponentAttacked.Add(move.TargetSquare);

            int movedKingSquare = movingIsWhite ? WhiteKingSquare : BlackKingSquare;
            if (!opponentAttacked.Contains(movedKingSquare))
            {
                legalMoves.Add(moveToVerify);
            }

            UnmakeMove(moveToVerify, prevState); // restores isWhiteTurn and board state
        }

        return legalMoves;
    }

    public static void PrecomputedMoveData()
    {
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int numNorth = 7 - rank;
                int numSouth = rank;
                int numWest = file;
                int numEast = 7 - file;

                int squareIndex = rank * 8 + file;

                numSquaresToEdge[squareIndex] = new int[8] {
                    numNorth,
                    numSouth,
                    numWest,
                    numEast,
                    Math.Min(numSouth, numEast),
                    Math.Min(numNorth, numWest),
                    Math.Min(numNorth, numEast),
                    Math.Min(numSouth, numWest)
                };
            }
        }
    }

    private static bool IsLegalMove(int fromIndex, int toIndex)
    {
        if (!IsOnBoard(fromIndex) || !IsOnBoard(toIndex)) return false;

        int targetPiece = Square[toIndex];
        bool isWhitePiece = (Square[fromIndex] & Piece.White) != 0;
        bool isTargetWhitePiece = (targetPiece & Piece.White) != 0;

        if (isWhiteTurn != isWhitePiece) return false;

        if (isWhitePiece && isTargetWhitePiece ||
            (!isWhitePiece && !isTargetWhitePiece && targetPiece != Piece.None))
        {
            return false;
        }

        return true;
    }

    public static List<Move> GenerateMoves()
    {
        // reset attacked squares lists before generating moves
        WhiteAttackedSquares.Clear();
        BlackAttackedSquares.Clear();

        List<Move> moves = new List<Move>();

        for (int i = 0; i < 64; i++)
        {
            int piece = Square[i];
            if (piece == Piece.None) continue;

            bool isWhitePiece = (piece & Piece.White) != 0;
            if (isWhiteTurn != isWhitePiece) continue;

            List<Move> pieceMoves = GenerateMovesForPiece(i);
            if (isWhiteTurn)
            {
                foreach (Move move in pieceMoves)
                {
                    moves.Add(move);
                    if ((Square[move.StartSquare] & 0b111) != Piece.Pawn) WhiteAttackedSquares.Add(move.TargetSquare);
                }
            }
            else
            {
                foreach (Move move in pieceMoves)
                {
                    moves.Add(move);
                    if ((Square[move.StartSquare] & 0b111) != Piece.Pawn) BlackAttackedSquares.Add(move.TargetSquare);
                }
            }
        }

        return moves;
    }

    public static List<Move> GenerateMovesForPiece(int fromIndex)
    {
        List<Move> availableMoves = new List<Move>();
        int piece = Square[fromIndex];
        if (piece == Piece.None) return availableMoves;

        bool isWhitePiece = (piece & Piece.White) != 0;
        if (isWhiteTurn != isWhitePiece) return availableMoves;

        switch (piece & 0b111) // Mask to get piece type ignoring color
        {
            case Piece.Knight:
                availableMoves.AddRange(GetMovesForOffsets(fromIndex, knightOffsets));
                break;
            case Piece.Bishop:
            case Piece.Rook:
            case Piece.Queen:
                availableMoves.AddRange(GetSlidingPieceMoves(fromIndex));
                break;
            case Piece.King:
                availableMoves.AddRange(GetMovesForOffsets(fromIndex, directionOffsets));
                availableMoves.AddRange(GetCastlingMoves(fromIndex));
                break;
            case Piece.Pawn:
                availableMoves.AddRange(GetPawnMoves(fromIndex));
                break;
        }

        return availableMoves;
    }

    private static List<Move> GetMovesForOffsets(int fromIndex, int[] offsets)
    {
        List<Move> moves = new List<Move>();
        int fromRank = fromIndex / 8;
        int fromFile = fromIndex % 8;
        bool pieceCol = (Square[fromIndex] & Piece.White) != 0;

        foreach (int offset in offsets)
        {
            int targetIndex = fromIndex + offset;
            if (!IsOnBoard(targetIndex))
            {
                continue;
            }

            int pieceOnTargetSquare = Square[targetIndex];
            bool targetPieceCol = (pieceOnTargetSquare & Piece.White) != 0;
            int toRank = targetIndex / 8;
            int toFile = targetIndex % 8;

            // Ensure the move doesn't wrap around the board horizontally
            if (Math.Abs(toRank - fromRank) > 2 || Math.Abs(toFile - fromFile) > 2)
            {
                continue;
            }
            if (targetPieceCol == pieceCol && pieceOnTargetSquare != Piece.None)
            {
                continue;
            }

            moves.Add(new Move(fromIndex, targetIndex));
        }
        return moves;
    }

    private static List<Move> GetSlidingPieceMoves(int fromIndex)
    {
        List<Move> moves = new List<Move>();

        int piece = Square[fromIndex];
        int startDirectionIndex = (piece & 0b111) == Piece.Bishop ? 4 : 0;
        int endDirectionIndex = (piece & 0b111) == Piece.Rook ? 4 : 8;
        bool pieceCol = (piece & Piece.White) != 0;

        for (int directionIndex = startDirectionIndex; directionIndex < endDirectionIndex; directionIndex++)
        {
            for (int n = 0; n < numSquaresToEdge[fromIndex][directionIndex]; n++)
            {
                int targetSquare = fromIndex + directionOffsets[directionIndex] * (n + 1);
                if (!IsOnBoard(targetSquare)) break;
                int pieceOnTargetSquare = Square[targetSquare];
                bool targetPieceCol = (pieceOnTargetSquare & Piece.White) != 0;

                if (pieceOnTargetSquare == Piece.None)
                {
                    moves.Add(new Move(fromIndex, targetSquare));
                }
                else if (targetPieceCol == pieceCol) // Blocked by a friendly piece
                {
                    break;
                }
                else if (targetPieceCol != pieceCol)
                {
                    moves.Add(new Move(fromIndex, targetSquare));
                    break;
                }
            }
        }

        return moves;
    }

    private static List<Move> GetPawnMoves(int fromIndex)
    {
        List<Move> moves = new List<Move>();
        bool isWhitePawn = (Square[fromIndex] & Piece.White) != 0;
        int direction = isWhitePawn ? 8 : -8;
        int startingRank = isWhitePawn ? 1 : 6;
        int promotionRank = isWhitePawn ? 7 : 0;
        int enPassantRank = isWhitePawn ? 4 : 3;

        int oneStepMove = fromIndex + direction;
        int twoStepMove = fromIndex + 2 * direction;
        
        // Generate pawn captures
        int leftCapture = fromIndex + direction - 1;
        int rightCapture = fromIndex + direction + 1;

        // Generate normal pawn moves
        if (IsLegalMove(fromIndex, oneStepMove) && Square[oneStepMove] == Piece.None)
        {
            // Check for promotion
            if (oneStepMove / 8 == promotionRank)
            {
                moves.Add(new Move(fromIndex, oneStepMove, Piece.Queen));
                moves.Add(new Move(fromIndex, oneStepMove, Piece.Rook));
                moves.Add(new Move(fromIndex, oneStepMove, Piece.Bishop));
                moves.Add(new Move(fromIndex, oneStepMove, Piece.Knight));
            }
            else
            {
                moves.Add(new Move(fromIndex, oneStepMove));
            }

            // If the pawn is on its starting rank, it can move two steps
            if (fromIndex / 8 == startingRank && IsLegalMove(fromIndex, twoStepMove) && Square[twoStepMove] == Piece.None)
            {
                moves.Add(new Move(fromIndex, twoStepMove));
            }
        }

        // Capture to the left
        if (IsOnBoard(leftCapture) && IsSameRank(fromIndex, leftCapture) && isWhitePawn != ((Square[leftCapture] & Piece.White) != 0))
        {
            if (isWhitePawn)
            {
                WhiteAttackedSquares.Add(leftCapture);
            }
            else
            {
                BlackAttackedSquares.Add(leftCapture);
            }

            if (Square[leftCapture] != Piece.None)
            {
                if (leftCapture / 8 == promotionRank)
                {
                    moves.Add(new Move(fromIndex, leftCapture, Piece.Queen));
                    moves.Add(new Move(fromIndex, leftCapture, Piece.Rook));
                    moves.Add(new Move(fromIndex, leftCapture, Piece.Bishop));
                    moves.Add(new Move(fromIndex, leftCapture, Piece.Knight));
                }
                else
                {
                    moves.Add(new Move(fromIndex, leftCapture));
                }
            }
        }


        // Capture to the right
        if (IsOnBoard(rightCapture) && IsSameRank(fromIndex, rightCapture) && (isWhitePawn != ((Square[rightCapture] & Piece.White) != 0)))
        {
            if (isWhitePawn)
            {
                WhiteAttackedSquares.Add(rightCapture);
            }
            else
            {
                BlackAttackedSquares.Add(rightCapture);
            }

            if (Square[rightCapture] != Piece.None)
            {
                if (rightCapture / 8 == promotionRank)
                {
                    moves.Add(new Move(fromIndex, rightCapture, Piece.Queen));
                    moves.Add(new Move(fromIndex, rightCapture, Piece.Rook));
                    moves.Add(new Move(fromIndex, rightCapture, Piece.Bishop));
                    moves.Add(new Move(fromIndex, rightCapture, Piece.Knight));
                }
                else
                {
                    moves.Add(new Move(fromIndex, rightCapture));
                }
            }
        }

        // Handle en passant
        if (fromIndex / 8 == enPassantRank)
        {
            int enPassantLeft = fromIndex + direction - 1;
            int enPassantRight = fromIndex + direction + 1;

            if (IsOnBoard(enPassantLeft) && enPassantTargetSquare == enPassantLeft)
            {
                moves.Add(new Move(fromIndex, enPassantLeft));
            }
            else if (IsOnBoard(enPassantRight) && enPassantTargetSquare == enPassantRight)
            {
                moves.Add(new Move(fromIndex, enPassantRight));
            }
        }

        return moves;
    }

    private static List<Move> GetCastlingMoves(int fromIndex)
    {
        List<Move> moves = new List<Move>();

        if (isWhiteTurn)
        {
            // White castling
            if (whiteKingsideCastlingRights && Square[fromIndex + 1] == Piece.None && Square[fromIndex + 2] == Piece.None
                && Square[fromIndex + 3] == (Piece.White | Piece.Rook)
                && (!BlackAttackedSquares.Contains(fromIndex + 1) && !BlackAttackedSquares.Contains(fromIndex + 2)))
            {
                moves.Add(new Move(fromIndex, fromIndex + 2));
            }
            if (whiteQueensideCastlingRights && Square[fromIndex - 1] == Piece.None && Square[fromIndex - 2] == Piece.None
                && Square[fromIndex - 3] == Piece.None && Square[fromIndex - 4] == (Piece.White | Piece.Rook)
                && (!BlackAttackedSquares.Contains(fromIndex - 1) && !BlackAttackedSquares.Contains(fromIndex - 2)))
            {
                moves.Add(new Move(fromIndex, fromIndex - 2));
            }
        }
        else
        {
            // Black castling
            if (blackKingsideCastlingRights && Square[fromIndex + 1] == Piece.None && Square[fromIndex + 2] == Piece.None
                && Square[fromIndex + 3] == (Piece.Black | Piece.Rook)
                && (!WhiteAttackedSquares.Contains(fromIndex + 1) && !WhiteAttackedSquares.Contains(fromIndex + 2)))
            {
                moves.Add(new Move(fromIndex, fromIndex + 2));
            }
            if (blackQueensideCastlingRights && Square[fromIndex - 1] == Piece.None && Square[fromIndex - 2] == Piece.None
                && Square[fromIndex - 3] == Piece.None && Square[fromIndex - 4] == (Piece.Black | Piece.Rook)
                && (!WhiteAttackedSquares.Contains(fromIndex - 1) && !WhiteAttackedSquares.Contains(fromIndex - 2)))
            {
                moves.Add(new Move(fromIndex, fromIndex - 2));
            }
        }

        return moves;
    }

    private static bool IsOnBoard(int index)
    {
        return index >= 0 && index < 64;
    }

    private static bool IsSameRank(int fromIndex, int toIndex)
    {
        return Math.Abs((fromIndex % 8) - (toIndex % 8)) <= 1;
    }

    public static bool IsValidMove(int originalIndex, int newIndex, int promotionPiece)
    {
        foreach (Move move in moves)
        {
            if (move.StartSquare == originalIndex &&
                move.TargetSquare == newIndex &&
                move.PromotionPiece == promotionPiece)
            {
                return true;
            }
        }
        return false;
    }
}