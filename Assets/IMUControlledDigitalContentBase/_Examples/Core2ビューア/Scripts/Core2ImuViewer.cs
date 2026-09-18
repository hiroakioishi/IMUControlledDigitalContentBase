using UnityEngine;

/// <summary>
/// M5Stack Core2 の3Dモデルを、IMU の値（Roll / Pitch / Yaw と加速度）とボタンで動かすスクリプト
///
///   ・姿勢（Roll / Pitch / Yaw）… モデルの回転になる
///   ・加速度               … 急な動きの分だけ、モデルの位置が少しずれる
///                            （重力やゆっくりした傾きの分はフィルターで取り除く）
///   ・ボタン A / B / C     … 押している間だけ画面の色が変わり、離すと元に戻る
/// </summary>
public class Core2ImuViewer : MonoBehaviour
{
    public enum Source
    {
        /// <summary>IMUInputManager（M5Stack / キーボード）の値を使う</summary>
        InputManager,
        /// <summary>インスペクタで入力した値を使う</summary>
        Manual,
    }

    [Header("値の入力元")]
    public Source source = Source.InputManager;

    [SerializeField]
    IMUInputManager _inputManager = null;

    [Header("現在の値（Manual のときはここを動かす）")]
    [Range(-180f, 180f)] public float roll;
    [Range(-90f, 90f)]   public float pitch;
    [Range(-180f, 180f)] public float yaw;
    /// <summary>加速度（G）。M5Stack の x, y, z の順</summary>
    public Vector3 acceleration = new Vector3(0f, 0f, 1f);
    public bool buttonA;
    public bool buttonB;
    public bool buttonC;

    [Header("加速度によるずれ")]
    [Tooltip("1G あたりのずれ量（m）")]
    public float accelGain = 0.05f;
    [Tooltip("ずれの上限（m）")]
    public float maxOffset = 0.03f;
    [Tooltip("重力・傾き成分を追いかける速さ。大きいほどすぐ元の位置に戻る")]
    public float gravityFilterSpeed = 1.5f;
    [Tooltip("位置が目標に追いつく速さ")]
    public float followSpeed = 12f;

    [Header("画面")]
    public TextMesh screenText;
    public Renderer screenRenderer;
    public Color defaultScreenColor = new Color32(0x0C, 0x1A, 0x24, 255);
    public Color buttonAColor = new Color32(0x8A, 0x1F, 0x2E, 255);
    public Color buttonBColor = new Color32(0x1F, 0x6E, 0x3C, 255);
    public Color buttonCColor = new Color32(0x22, 0x44, 0x9A, 255);

    Vector3 _basePosition;
    Vector3 _accLowPass;
    Vector3 _offset;
    bool _lowPassReady;
    float _textTimer;

    Color _screenColor;
    MaterialPropertyBlock _mpb;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        _basePosition = transform.localPosition;
        if (_inputManager == null)
        {
            _inputManager = FindAnyObjectByType<IMUInputManager>();
        }
        _mpb = new MaterialPropertyBlock();
        SetScreenColor(defaultScreenColor);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ---- 1. 値を受け取る ----
        if (source == Source.InputManager)
        {
            if (_inputManager == null) return;
            pitch        = _inputManager.Ahrs.x;
            roll         = _inputManager.Ahrs.y;
            yaw          = _inputManager.Ahrs.z;
            acceleration = _inputManager.Acceleration;
            buttonA      = _inputManager.ButtonA;
            buttonB      = _inputManager.ButtonB;
            buttonC      = _inputManager.ButtonC;
        }

        // ---- 2. 姿勢 → 回転 ----
        // M5Stack の AHRS は「Yaw → Pitch → Roll」の順で回した結果を角度で表しています。
        // 3つの回転を同じ順番で掛け合わせないと、傾きによっては向きがおかしくなります
        // （ジンバルロックのように見える原因）。
        //
        // 軸の対応（M5Stack → Unity）:
        //   x（右）        → X   … Roll
        //   y（画面の上）  → Z   … Pitch
        //   z（画面の法線）→ Y   … Yaw
        // 右手系 → 左手系の変換なので、角度の符号は反転させます。
        Quaternion qYaw   = Quaternion.AngleAxis(-yaw,   Vector3.up);
        Quaternion qPitch = Quaternion.AngleAxis(-pitch, Vector3.forward);
        Quaternion qRoll  = Quaternion.AngleAxis(-roll,  Vector3.right);
        transform.localRotation = qYaw * qPitch * qRoll;

        // ---- 3. 加速度 → 位置のずれ ----
        // ゆっくり変わる成分（重力・傾き）を低域フィルターで追いかけ、
        // それとの差（＝急な動きの分）だけを使う
        if (!_lowPassReady) { _accLowPass = acceleration; _lowPassReady = true; }
        _accLowPass = Vector3.Lerp(_accLowPass, acceleration, 1f - Mathf.Exp(-gravityFilterSpeed * dt));
        Vector3 dynamic = acceleration - _accLowPass;

        Vector3 local  = new Vector3(dynamic.x, dynamic.z, dynamic.y);
        Vector3 target = Vector3.ClampMagnitude(transform.localRotation * local * accelGain, maxOffset);
        _offset = Vector3.Lerp(_offset, target, 1f - Mathf.Exp(-followSpeed * dt));
        transform.localPosition = _basePosition + _offset;

        // ---- 4. ボタン → 画面の色 ----
        // 押している間だけその色にする（複数押したときは A → B → C の順で優先）
        Color wanted = defaultScreenColor;
        if      (buttonA) wanted = buttonAColor;
        else if (buttonB) wanted = buttonBColor;
        else if (buttonC) wanted = buttonCColor;
        if (wanted != _screenColor) SetScreenColor(wanted);

        // ---- 5. 画面に値を表示（0.1秒ごと） ----
        _textTimer -= dt;
        if (screenText != null && _textTimer <= 0f)
        {
            _textTimer = 0.1f;
            screenText.text =
                $"IMU\nR {roll,7:F1}\nP {pitch,7:F1}\nY {yaw,7:F1}\n" +
                $"ax {acceleration.x,5:F2}\nay {acceleration.y,5:F2}\naz {acceleration.z,5:F2}";
        }
    }

    void SetScreenColor(Color c)
    {
        _screenColor = c;
        if (screenRenderer == null) return;
        screenRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorId, c);
        screenRenderer.SetPropertyBlock(_mpb);
    }
}
