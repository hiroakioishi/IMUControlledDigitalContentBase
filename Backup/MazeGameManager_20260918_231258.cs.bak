using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMUControllerDigitalAContentBase.Example.Maze
{
    public class MazeGameManager : MonoBehaviour
    {
        /// <summary>
        /// ゲームの状態
        /// </summary>
        enum GameState
        {
            Play,
            Goal,
            GameOver,
        };

        /// <summary>
        /// 
        /// </summary>
        [Header("パラメータ")]
        public float FallToDeathPositionY = -5.0f;

        /// <summary>
        /// 迷路ボードの回転の制限量
        /// </summary>
        public float BoadRotateAngleLimit = 30.0f;

        /// <summary>
        /// スタートの参照
        /// </summary>
        [Header("ゲーム内オブジェクトの参照")]
        [SerializeField]
        GameObject _startObjectRef = null;

        /// <summary>
        /// ゴールの参照
        /// </summary>
        [SerializeField]
        GameObject _goalObjectRef = null;

        /// <summary>
        /// ボールの参照
        /// </summary>
        [SerializeField]
        GameObject _ballObjectRef = null;

        /// <summary>
        /// 迷路の参照
        /// </summary>
        [SerializeField]
        Transform _mazeRootRef = null;
        
        /// <summary>
        /// ゴールテキスト
        /// </summary>
        [Header("UIの参照")]
        [SerializeField]
        TMPro.TextMeshProUGUI _text_goal = null;

        /// <summary>
        /// タイムテキスト
        /// </summary>
        [SerializeField]
        TMPro.TextMeshProUGUI _text_time = null;

        /// <summary>
        /// 迷路生成器の参照
        /// </summary>
        [Header("スクリプトの参照")]
        [SerializeField]
        MazeGenerator _mazeGenerator = null;

        /// <summary>
        /// IMU入力管理テキストの参照
        /// </summary>
        [SerializeField]
        IMUInputManager _IMUInputManager = null;

        /// <summary>
        /// タイム
        /// </summary>
        float _gameTimer = 0.0f;

        /// <summary>
        /// 迷路の回転
        /// </summary>
        Vector3 _mazeRootRotate = Vector3.zero;

        /// <summary>
        /// ゲームの状態
        /// </summary>
        [SerializeField]
        GameState _gameState = GameState.Play;

        void Start()
        {
            if (_startObjectRef == null)
                Debug.LogError("スタートオブジェクトがセットされていません.");
            if (_goalObjectRef == null)
                Debug.LogError("ゴールオブジェクトがセットされていません.");
            if (_ballObjectRef == null)
                Debug.LogError("ボールオブジェクトがセットされていません.");

        }

        void Update()
        {

            // --- センサーのAhrsの値を、ボードの回転に適用。--- 
            // その際 ローパスフィルターを適用させる。
            _mazeRootRotate = Vector3.Lerp(_mazeRootRotate,
                new Vector3(
                    _IMUInputManager.Ahrs.y * -1.0f,
                    0.0f,
                    _IMUInputManager.Ahrs.x * -1.0f
                    )
                , Time.deltaTime * 6.0f
            );
            // 回転の値を制限
            _mazeRootRotate.x = Mathf.Clamp(_mazeRootRotate.x, -BoadRotateAngleLimit, BoadRotateAngleLimit);
            _mazeRootRotate.z = Mathf.Clamp(_mazeRootRotate.z, -BoadRotateAngleLimit, BoadRotateAngleLimit);
            // ボードに回転の値をセット
            _mazeRootRef.localEulerAngles = _mazeRootRotate;


            // ある一定の高さよりも下にボールがあれば、下に落ちたものとして、ゲームオーバーとする
            if (_ballObjectRef.transform.position.y < FallToDeathPositionY)
            {
                // ゲームおーば
                GameOver();
            }

            // プレイ中だったら
            if (_gameState == GameState.Play)
            {
                // タイマーを加算
                _gameTimer += Time.deltaTime;
            }

            // タイム文字をセット
            _text_time.text = "time : " + _gameTimer.ToString("000.00");
        }

        /// <summary>
        /// リセット
        /// </summary>
        public void ResetGame()
        {
            // 迷路を再生成する
            _mazeGenerator.GenerateMazeBlock();

            // ボールをスタート位置に戻す
            _ballObjectRef.transform.position = _startObjectRef.transform.position;

            // ステートをセット
            _gameState = GameState.Play;

            // GOAL文字非表示
            _text_goal.enabled     = false;
            
            // タイマーをリセット
            _gameTimer = 0.0f;
        }

        /// <summary>
        /// ゴールした
        /// </summary>
        public void Goal()
        {
            Debug.Log("Goal");
            StartCoroutine(GoalCoroutine());
        }

        /// <summary>
        /// ゴールした時の処理
        /// </summary>
        /// <returns></returns>
        IEnumerator GoalCoroutine()
        {
            // GOALという文字を表示
            _text_goal.enabled = true;
            
            // ステートセット
            _gameState = GameState.Goal;

            yield return new WaitForSeconds(5.0f);

            // GOALという文字を非表示に
            _text_goal.enabled = false;

            // ステートセット
            _gameState = GameState.Goal; 

            // ゲームをリセット
            ResetGame();
        }

        /// <summary>
        /// ゲームオーバー
        /// </summary>
        public void GameOver()
        {
            // ボールをスタート位置に戻す
            _ballObjectRef.transform.position = _startObjectRef.transform.position;

            // タイマーリセット
            _gameTimer = 0.0f;
        }
    }
}
