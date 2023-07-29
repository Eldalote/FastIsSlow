using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexBoard
{
    // Ulong containing the least significant bits of each space.
    public ulong LSB;
    // Ulong containing the most significant bits of each space.
    public ulong MSB;
}

public class FullValueLines
{
    // Ulong Array holding the 4 full value lines.
    public ulong[] Lines;
    // Constructor defining the length of the array.
    public FullValueLines()
    {
        Lines = new ulong[4];
    }

    // Flip each line from AaBbCcDd to DdCcBbAa.
    public void FlipLines()
    {
        for (int i = 0; i < Lines.Length; i++)
        {
            Lines[i] = ((Lines[i] & 0xFF) << 24) | ((Lines[i] & 0xFF00) << 8) | ((Lines[i] & 0xFF0000) >> 8) | ((Lines[i] & 0xFF000000) >> 24);
        }
    }
    // Move and merge on each line. Returns the increase in score caused by the merges.
    public ulong MoveMergeLines()
    {
        // Keep a scoreIncrease variable.
        ulong scoreIncrease = 0;

        // Loop through the lines.
        for (int i = 0; i < Lines.Length; i++)
        {
            // Only do work flipping the line if it is not 0.
            if (Lines[i] != 0)
            {
                // If the first place is 0, shift over. Keep doing this until it is no longer 0, keep track of how many times this has been done,
                // to shorten the later loops. While loop should never get stuck, since it just checked if the line is 0.
                int emptyShifts = 0;
                while ((Lines[i] & 0xFF) == 0)
                {
                    Lines[i] >>= 8;
                    emptyShifts++;
                }
                // After this we check each space it is possible to move and/ or merge to.
                // This is space 0, 1, and 2, since we won't move to space 3 (wrong direction). This can be shortened by the number of times
                // already shifted over in the previous loop.
                // Loop itterator mt (move target).
                for (int mt = 0; mt < (3 - emptyShifts); mt++) // TESTING 3 -  is 4 - in old file.
                {
                    // Create the masks for the space to move to, and the space to check.
                    ulong moveTargetMask = (ulong)0xFF << (mt * 8);
                    ulong checkSpaceMask = (ulong)0xFF00 << (mt * 8);
                    // Store the offset between moveTargetMask and checkLocationMask. Must be stored outside the inner loops, as it can be independantly increased in 
                    // different loops.
                    int moveCheckOffset = 8;

                    // If the check space is 0, find a block to move to this location.
                    if ((Lines[i] & moveTargetMask) == 0)
                    {
                        // Loop over the possible check spaces and find a non-0 value. The further along in the mt-loop the fewer spaces have to be checked.
                        // Loop itterator cs (CheckSpace)
                        for (int cs = 0; cs < (3 - (mt + emptyShifts)); cs++) // TESTING 3 or 4?
                        {
                            // If a non-0 value is found, move it to the move target space.
                            if ((Lines[i] & checkSpaceMask) != 0)
                            {
                                // First set the found value to the move target space.
                                // The difference in location is stored in moveCheckOffset.
                                Lines[i] = Lines[i] | ((Lines[i] & checkSpaceMask) >> moveCheckOffset);
                                // Then remove the value from it's original location.
                                Lines[i] &= ~checkSpaceMask;
                                // Since a block was found, the search must stop.
                                break;
                            }
                            // Move the checkspace over one space.
                            checkSpaceMask <<= 8;
                            moveCheckOffset += 8;
                        }
                    }
                    // Now look again if the the value of the space is non-0 (a block might have just moved here), then check if a merge is possible.
                    if ((Lines[i] & moveTargetMask) != 0)
                    {
                        // The same loop as for the move is repeated, except now the value is also checked to match to the value in the moveTarget space.
                        // Loop itterator cs (CheckSpace)
                        for (int cs = 0; cs < (3 - (mt + emptyShifts)); cs++) // TESTING 3 or 4?
                        {
                            // If the checking space is 0, shift the check over, and continue the search.
                            if ((Lines[i] & checkSpaceMask) == 0)
                            {
                                checkSpaceMask <<= 8;
                                moveCheckOffset += 8;
                            }
                            // else if the checking space value is equal to the move target value, merge them, then break the search.
                            else if (((Lines[i] & checkSpaceMask) >> moveCheckOffset) == (Lines[i] & moveTargetMask))
                            {
                                // Clear the value from the check space.
                                Lines[i] &= ~checkSpaceMask;
                                // While overflow of the new location is possible in the programming sense, it is not possible to actually get such a 
                                // value in game, so it is not needed to check for overflow.
                                // But the value is needed for the score calculation, so it needs to be extracted from the space.
                                ulong spaceValue = (Lines[i] & moveTargetMask) >> (mt * 8);
                                // Increase that value by one, and the space value by one.
                                Lines[i] += ((ulong)1 << (mt * 8));
                                spaceValue++;
                                // Increase the score.
                                scoreIncrease += ((ulong)1 << (int)spaceValue);
                                // Search for merge target is over.
                                break;

                            }
                            // If a value is found that is non-0, but also doesn't match the move target value, the search is over.
                            else { break; }

                        }
                    }
                }
            }
        }
        // When that is all done, the score increase is returned.
        return scoreIncrease;
    }
}


