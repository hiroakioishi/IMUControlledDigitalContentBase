using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

/// <summary>
/// M5Stack から USB ケーブル（シリアル通信）で送られてくる IMU（動きセンサー）の値を受け取るスクリプト
///
/// M5Stack からは、次のような1行の文字列が何度も送られてくる想定です。
///   accX,accY,accZ,gyroX,gyroY,gyroZ,pitch,roll,yaw,buttonA,buttonB,buttonC
///
/// 【しくみ】
///   ・受信係（別スレッド）  … M5Stack から1行届くのを待ち、届いたら「箱」に入れる
///   ・Update（Unity）      … 毎フレーム「箱」を見て、入っている行を取り出して値に変換する
///   受信を待っている間に Unity 全体が止まってしまわないよう、受信係は Unity とは別に動かしています。
/// </summary>
[RequireComponent(typeof(IMUKeyboardEmulator))]
public class IMUInputManager : MonoBehaviour
{
    /// <summary>
    /// 入力モード
    /// </summary>
    public enum InputMode
    {
        /// <summary>
        /// シリアル通信（M5Stack）から入力
        /// </summary>
        Serial,
        /// <summary>
        /// キーボードで入力（M5Stack が無くても動作確認できる）
        /// </summary>
        Keyboard,
    }

    // ==================================================================
    //  インスペクタで設定する項目
    // ==================================================================

    [Header("入力モード(シリアル通信 or キーボード入力)")]
    public InputMode mode = InputMode.Serial;

    /// <summary>
    /// キーボード入力で IMU の値の代わりを作るスクリプト
    /// </summary>
    [SerializeField]
    IMUKeyboardEmulator _keyboardEmulator = null;

    [Header("使用できるシリアルポート名のリスト")]
    [SerializeField]
    string[] _serialPortNames = new string[0];

    [Header("受信した値（加速度, 角速度, 姿勢(Pitch, Roll, Yaw)）")]
    /// <summary>
    /// 加速度（どの向きにどれだけ力がかかっているか）
    /// </summary>
    public Vector3 Acceleration;

    /// <summary>
    /// 角速度（どの向きにどれだけの速さで回っているか）
    /// </summary>
    public Vector3 Gyro;

    /// <summary>
    /// 姿勢（どれだけ傾いているか）
    /// x: Pitch（前後の傾き）, y: Roll（左右の傾き）, z: Yaw（水平方向の向き）
    /// </summary>
    public Vector3 Ahrs;

    /// <summary>
    /// ボタンA が押されているかどうか
    /// </summary>
    public bool ButtonA;

    /// <summary>
    /// ボタンB が押されているかどうか
    /// </summary>
    public bool ButtonB;

    /// <summary>
    /// ボタンC が押されているかどうか
    /// </summary>
    public bool ButtonC;

    [Header("アプリ実行時に、自動的にシリアル通信を開始するかどうか")]
    public bool AutoOpenSerial = false;

    [Header("接続に使用するシリアルポート名（例: COM3, /dev/cu.usbserial-xxxx）")]
    public string SerialPortName = string.Empty;

    [Header("通信速度（M5Stack側の Serial.begin() の数字と同じにする）")]
    /// <summary>
    /// 通信速度（ボーレート）
    /// 9600 だと1秒に10回くらいしか値が届かないので、115200 がおすすめ（M5Stack側も合わせること）
    /// </summary>
    public int BaudRate = 115200;

    [Header("キーボード入力モードのとき、ボタンA/B/Cの代わりに使うキー")]
    public KeyCode ButtonAKey = KeyCode.Z;
    public KeyCode ButtonBKey = KeyCode.X;
    public KeyCode ButtonCKey = KeyCode.C;

    [Header("GUIの設定")]
    /// <summary>
    /// シリアル通信の設定ウィンドウを表示するかどうか
    /// </summary>
    public bool ShowSettingWindow = true;

    /// <summary>
    /// 設定ウィンドウの表示/非表示を切り替えるキー
    /// </summary>
    public KeyCode ShowSettingWindowKey = KeyCode.Alpha0;

    /// <summary>
    /// シリアルポートが開いているかどうか（他のスクリプトから確認する用）
    /// </summary>
    public bool IsOpen => _serialPort != null && _serialPort.IsOpen;

    // ==================================================================
    //  スクリプトの中だけで使う変数
    // ==================================================================

    /// <summary>
    /// シリアル通信の窓口
    /// </summary>
    SerialPort _serialPort;

    /// <summary>
    /// 受信係（別スレッド）
    /// </summary>
    Thread _receiveThread;

