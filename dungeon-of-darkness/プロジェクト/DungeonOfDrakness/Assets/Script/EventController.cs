using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Common;
using EventClass;
using PlayerClass;

//イベントを制御するクラス
public class EventController : MonoBehaviour
{
    //イベント発動条件を表す列挙型
    public enum EVACTIVATION
    {
        NOTHING,        //なし
        ENCOUNTER,      //移動直後に発動
        CHECK,          //調べるで発動
        BOTH            //移動直後、調べる両方（階段に使用）
    }

    //イベントの種類を表す列挙型
    public enum EVTYPE
    {
        NOTHING,        //なし
        UPSTAIRS,       //上り階段
        DOWNSTAIRS,     //下り階段
        NPC,            //NPC
        ENEMY,          //敵（固定）
        ITEM,           //アイテム
        TRAP,           //罠
        KEY_DOOR,       //鍵付き扉
        OTHER = 99      //その他
    }

    //罠イベント種類（イベント番号ファイルに設定）を表す列挙型
    public enum TRAPTYPE
    {
        NONE_TRAP,          //なし
        PIT_FALL,           //落とし穴
        POISON_ARROW,       //毒矢
        ROTATING_FLOOR      //回転床
    }

    //街のイベントフラグの列挙型
    public enum CITY_EV_FLAG
    {
        GUARD,              //衛兵
        WEAPON_MASTER,      //武器屋の店主
        KING,               //王様
        PRINCESS,           //姫
        EV_MAX              //イベントフラグ配列の要素数
    }

    //鍵付き扉の状態（イベントフラグファイルに設定）を表す列挙型
    public enum KEYFLAG
    {
        LOCKED,            //未開錠
        UNLOCK             //開錠済み
    }

    //スイッチで開けるタイプの鍵のイベント番号
    public const int SWITCH_LOCK = 999;        

    //落とし穴ダメージ
    private const int PITFALL_MIN = 5;  //最小値
    private const int PITFALL_MAX = 15; //最大値

    //毒矢ダメージ
    private const int P_ARROW_MIN = 2;  //最小値
    private const int P_ARROW_MAX = 8;  //最大値

    //回転床方向転換回数
    private const int R_FLOOR_MIN = 2;  //最小値
    private const int R_FLOOR_MAX = 8;  //最大値

    //回転床回転時間
    private const float ROT_TIME = 0.2f;

    //罠回避の計算に使用する定数
    private const int PITFALL_AVOID_BASE = 10;  //落とし穴回避の確率の計算に使用する基本値
    private const int PITFALL_AVOID_MAX = 60;   //落とし穴回避の確率の計算に使用する基本値（最大）
    private const int P_ARROW_AVOID_BASE = 30;  //毒矢回避の確率の計算に使用する基本値
    private const int P_ARROW_AVOID_MAX = 80;   //毒矢回避の確率の計算に使用する基本値（最大）
    private const int TRAP_AVOID_MAX = 100;     //罠回避の確率の計算に使用する最大値

    //その他イベントの定数
    public const int GUARD_BRIBE = 400;        //衛兵の賄賂
    public const int ORE_PRICE = 2000;         //ミストリル鉱石の値段
    public const int FROM_KING = 200;          //王様からもらえるお金

    private int rotFloorCount = 0;         //回転床方向転換カウンタ
    private int rotFloorMax = 0;           //回転床方向転換最大値
    private bool rotFloorFlag = false;     //回転床フラグ（true：回転中、false：回転していない）
    private int rotDirectionNow = 0;       //現在の方向
    private bool rotTurnDirection = false; //回転方向（true：反時計回り、false：時計回り）
    private float rotMoveAngle;            //現在の角度
    private float rotEndAngle;             //回転終了角度
    private float rotTimer;                //回転床の経過時間

    private TextAsset csvEvTypeFile;       //イベントの種類データのCSVファイル
    private TextAsset csvEvNumFile;        //イベント番号データのCSVファイル
    private TextAsset csvEvFlagFile;       //イベントの進行状況を示すフラグデータのCSVファイル
    private TextAsset csvEvActFile;        //イベントの発動条件データのCSVファイル
    private TextAsset csvEvDarkFile;       //ダークゾーンの有無を示すデータのCSVファイル

    private List<List<List<EventObject>>> eventDatas;   //ダンジョン内のイベントデータリスト

    private int[] cityEventFlag = new int[(int)CITY_EV_FLAG.EV_MAX];   //街内のイベントフラグ配列

    void Awake()
    {
        //ダンジョン内のイベントデータを読み込む
        CreateEventMap();

        //街のイベントフラグ配列を初期化する
        //CityEventFlagInit();
    }

