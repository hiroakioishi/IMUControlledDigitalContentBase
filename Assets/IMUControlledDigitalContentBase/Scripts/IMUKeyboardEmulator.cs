using UnityEngine;

/// <summary>
/// キーボードで、M5Stack の IMU の値の代わりを作るスクリプト
/// （M5Stack が手元になくても、作品の動作確認ができる）
///
/// 【操作】
///   ← → キー          : Pitch（Ahrs.x）を傾ける
///   ↑ ↓ キー          : Roll（Ahrs.y）を傾ける
///   Shift + ← → キー  : Yaw（Ahrs.z）を回す
///   キーを離すと、傾きはゆっくり 0 に戻る
/// </summary>
public class IMUKeyboardEmulator : MonoBehaviour
{
    [Header("加速度, 角速度, 姿勢(Pitch, Roll, Yaw)")]
    /// <summary>
    /// 加速度（傾きに合わせて -1 ～ 1 で変化する。じっとしているときの 1G は含まない）
    /// </summary>
    public Vector3 Acceleration;

    /// <summary>
    /// 角速度（度/秒）
    /// </summary>
    public Vector3 Gyro;

    /// <summary>
    /// 姿勢（度） x: Pitch, y: Roll, z: Yaw
    /// </summary>
    public Vector3 Ahrs;

    [Header("設定")]
    /// <summary>
    /// 矢印キーを押し続けたときの、最大の傾き（度）
    /// </summary>
    public float MaxTiltAngle = 30.0f;

    /// <summary>
    /// 傾く・戻る速さ（大きいほど素早い）
    /// </summary>
    public float TiltSpeed = 3.0f;

    /// <summary>
    /// Shift + ← → で回る速さ（度/秒）
    /// </summary>
    public float YawSpeed = 90.0f;

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0.0f)
        {
            return;
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // --- キーの状態から、目標の傾きを決める（押していないときは 0） ---
        float targetPitch = 0.0f;
        float targetRoll  = 0.0f;
        float yawSpeed    = 0.0f;

        if (!shift)
        {
            if (Input.GetKey(KeyCode.LeftArrow))  targetPitch =  MaxTiltAngle;
            if (Input.GetKey(KeyCode.RightArrow)) targetPitch = -MaxTiltAngle;
            if (Input.GetKey(KeyCode.UpArrow))    targetRoll  =  MaxTiltAngle;
            if (Input.GetKey(KeyCode.DownArrow))  targetRoll  = -MaxTiltAngle;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow))  yawSpeed =  YawSpeed;
            if (Input.GetKey(KeyCode.RightArrow)) yawSpeed = -YawSpeed;
        }

        // 1フレーム前の姿勢（角速度を計算するために覚えておく）
        Vector3 previousAhrs = Ahrs;

        // --- 姿勢 ---
        // 目標の傾きに、少しずつ近づける（キーを離すと 0 に戻る）
        // Time.deltaTime を使っているので、パソコンの速さ（フレームレート）が違っても同じ速さで動く
        Ahrs.x = Mathf.Lerp(Ahrs.x, targetPitch, dt * TiltSpeed);
        Ahrs.y = Mathf.Lerp(Ahrs.y, targetRoll,  dt * TiltSpeed);
        // Yaw は、押している間だけ回り続ける（-180 ～ 180 の範囲にする）
        Ahrs.z = Mathf.DeltaAngle(0.0f, Ahrs.z + yawSpeed * dt);

        // --- 角速度（1秒あたりに、どれだけ角度が変わったか） ---
        Gyro.x = (Ahrs.x - previousAhrs.x) / dt;
        Gyro.y = (Ahrs.y - previousAhrs.y) / dt;
        Gyro.z = Mathf.DeltaAngle(previousAhrs.z, Ahrs.z) / dt;

        // --- 加速度（傾きに合わせて -1 ～ 1） ---
        float maxTilt = Mathf.Max(MaxTiltAngle, 1.0f);
        Acceleration = new Vector3(Ahrs.x / maxTilt, Ahrs.y / maxTilt, 0.0f);
    }
}
