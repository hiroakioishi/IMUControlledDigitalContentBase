using UnityEngine;

namespace IMUControllerDigitalAContentBase.Example.Sparkler
{
    /// <summary>
    /// 手持ち花火（スパークラー）
    ///
    /// 【しくみ】
    ///   ・姿勢（Pitch / Roll / Yaw）… 花火の向きになる（Core2ビューアと同じ回し方）
    ///   ・加速度 / 角速度         … 振った強さを取り出して、火花の量・勢い・明るさを増やす
    ///                               （ゆっくり変わる成分（重力・傾き）をフィルターで取り除く。シェイクで音と同じ方法）
    ///   ・ボタン A               … 打ち上げ花火を打ち上げる
    ///   ・ボタン B               … 火花の色を変える
    ///   ・ボタン C               … いま向いている方向を「正面」にする（Yaw を使うときだけ）
    ///
    /// 【ParticleSystem の見どころ】
    ///   ・火花（Sparks）は、消えるときに Sub Emitter（SparkBranch）で小さな火花を散らして、
    ///     本物の線香花火やスパークラーのような「枝分かれ」を作っています。
    ///   ・打ち上げ花火（Rocket）は、消えるときに Sub Emitter（Explosion）で大きく開き、
    ///     その火の粉もさらに Sub Emitter（Crackle）でパチパチはじけます。
    ///   ・火花・打ち上げ花火には Trails（光の尾）を付けています。
    ///   ・スクリプトからは emission / main などのモジュールの値を、毎フレーム書きかえています。
    ///
    /// 【キーボードで試すとき】
    ///   矢印キー: 傾ける / スペースキー: 振ったのと同じ / Z: 打ち上げ / X: 色を変える
    /// </summary>
    public class SparklerController : MonoBehaviour
    {
        // ==================================================================
        //  インスペクタで設定する項目
        // ==================================================================

        [Header("スクリプトの参照")]
        [SerializeField]
        IMUInputManager _imuInputManager = null;

        [Header("花火の部品の参照")]
        /// <summary>
        /// 火花が出る先端（この Transform の上方向（緑の矢印）が、花火の棒の向き）
        /// </summary>
        [SerializeField]
        Transform _tip = null;

        /// <summary>
        /// 火花
        /// </summary>
        [SerializeField]
        ParticleSystem _sparks = null;

        /// <summary>
        /// 先端の火の玉
        /// </summary>
        [SerializeField]
        ParticleSystem _glow = null;

        /// <summary>
        /// 打ち上げ花火
        /// </summary>
        [SerializeField]
        ParticleSystem _rocket = null;

        /// <summary>
        /// 先端の明かり（まわりを照らす）
        /// </summary>
        [SerializeField]
        Light _tipLight = null;

        [Header("向き")]
        /// <summary>
        /// 向きのなめらかさ（大きいほど、M5Stack の傾きに素早くついてくる）
        /// </summary>
        public float RotationSmoothness = 12.0f;

        /// <summary>
        /// Yaw（水平方向の向き）を使うかどうか
        /// M5Stack の Yaw は時間とともに少しずつずれていくので、最初は使わない設定にしています
        /// </summary>
        public bool UseYaw = false;

        /// <summary>
        /// 花火の向きがおかしいときに、追加で回す角度（度）
        /// </summary>
        public Vector3 RotationOffset = Vector3.zero;

        [Header("振ったときの反応")]
        /// <summary>
        /// 重力・傾き成分を追いかける速さ
        /// </summary>
        public float GravityFilterSpeed = 3.0f;

        /// <summary>
        /// この強さ（G）で振ると、火花がいちばん強くなる（小さいほど、軽く振っただけで強くなる）
        /// </summary>
        public float ShakeSensitivity = 1.5f;

        /// <summary>
        /// この速さ（度/秒）で回すと、火花がいちばん強くなる
        /// </summary>
        public float GyroSensitivity = 400.0f;

        /// <summary>
        /// 強くなった火花が、元に戻る速さ
        /// </summary>
        public float IntensityDecay = 2.5f;

        /// <summary>
        /// テスト用: このキーを押している間は、振ったのと同じになる
        /// </summary>
        public KeyCode TestShakeKey = KeyCode.Space;

        [Header("火花")]
        /// <summary>
        /// 静かに持っているときの、1秒あたりの火花の数
        /// </summary>
        public float BaseEmissionRate = 250.0f;

        /// <summary>
        /// いちばん強く振ったときの、1秒あたりの火花の数
        /// </summary>
        public float MaxEmissionRate = 1500.0f;

        /// <summary>
        /// 静かに持っているときの、火花の飛ぶ速さ
        /// </summary>
        public float BaseSparkSpeed = 1.2f;

        /// <summary>
        /// いちばん強く振ったときの、火花の飛ぶ速さ
        /// </summary>
        public float MaxSparkSpeed = 3.5f;

