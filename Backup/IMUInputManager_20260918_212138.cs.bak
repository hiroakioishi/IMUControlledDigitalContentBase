using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading.Tasks;

[ExecuteAlways, RequireComponent(typeof(IMUKeyboardEmulator))]
public class IMUInputManager : MonoBehaviour
{
    /// <summary>
    /// 入力モード
    /// </summary>
    public enum InputMode
    {
        /// <summary>
        /// シリアル
        /// </summary>
        Serial,
        /// <summary>
        /// キーボード
        /// </summary>
        Keyboard,        
    }

    [Header("入力モード(シリアル通信 or キーボード入力)")]
    /// <summary>
    /// 入力モード 
    /// </summary>
    public InputMode mode = InputMode.Serial;

    /// <summary>
    /// キーボード入力でIMUの値をエミュレートするスクリプトの参照
    /// </summary>
    [SerializeField]
    IMUKeyboardEmulator _keyboardEmulator = null;

    [Header("使用できるシリアルポート名のリスト")]
    /// <summary>
    /// 使用できるシリアルポート名のリスト
    /// </summary>
    [SerializeField]
    string[] _serialPortNames;

    /// <summary>
    /// 受信したメッセージテキスト
    /// </summary>
    string _messageText = string.Empty;

    [Header("受信した値（加速度, 角速度, 姿勢(Pitch, Roll, Yaw)）")]
    /// <summary>
    /// 加速度
    /// </summary>
    public Vector3 Acceleration;

    /// <summary>
    /// ジャイロ
    /// </summary>
    public Vector3 Gyro;

    /// <summary>
    /// 姿勢方位基準システム（Attitude and Heading Reference System）
    /// Pitch(), Roll(), Yaw()
    /// </summary>
    public Vector3 Ahrs;

    /// <summary>
    /// ボタンAが押されているかどうか
    /// </summary>
    public bool ButtonA;

    /// <summary>
    /// ボタンBが押されているかどうか
    /// </summary>
    public bool ButtonB;
    
    /// <summary>
    /// ボタンCが押されているかどうか
    /// </summary>
    public bool ButtonC;

    [Header("アプリ実行時に、自動的にシリアル通信を開始するかどうか")]
    /// <summary>
    /// アプリ実行時に、自動的にシリアル通信を開始するかどうか
    /// </summary>
    public bool   AutoOpenSerial = false;

    [Header("接続に使用するシリアルポート名")]
    /// <summary>
    /// 接続に使用するシリアルポート名
    /// Windowsでシリアル通信を行う場合、COM10以上は接続できません
    /// </summary>
    public string SerialPortName = string.Empty;

    /// <summary>
    /// シリアル通信を行うためのポート
    /// </summary>
    SerialPort _serialPort;

    /// <summary>
    /// シリアル通信を行っているかどうか
    /// </summary>
    bool _isRunning = false;

    /// <summary>
    /// シリアル通信設定ウィンドウを表示するかどうか
    /// </summary>
    [Header("GUIの設定")]
    public bool ShowSettingWindow = true;

    /// <summary>
    /// 設定ウィンドウの表示/非表示を切り替えるキー
    /// </summary>
    public KeyCode ShowSettingWindowKey = KeyCode.Alpha0;

    /// <summary>
    /// ポート選択リストスクロールの値
    /// </summary>
    Vector2 _scroll;

    /// <summary>
    /// 選択されたポート
    /// </summary>
    int _selected;

    /// <summary>
    /// シリアルポート設定ウィンドウの位置、サイズ
    /// </summary>
    Rect windowRect = new Rect(16, 16, 512, 512);

    void Awake()
    {
        _keyboardEmulator = GetComponent<IMUKeyboardEmulator>();    
    }

    void Start()
    {
        // 使用できるシリアルポート名のリストを取得
        _serialPortNames = SerialPort.GetPortNames();

        // アプリ起動時に、自動でシリアル通信を開始する場合、
        if (AutoOpenSerial)
        {
            // シリアル通信を開始（シリアルポートを開く）
            Open();
        }
    }

    void Update()
    {
        // キーを押して離したら
        if (Input.GetKeyUp(ShowSettingWindowKey))
        {
            // True であれば Falseに、False であれば True に
            ShowSettingWindow ^= true;
        }

        // 受信したメッセージテキストが何かあれば
        if (_messageText != string.Empty)
        {
            // 受信したメッセージテキストをVector3型の加速度, 角速度, 姿勢 の値に変換する
            ConvertStringToIMU(_messageText, out Acceleration, out Gyro, out Ahrs, out ButtonA, out ButtonB, out ButtonC);
        } 

        // キーボード入力モードであれば
        if (mode == InputMode.Keyboard)
        {
            // キーボード入力エミュレータスクリプトの参照があれば
            if (_keyboardEmulator != null)
            {
                // 値を代入
                Acceleration = _keyboardEmulator.Acceleration;
                Gyro         = _keyboardEmulator.Gyro;
                Ahrs         = _keyboardEmulator.Ahrs;
            }
        }
    }

