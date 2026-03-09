using System;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public MatrisCreater matrisCreater;
    public SudokuGridBuilder uiBuilder;

    private int[,] solvedMatris;
    private int[,] puzzleMatris;

    private int mistakes = 0;
    private int remainingHint = 3;


    void Start()
    {
        uiBuilder.OnNumberEntered += CheckPlayerInput;
        uiBuilder.OnDifficultyChangedEvent += StartNewGame;

        StartNewGame(Difficulty.Medium);
    }

    public void StartNewGame(Difficulty difficulty)
    {
        mistakes = 0;
        remainingHint = 3;

        uiBuilder.UpdateMistakeCountUI(0);

        uiBuilder.UpdateHintUI(remainingHint);

        matrisCreater.GenerateMatris();

        solvedMatris = matrisCreater.GetGrid();

        CreatePuzzle(difficulty);


        uiBuilder.ClearAllCells();
        uiBuilder.LoadMatrixToUI(puzzleMatris);
    }

    private void CreatePuzzle(Difficulty difficulty)
    {
        puzzleMatris = new int[9, 9];
        int cellsToRemove = 0;

        if (difficulty == Difficulty.Easy) 
            cellsToRemove = 25;

        else if (difficulty == Difficulty.Medium) 
            cellsToRemove = 35;

        else if (difficulty == Difficulty.Hard) 
            cellsToRemove = 45;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                puzzleMatris[r, c] = solvedMatris[r, c];
            }
        }

        int removed = 0;
        while (removed < cellsToRemove)
        {
            int r = UnityEngine.Random.Range(0, 9);
            int c = UnityEngine.Random.Range(0, 9);

            if (puzzleMatris[r, c] != 0)
            {
                puzzleMatris[r, c] = 0;
                removed++;
            }
        }
    }

    public void GiveHint()
    {
        while (remainingHint>0)
        {
            
            int r = UnityEngine.Random.Range(0, 9);
            int c = UnityEngine.Random.Range(0, 9);

            if (puzzleMatris[r, c] == 0)
            {
                remainingHint--;
                puzzleMatris[r, c] = solvedMatris[r, c];
                uiBuilder.SetHintCell(r, c, puzzleMatris[r, c]);
                uiBuilder.UpdateHintUI(remainingHint);
                CheckWinCondition();
                return;
            }
            else
            {
                GiveHint();
                return;
            }
        }

    }

    public void SolvePuzzle()
    {
        for (int row= 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (puzzleMatris[row, col] == 0) 
                { 
                    puzzleMatris[row, col] = solvedMatris[row, col];
                    uiBuilder.SetHintCell(row, col, puzzleMatris[row, col]);
                }

            }

        }

    }

    private void CheckPlayerInput(int index, int number)
    {
        int r = index / 9;
        int c = index % 9;

        if (puzzleMatris[r, c] == 0)
        {
            if (solvedMatris[r, c] == number)
            {
                puzzleMatris[r, c] = number;
                uiBuilder.SetPlayerCell(index, number, false);
                CheckWinCondition();
            }
            else
            {
                mistakes++;
                uiBuilder.SetPlayerCell(index, number, true);
                uiBuilder.UpdateMistakeCountUI(mistakes);

                if (mistakes == 4)
                {
  
                    uiBuilder.ShowGameOverDialog();
                }
            }
        }
    }

 
    private void CheckWinCondition()
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
              
                if (puzzleMatris[r, c] == 0) return;
            }
        }

        uiBuilder.ShowWinDialog();
    }
}