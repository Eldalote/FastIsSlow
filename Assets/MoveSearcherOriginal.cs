using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveSearcherOriginal
{
    /// <summary>
    /// Searcher archive; Original working one, only has mini-max recursive searching as feature.
    /// Features:
    /// Mini-max recursive searching.    
    /// </summary>


    const int positiveInfinity = 100000;
    const int negativeInfinity = -positiveInfinity;

    private uint _nodesSearched = 0;
    private MoveDirection _bestDirection = MoveDirection.None;


    public (MoveDirection, uint, int) StartSearch(HexBoard board, ulong score, int depth)
    {
        _nodesSearched = 0;
        int eval = SearchMovesTopLevel(depth, board, score);

        //Debug.Log($"Total end states evaluated: {_nodesSearched}, end eval: {eval}");
        return (_bestDirection, _nodesSearched, eval);


    }

    public int SearchMoves(int depth, HexBoard board, ulong score, bool playerToMove)
    {
        if (depth == 0)
        {
            _nodesSearched++;
            return PositionEvaluator.EvaluatePosition(board, score);
        }

        int bestEval;
        if (playerToMove)
        {
            bestEval = negativeInfinity;
            List<PlayerMoveOption> moveOptions = new List<PlayerMoveOption>();
            moveOptions = MoveOptionsGenerator.PlayerMoveOptions(board, score);

            // If no more moves are possible, it's game over.
            if (moveOptions.Count == 0)
            {
                return negativeInfinity;
            }

            for (int i = 0; i < moveOptions.Count; i++)
            {

                int eval = SearchMoves(depth - 1, moveOptions[i].BoardResult, moveOptions[i].ScoreResult, false);
                if (eval > bestEval)
                {
                    bestEval = eval;
                }
            }
        }
        else
        {
            bestEval = positiveInfinity;
            List<RandomPlacementOption> moveOptions = new List<RandomPlacementOption>();
            moveOptions = MoveOptionsGenerator.RandomPlacementOptions(board, score);

            for (int i = 0; i < moveOptions.Count; i++)
            {
                int eval = SearchMoves(depth - 1, moveOptions[i].BoardResult, moveOptions[i].Score, true);
                if (eval < bestEval)
                {
                    bestEval = eval;
                }
            }
        }

        return bestEval;
    }

    private int SearchMovesTopLevel(int depth, HexBoard board, ulong score)
    {
        int bestEval;

        bestEval = negativeInfinity;
        List<PlayerMoveOption> moveOptions = MoveOptionsGenerator.PlayerMoveOptions(board, score);

        for (int i = 0; i < moveOptions.Count; i++)
        {

            int eval = SearchMoves(depth - 1, moveOptions[i].BoardResult, moveOptions[i].ScoreResult, false);
            if (eval > bestEval)
            {
                bestEval = eval;
                _bestDirection = moveOptions[i].Direction;
            }
        }
        if (bestEval == negativeInfinity)
        {
            _bestDirection = moveOptions[0].Direction;
        }


        return bestEval;
    }


    public void AbortSearch()
    {

    }
}