    // Start is called before the first frame update
    void Start()
    {
 
    }

    // Update is called once per frame
    void Update()
    {
    }

    //ダンジョン内のイベントデータファイルを読み込む関数
    void CreateEventMap()
    {
        //イベントデータののCSVの中身を入れるリストの初期化
        List<List<string[]>> evtypeList = new List<List<string[]>>();   //イベントの種類
        List<List<string[]>> evnumList = new List<List<string[]>>();    //イベント番号
        List<List<string[]>> evflagList = new List<List<string[]>>();   //イベントの進行状況を示すフラグ
        List<List<string[]>> evactList = new List<List<string[]>>();    //イベントの発動条件
        List<List<string[]>> evdarkList = new List<List<string[]>>();   //ダークゾーンの有無

        //ダンジョン内のイベントデータリストの初期化
        eventDatas = new List<List<List<EventObject>>>();

        //フロアごとにデータを読み込み、そのデータをダンジョン内のイベントデータリストに渡す
        for (int f = 1; f <= GlobalConst.FLOOR_MAX; f++)
        {
            //読み込むファイル名を作成するためにフロア数のフォーマットを変更する
            string strFloor = GlobalConst.GetFloorString(f);

            //イベントの種類

            //作成したファイル名を元にファイルを読み込む
            csvEvTypeFile = Resources.Load(GlobalConst.DATA_DIR + "evtypefile" + strFloor) as TextAsset;
            StringReader readerType = new StringReader(csvEvTypeFile.text);
            evtypeList.Add(new List<string[]>());
            //ファイルの中身をリストに入れる
            while (readerType.Peek() != -1)
            {
                string line = readerType.ReadLine();
                evtypeList[f - 1].Add(line.Split(','));
            }

            //イベント番号

            //作成したファイル名を元にファイルを読み込む
            csvEvNumFile = Resources.Load(GlobalConst.DATA_DIR + "evnumfile" + strFloor) as TextAsset;
            StringReader readerNum = new StringReader(csvEvNumFile.text);
            evnumList.Add(new List<string[]>());
            //ファイルの中身をリストに入れる
            while (readerNum.Peek() != -1)
            {
                string line = readerNum.ReadLine();
                evnumList[f - 1].Add(line.Split(','));
            }

            //イベントの進行状況を示すフラグ

            //作成したファイル名を元にファイルを読み込む
            csvEvFlagFile = Resources.Load(GlobalConst.DATA_DIR + "evflagfile" + strFloor) as TextAsset;
            StringReader readerFlag = new StringReader(csvEvFlagFile.text);
            evflagList.Add(new List<string[]>());
            //ファイルの中身をリストに入れる
            while (readerFlag.Peek() != -1)
            {
                string line = readerFlag.ReadLine();
                evflagList[f - 1].Add(line.Split(','));
            }

            //イベントの発動条件

            //作成したファイル名を元にファイルを読み込む
            csvEvActFile = Resources.Load(GlobalConst.DATA_DIR + "evactfile" + strFloor) as TextAsset;
            StringReader readerAct = new StringReader(csvEvActFile.text);
            //ファイルの中身をリストに入れる
            evactList.Add(new List<string[]>());
            while (readerAct.Peek() != -1)
            {
                string line = readerAct.ReadLine();
                evactList[f - 1].Add(line.Split(','));
            }

            //ダークゾーンの有無

            //作成したファイル名を元にファイルを読み込む
            csvEvDarkFile = Resources.Load(GlobalConst.DATA_DIR + "evdarkfile" + strFloor) as TextAsset;
            StringReader readerDark = new StringReader(csvEvDarkFile.text);
            //ファイルの中身をリストに入れる
            evdarkList.Add(new List<string[]>());
            while (readerDark.Peek() != -1)
            {
                string line = readerDark.ReadLine();
                evdarkList[f - 1].Add(line.Split(','));
            }

            //それぞれのリストからダンジョン内のイベントデータリストにデータを渡す
            eventDatas.Add(new List<List<EventObject>>());
            for (int z = 0; z < evtypeList[0].Count; z++)
            {
                eventDatas[f - 1].Add(new List<EventObject>());
                for (int x = 0; x < evtypeList[f - 1][z].Length; x++)
                {
                    EventObject evobj = new EventObject();
                    evobj.eventType = (EVTYPE)(int.Parse(evtypeList[f - 1][z][x]));
                    evobj.eventNumber = int.Parse(evnumList[f - 1][z][x]);
                    evobj.eventFlag = int.Parse(evflagList[f - 1][z][x]);
                    evobj.evActivation = (EVACTIVATION)(int.Parse(evactList[f - 1][z][x]));
                    //ダークゾーンフラグはリストのデータが0の時はfalseを、1の時はtrueを入れる
                    if (int.Parse(evdarkList[f - 1][z][x]) == 0)
                    {
                        evobj.darkZoneFlag = false;
                    }
                    else
                    {
                        evobj.darkZoneFlag = true;
                    }
                    eventDatas[f - 1][z].Add(evobj);
                }
            }

        }
    }

