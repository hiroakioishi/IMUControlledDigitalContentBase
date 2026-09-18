using UnityEngine;

/// <summary>
/// M5Stack をシェイクすると音が鳴るスクリプト
///
/// 【しくみ】
///   加速度には、重力やゆっくりした傾きの分も含まれているので、
///   ゆっくり変わる成分をフィルターで追いかけて差し引き、「急な動きの強さ」だけを取り出します。
///   その強さが threshold（G）を超えたら音を鳴らします。
///   鳴らしたあとは cooldown 秒だけ次の判定を休み、1回のシェイクで何度も鳴らないようにします。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ShakeSound : MonoBehaviour
{
    [SerializeField]
    IMUInputManager _inputManager = null;

    [Header("シェイクの判定")]
    [Tooltip("この強さ（G）以上で振ったら鳴らす。小さいほど軽く振っただけで鳴る")]
    public float threshold = 1.0f;
    [Tooltip("一度鳴ったあと、次に鳴らせるようになるまでの時間（秒）")]
    public float cooldown = 0.25f;
    [Tooltip("重力・傾き成分を追いかける速さ")]
    public float gravityFilterSpeed = 3f;

    [Header("音")]
    [Tooltip("鳴らす音。空なら、スクリプトで作ったチャイムの音を使う")]
    public AudioClip clip;
    [Tooltip("強く振るほど大きな音にする")]
    public bool volumeByStrength = true;
    [Tooltip("鳴らすたびに音の高さを少しばらつかせる")]
    [Range(0f, 0.5f)] public float pitchRandom = 0.08f;

    [Header("テスト用（M5Stack がなくても鳴らせる）")]
    public KeyCode testKey = KeyCode.Space;

    [Header("確認用：いまのシェイクの強さ（G）")]
    public float currentStrength;

    AudioSource _audio;
    Vector3 _accLowPass;
    bool _lowPassReady;
    float _cooldownTimer;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;

        if (_inputManager == null)
        {
            _inputManager = FindAnyObjectByType<IMUInputManager>();
        }
        if (clip == null)
        {
            clip = CreateChime();
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _cooldownTimer -= dt;

        // ---- シェイクの強さを求める ----
        if (_inputManager != null)
        {
            Vector3 acc = _inputManager.Acceleration;
            if (!_lowPassReady) { _accLowPass = acc; _lowPassReady = true; }
            _accLowPass = Vector3.Lerp(_accLowPass, acc, 1f - Mathf.Exp(-gravityFilterSpeed * dt));
            currentStrength = (acc - _accLowPass).magnitude;

            // ---- しきい値を超えたら鳴らす ----
            if (currentStrength >= threshold && _cooldownTimer <= 0f)
            {
                Play(currentStrength);
            }
        }

        // ---- テスト用のキー ----
        if (Input.GetKeyDown(testKey))
        {
            Play(threshold * 2f);
        }
    }

    void Play(float strength)
    {
        _cooldownTimer = cooldown;

        float volume = 1f;
        if (volumeByStrength)
        {
            // しきい値ちょうど → 0.4、しきい値の3倍以上 → 1.0
            volume = Mathf.Lerp(0.4f, 1f, Mathf.InverseLerp(threshold, threshold * 3f, strength));
        }
        _audio.pitch = 1f + Random.Range(-pitchRandom, pitchRandom);
        _audio.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// 「チーン」というチャイムの音をプログラムで作る（音声ファイルを用意しなくても鳴らせるように）
    /// </summary>
    static AudioClip CreateChime()
    {
        const int sampleRate = 44100;
        const float length = 0.8f;
        int count = (int)(sampleRate * length);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float attack = Mathf.Min(1f, t * 400f);       // 鳴り始めのプチッという音を防ぐ
            float decay  = Mathf.Exp(-t * 6f);             // だんだん小さくなる
            float wave =
                0.60f * Mathf.Sin(2f * Mathf.PI * 1046.5f * t) +                       // ド（C6）
                0.25f * Mathf.Sin(2f * Mathf.PI * 2093.0f * t) * Mathf.Exp(-t * 4f) +  // 1オクターブ上
                0.12f * Mathf.Sin(2f * Mathf.PI * 3140.0f * t) * Mathf.Exp(-t * 8f);   // 高い倍音
            data[i] = 0.8f * attack * decay * wave;
        }

        AudioClip c = AudioClip.Create("Chime", count, 1, sampleRate, false);
        c.SetData(data, 0);
        return c;
    }
}
