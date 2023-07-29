using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveSearcherWorking
{
    /// <summary>
    /// Most up to date version of the move searcher. 
    /// Features:
    /// Mini-max recursive searching.
    /// Parallel multithreading, double deep. (The first max 4 options are split into threads, and each followup random placement too.)
    /// Optional double deep parallelisation, double deep is probably slower on lower core count systems.
    /// </summary>


    // Set values for minimum and maximum evaluation. Named as Infinity because of theory naming.
    const int positiveInfinity = 100000;
    const int negativeInfinity = -positiveInfinity;


    // Simply starts the search, and gives the answer back, optionally with some debug logging.
    public (MoveDirection, uint, int) StartSearch(HexBoard board, ulong score, int depth, bool doubleDeepParallel)
    {
        int evaluation;
        uint nodesSearched;
        MoveDirection bestDirection;
        (bestDirection, nodesSearched, evaluation) = SearchMovesTopLevel(depth, board, score, doubleDeepParallel);
        //Debug.Log($"Total end states evaluated: {nodesSearched}, end eval: {evaluation}, doubleparallel on: {doubleDeepParallel}");
        //Console.WriteLine($"Total end states evaluated: {nodesSearched}, end eval: {eval}, end direction {bestDirection}.");
        return (bestDirection, nodesSearched, evaluation);

    }

    // The main recursive search function, after the initial 2 top level searches.
    public (uint, int) SearchMoves(int depth, HexBoard board, ulong score, bool playerToMove)
    {
        // If depth is zero, return 1 (number of positions evaluated in this function) and the evaluation of the current board.
        if (depth == 0)
        {
            return (1, PositionEvaluator.EvaluatePosition(board, score));
        }
        // Keep track of how many nodes are searched in this iteration.
        uint nodesSearched = 0;

        int bestEvaluation;
        // There are two "players", the player, and random number determining the placement of the next block.
        // This is the player, or the maximizing player in mini-max
        if (playerToMove)
        {
            // Start of with the worst possible evaluation, look for beter ones.
            bestEvaluation = negativeInfinity;
            // Generate a list with all possible move options.
            List<PlayerMoveOption> moveOptions = MoveOptionsGenerator.PlayerMoveOptions(board, score);

            // If no more moves are possible, it's game over. Return worst possible evaluation.
            if (moveOptions.Count == 0)
            {
                return (0, negativeInfinity);
            }
            // Loop over the possible move options.
            for (int i = 0; i < moveOptions.Count; i++)
            {
                int evaluation;
                uint nodesThisSearch;
                // Get the evaluation of the move, plus how many nodes were searched to get that evaluation.
                (nodesThisSearch, evaluation) = SearchMoves(depth - 1, moveOptions[i].BoardResult, moveOptions[i].ScoreResult, false);
                nodesSearched += nodesThisSearch;
                // If this evaluation is better than the best evaluation, store it.
                if (evaluation > bestEvaluation)
                {
                    bestEvaluation = evaluation;
                }
            }
        }
        // This is the "minimizing player" or simply all the options for random placement.
        else
        {
            // The worst possible evaluation for the minimizing player is positiveInfinity. Find better options.
            bestEvaluation = positiveInfinity;
            // Generate a list with all possible boardstates after a random placement.
            List<RandomPlacementOption> moveOptions = MoveOptionsGenerator.RandomPlacementOptions(board, score);
            // Loop over the possibilities.
            for (int i = 0; i < moveOptions.Count; i++)
            {
                int evaluation;
                uint nodesThisSearch;
                // Get the evaluation of the current board, and how many nodes were evaluated for that board.
                (nodesThisSearch, evaluation) = SearchMoves(depth - 1, moveOptions[i].BoardResult, moveOptions[i].Score, true);
                nodesSearched += nodesThisSearch;
                // If the evaluation is better (lower) than the best, store it.
                if (evaluation < bestEvaluation)
                {
                    bestEvaluation = evaluation;
                }
            }
        }
        // Return how many nodes were searched and the best evaluation result.
        return (nodesSearched, bestEvaluation);
    }
    // The Top level of the recursive search. This one splits the possible searches into parallel threads. Returns the best direction, in addition to normal search results. 
    private (MoveDirection, uint, int) SearchMovesTopLevel(int depth, HexBoard board, ulong score, bool doubleDeepParallel)
    {
        // Keep track of the best direction, and how many nodes were searched.
        uint nodesSearched = 0;
        MoveDirection bestDirection = MoveDirection.None;
        // Start with worst possible.
        int bestEvaluation = negativeInfinity;
        // Generate list of all possible moves.
        List<PlayerMoveOption> moveOptions = MoveOptionsGenerator.PlayerMoveOptions(board, score);
        // Make list for results for each move. Since the searches are parallel, it cannot be relied upon that they are resolve in a predictable order.
        List<Results> resultList = new List<Results>();
        // Do a search for each possible move on a seperate parallel thread.
        // When double deep is enabled, call the second level function, if not, call the normal search function.
        if (doubleDeepParallel)
        {
            Parallel.ForEach(moveOptions, move =>
            {
                int evaluation;
                uint nodesThisSearch;
                // As normal, get results, but don't compare just yet, store them in the list.
                (nodesThisSearch, evaluation) = SearchMovesSecondLevel(depth - 1, move.BoardResult, move.ScoreResult);
                nodesSearched += nodesThisSearch;
                resultList.Add(new Results(evaluation, move));
            });
        }
        else
        {
            Parallel.ForEach(moveOptions, move =>
            {
                int evaluation;
                uint nodesThisSearch;
                // As normal, get results, but don't compare just yet, store them in the list.
                (nodesThisSearch, evaluation) = SearchMoves(depth - 1, move.BoardResult, move.ScoreResult, false);
                nodesSearched += nodesThisSearch;
                resultList.Add(new Results(evaluation, move));
            });
        }

        // Once all the results are in, compare them, and get the direction of the best result.
        for (int i = 0; i < resultList.Count; i++)
        {
            if (resultList[i].Evaluation > bestEvaluation)
            {
                bestEvaluation = resultList[i].Evaluation;
                bestDirection = resultList[i].MoveOption.Direction;
            }


        }
        // If all possible moves returned with evaluation of negativeInfinity, just go with the first possible.
        if (bestEvaluation == negativeInfinity)
        {
            bestDirection = moveOptions[0].Direction;
        }
        // Return best direction, how many nodes were searched, and the best evaluation.
        return (bestDirection, nodesSearched, bestEvaluation);
    }
    // Second level of the recurse search. This one also splits the searches into parallel threads. Fairly sure this is only faster in high core count systems.
    private (uint, int) SearchMovesSecondLevel(int depth, HexBoard board, ulong score)
    {

        uint nodesSearched = 0;
        int bestEvaluation = positiveInfinity;
        // Get all moves.
        List<RandomPlacementOption> moveOptions = new List<RandomPlacementOption>();
        moveOptions = MoveOptionsGenerator.RandomPlacementOptions(board, score);
        // Store results in a list.
        List<int> evaluationList = new List<int>();
        // Split all searches into parallel threads.
        Parallel.ForEach(moveOptions, move =>
        {
            int eval;
            uint nodesThisSearch;
            (nodesThisSearch, eval) = SearchMoves(depth - 1, move.BoardResult, move.Score, true);
            nodesSearched += nodesThisSearch;
            // Get results, store them in list for comparisson once all are in.
            evaluationList.Add(eval);
        });
        // Compare them.
        for (int i = 0; i < evaluationList.Count; i++)
        {
            if (evaluationList[i] < bestEvaluation)
            {
                bestEvaluation = evaluationList[i];
            }
        }
        return (nodesSearched, bestEvaluation);
    }
    private struct Results
    {
        public int Evaluation;
        public PlayerMoveOption MoveOption;
        public Results(int evaluation, PlayerMoveOption moveOption)
        {
            Evaluation = evaluation;
            MoveOption = moveOption;
        }
    }
}
