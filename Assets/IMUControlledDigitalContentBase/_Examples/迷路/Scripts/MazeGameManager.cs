using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGameManager : MonoBehaviour
{
    [SerializeField]
    GameObject _startObjectRef = null;

    [SerializeField]
    GameObject _goalObjectRef = null;

    [SerializeField]
    GameObject _ballObjectRef = null;

    /// <summary>
    /// 
    /// </summary>
    [SerializeField]
    MazeGenerator _mazeGenerator = null;

    [SerializeField]
    IMUInputManager _IMUInputManager = null;

    [SerializeField]
    Transform _mazeRootRef = null;

    float _gameTimer = 0.0f;

    Vector3 _mazeRootRotate = Vector3.zero;

    void Start()
    {
        if (_startObjectRef == null)
            Debug.LogError("�X�^�[�g�I�u�W�F�N�g���Z�b�g����Ă��܂���.");
        if (_goalObjectRef == null)
            Debug.LogError("�S�[���I�u�W�F�N�g���Z�b�g����Ă��܂���.");
        if (_ballObjectRef == null)
            Debug.LogError("�{�[���I�u�W�F�N�g���Z�b�g����Ă��܂���.");

    }

    void Update()
    {

        if (_mazeRootRef != null && _IMUInputManager != null)
        {
            _mazeRootRotate = Vector3.Lerp(_mazeRootRotate,
                new Vector3(
                    _IMUInputManager.Ahrs.y * -1.0f,
                    0.0f,
                    _IMUInputManager.Ahrs.x * -1.0f
                    )
                , Time.deltaTime * 6.0f
            );
            _mazeRootRef.localEulerAngles = _mazeRootRotate;
        }
        
        // ������̍����������Ƀ{�[��������΁A���ɗ��������̂Ƃ��āA�Q�[���I�[�o�[�Ƃ���

        // �S�[���I�u�W�F�N�g�Ƀ{�[�����ڐG������S�[���Ƃ���
        if (_goalObjectRef != null)
        {
            if (_ballObjectRef != null)
            {

            }    
        }

    }

    void ResetGame()
    {
        // ���H���Đ�������
        if (_mazeGenerator != null)
        {
            _mazeGenerator.GenerateMazeBlock();
        }

    }

    void Goal()
    {

    }

    void GameOver()
    {

    }

    
}