    /// <summary>
    /// 受信係が動いてよいかどうか
    /// false にすると、受信係は仕事をやめて終了する
    /// ※ volatile は「別のスレッドにも、値の変更が確実に伝わるようにする」ためのおまじない
    /// </summary>
    volatile bool _isReceiving = false;

    /// <summary>
    /// 受信した行をためておく「箱」（キュー）
    /// 受信係が Enqueue で入れて、Update が TryDequeue で古い順に取り出す。
    /// ConcurrentQueue は、2つのスレッドから同時に出し入れしても壊れないように作られた箱。
    /// </summary>
    readonly ConcurrentQueue<string> _receivedLines = new ConcurrentQueue<string>();

    /// <summary>
    /// 最後に受信した行（設定ウィンドウに表示する用）
    /// </summary>
    string _messageText = string.Empty;

    /// <summary>
    /// 形がおかしくて使えなかった行の数（設定ウィンドウに表示する用）
    /// </summary>
    int _invalidLineCount = 0;

    /// <summary>
    /// ポート選択リストのスクロール位置
    /// </summary>
    Vector2 _scroll;

    /// <summary>
    /// ポート選択リストで選ばれている番号
    /// </summary>
    int _selected;

    /// <summary>
    /// 設定ウィンドウの位置と大きさ
    /// </summary>
    Rect _windowRect = new Rect(16, 16, 512, 512);

    // ==================================================================
    //  Unity から呼ばれる関数
    // ==================================================================

    void Awake()
    {
        if (_keyboardEmulator == null)
        {
            _keyboardEmulator = GetComponent<IMUKeyboardEmulator>();
        }
    }

    void Start()
    {
        // 使えるシリアルポートの一覧を取得
        RefreshPortNames();

        // 自動接続する設定なら、ポートを開く
        if (AutoOpenSerial)
        {
            Open();
        }
    }

    void Update()
    {
        // 設定ウィンドウの表示/非表示を切り替える
        if (Input.GetKeyUp(ShowSettingWindowKey))
        {
            ShowSettingWindow = !ShowSettingWindow;
        }

        // --- 箱に入っている行を、古い順に全部取り出して使う ---
        while (_receivedLines.TryDequeue(out string line))
        {
            _messageText = line;

            // シリアル入力モードのときだけ、値に反映する
            if (mode == InputMode.Serial)
            {
                ApplyReceivedLine(line);
            }
        }

        // 受信係がエラー（ケーブルが抜けた等）で止まっていたら、ポートを閉じる
        if (_serialPort != null && !_isReceiving)
        {
            Close();
        }

        // --- キーボード入力モードのとき ---
        if (mode == InputMode.Keyboard && _keyboardEmulator != null)
        {
            Acceleration = _keyboardEmulator.Acceleration;
            Gyro         = _keyboardEmulator.Gyro;
            Ahrs         = _keyboardEmulator.Ahrs;
            ButtonA      = Input.GetKey(ButtonAKey);
            ButtonB      = Input.GetKey(ButtonBKey);
            ButtonC      = Input.GetKey(ButtonCKey);
        }
    }

    // 再生を止めたとき・オブジェクトが消えたとき・アプリを終了したときに、
    // ポートを閉じ忘れないようにする（開いたままだと、次に接続できなくなることがある）
    void OnDisable()         { Close(); }
    void OnDestroy()         { Close(); }
    void OnApplicationQuit() { Close(); }

    // ==================================================================
    //  シリアルポートを開く・閉じる
    // ==================================================================

    /// <summary>
    /// 使えるシリアルポートの一覧を取得し直す
    /// </summary>
    public void RefreshPortNames()
    {
        try
        {
            _serialPortNames = SerialPort.GetPortNames();
        }
        catch (Exception e)
        {
            Debug.LogWarning("シリアルポートの一覧を取得できませんでした: " + e.Message);
            _serialPortNames = new string[0];
        }
        Array.Sort(_serialPortNames);

        if (_selected >= _serialPortNames.Length)
        {
            _selected = 0;
        }
    }

