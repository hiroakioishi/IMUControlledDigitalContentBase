using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;

namespace IMUControllerDigitalAContentBase.Example.Maze
{
    /// <summary>
    /// 迷路生成プログラム
    /// http://www5d.biglobe.ne.jp/~stssk/maze/make.html
    /// </summary>
    [ExecuteAlways]
    public class MazeGenerator : MonoBehaviour
    {
        /// <summary>
        /// 迷路の幅
        /// </summary>
        public int Width = 12;
        /// <summary>
        /// 迷路の高さ
        /// </summary>
        public int Height = 12;

        /// <summary>
        /// 迷路の壁のブロックのサイズ（幅、高さ、奥行き）
        /// </summary>
        public Vector3 WallBlockSize = new Vector3(1, 1, 1);
        
        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        GameObject _mazeFloorRef = null;

        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        Transform _mazeWallGroupRef = null;

        /// <summary>
        /// 迷路の壁のオブジェクトのPrefabの参照
        /// </summary>
        [SerializeField]
        GameObject _mazeWallObjectPrefab = null;

        /// <summary>
        /// 作成された迷路のブロックのリスト
        /// </summary>
        // List<GameObject> _generatedMazeWallObjectList = new List<GameObject>();

        /// <summary>
        /// 迷路を表わす2次元配列
        /// </summary>
        int[,] _maze;

        /// <summary>
        /// 迷路のブロックを作成する
        /// </summary>
        [ContextMenu("CreateMazeBlock")]
        public void GenerateMazeBlock()
        {
            // 足場のCubeの位置やサイズを変更
            if (_mazeFloorRef != null)
            {
                // 位置を変更
                _mazeFloorRef.transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
                // サイズを変更
                _mazeFloorRef.transform.localScale = new Vector3(Width, 1.0f, Height);
            }

            // 迷路のデータを作成
            _maze = GenerateMaze(Width, Height);

            // --- 既に存在する迷路の壁のオブジェクトを削除する --- 
            for (int i = _mazeWallGroupRef.transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isEditor)
                    GameObject.DestroyImmediate(_mazeWallGroupRef.transform.GetChild(i).gameObject);
                else
                    GameObject.Destroy(_mazeWallGroupRef.transform.GetChild(i).gameObject);
            }
     

            // --- 迷路の壁のCubeを作成 ---
            if (_maze != null)
            {
                // 生成したCubeの個数
                int generatedCubeCount = 0;
                // 迷路データをイテレーション
                for (int j = 0; j < _maze.GetLength(1); j++)
                {
                    for (int i = 0; i < _maze.GetLength(0); i++)
                    {
                        if (_maze[i, j] > 0.5f)
                        {
                            GameObject go = (GameObject)Instantiate(_mazeWallObjectPrefab, _mazeWallGroupRef);
                            go.name = "MazeWallObject_" + (generatedCubeCount++).ToString("000");
                            // i, jによる位置をセット
                            var pos = new Vector3(i, 0.0f, j);
                            // フィールドの幅の半分ずらして、生成されたブロック群が中央に位置するように調整
                            pos += new Vector3(-0.5f * Width, 0.0f, -0.5f * Height);
                            // ブロック1つの半分ずらす
                            pos += WallBlockSize * 0.5f;
                            // 位置をセット
                            go.transform.localPosition = pos;
                            // スケールをセット
                            go.transform.localScale = WallBlockSize;
                            // 子階層に追加
                            // go.transform.parent = _mazeWallGroupRef;
                            // Listに追加
                            // _generatedMazeWallObjectList.Add(go);
                        }
                    }
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
}