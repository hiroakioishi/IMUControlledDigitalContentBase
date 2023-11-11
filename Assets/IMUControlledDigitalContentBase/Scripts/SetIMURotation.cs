using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IMUの入力管理スクリプト
/// </summary>
public class SetIMURotation : MonoBehaviour
{
    /// <summary>
    /// IMU入力管理スクリプトの参照
    /// </summary>
    [SerializeField]
    IMUInputManager _inputManager = null;

    /// <summary>
    /// 値を滑らかに変化させるかどうか
    /// </summary>
    [SerializeField]
    bool _smooth = false;

    /// <summary>
    /// 値を滑らかに変化させるときのスピード
    /// </summary>
    [SerializeField]
    float _smoothLerpAmp = 2.0f;

    Vector3 _rotation = Vector3.zero;

    void Update()
    {
        if (_inputManager != null)
        {
            // 値を滑らかに変化させる
            if (_smooth)
            {
                _rotation = Vector3.Lerp(_rotation, _inputManager.Ahrs, Time.deltaTime * _smoothLerpAmp);
            }
            // そのまま
            else
            {
                _rotation = _inputManager.Ahrs;
            }
            
            // 回転の値に代入
            transform.localEulerAngles = new Vector3(
                -1.0f * _rotation.y,
                0.0f,
                -1.0f * _rotation.x
            );
        }
    }
}