        /// <summary>
        /// 火花のゆらぎ（0 = 一定, 1 = 大きくゆらぐ）
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float Flicker = 0.35f;

        [Header("明かり")]
        public float BaseLightIntensity = 1.5f;
        public float MaxLightIntensity = 5.0f;

        [Header("打ち上げ花火（ボタンA）")]
        /// <summary>
        /// 打ち上げる速さ
        /// </summary>
        public float LaunchSpeed = 6.0f;

        /// <summary>
        /// 打ち上げる向きを、どれだけ真上に寄せるか（0 = 花火の棒の向きそのまま, 1 = いつも真上）
        /// 棒を下に向けたときに、地面に打ち込まないようにしたいときは、少し大きくする
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float UpwardBias = 0.0f;

        /// <summary>
        /// 一度打ち上げてから、次に打ち上げられるまでの時間（秒）
        /// </summary>
        public float LaunchCooldown = 0.3f;

        [Header("色（ボタンBで順番に切り替わる）")]
        public Color[] Colors = new Color[]
        {
            new Color(1.00f, 0.72f, 0.30f),   // 金色
            new Color(1.00f, 0.45f, 0.70f),   // 桜色
            new Color(0.45f, 1.00f, 0.40f),   // 緑
            new Color(0.40f, 0.70f, 1.00f),   // 青
            new Color(0.75f, 0.45f, 1.00f),   // 紫
        };

        [Header("確認用：いまの火花の強さ（0 ～ 1）")]
        [Range(0.0f, 1.0f)]
        public float CurrentIntensity = 0.0f;

        // ==================================================================
        //  スクリプトの中だけで使う変数
        // ==================================================================

        Quaternion _rotation = Quaternion.identity;
        Vector3 _accLowPass;
        bool _lowPassReady = false;
        float _yawOffset = 0.0f;

        bool _prevButtonA = false;
        bool _prevButtonB = false;
        bool _prevButtonC = false;

        int _colorIndex = 0;
        float _launchTimer = 0.0f;

        // ==================================================================
        //  Unity から呼ばれる関数
        // ==================================================================

        void Start()
        {
            if (_imuInputManager == null)
            {
                _imuInputManager = FindAnyObjectByType<IMUInputManager>();
            }
            _rotation = transform.localRotation;
            ApplyColor();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _launchTimer -= dt;

            // --- 1. 振った強さ（0 ～ 1）を求める ---
            float target = 0.0f;
            if (_imuInputManager != null)
            {
                UpdateRotation(dt);
                target = GetShakeStrength(dt);
                HandleButtons();
            }
            if (Input.GetKey(TestShakeKey))
            {
                target = 1.0f;
            }

            // 強くなるときはすぐ、弱くなるときはゆっくり（余韻が残る）
            CurrentIntensity = Mathf.Max(CurrentIntensity, target);
            CurrentIntensity = Mathf.Lerp(CurrentIntensity, 0.0f, 1.0f - Mathf.Exp(-IntensityDecay * dt));

            // --- 2. 強さを ParticleSystem と明かりに反映する ---
            ApplySparks();
        }

        // ==================================================================
        //  M5Stack の値を読む
        // ==================================================================

        /// <summary>
        /// 姿勢（Pitch / Roll / Yaw）から、花火の向きを決める
        /// </summary>
        void UpdateRotation(float dt)
        {
            Vector3 ahrs = _imuInputManager.Ahrs;
            float pitch = ahrs.x;
            float roll  = ahrs.y;
            float yaw   = UseYaw ? Mathf.DeltaAngle(_yawOffset, ahrs.z) : 0.0f;

            // M5Stack の AHRS は「Yaw → Pitch → Roll」の順で回した結果なので、同じ順で掛け合わせる
            // （軸の対応や符号の反転は、Core2ビューアと同じ）
            Quaternion qYaw   = Quaternion.AngleAxis(-yaw,   Vector3.up);
            Quaternion qPitch = Quaternion.AngleAxis(-pitch, Vector3.forward);
            Quaternion qRoll  = Quaternion.AngleAxis(-roll,  Vector3.right);
            Quaternion target = qYaw * qPitch * qRoll * Quaternion.Euler(RotationOffset);

            // 少しずつ近づける（センサーの細かい揺れをならす）
            _rotation = Quaternion.Slerp(_rotation, target, 1.0f - Mathf.Exp(-RotationSmoothness * dt));
            transform.localRotation = _rotation;
        }

