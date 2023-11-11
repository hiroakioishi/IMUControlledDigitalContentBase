using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMUKeyboardEmulator : MonoBehaviour
{
    [Header("加速度, 角速度, 姿勢(Pitch, Roll, Yaw)")]
    /// <summary>
    /// 加速度
    /// </summary>
    public Vector3 Acceleration;

    /// <summary>
    /// ジャイロ
    /// </summary>
    public Vector3 Gyro;

    /// <summary>
    /// 姿勢航法基準装置（Attitude and Heading Reference System）
    /// Pitch(), Roll(), Yaw()
    /// </summary>
    public Vector3 Ahrs;

    /// <summary>
    /// 加速度の減衰値
    /// </summary>
    public float AccDampingRate = 0.99f;


    void Start()
    {
        
    }

    void Update()
    {
        var dt = Time.deltaTime;

        var dAccX = 0.0f;
        var dAccY = 0.0f;
        var dAccZ = 0.0f;

        var dGyroX = 0.0f;
        var dGyroY = 0.0f;
        var dGyroZ = 0.0f;

        // --- X ---
        if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.LeftShift))
        {
            dAccX  = dt;
            dGyroX = 1.0f;
        }

        if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftShift))
        {
            dAccX  = -dt;
            dGyroX = -1.0f;
        }

        // --- Y ---
        if (Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.LeftShift))
        {
            dAccY  = dt;
            dGyroY = 1.0f;
        }

        if (Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.LeftShift))
        {
            dAccY  = -dt;
            dGyroY = -1.0f;
        }

        // --- Z ---
        if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            dAccZ = dt;
            dGyroZ = 1.0f;
        }

        if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            dAccZ = -dt;
            dGyroZ = -1.0f;
        }

        if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            dAccZ = dt;
            dGyroZ = 1.0f;
        }

        if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftShift))
        {
            dAccZ = -dt;
            dGyroZ = -1.0f;
        }

        // --- 加速度 ----------------------------------
        // 加速度Xについて
        if (dAccX != 0.0f)
        {
            // 加算
            Acceleration.x += dAccX;
        }
        else
        {
            // 少しずつ値を減衰させて、0に近づける
            Acceleration.x *= AccDampingRate;            
        }

        // 加速度Yについて
        if (dAccY != 0.0f)
        {
            // 加算
            Acceleration.y += dAccY;            
        }
        else
        {
            // 少しづつ値を減衰させて、0に近づける
            Acceleration.y *= AccDampingRate;
        }

        // 加速度Zについて
        if (dAccZ != 0.0f)
        {
            // 加算
            Acceleration.z += dAccZ;
        }
        else
        {
            // 少しずつ値を減衰させて、0に近づける
            Acceleration.z *= AccDampingRate;
        }

        // 値が小さくなれば、0にする
        Acceleration.x = Mathf.Abs(Acceleration.x) < 0.001f ? 0.0f : Acceleration.x;
        Acceleration.y = Mathf.Abs(Acceleration.y) < 0.001f ? 0.0f : Acceleration.y;
        Acceleration.z = Mathf.Abs(Acceleration.z) < 0.001f ? 0.0f : Acceleration.z;
        // 値の範囲を -1.0f～1.0fに制限する
        Acceleration.x = Mathf.Clamp(Acceleration.x, -1.0f, 1.0f);
        Acceleration.y = Mathf.Clamp(Acceleration.y, -1.0f, 1.0f);
        Acceleration.z = Mathf.Clamp(Acceleration.z, -1.0f, 1.0f);

        // --- 角速度 ----------------------------------
        // 角速度 値を変化させる
        Gyro.x = Mathf.Lerp(Gyro.x, dGyroX, dt * 1.0f);
        Gyro.y = Mathf.Lerp(Gyro.y, dGyroY, dt * 1.0f);
        Gyro.z = Mathf.Lerp(Gyro.z, dGyroZ, dt * 1.0f);
        // 値が小さくなれば、0にする
        Gyro.x = Mathf.Abs(Gyro.x) < 0.00001f ? 0.0f : Gyro.x;
        Gyro.y = Mathf.Abs(Gyro.y) < 0.00001f ? 0.0f : Gyro.y;
        Gyro.z = Mathf.Abs(Gyro.z) < 0.00001f ? 0.0f : Gyro.z;

        // --- 姿勢値 ----------------------------------
        // 姿勢値を変化させる
        Ahrs.x += Gyro.x;
        Ahrs.y += Gyro.y;
        Ahrs.z += Gyro.z;

        // 姿勢 (x: Pitch, y: Roll, z: Yaw)
        Ahrs.x = Ahrs.x % 360.0f;
        Ahrs.y = Ahrs.y % 360.0f;
        Ahrs.z = Ahrs.z % 360.0f;
    }
}