public static class HexBoardActions
{
    // Split up the HexBoard into Rows, full value lines
    public static FullValueLines GetFullValueRows(HexBoard board)
    {
        // New working values.
        FullValueLines fullValueRows = new FullValueLines();

        // Loop through the rows, and extract the full values.
        for (int i = 0; i < 4; i++)
        {
            // The mask is 0xFFFF shifted over i rows.
            ulong selectionMask = ((ulong)0xFFFF << (16 * i));
            // Get LSB and MSB for the right row
            ulong LSB, MSB;
            LSB = board.LSB & selectionMask;
            MSB = board.MSB & selectionMask;
            // Shift them over to be at the end.
            LSB >>= (i * 16);
            MSB >>= (i * 16);
            // Combine them. abcd(lsb) + ABCD(msb) => AaBbCcDd
            ulong fullValueLong = (LSB & 0xF) | ((MSB & 0xF) << 4) | ((LSB & 0xF0) << 4) | ((MSB & 0xF0) << 8) |
                ((LSB & 0xF00) << 8) | ((MSB & 0xF00) << 12) | ((LSB & 0xF000) << 12) | ((MSB & 0xF000) << 16);
            fullValueRows.Lines[i] = fullValueLong;
        }
        return fullValueRows;
    }
    // Split up the HexBoard into Columns, full value lines
    public static FullValueLines GetFullValueColumns(HexBoard board)
    {
        // New working variable.
        FullValueLines fullValueColumns = new FullValueLines();

        // Loop through the columns, and extract the full values.
        for (int i = 0; i < 4; i++)
        {
            // Mask is the columns, shifted over 1 hex per itteration.
            ulong selectionMask = ((ulong)0x000F000F000F000F << (4 * i));
            // Get LSB and MSB for the right row
            ulong LSB, MSB;
            LSB = board.LSB & selectionMask;
            MSB = board.MSB & selectionMask;
            // Shift them over to be at the end.
            LSB >>= (i * 4);
            MSB >>= (i * 4);
            // Combine them. 000a000b000c000d(lsb) + 000A000B000C000D(msb) => AaBbCcDd
            ulong fullValueLong = (LSB & 0xF) | ((MSB & 0xF) << 4) | ((LSB & 0xF0000) >> 8) | ((MSB & 0xF0000) >> 4) |
               ((LSB & 0xF00000000) >> 16) | ((MSB & 0xF00000000) >> 12) | ((LSB & 0xF000000000000) >> 24) | ((MSB & 0xF000000000000) >> 20);
            fullValueColumns.Lines[i] = fullValueLong;
        }
        return fullValueColumns;
    }
    // Remake a HexBoard from full value lines, in Rows form.
    public static HexBoard RebuildHexBoardFromFullValueRows(FullValueLines fullValueRows)
    {
        // New working variable.
        HexBoard board = new HexBoard();

        // Loop through the lines, split them up, and put them in the right place in the hexBoard.
        for (int i = 0; i < 4; i++)
        {
            ulong LSB, MSB;
            // Split into the LSBs and MSBs.
            // AaBbCcDc => abcd + ABCD
            LSB = (fullValueRows.Lines[i] & 0xF) | ((fullValueRows.Lines[i] & 0xF00) >> 4) |
                ((fullValueRows.Lines[i] & 0xF0000) >> 8) | ((fullValueRows.Lines[i] & 0xF000000) >> 12);

            MSB = ((fullValueRows.Lines[i] & 0xF0) >> 4) | ((fullValueRows.Lines[i] & 0xF000) >> 8) |
                ((fullValueRows.Lines[i] & 0xF00000) >> 12) | ((fullValueRows.Lines[i] & 0xF0000000) >> 16);
            // Shift them to the left i rows
            LSB <<= (i * 16);
            MSB <<= (i * 16);
            // Add them to the hexBoard
            board.LSB += LSB;
            board.MSB += MSB;
        }
        return board;
    }
    // Remake a HexBoard from full value lines, in Columns form.
    public static HexBoard RebuildHexBoardFromFullValueColumns(FullValueLines fullValueColumns)
    {
        // New working variable.
        HexBoard board = new HexBoard();

        // Loop through the lines, split them up, and put them in the right place in the hexBoard.
        for (int i = 0; i < 4; i++)
        {
            ulong LSB, MSB;
            // Split into the LSBs and MSBs.
            // AaBbCcDc => 000a000b000c000d + 000A000B000C000D
            LSB = (fullValueColumns.Lines[i] & 0xF) | ((fullValueColumns.Lines[i] & 0xF00) << 8) |
                ((fullValueColumns.Lines[i] & 0xF0000) << 16) | ((fullValueColumns.Lines[i] & 0xF000000) << 24);

            MSB = ((fullValueColumns.Lines[i] & 0xF0) >> 4) | ((fullValueColumns.Lines[i] & 0xF000) << 4) |
                ((fullValueColumns.Lines[i] & 0xF00000) << 12) | ((fullValueColumns.Lines[i] & 0xF0000000) << 20);
            // Shift them to the left i columns
            LSB <<= (i * 4);
            MSB <<= (i * 4);
            // Add them to the hexBoard
            board.LSB += LSB;
            board.MSB += MSB;
        }
        return board;
    }

