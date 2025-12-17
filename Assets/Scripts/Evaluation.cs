using UnityEngine;
using static Board;

public static class Evaluation
{
	public static readonly int Queen = 900;
	public static readonly int Rook = 500;
	public static readonly int Bishop = 320;
	public static readonly int Knight = 300;
	public static readonly int Pawn = 100;

	public static int EvaluateBitboards()
	{
		int score = 0;
		score += Bitboard.PopCount(WhiteQueens) * Queen;
		score += Bitboard.PopCount(WhiteRooks) * Rook;
		score += Bitboard.PopCount(WhiteBishops) * Bishop;
		score += Bitboard.PopCount(WhiteKnights) * Knight;
		score += Bitboard.PopCount(WhitePawns) * Pawn;

		score -= Bitboard.PopCount(BlackQueens) * Queen;
		score -= Bitboard.PopCount(BlackRooks) * Rook;
		score -= Bitboard.PopCount(BlackBishops) * Bishop;
		score -= Bitboard.PopCount(BlackKnights) * Knight;
		score -= Bitboard.PopCount(BlackPawns) * Pawn;

		return score;
	}
}
