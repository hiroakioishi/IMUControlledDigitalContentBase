using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 迷路生成プログラム
/// http://www5d.biglobe.ne.jp/~stssk/maze/make.html
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    /// <summary>
    /// 迷路の幅
    /// </summary>
    public int Width  = 12;    
    /// <summary>
    /// 迷路の高さ
    /// </summary>
    public int Height = 12;
    /// <summary>
    /// 迷路を表わす2次元配列
    /// </summary>
    public int[,] Maze;

    void Start()
    {
        Maze = GenerateMaze(Width, Height);    
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Maze = GenerateMaze(Width, Height);
        }
    }

    void OnDrawGizmos()
    {
        if (Maze == null)
            return;

        for (var j = 0; j < Maze.GetLength(1); j++)
        {
            for (var i = 0; i < Maze.GetLength(0); i++)
            {
                var pos = new Vector3(i, 0.0f, j) - new Vector3(-Width * 0.5f, 0.0f, -Height * 0.5f);
                Gizmos.DrawSphere(pos, Maze[i, j] < 0.5f ? 0.1f : 0.5f);
            }            
        }
    }

    /// <summary>
    /// 迷路のデータを生成
    /// </summary>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <returns></returns>
    int[,] GenerateMaze(int width, int height)
    {
        // 迷路のデータを格納するための配列
        int[,] maze = new int[width, height];
        // 各セルを壁（1）として初期化
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                maze[x, y] = 1;

        // DFSアルゴリズムを使用した迷路の生成
        // 探索用のスタック
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        // 開始地点
        var start = new Vector2Int(1, 1);
        // 開始地点を通路(0)に設定
        maze[start.x, start.y] = 0;
        // スタックに開始地点を追加
        stack.Push(start);

        while (stack.Count > 0)
        {
            // 現在のセルをスタックから取り出す
            Vector2Int current = stack.Pop();
            // 未訪問の隣接セルを取得
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current, maze);
            
            if (neighbors.Count > 0)
            {
                // 現在のセルを再びスタックに追加
                stack.Push(current);
                // ランダムに隣接セルを取得
                Vector2Int chosen = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                // 現在のセルと選択された隣接セルの間の壁を取り除く
                RemoveWall(current, chosen, maze);
                // 選択された隣接セルを通路に設定する
                maze[chosen.x, chosen.y] = 0;
                // 選択された隣接セルをスタックに追加
                stack.Push(chosen);
            }
        }
        // 生成された迷路を返す
        return maze;
    }

    /// <summary>
    /// 指定されたセルの未訪問の隣接セルを取得するメソッド
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="maze"></param>
    /// <returns></returns>
    List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell, int[,] maze)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        foreach (var dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int neighbor = cell + dir * 2;
            if (neighbor.x >= 0 && neighbor.x < Width && neighbor.y >= 0 && neighbor.y < Height && maze[neighbor.x, neighbor.y] == 1)
            {
                // 未訪問のセルをリストに追加
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// 二つのセルの間の壁を取り除くメソッド
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="maze"></param>
    void RemoveWall(Vector2Int a, Vector2Int b, int[,] maze)
    {
        // a と b の 間のセルの位置を計算
        Vector2Int wall = a + (b - a) / 2;
        // 壁を取り除き、通路に設定
        maze[wall.x, wall.y] = 0;
    }
}