    // Helpfull for testing, prints the content of a hexboard to string, and optionally to debug.log.
    public static string PrintHexBoard(HexBoard board, bool print)
    {
        string hexBoardString = new string("");
        FullValueLines fullValueLines = new FullValueLines();
        fullValueLines = GetFullValueRows(board);
        // Start at 3 working down to 0, because the game display has row 3 on top.
        for (int y = 3; y >= 0; y--)
        {
            for (int x = 0; x < 4; x++)
            {
                // Get the value of the space, and add the the to string to the string.
                ulong spaceValue = (fullValueLines.Lines[y] >> (8 * x)) & 0xFF;
                ulong printValue = 0;
                if (spaceValue != 0)
                {
                    printValue = (ulong)1 << (int)spaceValue;
                }

                hexBoardString = hexBoardString + printValue.ToString().PadLeft(6, '0') + ", ";
            }
            hexBoardString += "\n";
        }
        if (print)
        {
            Debug.Log(hexBoardString);
        }
        return hexBoardString;
    }

    // Functions working with the HexBoard class. Only does the move and merge, does not place new block.
    // Returns Hexboard after move-merge, ulong score after move-merge, and bool whether move and/or merge happened.
    public static (HexBoard, ulong, bool) MoveAndMerge(HexBoard originalBoard, ulong OriginalScore, MoveDirection direction)
    {
        // Set up variables we can work with.
        ulong score = OriginalScore;
        HexBoard board = originalBoard;
        FullValueLines lines = new FullValueLines();
        // If the direction is left or right, get rows.
        if (direction == MoveDirection.Left || direction == MoveDirection.Right)
        {
            lines = HexBoardActions.GetFullValueRows(board);
        }
        // if the direction is up or down, get columns.
        else
        {
            lines = HexBoardActions.GetFullValueColumns(board);
        }
        // The lines are now ready for moving down or left, but need to be flipped for moving up or right.
        if (direction == MoveDirection.Up || direction == MoveDirection.Right)
        {
            lines.FlipLines();
        }

        // After this, execute move and merge on the lines, note how much the score increased.
        ulong scoreincrease = lines.MoveMergeLines();
        score += scoreincrease;

        // After the move merge, the hexboard needs to be rebuild from the lines.
        // First, if the lines were flipped before, now flip them back.
        if (direction == MoveDirection.Up || direction == MoveDirection.Right)
        {
            lines.FlipLines();
        }
        // Then rebuild from columns or rows depending on direction.
        // If the direction is left or right, rebuild form rows.
        if (direction == MoveDirection.Left || direction == MoveDirection.Right)
        {
            board = HexBoardActions.RebuildHexBoardFromFullValueRows(lines);
        }
        // if the direction is up or down, rebuild from columns.
        else
        {
            board = HexBoardActions.RebuildHexBoardFromFullValueColumns(lines);
        }
        // Check if the board is still the same, and note the result.
        bool changeHappened = false;
        if (board.LSB != originalBoard.LSB || board.MSB != originalBoard.MSB)
        {
            changeHappened = true;
        }
        // Return the results
        return (board, score, changeHappened);

    }
    // Function to calculate the number of empty spaces on the board.
    public static int CalculateNumberOfEmptySpaces(HexBoard board)
    {
        int emptySpaces = 0;
        // Loop over every space.
        for (int i = 0; i < 16; i++)
        {
            // Check if the value of the space is 0, if it is, increment emptySpaces by one
            if (((board.LSB & ((ulong)0xF << (i * 4))) == 0) && ((board.MSB & ((ulong)0xF << (i * 4))) == 0))
            {
                emptySpaces++;
            }

        }
        return emptySpaces;
    }