        /// <summary>
        /// 振った強さ（0 ～ 1）を求める
        /// </summary>
        float GetShakeStrength(float dt)
        {
            // 加速度: ゆっくり変わる成分（重力・傾き）を追いかけ、その差（＝急な動き）だけを使う
            Vector3 acc = _imuInputManager.Acceleration;
            if (!_lowPassReady)
            {
                _accLowPass = acc;
                _lowPassReady = true;
            }
            _accLowPass = Vector3.Lerp(_accLowPass, acc, 1.0f - Mathf.Exp(-GravityFilterSpeed * dt));
            float shake = (acc - _accLowPass).magnitude / Mathf.Max(ShakeSensitivity, 0.01f);

            // 角速度: くるっと回したときも、火花を強くする
            float spin = _imuInputManager.Gyro.magnitude / Mathf.Max(GyroSensitivity, 1.0f);

            return Mathf.Clamp01(Mathf.Max(shake, spin));
        }

        /// <summary>
        /// ボタン（押した瞬間だけ反応させる）
        /// </summary>
        void HandleButtons()
        {
            bool a = _imuInputManager.ButtonA;
            bool b = _imuInputManager.ButtonB;
            bool c = _imuInputManager.ButtonC;

            if (a && !_prevButtonA) Launch();
            if (b && !_prevButtonB) ChangeColor();
            if (c && !_prevButtonC) _yawOffset = _imuInputManager.Ahrs.z;   // 今の向きを正面にする

            _prevButtonA = a;
            _prevButtonB = b;
            _prevButtonC = c;
        }

        // ==================================================================
        //  ParticleSystem を動かす
        // ==================================================================

        /// <summary>
        /// 火花の量・勢い、火の玉の大きさ、明かりの強さを、今の強さに合わせる
        /// </summary>
        void ApplySparks()
        {
            float i = CurrentIntensity;

            // パーリンノイズで、パチパチとゆらぐ感じを出す（1 を中心に ±Flicker）
            float noise = Mathf.PerlinNoise(Time.time * 12.0f, 0.37f);
            float flicker = 1.0f + (noise - 0.5f) * 2.0f * Flicker;

            if (_sparks != null)
            {
                // ParticleSystem のモジュールは、いったん変数に入れてから値を書きかえる
                var emission = _sparks.emission;
                emission.rateOverTimeMultiplier = Mathf.Lerp(BaseEmissionRate, MaxEmissionRate, i) * flicker;

                var main = _sparks.main;
                float speed = Mathf.Lerp(BaseSparkSpeed, MaxSparkSpeed, i);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            }

            if (_glow != null)
            {
                var main = _glow.main;
                main.startSizeMultiplier = Mathf.Lerp(0.12f, 0.22f, i) * flicker;
            }

            if (_tipLight != null)
            {
                _tipLight.intensity = Mathf.Lerp(BaseLightIntensity, MaxLightIntensity, i) * flicker;
            }
        }

        /// <summary>
        /// 打ち上げ花火を1発打ち上げる（ボタンA）
        /// </summary>
        public void Launch()
        {
            if (_rocket == null || _tip == null || _launchTimer > 0.0f)
            {
                return;
            }
            _launchTimer = LaunchCooldown;

            // 花火の棒の向き（Tip の上方向 = 緑の矢印）に打ち上げる
            // UpwardBias が 0 より大きいときは、その分だけ真上に寄せる
            Vector3 direction = Vector3.Lerp(_tip.up, Vector3.up, UpwardBias);
            if (direction.sqrMagnitude < 0.0001f)
            {
                // 真下に向けて、ちょうど打ち消し合ったとき用
                direction = _tip.up;
            }
            direction.Normalize();

            // EmitParams で、位置・速さ・色を指定して、1粒だけ出す
            // （Rocket は World 空間でシミュレーションしているので、位置は画面の中の座標で指定する）
            var emitParams = new ParticleSystem.EmitParams();
            emitParams.position = _tip.position;
            emitParams.velocity = direction * LaunchSpeed;
            emitParams.startColor = GetCurrentColor();
            emitParams.applyShapeToPosition = false;
            _rocket.Emit(emitParams, 1);
            // ※ 開く花火（Explosion）は、Rocket が消えたときに Sub Emitter として自動で出る
        }

        /// <summary>
        /// 色を、次の色に変える（ボタンB）
        /// </summary>
        public void ChangeColor()
        {
            if (Colors.Length == 0) return;
            _colorIndex = (_colorIndex + 1) % Colors.Length;
            ApplyColor();
        }

        Color GetCurrentColor()
        {
            return Colors.Length > 0 ? Colors[_colorIndex] : Color.white;
        }

        /// <summary>
        /// 今の色を、火花・火の玉・明かりに反映する
        /// （枝分かれの火花 SparkBranch は、Sub Emitter の Inherit Color で、親の色を引き継ぐ）
        /// </summary>
        void ApplyColor()
        {
            Color c = GetCurrentColor();

            if (_sparks != null)
            {
                var main = _sparks.main;
                main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.6f));
            }
            if (_glow != null)
            {
                var main = _glow.main;
                main.startColor = Color.Lerp(c, Color.white, 0.5f);
            }
            if (_tipLight != null)
            {
                _tipLight.color = c;
            }
        }
    }
}
