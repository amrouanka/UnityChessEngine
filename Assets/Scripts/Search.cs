using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Search
{
    public static GameLogic.Move ChooseComputerMove(List<GameLogic.Move> moves)
    {
        return moves[Random.Range(0, moves.Count)];
    }
}