    /// <summary>
    /// シリアルポートを開く
    /// </summary>
    async void Open()
    {
        // すでに、シリアルポートが初期化され、
        // シリアルポートが開いていたら、何もしない
        if (_serialPort != null && _serialPort.IsOpen == true)
            return;

        // 起動時に自動でシリアル通信を開始する
        if (AutoOpenSerial)
        {
            // シリアルポートを初期化する
            // 設定したシリアルポート名をセット
            _serialPort = new SerialPort(SerialPortName, 9600, Parity.None, 8, StopBits.One);
        }
        // 
        else
        {
            // シリアルポートを初期化する
            // 選択された有効なシリアルポート名をセット
            _serialPort = new SerialPort(_serialPortNames[_selected], 9600, Parity.None, 8, StopBits.One);
        }

        // 読み取りのタイムアウト時間を設定する（動作が止まってしまうので）
        _serialPort.ReadTimeout = 500;
        
        // ポートを開く
        _serialPort.Open();

        // シリアル通信が動作していることを示すフラグをTrueにする
        _isRunning = true;

        // デバッグメッセージをコンソールに表示
        Debug.Log("シリアルポートは開かれました");

        // データの読み取りを開始
        await ReadDataAsync();
    }

    /// <summary>
    /// 非同期でデータを読み取る
    /// </summary>
    /// <returns></returns>
    async Task ReadDataAsync()
    {
        // 別スレッドで実行
        await Task.Run(() =>
        {
            while (_isRunning)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    try
                    {
                        // シリアルポートで、受信したメッセージを読み込む
                        _messageText = _serialPort.ReadLine();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning(e.Message);
                    }
                }
            }
        });
    }

    /// <summary>
    /// 通信を閉じる
    /// </summary>
    void Close()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            // シリアルポートを閉じる
            _serialPort.Close();
            _serialPort?.Dispose();
            // シリアル通信を行っているかどうかを示すフラグを False に
            _isRunning = false;            
        }
    }

    /// <summary>
    /// 読み取った文字列を、Vector3型のacc, gyro, ahrl の値に代入する
    /// </summary>
    /// <param name="str"></param>
    /// <param name="acc"></param>
    /// <param name="gyro"></param>
    /// <param name="ahrl"></param>
    /// <exception cref="System.ArgumentException"></exception>
    void ConvertStringToIMU(string str, out Vector3 acc, out Vector3 gyro, out Vector3 ahrl, out bool buttonA, out bool buttonB, out bool buttonC)
    {
        // 文字列を','で分割する
        string[] parts = str.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 分割された文字列の数が12つでない場合はエラーとして処理
        if (parts.Length != 12)
        {
            throw new System.ArgumentException("Invalid string format. Expected format: 'x,y,z,x,y,z,x,y,z,i,i,i'");
        }

        // 各部分文字列を浮動小数点数に変換する
        float accX  = float.Parse(parts[0].Trim());
        float accY  = float.Parse(parts[1].Trim());
        float accZ  = float.Parse(parts[2].Trim());
        float gyroX = float.Parse(parts[3].Trim());
        float gyroY = float.Parse(parts[4].Trim());
        float gyroZ = float.Parse(parts[5].Trim());
        float pitch = float.Parse(parts[6].Trim());
        float roll  = float.Parse(parts[7].Trim());
        float yaw   = float.Parse(parts[8].Trim());
        int btnA = int.Parse(parts[9].Trim());
        int btnB = int.Parse(parts[10].Trim());
        int btnC = int.Parse(parts[11].Trim());

        acc  = new Vector3(accX, accY, accZ);
        gyro = new Vector3(gyroX, gyroY, gyroZ);
        ahrl = new Vector3(pitch, roll, yaw);

        buttonA = btnA > 0 ? true : false;
        buttonB = btnB > 0 ? true : false;
        buttonC = btnC > 0 ? true : false;
    }

    void OnDestroy()
    {
        // シリアルポートを閉じる
        Close();
    }

    void OnGUI()
    {
        // シリアルポート設定ウィンドウを表示するかどうか
        if (ShowSettingWindow)
        {
            // ウィンドウの表示
            windowRect = GUILayout.Window(0, windowRect, DoWindow, "シリアルポート設定");
        }
    }

    /// <summary>
    /// シリアル通信設定ウィンドウの実行
    /// </summary>
    /// <param name="windowID"></param>
    void DoWindow(int windowID)
    {

        GUILayout.Label("<b>アプリ起動時に自動的に接続する</b>");
        AutoOpenSerial = GUILayout.Toggle(AutoOpenSerial, "有効 / 無効 ※無効の場合は、有効なポート名を選択しOpenを押します。");

        if (AutoOpenSerial)
        {
            GUILayout.Label("<b>接続に使用するシリアルポート名</b>");
            SerialPortName = GUILayout.TextField(SerialPortName);
        }
        else
        {
            GUILayout.Label("<b>ポート名のリスト : </b>");
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            _selected = GUILayout.SelectionGrid(_selected, _serialPortNames, 1);
            GUILayout.EndScrollView();
            if (_serialPortNames != null && _serialPortNames.Length > 0)
            {
                GUILayout.Label("<b>選択されたポート名 : </b>");
                GUILayout.Label(_serialPortNames[_selected]);
            }
        }

        GUILayout.Label("<b>シリアルポートを開く, または, 閉じる : </b>");
        if (GUILayout.Button("Open"))
        {
            Open();            
        }
        if (GUILayout.Button("Close"))
        {
            Close();
        }

        GUILayout.Space(24);
        GUILayout.Label("<b>▼シリアル通信ステータス</b>");

        GUILayout.Label("<b>シリアルポートが開いているかどうか: </b>");
        if (_serialPort != null)
        {
            GUILayout.Label(_serialPort.IsOpen ? "<color=Lime>True</color>" : "<color=Red>False</color>");
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
    }
}