    //ダンジョン内の指定された場所のイベント情報を渡す関数
    //引数
    //f:フロア
    //x:X座標
    //z:Z座標
    //戻り値（指定された場所のイベント情報）
    public EventObject GetEventInfo(int f, int x, int z)
    {
        EventObject eo = new EventObject();
        eo = eventDatas[f - 1][z][x];
        return eo;
    }

    //ダンジョン内のイベントデータリストを渡す関数
    //戻り値（ダンジョン内のイベントデータリスト）
    public List<List<List<EventObject>>> GetEventData()
    {
        return eventDatas;
    }

    //プレイヤーが持つイベントの進行状況を示すフラグを受け取る関数
    //引数
    //player:プレイヤーデータ
    public void SetEventFlag(Player player)
    {
        for (int f = 0; f < eventDatas.Count; f++)
        {
            for (int z = 0; z < eventDatas[0].Count; z++)
            {
                for (int x = 0; x < eventDatas[0][0].Count; x++)
                {
                    eventDatas[f][z][x].eventFlag = player.GetEventMapFlag(f, z, x);
                }
            }
        }
    }

    //街内のイベントフラグ配列の初期化を行う関数
    public void CityEventFlagInit()
    {

        //cityEventFlag = new int[(int)CITY_EV_FLAG.EV_MAX];

        for (int i = 0; i < cityEventFlag.Length; i++)
        {
            cityEventFlag[i] = 0;
        }
    }

    //指定した街内のイベントフラグをを渡す関数
    //引数
    //c_ev:指定対象のイベントフラグの要素番号（街のイベントフラグの列挙型を使用）
    //戻り値（指定した街のイベントの進行を表すフラグ）
    public int GetCityEventFlag(EventController.CITY_EV_FLAG c_ev)
    {
        return cityEventFlag[(int)c_ev];
    }

    //街内のイベントフラグ配列を渡す関数
    //戻り値（街内のイベントフラグ配列）
    public int[] GetCityEventFlagArray()
    {
        return cityEventFlag;
    }

    //プレイヤーが持つ街内イベントの進行状況を示すフラグを受け取る関数
    //引数
    //player:プレイヤーデータ
    public void SetCityEventFlag(Player player)
    {
        for (int i = 0; i < cityEventFlag.Length; i++)
        {
            cityEventFlag[i] = player.cityEventFlag[i];
        }
    }

    //指定した街内のイベントフラグを変更する関数
    //引数
    //c_ev:変更対象のイベントフラグの要素番号（街のイベントフラグの列挙型を使用）
    //value:変更値
    public void ChangeCityEventFlag(EventController.CITY_EV_FLAG c_ev, int value)
    {
        cityEventFlag[(int)c_ev] = value;
    }

    //罠の回避判定を行う関数
    //引数
    //type:罠の種類
    //speed:プレイヤーの素早さ
    //luck:プレイヤーの運
    //戻り値（true:回避成功、false:回避失敗）
    public bool TrapAvoidCheck(TRAPTYPE type, int speed, int luck)
    {
        //回避率を算出する
        int avoid_par = Mathf.RoundToInt((speed + luck) / 2.0f);

        //罠によって回避率の修正を行う
        switch (type)
        {
            case TRAPTYPE.PIT_FALL:         //落とし穴
                avoid_par += PITFALL_AVOID_BASE;
                if (avoid_par > PITFALL_AVOID_MAX)
                {
                    //回避率が最大値を超えた時は最大値を設定する
                    avoid_par = PITFALL_AVOID_MAX;
                }
                break;
            case TRAPTYPE.POISON_ARROW:     //毒矢
                avoid_par += P_ARROW_AVOID_BASE;
                if (avoid_par > P_ARROW_AVOID_MAX)
                {
                    //回避率が最大値を超えた時は最大値を設定する
                    avoid_par = P_ARROW_AVOID_MAX;
                }
                break;
            default:
                //該当する罠がない時はtrueを返す
                return true;
        }

        //乱数を取得する
        int rnd = Random.Range(0, TRAP_AVOID_MAX);

        if (rnd < avoid_par)
        {
            //回避成功
            return true;
        }
        else
        {
            //回避失敗
            return false;
        }
    }

