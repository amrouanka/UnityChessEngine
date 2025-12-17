public static class Piece
{
    public const int None = 0b000;
    public const int King = 0b001;
    public const int Pawn = 0b010;
    public const int Knight = 0b011;
    public const int Bishop = 0b100;
    public const int Rook = 0b101;
    public const int Queen = 0b110;

    public const int White = 0b01000;
    public const int Black = 0b10000;
    public const int TypeMask = 0b111;
}
