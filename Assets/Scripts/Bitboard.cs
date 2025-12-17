//using System.Collections;
//using System.Collections.Generic;

//public static class Bitboard
//{
//    public static ulong whiteKing = 0, whiteQueen = 0, whiteRook = 0, whiteBishop = 0, whiteKnight = 0, whitePawn = 0;
//    public static ulong blackKing = 0, blackQueen = 0, blackRook = 0, blackBishop = 0, blackKnight = 0, blackPawn = 0;
//    public static ulong whitePieces = 0, blackPieces = 0;

//    static ulong fileA = 9259542123273814144;
//    static ulong fileH = 72340172838076673;
//    static ulong rank1 = 18374686479671623680;
//    static ulong rank8 = 255;


//    public static void InitializeBitboards(int[] chessBoard)
//    {
//        for (int i = 0; i < 64; i++)
//        {
//            /*
//             For each square i from 0 to 63, we determine the piece and set the corresponding bit in the bitboard:

//                Calculating the Bitmask:

//                    ulong bit = 1UL << i;
//                    If i = 0, bit = 1UL << 0 = 1 (binary: 0 0 0 0 0 0 0 0
//                                                          0 0 0 0 0 0 0 0
//                                                          0 0 0 0 0 0 0 0
//                                                          0 0 0 0 0 0 0 0
//                                                          0 0 0 0 0 0 0 0      ==     1
//                                                          0 0 0 0 0 0 0 0
//                                                          0 0 0 0 0 0 0 0
//                                                          0 0 0 0 0 0 0 1

//                    If i = 1, bit = 1UL << 1 = 2 (binary: 0000000000000000000000000000000000000000000000000000000000000010). */

//            ulong bit = 1UL << i;

//            switch (chessBoard[i])
//            {
//                case Piece.White | Piece.Pawn:
//                    whitePawn |= bit;
//                    break;
//                case Piece.White | Piece.Knight:
//                    whiteKnight |= bit;
//                    break;
//                case Piece.White | Piece.Bishop:
//                    whiteBishop |= bit;
//                    break;
//                case Piece.White | Piece.Rook:
//                    whiteRook |= bit;
//                    break;
//                case Piece.White | Piece.Queen:
//                    whiteQueen |= bit;
//                    break;
//                case Piece.Black | Piece.Pawn:
//                    blackPawn |= bit;
//                    break;
//                case Piece.Black | Piece.Knight:
//                    blackKnight |= bit;
//                    break;
//                case Piece.Black | Piece.Bishop:
//                    blackBishop |= bit;
//                    break;
//                case Piece.Black | Piece.Rook:
//                    blackRook |= bit;
//                    break;
//                case Piece.Black | Piece.Queen:
//                    blackQueen |= bit;
//                    break;

//                case Piece.White | Piece.King:
//                    whiteKing |= bit;
//                    break;
//                case Piece.Black | Piece.King:
//                    blackKing |= bit;
//                    break;
//            }
//        }

//        whitePieces = whiteQueen | whiteRook | whiteBishop | whiteKnight | whitePawn;
//        blackPieces = blackQueen | blackRook | blackBishop | blackKnight | blackPawn;
//    }

//    public static void initializePiecesAttacks()
//    {
//        initializeSlidingPiecesAttacks();
//        initializeLeaperPiecesAttacks();
//        initializePawnAttacks();
//    }
//    public static void initializeSlidingPiecesAttacks()
//    {

//    }

//    public static void initializeLeaperPiecesAttacks()
//    {

//    }

//    public static void initializePawnAttacks()
//    {
//        ulong leftCaptures = (whitePawn << 9) & blackPieces & ~fileH;
//        ulong rightCaptures = (whitePawn << 7) & blackPieces & ~fileA;

//    }
//}
