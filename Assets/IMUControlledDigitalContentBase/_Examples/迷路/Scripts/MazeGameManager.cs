using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace IMUControllerDigitalAContentBase.Example.Maze
{
    /// <summary>
    /// 迷路ゲームの進行を管理する
    ///   ・M5Stack の傾き（Pitch, Roll）で、迷路の盤を傾ける
    ///   ・ボールがゴールに着いたら GOAL を表示して、新しい迷路でやり直す
    ///   ・ボールが落ちたら、スタートに戻す
    /// </summary>
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

        [Header("パラメータ")]
        /// <summary>
        /// ボールがこの高さより下に落ちたら、ゲームオーバー
        /// </summary>
        public float FallToDeathPositionY = -5.0f;

        /// <summary>
        /// 迷路の盤を傾けられる、最大の角度
        /// </summary>
        [FormerlySerializedAs("BoadRotateAngleLimit")]
        public float BoardRotateAngleLimit = 30.0f;

        /// <summary>
        /// 盤の傾きのなめらかさ（大きいほど、M5Stack の傾きに素早くついてくる）
        /// </summary>
        public float BoardRotateSmoothness = 6.0f;

        /// <summary>
        /// 盤が傾く速さの上限（度/秒）
        /// 盤を急に傾けると、床がボールを勢いよく押し上げて、ボールが飛んでしまうので制限する
        /// </summary>
        public float BoardMaxRotateSpeed = 30.0f;

        /// <summary>
        /// ゴールしてから、次の迷路が始まるまでの時間（秒）
        /// </summary>
        public float GoalWaitSeconds = 5.0f;

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
        /// 迷路の盤（傾けるオブジェクト）の参照
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
        /// IMU入力管理スクリプトの参照
        /// </summary>
        [SerializeField]
        IMUInputManager _IMUInputManager = null;

        /// <summary>
        /// ゲームの状態
        /// </summary>
        [SerializeField]
        GameState _gameState = GameState.Play;

        /// <summary>
        /// タイム
        /// </summary>
        float _gameTimer = 0.0f;

        /// <summary>
        /// 迷路の盤の傾き（オイラー角）
        /// </summary>
        Vector3 _mazeRootRotate = Vector3.zero;

        /// <summary>
        /// 迷路の盤の Rigidbody（盤を物理的に正しく動かすため）
        /// </summary>
        Rigidbody _mazeRootBody;

        /// <summary>
        /// ボールの Rigidbody
        /// </summary>
        Rigidbody _ballBody;

        /// <summary>
        /// 必要な参照がそろっているかどうか
        /// </summary>
        bool _isReady = false;

        void Start()
        {
            // --- 参照がセットされているか確認する ---
            _isReady = true;
            if (_startObjectRef == null) { Debug.LogError("スタートオブジェクトがセットされていません."); _isReady = false; }
            if (_goalObjectRef == null)  { Debug.LogError("ゴールオブジェクトがセットされていません.");   _isReady = false; }
            if (_ballObjectRef == null)  { Debug.LogError("ボールオブジェクトがセットされていません.");   _isReady = false; }
            if (_mazeRootRef == null)    { Debug.LogError("迷路の盤がセットされていません.");           _isReady = false; }
            if (_IMUInputManager == null) Debug.LogWarning("IMUInputManager がセットされていません。盤は傾きません.");

            if (!_isReady)
            {
                return;
            }

            // --- 迷路の盤は、Rigidbody（Is Kinematic）を通して動かす ---
            // transform を直接回すと、物理エンジンから見ると、壁や床が「瞬間移動」したことになり、
            // ボールが壁や床にめり込んだり、ガタついたりする。
            // Is Kinematic の Rigidbody を付けて MoveRotation で回すと、
            // 「壁や床が動いている」ことが物理エンジンに伝わり、ボールが正しく押される。
            _mazeRootBody = _mazeRootRef.GetComponent<Rigidbody>();
            if (_mazeRootBody == null)
            {
                _mazeRootBody = _mazeRootRef.gameObject.AddComponent<Rigidbody>();
            }
            _mazeRootBody.isKinematic = true;
            _mazeRootBody.useGravity = false;
            _mazeRootBody.interpolation = RigidbodyInterpolation.Interpolate;

            _ballBody = _ballObjectRef.GetComponent<Rigidbody>();

            // ボールをスタート位置に置く
            ResetBall();
        }

        void Update()
        {
            if (!_isReady)
            {
                return;
            }

            // --- センサーの傾き（Ahrs）から、盤の傾きを決める ---
            if (_IMUInputManager != null)
            {
                // M5Stack の Roll → 盤の X軸まわりの回転、Pitch → 盤の Z軸まわりの回転
                Vector3 target = new Vector3(
                    _IMUInputManager.Ahrs.y * -1.0f,
                    0.0f,
                    _IMUInputManager.Ahrs.x * -1.0f
                );
                // 傾けすぎないように制限する
                target.x = Mathf.Clamp(target.x, -BoardRotateAngleLimit, BoardRotateAngleLimit);
                target.z = Mathf.Clamp(target.z, -BoardRotateAngleLimit, BoardRotateAngleLimit);

                // 少しずつ近づける（ローパスフィルター。センサーの細かい揺れをならす）
                Vector3 smoothed = Vector3.Lerp(_mazeRootRotate, target, Time.deltaTime * BoardRotateSmoothness);

                // 傾く速さを制限する（急に傾けたとき、床に押し上げられてボールが飛んでしまうのを防ぐ）
                _mazeRootRotate = Vector3.MoveTowards(_mazeRootRotate, smoothed, BoardMaxRotateSpeed * Time.deltaTime);
            }

            // ある一定の高さよりも下にボールがあれば、下に落ちたものとして、ゲームオーバーとする
            if (_ballObjectRef.transform.position.y < FallToDeathPositionY)
            {
                GameOver();
            }

            // プレイ中だったら、タイマーを進める
            if (_gameState == GameState.Play)
            {
                _gameTimer += Time.deltaTime;
            }

            // タイム文字をセット
            if (_text_time != null)
            {
                _text_time.text = "time : " + _gameTimer.ToString("000.00");
            }
        }

        void FixedUpdate()
        {
            if (!_isReady)
            {
                return;
            }

            // 盤を回す（物理の計算に合わせて、Rigidbody を通して回す）
            _mazeRootBody.MoveRotation(Quaternion.Euler(_mazeRootRotate));
        }

        /// <summary>
        /// ボールをスタート位置に戻して、勢いを 0 にする
        /// </summary>
        void ResetBall()
        {
            Vector3 startPosition = _startObjectRef.transform.position;

            if (_ballBody != null)
            {
                // 速さと回転を 0 にする（しないと、前の勢いのまま転がり出してしまう）
                _ballBody.linearVelocity = Vector3.zero;
                _ballBody.angularVelocity = Vector3.zero;
                _ballBody.position = startPosition;
            }
            _ballObjectRef.transform.position = startPosition;
        }

        /// <summary>
        /// リセット（新しい迷路で、最初から）
        /// </summary>
        public void ResetGame()
        {
            // 迷路を作り直す
            if (_mazeGenerator != null)
            {
                _mazeGenerator.GenerateMazeBlock();
            }

            // ボールをスタート位置に戻す
            ResetBall();

            // ステートをセット
            _gameState = GameState.Play;

            // GOAL文字を非表示
            if (_text_goal != null)
            {
                _text_goal.enabled = false;
            }

            // タイマーをリセット
            _gameTimer = 0.0f;
        }

        /// <summary>
        /// ゴールした（Ball の GoalFunc から呼ばれる）
        /// </summary>
        public void Goal()
        {
            // すでにゴールしているときは、何もしない
            // （ゴールを何度も出入りしても、1回だけ反応するようにする）
            if (_gameState != GameState.Play)
            {
                return;
            }

            Debug.Log("Goal");
            StartCoroutine(GoalCoroutine());
        }

        /// <summary>
        /// ゴールした時の処理
        /// </summary>
        IEnumerator GoalCoroutine()
        {
            // ステートをセット（ここでプレイ中ではなくなるので、タイマーが止まる）
            _gameState = GameState.Goal;

            // GOALという文字を表示
            if (_text_goal != null)
            {
                _text_goal.enabled = true;
            }

            // しばらく待つ
            yield return new WaitForSeconds(GoalWaitSeconds);

            // ゲームをリセット（GOAL文字を消して、プレイ中に戻る）
            ResetGame();
        }

        /// <summary>
        /// ゲームオーバー（ボールが落ちた）
        /// </summary>
        public void GameOver()
        {
            // ボールをスタート位置に戻す
            ResetBall();

            // タイマーリセット
            _gameTimer = 0.0f;
        }
    }
}
