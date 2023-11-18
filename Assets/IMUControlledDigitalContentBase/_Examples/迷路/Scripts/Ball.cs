using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace IMUControllerDigitalAContentBase.Example.Maze
{
    /// <summary>
    /// ボール
    /// </summary>
    public class Ball : MonoBehaviour
    {
        /// <summary>
        /// ゴールオブジェクトの名前
        /// </summary>
        public string GoalObjectName = "Goal";

        /// <summary>
        /// ボールが接触した時に実行するUnityEvent
        /// </summary>
        public UnityEvent GoalFunc = new UnityEvent();

        /// <summary>
        /// ボールが IsTrigger=True とセットされたオブジェクトと接触した時に呼び出される関数
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            // 接触したオブジェクトが GoalObjectName と一致していれば
            if (other.name == GoalObjectName)
            {
                // ゴールした時に実行するUnityEvent（インスペクタで参照した他のスクリプトの関数）を実行する
                GoalFunc.Invoke();
            }
        }
    }
}