    /// <summary>
    /// シリアルポートを開いて、受信を始める
    /// </summary>
    /// <returns>開けたら true</returns>
    public bool Open()
    {
        // もう開いていたら、何もしない
        if (IsOpen)
        {
            return true;
        }

        // 前回の残りがあれば片付けておく
        Close();

        // --- どのポートに接続するか決める ---
        string portName = null;
        if (AutoOpenSerial)
        {
            portName = SerialPortName;
        }
        else if (_selected < _serialPortNames.Length)
        {
            portName = _serialPortNames[_selected];
        }

        if (string.IsNullOrWhiteSpace(portName))
        {
            Debug.LogWarning("シリアルポート名が指定されていません");
            return false;
        }

        // --- ポートを開く（失敗することもあるので try で囲む） ---
        try
        {
            _serialPort = new SerialPort(FixPortNameForWindows(portName), BaudRate, Parity.None, 8, StopBits.One);
            _serialPort.ReadTimeout = 500;   // 0.5秒待っても何も来なければ、いったん待つのをやめる
            _serialPort.NewLine = "\n";     // 1行の終わりを表す文字
            _serialPort.Open();
            _serialPort.DiscardInBuffer();   // 開く前に溜まっていた古いデータは捨てる
        }
        catch (Exception e)
        {
            // よくある原因: ポート名が違う / 他のアプリ（Arduino IDE のシリアルモニタ等）が使っている
            Debug.LogError($"シリアルポート {portName} を開けませんでした: {e.Message}");
            _serialPort?.Dispose();
            _serialPort = null;
            return false;
        }

        // 箱の中に前回の残りがあれば捨てる
        while (_receivedLines.TryDequeue(out _)) { }
        _invalidLineCount = 0;

        // --- 受信係を動かし始める ---
        _isReceiving = true;
        _receiveThread = new Thread(ReceiveLoop);
        _receiveThread.IsBackground = true;   // アプリ終了時に、受信係も一緒に終わるようにする
        _receiveThread.Start();

        Debug.Log($"シリアルポート {portName} を開きました（通信速度 {BaudRate}）");
        return true;
    }

    /// <summary>
    /// 受信をやめて、シリアルポートを閉じる
    /// </summary>
    public void Close()
    {
        // 1. まず受信係に「やめて」と伝える
        _isReceiving = false;

        // 2. 受信係が仕事を終えるのを待つ（長くても 0.5 秒ほどで終わる）
        if (_receiveThread != null)
        {
            if (_receiveThread.IsAlive && Thread.CurrentThread != _receiveThread)
            {
                _receiveThread.Join(1000);
            }
            _receiveThread = null;
        }

        // 3. ポートを閉じる
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning("シリアルポートを閉じるときにエラーが起きました: " + e.Message);
            }
            _serialPort = null;

            // ボタンが押されたままにならないようにする
            ButtonA = false;
            ButtonB = false;
            ButtonC = false;