    public static HexBoard SpawnNewBlock(HexBoard board, int locationRandom, int valueRandom)
    {
        int countDown = locationRandom;
        HexBoard workingBoard = board;
        // Loop over every space.
        for (int i = 0; i < 16; i++)
        {
            // Check if the value of the space is 0, if it is, check countDown. If countdown is 0, place new block here, if not, countDown--
            if ((board.LSB & ((uint)0xF << (i * 4))) == 0 && (board.MSB & ((uint)0xF << (i * 4))) == 0)
            {
                if (countDown == 0)
                {
                    workingBoard.LSB += (ulong)valueRandom << (i * 4);
                }
                else
                {
                    countDown--;
                }
            }

        }

        return workingBoard;
    }



}

public static class MoveOptionsGenerator
{


    public static List<PlayerMoveOption> PlayerMoveOptions(HexBoard startBoard, ulong startScore)
    {
        List<PlayerMoveOption> moveOptions = new List<PlayerMoveOption>();
        // Loop through the 4 move directions (0 through 3).
        for (int i = 0; i < 4; i++)
        {
            // Do a move in this direction, and record the results.
            HexBoard resultBoard = new HexBoard();
            ulong resultScore = 0;
            bool moveSuccess = false;
            MoveDirection direction = (MoveDirection)i;
            (resultBoard, resultScore, moveSuccess) = HexBoardActions.MoveAndMerge(startBoard, startScore, direction);
            // If something happened (moveSuccess) then this is a valid move option. Add it to the List.
            if (moveSuccess)
            {
                PlayerMoveOption moveOption = new PlayerMoveOption
                {
                    BoardResult = resultBoard,
                    ScoreResult = resultScore,
                    Direction = direction
                };
                moveOptions.Add(moveOption);
            }

        }
        // Return the list with possible move options.
        return moveOptions;
    }

    public static List<RandomPlacementOption> RandomPlacementOptions(HexBoard startBoard, ulong startScore)
    {
        List<RandomPlacementOption> placementOptions = new List<RandomPlacementOption>();
        // First calculate the number of empty spaces.
        int emptySpaces = HexBoardActions.CalculateNumberOfEmptySpaces(startBoard);
        // Loop for every empty space, add a block to that space, and add the new options to the list.
        for (int i = 0; i < emptySpaces; i++)
        {
            HexBoard boardValueOne = new HexBoard();
            HexBoard boardValueTwo = new HexBoard();
            boardValueOne = HexBoardActions.SpawnNewBlock(startBoard, i, 1);
            boardValueTwo = HexBoardActions.SpawnNewBlock(startBoard, i, 2);
            placementOptions.Add(new RandomPlacementOption { BoardResult = boardValueOne, Score = startScore });
            placementOptions.Add(new RandomPlacementOption { BoardResult = boardValueTwo, Score = startScore });
        }

        // Return the list
        return placementOptions;

    }

}

public class PlayerMoveOption
{

    public MoveDirection Direction;
    public HexBoard BoardResult;
    public ulong ScoreResult;
    public PlayerMoveOption() { }
}

public class RandomPlacementOption
{
    public HexBoard BoardResult;
    public ulong Score;

    public RandomPlacementOption() { }
}

public static class PositionEvaluator
{

    public static int EvaluatePosition(HexBoard board, ulong score)
    {
        return (int)score;
    }

}