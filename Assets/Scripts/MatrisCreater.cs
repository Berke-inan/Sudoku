using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatrisCreater : MonoBehaviour
{
    public SudokuGridBuilder uiBuilder;

    List<int> pool;
    int[,] grid;

    void Start()
    {
       GenerateMatris();
    }

    public void GenerateMatris()
    {
        bool isSuccessful = false;

        while (!isSuccessful)
        {
            isSuccessful = true;
            pool = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            grid = new int[9, 9];

            FillFirstRow();

            isSuccessful = NavigateBlocks();
        }
    }

    public int[,] GetGrid()
    {
        return grid;
    }

    private void FillFirstRow()
    {
        //ilk satır
        Buffer.BlockCopy(pool.OrderBy(x => UnityEngine.Random.value).ToArray(), 0, grid, 0, 9 * sizeof(int));
    }

    private bool NavigateBlocks()
    {
        int maxRetries = 50;

        for (int blockRow = 0; blockRow < 9; blockRow += 3)
        {
            for (int blockCol = 0; blockCol < 9; blockCol += 3)
            {
                bool blockSuccessful = false;
                int retries = 0;

                while (!blockSuccessful && retries < maxRetries)
                {
                    blockSuccessful = true;
                    retries++;

                    if (retries > 1)
                    {
                        ClearBlockCells(blockRow, blockCol);
                    }

                    pool = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

                    for (int innerRow = 0; innerRow < 3 && blockSuccessful; innerRow++)
                    {
                        for (int innerCol = 0; innerCol < 3 && blockSuccessful; innerCol++)
                        {
                            int row = blockRow + innerRow;
                            int col = blockCol + innerCol;

                            blockSuccessful = CheckAndFillCell(row, col);
                        }
                    }
                }

                if (!blockSuccessful)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ClearBlockCells(int blockRow, int blockCol)
    {
        for (int innerRow = 0; innerRow < 3; innerRow++)
        {
            for (int innerCol = 0; innerCol < 3; innerCol++)
            {
                int row = blockRow + innerRow;
                int col = blockCol + innerCol;
                if (row != 0)
                {
                    grid[row, col] = 0;
                }
            }
        }
    }

    private bool CheckAndFillCell(int row, int col)
    {
        int cellValue = grid[row, col];

        if (cellValue != 0)
        {
            pool.Remove(cellValue);
            return true;
        }
        else
        {
            List<int> removedForThisCell = new List<int>();

            for (int k = 0; k < 9; k++)
            {
                int rowValue = grid[row, k];
                int colValue = grid[k, col];

                if (pool.Remove(rowValue)) removedForThisCell.Add(rowValue);
                if (pool.Remove(colValue)) removedForThisCell.Add(colValue);
            }

            if (pool.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, pool.Count);
                int selectedNumber = pool[randomIndex];

                grid[row, col] = selectedNumber;

                pool.RemoveAt(randomIndex);
            }
            else
            {
                return false;
            }

            foreach (int restoredNumber in removedForThisCell)
            {
                if (!pool.Contains(restoredNumber))
                    pool.Add(restoredNumber);
            }

            return true;
        }
    }
}