            Debug.Log("シリアルポートを閉じました");
        }
    }

    // ==================================================================
    //  受信係（別スレッドで動く）
    // ==================================================================

    /// <summary>
    /// 受信係の仕事: M5Stack から1行届くのを待ち、届いたら箱に入れる。これをくり返す。
    /// ※ この関数は Unity とは別のスレッドで動くので、ここでは Unity のオブジェクト
    ///   （transform など）を触らないこと。値の変換や反映は Update で行う。
    /// </summary>
    void ReceiveLoop()
    {
        SerialPort port = _serialPort;

        while (_isReceiving)
        {
            try
            {
                // 1行届くまで待つ（0.5秒待っても来なければ TimeoutException になる）
                string line = port.ReadLine();

                // 届いた行を箱に入れる
                _receivedLines.Enqueue(line);
            }
            catch (TimeoutException)
            {
                // まだデータが来ていないだけなので、気にせずもう一度待つ
            }
            catch (Exception e)
            {
                // ケーブルが抜けた等で受信できなくなった
                // （Close() でポートを閉じたときにもここに来るので、そのときはログを出さない）
                if (_isReceiving)
                {
                    Debug.LogWarning("受信できなくなったため、受信を止めます: " + e.Message);
                }
                _isReceiving = false;
            }
        }
    }

    // ==================================================================
    //  受信した文字列を値に変換する
    // ==================================================================

    /// <summary>
    /// 受信した1行を数値に変換して、Acceleration などに入れる
    /// </summary>
    void ApplyReceivedLine(string line)
    {
        if (TryParseIMU(line, out Vector3 acc, out Vector3 gyro, out Vector3 ahrs,
                              out bool buttonA, out bool buttonB, out bool buttonC))
        {
            Acceleration = acc;
            Gyro         = gyro;
            Ahrs         = ahrs;
            ButtonA      = buttonA;
            ButtonB      = buttonB;
            ButtonC      = buttonC;
        }
        else
        {
            // 接続した直後は、行の途中から受信してしまうことがあるので、
            // 形がおかしい行はエラーにせず、使わずに捨てる
            _invalidLineCount++;
        }
    }

    /// <summary>
    /// "1.0,2.0,3.0,..." のような文字列を、数値に変換する
    /// </summary>
    /// <returns>うまく変換できたら true、形がおかしければ false</returns>
    static bool TryParseIMU(string text,
                            out Vector3 acc, out Vector3 gyro, out Vector3 ahrs,
                            out bool buttonA, out bool buttonB, out bool buttonC)
    {
        acc  = Vector3.zero;
        gyro = Vector3.zero;
        ahrs = Vector3.zero;
        buttonA = false;
        buttonB = false;
        buttonC = false;

        // カンマ(,)で区切って、12個に分ける
        string[] parts = text.Split(',');
        if (parts.Length != 12)
        {
            return false;
        }

        // 12個それぞれを数値に変換する
        // （InvariantCulture は、PCの言語設定に関係なく「.」を小数点として読むための指定）
        float[] v = new float[12];
        for (int i = 0; i < 12; i++)
        {
            if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]))
            {
                return false;
            }
        }

        acc  = new Vector3(v[0], v[1], v[2]);   // 加速度 x, y, z
        gyro = new Vector3(v[3], v[4], v[5]);   // 角速度 x, y, z
        ahrs = new Vector3(v[6], v[7], v[8]);   // 姿勢 Pitch, Roll, Yaw

        buttonA = v[9]  > 0.5f;                 // 1 なら押されている
        buttonB = v[10] > 0.5f;
        buttonC = v[11] > 0.5f;
        return true;
    }

    /// <summary>
    /// Windows で COM10 以上のポートにつなぐときは、名前を \\.\COM10 の形にする必要があるので変換する
    /// </summary>
    static string FixPortNameForWindows(string name)
    {
        name = name.Trim();

        bool isWindows = Application.platform == RuntimePlatform.WindowsEditor
                      || Application.platform == RuntimePlatform.WindowsPlayer;

        if (isWindows && Regex.IsMatch(name, @"^COM\d{2,}$", RegexOptions.IgnoreCase))
        {
            return @"\\.\" + name;
        }
        return name;
    }

    // ==================================================================
    //  設定ウィンドウ（画面左上に表示される）
    // ==================================================================

    void OnGUI()
    {
        if (ShowSettingWindow)
        {
            _windowRect = GUILayout.Window(0, _windowRect, DrawSettingWindow, "シリアルポート設定");
        }
    }

    void DrawSettingWindow(int windowID)
    {
        // --- 自動接続 ---
        GUILayout.Label("<b>アプリ起動時に自動的に接続する</b>");
        AutoOpenSerial = GUILayout.Toggle(AutoOpenSerial, "有効 / 無効 ※無効の場合は、有効なポート名を選択しOpenを押します。");

        if (AutoOpenSerial)
        {
            GUILayout.Label("<b>接続に使用するシリアルポート名</b>");
            SerialPortName = GUILayout.TextField(SerialPortName);
        }
        else
        {
            // --- ポートの一覧 ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>ポート名のリスト : </b>");
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshPortNames();
            }
            GUILayout.EndHorizontal();

            if (_serialPortNames.Length > 0)
            {
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                _selected = GUILayout.SelectionGrid(_selected, _serialPortNames, 1);
                GUILayout.EndScrollView();

                GUILayout.Label("<b>選択されたポート名 : </b>");
                GUILayout.Label(_serialPortNames[_selected]);
            }
            else
            {
                GUILayout.Label("<color=Orange>シリアルポートが見つかりません。M5Stackを接続して Refresh を押してください。</color>");
            }
        }

        // --- 通信速度 ---
        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>通信速度 : </b>", GUILayout.Width(100));
        if (int.TryParse(GUILayout.TextField(BaudRate.ToString()), out int baud) && baud > 0)
        {
            BaudRate = baud;
        }
        GUILayout.EndHorizontal();

        // --- 開く / 閉じる ---
        GUILayout.Label("<b>シリアルポートを開く, または, 閉じる : </b>");
        if (GUILayout.Button("Open"))
        {
            Open();
        }
        if (GUILayout.Button("Close"))
        {
            Close();
        }

        // --- 状態の表示 ---
        GUILayout.Space(24);
        GUILayout.Label("<b>▼シリアル通信ステータス</b>");

        GUILayout.Label("<b>シリアルポートが開いているかどうか: </b>");
        if (_serialPort != null)
        {
            GUILayout.Label(IsOpen ? "<color=Lime>True</color>" : "<color=Red>False</color>");
        }
        else
        {
            GUILayout.Label("---");
        }

        GUILayout.Label("<b>受信メッセージ : </b>");
        if (string.IsNullOrEmpty(_messageText))
        {
            GUILayout.Label("accX, accY, accZ, gyroX, gyroY, gyroZ, pitch, roll, yaw, buttonA, buttonB, buttonC");
        }
        else
        {
            GUILayout.Label(_messageText);
        }

        GUILayout.Label("<b>形がおかしくて使えなかった行の数 : </b>" + _invalidLineCount);

        // ウィンドウをマウスでドラッグして動かせるようにする
        GUI.DragWindow();
    }
}