    //罠によるダメージを渡す関数
    //引数
    //type:罠の種類
    //戻り値（罠によるダメージ）
    public int GetTrapDamage(TRAPTYPE type)
    {
        int rnd;
        //ランダム性を持たせるため乱数を使用（罠によって乱数の範囲が違う）
        switch (type)
        {
            case TRAPTYPE.PIT_FALL:     //落とし穴
                rnd = Random.Range(PITFALL_MIN, PITFALL_MAX + 1);
                break;
            case TRAPTYPE.POISON_ARROW: //毒矢
                rnd = Random.Range(P_ARROW_MIN, P_ARROW_MAX + 1);
                break;
            default:                    
                //該当する罠がない場合は0を返す
                rnd = 0;
                break;
        }

        return rnd;
    }

    //回転床の初期化を行う関数
    //引数
    //dir:回転前のプレイヤーの向いている方向（0が北で、時計回りで1ずつ増やす）
    //angle:Y軸を中心としたプレイヤーの現在の角度（北が0度）
    //戻り値（回転終了時のプレイヤーが向いている方向）※使用時はプレイヤークラスの方角に関する列挙型に変換する
    public int FloorRotateInit(int dir, float angle)
    {
        //タイマーの初期化
        rotTimer = 0.0f;
        //現在角度の設定
        rotMoveAngle = angle;
        //回転フラグを回転中にする
        rotFloorFlag = true;
        //回転床方向転換カウンタの初期化
        rotFloorCount = 0;
        //回転床方向転換最大値を設定（乱数使用）
        rotFloorMax = Random.Range(R_FLOOR_MIN, R_FLOOR_MAX + 1);
        //回転前の方向の設定
        rotDirectionNow = dir;

        //回転方向をランダムで決める
        if (Random.Range(0, 2) == 0)
        {
            //反時計回り
            rotTurnDirection = true;
        }
        else
        {
            //時計回り
            rotTurnDirection = false;
        }

        //回転終了時角度の設定

        //回転終了時角度の初期化
        rotEndAngle = angle;

        //回転床方向転換最大値に達するまで回転終了時角度を変化させる
        for (int i = 0; i < rotFloorMax; i++)
        {
            if (rotTurnDirection == true)
            {
                //反時計回りの時
                rotDirectionNow--;
                if (rotDirectionNow < 0)
                {
                    //北→西の時は3を入れる
                    rotDirectionNow = 3;
                }

                //回転終了時角度を90減らす
                rotEndAngle -= 90.0f;
            }
            else
            {
                //時計回りの時
                rotDirectionNow++;
                if (rotDirectionNow > 3)
                {
                    //西→北の時は0を入れる
                    rotDirectionNow = 0;
                }

                //回転終了時角度を90増やす
                rotEndAngle += 90.0f;
            }
        }

        return rotDirectionNow;

    }

    //回転床が回転中かどうかをチェックする関数
    //戻り値:（true:回転中、false:回転していない）
    public bool FloorRotateCheck()
    {
        return rotFloorFlag;
    }

    //回転床の回転処理を行う関数
    //戻り値（変更後の角度）
    public float FloorRotating()
    {
        rotTimer += Time.deltaTime;
        if (rotTimer >= ROT_TIME)
        {
            rotTimer = ROT_TIME;
        }

        if (rotTurnDirection == true)
        {
            //反時計回りの時は角度を減らしていく
            rotMoveAngle -= (rotTimer / ROT_TIME) * rotEndAngle;
        }
        else
        {
            //時計回りの時は角度を増やしていく
            rotMoveAngle += (rotTimer / ROT_TIME) * rotEndAngle;
        }

        if (rotTimer >= ROT_TIME)
        {
            //指定時間に達したとき
            
            //回転終了時の角度を設定する
            rotMoveAngle = (float)(rotDirectionNow * 90.0f);
            //回転床フラグをオフにする
            rotFloorFlag = false;
        }

        return rotMoveAngle;
    }

    //指定した位置にある扉の鍵を開ける処理を行う関数
    //引数
    //f:フロア
    //x:X座標
    //z:Z座標
    public void DoorKeyUnlock(int f, int x, int z)
    {
        EventObject eo = new EventObject();
        eo = eventDatas[f - 1][z][x];
        //鍵付き扉の状態を開錠済みに設定
        eo.eventFlag = (int)KEYFLAG.UNLOCK;
    }



}
