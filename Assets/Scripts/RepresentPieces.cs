using System.Collections.Generic;
using UnityEngine;
using static GameLogic;
using UnityEngine.SceneManagement;

public class RepresentPieces : MonoBehaviour
{
    public GameObject whiteKing, whitePawn, whiteKnight, whiteBishop, whiteRook, whiteQueen;
    public GameObject blackKing, blackPawn, blackKnight, blackBishop, blackRook, blackQueen;
    public GameObject redSquare;
    public GameObject gameOverScreen;
    public GameObject winningScreen;

    private GameObject selectedPiece;
    private Vector3 originalPosition;
    private int originalIndex;
    private bool gameOver = false;
    private bool playerWon = false;

    private List<GameObject> highlightSquares = new List<GameObject>();
    private Dictionary<int, GameObject> pieceObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> piecePrefabs;

    void Start()
    {
        InitializePiecePrefabs();
        Board.LoadPositionFromFen(Board.startFEN);
        CreatePieces();
        PrecomputedMoveData();
        moves = GenerateLegalMoves();
    }

    void Update()
    {
        if (!gameOver && !playerWon) HandleMouseInput();
    }

    public void RestartGame()
    {
        ClearPieces();
        ResetBoardState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Resetting scene completely
    }

    private void InitializePiecePrefabs()
    {
        piecePrefabs = new Dictionary<int, GameObject>
        {
            { Piece.White | Piece.King, whiteKing },
            { Piece.White | Piece.Pawn, whitePawn },
            { Piece.White | Piece.Knight, whiteKnight },
            { Piece.White | Piece.Bishop, whiteBishop },
            { Piece.White | Piece.Rook, whiteRook },
            { Piece.White | Piece.Queen, whiteQueen },
            { Piece.Black | Piece.King, blackKing },
            { Piece.Black | Piece.Pawn, blackPawn },
            { Piece.Black | Piece.Knight, blackKnight },
            { Piece.Black | Piece.Bishop, blackBishop },
            { Piece.Black | Piece.Rook, blackRook },
            { Piece.Black | Piece.Queen, blackQueen }
        };
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }
        else if (Input.GetMouseButton(0) && selectedPiece != null)
        {
            OnMouseDrag();
        }
        else if (Input.GetMouseButtonUp(0) && selectedPiece != null)
        {
            OnMouseUp();
        }
    }

    private void OnMouseDown()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            selectedPiece = hit.collider.gameObject;
            originalPosition = selectedPiece.transform.position;
            originalIndex = GetBoardIndex(originalPosition);

            // Highlight available moves
            HighlightAvailableMoves(originalIndex);
        }
    }

    private void OnMouseDrag()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        selectedPiece.transform.position = new Vector3(mousePos.x, mousePos.y, -1);
    }

    private void OnMouseUp()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        int newIndex = GetBoardIndex(mousePos);
        int promotionPiece = Piece.None;
        // If the drop is outside the board, cancel
        if (newIndex < 0 || newIndex > 63)
        {
            selectedPiece.transform.position = originalPosition;
            selectedPiece = null;
            ClearHighlights();
            return;
        }

        // Check if the move is valid
        if ((IsValidMove(originalIndex, newIndex, Piece.None) ||
            IsValidMove(originalIndex, newIndex, Piece.Queen)) &&
            originalIndex != newIndex)
        {
            // Handle pawn promotion
            if ((Board.Square[originalIndex] & 0b111) == Piece.Pawn)
            {
                bool isWhitePawn = (Board.Square[originalIndex] & Piece.White) != 0;
                int promotionRank = isWhitePawn ? 7 : 0;
                if (newIndex / 8 == promotionRank)
                {
                    promotionPiece = Piece.Queen | (isWhitePawn ? Piece.White : Piece.Black);
                }
            }

            // Execute the player's move
            MoveGraphicalPiece(originalIndex, newIndex, promotionPiece);

            // Generate legal moves for the computer
            moves = GenerateLegalMoves();

            if (moves.Count == 0)
            {
                playerWon = true;
                winningScreen.SetActive(true);
                ClearHighlights();
                selectedPiece = null;
                return;
            }

            // Computer's turn
            Move computerMove = Search.ChooseComputerMove(moves);
            MoveGraphicalPiece(computerMove.StartSquare, computerMove.TargetSquare, computerMove.PromotionPiece);

            // Check if the game is over for the player
            moves = GenerateLegalMoves();
            if (moves.Count == 0)
            {
                gameOver = true;
                gameOverScreen.SetActive(true);
                return;
            }
        }
        else
        {
            selectedPiece.transform.position = originalPosition;
        }

        selectedPiece = null;
        ClearHighlights();
    }

    private void HighlightAvailableMoves(int fromIndex)
    {
        ClearHighlights();

        if (fromIndex < 0 || fromIndex > 63 || Board.Square[fromIndex] == Piece.None) return;

        List<Move> availableMoves = GenerateMovesForPiece(fromIndex);
        foreach (Move move in availableMoves)
        {
            if (!moves.Contains(move))
            {
                continue;
            }
            Vector3 position = GetPositionFromBoardIndex(move.TargetSquare);
            GameObject highlight = Instantiate(redSquare, position, Quaternion.identity);
            highlightSquares.Add(highlight);
        }
    }

    private void ClearHighlights()
    {
        foreach (GameObject highlight in highlightSquares)
        {
            Destroy(highlight);
        }
        highlightSquares.Clear();
    }

    private void ClearPieces()
    {
        foreach (GameObject piece in pieceObjects.Values)
        {
            if (piece != null) Destroy(piece);
        }
        pieceObjects.Clear();
    }

    private void ResetBoardState()
    {
        Board.whiteKingsideCastlingRights = true;
        Board.whiteQueensideCastlingRights = true;
        Board.blackKingsideCastlingRights = true;
        Board.blackQueensideCastlingRights = true;
        Board.enPassantTargetSquare = -1;
        gameOver = false;
        playerWon = false;

        Board.Square = new int[64];
        Board.LoadPositionFromFen(Board.startFEN);
        CreatePieces();
        moves = GenerateLegalMoves();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector3(mousePos.x, mousePos.y, 0);
    }

    private int GetBoardIndex(Vector3 position)
    {
        int file = Mathf.RoundToInt(position.x + 3);
        int rank = Mathf.RoundToInt(position.y + 3);
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
        return rank * 8 + file;
    }

    private void CreatePieces()
    {
        for (int i = 0; i < 64; i++)
        {
            int pieceType = Board.Square[i];
            if (pieceType != Piece.None && piecePrefabs.TryGetValue(pieceType, out GameObject prefab))
            {
                Vector3 position = new Vector3(i % 8 - 3, i / 8 - 3, -1);
                GameObject piece = Instantiate(prefab, position, Quaternion.identity);
                pieceObjects[i] = piece;
            }
            else
            {
                pieceObjects[i] = null;
            }
        }
    }

    private void MoveGraphicalPiece(int fromIndex, int toIndex, int promotionPiece)
    {
        Board.MakeMove(new Move(fromIndex, toIndex, promotionPiece));
        ClearPieces();
        CreatePieces();
    }

    private Vector3 GetPositionFromBoardIndex(int index)
    {
        int file = index % 8;
        int rank = index / 8;
        return new Vector3(file - 3, rank - 3, -1);
    }
}
