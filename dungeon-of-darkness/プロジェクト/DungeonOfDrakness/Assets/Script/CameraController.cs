using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerClass;
using Common;
using Cinemachine;

//ダンジョンにおけるカメラ（マップの表示、画面の振動）の制御を行うクラス
public class CameraController : MonoBehaviour
{
    public GameObject mapCamera;        //マップ用カメラプレハブ     
    public GameObject smallMapCamera;   //小マップ用カメラプレハブ 

    private Camera mapCam;              //マップ用カメラオブジェクト
    private Camera smallMapCam;         //小マップ用カメラオブジェクト

    private Text floorText;             //マップに表示する現在フロア
    private Text positionText;          //マップに表示する現在位置
    private Text directionText;         //マップに表示する向いている方向

    private CinemachineImpulseSource impulseSource; //Cinemachine Impulse Sourceオブジェクト（画面の振動に使用）

    // Start is called before the first frame update
    void Start()
    {
        //マップ用カメラを取得
        mapCam = mapCamera.GetComponent<Camera>();
        //小マップ用カメラを取得
        smallMapCam = smallMapCamera.GetComponent<Camera>();
        //マップ上の情報表示用オブジェクトの取得
        floorText = GameObject.Find("FloorText").GetComponent<Text>();
        positionText = GameObject.Find("PositionText").GetComponent<Text>();
        directionText = GameObject.Find("DirectionText").GetComponent<Text>();
        //Cinemachine Impulse Sourceオブジェクトの取得
        impulseSource = GameObject.FindGameObjectWithTag("ShakeCamera").GetComponent<CinemachineImpulseSource>();
    }

    //小マップ用カメラの座標を設定する関数
    //引数
    //x:X座標
    //y:Y座標
    //z:Z座標
    public void SmallMapCameraPositionSet(float x, float y, float z)
    {
        Vector3 v = new Vector3(x, y, z);
        smallMapCamera.transform.position = v;
    }

    //表示するマップを交代する関数
    //引数
    //flag:マップ表示フラグ（true:表示、false:非表示）※小マップはその逆となる
    public void MapChange(bool flag)
    {
        if (flag == true)
        {
            //マップ表示の時
            mapCam.enabled = flag;
            smallMapCam.enabled = !flag;
        }
        else
        {
            //マップ非表示の時
            mapCam.enabled = flag;
            smallMapCam.enabled = !flag;
        }
    }

    //マップを開く関数
    //引数
    //f:現在フロア
    //x:現在のX座標
    //z:Z座標の最大値から現在のZ座標を引いた値
    //dir:現在向いている方向
    public void MapOpen(int f, int x, int z, Player.DIRECTION dir)
    {
        //マップを表示させ、小マップを非表示にする
        mapCam.enabled = true;
        smallMapCam.enabled = false;

        //現在フロアを表示
        if (f == GlobalConst.FLOOR_MAX)
        {
            //最終フロアの時
            floorText.text = "？？？？";
        }
        else
        {
            //その他のフロアの時
            floorText.text = "地下" + f.ToString() + "階";
        }
        
        //現在向いている方向の設定
        string strDir = "北";

        switch (dir)
        {
            case Player.DIRECTION.NORTH:
                strDir = "北";
                break;
            case Player.DIRECTION.EAST:
                strDir = "東";
                break;
            case Player.DIRECTION.SOUTH:
                strDir = "南";
                break;
            case Player.DIRECTION.WEST:
                strDir = "西";
                break;
        }

        //現在位置と現在向いている方向を表示
        positionText.text = "北に" + z.ToString() + " " + "東に" + x.ToString();
        directionText.text = strDir;

    }

    //マップを閉じる関数
    //引数
    //flag:小マップ用アイテム所持フラグ（true:所持、false:未所持）
    public void MapClose(bool flag)
    {
        //マップを非表示にする
        mapCam.enabled = false;
        //小マップ用アイテム所持フラグがオンの時のみ小マップを表示する
        smallMapCam.enabled = flag;
    }

    //マップが開いているかチェックする関数
    //戻り値（true:開いている、false:閉じている）
    public bool MapOpenCheck()
    {
        return mapCam.enabled;
    }

    //ダンジョン画面を振動させる関数
    public void DungeonCameraShake()
    {
        impulseSource.GenerateImpulse();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
