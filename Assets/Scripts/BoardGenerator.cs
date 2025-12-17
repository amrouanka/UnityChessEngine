using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public GameObject lightSquare;
    public GameObject darkSquare;

    void Start()
    {
        CreateGraphicalBoard();
    }

    void CreateGraphicalBoard()
    {
        bool isDarkSquare = true;

        for (int file = -3; file < 5; file++)
        {
            for (int rank = -3; rank < 5; rank++)
            {
                Vector3 position = new Vector3(file, rank, 0);
                DrawSquare(isDarkSquare, position);
                isDarkSquare = !isDarkSquare;
            }
            isDarkSquare = !isDarkSquare;
        }
    }

    void DrawSquare(bool isDarkSquare, Vector3 position)
    {
        if (isDarkSquare)
        {
            Instantiate(darkSquare, position, transform.rotation);
        }
        else
        {
            Instantiate(lightSquare, position, transform.rotation);
        }

    }
}
