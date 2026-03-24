using System;

using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;

using Ui;





namespace gameManagement

{

    public class GameManager : MonoBehaviour

    {

        public MatrisCreater matrisCreater;

        public GameUI uiBuilder;

        public BoardUI boardUI;



        private int[,] solvedMatris;

        private int[,] puzzleMatris;

        private int[] remainingNumber = new int[10];



        private int mistakes = 0;

        private int remainingHint = 3;

        private int remainingSmartClears = 3;



        private Stack<int> moveHistory = new Stack<int>();





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

            remainingSmartClears = 3;



            uiBuilder.UpdateSmartClearUI(remainingSmartClears);

            uiBuilder.UpdateMistakeCountUI(0);

            uiBuilder.UpdateHintUI(remainingHint);

            moveHistory.Clear();



            bool puzzleCreated = false;

            while (!puzzleCreated)

            {

                matrisCreater.GenerateMatris();

                solvedMatris = matrisCreater.GetGrid();

                remainingNumber = new int[10];

                puzzleCreated = CreatePuzzle(difficulty);

            }



            uiBuilder.LoadMatrixToUI(puzzleMatris);

        }



        private bool CreatePuzzle(Difficulty difficulty)

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



            List<int> cellList = new List<int>(81);

            for (int i = 0; i < 81; i++)

            {

                cellList.Add(i);

            }



            for (int i = 0; i < cellList.Count; i++)

            {

                int temp = cellList[i];

                int randomIndex = UnityEngine.Random.Range(i, cellList.Count);

                cellList[i] = cellList[randomIndex];

                cellList[randomIndex] = temp;

            }



            int removed = 0;

            foreach (int index in cellList)

            {

                if (removed >= cellsToRemove)

                    break;



                int r = index / 9;

                int c = index % 9;



                int temp = puzzleMatris[r, c];

                puzzleMatris[r, c] = 0;



                int solutions = GetSolutionCount(puzzleMatris);



                if (solutions == 1)

                {

                    remainingNumber[temp] += 1;

                    removed++;

                }

                else

                {

                    puzzleMatris[r, c] = temp;

                }

            }



            if (removed < cellsToRemove)

            {

                return false;

            }



            uiBuilder.UpdateRemainingNumbersUI(remainingNumber);

            return true;

        }



        private int GetSolutionCount(int[,] grid)

        {

            int minCandidates = 10;

            int bestRow = -1;

            int bestCol = -1;

            List<int> bestCandidates = null;





            bool foundSingle = false;



            for (int r = 0; r < 9; r++)

            {

                for (int c = 0; c < 9; c++)

                {

                    if (grid[r, c] == 0)

                    {

                        List<int> candidates = GetValidCandidates(grid, r, c);



                        if (candidates.Count == 1)

                        {

                            bestRow = r;

                            bestCol = c;

                            bestCandidates = candidates;



                            foundSingle = true;

                            break;

                        }



                        if (candidates.Count < minCandidates)

                        {

                            minCandidates = candidates.Count;

                            bestRow = r;

                            bestCol = c;

                            bestCandidates = candidates;

                        }

                    }

                }





                if (foundSingle) break;

            }



            if (bestRow == -1) return 1;

            if (bestCandidates.Count == 0) return 0;



            int solutionCount = 0;



            foreach (int candidate in bestCandidates)

            {

                grid[bestRow, bestCol] = candidate;

                solutionCount += GetSolutionCount(grid);

                grid[bestRow, bestCol] = 0;



                if (solutionCount > 1) break;

            }



            return solutionCount;

        }



        private List<int> GetValidCandidates(int[,] grid, int row, int col)

        {

            List<int> candidates = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };



            for (int i = 0; i < 9; i++)

            {

                candidates.Remove(grid[row, i]);

                candidates.Remove(grid[i, col]);

            }



            int startRow = row - (row % 3);

            int startCol = col - (col % 3);



            for (int r = 0; r < 3; r++)

            {

                for (int c = 0; c < 3; c++)

                {

                    candidates.Remove(grid[startRow + r, startCol + c]);

                }

            }



            return candidates;

        }

        public void GiveHint()

        {

            while (remainingHint > 0)

            {



                int r = UnityEngine.Random.Range(0, 9);

                int c = UnityEngine.Random.Range(0, 9);



                if (puzzleMatris[r, c] == 0)

                {

                    remainingHint--;

                    moveHistory.Push(r * 9 + c);

                    puzzleMatris[r, c] = solvedMatris[r, c];

                    uiBuilder.SetHintCell(r, c, puzzleMatris[r, c]);

                    uiBuilder.UpdateHintUI(remainingHint);

                    remainingNumber[puzzleMatris[r, c]] -= 1;

                    uiBuilder.UpdateRemainingNumbersUI(remainingNumber);

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

            for (int row = 0; row < 9; row++)

            {

                for (int col = 0; col < 9; col++)

                {

                    if (puzzleMatris[row, col] == 0)

                    {

                        puzzleMatris[row, col] = solvedMatris[row, col];

                        remainingNumber[puzzleMatris[row, col]] -= 1;

                        uiBuilder.UpdateRemainingNumbersUI(remainingNumber);

                        uiBuilder.SetHintCell(row, col, puzzleMatris[row, col]);

                    }



                }



            }



        }



        private void CheckPlayerInput(int index, int number)

        {

            if (remainingNumber[number] <= 0)

            {

                return;

            }



            int r = index / 9;

            int c = index % 9;



            if (puzzleMatris[r, c] == 0)

            {

                moveHistory.Push(index);



                if (solvedMatris[r, c] == number)

                {

                    puzzleMatris[r, c] = number;

                    remainingNumber[number] -= 1;

                    //moveHistory.Push(index); //TODO: Command pattern

                    uiBuilder.UpdateRemainingNumbersUI(remainingNumber);

                    uiBuilder.SetPlayerCell(index, number, false);

                    CheckWinCondition();

                }

                else

                {

                    mistakes++;

                    uiBuilder.SetPlayerCell(index, number, true);

                    uiBuilder.UpdateMistakeCountUI(mistakes);



                    if (mistakes >= 3)

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



        public void Undo()

        {

            if (moveHistory.Count == 0) return;



            int lastIndex = moveHistory.Pop();

            int r = lastIndex / 9;

            int c = lastIndex % 9;



            int currentNumber = puzzleMatris[r, c];



            remainingNumber[currentNumber]++;

            uiBuilder.UpdateRemainingNumbersUI(remainingNumber);



            puzzleMatris[r, c] = 0;

            boardUI.ClearCell(lastIndex);



        }



    }



}