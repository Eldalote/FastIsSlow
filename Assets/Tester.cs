using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

public class Tester : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MoveSearcherOriginal moveSearcherOriginal = new MoveSearcherOriginal();
        MoveSearcherWorking moveSearcherWorking = new MoveSearcherWorking();

        HexBoard[] boardToTestArray = new HexBoard[] {
            new HexBoard { LSB = 0x1001, MSB = 0 },
            new HexBoard { LSB = 0x102081, MSB = 0 },
            new HexBoard { LSB = 0x101101001, MSB = 0 },
            new HexBoard { LSB = 0x11110001001, MSB = 0 },
            new HexBoard { LSB = 0x12861001, MSB = 0 },
            new HexBoard { LSB = 0x01991221001, MSB = 0x1 },
            new HexBoard { LSB = 0x2002, MSB = 0 },
            new HexBoard { LSB = 0x3003, MSB = 0 },
            new HexBoard { LSB = 0x4004000000004004, MSB = 0 },
            new HexBoard { LSB = 0x1012345678901, MSB = 0 }
        };
        ulong[] scoreToTestArray = new ulong[] {
            0,
            1,
            18265431,
            0,
            0,
            9,
            98,
            1000,
            0,
            10
        };
        int depth = 6;

        Stopwatch stopwatch = new Stopwatch();


        // First the original.
        stopwatch.Start();
        uint originalTotalNodes = 0;
        int originalTotalEvaluation = 0;
        for (int i = 0; i < 10; i++)
        {
            uint nodes;
            int eval;
            MoveDirection move;
            (move, nodes, eval) = moveSearcherOriginal.StartSearch(boardToTestArray[i], scoreToTestArray[i], depth);
            originalTotalEvaluation += eval;
            originalTotalNodes += nodes;
        }
        stopwatch.Stop();
        TimeSpan timeOriginal = stopwatch.Elapsed;
        stopwatch.Reset();

        // Then the working, no double threading.
        stopwatch.Start();
        uint workingNoDoubleTotalNodes = 0;
        int workingNoDoubleEvaluation = 0;
        for (int i = 0; i < 10; i++)
        {
            uint nodes;
            int eval;
            MoveDirection move;
            (move, nodes, eval) = moveSearcherWorking.StartSearch(boardToTestArray[i], scoreToTestArray[i], depth, false);
            workingNoDoubleEvaluation += eval;
            workingNoDoubleTotalNodes += nodes;
        }
        stopwatch.Stop();
        TimeSpan timeWorkingNoDouble = stopwatch.Elapsed;
        stopwatch.Reset();

        // Then the working, double threading.
        stopwatch.Start();
        uint workingDoubleTotalNodes = 0;
        int workingDoubleEvaluation = 0;
        for (int i = 0; i < 10; i++)
        {
            uint nodes;
            int eval;
            MoveDirection move;
            (move, nodes, eval) = moveSearcherWorking.StartSearch(boardToTestArray[i], scoreToTestArray[i], depth, true);
            workingDoubleEvaluation += eval;
            workingDoubleTotalNodes += nodes;
        }
        stopwatch.Stop();
        TimeSpan timeWorkingDouble = stopwatch.Elapsed;
        stopwatch.Reset();

        Debug.Log($"Original:\n" + $"Total Evaluation: {originalTotalEvaluation}, Total nodes {originalTotalNodes}, Time taken: {timeOriginal.Seconds}s, {timeOriginal.Milliseconds}ms");
        Debug.Log($"Working no double threading:\n" + $"Total Evaluation: {workingNoDoubleEvaluation}, Total nodes {workingNoDoubleTotalNodes}, Time taken: {timeWorkingNoDouble.Seconds}s, {timeWorkingNoDouble.Milliseconds}ms.");
        Debug.Log($"Working double threading:\n" + $"Total Evaluation: {workingDoubleEvaluation}, Total nodes {workingDoubleTotalNodes}, Time taken: {timeWorkingDouble.Seconds}s, {timeWorkingDouble.Milliseconds}ms.");




    }

    
}
