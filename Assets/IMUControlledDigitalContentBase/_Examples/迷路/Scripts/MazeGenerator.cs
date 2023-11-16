using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// ���H�����v���O����
/// http://www5d.biglobe.ne.jp/~stssk/maze/make.html
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    /// <summary>
    /// ���H�̕�
    /// </summary>
    public int Width  = 12;    
    /// <summary>
    /// ���H�̍���
    /// </summary>
    public int Height = 12;
    /// <summary>
    /// ���H��\�킷2�����z��
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
    /// ���H�̃f�[�^�𐶐�
    /// </summary>
    /// <param name="width">��</param>
    /// <param name="height">����</param>
    /// <returns></returns>
    int[,] GenerateMaze(int width, int height)
    {
        // ���H�̃f�[�^���i�[���邽�߂̔z��
        int[,] maze = new int[width, height];
        // �e�Z����ǁi1�j�Ƃ��ď�����
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                maze[x, y] = 1;

        // DFS�A���S���Y�����g�p�������H�̐���
        // �T���p�̃X�^�b�N
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        // �J�n�n�_
        var start = new Vector2Int(1, 1);
        // �J�n�n�_��ʘH(0)�ɐݒ�
        maze[start.x, start.y] = 0;
        // �X�^�b�N�ɊJ�n�n�_��ǉ�
        stack.Push(start);

        while (stack.Count > 0)
        {
            // ���݂̃Z�����X�^�b�N������o��
            Vector2Int current = stack.Pop();
            // ���K��̗אڃZ�����擾
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current, maze);
            
            if (neighbors.Count > 0)
            {
                // ���݂̃Z�����ĂуX�^�b�N�ɒǉ�
                stack.Push(current);
                // �����_���ɗאڃZ�����擾
                Vector2Int chosen = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                // ���݂̃Z���ƑI�����ꂽ�אڃZ���̊Ԃ̕ǂ���菜��
                RemoveWall(current, chosen, maze);
                // �I�����ꂽ�אڃZ����ʘH�ɐݒ肷��
                maze[chosen.x, chosen.y] = 0;
                // �I�����ꂽ�אڃZ�����X�^�b�N�ɒǉ�
                stack.Push(chosen);
            }
        }
        // �������ꂽ���H��Ԃ�
        return maze;
    }

    /// <summary>
    /// �w�肳�ꂽ�Z���̖��K��̗אڃZ�����擾���郁�\�b�h
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
                // ���K��̃Z�������X�g�ɒǉ�
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// ��̃Z���̊Ԃ̕ǂ���菜�����\�b�h
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="maze"></param>
    void RemoveWall(Vector2Int a, Vector2Int b, int[,] maze)
    {
        // a �� b �� �Ԃ̃Z���̈ʒu���v�Z
        Vector2Int wall = a + (b - a) / 2;
        // �ǂ���菜���A�ʘH�ɐݒ�
        maze[wall.x, wall.y] = 0;
    }
}
