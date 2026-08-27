using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

//カメラ振動のテストを行うクラス（EffectSampleSceneで使用）※不要になったらシーンごと削除
public class ShakerSample : MonoBehaviour
{
    public bool isFire = false;     //振動フラグ（true:振動させる、false:振動させない）
    private CinemachineImpulseSource source;    //CinemachineImpulseSourceオブジェクト

    // Start is called before the first frame update
    void Start()
    {
        //CinemachineImpulseSourceオブジェクトの取得
        source = GetComponent<CinemachineImpulseSource>();
    }

    // Update is called once per frame
    void Update()
    {
        //isFireチェックボックスをクリックすると振動する
        if (isFire)
        {
            source.GenerateImpulse();
            Debug.Log("Shake!");
            isFire = false;
        }
    }
}
