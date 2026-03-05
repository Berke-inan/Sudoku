using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;

public class MatrisCreater : MonoBehaviour
{

    public SudokuGridBuilder uiBuilder;

    List<int> pool = new List<int>{1, 2, 3, 4, 5, 6, 7, 8, 9};

    int[,] grid = new int[9, 9];


    void Start()
    {
        //ilk satır
        //Array.Copy(pool.OrderBy(x => UnityEngine.Random.value).ToArray(), 0, grid, 0, 9); hata verdi o yüzden aşağıdakini kullandım

        Buffer.BlockCopy(pool.OrderBy(x => UnityEngine.Random.value).ToArray(), 0, grid, 0, 9 * sizeof(int));

        pool = pool.OrderBy(x => UnityEngine.Random.value).ToList();

        //ilk sütun
        pool.Remove(grid[0, 0]);

        for (int i = 1; i < 9; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);

            grid[i, 0] = pool[randomIndex];

            pool.RemoveAt(randomIndex);
        }

 




        
        for (int satir = 0; satir < 9; satir+=3)
        {
            for (int sutun = 0; sutun < 9; sutun += 3)
            {

                pool = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                for (int icSatir = 0; icSatir < 3; icSatir ++)
                {
                    for (int icSutun = 0; icSutun < 3; icSutun++)
                    {
                        
                        int row = satir+icSatir;
                        int col = sutun+icSutun;
                        int deger = grid[row,col];

                        if (deger!=0)
                        {
                            pool.Remove(deger);
                        }
                        else
                        {
                            // GEÇİCİ ÇIKARTMA YÖNTEMİ
                            List<int> buHucredeSilinenler = new List<int>();

                            // O anki satır ve sütundaki sayıları havuzdan geçici olarak çıkart
                            for (int k = 0; k < 9; k++)
                            {
                                int satirDegeri = grid[row, k];
                                int sutunDegeri = grid[k, col];

                                if (pool.Remove(satirDegeri)) buHucredeSilinenler.Add(satirDegeri);
                                if (pool.Remove(sutunDegeri)) buHucredeSilinenler.Add(sutunDegeri);
                            }

                            // KRİTİK: Havuzda sayı kaldıysa seç (Index out of range koruması)
                            if (pool.Count > 0)
                            {
                                int randomIndex = UnityEngine.Random.Range(0, pool.Count);
                                int secilen = pool[randomIndex];

                                grid[row, col] = secilen;

                                // Seçtiğimiz sayıyı 3x3 havuzundan KALICI olarak sil
                                pool.RemoveAt(randomIndex);
                            }

                            // GERİ EKLEME: Geçici olarak sildiğimiz tüm sayıları havuza geri koy
                            foreach (int geriGelen in buHucredeSilinenler)
                            {
                                // Sadece havuzda yoksa ekle (kopya oluşmasın diye)
                                if (!pool.Contains(geriGelen))
                                    pool.Add(geriGelen);
                            }
                        }
                    }
                }
            }
        }

        uiBuilder.LoadMatrixToUI(grid);


    } 
}
