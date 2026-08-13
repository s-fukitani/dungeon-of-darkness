using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Common;
using PlayerClass;
using ItemClass;
using EnemyClass;
using EventClass;
using GameDataClass;

//ダンジョン内においてプレイヤーの制御を行うクラス
public class PlayerController : MonoBehaviour
{
    //プレイヤーの行動に関する状態の列挙型
    enum ACTMODE
    {
        DUNGEON_IN,         //ダンジョンに入った直後
        DUNGEON_OUT,        //ダンジョンを出る直前（階段）
        DUNGEON_WARP_OUT,   //ダンジョンを出る直前（ワープ）
        RETURN_TITLE,       //タイトルへ戻る直前
        GAME_OVER,          //ゲームオーバー画面への移動直前
        FLOOR_CHANGING,     //フロア変更中
        MOVING,             //移動中
        STAYING,            //停止中
        C_DIRECTION,        //方向転換中
        ROT_FLOOR,          //回転床作動中
        TALKING,            //会話中
        USING_ITEM,         //アイテム使用中（戦闘時以外）
        CRASH_WALL,         //壁に衝突
        ENCOUNT_TRAP,       //罠遭遇時（回転床以外）
        SWITCH_ON,          //スイッチを押した時
        LOCKED_DOOR,        //扉が開かない時
        DOOR_OPEN           //扉を開けた時
    }

    //戦闘に関する状態の列挙型
    enum FIGHTMODE
    {
        NONE_FIGHT,             //非戦闘中
        FIGHT_BGM_PLAY,         //戦闘場面音声再生
        FIGHT_START,            //戦闘開始
        ENEMY_TALK,             //戦闘開始時の敵の会話（NPCを攻撃したときに使用）
        P_CONDITION_CHECK,      //プレイヤーの状態チェック
        PLAYER_WAIT,            //プレイヤーのコマンド選択
        PLAYER_ATTACK,          //プレイヤーの攻撃
        PLAYER_ITEM,            //プレイヤーのアイテム使用
        E_CONDITION_CHECK,      //敵の状態チェック
        ENEMY_ATTACK,           //敵の攻撃
        PLAYER_DEAD,            //プレイヤー死亡
        PLAYER_WIN,             //プレイヤー勝利
        PLAYER_ESCAPE,          //プレイヤー逃走
        L_BOSS_DEFEAT,          //最終ボス打倒
        E_BEFORE_EVENT,         //エンディング直前のイベント
        LEVEL_UP,               //レベルアップ
        FIGHT_BGM_STOP,         //戦闘場面音声停止
        FIGHT_END               //戦闘終了
    }

    //ダンジョンのBGMファイル名の配列
    private string[] dungeonBGMArray = new string[GlobalConst.FLOOR_MAX] {
        "bgm_floor1",
        "bgm_floor2",
        "bgm_floor3",
        "bgm_floor4",
        "bgm_floor5",
        "bgm_floor6",
        "bgm_floor7",
        "bgm_floor8"
    };

    private const float MOVE_TIME = 0.1f;       //プレイヤーの移動および方向転換時間
    private const float S_MAP_COM_POS_Y = 7.0f; //画面左上の小マップのカメラのY座標

    //ゲーム制御に関するオブジェクト
    private MapGenerator mapGenerator;          //マップ生成オブジェクト
    private CameraController cameraController;  //カメラ制御オブジェクト
    private EventController eventController;    //イベント制御オブジェクト
    private EnemyGenerator enemyGenerator;      //敵生成オブジェクト
    private ItemGenerator itemGenerator;        //アイテム生成オブジェクト
               
    private bool movableFlag;                   //移動操作可能フラグ（true:可能、false:不可能）                    
    private bool moveInFlag;                    //移動直後フラグ（true:移動直後、false:移動直後でない）※遭遇型イベント発動に使用
    private bool npcFlag;                       //NPC存在フラグ（true:存在している、false:存在していない）
    private bool fightFlag;                     //戦闘中フラグ（true:戦闘中、false:戦闘中でない）
    private bool statusFlag;                    //ステータスウィンドウフラグ（true:開いている、false:閉じている）                    
    private bool itemFlag;                      //アイテムウィンドウフラグ（true:開いている、false:閉じている）
    private bool miniMapFlag;                   //画面左上の小マップフラグ（true:開いている、false:閉じている）

    private ACTMODE actMode;                    //プレイヤーの行動に関する状態
    private int map_x_max;                      //マップ（配列）のx座標最大値
    private int map_z_max;                      //マップ（配列）のz座標最大値    
    private float movePosX;                     //プレイヤー移動に使用する変数（X座標）                  
    private float movePosZ;                     //プレイヤー移動に使用する変数（Z座標） 
    private float moveAngle;                    //プレイヤーの方向転換に使用する変数 （角度）
    private float endAngle;                     //プレイヤーの方向転換先の角度
    private float moveTimer;                    //プレイヤーの移動および方向転換に使用するタイマー（秒）
    Player.DIRECTION oldDirection;              //プレイヤーの向いている方向（退避用）※方向転換時に使用
    private int stepCount;                      //プレイヤーの歩数

    private Player player;                      //プレイヤーオブジェクト

    //ステータスウィンドウ（小）※コマンド表示時に画面右上に存在
    public GameObject statusPanel;              //ステータスウィンドウ（小）オブジェクト
    private Text LvText;                        //レベル
    private Text hpText;                        //HP
    private Text expText;                       //経験値
    private Text goldText;                      //ゴールド
    private Text conditionText;                 //状態

    //ステータスウィンドウ
    public GameObject statusWindow;             //ステータスウィンドウオブジェクト
    private Text st_nameText;                   //名前
    private Text st_levelText;                  //レベル
    private Text st_hpText;                     //HP
    private Text st_attackText;                 //攻撃力
    private Text st_defenseText;                //防御力
    private Text st_speedText;                  //素早さ
    private Text st_luckText;                   //運
    private Text st_conditionText;              //状態
    private Text st_expText;                    //経験値
    private Text st_goldText;                   //ゴールド
    private Text st_nextLevelText;              //次のレベルまでの経験値
    private Text st_weaponText;                 //装備武器
    private Text st_armorText;                  //装備鎧
    private Text st_shieldText;                 //装備盾
    private Text st_helmetText;                 //装備兜

    //アイテムウィンドウ
    public GameObject itemWindow;               //アイテムウィンドウオブジェクト
    [SerializeField] private GameObject[] itemNameLabels;        //アイテム名ラベル
    [SerializeField] private GameObject[] itemCountLabels;       //アイテム数ラベル
    [SerializeField] private GameObject[] itemEquipLabels;       //アイテム装備表示ラベル
    private Text itemExplanation;               //アイテム説明文
    private GameObject itemPageLabel;           //アイテムウィンドウページ
    private GameObject itemPreviousButton;      //前ページ移動ボタン
    private GameObject itemNextButton;          //次ページ移動ボタン
    private GameObject itemCloseButton;         //ウィンドウクローズボタン
    private List<Item> itemList;                //全アイテムリスト
    private bool itemClickFlag;                 //アイテム名クリックフラグ（true:クリックされた、false:クリックされていない）
    private int itemBoxNum;                     //選択された所持アイテムリスト番号
    private int saveItemNum;                    //選択されたアイテム名番号を保存しておく変数
    public int itemPage;                        //アイテムウィンドウの現在ページ
    public int itemPageMax;                     //アイテムウィンドウの最大ページ数

    //アイテム使用ウィンドウ
    public GameObject itemUsePanel;             //アイテム使用ウィンドウオブジェクト
    private Text itemUseText;                   //ウィンドウメッセージ
    private GameObject itemUseButton;           //使用ボタン
    private GameObject itemDiscardButton;       //廃棄ボタン
    private GameObject itemCancelButton;        //キャンセルボタン

    //コマンドパネル
    public GameObject commandPanel;             //コマンドパネルオブジェクト

    //メッセージパネル
    public GameObject messagePanel;             //メッセージパネルオブジェクト

    //イベント画像パネル
    public GameObject eventPanel;               //イベント画像パネルオブジェクト

    //セーブウィンドウ
    public GameObject saveWindow;               //セーブウィンドウオブジェクト
    [SerializeField] private GameObject[] saveNumberLabels;      //セーブデータ番号
    [SerializeField] private GameObject[] saveNameLabels;        //プレイヤー名
    [SerializeField] private GameObject[] saveLevelLabels;       //プレイヤーレベル
    [SerializeField] private GameObject[] saveFloorLabels;       //現在フロア
    [SerializeField] private GameObject[] saveTimeLabels;        //セーブ日時
    private GameObject savePageLabel;           //セーブウィンドウページ
    private GameObject savePreviousButton;      //前ページ移動ボタン
    private GameObject saveNextButton;          //次ページ移動ボタン
    private GameObject saveCloseButton;         //ウィンドウクローズボタン
    private GameObject saveGameEndButton;       //ゲーム終了ボタン
    private bool saveFlag;                      //セーブウィンドウフラグ（true:開いている、false:閉じている）
    public int savePage;                        //セーブウィンドウの現在ページ
    public int savePageMax;                     //セーブウィンドウの最大ページ数
    private bool saveClickFlag;                 //セーブデータクリックフラグ（true:クリックされた、false:クリックされていない）
    private int saveArrayNum;                   //選択されたセーブデータリスト番号
    private int saveSaveNum;                    //選択されたセーブデータ番号を保存しておく変数
    private bool gameEndFlag;                   //ゲーム終了ボタンクリックフラグ（true:クリックされている、false:クリックされていない）

    private GameObject darkZoneImage;           //ダークゾーン用のイメージオブジェクト
    private GameObject fadeImage;               //フェードインおよびフェードアウト用のイメージオブジェクト

    private Enemy fightEnemy = null;            //戦闘対象の敵オブジェクト

    private FIGHTMODE fightMode;                //戦闘場面の状態
    private FIGHTMODE nextFightMode;            //次に移行する戦闘場面の状態

    private bool fightWaitFlag;                 //戦闘場面状態変化時の待機状態であるかどうかのフラグ（true:待機状態、false:待機状態でない）           

    private bool fightTurn;                     //戦闘における行動ターンフラグ（true:プレイヤー、false:敵）

    private bool returnFlag;                    //帰還アイテム使用フラグ（true:使用中、false:未使用）

    private bool stairsFlag;                    //階段フラグ（true:上り階段、false:下り階段）

    private bool stairsDispFlag;                //階段表示フラグ（true:表示する、false:表示しない）※階段で迷宮に入った時および、フロア移動したときに階段の上り下りの確認を表示しないようにするために必要

    private bool pickUpFlag;                    //倒した敵もしくはNPCが落としたアイテムを拾うイベントが発生中かどうかのフラグ（true:発生中、false:発生中でない）

    private int eventCount;                     //イベントの進行状況を示すカウンタ

    private float currentTime;                  //タイマーの現在時間（フェードイン、フェードアウトに使用）

    private int playerAttackDamage;             //プレイヤーが敵に与えたダメージ
    private int enemyAttackDamage;              //敵がプレイヤーに与えたダメージ
    private string dungeonMessage;              //迷宮内でのメッセージを収納する文字列
    private bool criticalFlag;                  //会心の一撃フラグ（true:会心の一撃、false:会心の一撃でない）
    private int fightRandom;                    //戦闘時に乱数を入れる変数
    private EnemyAttackPattern.ATTACK_METHOD enemyAttackMethod;     //戦闘時に敵が使用した攻撃手段
    private int hitSPAttackPar;                 //特殊攻撃の成功率
    private int useItemId;                      //使用したアイテムのID
    private int preEquipBoxNumber;              //装備変更前に装備していたアイテムの所持アイテムリスト番号
    private int equipNumber;                    //装備アイテム種類番号
    private Item useItem;                       //使用したアイテムの情報
    private int doorPosX;                       //プレイヤーの一歩前のX座標（鍵アイテムを使用する時のため）
    private int doorPosZ;                       //プレイヤーの一歩前のZ座標（鍵アイテムを使用する時のため）

    // Start is called before the first frame update
    void Start()
    {
        //乱数の初期化
        Random.InitState(System.DateTime.Now.Millisecond);

        //ゲーム制御に関するオブジェクトの取得
        mapGenerator = GameObject.FindGameObjectWithTag("MapGenerator").GetComponent<MapGenerator>();
        cameraController = GameObject.FindGameObjectWithTag("CameraController").GetComponent<CameraController>();
        eventController = GameObject.FindGameObjectWithTag("EventController").GetComponent<EventController>();
        enemyGenerator = GameObject.FindGameObjectWithTag("EnemyGenerator").GetComponent<EnemyGenerator>();
        itemGenerator = GameObject.FindGameObjectWithTag("ItemGenerator").GetComponent<ItemGenerator>();

        //マップ（配列）の座標最大値取得
        map_x_max = mapGenerator.GetMapXMax();
        map_z_max = mapGenerator.GetMapZMax();

        //フラグおよびタイマーの初期化
        ACTMODE actMode = ACTMODE.DUNGEON_IN;
        moveTimer = 0.0f;
        movableFlag = true;
        moveInFlag = true;
        npcFlag = false;
        fightFlag = false;
        statusFlag = false;
        itemFlag = false;
        fightMode = FIGHTMODE.NONE_FIGHT;
        fightWaitFlag = false;
        itemClickFlag = false;
        saveFlag = false;
        saveClickFlag = false;
        gameEndFlag = false;
        returnFlag = false;
        stairsFlag = false;
        stairsDispFlag = false;
        pickUpFlag = false;
        eventCount = 0;
        currentTime = 0.0f;
        playerAttackDamage = 0;
        enemyAttackDamage = 0;
        dungeonMessage = "";
        criticalFlag = false;
        fightRandom = 0;
        enemyAttackMethod = EnemyAttackPattern.ATTACK_METHOD.NONE;
        hitSPAttackPar = 0;
        useItemId = 0;
        preEquipBoxNumber = -1;
        equipNumber = -1;
        useItem = new Item();
        doorPosX = 0;
        doorPosZ = 0;
        stepCount = 0;

        //ステータスウィンドウ（小）上のオブジェクト取得
        LvText = GameObject.Find("LevelText").GetComponent<Text>();
        hpText = GameObject.Find("HPText").GetComponent<Text>();
        expText = GameObject.Find("ExpText").GetComponent<Text>();
        goldText = GameObject.Find("GoldText").GetComponent<Text>();
        conditionText = GameObject.Find("ConditionText").GetComponent<Text>();

        //ステータスウィンドウ上のオブジェクト取得
        st_nameText = GameObject.Find("StNameText").GetComponent<Text>();
        st_levelText = GameObject.Find("StLevelText").GetComponent<Text>();
        st_hpText = GameObject.Find("StHPText").GetComponent<Text>();
        st_attackText = GameObject.Find("StAttackText").GetComponent<Text>();
        st_defenseText = GameObject.Find("StDefenseText").GetComponent<Text>();
        st_speedText = GameObject.Find("StSpeedText").GetComponent<Text>();
        st_luckText = GameObject.Find("StLuckText").GetComponent<Text>();
        st_conditionText = GameObject.Find("StConditionText").GetComponent<Text>();
        st_expText = GameObject.Find("StExpText").GetComponent<Text>();
        st_goldText = GameObject.Find("StGoldText").GetComponent<Text>();
        st_nextLevelText = GameObject.Find("StNextLevelText").GetComponent<Text>();
        st_weaponText = GameObject.Find("StWeaponText").GetComponent<Text>();
        st_armorText = GameObject.Find("StArmorText").GetComponent<Text>();
        st_shieldText = GameObject.Find("StShieldText").GetComponent<Text>();
        st_helmetText = GameObject.Find("StHelmetText").GetComponent<Text>();

        //アイテムウィンドウ上のオブジェクト取得
        itemExplanation = GameObject.Find("ItemExplainText").GetComponent<Text>();
        itemPageLabel = GameObject.FindGameObjectWithTag("ItemPage");
        itemPreviousButton = GameObject.FindGameObjectWithTag("ItemPrevious");
        itemNextButton = GameObject.FindGameObjectWithTag("ItemNext");
        itemCloseButton = GameObject.FindGameObjectWithTag("ItemClose");

        //アイテム使用ウィンドウ上のオブジェクト取得
        itemUseButton = GameObject.FindGameObjectWithTag("ItemUse");
        itemDiscardButton = GameObject.FindGameObjectWithTag("ItemDiscard");
        itemCancelButton = GameObject.FindGameObjectWithTag("ItemCancel");
        itemUseText = GameObject.Find("ItemUseText").GetComponent<Text>();

        //ダークゾーンイメージのオブジェクト取得
        darkZoneImage = GameObject.FindGameObjectWithTag("DarkZoneImage");

        //フェード用イメージのオブジェクト取得
        fadeImage = GameObject.FindGameObjectWithTag("FadeImage");

        if (GameDataController.newGameFlag == true)
        {
            #region デバッグ
            //ダンジョンシーンから実行した時のプレイヤーの初期化（デバッグ用）
            player = new Player();
            player.SetEventFlag(eventController.GetEventData());
            eventController.CityEventFlagInit();
            player.SetCityEventFlagArray(eventController.GetCityEventFlagArray());
            player.NowFloorChange(1);
            player.NowPositionSet(1, 24);
            player.NowDirectionSet(Player.DIRECTION.NORTH);
            movePosX = (float)player.nowPosX;
            movePosZ = (float)player.nowPosZ;
            moveAngle = (float)((int)player.nowDirection * 90.0f);
            Player.DIRECTION oldDirection = player.nowDirection;

            //デバッグ用レベルアップ
            //player.DebugLevelUp(2280);

            //デバッグ用アイテム取得
            player.ItemDebugDataInput();

            //デバッグ用カルマ設定
            //player.playerKarma = 7;

            //デバッグ用装備取得
            /*
            foreach (ItemBox box in player.GetItemBox())
            {
                Item item = itemGenerator.GetItemInfo(box.itemId);
                if (item.itemType > ItemGenerator.ITEM_TYPE.EQUIP && item.itemType < ItemGenerator.ITEM_TYPE.EQUIP_END)
                {
                    if (item.itemType == ItemGenerator.ITEM_TYPE.WEAPON)
                    {
                        if (box.itemEquiped == true)
                        {
                            player.Equip(item, 0);
                        }
                    }
                    if (item.itemType == ItemGenerator.ITEM_TYPE.ARMOR)
                    {
                        if (box.itemEquiped == true)
                        {
                            player.Equip(item, 8);
                        }
                    }
                    if (item.itemType == ItemGenerator.ITEM_TYPE.SHIELD)
                    {
                        if (box.itemEquiped == true)
                        {
                            player.Equip(item, 14);
                        }
                    }
                    if (item.itemType == ItemGenerator.ITEM_TYPE.HELM)
                    {
                        if (box.itemEquiped == true)
                        {
                            player.Equip(item, 19);
                        }
                    }
                }

            }
            */

            GameDataController.newGameFlag = false;

            #endregion
        }
        else
        {
            //ロード時もしくはダンジョンに入った時

            //プレイヤーデータの取得
            player = GameDataController.GetPlayerData();
            eventController.SetEventFlag(player);
            eventController.SetCityEventFlag(player);
            movePosX = (float)player.nowPosX;
            movePosZ = (float)player.nowPosZ;
            moveAngle = (float)((int)player.nowDirection * 90.0f);
            Player.DIRECTION oldDirection = player.nowDirection;

            //データロード直後に遭遇型イベントが始まらないようにする
            if (GameDataController.loadFlag == true)
            {
                moveInFlag = false;
                GameDataController.loadFlag = false;
            }
        }

        //全アイテムリストのデータ取得
        itemList = itemGenerator.GetItemList();

        //選択された所持アイテムリスト番号の初期化
        itemBoxNum = 0;

        //戦闘における行動ターンフラグの初期化
        fightTurn = true;

        //画面左上の小マップフラグの取得
        miniMapFlag = BoxItemMiniMapCheck();

        //アイテムウィンドウのページの初期化
        itemPage = 1;
        itemPageMax = player.ItemPageMaxCalc();

        //セーブウィンドウのページの初期化
        savePage = 1;
        savePageMax = GameDataController.SavePageMaxCalc();

        //マップを閉じ、小マップ用のアイテムがある時は小マップを表示する
        cameraController.MapClose(miniMapFlag);
        cameraController.SmallMapCameraPositionSet(transform.position.x, S_MAP_COM_POS_Y, transform.position.z);

        //ステータスウィンドウ（小）の初期化
        StatusDisp();
        //セーブウィンドウの初期化
        SaveWindowInit();

        //全てのウィンドウおよびパネルを閉じる
        statusPanel.SetActive(false);
        commandPanel.SetActive(false);
        MessageController.msgController.MessagePanelClose();
        eventPanel.SetActive(false);
        statusWindow.SetActive(false);
        itemWindow.SetActive(false);
        itemUsePanel.SetActive(false);
        cameraController.MapClose(false);

        //ダークゾーンをオフにする
        DarkZoneChange(false);

        //フェード用イメージを黒にする
        FadeImageInit(true);

    }

    // Update is called once per frame
    void Update()
    {
        //イベントオブジェクトの初期化
        EventObject eo = new EventObject();

        if (actMode == ACTMODE.DUNGEON_IN)
        {
            //ダンジョンに入った直後もしくはロード直後
            switch (eventCount)
            {
                case 0:
                    //現在位置のイベント情報の取得
                    EventObject ev_now = eventController.GetEventInfo(player.nowFloor, player.nowPosX, player.nowPosZ);

                    //ダークゾーンチェック
                    if (DarkZoneCheck(ev_now) == true)
                    {
                        //ダークゾーンをオンにする
                        DarkZoneChange(true);
                        //フェード用イメージを透明にする
                        FadeImageInit(false);
                        //フェードインを飛ばす
                        eventCount += 2;
                    }
                    else
                    {
                        //ダークゾーンをオフにする
                        DarkZoneChange(false);
                        //フェードインへ移行する
                        eventCount++;
                    }

                    break;
                case 1:
                    //ダンジョン背景をフェードインする
                    if (BlackFadeOut(0.75f) == true)
                    {
                        eventCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    eventCount = 0;

                    //小マップ用のアイテムがある時は小マップを表示する
                    cameraController.MapClose(miniMapFlag);
                    cameraController.SmallMapCameraPositionSet(transform.position.x, S_MAP_COM_POS_Y, transform.position.z);

                    //BGMを再生する
                    SoundManager.soundManager.PlayBGM(dungeonBGMArray[player.nowFloor - 1], 0.5f, true);

                    //プレイヤーの行動に関する状態を「プレイヤーが停止中の時」に移行する
                    actMode = ACTMODE.STAYING;
                    break;
            }
            
        }
        else if (actMode == ACTMODE.DUNGEON_OUT)
        {
            //ダンジョンを出る直前（階段）
            switch (eventCount)
            {
                case 0:
                    //BGMの停止
                    SoundManager.soundManager.StopBGM(0.5f);

                    //全てのパネルおよびウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();
                    EventImageController.evImageController.ImagePanelClose();
                    MessageController.msgController.MessagePanelClose();
                    cameraController.MapClose(false);

                    eventCount++;
                    break;
                case 1:
                    //階段を上る効果音を鳴らす
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        SoundManager.soundManager.PlaySE("se_stairs");
                        eventCount++;
                    }
                    break;
                case 2:
                    //ダンジョン背景をフェードアウトする
                    if (BlackFadeIn(0.75f) == true)
                    {
                        eventCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    eventCount = 0;

                    //街の入口へ移動する
                    player.NowFloorChange(-1);
                    player.NowPositionSet(0, 0);
                    player.SetEventFlag(eventController.GetEventData());
                    player.SetCityEventFlagArray(eventController.GetCityEventFlagArray());
                    GameDataController.SetPlayerData(player);
                    SceneManager.LoadScene("CityScene");

                    break;
            }

        }
        else if (actMode == ACTMODE.DUNGEON_WARP_OUT)
        {
            //ダンジョンを出る直前（ワープ）
            switch (eventCount)
            {
                case 0:
                    //BGMの停止
                    SoundManager.soundManager.StopBGM(0.5f);

                    //一時的にコマンドボタンをクリックできないようにする
                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                    eventCount++;
                    break;
                case 1:
                    //一定時間待機の後、全てのパネルおよびウィンドウを閉じる
                    if (CommonMethod.TimeWait(2.0f) == true)
                    {
                        YesNoController.yesNoController.YesNoPanelClose();
                        EventImageController.evImageController.ImagePanelClose();
                        MessageController.msgController.MessagePanelClose();
                        cameraController.MapClose(false);
                        statusPanel.SetActive(false);
                        commandPanel.SetActive(false);

                        eventCount++;
                    }
                    break;
                case 2:
                    //ワープの効果音を鳴らす
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        SoundManager.soundManager.PlaySE("se_warp_out");
                        eventCount++;
                    }
                    break;
                case 3:
                    //ダンジョン背景をフェードアウトする
                    if (BlackFadeIn(3.0f) == true)
                    {
                        eventCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    eventCount = 0;

                    //街の入口へ移動する
                    player.NowFloorChange(-1);
                    player.NowPositionSet(0, 0);
                    player.SetEventFlag(eventController.GetEventData());
                    player.SetCityEventFlagArray(eventController.GetCityEventFlagArray());
                    GameDataController.SetPlayerData(player);
                    SceneManager.LoadScene("CityScene");

                    break;
            }
        }
        else if (actMode == ACTMODE.FLOOR_CHANGING)
        {
            //フロア変更中
            switch (eventCount)
            {
                case 0:
                    //BGMの停止
                    SoundManager.soundManager.StopBGM(0.5f);

                    //全てのパネルおよびウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();
                    EventImageController.evImageController.ImagePanelClose();
                    MessageController.msgController.MessagePanelClose();
                    cameraController.MapClose(false);

                    eventCount++;
                    break;
                case 1:
                    //階段を上る効果音を鳴らす
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        SoundManager.soundManager.PlaySE("se_stairs");
                        eventCount++;
                    }
                    break;
                case 2:
                    //ダークゾーンチェック
                    if (DarkZoneCheck(eo) == true)
                    {
                        //フェード用イメージを黒にする
                        FadeImageInit(true);
                        //フェードアウトを飛ばす
                        eventCount += 2;
                    }
                    else
                    {
                        //フェードアウトへ移行する
                        eventCount++;
                    }
                    break;
                case 3:
                    //ダンジョン背景をフェードアウトする
                    if (BlackFadeIn(0.75f) == true)
                    {
                        eventCount++;
                    }
                    break;
                case 4:

                    //フロアを変更する
                    if (stairsFlag == true)
                    {
                        //上り階段の時
                        player.FloorUpDown(true);
                    }
                    else
                    {
                        //下り階段の時
                        player.FloorUpDown(false);
                    }
                    mapGenerator.ReCreateMap(player.nowFloor);

                    eventCount++;
                    break;
                case 5:
                    //現在位置のイベント情報の取得
                    EventObject ev_now = eventController.GetEventInfo(player.nowFloor, player.nowPosX, player.nowPosZ);

                    //ダークゾーンチェック
                    if (DarkZoneCheck(ev_now) == true)
                    {
                        //ダークゾーンをオンにする
                        DarkZoneChange(true);
                        //フェード用イメージを透明にする
                        FadeImageInit(false);
                        //フェードインを飛ばす
                        eventCount += 2;
                    }
                    else
                    {
                        //ダークゾーンをオフにする
                        DarkZoneChange(false);
                        //フェードインへ移行する
                        eventCount++;
                    }
                    break;
                case 6:
                    //ダンジョン背景をフェードインする
                    if (BlackFadeOut(0.75f) == true)
                    {
                        eventCount++;
                    }
                    break;
                default:
                    //小マップ用のアイテムがある時は小マップを表示する
                    cameraController.MapClose(miniMapFlag);
                    cameraController.SmallMapCameraPositionSet(transform.position.x, S_MAP_COM_POS_Y, transform.position.z);

                    //BGMを再生する
                    SoundManager.soundManager.PlayBGM(dungeonBGMArray[player.nowFloor - 1], 0.5f, true);

                    //カウンタの初期化
                    eventCount = 0;

                    //プレイヤーの行動に関する状態を「プレイヤーが停止中の時」に移行する
                    actMode = ACTMODE.STAYING;

                    //階段表示フラグをオフにする
                    stairsDispFlag = false;

                    movableFlag = true;
                    moveInFlag = true;
                    break;
            }
        }
        else if (actMode == ACTMODE.RETURN_TITLE)
        {
            //タイトルへ戻る直前
            switch (eventCount)
            {
                case 0:
                    //BGMの停止
                    SoundManager.soundManager.StopBGM(0.5f);

                    //一時的にコマンドボタンをクリックできないようにする
                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                    eventCount++;
                    break;
                case 1:
                    //効果音が終了したとき、次へ進む
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        eventCount++;
                    }
                    break;
                case 2:
                    //一定時間待機の後、全てのパネルおよびウィンドウを閉じる
                    if (CommonMethod.TimeWait(2.0f) == true)
                    {
                        EventImageController.evImageController.ImagePanelClose();
                        MessageController.msgController.MessagePanelClose();
                        cameraController.MapClose(false);
                        statusPanel.SetActive(false);
                        commandPanel.SetActive(false);
                        SaveWindowClose();

                        eventCount++;
                    }
                    break;
                case 3:
                    //ダンジョン背景をフェードアウトする
                    if (BlackFadeIn(1.0f) == true)
                    {
                        eventCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    eventCount = 0;
                    //タイトル画面へ移動するへ移動する
                    GoTitle();

                    break;
            }
        }
        else if (actMode == ACTMODE.GAME_OVER)
        {
            //ゲームオーバー画面への移動直前
            switch (eventCount)
            {
                case 0:
                    //BGMを停止する
                    SoundManager.soundManager.StopBGM(0.5f);
                    //コマンドボタンを使用不可にして、メッセージを表示する
                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
                    MessageController.msgController.MessageDisp("あなたは死にました。\n");

                    eventCount++;
                    break;
                case 1:
                    //一定時間待機の後、全てのパネルおよびウィンドウを閉じる
                    if (CommonMethod.TimeWait(2.0f) == true)
                    {
                        EventImageController.evImageController.ImagePanelClose();
                        MessageController.msgController.MessagePanelClose();
                        cameraController.MapClose(false);
                        statusPanel.SetActive(false);
                        commandPanel.SetActive(false);

                        eventCount++;
                    }
                    break;
                case 2:
                    //ダンジョン背景をフェードアウトする
                    if (BlackFadeIn(1.0f) == true)
                    {
                        eventCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    eventCount = 0;

                    //ゲームオーバー画面への移動
                    GoGameOver();
                    break;
            }
        }
        else
        {
            //現在地のイベント情報の取得
            eo = eventController.GetEventInfo(player.nowFloor, player.nowPosX, player.nowPosZ);

            //画面左上の小マップフラグの取得
            miniMapFlag = BoxItemMiniMapCheck();

            //ステータスウィンドウ（小）の表示
            StatusDisp();

            if (actMode == ACTMODE.STAYING)
            {
                //プレイヤーが停止中の時
                if (moveInFlag == true)
                {
                    //移動直後フラグがオンのとき

                    //ダークゾーンチェック
                    if (DarkZoneCheck(eo) == true)
                    {
                        //ダークゾーンをオンにする
                        DarkZoneChange(true);
                    }
                    else
                    {
                        //ダークゾーンをオフにする
                        DarkZoneChange(false);
                    }

                    //毒によるダメージ
                    MovePoison();

                    //祝福によるHP回復
                    MoveBlessing();

                    //プレイヤー死亡チェック
                    if (player.DeadCheck() == false)
                    {
                        //生存時は遭遇型イベントを発生させる

                        //遭遇型イベントもしくは両方型イベントの時
                        if (eo.evActivation == EventController.EVACTIVATION.ENCOUNTER || 
                            eo.evActivation == EventController.EVACTIVATION.BOTH)
                        {

                            //NPC
                            if (eo.eventType == EventController.EVTYPE.NPC)
                            {

                                if (player.nowFloor == 1)
                                {
                                    //地下1階

                                    if (eo.eventNumber == 1)
                                    {
                                        //魔法の地図の情報を持つ冒険者
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(45);
                                        }

                                    }
                                    else if (eo.eventNumber == 2)
                                    {
                                        //鍵売りの老人
                                        if (eo.eventFlag == 0)
                                        {
                                            //通常時
                                            NPCEncount(46);
                                        }
                                        else if (eo.eventFlag == 2)
                                        {
                                            //戦って倒したとき
                                            KillNPCItemGet(eo, (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY1);
                                        }
                                    }

                                }
                                else if (player.nowFloor == 2)
                                {
                                    //地下2階
                                    if (eo.eventNumber == 1)
                                    {
                                        //ミストリル鉱石の情報を持つ冒険者
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(47);
                                        }

                                    }


                                }
                                else if (player.nowFloor == 3)
                                {
                                    //地下3階
                                    if (eo.eventNumber == 1)
                                    {
                                        //衛兵
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(48);
                                        }

                                    }
                                    else if (eo.eventNumber == 2)
                                    {
                                        //地下3階の冒険者
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(49);
                                        }
                                    }
                                }
                                else if (player.nowFloor == 4)
                                {
                                    //地下4階
                                    if (eo.eventNumber == 1)
                                    {
                                        //雷の杖を持っている魔法使い
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(50);
                                        }
                                        else if (eo.eventFlag == 2)
                                        {
                                            //戦って倒したとき
                                            KillNPCItemGet(eo, (int)ItemGenerator.EVITEM_FIGHT.WAND1);
                                        }

                                    }
                                }
                                else if (player.nowFloor == 5)
                                {
                                    //地下5階
                                    if (eo.eventNumber == 1)
                                    {
                                        //謎の人物
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(51);
                                        }
                                        else if (eo.eventFlag == 2)
                                        {
                                            //戦って倒したとき
                                            KillNPCItemGet(eo, (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY3);
                                        }

                                    }
                                }
                                else if (player.nowFloor == 7)
                                {
                                    //地下7階
                                    if (eo.eventNumber == 1)
                                    {
                                        //死神
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(43);
                                        }
                                    }
                                }
                                else if (player.nowFloor == 8)
                                {
                                    //地下8階
                                    if (eo.eventNumber == 1)
                                    {
                                        //魔王
                                        if (eo.eventFlag == 0)
                                        {
                                            NPCEncount(44);
                                        }
                                    }
                                }


                            }
                            else if (eo.eventType == EventController.EVTYPE.ENEMY)
                            {
                                //敵

                                //地下6階
                                if (player.nowFloor == 6)
                                {
                                    //ゴーレム
                                    if (eo.eventNumber == 1)
                                    {
                                        if (eo.eventFlag == 0)
                                        {
                                            movableFlag = false;
                                            fightFlag = true;
                                            fightEnemy = null;
                                            fightEnemy = new Enemy();
                                            fightEnemy.EnemyDataSet(enemyGenerator.GetEnemy(41));
                                            MoveToFightScene();
                                        }
                                    }

                                }

                            }
                            else if (eo.eventType == EventController.EVTYPE.TRAP)
                            {
                                //罠
                                if (eo.eventNumber != (int)EventController.TRAPTYPE.ROTATING_FLOOR)
                                {
                                    //回転床以外

                                    //プレイヤーの行動に関する状態を「罠遭遇時（回転床以外）」に移行する
                                    actMode = ACTMODE.ENCOUNT_TRAP;

                                }
                                else
                                {
                                    //回転床
                                    MessageController.msgController.MessageJoinDisp("回転床だ！！\n");
                                    oldDirection = player.nowDirection;
                                    player.nowDirection = (Player.DIRECTION)eventController.FloorRotateInit((int)oldDirection, transform.localEulerAngles.y);
                                    //プレイヤーの行動に関する状態を「回転床作動中」に移行する
                                    actMode = ACTMODE.ROT_FLOOR;
                                }

                            }
                            else if (eo.eventType == EventController.EVTYPE.UPSTAIRS)
                            {
                                //上り階段
                                if (stairsDispFlag == true)
                                {
                                    //階段表示フラグがオンの時に表示する
                                    EventImageController.evImageController.ImageDisp(GlobalConst.IMG_UPSTAIRS);
                                    MessageController.msgController.MessageDisp("上る階段がある。\n上りますか？");
                                    YesNoController.yesNoController.YesNoPanelOpen();
                                    movableFlag = false;
                                }
                                
                            }
                            else if (eo.eventType == EventController.EVTYPE.DOWNSTAIRS)
                            {
                                //下り階段
                                if (stairsDispFlag == true)
                                {
                                    //階段表示フラグがオンの時に表示する
                                    EventImageController.evImageController.ImageDisp(GlobalConst.IMG_DOWNSTAIRS);
                                    MessageController.msgController.MessageDisp("下る階段がある。\n下りますか？");
                                    YesNoController.yesNoController.YesNoPanelOpen();
                                    movableFlag = false;
                                }
                            }
                            else
                            {
                                //その他
                                if (player.nowFloor == 1)
                                {
                                    //地下1階
                                    if (eo.eventNumber >= 1 && eo.eventNumber <= 4)
                                    {
                                        //持ち物に関する注意、カルマに関する情報、銀の女神像の情報、迷宮の調査に関する情報のどれか
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber == 5)
                                    {
                                        //銀の女神像

                                        //銀の女神像の画像を表示する
                                        EventImageController.evImageController.ImageDisp("silver_goddess");

                                        //カルマチェック
                                        if (player.playerKarma == 0)
                                        {
                                            //カルマが0のとき

                                            //メッセージを表示する
                                            MessageController.msgController.MessageDisp("銀の女神像があるが、今は祈る必要がない。\n");
                                            movableFlag = false;

                                            //少したってから画像とメッセージを消去する（NPC退去関数を流用する）
                                            Invoke("NPCMoveOut", 5.0f);
                                        }
                                        else
                                        {
                                            //カルマが1以上のとき

                                            //カルマを0にするのに必要なゴールドを取得する
                                            int gold = player.GetKarmaGold();

                                            //メッセージを作成する
                                            string msg = "銀の女神像がある。\n" + gold + "ゴールド寄付して祈りますか？\n";

                                            //メッセージとYesNoウィンドウを表示する
                                            MessageController.msgController.MessageDisp(msg);
                                            YesNoController.yesNoController.YesNoPanelOpen();
                                            movableFlag = false;
                                        }
                                    }
                                }
                                else if (player.nowFloor == 2)
                                {
                                    //地下2階
                                    if (eo.eventNumber == 1)
                                    {
                                        //金のカギに関する情報
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                }
                                else if (player.nowFloor == 3)
                                {
                                    //地下3階
                                    if (eo.eventNumber == 1)
                                    {
                                        //勇者の装備に関する情報
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                }
                                else if (player.nowFloor == 4)
                                {
                                    //地下4階
                                    if (eo.eventNumber == 1)
                                    {
                                        //紋章の扉の情報
                                        MessageController.msgController.MessageDisp("うっすらと光り輝く文字で\n壁に何か書いてある。\n" +
                                                            "調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber == 2)
                                    {
                                        //扉の絵の情報
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                }
                                else if (player.nowFloor == 5)
                                {
                                    //地下5階
                                    if (eo.eventNumber == 1)
                                    {
                                        //紋章の扉の情報
                                        MessageController.msgController.MessageDisp("うっすらと光り輝く文字で\n壁に何か書いてある。\n" +
                                                            "調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber >= 2 && eo.eventNumber <= 4)
                                    {
                                        //地下6階の情報
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                }
                                else if (player.nowFloor == 6)
                                {
                                    //地下6階
                                    if (eo.eventNumber == 1)
                                    {
                                        //紋章の扉の情報
                                        MessageController.msgController.MessageDisp("うっすらと光り輝く文字で\n壁に何か書いてある。\n" +
                                                            "調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber >= 2 && eo.eventNumber <= 4)
                                    {
                                        //鏡の番人の情報、真実の腕輪の情報、ダークゾーンの警告のどれか
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                }
                                else if (player.nowFloor == 7)
                                {
                                    //地下7階
                                    if (eo.eventNumber == 1)
                                    {
                                        //紋章の扉の情報
                                        MessageController.msgController.MessageDisp("うっすらと光り輝く文字で\n壁に何か書いてある。\n" +
                                                            "調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber >= 2 && eo.eventNumber <= 5)
                                    {
                                        //死神の情報、悪魔の像の情報、死神の警告、謁見の間のどれか
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber == 6 || eo.eventNumber == 7)
                                    {
                                        //姫の石像（偽物1）または姫の石像（偽物2）

                                        //真実の腕輪の情報を取得
                                        Item item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.BANGLE);

                                        //真実の腕輪の所持しているか、姫が救出済みもしくは死亡しているとき
                                        if (player.ItemHaveCheck(item) == true ||
                                            eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) > 0)
                                        {
                                            //フラグを1にする
                                            eo.eventFlag = 1;
                                        }

                                        //フラグが0の時のみイベントを実行する
                                        if (eo.eventFlag == 0)
                                        {
                                            PrincessStatueEvent();
                                        }
                                    }
                                    else if (eo.eventNumber == 8)
                                    {
                                        //姫の石像

                                        //フラグが0の時のみイベントを実行する
                                        if (eo.eventFlag == 0)
                                        {
                                            PrincessStatueEvent();
                                        }
                                    }
                                    else if (eo.eventNumber == 9)
                                    {
                                        //金の女神像の情報
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber == 10)
                                    {
                                        //金の女神像

                                        //金の女神像の画像を表示する
                                        EventImageController.evImageController.ImageDisp("gold_goddess");

                                        //姫の生死チェック
                                        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) != 3)
                                        {
                                            //姫フラグが3以外（姫生存）のとき

                                            //メッセージを表示する
                                            MessageController.msgController.MessageDisp("金の女神像があるが、今は祈る必要がない。\n");
                                            movableFlag = false;

                                            //少したってから画像とメッセージを消去する（NPC退去関数を流用する）
                                            Invoke("NPCMoveOut", 5.0f);
                                        }
                                        else
                                        {
                                            //姫フラグが3（姫死亡）のとき

                                            //命の花の情報を取得
                                            Item item = new Item();
                                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.FLOWER);

                                            //命の花が格納されている所持アイテムリストのインデックス番号を取得する
                                            int item_index = player.GetItemBoxSpecifyIndex(item);

                                            //命の花の所持チェック
                                            if (item_index == -1)
                                            {
                                                //命の花を持っていないとき

                                                //メッセージを表示する
                                                MessageController.msgController.MessageDisp("金の女神像があるが、\n今はどうすることもできない。\n");
                                                movableFlag = false;

                                                //少したってから画像とメッセージを消去する（NPC退去関数を流用する）
                                                Invoke("NPCMoveOut", 5.0f);
                                            }
                                            else
                                            {
                                                //メッセージを作成する
                                                string msg = "金の女神像がある。\n" + item.itemName + "を捧げて祈りますか？\n";
                                                //メッセージとYesNoウィンドウを表示する
                                                MessageController.msgController.MessageDisp(msg);
                                                YesNoController.yesNoController.YesNoPanelOpen();
                                                movableFlag = false;
                                            }

                                        }
                                    }
                                }
                                else if (player.nowFloor == 8)
                                {
                                    //地下8階
                                    if (eo.eventNumber == 1)
                                    {
                                        //ワープ地点のメッセージ

                                        //フラグが0の時のみイベントを実行する
                                        if (eo.eventFlag == 0)
                                        {
                                            //メッセージを表示する
                                            MessageController.msgController.MessageDisp("気が付くと全く知らない場所にいた。\n" +
                                                                            "これまでに感じたことのない\n" +
                                                                            "邪悪な気配が漂っている。\n" +
                                                                            "おそらく、ここに魔王がいるに違いない！\n");

                                            //フラグを1にする
                                            eo.eventFlag = 1;
                                        }

                                    }
                                    else if (eo.eventNumber >= 2 && eo.eventNumber <= 5)
                                    {
                                        //魔王の存在、仕掛け扉1の情報、仕掛け扉2の情報、悪魔の像の扉の情報のどれか
                                        MessageController.msgController.MessageDisp("壁に何か書いてある。\n調べてみますか？\n");
                                        YesNoController.yesNoController.YesNoPanelOpen();
                                        movableFlag = false;
                                    }
                                    else if (eo.eventNumber == 6)
                                    {
                                        //スイッチ1

                                        //フラグが0の時のみイベントを実行する
                                        if (eo.eventFlag == 0)
                                        {
                                            //スイッチ1の画像を表示する
                                            EventImageController.evImageController.ImageDisp("switch01");

                                            //メッセージとYesNoウィンドウを表示する
                                            MessageController.msgController.MessageDisp("壁にスイッチがある。\n押してみますか？\n");
                                            YesNoController.yesNoController.YesNoPanelOpen();
                                            movableFlag = false;
                                        }

                                    }
                                    else if (eo.eventNumber == 7)
                                    {
                                        //スイッチ2

                                        //フラグが0の時のみイベントを実行する
                                        if (eo.eventFlag == 0)
                                        {
                                            //スイッチ2の画像を表示する
                                            EventImageController.evImageController.ImageDisp("switch02");

                                            //メッセージとYesNoウィンドウを表示する
                                            MessageController.msgController.MessageDisp("壁にスイッチがある。\n押してみますか？\n");
                                            YesNoController.yesNoController.YesNoPanelOpen();
                                            movableFlag = false;
                                        }

                                    }
                                }
                            }
                        }
                        else
                        {
                            //遭遇型イベントが設定されていないときはランダムエンカウント処理を行う
                            fightEnemy = null;

                            //戦闘対象敵データの取得
                            Enemy tmp_enemy = new Enemy();

                            if (player.nowFloor == 1)
                            {
                                //地下1階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(1, 5, stepCount);
                            }
                            else if (player.nowFloor == 2)
                            {
                                //地下2階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(6, 10, stepCount);
                            }
                            else if (player.nowFloor == 3)
                            {
                                //地下3階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(11, 15, stepCount);
                            }
                            else if (player.nowFloor == 4)
                            {
                                //地下4階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(16, 20, stepCount);
                            }
                            else if (player.nowFloor == 5)
                            {
                                //地下5階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(21, 25, stepCount);
                            }
                            else if (player.nowFloor == 6)
                            {
                                //地下6階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(26, 30, stepCount);
                            }
                            else if (player.nowFloor == 7)
                            {
                                //地下7階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(31, 35, stepCount);
                            }
                            else if (player.nowFloor == 8)
                            {
                                //地下8階の時
                                tmp_enemy = enemyGenerator.RandomEncount2(36, 40, stepCount);
                            }

                            if (tmp_enemy != null)
                            {
                                //敵データがある時

                                //歩数を初期化
                                stepCount = 0;

                                //戦闘場面へ移行
                                fightEnemy = new Enemy();
                                fightEnemy.EnemyDataSet(tmp_enemy);
                                movableFlag = false;
                                fightFlag = true;
                                MoveToFightScene();
                            }

                            tmp_enemy = null;
                        }

                        //移動直後フラグをオフにする
                        moveInFlag = false;
                    }
                    else
                    {
                        //移動直後フラグをオフにする
                        moveInFlag = false;

                        //死亡時はメッセージを表示後、タイトル画面に戻る
                        PlayerDead();
                    }


                }
                else
                {

                    if (YesNoController.yesNoController.GetPanelOpenFlag() == true)
                    {
                        //YesNoウィンドウが開いているときの処理
                        if (saveFlag == true)
                        {
                            if (gameEndFlag == false)
                            {
                                //データセーブ時
                                if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                                {
                                    //「はい」を選択したときはセーブを実行する
                                    SaveWindowButtonInit(true);
                                    SaveLabelInit();
                                    MessageController.msgController.MessageDisp("セーブしました。\n");
                                    player.SetEventFlag(eventController.GetEventData());
                                    player.SetCityEventFlagArray(eventController.GetCityEventFlagArray());
                                    GameDataController.DataSave(player, saveArrayNum);
                                    SaveWindowDisp();
                                    YesNoController.yesNoController.YesNoPanelClose();
                                    commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.SAVE);
                                    saveClickFlag = false;
                                }
                                else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                                {
                                    //「いいえ」を選択したときはセーブをキャンセルする
                                    SaveWindowButtonInit(true);
                                    SaveLabelInit();
                                    MessageController.msgController.MessageDisp("何番にセーブしますか？\n");
                                    YesNoController.yesNoController.YesNoPanelClose();
                                    commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.SAVE);
                                    saveClickFlag = false;
                                }
                            }
                            else
                            {
                                //ゲーム終了選択時
                                if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                                {
                                    //「はい」を選択したときはプレイヤーの行動に関する状態を「タイトルへ戻る直前」に移行する
                                    YesNoController.yesNoController.YesNoPanelClose();
                                    actMode = ACTMODE.RETURN_TITLE;
                                }
                                else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                                {
                                    //「いいえ」を選択したときはセーブ対象ファイル選択へ戻る
                                    SaveWindowButtonInit(true);
                                    MessageController.msgController.MessageDisp("何番にセーブしますか？\n");
                                    YesNoController.yesNoController.YesNoPanelClose();
                                    commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.SAVE);
                                    gameEndFlag = false;
                                }
                            }
                        }
                        else
                        {
                            //データセーブおよびゲーム終了以外
                            YesNoEvent(eo);
                        }
                    }
                    else
                    {
                        //YesNoウィンドウが開いていないときの処理
                    }

                }

                if (fightFlag == true)
                {
                    //戦闘中は戦闘時の処理を実行する
                    FightProcess();
                }

                if (player.DeadCheck() == false)
                {
                    //プレイヤー生存時はプレイヤー操作処理を実行する
                    PlayerAction();
                }

                //階段表示フラグをオンにする
                stairsDispFlag = true;
            }
            else if (actMode == ACTMODE.TALKING)
            {
                //会話中
                if (npcFlag == true)
                {
                    //NPCがいるとき
                    if (player.nowFloor == 1)
                    {
                        //地下1階の時
                        if (eo.eventNumber == 1)
                        {
                            //魔法の地図の情報を持つ冒険者
                            switch (eventCount)
                            {
                                case 0:
                                    //メッセージを表示する
                                    MessageController.msgController.MessageDisp("この階には地図がある。\n必ず見つけろ。\nでは、気をつけてな。\n");

                                    //コマンドボタンを使用不可にする
                                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
                                    eventCount++;
                                    break;
                                default:
                                    //一定時間後にNPC退去処理を行う
                                    if (CommonMethod.TimeWait(5.0f) == true)
                                    {
                                        NPCMoveOut();

                                        //プレイヤーの行動に関する状態を停止中にする
                                        actMode = ACTMODE.STAYING;

                                        //カウンタの初期化
                                        eventCount = 0;
                                    }
                                    break;
                            }
                        }
                        else if (eo.eventNumber == 2)
                        {
                            //鍵売りの老人

                            //銀の鍵と魔法の地図の情報を取得する
                            Item item = new Item();
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.KEY1);
                            Item item2 = new Item();
                            item2 = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.MAP);

                            //鍵の価格を算出する
                            int key_price = item.buyPrice * 2;

                            //メッセージを表示する
                            string msg = "この先の扉の向こうに" + item2.itemName +
                                         "があるけど、\n鍵がかかってるよ。" + item.itemName +
                                         "はあるかい？\n良かったら売ってあげるよ。\n";
                            msg = msg + key_price + "ゴールドでどうだい？\n";
                            MessageController.msgController.MessageDisp(msg);

                            //YesNoウィンドウを表示する
                            YesNoController.yesNoController.YesNoPanelOpen();

                            //コマンドボタンを使用不可にする
                            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.YESNO);

                            //プレイヤーの行動に関する状態を停止中にする
                            actMode = ACTMODE.STAYING;
                        }
                    }
                    else if (player.nowFloor == 2)
                    {
                        //地下2階の時
                        if (eo.eventNumber == 1)
                        {
                            //アイテムオブジェクトの初期化
                            Item item = new Item();

                            //ミストリル鉱石の情報を持つ冒険者
                            switch (eventCount)
                            {
                                case 0:
                                    //ミストリル鉱石の情報を取得する
                                    item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.ORE);

                                    //メッセージを表示する
                                    string msg = "武器屋のオヤジが強い武器を作るのに必要な\n" +
                                                 item.itemName + "を欲しがってる。それで、\n" +
                                                "次の階にあるのが分かって取りに来たんだけど、\n" +
                                                "衛兵がいて通せんぼしやがる。どうしたもんかね。\n";
                                    MessageController.msgController.MessageDisp(msg);

                                    //コマンドボタンを使用不可にする
                                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
                                    eventCount++;
                                    break;
                                default:
                                    //一定時間後にNPC退去処理を行う
                                    if (CommonMethod.TimeWait(5.0f) == true)
                                    {
                                        NPCMoveOut();

                                        //プレイヤーの行動に関する状態を停止中にする
                                        actMode = ACTMODE.STAYING;

                                        //カウンタの初期化
                                        eventCount = 0;
                                    }
                                    break;
                            }
                        }
                    }
                    else if (player.nowFloor == 3)
                    {
                        //地下3階の時
                        if (eo.eventNumber == 1)
                        {
                            //衛兵

                            //賄賂の額を取得する
                            int bribe = EventController.GUARD_BRIBE;

                            //メッセージを表示する
                            string msg = "おっと、ここを通すわけにはいかねえ。\nどうしてもというなら、";
                            msg = msg + bribe + "ゴールドよこしな。\n";
                            MessageController.msgController.MessageDisp(msg);

                            //YesNoウィンドウを表示する
                            YesNoController.yesNoController.YesNoPanelOpen();

                            //コマンドボタンを使用不可にする
                            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.YESNO);

                            //プレイヤーの行動に関する状態を停止中にする
                            actMode = ACTMODE.STAYING;
                        }
                        else if (eo.eventNumber == 2)
                        {
                            //地下3階の冒険者
                            switch (eventCount)
                            {
                                case 0:
                                    MessageController.msgController.MessageDisp("迷宮の入り口で怪しい黒ずくめの男を\n" +
                                           "見かけたんだが、そいつが不気味な像を\n" +
                                           "天に掲げたとたん、一瞬で姿を消して\n" +
                                           "しまった。何だったんだろう・・・。\n");

                                    //コマンドボタンを使用不可にする
                                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
                                    eventCount++;
                                    break;
                                default:
                                    //一定時間後にNPC退去処理を行う
                                    if (CommonMethod.TimeWait(5.0f) == true)
                                    {
                                        NPCMoveOut();

                                        //プレイヤーの行動に関する状態を停止中にする
                                        actMode = ACTMODE.STAYING;

                                        //カウンタの初期化
                                        eventCount = 0;
                                    }
                                    break;
                            }
                        }
                    }
                    else if (player.nowFloor == 4)
                    {
                        //地下4階の時
                        if (eo.eventNumber == 1)
                        {
                            //雷の杖を持っている魔法使い

                            //聖なる薬草の情報を取得する
                            Item item = new Item();
                            item = itemGenerator.GetItemInfo(28);

                            //メッセージを表示する
                            string msg = "お若いの、ちょっとケガをしてしまっての、\n";
                            msg = msg + item.itemName + "をひとついただけんかのう？\n";
                            msg = msg + "十分な礼はさせていただくでの。\n";
                            MessageController.msgController.MessageDisp(msg);

                            //YesNoウィンドウを表示する
                            YesNoController.yesNoController.YesNoPanelOpen();

                            //コマンドボタンを使用不可にする
                            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.YESNO);

                            //プレイヤーの行動に関する状態を停止中にする
                            actMode = ACTMODE.STAYING;
                        }
                    }
                    else if (player.nowFloor == 5)
                    {
                        //地下5階の時
                        if (eo.eventNumber == 1)
                        {
                            //謎の人物
                            switch (eventCount)
                            {
                                case 0:
                                    //メッセージを表示する
                                    if (player.playerKarma >= 5)
                                    {
                                        //カルマが5以上の時

                                        MessageController.msgController.MessageDisp("あなたのような、悪人に用はありません。\n" +
                                                                        "直ちに立ち去りなさい。\n");

                                    }
                                    else
                                    {
                                        //カルマが4以下の時

                                        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) == 0)
                                        {
                                            //姫を助けていないとき

                                            //解呪の手鏡の情報を取得する
                                            Item item = new Item();
                                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.MIRROR);

                                            //メッセージを表示する
                                            string msg = "姫は呪いで石にされています。\n" +
                                                  "呪いを解くには" + item.itemName + "が必要です。\n" +
                                                  "姫を助けたら、もう一度ここへ来てください。\n";
                                            MessageController.msgController.MessageDisp(msg);
                                        }
                                        else
                                        {
                                            //姫を助けているとき

                                            //勇者の紋章の情報を取得
                                            Item item = new Item();
                                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.KEY3);

                                            Player.PICK_COMPLETION pick = player.ItemPick(item);
                                            if (pick == Player.PICK_COMPLETION.OK)
                                            {
                                                //持ち物に空きがあるときは勇者の紋章を入手してメッセージを表示する
                                                string msg = "あなたこそ、真の勇者です。\n" +
                                                             "この紋章を持って、勇者の装備の封印を解き、\n" +
                                                             "この迷宮の魔王を倒すのです。\n";
                                                MessageController.msgController.MessageDisp(msg);

                                                //イベントフラグを1にする
                                                eo.eventFlag = 1;
                                            }
                                            else
                                            {
                                                //持ち物が一杯の時はメッセージを表示する
                                                string msg = "あなたこそ、真の勇者です。渡したい物が\n" +
                                                             "あるのですが、持ち物が一杯のようですね。\n" +
                                                             "持ち物を整理してから、もう一度来てください。\n";
                                                MessageController.msgController.MessageDisp(msg);
                                            }

                                        }

                                    }

                                    //コマンドボタンを使用不可にする
                                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
                                    eventCount++;
                                    break;
                                default:
                                    //一定時間後にNPC退去処理を行う
                                    if (CommonMethod.TimeWait(5.0f) == true)
                                    {
                                        NPCMoveOut();

                                        //プレイヤーの行動に関する状態を停止中にする
                                        actMode = ACTMODE.STAYING;

                                        //カウンタの初期化
                                        eventCount = 0;
                                    }
                                    break;
                            }
                        }
                    }
                    else if (player.nowFloor == 7)
                    {
                        //地下7階の時
                        if (eo.eventNumber == 1)
                        {
                            //死神
                            if (player.playerKarma >= 7)
                            {
                                //カルマが7以上の時
                                switch (eventCount)
                                {
                                    case 0:
                                        //メッセージを表示する
                                        MessageController.msgController.MessageDisp("貴様の心からは深い闇を感じる。\n" +
                                                                        "いいだろう、通してやる。\n" +
                                                                        "この先にある像を手にし、\n" +
                                                                        "我が王の元へ行くがよい。\n");

                                        //コマンドボタンを使用不可にする
                                        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                                        //フラグを1にする
                                        eo.eventFlag = 1;
                                        eventCount++;
                                        break;
                                    default:
                                        //一定時間後にNPC退去処理を行う
                                        if (CommonMethod.TimeWait(5.0f) == true)
                                        {
                                            NPCMoveOut();

                                            //プレイヤーの行動に関する状態を「停止中」にする
                                            actMode = ACTMODE.STAYING;

                                            //カウンタの初期化
                                            eventCount = 0;
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                //カルマが6以下の時

                                //メッセージを表示する
                                MessageController.msgController.MessageDisp("ここから先は通さん、死にたくなければ引き返せ。\n");

                                //YesNoウィンドウを表示する
                                YesNoController.yesNoController.YesNoPanelOpen();

                                //コマンドボタンを使用不可にする
                                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.YESNO);

                                //プレイヤーの行動に関する状態を「停止中」にする
                                actMode = ACTMODE.STAYING;
                            }

                        }
                        else if (eo.eventNumber == 6 || eo.eventNumber == 7)
                        {
                            //姫（偽者）

                            //メッセージを表示する
                            MessageController.msgController.MessageDisp("かかったわね！\nここで死になさい！\n");

                            //コマンドボタンを使用不可にする
                            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.YESNO);

                            //プレイヤーの行動に関する状態を「停止中」にする
                            actMode = ACTMODE.STAYING;

                            //敵の情報をサキュバスの物に変更する
                            movableFlag = false;
                            fightFlag = true;
                            npcFlag = false;
                            fightEnemy = null;
                            fightEnemy = new Enemy();
                            fightEnemy.EnemyDataSet(enemyGenerator.GetEnemy((int)EnemyGenerator.F_ENEMY_TALK.SUCCUBUS));

                            //少ししてから戦闘場面へ移行する
                            Invoke("MoveToFightScene", 3.0f);
                        }
                        else if (eo.eventNumber == 8)
                        {
                            //姫
                            switch (eventCount)
                            {
                                case 0:
                                    //メッセージを表示する
                                    MessageController.msgController.MessageDisp("助けていただき、ありがとうございます。\n");

                                    //イベントフラグを1にする
                                    eo.eventFlag = 1;

                                    //街のイベントフラグ（姫）を1（姫救出）に変更する
                                    eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS, 1);

                                    //コマンドボタンを使用不可にする
                                    commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
                                    eventCount++;
                                    break;
                                case 1:
                                    //「話す」ボタンを押したときの効果音が終了した後、姫救出時のジングルを再生する
                                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                                    {
                                        SoundManager.soundManager.PlayBGM("bgm_princess_jingle", 0.5f, false);
                                        eventCount++;
                                    }
                                    break;
                                case 2:
                                    //ジングル終了後に次のメッセージを表示する
                                    if (SoundManager.soundManager.BGMPlayingCheck() == false)
                                    {
                                        MessageController.msgController.MessageJoinDisp("助けていただいたお礼がしたいので、\n" +
                                                                                        "一足先に帰ってお城でお待ちしております。\n" +
                                                                                        "どうか、ご無事で。\n");
                                        eventCount++;
                                    }
                                    break;
                                default:
                                    //一定時間後にNPC退去処理を行う
                                    if (CommonMethod.TimeWait(5.0f) == true)
                                    {
                                        NPCMoveOut();

                                        //BGMを再生する
                                        SoundManager.soundManager.PlayBGM(dungeonBGMArray[player.nowFloor - 1], 0.5f, true);

                                        //プレイヤーの行動に関する状態を「停止中」にする
                                        actMode = ACTMODE.STAYING;

                                        //カウンタの初期化
                                        eventCount = 0;
                                    }
                                    break;
                            }
                        }
                    }
                    else if (player.nowFloor == 8)
                    {
                        //魔王

                        //メッセージを表示する
                        MessageController.msgController.MessageDisp("私はガイウス、この地下世界の魔王だ。\n" +
                                                        "お前のような強者を私は待っておった。\n" +
                                                        "もし私の部下になるなら、お前に地上を\n" +
                                                        "治めさせてやる。どうだ、悪い話であるまい？\n");

                        //YesNoウィンドウを表示する
                        YesNoController.yesNoController.YesNoPanelOpen();

                        //コマンドボタンを使用不可にする
                        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.YESNO);

                        //プレイヤーの行動に関する状態を「停止中」にする
                        actMode = ACTMODE.STAYING;

                    }
                }
                else
                {
                    //NPCがいないとき
                    MessageController.msgController.MessageDisp("誰もいない。\n");

                    //プレイヤーの行動に関する状態を「停止中」にする
                    actMode = ACTMODE.STAYING;
                }
            }
            else if (actMode == ACTMODE.MOVING)
            {
                //移動中の処理
                PlayerMoving();
            }
            else if (actMode == ACTMODE.C_DIRECTION)
            {
                //方向転換中の処理
                PlayerDirectionChanging();
            }
            else if (actMode == ACTMODE.CRASH_WALL)
            {
                //壁に衝突時の処理
                switch (eventCount)
                {
                    case 0:
                        //壁に衝突したときの効果音を再生する
                        SoundManager.soundManager.PlaySE("se_crash_wall");
                        //メッセージを表示
                        MessageController.msgController.MessageDisp("いてっ！\n");

                        eventCount++;
                        break;
                    default:
                        //壁に衝突時の終了処理

                        //効果音の再生が終了したときに処理を開始する
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            //カウンタの初期化
                            eventCount = 0;
                            //プレイヤーの行動に関する状態を「停止中」にする
                            actMode = ACTMODE.STAYING;
                        }
                        break;
                }

            }
            else if (actMode == ACTMODE.ROT_FLOOR)
            {
                //回転床作動中の処理
                float angle = eventController.FloorRotating();
                transform.rotation = Quaternion.Euler(0, angle, 0);
                if (eventController.FloorRotateCheck() == false)
                {
                    //回転終了時にプレイヤーの行動に関する状態を「停止中」に戻す
                    actMode = ACTMODE.STAYING;
                }
            }
            else if (actMode == ACTMODE.ENCOUNT_TRAP)
            {
                //罠遭遇時（回転床以外）の処理
                switch (eventCount)
                {
                    case 0:
                        //遭遇した罠のメッセージを表示
                        if (eo.eventNumber == (int)EventController.TRAPTYPE.PIT_FALL)
                        {
                            //落とし穴
                            MessageController.msgController.MessageDisp("落とし穴だ！！\n");
                        }
                        else if (eo.eventNumber == (int)EventController.TRAPTYPE.POISON_ARROW)
                        {
                            //毒矢
                            MessageController.msgController.MessageDisp("毒矢の罠だ！！\n");
                        }

                        eventCount++;
                        break;
                    case 1:
                        //遭遇した罠にかかったかどうかで処理を分岐する

                        //罠のダメージの変数の初期化
                        int tr_damage = 0;

                        if (eo.eventNumber == (int)EventController.TRAPTYPE.PIT_FALL)
                        {
                            //落とし穴

                            //落とし穴のダメージを取得
                            tr_damage = eventController.GetTrapDamage(EventController.TRAPTYPE.PIT_FALL);

                            if (eventController.TrapAvoidCheck(EventController.TRAPTYPE.PIT_FALL, player.playerSpeed,
                                                                player.playerLuck) == false)
                            {
                                //落ちた時

                                //画面を振動させる
                                cameraController.DungeonCameraShake();
                                //罠にかかった時のエフェクト（敵の攻撃が命中した時の物を流用）
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.HIT);

                                //罠にかかった時の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_attack_hit");
                                //プレイヤーのHPを減らし、メッセージを表示する
                                player.DeclineHp(tr_damage);
                                MessageController.msgController.MessageJoinDisp(player.playerName + "は" + tr_damage + "のダメージ！！\n");
                            }
                            else
                            {
                                //回避したとき

                                //罠をかわした時の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_attack_avoid");

                                MessageController.msgController.MessageJoinDisp(player.playerName + "は罠をかわした！！\n");
                            }

                        }
                        else if (eo.eventNumber == (int)EventController.TRAPTYPE.POISON_ARROW)
                        {
                            //毒矢

                            //毒矢のダメージを取得
                            tr_damage = eventController.GetTrapDamage(EventController.TRAPTYPE.POISON_ARROW);

                            if (eventController.TrapAvoidCheck(EventController.TRAPTYPE.POISON_ARROW, player.playerSpeed,
                                    player.playerLuck) == false)
                            {
                                //命中したとき

                                //画面を振動させる
                                cameraController.DungeonCameraShake();
                                //罠にかかった時のエフェクト（敵の攻撃が命中した時の物を流用）
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.HIT);

                                //罠にかかった時の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_attack_hit");
                                //プレイヤーのHPを減らし、メッセージを表示する
                                player.DeclineHp(tr_damage);
                                MessageController.msgController.MessageJoinDisp(player.playerName + "は" + tr_damage + "のダメージ！！\n");

                                if (player.playerCondition != Player.PLAYER_CONDITION.POISON)
                                {
                                    //毒状態でないときは状態を毒に変え、メッセージを表示する
                                    player.ChangeCondition(Player.PLAYER_CONDITION.POISON);
                                    MessageController.msgController.MessageJoinDisp(player.playerName + "は毒に侵された\n");
                                }
                            }
                            else
                            {
                                //回避したとき

                                //罠をかわした時の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_attack_avoid");

                                MessageController.msgController.MessageJoinDisp(player.playerName + "は罠をかわした！！\n");
                            }

                        }

                        eventCount++;
                        break;
                    case 2:
                        //プレイヤーの死亡チェックを行う

                        //効果音の再生が終了したときに処理を開始する
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            if (player.DeadCheck() == true)
                            {
                                //罠で死亡したとき
                                eventCount++;
                            }
                            else
                            {
                                //罠で死亡しなかったとき
                                eventCount = 99;
                            }
                        }

                        break;
                    case 3:
                        //罠遭遇時（回転床以外）の終了処理
                        //※罠で死亡した時

                        //一定時間待機の後、プレイヤー死亡時の処理を行う
                        if (CommonMethod.TimeWait(3.0f) == true)
                        {
                            //カウンタの初期化
                            eventCount = 0;

                            //プレイヤー死亡時の処理
                            PlayerDead();
                        }
                        break;
                    default:
                        //罠遭遇時（回転床以外）の終了処理
                        //※罠で死亡しなかった時

                        //カウンタの初期化
                        eventCount = 0;
                        //プレイヤーの行動に関する状態を「停止中」にする
                        actMode = ACTMODE.STAYING;

                        break;
                }

            }
            else if (actMode == ACTMODE.USING_ITEM)
            {
                //アイテム使用中（戦闘時以外）の時
                switch (eventCount)
                {
                    case 0:
                        //メッセージの初期化
                        dungeonMessage = "";

                        //装備変更前に装備していたアイテムの所持アイテムリスト番号の初期化
                        preEquipBoxNumber = -1;
                        //対象アイテムのID取得
                        useItemId = player.GetItemBoxIndex(itemBoxNum).itemId;
                        //装備アイテム種類番号の初期化
                        equipNumber = -1;

                        //対象アイテムのデータ取得
                        useItem = new Item();
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            if (useItemId == itemList[i].itemId)
                            {
                                useItem = itemList[i];
                                break;
                            }
                        }

                        //帰還フラグの初期化
                        returnFlag = false;

                        eventCount++;
                        break;
                    case 1:
                        //アイテム使用のメッセージを表示する
                        //（装備アイテムの場合は装備処理も行い、地図の場合は表示処理を行う）

                        //アイテム使用メッセージの共通部分を格納
                        dungeonMessage = player.playerName + "は";

                        if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE)
                        {
                            //一般使い捨てアイテムの時
                            dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\n";

                            eventCount++;
                        }
                        else if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL)
                        {
                            //一般アイテムの時
                            dungeonMessage = "今使用する必要はない。\n";

                            //カウンタを終了処理へと進める
                            eventCount = 99;
                        }
                        else if (useItem.itemType > ItemGenerator.ITEM_TYPE.EQUIP && useItem.itemType < ItemGenerator.ITEM_TYPE.EQUIP_END)
                        {
                            //装備アイテムの時
                            //装備変更前に装備していたアイテムの所持アイテムリスト番号の取得
                            preEquipBoxNumber = player.GetPreEquipNumber(useItem.itemType);
                            //これから装備する装備アイテム種類番号の取得
                            equipNumber = itemGenerator.GetEquipNumber(useItem.itemType);

                            if (preEquipBoxNumber == -1)
                            {
                                //何も装備されていない時
                                //選択した装備アイテムの装備処理を実行する
                                player.Equip(useItem, itemBoxNum);
                                dungeonMessage = dungeonMessage + useItem.itemName + "を装備した！\n";
                            }
                            else
                            {
                                if (itemBoxNum == preEquipBoxNumber)
                                {
                                    //現在装備中のアイテムが選択された時
                                    //装備解除の処理を実行する
                                    player.UnEquip(useItem, itemBoxNum);
                                    dungeonMessage = dungeonMessage + useItem.itemName + "を装備から外した！\n";
                                }
                                else
                                {
                                    //既に別の装備アイテムが装備されていた時
                                    //前の装備アイテムの装備を解除する
                                    Item unEquipItem = itemGenerator.GetItemInfo(player.GetItemBoxIndex(preEquipBoxNumber).itemId);
                                    player.UnEquip(unEquipItem, preEquipBoxNumber);
                                    //選択した装備アイテムの装備処理を実行する
                                    player.Equip(useItem, itemBoxNum);
                                    dungeonMessage = dungeonMessage + useItem.itemName + "を装備した！\n";
                                }
                            }
                            //カウンタを終了処理へと進める
                            eventCount = 99;
                        }
                        else if (useItem.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE ||
                                 useItem.itemType == ItemGenerator.ITEM_TYPE.EVENT)
                        {
                            //イベントアイテムの時
                            if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.RETURN &&
                                npcFlag == true)
                            {
                                //NPC遭遇時で帰還アイテム（扉の絵）を使用した時
                                dungeonMessage = "今使用する必要はない。\n";

                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            else if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.RETURN &&
                                     HpCheck(player.playerHp - useItem.hpCost) <= 0)
                            {
                                //現在HPが消費HP以下の時に帰還アイテム（扉の絵）を使用した時
                                dungeonMessage = dungeonMessage + useItem.itemName + "を使おうとしたが\n" + "HPが足りなかった！\n";
                            }
                            else
                            {
                                //上の条件以外の時
                                dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\n";

                                eventCount++;
                            }
                        }
                        else if (useItem.itemType == ItemGenerator.ITEM_TYPE.MAP)
                        {
                            //地図の時
                            if (npcFlag == false)
                            {
                                //通常時

                                //ダークゾーンチェック
                                if (DarkZoneCheck(eo) == true)
                                {
                                    //ダークゾーンが有効の時

                                    //地図が使用できないというメッセージを作成する
                                    dungeonMessage = "闇の魔力のせいで" + useItem.itemName + "が使用できない！\n";
                                }
                                else
                                {
                                    //ダークゾーンが無効または存在しない時

                                    //マップを表示する
                                    dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\n";
                                    cameraController.MapOpen(player.nowFloor, player.nowPosX, map_z_max - player.nowPosZ, player.nowDirection);
                                    Vector3 v = new Vector3(transform.position.x, S_MAP_COM_POS_Y, transform.position.z);
                                    cameraController.SmallMapCameraPositionSet(v.x, v.y, v.z);
                                    //コマンドボタンを使用不可にする
                                    commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.WAIT);
                                    //移動不可にする
                                    movableFlag = false;
                                    //ダークゾーン用のイメージオブジェクトを非表示にする（マップを閉じるボタンが押せなくなるため）
                                    darkZoneImage.SetActive(false);
                                    //画面全体用エフェクトパネルを非表示にする（マップを閉じるボタンが押せなくなるため）
                                    LargeEffectController.largeEffectController.LargeEffectPanelClose();
                                }
                            }
                            else
                            {
                                //NPC遭遇時
                                dungeonMessage = "今使用する必要はない。\n";
                            }
                            //カウンタを終了処理へと進める
                            eventCount = 99;
                        }
                        else if (useItem.itemType == ItemGenerator.ITEM_TYPE.MINI_MAP)
                        {
                            //小型の地図

                            //現在位置を取得
                            int pos_x = player.nowPosX;
                            int pos_z = map_z_max - player.nowPosZ;
                            
                            //現在向いている方角を取得
                            string str_dir = "";
                            switch (player.nowDirection)
                            {
                                case Player.DIRECTION.NORTH:
                                    str_dir = "北";
                                    break;
                                case Player.DIRECTION.EAST:
                                    str_dir = "東";
                                    break;
                                case Player.DIRECTION.SOUTH:
                                    str_dir = "南";
                                    break;
                                case Player.DIRECTION.WEST:
                                    str_dir = "西";
                                    break;
                            }

                            //現在フロアの取得
                            string str_floor;
                            if (player.nowFloor == GlobalConst.FLOOR_MAX)
                            {
                                //最終フロア
                                str_floor = "？？？？";
                            }
                            else
                            {
                                //その他
                                str_floor = "地下" + player.nowFloor.ToString() + "階";
                            }

                            //メッセージを作成
                            dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\n";
                            dungeonMessage = dungeonMessage + "現在、あなたは" + str_floor + "の北に" + pos_z.ToString() +
                                             "、東に" + pos_x.ToString() + "の地点で\n" + str_dir + "を向いている。";

                            //カウンタを終了処理へと進める
                            eventCount = 99;
                        }
                        else if (useItem.itemType == ItemGenerator.ITEM_TYPE.LANTHANUM ||
                                 useItem.itemType == ItemGenerator.ITEM_TYPE.TRUTH_BANGLE)
                        {
                            //ランタン、真実の腕輪のどれかの時
                            dungeonMessage = useItem.itemName + "は持っているだけで\n効果を発揮する。\n";
                            //カウンタを終了処理へと進める
                            eventCount = 99;
                        }
                        else if (useItem.itemType == ItemGenerator.ITEM_TYPE.MITHRIL_ORE)
                        {
                            //ミストリル鉱石
                            dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\nしかし、何も起こらなかった。\n";
                            //カウンタを終了処理へと進める
                            eventCount = 99;
                        }
                        else
                        {
                            //その他のアイテム
                            dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\nしかし、何も起こらなかった。\n";
                            //カウンタを終了処理へと進める
                            eventCount = 99;
                        }

                        //アイテム使用のメッセージを表示する
                        MessageController.msgController.MessageDisp(dungeonMessage);
                        break;
                    case 2:
                        //効果音の再生が終了したときに処理を開始する
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE)
                            {
                                //一般使い捨てアイテムの時
                                if (useItem.itemRecover > 0)
                                {
                                    //HP回復アイテムの時

                                    //一定時間後にカウンタを次の処理へと進める
                                    if (CommonMethod.TimeWait(0.5f) == true)
                                    {
                                        eventCount++;
                                    }
                                        
                                }

                                if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.SEALED)
                                {
                                    //封印状態回復アイテムの時

                                    //一定時間後にカウンタを次の処理へと進める
                                    if (CommonMethod.TimeWait(0.5f) == true)
                                    {
                                        eventCount++;
                                    }
                                }

                                if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.POISON)
                                {
                                    //毒状態回復アイテムの時

                                    //一定時間後にカウンタを次の処理へと進める
                                    if (CommonMethod.TimeWait(0.5f) == true)
                                    {
                                        eventCount++;
                                    }
                                }
                                
                            }
                            else if (useItem.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE ||
                                     useItem.itemType == ItemGenerator.ITEM_TYPE.EVENT)
                            {
                                //イベントアイテムの時
                                if (npcFlag == false)
                                {
                                    //NPC遭遇時でない時

                                    //プレイヤーの一歩前の座標を取得（鍵アイテムを使用する時のため）
                                    doorPosX = player.nowPosX;
                                    doorPosZ = player.nowPosZ;

                                    switch (player.nowDirection)
                                    {
                                        case Player.DIRECTION.NORTH:
                                            //北
                                            doorPosZ--;
                                            break;
                                        case Player.DIRECTION.EAST:
                                            //東
                                            doorPosX++;
                                            break;
                                        case Player.DIRECTION.SOUTH:
                                            //南
                                            doorPosZ++;
                                            break;
                                        case Player.DIRECTION.WEST:
                                            //西
                                            doorPosX--;
                                            break;
                                    }

                                    if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY1 ||
                                        useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY2)
                                    {
                                        //鍵1（銀の鍵）または鍵2（金の鍵）
                                        if (DoorKeyMatchCheck(player.nowPosX, player.nowPosZ, useItem.itemId) == 0)
                                        {
                                            //鍵が合った時

                                            //開錠の効果音を再生する
                                            SoundManager.soundManager.PlaySE("se_unlock");

                                            //開錠処理を行う
                                            MessageController.msgController.MessageJoinDisp("扉の鍵が開いた！\n");
                                            eventController.DoorKeyUnlock(player.nowFloor, doorPosX, doorPosZ);

                                            if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY1)
                                            {
                                                //鍵1（銀の鍵）の時

                                                //アイテム数を1減らす
                                                player.BoxItemSubtraction(itemBoxNum);
                                            }
                                        }
                                        else if (DoorKeyMatchCheck(player.nowPosX, player.nowPosZ, useItem.itemId) == 1)
                                        {
                                            //開錠済みもしくは鍵付き扉がない時
                                            MessageController.msgController.MessageJoinDisp("しかし、何も起こらなかった。\n");
                                        }
                                        else
                                        {
                                            //鍵が合わなかった時
                                            MessageController.msgController.MessageJoinDisp("しかし、鍵が合わなかった。\n");
                                        }

                                        //カウンタを終了処理へと進める
                                        eventCount = 99;
                                    }
                                    else if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY3 ||
                                             useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY4)
                                    {
                                        //鍵3（勇者の紋章）または鍵4（悪魔の像）
                                        if (DoorKeyMatchCheck(player.nowPosX, player.nowPosZ, useItem.itemId) == 0)
                                        {
                                            //鍵が合った時

                                            //封印解除の効果音を再生する
                                            SoundManager.soundManager.PlaySE("se_unsealing");

                                            //開錠処理を行う
                                            MessageController.msgController.MessageJoinDisp("扉の封印が解けた！\n");
                                            eventController.DoorKeyUnlock(player.nowFloor, doorPosX, doorPosZ);
                                        }
                                        else
                                        {
                                            //鍵が合った時以外
                                            MessageController.msgController.MessageJoinDisp("しかし、何も起こらなかった。\n");
                                        }

                                        //カウンタを終了処理へと進める
                                        eventCount = 99;
                                    }
                                    else if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.RETURN)
                                    {
                                        //帰還アイテム（扉の絵）

                                        //表示メッセージの作成を行う
                                        dungeonMessage = player.playerName + "の体は" + useItem.itemName + "に\n吸い込まれていく！\n";

                                        //プレイヤーのHPから消費HPを引く
                                        player.DeclineHp(useItem.hpCost);

                                        //帰還フラグをオンにする
                                        returnFlag = true;

                                        //メッセージを表示する
                                        MessageController.msgController.MessageJoinDisp(dungeonMessage);

                                        //カウンタを終了処理へと進める
                                        eventCount = 99;
                                    }
                                    else
                                    {
                                        //その他のイベントアイテムの時
                                        MessageController.msgController.MessageJoinDisp("しかし、何も起こらなかった！\n");
                                        //カウンタを終了処理へと進める
                                        eventCount = 99;
                                    }
                                }
                                else
                                {
                                    //NPC遭遇時の時
                                    MessageController.msgController.MessageJoinDisp("しかし、何も起こらなかった！\n");
                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                            }
                        }
                        break;
                    case 3:
                        //効果音の再生が終了したときに処理を開始する（一般使い捨てアイテムの時のみ）
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE)
                            {
                                //一般使い捨てアイテムの時
                                if (useItem.itemRecover > 0)
                                {
                                    //HP回復アイテムの時

                                    //回復の効果音とエフェクトを再生する
                                    LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.PLAYER_HEAL);
                                    SoundManager.soundManager.PlaySE("se_heal");

                                    //HP回復処理の実行
                                    player.RecoverHp(useItem.itemRecover);
                                    MessageController.msgController.MessageJoinDisp(player.playerName + "の体力が回復した！\n");

                                    //女神の祝福を使用したときは状態を祝福にする
                                    if (useItem.itemRecover >= Item.FULL_HEAL)
                                    {
                                        player.ChangeCondition(Player.PLAYER_CONDITION.BLESSING);
                                        MessageController.msgController.MessageJoinDisp("そして、" + player.playerName + "は祝福を受けた！\n");
                                    }

                                    //アイテム数を1減らす
                                    player.BoxItemSubtraction(itemBoxNum);
                                }

                                if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.SEALED)
                                {
                                    //封印状態回復アイテムの時
                                    if (player.playerCondition == Player.PLAYER_CONDITION.SEALED)
                                    {
                                        //プレイヤーが封印状態の時

                                        //回復の効果音とエフェクトを再生する
                                        LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.PLAYER_HEAL);
                                        SoundManager.soundManager.PlaySE("se_heal");

                                        //プレイヤーの状態をOKにする
                                        player.ChangeCondition(Player.PLAYER_CONDITION.OK);
                                        MessageController.msgController.MessageJoinDisp(player.playerName + "の封印が解けた！\n");
                                    }
                                    else
                                    {
                                        //プレイヤーが封印状態でないとき
                                        MessageController.msgController.MessageJoinDisp("しかし、何も起こらなかった！\n");
                                    }

                                    //アイテム数を1減らす
                                    player.BoxItemSubtraction(itemBoxNum);
                                }

                                if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.POISON)
                                {
                                    //毒状態回復アイテムの時
                                    if (player.playerCondition == Player.PLAYER_CONDITION.POISON)
                                    {
                                        //プレイヤーが毒状態の時

                                        //回復の効果音とエフェクトを再生する
                                        LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.PLAYER_HEAL);
                                        SoundManager.soundManager.PlaySE("se_heal");

                                        //プレイヤーの状態をOKにする
                                        player.ChangeCondition(Player.PLAYER_CONDITION.OK);
                                        MessageController.msgController.MessageJoinDisp(player.playerName + "の毒が消えた！\n");
                                    }
                                    else
                                    {
                                        //プレイヤーが毒状態でないとき
                                        MessageController.msgController.MessageJoinDisp("しかし、何も起こらなかった！\n");
                                    }

                                    //アイテム数を1減らす
                                    player.BoxItemSubtraction(itemBoxNum);
                                }

                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            
                        }
                        break;
                    default:
                        //戦闘時以外のアイテム使用の終了処理

                        //効果音の再生が終了したときに処理を開始する
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            //帰還フラグがオンの時
                            if (returnFlag == true)
                            {
                                //プレイヤーの行動に関する状態を「ダンジョンを出る直前（ワープ）」に移行する
                                actMode = ACTMODE.DUNGEON_WARP_OUT;
                            }
                            else
                            {
                                //プレイヤーの行動に関する状態を「停止中」にする
                                actMode = ACTMODE.STAYING;

                                if (cameraController.MapOpenCheck() == false)
                                {
                                    //地図が開いていないとき、コマンドボタンを使用可能にする
                                    if (npcFlag == false)
                                    {
                                        //NPC遭遇時でないときは、コマンドボタンを「通常」の状態で使用可能にする
                                        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.NORMAL);
                                    }
                                    else
                                    {
                                        //NPC遭遇時は、コマンドボタンを「会話中」の状態で使用可能にする
                                        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.TALK);
                                    }
                                }
                            }
                            //カウンタの初期化
                            eventCount = 0;
                        }
                        break;
                }

            }
            else if (actMode == ACTMODE.SWITCH_ON)
            {
                //スイッチを押した時
                switch (eventCount)
                {
                    case 0:
                        //効果音の再生が終了したときに処理を開始する
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            //スイッチを押したときの効果音を再生する
                            SoundManager.soundManager.PlaySE("se_switch");

                            eventCount++;
                        }
                        break;
                    default:
                        //効果音の再生が終了したときに処理を開始する
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            //スイッチの画像を消して、プレイヤーを移動可能にする
                            NPCMoveOut();

                            //スイッチを押したときのメッセージを表示する
                            MessageController.msgController.MessageDisp("仕掛けが動き、何かが外れる音がした。\n");

                            //カウンタの初期化
                            eventCount = 0;

                            //プレイヤーの行動に関する状態を「停止中」にする
                            actMode = ACTMODE.STAYING;

                        }
                        break;
                }
            }
            else if (actMode == ACTMODE.LOCKED_DOOR)
            {
                //扉が開かないとき
                switch (eventCount)
                {
                    case 0:
                        //扉が開かないときのメッセージを表示し、効果音を再生する
                        KeyDoorMessageDisp(player.nowDirection);

                        eventCount++;
                        break;
                    default:
                        //効果音の再生が終了したときに処理を開始する
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            //カウンタの初期化
                            eventCount = 0;

                            //プレイヤーの行動に関する状態を「停止中」にする
                            actMode = ACTMODE.STAYING;
                        }
                        break;
                }
            }
            else if (actMode == ACTMODE.DOOR_OPEN)
            {
                //プレイヤーの移動に使用する3次元ベクトル構造体
                Vector3 v;

                //扉を開けた時
                switch (eventCount)
                {
                    case 0:
                        //扉を開けたメッセージを表示する、効果音を再生する
                        MessageController.msgController.MessageDisp("扉を蹴り開け、躍り込んだ！\n");

                        //扉を開けた時の効果音を再生する
                        SoundManager.soundManager.PlaySE("se_open_door");

                        eventCount++;
                        break;
                    case 1:
                        //一定時間後に処理を開始する
                        if (CommonMethod.TimeWait(1.0f) == true)
                        {
                            //扉の先へ移動する
                            switch (player.nowDirection)
                            {
                                case Player.DIRECTION.NORTH:    //北
                                    //現在のプレイヤーのz座標から2を引く
                                    player.nowPosZ -= 2;
                                    //プレイヤーの移動
                                    v = new Vector3(player.nowPosX, 0, map_z_max - player.nowPosZ);
                                    transform.position = v;
                                    //プレイヤーの移動に合わせて画面左上の小マップを更新する
                                    v.y = S_MAP_COM_POS_Y;
                                    cameraController.SmallMapCameraPositionSet(v.x, v.y, v.z);
                                    break;
                                case Player.DIRECTION.EAST:     //東
                                    //現在のプレイヤーのx座標に2を足す
                                    player.nowPosX += 2;
                                    //プレイヤーの移動
                                    v = new Vector3(player.nowPosX, 0, map_z_max - player.nowPosZ);
                                    transform.position = v;
                                    //プレイヤーの移動に合わせて画面左上の小マップを更新する
                                    v.y = S_MAP_COM_POS_Y;
                                    cameraController.SmallMapCameraPositionSet(v.x, v.y, v.z);
                                    break;
                                case Player.DIRECTION.SOUTH:    //南
                                    //現在のプレイヤーのz座標に2を足す
                                    player.nowPosZ += 2;
                                    v = new Vector3(player.nowPosX, 0, map_z_max - player.nowPosZ);
                                    transform.position = v;
                                    //プレイヤーの移動に合わせて画面左上の小マップを更新する
                                    v.y = S_MAP_COM_POS_Y;
                                    cameraController.SmallMapCameraPositionSet(v.x, v.y, v.z);
                                    break;
                                case Player.DIRECTION.WEST:     //西
                                    //現在のプレイヤーのx座標から2を引く
                                    player.nowPosX -= 2;
                                    v = new Vector3(player.nowPosX, 0, map_z_max - player.nowPosZ);
                                    transform.position = v;
                                    //プレイヤーの移動に合わせて画面左上の小マップを更新する
                                    v.y = S_MAP_COM_POS_Y;
                                    cameraController.SmallMapCameraPositionSet(v.x, v.y, v.z);
                                    break;
                            }

                            eventCount++;
                        }
                        break;
                    default:
                        //メッセージウィンドウを閉じる
                        MessageController.msgController.MessagePanelClose();

                        //移動直後フラグをオンにする
                        moveInFlag = true;

                        //カウンタの初期化
                        eventCount = 0;

                        //プレイヤーの行動に関する状態を「停止中」にする
                        actMode = ACTMODE.STAYING;
                        break;
                }
            }
            else
            {

            }

        }

    }

    //姫の石像イベントの処理を行う関数
    void PrincessStatueEvent()
    {
        //メッセージの基本部分を作成する
        string msg = "姫を象った石像がある。\n" + "呪いで石像に変えられたのだろうか？\n";

        //解呪の手鏡の情報を取得
        Item item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.MIRROR);

        //姫の石像の画像を表示する
        EventImageController.evImageController.ImageDisp("princess_stone");

        //解呪の手鏡の所持チェック
        if (player.ItemHaveCheck(item) == false)
        {
            //持っていないとき

            //メッセージの残りを連結する
            msg = msg + "しかし、今はどうすることもできない。\n";

            //メッセージを表示する
            MessageController.msgController.MessageDisp(msg);
            movableFlag = false;

            //少したってから画像とメッセージを消去する（NPC退去関数を流用する）
            Invoke("NPCMoveOut", 5.0f);
        }
        else
        {
            //持っているとき

            //メッセージの残りを連結する
            msg = msg + item.itemName + "を使用しますか？\n";

            //メッセージとYesNoウィンドウを表示する
            MessageController.msgController.MessageDisp(msg);
            YesNoController.yesNoController.YesNoPanelOpen();
            movableFlag = false;
        }
    }

    //「はい」と「いいえ」の選択があるイベントの処理を行う関数
    //引数
    //eo:対象のイベント情報
    void YesNoEvent(EventObject eo)
    {
        if (eo.evActivation == EventController.EVACTIVATION.ENCOUNTER || 
            eo.evActivation == EventController.EVACTIVATION.BOTH)
        {
            //遭遇型イベントもしくは両方型イベント
            if (eo.eventType == EventController.EVTYPE.UPSTAIRS)
            {
                //上り階段
                if (player.nowFloor == 1)
                {
                    //地下1階のとき
                    if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                    {
                        //「はい」を選択したときはプレイヤーの行動に関する状態を「ダンジョンを出る直前（階段）」に移行する
                        actMode = ACTMODE.DUNGEON_OUT;
                    }
                    else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                    {
                        //「いいえ」を選択したときはウィンドウを閉じて移動可能にする
                        YesNoController.yesNoController.YesNoPanelClose();
                        EventImageController.evImageController.ImagePanelClose();
                        MessageController.msgController.MessagePanelClose();
                        movableFlag = true;
                    }
                }
                else
                {
                    //地下2階以下のとき
                    if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                    {
                        //「はい」を選択したときはプレイヤーの行動に関する状態を「フロア変更中」に移行する
                        actMode = ACTMODE.FLOOR_CHANGING;
                        stairsFlag = true;
                    }
                    else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                    {
                        //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                        YesNoController.yesNoController.YesNoPanelClose();
                        EventImageController.evImageController.ImagePanelClose();
                        MessageController.msgController.MessagePanelClose();
                        movableFlag = true;
                    }
                }
            }
            else if (eo.eventType == EventController.EVTYPE.DOWNSTAIRS)
            {
                //下り階段
                if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                {
                    //「はい」を選択したときはプレイヤーの行動に関する状態を「フロア変更中」に移行する
                    actMode = ACTMODE.FLOOR_CHANGING;
                    stairsFlag = false;
                }
                else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                {
                    //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                    YesNoController.yesNoController.YesNoPanelClose();
                    EventImageController.evImageController.ImagePanelClose();
                    MessageController.msgController.MessagePanelClose();
                    movableFlag = true;
                }
            }
            else if (eo.eventType == EventController.EVTYPE.NPC)
            {
                //NPC
                if (player.nowFloor == 1)
                {
                    //地下1階の時
                    if (eo.eventNumber == 2)
                    {
                        //鍵売りの老人
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //YesNoウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();

                            //銀の鍵の情報を取得する
                            Item item = new Item();
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.KEY1);

                            if (player.playerGold < item.buyPrice * 2)
                            {
                                //お金が足りなくて銀の鍵を入手できなかったときはメッセージを表示する
                                MessageController.msgController.MessageDisp("お金が足りないようだね、\nお金を貯めてからもう一度来なよ。\n");
                            }
                            else
                            {
                                Player.PICK_COMPLETION pick = player.ItemPick(item);
                                if (pick == Player.PICK_COMPLETION.OK)
                                {
                                    //銀の鍵を入手したときは、ゴールドを減らした後、メッセージを表示する
                                    player.GoldLost(item.buyPrice * 2);
                                    MessageController.msgController.MessageDisp("毎度あり！\n");
                                    //イベントフラグを1（購入済み）にする
                                    eo.eventFlag = 1;
                                }
                                else
                                {
                                    //アイテム欄が満タンで銀の鍵を入手できなかったときはメッセージを表示する
                                    MessageController.msgController.MessageDisp("これ以上持てないようだね、\n持ち物を整理してからもう一度来なよ。\n");
                                }
                            }
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //YesNoウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();
                            MessageController.msgController.MessageDisp("気が変わったら、また来なよ。\n");
                        }
                    }
                }
                else if (player.nowFloor == 3)
                {
                    //地下3階の時
                    if (eo.eventNumber == 1)
                    {
                        //衛兵
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //YesNoウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();

                            //要求額を払えるかどうか、所持ゴールドのチェックを行う
                            if (player.playerGold >= EventController.GUARD_BRIBE)
                            {
                                //要求額以上ゴールドを持っているとき

                                //支払った分を所持ゴールドから引く
                                player.GoldLost(EventController.GUARD_BRIBE);
                                MessageController.msgController.MessageDisp("確かに受け取ったぜ、通りな！\n");

                                //イベントフラグを1にする
                                eo.eventFlag = 1;
                            }
                            else
                            {
                                //要求額以上ゴールドを持っていないとき
                                MessageController.msgController.MessageDisp("金が足りねえじゃねえか、失せな！\n");

                                //プレイヤーを東へ押し戻す
                                //現在のプレイヤーのx座標に1を足す
                                player.nowPosX++;
                                moveInFlag = true;
                                actMode = ACTMODE.MOVING;
                            }
                            
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //YesNoウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();
                            MessageController.msgController.MessageDisp("じゃあ通すわけにはいかねえ、失せな！\n");

                            //プレイヤーを東へ押し戻す
                            //現在のプレイヤーのx座標に1を足す
                            player.nowPosX++;
                            moveInFlag = true;
                            actMode = ACTMODE.MOVING;
                        }
                    }
                }
                else if (player.nowFloor == 4)
                {
                    //地下4階の時
                    if (eo.eventNumber == 1)
                    {
                        //雷の杖を持っている魔法使い
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();

                            //聖なる薬草の情報を取得する
                            Item item1 = new Item();
                            item1 = itemGenerator.GetItemInfo(28);

                            //聖なる薬草が格納されている所持アイテムリストのインデックス番号を取得する
                            int item_index = player.GetItemBoxSpecifyIndex(item1);

                            //聖なる薬草を持っているかのチェックを行う
                            if (item_index != -1)
                            {
                                //聖なる薬草を持っているとき

                                //雷の杖の情報を取得する
                                Item item2 = new Item();
                                item2 = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_FIGHT.WAND1);

                                //雷の杖の入手処理を行う
                                Player.PICK_COMPLETION pick = player.ItemPick(item2);

                                string msg = "礼にこの杖を渡そう。きっと役に立つぞ。\n" +
                                                       "これより先、魔物どもは更に強くなる。\n気を付けてな。";

                                if (pick == Player.PICK_COMPLETION.OK)
                                {
                                    //雷の杖を入手したとき（アイテム欄に空きがある場合）

                                    MessageController.msgController.MessageDisp(msg);

                                    //聖なる薬草を減らす
                                    player.BoxItemSubtraction(item_index);

                                    //イベントフラグを1（交換済み）にする
                                    eo.eventFlag = 1;
                                }
                                else if (pick == Player.PICK_COMPLETION.NG_BOX_MAX && 
                                         player.GetItemBoxIndex(item_index).itemCount == 1)
                                {
                                    //雷の杖を入手したとき（アイテム欄に空きはないが、聖なる薬草が1個だけの時）

                                    MessageController.msgController.MessageDisp(msg);

                                    //聖なる薬草をアイテム欄より削除する
                                    player.BoxItemSubtraction(item_index);
                                    //再度雷の杖の入手処理を行う
                                    player.ItemPick(item2);

                                    //イベントフラグを1（交換済み）にする
                                    eo.eventFlag = 1;
                                }
                                else
                                {
                                    //アイテム欄が満タンの時
                                    MessageController.msgController.MessageDisp("これ以上持てんようじゃの、\n持ち物を整理してから\n" +
                                                      "もう一度来るがよい。\n");
                                }
                            }
                            else
                            {
                                //聖なる薬草を持っていないとき
                                MessageController.msgController.MessageDisp("何じゃ、持っておらんではないか。\n手に入れてからもう一度来るがよい。\n");
                            }
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();
                            MessageController.msgController.MessageDisp("何じゃ、けちじゃのう。\n気が変わったらもう一度来るがよい。\n");
                        }
                    }
                }
                else if (player.nowFloor == 7)
                {
                    //地下7階
                    if (eo.eventNumber == 1)
                    {
                        //死神
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //YesNoウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();

                            MessageController.msgController.MessageDisp("では、立ち去るがよい。\n");

                            //プレイヤーを西へ押し戻す
                            //現在のプレイヤーのx座標から1を引く
                            player.nowPosX--;
                            moveInFlag = true;
                            actMode = ACTMODE.MOVING;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //YesNoウィンドウを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();

                            MessageController.msgController.MessageDisp("愚か者め、死ぬがよい。\n");
                            
                            //NPCを敵に変更する
                            fightFlag = true;
                            npcFlag = false;

                            //一時的にコマンドボタンをクリックできないようにする
                            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                            //少し後に戦闘場面へ移動する
                            Invoke("MoveToFightScene", 5.0f);
                        }
                    }
                }
                else if (player.nowFloor == 8)
                {
                    //地下8階
                    if (eo.eventNumber == 1)
                    {
                        //魔王
                        if (eo.eventFlag == 0)
                        {
                            //1回目のYesNoウィンドウ
                            if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                            {
                                //「はい」を選択したとき

                                //YesNoウィンドウを閉じる
                                YesNoController.yesNoController.YesNoPanelClose();

                                MessageController.msgController.MessageDisp("本当か？\n" +
                                                                "では、忠誠の証としてお前の持っている\n" +
                                                                "武器を私によこすのだ、できるな？\n");

                                //イベントフラグを1にする
                                eo.eventFlag = 1;

                                //再度YesNoウィンドウを開く
                                YesNoController.yesNoController.YesNoPanelOpen();

                            }
                            else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                            {
                                //「いいえ」を選択したとき

                                //YesNoウィンドウを閉じる
                                YesNoController.yesNoController.YesNoPanelClose();

                                MessageController.msgController.MessageDisp("そうか、ではかかって来い！\n");

                                //NPCを敵に変更する
                                fightFlag = true;
                                npcFlag = false;

                                //一時的にコマンドボタンをクリックできないようにする
                                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                                //少し後に戦闘場面へ移動する
                                Invoke("MoveToFightScene", 5.0f);
                            }
                        }
                        else if (eo.eventFlag == 1)
                        {
                            //2回目のYesNoウィンドウ
                            if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                            {
                                //「はい」を選択したとき

                                //YesNoウィンドウを閉じる
                                YesNoController.yesNoController.YesNoPanelClose();

                                //装備している武器のIDを取得
                                int equip_num = (int)(ItemGenerator.ITEM_TYPE.WEAPON - ItemGenerator.ITEM_TYPE.EQUIP) - 1;
                                int weapon_id = player.equipArray[equip_num];

                                if (weapon_id != 0)
                                {
                                    //装備している武器があるとき

                                    //装備している武器の情報を取得
                                    Item item = new Item();
                                    item = itemGenerator.GetItemInfo(weapon_id);

                                    //装備している武器が格納されている所持アイテムリストのインデックス番号を取得する
                                    int wep_index = player.GetItemBoxSpecifyIndex(item);

                                    //装備を外す
                                    player.UnEquip(item, wep_index);

                                    //装備していた武器を削除する
                                    player.BoxItemSubtraction(wep_index);

                                    //メッセージを表示する
                                    MessageController.msgController.MessageDisp("バカめ、騙されおったな、死ねい！\n");
                                }
                                else
                                {
                                    //装備している武器がないとき
                                    MessageController.msgController.MessageDisp("丸腰で来るとは、見上げた奴だ！\n" + 
                                                                    "褒美に一思いに殺してやろう！\n");
                                }

                                //NPCを敵に変更する
                                fightFlag = true;
                                npcFlag = false;

                                //一時的にコマンドボタンをクリックできないようにする
                                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                                //少し後に戦闘場面へ移動する
                                Invoke("MoveToFightScene", 5.0f);

                            }
                            else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                            {
                                //「いいえ」を選択したとき

                                //YesNoウィンドウを閉じる
                                YesNoController.yesNoController.YesNoPanelClose();

                                MessageController.msgController.MessageDisp("やはり、引っかからぬか。\n" +
                                                                "しかし、お前の運命は変わらん。\n" +
                                                                "行くぞ！\n");

                                //NPCを敵に変更する
                                fightFlag = true;
                                npcFlag = false;

                                //一時的にコマンドボタンをクリックできないようにする
                                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                                //少し後に戦闘場面へ移動する
                                Invoke("MoveToFightScene", 5.0f);
                            }
                        }

                    }
                }
            }
            else if (eo.eventType == EventController.EVTYPE.OTHER)
            {
                //その他
                if (player.nowFloor == 1)
                {
                    //地下1階の時
                    if (eo.eventNumber == 1)
                    {
                        //持ち物に関する注意
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("持ち物には少し余裕を持たせておけ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //カルマに関する情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("悪行を重ねれば、いずれ報いを受けるであろう。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //銀の女神像の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("罪深きものよ、財を捧げよ。\nさすれば、汝の罪は赦されるであろう。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //迷宮の調査に関する情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("怪しい場所はくまなく調べろ。\nそうすれば思わぬ発見があるぞ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //銀の女神像
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();

                            if (player.CheckKarmaGold() == true)
                            {
                                //所持ゴールドがカルマを0にするのに必要なゴールド以上の時

                                //所持ゴールドからカルマを0にするのに必要なゴールドを引く
                                player.GoldLost(player.GetKarmaGold());

                                //カルマを0にする
                                player.KarmaInit();

                                MessageController.msgController.MessageDisp("あなたの罪は赦されました。\n");
                            }
                            else
                            {
                                //所持ゴールドがカルマを0にするのに必要なゴールド未満の時
                                MessageController.msgController.MessageDisp("お金が足りません。\n");
                            }
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();
                        }
                    }

                }
                else if (player.nowFloor == 2)
                {
                    //地下2階の時
                    if (eo.eventNumber == 1)
                    {
                        //金の鍵の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("金の鍵はこの階にある。\n何度使用してもなくならない。\n" + 
                                               "見つけるには銀の鍵が必要だ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                }
                else if (player.nowFloor == 3)
                {
                    //地下3階の時
                    if (eo.eventNumber == 1)
                    {
                        //勇者の装備に関する情報情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("この迷宮には勇者の遺した装備が存在している。\n" +
                                               "証を持つ者のみが手にすることができる。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                }
                else if (player.nowFloor == 4)
                {
                    //地下4階の時
                    if (eo.eventNumber == 1)
                    {
                        //紋章の扉の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("真の勇者のみがこの扉を開くことができる。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //扉の絵情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("この階には一瞬で地上に戻ることのできる\n" +
                                               "宝が隠されている。必ず見つけろ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                }
                else if (player.nowFloor == 5)
                {
                    //地下5階の時
                    if (eo.eventNumber == 1)
                    {
                        //紋章の扉の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("真の勇者のみがこの扉を開くことができる。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //地下6階の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("地下6階は闇の世界、ここより西に明かりがある。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber >= 3 && eo.eventNumber <= 4)
                    {
                        //ダークゾーンの警告
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("これより先、闇の世界。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                }
                else if (player.nowFloor == 6)
                {
                    //地下6階の時
                    if (eo.eventNumber == 1)
                    {
                        //紋章の扉の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("真の勇者のみがこの扉を開くことができる。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //鏡の番人の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("ここより北東に石化の呪いを解く手鏡がある。\n" + 
                                                            "立ちはだかる番人に注意しろ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //真実の腕輪の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("魔物の中には人に化ける者がいる。\n" +
                                                            "見破るには腕輪が必要だ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //ランタンを持っていない者への警告
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("明かりを持たぬ者よ、今ならまだ間に合う。\n南を向いて引き返せ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                }
                else if (player.nowFloor == 7)
                {
                    //地下7階の時
                    if (eo.eventNumber == 1)
                    {
                        //紋章の扉の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("真の勇者のみがこの扉を開くことができる。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //死神の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("死神の死の力に対抗するには\n勇者の装備が必要だ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //悪魔の像の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("悪魔の像が我らを王の元へと導く。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //死神の警告
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("この先に死神がいる。\n死にたくなければ、引き返せ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //謁見の間
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("謁見の間\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 6 || eo.eventNumber == 7)
                    {
                        //姫の石像（偽者1）または姫の石像（偽者2）
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCEncount((int)EnemyGenerator.F_ENEMY_TALK.F_PRINCESS);
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            EventImageController.evImageController.ImagePanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 8)
                    {
                        //姫の石像
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCEncount((int)EnemyGenerator.F_ENEMY_TALK.PRINCESS);
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            EventImageController.evImageController.ImagePanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 9)
                    {
                        //金の女神像の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("花を捧げよ、さすれば死者は蘇らん。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 10)
                    {
                        //金の女神像
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();

                            //姫フラグを1にする
                            eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS, 1);

                            //カルマを2減らす
                            player.KarmaAddSub(-2);

                            //命の花をアイテム欄より削除する
                            Item item = new Item();
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.FLOWER);
                            int item_index = player.GetItemBoxSpecifyIndex(item);
                            player.BoxItemSubtraction(item_index);

                            MessageController.msgController.MessageDisp("どこからともなく美しい声で、\n「姫は蘇りました、地上に戻りなさい」と\n聞こえてきた。\n");
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();
                        }
                    }

                }
                else if (player.nowFloor == 8)
                {
                    //地下8階の時
                    if (eo.eventNumber == 2)
                    {
                        //魔王の存在
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("魔王の宮殿\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //仕掛け扉1の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("第一の扉、北西を探せ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //仕掛け扉2の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("第二の扉、北東を探せ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //悪魔の像の扉の情報
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき
                            MessageController.msgController.MessageDisp("魔王の部屋、証を示せ。\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            movableFlag = true;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときウィンドウを閉じて移動可能にする
                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessagePanelClose();
                            movableFlag = true;
                        }
                    }
                    else if (eo.eventNumber == 6 || eo.eventNumber == 7)
                    {
                        //スイッチ1、スイッチ2のどれか
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            EventObject switch_ev = new EventObject();

                            if (eo.eventNumber == 6)
                            {
                                //スイッチ1のとき
                                eventController.DoorKeyUnlock(player.nowFloor, 12, 14);
                            }
                            else
                            {
                                //スイッチ2のとき
                                eventController.DoorKeyUnlock(player.nowFloor, 12, 12);
                            }

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();

                            //フラグを1にする
                            eo.eventFlag = 1;

                            //プレイヤーの行動に関する状態を「スイッチを押した時」にする
                            actMode = ACTMODE.SWITCH_ON;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //ウィンドウとイベント画像パネルを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            NPCMoveOut();
                        }
                    }
                }
            }
            else
            {

            }
                    
        }
        else
        {
        }
    }

    //NPC遭遇処理を行う関数
    //引数
    //id:敵ID（戦う可能性があるため、敵IDにしている）
    void NPCEncount(int id)
    {
        //NPCの種類によって遭遇時のメッセージを変える
        if (id == (int)EnemyGenerator.F_ENEMY_TALK.F_PRINCESS ||
            id == (int)EnemyGenerator.F_ENEMY_TALK.PRINCESS)
        {
            //姫（偽者含む）の時
            MessageController.msgController.MessageDisp("姫の呪いが解けた！\n");
            
        }
        else
        {
            //その他
            MessageController.msgController.MessageDisp("誰かいるぞ！！\n");
        }
        
        //NPCの情報を取得する
        fightEnemy = null;
        fightEnemy = new Enemy();
        fightEnemy.EnemyDataSet(enemyGenerator.GetEnemy(id));
        EventImageController.evImageController.ImageDisp(fightEnemy.enemyImg);

        //フラグをNPC遭遇状態に設定する
        movableFlag = false;
        npcFlag = true;

        //ウィンドウおよびボタン設定をNPC遭遇状態に設定する
        statusPanel.SetActive(true);
        commandPanel.SetActive(true);
        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.TALK);
    }

    //NPC退去処理を行う関数
    void NPCMoveOut()
    {
        //ステータスウィンドウ（小）、コマンドパネル、メッセージウィンドウ、画像を閉じてプレイヤーを移動可能にする
        statusPanel.SetActive(false);
        commandPanel.SetActive(false);
        MessageController.msgController.MessagePanelClose();
        EventImageController.evImageController.ImagePanelClose();
        movableFlag = true;
        npcFlag = false;
    }

    //アイテム取得時に画像表示を行う関数
    //引数
    //id:アイテムID（0の時はゴールド）
    void GetItemImageDisp(int id = 0)
    {
        //取得物画像ファイルパスの文字列の作成準備
        string get_item = GlobalConst.GET_STRING;
        Item item = new Item();

        if (id > 0)
        {
            //アイテムの時は取得したアイテム画像ファイル名をパスにつなげる
            item = itemGenerator.GetItemInfo(id);
            get_item = get_item + item.itemImg;
        }
        else
        {
            //ゴールドの時は取得ゴールド画像ファイルパスを作成する
            get_item = get_item + GlobalConst.GOLD_STRING;
        }

        //画像を表示させる
        EventImageController.evImageController.ImageDisp(get_item);
    }

    //アイテム取得時に表示した画像を消す関数
    void GetItemImageClear()
    {
        EventImageController.evImageController.ImagePanelClose();
        pickUpFlag = false;
    }

    //倒したNPCのいた場所でアイテムを見つけた時の処理を行う関数
    //引数
    //eo:現在位置のイベントオブジェクト
    //id:見つけたアイテムのID
    void KillNPCItemGet(EventObject eo, int id)
    {
        //取得予定のアイテムの情報を取得
        Item item = itemGenerator.GetItemInfo(id);
        //アイテムの画像を表示
        GetItemImageDisp(item.itemId);

        //アイテム発見のメッセージを表示する
        MessageController.msgController.MessageDisp(player.playerName + "は" + item.itemName + "を見つけた！\n");
        movableFlag = false;
        pickUpFlag = true;

        //アイテム取得処理
        Player.PICK_COMPLETION pick = player.ItemPick(item);
        if (pick == Player.PICK_COMPLETION.NG_ITEM_MAX)
        {
            //見つけた使い捨てアイテムの現在数量が最大所持数量以上の時、アイテムを取得せずメッセージを表示する
            MessageController.msgController.MessageJoinDisp("しかし、これ以上" + item.itemName + "は持てない！\n");
            MessageController.msgController.MessageJoinDisp("持ち物を整理してからもう一度来よう。\n");
        }
        else if (pick == Player.PICK_COMPLETION.NG_BOX_MAX)
        {
            //プレイヤーのアイテム所持数が最大のとき、アイテムを取得せずメッセージを表示する
            MessageController.msgController.MessageJoinDisp("しかし、これ以上持てない！\n");
            MessageController.msgController.MessageJoinDisp("持ち物を整理してからもう一度来よう。\n");
        }
        else if (pick == Player.PICK_COMPLETION.OK)
        {
            //アイテムを取得できた時はフラグを取得済みの状態に変更する
            eo.eventFlag = 1;
        }

        //少し後にアイテムの画像を消去して移動可能にする
        Invoke("KillNPCItemGetEnd", 3.0f);
    }

    //倒したNPCのいた場所でアイテムを見つけた時の処理の後始末を行う関数
    void KillNPCItemGetEnd()
    {
        GetItemImageClear();
        movableFlag = true;
    }

    //戦闘場面へ移動するための関数
    void MoveToFightScene()
    {
        //戦闘開始の効果音を再生する
        SoundManager.soundManager.PlaySE("se_battle_start");

        //BGMを停止する
        SoundManager.soundManager.StopBGM(0.5f);

        //先攻を決める
        if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.SUCCUBUS)
        {
            //姫の偽者に話しかけた場合、敵が先攻になる
            fightTurn = false;
        }
        else
        {
            //それ以外はメソッドで決める
            fightTurn = player.FightTurnDecision(fightEnemy);
        }

        if (fightTurn == true)
        {
            //最初に行動する側がプレイヤーの時
            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "が現れた！！\n");
        }
        else
        {
            //最初に行動する側が敵の時
            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "が\n不意に襲い掛かってきた！！\n");
        }
        //敵の画像を表示させる
        EventImageController.evImageController.ImageDisp(fightEnemy.enemyImg);

        //小マップを閉じる
        cameraController.MapClose(false);

        //ステータスウィンドウ、コマンドパネルを表示させる
        statusPanel.SetActive(true);
        commandPanel.SetActive(true);

        //一時的にコマンドボタンをクリックできないようにする
        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

        //戦闘場面の状態を「戦闘場面音声再生」に設定
        fightMode = FIGHTMODE.FIGHT_BGM_PLAY;
        fightWaitFlag = true;
    }

    //戦闘用場面BGMを再生する関数
    void PlayFightBGM()
    {
        if (fightEnemy.enemyType == Enemy.ENEMY_TYPE.FINAL_BOSS)
        {
            //最終ボスの時
            SoundManager.soundManager.PlayBGM("bgm_last_fight", 0.5f, true);
        }
        else
        {
            //他の敵の時
            SoundManager.soundManager.PlayBGM("bgm_fight", 0.5f, true);
        }
    }

    //プレイヤーのキー操作に関する処理を実行する関数
    void PlayerAction()
    {
        //プレイヤーの現在位置、向いている方角を取得
        oldDirection = player.nowDirection;
        movePosX = (float)player.nowPosX;
        movePosZ = (float)player.nowPosZ;
        moveAngle = (float)((int)player.nowDirection * 90.0f);

        //振動テスト用
        /*
        if (Input.GetKeyDown(KeyCode.A))
        {
            cameraController.DungeonCameraShake();
        }
        */

        if (YesNoController.yesNoController.GetPanelOpenFlag() == false)
        {
            //YesNoウィンドウが開いていないとき
            if (movableFlag == true && eventController.FloorRotateCheck() == false)
            {
                //移動可能な時
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    //↑キーが押された時
                    switch (player.nowDirection)
                    {
                        case Player.DIRECTION.NORTH:    //北
                            if (MoveCheck(player.nowPosX, player.nowPosZ - 1) == true)
                            {
                                //移動可能な時

                                //歩数を1増やす
                                stepCount++;

                                //現在のプレイヤーのz座標から1を引く
                                player.nowPosZ--;
                                //メッセージパネルを閉じて移動準備を開始する
                                MessageController.msgController.MessagePanelClose();
                                moveInFlag = true;
                                actMode = ACTMODE.MOVING;
                            }
                            else
                            {
                                //移動不可な時は壁に衝突時の処理を実行する
                                actMode = ACTMODE.CRASH_WALL;
                            }
                            break;
                        case Player.DIRECTION.EAST:    //東
                            if (MoveCheck(player.nowPosX + 1, player.nowPosZ) == true)
                            {
                                //移動可能な時

                                //歩数を1増やす
                                stepCount++;

                                //現在のプレイヤーのx座標に1を足す
                                player.nowPosX++;
                                //メッセージパネルを閉じて移動準備を開始する
                                MessageController.msgController.MessagePanelClose();
                                moveInFlag = true;
                                actMode = ACTMODE.MOVING;
                            }
                            else
                            {
                                //移動不可な時は壁に衝突時の処理を実行する
                                actMode = ACTMODE.CRASH_WALL;
                            }
                            break;
                        case Player.DIRECTION.SOUTH:    //南
                            if (MoveCheck(player.nowPosX, player.nowPosZ + 1) == true)
                            {
                                //移動可能な時

                                //歩数を1増やす
                                stepCount++;

                                //現在のプレイヤーのz座標に1を足す
                                player.nowPosZ++;
                                //メッセージパネルを閉じて移動準備を開始する
                                MessageController.msgController.MessagePanelClose();
                                moveInFlag = true;
                                actMode = ACTMODE.MOVING;
                            }
                            else
                            {
                                //移動不可な時は壁に衝突時の処理を実行する
                                actMode = ACTMODE.CRASH_WALL;
                            }
                            break;
                        case Player.DIRECTION.WEST:    //西
                            if (MoveCheck(player.nowPosX - 1, player.nowPosZ) == true)
                            {
                                //移動可能な時

                                //歩数を1増やす
                                stepCount++;

                                //現在のプレイヤーのx座標から1を引く
                                player.nowPosX--;
                                //メッセージパネルを閉じて移動準備を開始する
                                MessageController.msgController.MessagePanelClose();
                                moveInFlag = true;
                                actMode = ACTMODE.MOVING;
                            }
                            else
                            {
                                //移動不可な時は壁に衝突時の処理を実行する
                                actMode = ACTMODE.CRASH_WALL;
                            }
                            break;
                    }

                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    //↓キーが押された時

                    //現在向いている方角と反対の方角を設定
                    switch (player.nowDirection)
                    {
                        case Player.DIRECTION.NORTH:    //北
                            player.nowDirection = Player.DIRECTION.SOUTH;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.EAST:     //東
                            player.nowDirection = Player.DIRECTION.WEST;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.SOUTH:    //南
                            player.nowDirection = Player.DIRECTION.NORTH;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.WEST:     //西
                            player.nowDirection = Player.DIRECTION.EAST;
                            MessageController.msgController.MessagePanelClose();
                            break;
                    }
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    //←キーが押された時

                    //現在向いている方角から見て左の方角を設定
                    switch (player.nowDirection)
                    {
                        case Player.DIRECTION.NORTH:    //北
                            player.nowDirection = Player.DIRECTION.WEST;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.EAST:     //東
                            player.nowDirection = Player.DIRECTION.NORTH;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.SOUTH:    //南
                            player.nowDirection = Player.DIRECTION.EAST;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.WEST:     //西
                            player.nowDirection = Player.DIRECTION.SOUTH;
                            MessageController.msgController.MessagePanelClose();
                            break;
                    }
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    //→キーが押された時

                    //現在向いている方角から見て右の方角を設定
                    switch (player.nowDirection)
                    {
                        case Player.DIRECTION.NORTH:    //北
                            player.nowDirection = Player.DIRECTION.EAST;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.EAST:     //東
                            player.nowDirection = Player.DIRECTION.SOUTH;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.SOUTH:    //南
                            player.nowDirection = Player.DIRECTION.WEST;
                            MessageController.msgController.MessagePanelClose();
                            break;
                        case Player.DIRECTION.WEST:     //西
                            player.nowDirection = Player.DIRECTION.NORTH;
                            MessageController.msgController.MessagePanelClose();
                            break;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Z))
                {
                    //zキーが押された時
                    switch (player.nowDirection)
                    {
                        case Player.DIRECTION.NORTH:    //北
                            if (DoorCheck(player.nowPosX, player.nowPosZ - 1) == true)
                            {
                                //現在向いている方角に扉があるとき
                                if (DoorLockCheck(player.nowPosX, player.nowPosZ - 1) == false)
                                {
                                    //扉に鍵がかかっていないとき

                                    //歩数を1増やす
                                    stepCount++;

                                    //プレイヤーの行動に関する状態を「扉を開けた時」にする
                                    actMode = ACTMODE.DOOR_OPEN;
                                }
                                else
                                {
                                    //扉に鍵がかかっているとき

                                    //プレイヤーの行動に関する状態を「扉が開かない時」にする
                                    actMode = ACTMODE.LOCKED_DOOR;
                                }

                            }
                            break;
                        case Player.DIRECTION.EAST:     //東
                            if (DoorCheck(player.nowPosX + 1, player.nowPosZ) == true)
                            {
                                //現在向いている方角に扉があるとき
                                if (DoorLockCheck(player.nowPosX + 1, player.nowPosZ) == false)
                                {
                                    //扉に鍵がかかっていないとき

                                    //歩数を1増やす
                                    stepCount++;

                                    //プレイヤーの行動に関する状態を「扉を開けた時」にする
                                    actMode = ACTMODE.DOOR_OPEN;
                                }
                                else
                                {
                                    //扉に鍵がかかっているとき

                                    //プレイヤーの行動に関する状態を「扉が開かない時」にする
                                    actMode = ACTMODE.LOCKED_DOOR;
                                }

                            }
                            break;
                        case Player.DIRECTION.SOUTH:    //南
                            if (DoorCheck(player.nowPosX, player.nowPosZ + 1) == true)
                            {
                                //現在向いている方角に扉があるとき
                                if (DoorLockCheck(player.nowPosX, player.nowPosZ + 1) == false)
                                {
                                    //扉に鍵がかかっていないとき

                                    //歩数を1増やす
                                    stepCount++;

                                    //プレイヤーの行動に関する状態を「扉を開けた時」にする
                                    actMode = ACTMODE.DOOR_OPEN;
                                }
                                else
                                {
                                    //扉に鍵がかかっているとき

                                    //プレイヤーの行動に関する状態を「扉が開かない時」にする
                                    actMode = ACTMODE.LOCKED_DOOR;
                                }

                            }
                            break;
                        case Player.DIRECTION.WEST:     //西
                            if (DoorCheck(player.nowPosX - 1, player.nowPosZ) == true)
                            {
                                //現在向いている方角に扉があるとき
                                if (DoorLockCheck(player.nowPosX - 1, player.nowPosZ) == false)
                                {
                                    //扉に鍵がかかっていないとき

                                    //歩数を1増やす
                                    stepCount++;

                                    //プレイヤーの行動に関する状態を「扉を開けた時」にする
                                    actMode = ACTMODE.DOOR_OPEN;
                                }
                                else
                                {
                                    //扉に鍵がかかっているとき

                                    //プレイヤーの行動に関する状態を「扉が開かない時」にする
                                    actMode = ACTMODE.LOCKED_DOOR;
                                }

                            }
                            break;
                    }
                }
            }

            if (eventController.FloorRotateCheck() == false)
            {
                //回転床が起動していないとき

                if (cameraController.MapOpenCheck() == false && itemFlag == false && saveFlag == false)
                {
                    //マップ、アイテムウィンドウ、セーブウィンドウの全てが閉じているとき

                    if (Input.GetKeyDown(KeyCode.X))
                    {
                        //xキーが押された時
                        if (movableFlag == true)
                        {
                            //ステータスウィンドウ（小）およびコマンドパネルが表示されていないときは表示させて、プレイヤーを移動不可にする
                            movableFlag = false;
                            statusPanel.SetActive(true);
                            commandPanel.SetActive(true);
                            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.NORMAL);
                            //MessageController.msgController.MessageDisp("コマンド");

                        }
                        else
                        {
                            if (fightFlag == false)
                            {
                                //戦闘中でないとき
                                if (statusFlag == false && itemFlag == false && 
                                    npcFlag == false && cameraController.MapOpenCheck() == false && 
                                    returnFlag == false && pickUpFlag == false)
                                {
                                    //以下の条件を全て満たしているとき
                                    //1.ステータスウィンドウとアイテムウィンドウが閉じているとき
                                    //2.NPC遭遇時でないとき
                                    //3.地図が閉じているとき
                                    //4.帰還アイテムを使用中でないとき
                                    //5.倒した敵もしくはNPCが落としたアイテムを拾うイベントが発生中でない
                                    
                                    //ステータスウィンドウ（小）、コマンドパネル、メッセージウィンドウを閉じてプレイヤーを移動可能にする
                                    movableFlag = true;
                                    npcFlag = false;
                                    statusPanel.SetActive(false);
                                    commandPanel.SetActive(false);
                                    MessageController.msgController.MessagePanelClose();
                                    EventImageController.evImageController.ImagePanelClose();
                                }

                            }

                        }

                    }
                }


            }
        }

        if (player.nowDirection != oldDirection)
        {
            //方向転換の操作が行われた時はプレイヤーの行動に関する状態を「方向転換中」に設定する
            actMode = ACTMODE.C_DIRECTION;
        }
    }

    //扉に鍵がかかっているときにメッセージを表示し、効果音を再生する関数
    //引数
    //dir:現在プレイヤーが向いている方角
    void KeyDoorMessageDisp(Player.DIRECTION dir)
    {
        int door_type;

        switch (dir) {
            case Player.DIRECTION.NORTH:    //北
                door_type = GetDoorLockType(player.nowPosX, player.nowPosZ - 1);
                break;
            case Player.DIRECTION.EAST:     //東
                door_type = GetDoorLockType(player.nowPosX + 1, player.nowPosZ);
                break;
            case Player.DIRECTION.SOUTH:    //南
                door_type = GetDoorLockType(player.nowPosX, player.nowPosZ + 1);
                break;
            case Player.DIRECTION.WEST:     //西
                door_type = GetDoorLockType(player.nowPosX - 1, player.nowPosZ);
                break;
            default:    //どれにも該当しない場合は北の時の値を入れる
                door_type = GetDoorLockType(player.nowPosX, player.nowPosZ - 1);
                break;
        }

        //鍵の種類をチェックして表示するメッセージを決める
        if ((ItemGenerator.EVITEM_NONE_FIGHT)door_type == ItemGenerator.EVITEM_NONE_FIGHT.KEY3 || 
            (ItemGenerator.EVITEM_NONE_FIGHT)door_type == ItemGenerator.EVITEM_NONE_FIGHT.KEY4)
        {
            //紋章または悪魔の像
            MessageController.msgController.MessageDisp("扉は不思議な力で封印されている。\n");
        }
        else if ((ItemGenerator.EVITEM_NONE_FIGHT)door_type == ItemGenerator.EVITEM_NONE_FIGHT.KEY1 ||
            (ItemGenerator.EVITEM_NONE_FIGHT)door_type == ItemGenerator.EVITEM_NONE_FIGHT.KEY2)
        {
            //他の鍵
            MessageController.msgController.MessageDisp("扉には鍵がかかっている。\n");
        }
        else
        {
            //スイッチで開く扉
            MessageController.msgController.MessageDisp("扉が開かない。\n");
        }

        //扉が開かない時の効果音を再生する
        SoundManager.soundManager.PlaySE("se_locked_door");
    }


    //プレイヤーを移動させる関数（定数で指定した時間で移動させる）
    void PlayerMoving()
    {
        moveTimer += Time.deltaTime;
        if (moveTimer >= MOVE_TIME)
        {
            moveTimer = MOVE_TIME;

        }

        if ((float)player.nowPosX - movePosX > 0)
        {
            //東へ移動させる
            movePosX += moveTimer / MOVE_TIME;
            if (movePosX >= (float)player.nowPosX)
            {
                movePosX = (float)player.nowPosX;
            }
        }
        else
        {
            //西へ移動させる
            movePosX -= moveTimer / MOVE_TIME;
            if (movePosX <= (float)player.nowPosX)
            {
                movePosX = (float)player.nowPosX;
            }
        }

        if ((float)player.nowPosZ - movePosZ > 0)
        {
            //南へ移動させる
            movePosZ += moveTimer / MOVE_TIME;
            if (movePosZ >= (float)player.nowPosZ)
            {
                movePosZ = (float)player.nowPosZ;
            }
        }
        else
        {
            //北へ移動させる
            movePosZ -= moveTimer / MOVE_TIME;
            if (movePosZ <= (float)player.nowPosZ)
            {
                movePosZ = (float)player.nowPosZ;
            }
        }

        //指定座標へプレイヤーを移動させる
        float pz = (float)map_z_max - movePosZ;
        Vector3 v = new Vector3(movePosX, 0.0f, pz);
        transform.position = v;

        //プレイヤーに合わせて画面左上の小マップを更新する
        v.y = S_MAP_COM_POS_Y;
        cameraController.SmallMapCameraPositionSet(v.x, v.y, v.z);

        if (moveTimer >= MOVE_TIME)
        {
            //移動が完了したらプレイヤーの行動に関する状態を「停止中」に設定する
            actMode = ACTMODE.STAYING;
            //移動用タイマーの初期化
            moveTimer = 0.0f;
        }
    }

    //指定位置への移動が可能かをチェックする関数
    //引数
    //x:X座標
    //z:Z座標
    //戻り値（true:移動可能、false:移動不可）
    bool MoveCheck(int x, int z)
    {
        if (mapGenerator.MapInfoGet(player.nowFloor, x, z) == (int)MapGenerator.MAPINFO.OUTERWALL)
        {
            //外壁
            return false;
        }
        else if (mapGenerator.MapInfoGet(player.nowFloor, x, z) == (int)MapGenerator.MAPINFO.INNERWALL)
        {
            //内壁
            return false;
        }
        else if (mapGenerator.MapInfoGet(player.nowFloor, x, z) == (int)MapGenerator.MAPINFO.DOOR)
        {
            //扉
            return false;
        }
        else
        {
            //何もなし
            return true;
        }

    }

    //プレイヤーの目の前に扉があるかをチェックする関数
    //引数
    //x:X座標
    //z:Z座標
    //戻り値（true:扉あり、false:扉なし）
    bool DoorCheck(int x, int z)
    {
        if (mapGenerator.MapInfoGet(player.nowFloor, x, z) == (int)MapGenerator.MAPINFO.DOOR)
        {
            //扉があるとき
            return true;
        }
        //扉がないとき
        return false;
    }

    //プレイヤーの目の前にある鍵付き扉が開錠済みかをチェックする関数
    //引数
    //x:X座標
    //z:Z座標
    //戻り値（true:未開錠、false:開錠済み）
    bool DoorLockCheck(int x, int z)
    {
        EventObject eo = new EventObject();
        eo = eventController.GetEventInfo(player.nowFloor, x, z);
        if (eo.eventType == EventController.EVTYPE.KEY_DOOR && eo.eventFlag == (int)EventController.KEYFLAG.LOCKED)
        {
            //未開錠の時
            return true;
        }
        //開錠済みの時
        return false;
    }

    //プレイヤーの目の前にある鍵付き扉の種類を取得する関数
    //引数
    //x:X座標
    //z:Z座標
    //戻り値（鍵付き扉の種類 ※イベント番号）
    int GetDoorLockType(int x, int z)
    {
        EventObject eo = new EventObject();
        eo = eventController.GetEventInfo(player.nowFloor, x, z);

        return eo.eventNumber;
    }

    //使用した鍵が対象の鍵付き扉と合っているかをチェックする関数
    //引数
    //x:X座標
    //z:Z座標
    //item_id:アイテムID
    //戻り値（0:プレイヤーの向いている方向に鍵付き扉があり、使用した鍵が対象の鍵付き扉と合う場合、
    //        1:プレイヤーの向いている方向に鍵付き扉がない、もしくは開錠済みの場合、
    //        2:使用した鍵が対象の鍵付き扉と合わない場合）
    int DoorKeyMatchCheck(int x, int z, int item_id)
    {
        EventObject eo = new EventObject();

        //プレイヤーが向いている1歩先の座標を取得
        int door_x = x;
        int door_z = z;

        switch (player.nowDirection)
        {
            case Player.DIRECTION.NORTH:    //北
                door_z--;
                break;
            case Player.DIRECTION.EAST:     //東
                door_x++;
                break;
            case Player.DIRECTION.SOUTH:    //南
                door_z++;
                break;
            case Player.DIRECTION.WEST:     //西
                door_x--;
                break;
        }

        //プレイヤーが向いている方角の扉情報を取得
        eo = eventController.GetEventInfo(player.nowFloor, door_x, door_z);

        if (eo.eventType != EventController.EVTYPE.KEY_DOOR || eo.eventFlag != (int)EventController.KEYFLAG.LOCKED)
        {
            //プレイヤーの向いている方向に鍵付き扉がない、もしくは開錠済みの場合は1を返す
            return 1;
        }

        if (eo.eventNumber != item_id)
        {
            //使用した鍵が対象の鍵付き扉と合わない場合は2を返す
            return 2;
        }

        //プレイヤーの向いている方向に鍵付き扉があり、使用した鍵が対象の鍵付き扉と合う場合は0を返す
        return 0;
    }

    //現在位置にダークゾーンが存在し、かつ有効であるかどうかをチェックする関数
    //引数
    //eo:現在位置のイベント情報
    //戻り値（true:ダークゾーン有効、false:ダークゾーン無効または未存在）
    bool DarkZoneCheck(EventObject eo)
    {
        if(eo.darkZoneFlag == true && BoxItemLanthanumCheck() == false)
        {
            //現在位置がダークゾーンで、魔法のランタンが未所持のとき
            return true;
        }
        else
        {
            //上記以外の条件の時
            return false;
        }  
    }

    //プレイヤーを方向転換させる関数（定数で指定した時間で方向転換させる）
    void PlayerDirectionChanging()
    {
        if (moveTimer <= 0)
        {
            //タイマーが0の時に転換先の角度を設定する
            if (oldDirection == Player.DIRECTION.NORTH)
            {
                //現在の方角が北の時
                if (player.nowDirection == Player.DIRECTION.WEST)
                {
                    //転換先の方角が西の時
                    endAngle = transform.localEulerAngles.y - 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.EAST)
                {
                    //転換先の方角が東の時
                    endAngle = transform.localEulerAngles.y + 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.SOUTH)
                {
                    //転換先の方角が南の時
                    endAngle = transform.localEulerAngles.y + 180.0f;
                }
            }
            else if (oldDirection == Player.DIRECTION.EAST)
            {
                //現在の方角が東の時
                if (player.nowDirection == Player.DIRECTION.NORTH)
                {
                    //転換先の方角が北の時
                    endAngle = transform.localEulerAngles.y - 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.SOUTH)
                {
                    //転換先の方角が南の時
                    endAngle = transform.localEulerAngles.y + 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.WEST)
                {
                    //転換先の方角が西の時
                    endAngle = transform.localEulerAngles.y + 180.0f;
                }
            }
            else if (oldDirection == Player.DIRECTION.SOUTH)
            {
                //現在の方角が南の時
                if (player.nowDirection == Player.DIRECTION.EAST)
                {
                    //転換先の方角が東の時
                    endAngle = transform.localEulerAngles.y - 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.WEST)
                {
                    //転換先の方角が西の時
                    endAngle = transform.localEulerAngles.y + 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.NORTH)
                {
                    //転換先の方角が北の時
                    endAngle = transform.localEulerAngles.y + 180.0f;
                }
            }
            else if (oldDirection == Player.DIRECTION.WEST)
            {
                //現在の方角が西の時
                if (player.nowDirection == Player.DIRECTION.SOUTH)
                {
                    //転換先の方角が南の時
                    endAngle = transform.localEulerAngles.y - 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.NORTH)
                {
                    //転換先の方角が北の時
                    endAngle = transform.localEulerAngles.y + 90.0f;
                }
                else if (player.nowDirection == Player.DIRECTION.EAST)
                {
                    //転換先の方角が東の時
                    endAngle = transform.localEulerAngles.y + 180.0f;
                }
            }
        }

        moveTimer += Time.deltaTime;
        if (moveTimer >= MOVE_TIME)
        {
            moveTimer = MOVE_TIME;
        }

        //転換先の角度へ回転させる
        if (oldDirection == Player.DIRECTION.NORTH)
        {
            //現在の方角が北の時
            if (player.nowDirection == Player.DIRECTION.WEST)
            {
                //転換先の方角が西の時
                moveAngle -= (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle <= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.EAST)
            {
                //転換先の方角が東の時
                moveAngle += (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.SOUTH)
            {
                //転換先の方角が南の時
                moveAngle += (moveTimer / MOVE_TIME) * 180.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
        }
        else if (oldDirection == Player.DIRECTION.EAST)
        {
            //現在の方角が東の時
            if (player.nowDirection == Player.DIRECTION.NORTH)
            {
                //転換先の方角が北の時
                moveAngle -= (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle <= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.SOUTH)
            {
                //転換先の方角が南の時
                moveAngle += (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.WEST)
            {
                //転換先の方角が西の時
                moveAngle += (moveTimer / MOVE_TIME) * 180.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
        }
        else if (oldDirection == Player.DIRECTION.SOUTH)
        {
            //現在の方角が南の時
            if (player.nowDirection == Player.DIRECTION.EAST)
            {
                //転換先の方角が東の時
                moveAngle -= (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle <= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.WEST)
            {
                //転換先の方角が西の時
                moveAngle += (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.NORTH)
            {
                //転換先の方角が北の時
                moveAngle += (moveTimer / MOVE_TIME) * 180.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
        }
        else if (oldDirection == Player.DIRECTION.WEST)
        {
            //現在の方角が西の時
            if (player.nowDirection == Player.DIRECTION.SOUTH)
            {
                //転換先の方角が南の時
                moveAngle -= (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle <= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.NORTH)
            {
                //転換先の方角が北の時
                moveAngle += (moveTimer / MOVE_TIME) * 90.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
            else if (player.nowDirection == Player.DIRECTION.EAST)
            {
                //転換先の方角が東の時
                moveAngle += (moveTimer / MOVE_TIME) * 180.0f;
                if (moveAngle >= endAngle)
                {
                    moveAngle = endAngle;
                }
            }
        }

        //転換先の角度をプレイヤーの現在角度へ代入
        transform.rotation = Quaternion.Euler(0, moveAngle, 0);

        if (moveTimer >= MOVE_TIME)
        {
            //方向転換が完了したらプレイヤーの行動に関する状態を「停止中」に設定する
            actMode = ACTMODE.STAYING;
            //移動用タイマーの初期化
            moveTimer = 0.0f;
        }

    }

    //移動中の毒によるダメージをプレイヤーに与える関数
    void MovePoison()
    {
        if (player.playerCondition == Player.PLAYER_CONDITION.POISON)
        {
            //毒状態の時にダメージを与える
            player.PoisonDamage(fightFlag);
        }
    }

    //移動中の祝福によるプレイヤーのHP回復を行う関数
    void MoveBlessing()
    {
        if (player.playerCondition == Player.PLAYER_CONDITION.BLESSING)
        {
            //祝福状態の時にHPを回復する
            player.BlessingHpHeal();
        }
    }

    //ステータスウィンドウ（小）の情報表示関数
    void StatusDisp()
    {
        //レベル
        LvText.text = player.playerLv.ToString();
        //HP
        hpText.text = player.playerHp.ToString() + " / " + player.playerHpMax.ToString();
        //経験値
        expText.text = player.playerExp.ToString();
        //ゴールド
        goldText.text = player.playerGold.ToString();
        //状態
        conditionText.text = player.ConditionTextGet();
    }

    //ステータスウィンドウの情報表示関数
    void StatusWindowDisp()
    {
        //名前
        st_nameText.text = player.playerName;
        //レベル
        st_levelText.text = player.playerLv.ToString();
        //HP
        st_hpText.text = player.playerHp.ToString() + " / " + player.playerHpMax.ToString();
        //攻撃力
        st_attackText.text = player.playerAttack.ToString();
        //防御力
        st_defenseText.text = player.playerDefense.ToString();
        //素早さ
        st_speedText.text = player.playerSpeed.ToString();
        //運
        st_luckText.text = player.playerLuck.ToString();
        //状態
        st_conditionText.text = player.ConditionTextGet();
        //経験値
        st_expText.text = player.playerExp.ToString();
        //ゴールド
        st_goldText.text = player.playerGold.ToString();
        //次のレベルUPに必要な経験値
        st_nextLevelText.text = player.PlayerNextLevelExp();
        //装備武器
        st_weaponText.text = GetEquipItemName(itemGenerator.GetEquipNumber(ItemGenerator.ITEM_TYPE.WEAPON));
        //装備鎧
        st_armorText.text = GetEquipItemName(itemGenerator.GetEquipNumber(ItemGenerator.ITEM_TYPE.ARMOR));
        //装備盾
        st_shieldText.text = GetEquipItemName(itemGenerator.GetEquipNumber(ItemGenerator.ITEM_TYPE.SHIELD));
        //装備兜
        st_helmetText.text = GetEquipItemName(itemGenerator.GetEquipNumber(ItemGenerator.ITEM_TYPE.HELM));
    }

    //装備名取得関数
    //引数
    //num:装備種類番号
    //戻り値（装備しているアイテム名）
    string GetEquipItemName(int num)
    {
        Item item;
        if (num < Player.EQUIP_MAX)
        {
            if (player.equipArray[num] > 0)
            {
                //装備しているときは装備しているアイテム名を返す
                item = itemGenerator.GetItemInfo(player.equipArray[num]);
                return item.itemName;
            }
            else
            {
                //装備していないときは空白を返す
                return "";
            }
        }
        else
        {
            //装備番号が装備の種類数以上（入力ミス）の時は空白を返す
            return "";
        }
    }

    //ステータスウィンドウを開く関数
    void StatusWindowOpen()
    {
        statusFlag = true;
        //コマンドボタンを押せないようにする
        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.STATUS);
        //ウィンドウを開いて内容を表示
        statusWindow.SetActive(true);
        StatusWindowDisp();
    }

    //ステータスウィンドウを閉じる関数
    void StatusWindowClose()
    {
        statusFlag = false;
        //ウィンドウを閉じる
        statusWindow.SetActive(false);
        //コマンドボタンを押せるようにする
        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.NORMAL);
    }

    //アイテムウィンドウを開く関数
    void ItemWindowOpen()
    {
        //選択されたアイテム名番号を保存しておく変数の初期化（最初の番号が0のため初期値を-1に設定している）
        saveItemNum = -1;

        itemFlag = true;

        //コマンドボタンを押せないようにする
        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.ITEM);
        //ウィンドウを開く
        itemWindow.SetActive(true);
        //最大ページ数の取得
        itemPageMax = player.ItemPageMaxCalc();
        //内容を表示する
        ItemWindowDisp();

    }

    //アイテムウィンドウを閉じる関数
    void ItemWindowClose()
    {
        itemFlag = false;

        //ウィンドウを閉じる
        itemWindow.SetActive(false);

        //コマンドパネルを表示する（状況によって使用できるボタンを設定する）
        if (fightFlag == true)
        {
            //戦闘中
            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);
        }
        else if (npcFlag == true)
        {
            //NPC遭遇中
            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.TALK);
        }
        else
        {
            //通常
            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.NORMAL);
        }

    }

    //アイテムウィンドウの内容を表示する関数
    void ItemWindowDisp()
    {
        //ウィンドウの表示内容の初期化
        ItemWindowClear();

        //現在ページおよび最大ページを表示する
        int current = itemPage;
        int page_last = itemPageMax;
        string str = current.ToString() + " / " + page_last.ToString();
        Text page = itemPageLabel.GetComponent<Text>();
        page.text = str;

        //現在のページに表示する所持アイテムの開始番号の設定
        int box_start = (itemPage - 1) * Player.ITEM_PER_PAGE;

        //現在のページに表示する所持アイテムラベル配列の終端番号の設定
        int label_end;

        if (current == page_last)
        {
            //現在ページが最終ページの時
            if (page_last == 1)
            {
                //最大ページが1のときは終端番号にアイテム所持数を設定
                label_end = player.GetItemBoxCount();
            }
            else
            {
                //最大ページが2以上のとき
                if (player.GetItemBoxCount() % Player.ITEM_PER_PAGE == 0)
                {
                    //プレイヤーの所持アイテム数が１ページ当たりの表示アイテム数で割り切れるとき
                    //終端番号に所持アイテムラベル配列の長さを設定
                    label_end = itemNameLabels.Length;
                }
                else
                {
                    //プレイヤーの所持アイテム数が１ページ当たりの表示アイテム数で割り切れないとき
                    //終端番号に上記の余りを設定
                    label_end = player.GetItemBoxCount() % Player.ITEM_PER_PAGE;
                }
            }
        }
        else
        {
            //現在ページが最終ページでないの時は終端番号に所持アイテムラベル配列の長さを設定
            label_end = itemNameLabels.Length;
        }

        if (current == 1)
        {
            //最初のページの時前ページ移動ボタンを使用不可にする
            itemPreviousButton.GetComponent<Button>().interactable = false;
        }

        if (current == page_last)
        {
            //最後のページの時次ページ移動ボタンを使用不可にする
            itemNextButton.GetComponent<Button>().interactable = false;
        }

        //ウィンドウに所持アイテム一覧を表示する
        for (int i = 0; i < label_end; i++)
        {
            foreach (Item item in itemList)
            {
                if (player.GetItemBoxIndex(i + box_start).itemId == item.itemId && player.GetItemBoxIndex(i + box_start).itemId != 0)
                {
                    //所持アイテムのIDと全アイテムリストのIDを照合してアイテム情報を取得
                    itemNameLabels[i].SetActive(true);
                    Text name = itemNameLabels[i].GetComponent<Text>();
                    Text count = itemCountLabels[i].GetComponent<Text>();
                    Text equip = itemEquipLabels[i].GetComponent<Text>();

                    //アイテム名を表示
                    name.text = item.itemName;
                    //使い捨てアイテムの時、数量を表示する
                    if (item.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE
                        || item.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE)
                    {
                        count.text = player.GetItemBoxIndex(i + box_start).itemCount.ToString();
                    }
                    //装備中の時「E」を表示する
                    if (player.GetItemBoxIndex(i + box_start).itemEquiped == true)
                    {
                        equip.text = "E";
                    }
                }
            }
        }
    }

    //アイテムウィンドウを初期化する関数
    void ItemWindowClear()
    {
        //改ページボタンの初期化
        itemPreviousButton.GetComponent<Button>().interactable = true;
        itemNextButton.GetComponent<Button>().interactable = true;

        //アイテム情報の文字色取得（初期値）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        //アイテムウィンドウページ数ラベルの初期化
        Text page = itemPageLabel.GetComponent<Text>();
        page.text = "";

        //アイテム名、数量、装備中のラベルの初期化
        for (int i = 0; i < itemNameLabels.Length; i++)
        {
            Text name = itemNameLabels[i].GetComponent<Text>();
            Text count = itemCountLabels[i].GetComponent<Text>();
            Text equip = itemEquipLabels[i].GetComponent<Text>();
            
            //テキストの初期化
            name.text = "";
            count.text = "";
            equip.text = "";
            
            //色の初期化
            equip.color = labelColor;
            name.color = labelColor;
            count.color = labelColor;

            itemNameLabels[i].SetActive(false);
        }

        //アイテム情報の初期化
        Text explain = itemExplanation.GetComponent<Text>();
        explain.text = "";

        //アイテム画像の初期化
        ItemImageDisp("NoItem");
    }

    //アイテム画像を表示する関数
    //引数
    //img:画像ファイル名
    void ItemImageDisp(string img)
    {
        Image itemImage;
        Sprite itemSprite;

        //指定した画像ファイルをロードして表示する
        itemSprite = Resources.Load<Sprite>(GlobalConst.IMG_DIR + img) as Sprite;
        GameObject ob = GameObject.Find("ItemImage");
        itemImage = ob.GetComponent<Image>();
        itemImage.sprite = itemSprite;
    }

    //「調べる」ボタンクリック時の処理を行う関数
    public void CheckButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //発見フラグの初期化
        //発見フラグ（0:何も発見していない、1:アイテム発見、2:ゴールド発見）
        int discover_flag = 0;

        //現在地のイベント情報の取得
        EventObject eo = new EventObject();
        eo = eventController.GetEventInfo(player.nowFloor, player.nowPosX, player.nowPosZ);

        if (eo.evActivation == EventController.EVACTIVATION.CHECK)
        {
            //対象イベントが調査型であるとき
            if (eo.eventType == EventController.EVTYPE.ITEM)
            {
                //アイテムおよびゴールド

                //アイテムオブジェクトの初期化
                Item item = new Item();
                //取得ゴールドの初期化
                int get_gold = 0;

                //取得予定のアイテムの情報を取得（ゴールドの場合は取得処理を行う）
                if (player.nowFloor == 1)
                {
                    //地下1階の時
                    if (eo.eventNumber == 1)
                    {
                        //魔法の地図
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.MAP);
                            discover_flag = 1;
                        }

                    }
                    else if (eo.eventNumber == 2)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(30);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(24);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(36);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(30);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 6)
                    {
                        //薬草
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(25);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 7)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(40);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 8)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(40);
                            discover_flag = 2;
                        }
                    }

                }
                else if (player.nowFloor == 2)
                {
                    //地下2階の時
                    if (eo.eventNumber == 1)
                    {
                        //金の鍵
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.KEY2);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(65);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(100);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(50);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(85);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 6)
                    {
                        //毒消し草
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(26);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 7)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(90);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 8)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(60);
                            discover_flag = 2;
                        }
                    }
                }
                else if (player.nowFloor == 3)
                {
                    //地下3階の時
                    if (eo.eventNumber == 1)
                    {
                        //ミストリル鉱石
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.ORE);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(130);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(200);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(100);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(100);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 6)
                    {
                        //聖水
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(27);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 7)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(140);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 8)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(160);
                            discover_flag = 2;
                        }
                    }
                }
                else if (player.nowFloor == 4)
                {
                    //地下4階の時
                    if (eo.eventNumber == 1)
                    {
                        //勇者の兜
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_FIGHT.HELM1);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //扉の絵
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.RETURN);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(200);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(130);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(140);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 6)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(70);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 7)
                    {
                        //聖なる薬草
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(28);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 8)
                    {
                        //女神の祝福
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(47);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 9)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(200);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 10)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(150);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 11)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(150);
                            discover_flag = 2;
                        }
                    }
                }
                else if (player.nowFloor == 5)
                {
                    //地下5階の時
                    if (eo.eventNumber == 1)
                    {
                        //魔法のランタン
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.LANTHANUM);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //死の首飾り
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_FIGHT.D_NECKLACE);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //勇者の盾
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_FIGHT.SHIELD1);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(200);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(300);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 6)
                    {
                        //毒消し草
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(26);
                            discover_flag = 1;
                        }

                    }
                    else if (eo.eventNumber == 7)
                    {
                        //女神の祝福
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(47);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 8)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(250);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 9)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(300);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 10)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(200);
                            discover_flag = 2;
                        }
                    }
                }
                else if (player.nowFloor == 6)
                {
                    //地下6階の時
                    if (eo.eventNumber == 1)
                    {
                        //解呪の手鏡
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.MIRROR);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //真実の腕輪
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.BANGLE);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //勇者の鎧
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_FIGHT.ARMOR1);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(340);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(300);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 6)
                    {
                        //聖水
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(27);
                            discover_flag = 1;
                        }

                    }
                    else if (eo.eventNumber == 7)
                    {
                        //女神の祝福
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(47);
                            discover_flag = 1;
                        }

                    }
                    else if (eo.eventNumber == 8)
                    {
                        //命の花
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.FLOWER);
                            discover_flag = 1;
                        }

                    }
                    else if (eo.eventNumber == 9)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(500);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 10)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(500);
                            discover_flag = 2;
                        }
                    }

                }
                else if (player.nowFloor == 7)
                {
                    //地下7階の時
                    if (eo.eventNumber == 1)
                    {
                        //勇者の剣
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_FIGHT.WEAPON1);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //悪魔の像
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.KEY4);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 3)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(1000);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 4)
                    {
                        //聖なる薬草
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(28);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 5)
                    {
                        //女神の祝福
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(47);
                            discover_flag = 1;
                        }
                    }
                    else if (eo.eventNumber == 6)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(700);
                            discover_flag = 2;
                        }
                    }
                    else if (eo.eventNumber == 7)
                    {
                        //ゴールド
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            get_gold = player.GoldGet(800);
                            discover_flag = 2;
                        }
                    }
                }
                else if (player.nowFloor == 8)
                {
                    //地下8階の時
                    if (eo.eventNumber == 1)
                    {
                        //女神の祝福
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(47);
                            discover_flag = 1;
                        }

                    }
                    else if (eo.eventNumber == 2)
                    {
                        //女神の祝福
                        if (eo.eventFlag == 0)
                        {
                            //見つけていないとき取得する
                            item = itemGenerator.GetItemInfo(47);
                            discover_flag = 1;
                        }
                    }
                }

                //アイテムもしくはゴールドの発見メッセージを表示する
                //その後取得した場合はフラグを取得済みに変更する
                if (discover_flag == 1)
                {
                    //アイテムの時

                    //アイテム発見のメッセージと画像を表示する
                    MessageController.msgController.MessageDisp(player.playerName + "は" + item.itemName + "を見つけた！\n");
                    GetItemImageDisp(item.itemId);

                    //アイテム取得処理
                    Player.PICK_COMPLETION pick = player.ItemPick(item);
                    if (pick == Player.PICK_COMPLETION.NG_ITEM_MAX)
                    {
                        //見つけた使い捨てアイテムの現在数量が最大所持数量以上の時、アイテムを取得せずメッセージを表示する
                        MessageController.msgController.MessageJoinDisp("しかし、これ以上" + item.itemName + "は持てない！\n");
                        MessageController.msgController.MessageJoinDisp("持ち物を整理してからもう一度来よう。\n");
                    }
                    else if (pick == Player.PICK_COMPLETION.NG_BOX_MAX)
                    {
                        //プレイヤーのアイテム所持数が最大のとき、アイテムを取得せずメッセージを表示する
                        MessageController.msgController.MessageJoinDisp("しかし、これ以上持てない！\n");
                        MessageController.msgController.MessageJoinDisp("持ち物を整理してからもう一度来よう。\n");
                    }
                    else if (pick == Player.PICK_COMPLETION.OK)
                    {
                        //アイテムを取得できた時はフラグを取得済みの状態に変更する
                        eo.eventFlag = 1;
                    }

                    Invoke("GetItemImageClear", 3.0f);


                }
                else if(discover_flag == 2)
                {
                    //ゴールドの時

                    //ゴールド発見のメッセージと画像を表示する
                    MessageController.msgController.MessageDisp(player.playerName + "は" + get_gold + "ゴールドを見つけた！\n");
                    GetItemImageDisp(0);

                    //フラグを取得済みの状態に変更する
                    eo.eventFlag = 1;

                    Invoke("GetItemImageClear", 3.0f);
                }
                else
                {
                    //すでにアイテムおよびゴールドを見つけているとき
                    MessageController.msgController.MessageDisp("何もない。\n");
                }

            }
            else
            {
                //その他
                MessageController.msgController.MessageDisp("何かあった！！\n");
            }
        }
        else if (eo.evActivation == EventController.EVACTIVATION.BOTH)
        {
            //対象イベントが両方型であるとき
            if (eo.eventType == EventController.EVTYPE.UPSTAIRS)
            {
                //上り階段
                statusPanel.SetActive(false);
                commandPanel.SetActive(false);
                EventImageController.evImageController.ImageDisp(GlobalConst.IMG_UPSTAIRS);
                MessageController.msgController.MessageDisp("上る階段がある。\n上りますか？");
                YesNoController.yesNoController.YesNoPanelOpen();
            }
            else if (eo.eventType == EventController.EVTYPE.DOWNSTAIRS)
            {
                //下り階段
                statusPanel.SetActive(false);
                commandPanel.SetActive(false);
                EventImageController.evImageController.ImageDisp(GlobalConst.IMG_DOWNSTAIRS);
                MessageController.msgController.MessageDisp("下る階段がある。\n下りますか？");
                YesNoController.yesNoController.YesNoPanelOpen();
            }
        }
        else
        {
            //対象イベントが調査型でないとき
            MessageController.msgController.MessageDisp("何もない。\n");
        }
    }

    //「話す」ボタンクリック時の処理を行う関数
    public void TalkButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //プレイヤーの行動に関する状態を「会話中」に移行する
        actMode = ACTMODE.TALKING;
    }

    //「戦う」ボタンクリック時の処理を行う関数
    public void FightButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        if (fightFlag == true)
        {
            //戦闘中
            if (fightMode == FIGHTMODE.PLAYER_WAIT)
            {
                //戦闘に関する状態を「プレイヤーの攻撃」に変更してコマンドボタンを使用不可にする
                fightMode = FIGHTMODE.PLAYER_ATTACK;
                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
            }
        }
        else if (fightFlag == false && npcFlag == true)
        {
            //NPC遭遇時

            //NPCを敵に変更する
            fightFlag = true;
            npcFlag = false;

            //戦闘に関する状態を「戦闘開始時の敵の会話」に変更
            fightMode = FIGHTMODE.ENEMY_TALK;
            //会話後に「プレイヤーの攻撃」に移行するように設定
            nextFightMode = FIGHTMODE.PLAYER_ATTACK;
            //コマンドボタンを使用不可にする
            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
        }

    }

    //「道具」ボタンクリック時の処理を行う関数
    public void ItemButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        if (player.GetItemBoxCount() > 0)
        {
            //アイテムを1つでも持っているときはアイテムウィンドウを開く
            itemPage = 1;
            ItemWindowOpen();
        }
        else
        {
            //何も持っていないときはアイテムウィンドウを開かずメッセージを表示する
            MessageController.msgController.MessageDisp("何も持っていない。\n");
        }

    }

    //「強さ」ボタンクリック時の処理を行う関数
    public void StatusButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ステータスウィンドウを開く
        StatusWindowOpen();
    }

    //「逃げる」ボタンクリック時の処理を行う関数
    public void EscapeButtonClick()
    {
        //戦闘中のみ使用可能
        if (fightFlag == true)
        {
            //ボタンをクリックしたときの効果音を鳴らす
            SoundManager.soundManager.PlaySE("se_decision");

            //戦闘に関する状態を「プレイヤー逃走」に変更
            fightMode = FIGHTMODE.PLAYER_ESCAPE;
        }
    }

    //「保存／終了」ボタンクリック時の処理を行う関数
    public void SaveButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //コマンドボタンを使用不可にしてセーブウィンドウを開く
        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.SAVE);
        SaveWindowOpen();
        MessageController.msgController.MessageDisp("何番にセーブしますか？\n");
    }

    //ステータスウィンドウの「閉じる」ボタンがクリックされた時の処理を行う関数
    public void StCloseButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //ステータスウィンドウを閉じる
        StatusWindowClose();
    }
    //アイテムウィンドウの「閉じる」ボタンがクリックされた時の処理を行う関数
    public void ItemCloseButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //アイテムウィンドウを閉じる
        ItemWindowClose();

        //戦闘中及びNPC遭遇時でないとき
        if (fightFlag == false && npcFlag == false)
        {
            //メッセージウィンドウを空白にする
            MessageController.msgController.MessageDisp("");
        }
    }

    //アイテムウィンドウの「前ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void ItemPreviousButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ページ数を1減らす
        itemPage--;
        //最初のページの時はそのままにする
        if (itemPage < 1)
        {
            itemPage = 1;
        }
        //ウィンドウの内容を更新する
        ItemWindowDisp();
    }

    //アイテムウィンドウの「次ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void ItemNextButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ページ数を1増やす
        itemPage++;
        //最後のページの時はそのままにする
        if (itemPage > itemPageMax)
        {
            itemPage = itemPageMax;
        }
        //ウィンドウの内容を更新する
        ItemWindowDisp();
    }

    //マップの「閉じる」ボタンがクリックされた時の処理を行う関数
    public void MapCloseButtonClick()
    {
        //マップが開いてないときは何もしない（この処理を入れないと、ボタンがある場所をクリックするとエラーが出るため）
        if (GameObject.FindGameObjectWithTag("CameraController").GetComponent<CameraController>().MapOpenCheck() == false)
        {
            return;
        }

        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //マップを閉じて、小マップを開く　※小マップ用アイテムを所持しているときのみ
        bool mapflag = GameObject.FindGameObjectWithTag("PlayerCharacter").GetComponent<PlayerController>().miniMapFlag;
        GameObject.FindGameObjectWithTag("CameraController").GetComponent<CameraController>().MapClose(mapflag);

        //コマンドボタンを使用可能にする
        GameObject companel = GameObject.Find("CommandPanel");
        companel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.NORMAL);

        //ダークゾーン用のイメージオブジェクトを表示（透明状態）にする
        //※これを設定しないとダークゾーンの処理とフェード処理が正しく動作しなくなるため
        GameObject ob = GameObject.FindGameObjectWithTag("PlayerCharacter");
        GameObject darkzone = ob.transform.Find("Canvas/DarkZoneImage").gameObject;
        darkzone.SetActive(true);
    }

    //逃走成功時の処理を行う関数
    void Escaping()
    {
        MessageController.msgController.MessageDisp("なんとか逃げ延びた！\n");
    }

    //逃走失敗時の処理を行う関数
    void EscapeFailed()
    {
        //敵の種類によって逃走失敗のメッセージを変える
        if (fightEnemy.enemyType == Enemy.ENEMY_TYPE.BOSS ||
            fightEnemy.enemyType == Enemy.ENEMY_TYPE.FINAL_BOSS)
        {
            //中ボスもしくは最終ボスの時
            MessageController.msgController.MessageDisp("この敵からは逃げられない！\n");
        }
        else
        {
            //普通の敵の時
            MessageController.msgController.MessageDisp("しかし、回り込まれてしまった！\n");
        }
       
        //逃走回数の増加
        player.EscapeCountAdd();
    }

    //逃走失敗後、敵のターンへ移行する処理を行う関数
    void EscapeFailedReturn()
    {
        //戦闘に関する状態を敵の状態チェックに移行する
        fightMode = FIGHTMODE.E_CONDITION_CHECK;
    }

    //逃走成功による戦闘終了の処理を行う関数
    void EscapeEnd()
    {
        //逃走回数の初期化
        player.EscapeCountReset();

        //状態が毒と祝福以外の時はプレイヤーの状態をOKにする
        if (player.playerCondition != Player.PLAYER_CONDITION.POISON && 
            player.playerCondition != Player.PLAYER_CONDITION.BLESSING)
        {
            player.ChangeCondition(Player.PLAYER_CONDITION.OK);
        }

        //戦闘状態から移動可能状態へ移行する
        movableFlag = true;
        fightFlag = false;
        fightMode = FIGHTMODE.NONE_FIGHT;
        //ステータスウィンドウ（小）とコマンドパネルを閉じる
        statusPanel.SetActive(false);
        commandPanel.SetActive(false);
        //イベント画像とメッセージウィンドウを閉じる
        EventImageController.evImageController.ImagePanelClose();
        MessageController.msgController.MessageDisp("");
        MessageController.msgController.MessagePanelClose();
    }

    //ダメージ計算関数
    //引数
    //target:標的フラグ（true:プレイヤー→敵、false:敵→プレイヤー）
    //critical:会心の一撃フラグ（true:会心の一撃、false:普通のダメージ）
    //戻り値（ダメージ数）
    int DamageCheck(Player player, Enemy enemy, bool target, bool critical)
    {
        float damage;
        if (target == true)
        {
            //プレイヤー→敵のとき
            if (critical == true)
            {
                //会心の一撃
                damage = player.playerAttack * 2.0f / 3.0f;
            }
            else
            {
                //普通のダメージ
                damage = (player.playerAttack / 2.0f) - (enemy.enemyDefense / 4.0f);
            }

        }
        else
        {
            //敵→プレイヤー
            if (critical == true)
            {
                //痛恨の一撃
                damage = enemy.enemyAttack * 2.0f / 3.0f;
            }
            else
            {
                //普通のダメージ
                damage = (enemy.enemyAttack / 2.0f) - (player.playerDefense / 4.0f);
            }

        }

        if (damage < 0.0f)
        {
            //ダメージが0未満になった時は0を入れる
            damage = 0.0f;
        }

        //乱数を使用してダメージにランダム性を持たせる
        float rnd = Random.Range(-20.0f, 20.0f);
        float f_correct = 0.0f;

        //調整値を算出する
        if (damage == 0.0f)
        {
            //ダメージが0の時
            f_correct = Random.Range(0.0f, 1.0f);
        }
        else
        {
            //ダメージが0以外の時
            f_correct = damage * rnd / 100.0f;
        }

        //乱数で算出した数値をダメージに加算する
        damage += f_correct;
        //ダメージを小数点第1位で四捨五入する
        damage = (float)System.Math.Round(damage, 0, System.MidpointRounding.AwayFromZero);

        if (damage < 0.0f)
        {
            //ダメージが0未満になった時は0を入れる
            damage = 0.0f;
        }

        //ダメージを返す
        return (int)damage;
    }

    //現在のHPをチェックする関数
    //引数
    //hp:現在のHP
    //戻り値（チェック後のHP）
    int HpCheck(int hp)
    {
        if (hp < 0)
        {
            //0未満の時は0を返す
            return 0;
        }

        //1以上の時は現在のHPを返す
        return hp;
    }

    //プレイヤー死亡時の処理の関数
    void PlayerDead()
    {
        //プレイヤーの行動に関する状態を「ゲームオーバー画面への移動直前」に移行する
        actMode = ACTMODE.GAME_OVER;
    }

    //プレイヤー勝利時の処理の関数
    void PlayerWin()
    {
        //アイテム獲得フラグ
        bool get_flag = false;

        //敵の画像を消す
        EventImageController.evImageController.ImagePanelClose();
        //経験値およびゴールドの獲得メッセージを表示後、経験値およびゴールドの獲得処理を行う
        MessageController.msgController.MessageDisp(fightEnemy.enemyName +
                                                    "を倒した！！\n" + "経験値" + fightEnemy.enemyExp + "、" +
                                                    fightEnemy.enemyGold + "ゴールドを手に入れた！！\n");
        player.ExpGoldGet(fightEnemy);

        //獲得アイテムチェック
        if (fightEnemy.enemyItemId > 0)
        {
            //敵がアイテムを持っているとき

            //獲得アイテムの情報を取得
            Item item = new Item();
            item = itemGenerator.GetItemInfo(fightEnemy.enemyItemId);

            //獲得アイテムの画像を表示する
            GetItemImageDisp(item.itemId);

            //アイテム取得処理
            Player.PICK_COMPLETION pick = player.ItemPick(item);
            if (pick == Player.PICK_COMPLETION.NG_ITEM_MAX ||
                pick == Player.PICK_COMPLETION.NG_BOX_MAX)
            {
                //見つけた使い捨てアイテムの現在数量が最大所持数量以上の時、またはプレイヤーのアイテム所持数が最大の時、
                //アイテムを取得せずメッセージを表示する
                MessageController.msgController.MessageJoinDisp(item.itemName + "を手に入れたが、" +
                                                                    "これ以上持てない！\n");
                MessageController.msgController.MessageJoinDisp("持ち物を整理してから再びこの場所に来よう！\n");

            }
            else if (pick == Player.PICK_COMPLETION.OK)
            {
                //上の条件に該当しないときは、アイテムを取得しメッセージを表示する
                MessageController.msgController.MessageJoinDisp(item.itemName + "を手に入れた！！\n");

                //アイテム獲得フラグをオンにする
                get_flag = true;

            }

        }

        //イベントオブジェクトの初期化
        EventObject eo = new EventObject();

        //現在地のイベント情報の取得
        eo = eventController.GetEventInfo(player.nowFloor, player.nowPosX, player.nowPosZ);

        if (get_flag == false)
        {
            //以下の条件に1つでも該当する時のフラグ変更処理を行う
            //1:イベントでアイテムを持っていない敵を倒したとき
            //2:イベントでアイテムを持持っている敵を倒したが、アイテムを入手できなかったとき
            if (eo.eventType == EventController.EVTYPE.NPC)
            {
                //NPCのとき

                if (player.nowFloor == 1)
                {
                    //地下1階の時
                    if (eo.eventNumber == 1)
                    {
                        //魔法の地図の情報を持つ冒険者
                        eo.eventFlag = 1;
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //鍵売りの老人
                        eo.eventFlag = 2;
                    }
                }
                else if (player.nowFloor == 2)
                {
                    //地下2階の時
                    if (eo.eventNumber == 1)
                    {
                        //ミストリル鉱石の情報を持つ冒険者
                        eo.eventFlag = 1;
                    }
                }
                else if (player.nowFloor == 3)
                {
                    //地下3階の時
                    if (eo.eventNumber == 1)
                    {
                        //衛兵
                        eo.eventFlag = 1;
                    }
                    else if (eo.eventNumber == 2)
                    {
                        //地下3階の冒険者
                        eo.eventFlag = 1;
                    }
                }
                else if (player.nowFloor == 4)
                {
                    //地下4階の時
                    if (eo.eventNumber == 1)
                    {
                        //雷の杖を持っている魔法使い
                        eo.eventFlag = 2;
                    }
                }
                else if (player.nowFloor == 5)
                {
                    //地下5階の時
                    if (eo.eventNumber == 1)
                    {
                        //謎の人物
                        eo.eventFlag = 2;
                    }
                }
                else if (player.nowFloor == 7)
                {
                    //地下7階の時
                    if (eo.eventNumber == 1)
                    {
                        //死神
                        eo.eventFlag = 1;
                    }
                }

            }
            else if (eo.eventType == EventController.EVTYPE.ENEMY)
            {
                //固定敵の時

                if (player.nowFloor == 6)
                {
                    //地下6階の時
                    if (eo.eventNumber == 1)
                    {
                        //ゴーレム
                        eo.eventFlag = 1;
                    }
                }
            }
            else if (eo.eventType == EventController.EVTYPE.OTHER)
            {
                //その他
                if (player.nowFloor == 7)
                {
                    //地下7階の時
                    if (eo.eventNumber == 6 || eo.eventNumber == 7)
                    {
                        //サキュバス
                        eo.eventFlag = 1;
                    }
                    else if (eo.eventNumber == 8)
                    {
                        //姫
                        eo.eventFlag = 1;
                    }
                }
                
            }
        }
        else
        {
            //イベントでアイテムを持っている敵を倒し、アイテムを入手したときのフラグ変更処理を行う
            if (eo.eventType == EventController.EVTYPE.NPC)
            {
                //NPCのとき
                if (player.nowFloor == 1)
                {
                    //地下1階の時

                    if (eo.eventNumber == 2)
                    {
                        //鍵売りの老人
                        eo.eventFlag = 1;
                    }
                }
                else if (player.nowFloor == 4)
                {
                    //地下4階
                    if (eo.eventNumber == 1)
                    {
                        //雷の杖を持っている魔法使い
                        eo.eventFlag = 1;
                    }
                }
                else if (player.nowFloor == 5)
                {
                    //地下5階
                    if (eo.eventNumber == 1)
                    {
                        //謎の人物
                        eo.eventFlag = 1;
                    }
                }

            }
            else if (eo.eventType == EventController.EVTYPE.ENEMY)
            {
                //固定敵の時
            }
        }

        //姫を倒してしまったときは、街のイベントフラグ（姫）を3（姫死亡）に変更する
        if (player.nowFloor == 7)
        {
            if (eo.eventNumber == 8)
            {
                eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS, 3);
            }
        }

        //カルマの増加
        if (player.nowFloor == 1)
        {
            //地下1階の時
            if (eo.eventNumber == 1)
            {
                //魔法の地図の情報を持つ冒険者
                player.KarmaAddSub(1);
            }
            else if (eo.eventNumber == 2)
            {
                //鍵売りの老人
                player.KarmaAddSub(1);
            }
        }
        else if (player.nowFloor == 2)
        {
            //地下2階の時
            if (eo.eventNumber == 1)
            {
                //ミストリル鉱石の情報を持つ冒険者
                player.KarmaAddSub(1);
            }
        }
        else if (player.nowFloor == 3)
        {
            //地下3階の時
            if (eo.eventNumber == 2)
            {
                //地下3階の冒険者
                player.KarmaAddSub(1);
            }
        }
        else if (player.nowFloor == 4)
        {
            //地下4階の時
            if (eo.eventNumber == 1)
            {
                //雷の杖を持っている魔法使い
                player.KarmaAddSub(1);
            }
        }
        else if (player.nowFloor == 5)
        {
            //地下5階の時
            if (eo.eventNumber == 1)
            {
                //謎の人物
                player.KarmaAddSub(2);
            }
        }
        else if (player.nowFloor == 7)
        {
            //地下7階の時
            if (eo.eventNumber == 8)
            {
                //姫
                player.KarmaAddSub(5);
            }
        }

    }

    //戦闘終了時の処理を行う関数
    void FightEnd()
    {
        //戦闘状態から移動可能状態へ移行する
        movableFlag = true;
        fightFlag = false;
        fightMode = FIGHTMODE.NONE_FIGHT;
        //メッセージウィンドウを閉じる
        MessageController.msgController.MessageDisp("");
        MessageController.msgController.MessagePanelClose();
        //ステータスウィンドウ（小）およびコマンドパネルを閉じる
        statusPanel.SetActive(false);
        commandPanel.SetActive(false);
        //イベント画像を閉じる
        EventImageController.evImageController.ImagePanelClose();
    }

    //タイトル画面に移行する処理を行う関数
    void GoTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    //ゲームオーバー画面（エンディング画面を流用）に移行する処理を行う関数
    void GoGameOver()
    {
        //エンディングフラグを0（ゲームオーバー）に設定する
        GameDataController.SetEndingFlag(0);
        //エンディング画面に移行する
        SceneManager.LoadScene("EndingScene");
    }

    //エンディング直前のイベントの処理を行う関数
    void EndingJustBeforeEvent()
    {
        //最初のメッセージを作成する
        string msg = "ついに、" + fightEnemy.enemyName + "を倒した！\n" +
                     "これで、この国に平和が戻るだろう。\n" +
                     "さあ、地上に帰ろう！\n";

        if (player.playerKarma < 7)
        {
            //カルマが7未満の時

            MessageController.msgController.MessageDisp(msg);

            //少ししたらエンディングに移行する
            Invoke("GoEnding", 5.0f);
        }
        else
        {
            //カルマが7以上の時

            //専用イベント      
            switch (eventCount)
            {
                case 0:
                    MessageController.msgController.MessageDisp(msg);
                    eventCount++;
                    break;
                case 1:
                    if (CommonMethod.TimeWait(5.0f) == true)
                    {
                        eventCount++;
                    }
                    break;
                case 2:
                    msg = "突然、頭の中に何者かの声が響いた。\n" +
                            "声の主は倒したはずの" + fightEnemy.enemyName + "だ。\n" +
                            "その直後、体が動かなくなった！\n";
                    MessageController.msgController.MessageDisp(msg);
                    eventCount++;
                    break;
                case 3:
                    if (CommonMethod.TimeWait(5.0f) == true)
                    {
                        eventCount++;
                    }
                    break;
                case 4:
                    msg = "お前のような、心に深い闇を持つものを\n" +
                            "待っていた・・・。\n" +
                            "お前の体をよこせ・・・。\n" +
                            "さあ、私と一つになるのだ！！\n";
                    MessageController.msgController.MessageDisp(msg);
                    eventCount++;
                    break;
                case 5:
                    if (CommonMethod.TimeWait(5.0f) == true)
                    {
                        eventCount++;
                    }
                    break;
                case 6:
                    msg = "その声に抗う術はなく、\n" +
                            "次第に意識が遠のいていった・・・。\n";
                    MessageController.msgController.MessageDisp(msg);
                    eventCount++;
                    break;
                case 7:
                    BlackOut();
                    eventCount++;
                    break;
                default:
                    if (CommonMethod.TimeWait(5.0f) == true)
                    {
                        //イベント終了後エンディングに移行
                        GoEnding();
                    }
                    break;
            }
        }

        
    }

    //エンディングの分岐を行う関数
    void EndingBranchOut()
    {
        if (player.playerKarma >= 7)
        {
            //カルマが7以上の時
            GameDataController.SetEndingFlag(5);
        }
        else
        {
            //カルマが6以下の時

            if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) >= 1 &&
                eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) <= 2)
            {
                //姫を救出している時
                if (player.playerKarma == 0)
                {
                    //カルマが0の時
                    GameDataController.SetEndingFlag(1);
                }
                else if (player.playerKarma >= 1 && player.playerKarma <= 3)
                {
                    //カルマが1以上かつ3以下の時
                    GameDataController.SetEndingFlag(2);
                }
                else if (player.playerKarma >= 4 && player.playerKarma <= 6)
                {
                    //カルマが4以上かつ6以下の時
                    GameDataController.SetEndingFlag(3);
                }
            }
            else
            {
                //姫を救出していない時
                if (player.playerKarma >= 0 && player.playerKarma <= 2)
                {
                    //カルマが0以上かつ2以下の時
                    GameDataController.SetEndingFlag(2);
                }
                else if (player.playerKarma >= 3 && player.playerKarma <= 4)
                {
                    //カルマが3以上かつ4以下の時
                    GameDataController.SetEndingFlag(3);
                }
                else if (player.playerKarma >= 5 && player.playerKarma <= 6)
                {
                    //カルマが5以上かつ6以下の時
                    GameDataController.SetEndingFlag(4);
                }
            }
        }
    }

    //エンディングに移行する処理を行う関数
    void GoEnding()
    {
        //エンディング分岐を行う
        EndingBranchOut();
        //エンディング画面に移行する
        SceneManager.LoadScene("EndingScene");
    }

    //戦闘場面の関数
    void FightProcess()
    {
        //戦闘場面の状態によって処理を分岐させる
        switch (fightMode)
        {
            case FIGHTMODE.FIGHT_BGM_PLAY:  //戦闘場面音声再生
                if (SoundManager.soundManager.SEPlayingCheck() == false)
                {
                    //BGMを再生する
                    PlayFightBGM();

                    //戦闘開始に移行する
                    fightMode = FIGHTMODE.FIGHT_START;
                }
                break;
            case FIGHTMODE.FIGHT_START:     //戦闘開始
                if (fightWaitFlag == false)
                {
                    //待機状態でないとき
                    if (fightTurn == true)
                    {
                        //プレイヤーのターンの時
                        //プレイヤーのコマンド選択へ移行する
                        fightMode = FIGHTMODE.PLAYER_WAIT;
                        //コマンドボタンを戦闘中状態で使用可能にする
                        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);
                        MessageController.msgController.MessageDisp("コマンド\n");
                    }
                    else
                    {
                        //敵のターンの時は敵の状態チェックに移行する
                        fightMode = FIGHTMODE.E_CONDITION_CHECK;
                    }

                }
                else
                {
                    //待機状態の時
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき
                        //待機状態を解除する
                        fightWaitFlag = false;
                    }
                }
                break;
            case FIGHTMODE.ENEMY_TALK:      //戦闘開始時の敵の会話
                if (fightWaitFlag == false)
                {
                    //待機状態でない時
                    switch (eventCount)
                    {
                        case 0:
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //戦闘開始の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_battle_start");
                                //BGMを停止する
                                SoundManager.soundManager.StopBGM(0.5f);

                                if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.F_PRINCESS)
                                {
                                    //偽者の姫の場合は敵の情報ををサキュバスの物に変更する
                                    movableFlag = false;
                                    fightFlag = true;
                                    fightEnemy = null;
                                    fightEnemy = new Enemy();
                                    fightEnemy.EnemyDataSet(enemyGenerator.GetEnemy((int)EnemyGenerator.F_ENEMY_TALK.SUCCUBUS));
                                    //敵の画像を変更する
                                    EventImageController.evImageController.ImageDisp(fightEnemy.enemyImg);
                                }
                                else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.LAST_BOSS)
                                {
                                    //魔王の場合は攻撃力と会心の一撃の確率を上げる
                                    movableFlag = false;
                                    fightFlag = true;
                                    fightEnemy.enemyAttack = (int)(fightEnemy.enemyAttack * 1.2f);
                                    fightEnemy.enemyCritical += 5;
                                }

                                eventCount++;
                            }
                            break;
                        case 1:
                            //敵の会話を表示する
                            FightStartEnemyTalk();

                            //小マップを閉じる
                            cameraController.MapClose(false);

                            eventCount++;
                            break;
                        default:
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //BGMを再生する
                                PlayFightBGM();

                                //カウンタの初期化
                                eventCount = 0;

                                //待機状態にする
                                fightWaitFlag = true;
                            }
                            break;
                    }
                }
                else
                {
                    //待機状態の時
                    //待機状態から指定時間が経過したとき
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //次の移行先を設定
                        fightMode = nextFightMode;

                        //待機状態の解除
                        fightWaitFlag = false;
                    }
                }
                break;
            case FIGHTMODE.P_CONDITION_CHECK:     //プレイヤーの状態チェック
                if (fightWaitFlag == false)
                {
                    //待機状態でないとき
                    if (player.playerCondition == Player.PLAYER_CONDITION.SLEEP)
                    {
                        //睡眠状態の時
                        //状態回復判定を行う
                        if (player.ConditionRecoverCheck() == true)
                        {
                            //状態回復に成功したとき
                            //状態を「OK」にする
                            player.ChangeCondition(Player.PLAYER_CONDITION.OK);
                            //状態異常経過ターン数を初期化する
                            player.AbConditionReset();
                            //プレイヤーのコマンド選択へ移行し、コマンドボタンを戦闘中状態で使用可能にする
                            fightMode = FIGHTMODE.PLAYER_WAIT;
                            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);

                            MessageController.msgController.MessageDisp(player.playerName + "は目を覚ました！\nコマンド\n");
                        }
                        else
                        {
                            //状態回復に失敗したとき
                            //状態異常経過ターン数を1増やす
                            player.AbConditionCount();
                            //待機状態にする
                            fightWaitFlag = true;

                            MessageController.msgController.MessageDisp(player.playerName + "は眠っている！");
                        }
                    }
                    else if (player.playerCondition == Player.PLAYER_CONDITION.SEALED)
                    {
                        //封印状態の時
                        //状態回復判定を行う
                        if (player.ConditionRecoverCheck() == true)
                        {
                            //状態回復に成功したとき
                            //状態を「OK」にし、状態異常経過ターン数を初期化する
                            player.ChangeCondition(Player.PLAYER_CONDITION.OK);
                            player.AbConditionReset();
                            //プレイヤーのコマンド選択へ移行し、コマンドボタンを戦闘中状態で使用可能にする
                            fightMode = FIGHTMODE.PLAYER_WAIT;
                            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);

                            MessageController.msgController.MessageDisp(player.playerName + "の封印が解けた！\nコマンド\n");
                        }
                        else
                        {
                            //状態回復に失敗したとき
                            //状態異常経過ターン数を1増やす
                            player.AbConditionCount();
                            //プレイヤーのコマンド選択へ移行し、コマンドボタンを戦闘中状態で使用可能にする
                            fightMode = FIGHTMODE.PLAYER_WAIT;
                            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);

                            MessageController.msgController.MessageDisp(player.playerName + "は魔法を封印されている！\nコマンド\n");
                        }
                    }
                    else if (player.playerCondition == Player.PLAYER_CONDITION.POISON)
                    {
                        //毒状態の時
                        //毒によるダメージの取得
                        int damage = player.PoisonDamage(fightFlag);
                        //プレイヤーの死亡判定
                        if (player.DeadCheck() == true)
                        {
                            //死亡したとき
                            MessageController.msgController.MessageDisp(player.playerName + "は毒により、" + damage + "のダメージ！\n");
                            //待機状態にする
                            fightWaitFlag = true;
                        }
                        else
                        {
                            //死亡していないとき
                            MessageController.msgController.MessageDisp(player.playerName + "は毒により、" + damage + "のダメージ！\nコマンド\n");
                            //プレイヤーのコマンド選択へ移行し、コマンドボタンを戦闘中状態で使用可能にする
                            fightMode = FIGHTMODE.PLAYER_WAIT;
                            commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);
                        }
                    }
                    else if (player.playerCondition == Player.PLAYER_CONDITION.BLESSING)
                    {
                        //祝福状態の時
                        //祝福によるHPの回復
                        player.BlessingHpHealFight();
                        MessageController.msgController.MessageDisp(player.playerName + "は祝福により、体力が回復した！\nコマンド\n");
                        //プレイヤーのコマンド選択へ移行し、コマンドボタンを戦闘中状態で使用可能にする
                        fightMode = FIGHTMODE.PLAYER_WAIT;
                        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);
                    }
                    else
                    {
                        //状態がOKのとき
                        //プレイヤーのコマンド選択へ移行し、コマンドボタンを戦闘中状態で使用可能にする
                        fightMode = FIGHTMODE.PLAYER_WAIT;
                        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.FIGHT);
                        MessageController.msgController.MessageDisp("コマンド\n");
                    }
                }
                else
                {
                    //待機状態のとき
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき
                        //プレイヤーの死亡判定を行う
                        if (player.DeadCheck() == true)
                        {
                            //死亡時はプレイヤー死亡へ移行する
                            fightMode = FIGHTMODE.PLAYER_DEAD;
                        }
                        else
                        {
                            //死亡時でないときは敵の状態チェックへ移行する
                            fightMode = FIGHTMODE.E_CONDITION_CHECK;
                        }

                        //待機状態の解除
                        fightWaitFlag = false;
                    }
                }
                break;
            case FIGHTMODE.PLAYER_WAIT:          //プレイヤーのコマンド選択（ここでは何もしない）
                break;
            case FIGHTMODE.PLAYER_ATTACK:        //プレイヤーの攻撃
                if (fightWaitFlag == false)
                {
                    //待機状態でないときは敵への攻撃処理を行った後、待機状態にする
                    switch (eventCount)
                    {
                        case 0:
                            //メッセージの初期化
                            dungeonMessage = "";
                            //会心の一撃フラグの取得
                            criticalFlag = player.CriticalAttackCheck();
                            //敵に与えるダメージの取得
                            playerAttackDamage = DamageCheck(player, fightEnemy, true, criticalFlag);

                            eventCount++;
                            break;
                        case 1:
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //プレイヤーの攻撃エフェクト
                                EventImageController.evImageController.PlayEffect(EventImageController.ATTACK);
                                //プレイヤーの攻撃の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_player_attack");
                                //プレイヤーの攻撃のメッセージを表示
                                dungeonMessage = player.playerName + "の攻撃！\n";
                                MessageController.msgController.MessageDisp(dungeonMessage);

                                eventCount++;
                            }
                            break;
                        case 2:
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //プレイヤーの攻撃の効果音が終了後、次の処理を行う
                                if (fightEnemy.AttackAvoidCheck(player) == true)
                                {
                                    //敵の攻撃回避が成功したとき

                                    //攻撃回避の効果音を再生する
                                    SoundManager.soundManager.PlaySE("se_attack_avoid");
                                    //攻撃回避のメッセージを表示する
                                    dungeonMessage = fightEnemy.enemyName + "は素早く身をかわした！\n";
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                    //プレイヤーの攻撃で敵に与えるダメージを0にする
                                    playerAttackDamage = 0;
                                }
                                else
                                {
                                    //敵の攻撃回避が失敗したとき
                                    if (playerAttackDamage > 0)
                                    {
                                        //プレイヤーの攻撃命中エフェクト
                                        EventImageController.evImageController.PlayEffect(EventImageController.HIT);

                                        //ダメージが1以上の時
                                        if (criticalFlag == true)
                                        {
                                            //会心の一撃

                                            //攻撃命中の効果音を再生する
                                            SoundManager.soundManager.PlaySE("se_critical_hit");

                                            //会心の一撃のメッセージを格納
                                            dungeonMessage = "会心の一撃！！\n";
                                        }
                                        else
                                        {
                                            //通常のダメージ

                                            //攻撃命中の効果音を再生する
                                            SoundManager.soundManager.PlaySE("se_attack_hit");

                                            //メッセージの初期化
                                            dungeonMessage = "";
                                        }

                                        //攻撃命中メッセージを格納
                                        dungeonMessage = dungeonMessage + fightEnemy.enemyName + "に" + playerAttackDamage + "のダメージ！\n";

                                        //装備武器情報の取得
                                        Item item = itemGenerator.GetItemInfo(player.equipArray[itemGenerator.GetEquipNumber(ItemGenerator.ITEM_TYPE.WEAPON)]);

                                        //装備武器が毒を持っているとき
                                        if (item.itemEffect == ItemGenerator.ITEM_EFFECT_TYPE.POISON_ATTACK)
                                        {
                                            //乱数の取得
                                            int rnd = Random.Range(0, Player.EFFECT_PAR_MAX);

                                            //敵を毒状態にする確率の取得
                                            int per = player.GetEffectPercent(fightEnemy, item.itemEffect);

                                            //敵の状態がOKで、乱数が毒状態になる確率未満の時は敵の状態を毒にする
                                            if (rnd < per && fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.OK)
                                            {
                                                fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.POISON);
                                                //敵が毒に侵されたメッセージ格納
                                                dungeonMessage = dungeonMessage + fightEnemy.enemyName + "は毒に侵された！\n";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //ダメージが0の時
                                        //ミスの効果音を再生する
                                        SoundManager.soundManager.PlaySE("se_miss");

                                        //ミスのメッセージを格納する
                                        dungeonMessage = "ミス！\n" + fightEnemy.enemyName + "にダメージを与えられない！\n";
                                    }

                                    //攻撃命中メッセージを表示する
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);

                                    //敵のHPをダメージの分減少させる
                                    fightEnemy.DeclineHp(playerAttackDamage);
                                }
                                eventCount++;
                            }
                            
                            break;
                        default:
                            //カウンタの初期化
                            eventCount = 0;
                            //待機状態にする
                            fightWaitFlag = true;
                            //メッセージの初期化
                            dungeonMessage = "";
                            break;
                    }
                }
                else
                {
                    //待機状態の時
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき
                        //敵の死亡判定を行う
                        if (fightEnemy.DeadCheck() == true)
                        {
                            //死亡時はプレイヤー勝利へ移行する
                            fightMode = FIGHTMODE.PLAYER_WIN;
                        }
                        else
                        {
                            //死亡時でないときは敵の状態チェックへ移行する
                            fightMode = FIGHTMODE.E_CONDITION_CHECK;
                        }
                        //待機状態の解除
                        fightWaitFlag = false;
                    }
                }
                break;
            case FIGHTMODE.PLAYER_ITEM:     //プレイヤーのアイテム使用
                if (fightWaitFlag == false)
                {
                    //待機状態でないときはアイテム使用処理を行った後、待機状態にする
                    switch (eventCount)
                    {
                        case 0:
                            //効果音の再生が終了したときに戦闘中のアイテム使用処理を開始する
                            if(SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //メッセージの初期化
                                dungeonMessage = "";

                                //装備変更前に装備していたアイテムの所持アイテムリスト番号の初期化
                                preEquipBoxNumber = -1;
                                //対象アイテムのID取得
                                useItemId = player.GetItemBoxIndex(itemBoxNum).itemId;
                                //装備アイテム種類番号の初期化
                                equipNumber = -1;

                                //対象アイテムのデータ取得
                                useItem = new Item();
                                for (int i = 0; i < itemList.Count; i++)
                                {
                                    if (useItemId == itemList[i].itemId)
                                    {
                                        useItem = itemList[i];
                                        break;
                                    }
                                }

                                eventCount++;
                            }
                            break;
                        case 1:
                            //アイテム使用のメッセージを表示する（装備アイテムの場合は装備処理も行う）

                            //アイテム使用メッセージの共通部分を格納
                            dungeonMessage = player.playerName + "は";

                            if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE)
                            {
                                //一般使い捨てアイテムの時
                                dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\n";

                                eventCount++;
                            }
                            else if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL)
                            {
                                //一般アイテムの時
                                if(HpCheck(player.playerHp - useItem.hpCost) > 0)
                                {
                                    //魔法アイテム使用時の消費HPがプレイヤーの現在HP未満の時
                                    dungeonMessage = dungeonMessage + useItem.itemName + "を使った！\n";

                                    eventCount++;
                                }
                                else
                                {
                                    //魔法アイテム使用時の消費HPがプレイヤーの現在HP以上の時
                                    dungeonMessage = dungeonMessage + useItem.itemName + "を使おうとしたが\n" + "HPが足りなかった！\n";
                                    //カウンタを終了処理へと進める
                                    eventCount = 99;

                                }
                            }
                            else if (useItem.itemType > ItemGenerator.ITEM_TYPE.EQUIP && useItem.itemType < ItemGenerator.ITEM_TYPE.EQUIP_END)
                            {
                                //装備アイテムの時
                                //装備変更前に装備していたアイテムの所持アイテムリスト番号の取得
                                preEquipBoxNumber = player.GetPreEquipNumber(useItem.itemType);
                                //これから装備する装備アイテム種類番号の取得
                                equipNumber = itemGenerator.GetEquipNumber(useItem.itemType);

                                if (preEquipBoxNumber == -1)
                                {
                                    //何も装備されていないとき
                                    //選択した装備アイテムの装備処理を実行する
                                    player.Equip(useItem, itemBoxNum);
                                    dungeonMessage = dungeonMessage + useItem.itemName + "を装備した！\n";
                                }
                                else
                                {
                                    if (itemBoxNum == preEquipBoxNumber)
                                    {
                                        //現在装備中のアイテムが選択されたとき
                                        //装備解除の処理を実行する
                                        player.UnEquip(useItem, itemBoxNum);
                                        dungeonMessage = dungeonMessage + useItem.itemName + "を装備から外した！\n";
                                    }
                                    else
                                    {
                                        //既に別の装備アイテムが装備されていた時
                                        //前の装備アイテムの装備を解除する
                                        Item unEquipItem = itemGenerator.GetItemInfo(player.GetItemBoxIndex(preEquipBoxNumber).itemId);
                                        player.UnEquip(unEquipItem, preEquipBoxNumber);
                                        //選択した装備アイテムの装備処理を実行する
                                        player.Equip(useItem, itemBoxNum);
                                        dungeonMessage = dungeonMessage + useItem.itemName + "を装備した！\n";
                                    }
                                }
                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            else
                            {
                                //その他のアイテム
                                dungeonMessage = dungeonMessage + useItem.itemName + "を使おうとしたが\n" + "今は使用する必要がない！\n";
                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }

                            //アイテム使用のメッセージを表示する
                            MessageController.msgController.MessageDisp(dungeonMessage);

                            break;
                        case 2:
                            //アイテム使用メッセージの続きの表示および使用時の効果音再生、使用時の効果の処理を行う
                            if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE)
                            {
                                
                                //一般使い捨てアイテムの時
                                if (useItem.itemRecover > 0)
                                {
                                    //HP回復アイテムの時

                                    //一定時間後にカウンタを次の処理へと進める
                                    if (CommonMethod.TimeWait(0.5f) == true)
                                    {
                                        eventCount++;
                                    }
                                }

                                if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.SEALED)
                                {
                                    //封印状態回復アイテムの時

                                    //一定時間後にカウンタを次の処理へと進める
                                    if (CommonMethod.TimeWait(0.5f) == true)
                                    {
                                        eventCount++;
                                    }
                                }

                                if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.POISON)
                                {
                                    //毒状態回復アイテムの時

                                    //一定時間後にカウンタを次の処理へと進める
                                    if (CommonMethod.TimeWait(0.5f) == true)
                                    {
                                        eventCount++;
                                    }
                                }

                            }
                            else if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL)
                            {
                                //一般アイテムの時

                                //プレイヤーのHPから消費HPを引く
                                player.DeclineHp(useItem.hpCost);

                                if (player.playerCondition != Player.PLAYER_CONDITION.SEALED)
                                {
                                    //プレイヤーの状態が封印でないとき

                                    //効果音とエフェクトを再生する
                                    if (useItem.itemAttack > 0)
                                    {
                                        //攻撃魔法アイテム
                                        if (useItem.itemId == (int)ItemGenerator.ITEM_ATTACK.MAGIC_WAND)
                                        {
                                            //魔法の杖
                                            EventImageController.evImageController.PlayEffect(EventImageController.FIRE_BALL);
                                            SoundManager.soundManager.PlaySE("se_fire_magic");
                                        }
                                        else if (useItem.itemId == (int)ItemGenerator.ITEM_ATTACK.THUNDER_STAFF)
                                        {
                                            //雷の杖
                                            EventImageController.evImageController.PlayEffect(EventImageController.THUNDER);
                                            SoundManager.soundManager.PlaySE("se_thunder_magic");
                                        }  
                                    }
                                    else
                                    {
                                        //特殊魔法アイテム
                                        switch (useItem.itemEffect)
                                        {
                                            case ItemGenerator.ITEM_EFFECT_TYPE.SLEEP:  //眠りの鈴
                                                EventImageController.evImageController.PlayEffect(EventImageController.SLEEP);
                                                SoundManager.soundManager.PlaySE("se_sleep_magic");
                                                break;
                                            case ItemGenerator.ITEM_EFFECT_TYPE.SEALED:  //魔封じの護符
                                                EventImageController.evImageController.PlayEffect(EventImageController.SEALED);
                                                SoundManager.soundManager.PlaySE("se_sealed_magic");
                                                break;
                                            case ItemGenerator.ITEM_EFFECT_TYPE.POISON:  //毒蛇の香
                                                EventImageController.evImageController.PlayEffect(EventImageController.POISON);
                                                SoundManager.soundManager.PlaySE("se_poison_magic");
                                                break;
                                            case ItemGenerator.ITEM_EFFECT_TYPE.DEATH:  //死の首飾り
                                                EventImageController.evImageController.PlayEffect(EventImageController.DEATH);
                                                SoundManager.soundManager.PlaySE("se_death_magic");
                                                break;
                                            case ItemGenerator.ITEM_EFFECT_TYPE.FINAL_SEALED:   //姫の指輪
                                                if (fightEnemy.enemyType == Enemy.ENEMY_TYPE.FINAL_BOSS)
                                                {
                                                    //最終ボスの時は効果音とエフェクトを再生する
                                                    EventImageController.evImageController.PlayEffect(EventImageController.SEALED);
                                                    SoundManager.soundManager.PlaySE("se_sealed_magic");
                                                }
                                                else
                                                {
                                                    //最終ボス以外の時は何もせず、一定時間後にメッセージを表示する処理へと移行する
                                                    eventCount++;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }

                                    //カウンタを次の処理へと進める
                                    eventCount++;
                                }
                                else
                                {
                                    //プレイヤーの状態が封印のときはメッセージを表示する
                                    dungeonMessage = "しかし、" + player.playerName + "は魔法を封じられている！\n";
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);

                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                            }
                            else
                            {
                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            break;
                        case 3:
                            //アイテム使用メッセージの続きの表示および使用時の効果音再生、使用時の効果の処理を行う
                            //（前の処理で終わらなかったとき）

                            //効果音の再生が終了したときに処理を開始する
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                
                                if (useItem.itemAttack > 0)
                                {
                                    //攻撃魔法アイテムの時

                                    //敵に与えるダメージを取得する
                                    playerAttackDamage = useItem.AttackItemDamage(fightEnemy);

                                    if (playerAttackDamage > 0)
                                    {
                                        //ダメージが1以上の時

                                        //プレイヤーの攻撃命中エフェクト
                                        EventImageController.evImageController.PlayEffect(EventImageController.HIT);

                                        //攻撃命中の効果音を再生する
                                        SoundManager.soundManager.PlaySE("se_attack_hit");

                                        //敵のHPからダメージを引く
                                        fightEnemy.DeclineHp(playerAttackDamage);
                                        dungeonMessage = fightEnemy.enemyName + "に" + playerAttackDamage + "のダメージ！\n";
                                    }
                                    else
                                    {
                                        //ダメージが0の時
                                        dungeonMessage = "しかし、" + fightEnemy.enemyName + "には効かなかった！\n";
                                    }
                                }
                                else if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE)
                                {
                                    //一般使い捨てアイテムの時
                                    if (useItem.itemRecover > 0)
                                    {
                                        //HP回復アイテムの時

                                        //回復の効果音とエフェクトを再生する
                                        LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.PLAYER_HEAL);
                                        SoundManager.soundManager.PlaySE("se_heal");

                                        //HP回復処理の実行
                                        player.RecoverHp(useItem.itemRecover);
                                        dungeonMessage = player.playerName + "の体力が回復した！\n";

                                        //女神の祝福を使用したときは状態を祝福にする
                                        if (useItem.itemRecover >= Item.FULL_HEAL)
                                        {
                                            player.ChangeCondition(Player.PLAYER_CONDITION.BLESSING);
                                            dungeonMessage = dungeonMessage + "そして、" + player.playerName + "は祝福を受けた！\n";
                                        }

                                        //アイテム数を1減らす
                                        player.BoxItemSubtraction(itemBoxNum);
                                    }

                                    if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.SEALED)
                                    {
                                        //封印状態回復アイテムの時
                                        if (player.playerCondition == Player.PLAYER_CONDITION.SEALED)
                                        {
                                            //プレイヤーが封印状態の時

                                            //回復の効果音とエフェクトを再生する
                                            LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.PLAYER_HEAL);
                                            SoundManager.soundManager.PlaySE("se_heal");

                                            //プレイヤーの状態をOKにする
                                            player.ChangeCondition(Player.PLAYER_CONDITION.OK);
                                            dungeonMessage = player.playerName + "の封印が解けた！\n";
                                        }
                                        else
                                        {
                                            //プレイヤーが封印状態でないとき
                                            dungeonMessage = "しかし、何も起こらなかった！\n";
                                        }

                                        //アイテム数を1減らす
                                        player.BoxItemSubtraction(itemBoxNum);
                                    }

                                    if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.POISON)
                                    {
                                        //毒状態回復アイテムの時
                                        if (player.playerCondition == Player.PLAYER_CONDITION.POISON)
                                        {
                                            //プレイヤーが毒状態の時

                                            //回復の効果音とエフェクトを再生する
                                            LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.PLAYER_HEAL);
                                            SoundManager.soundManager.PlaySE("se_heal");

                                            //プレイヤーの状態をOKにする
                                            player.ChangeCondition(Player.PLAYER_CONDITION.OK);
                                            dungeonMessage = player.playerName + "の毒が消えた！\n";
                                        }
                                        else
                                        {
                                            //プレイヤーが毒状態でないとき
                                            dungeonMessage = "しかし、何も起こらなかった！\n";
                                        }

                                        //アイテム数を1減らす
                                        player.BoxItemSubtraction(itemBoxNum);
                                    }

                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                                else
                                {
                                    //乱数の取得
                                    fightRandom = Random.Range(0, Player.EFFECT_PAR_MAX);

                                    //特殊魔法アイテムの時
                                    switch (useItem.itemEffect)
                                    {
                                        case ItemGenerator.ITEM_EFFECT_TYPE.SLEEP:  //眠りの鈴

                                            //魔法が効いたかどうかの判定を行う
                                            if (fightRandom < player.GetEffectPercent(fightEnemy, useItem.itemEffect) &&
                                                fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.OK)
                                            {
                                                //魔法が効いたときは敵の状態を「眠り」にする
                                                fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.SLEEP);
                                                dungeonMessage = fightEnemy.enemyName + "を眠らせた！\n";
                                            }
                                            else
                                            {
                                                //魔法が効かなかったとき
                                                dungeonMessage = "しかし、" + fightEnemy.enemyName + "には効かなかった！\n";
                                            }
                                            break;
                                        case ItemGenerator.ITEM_EFFECT_TYPE.SEALED:  //魔封じの護符

                                            //魔法が効いたかどうかの判定を行う
                                            if (fightRandom < player.GetEffectPercent(fightEnemy, useItem.itemEffect) &&
                                                fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.OK)
                                            {
                                                //魔法が効いたときは敵の状態を「封印」にする
                                                fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.SEALED);
                                                dungeonMessage = fightEnemy.enemyName + "の魔法を封じた！\n";
                                            }
                                            else
                                            {
                                                //魔法が効かなかったとき
                                                dungeonMessage = "しかし、" + fightEnemy.enemyName + "には効かなかった！\n";
                                            }
                                            break;
                                        case ItemGenerator.ITEM_EFFECT_TYPE.POISON:  //毒蛇の香

                                            //魔法が効いたかどうかの判定を行う
                                            if (fightRandom < player.GetEffectPercent(fightEnemy, useItem.itemEffect) &&
                                                fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.OK)
                                            {
                                                //魔法が効いたときは敵の状態を「毒」にする
                                                fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.POISON);
                                                dungeonMessage = fightEnemy.enemyName + "は毒に侵された！\n";
                                            }
                                            else
                                            {
                                                //魔法が効かなかったとき
                                                dungeonMessage = "しかし、" + fightEnemy.enemyName + "には効かなかった！\n";
                                            }
                                            break;
                                        case ItemGenerator.ITEM_EFFECT_TYPE.DEATH:  //死の首飾り

                                            //魔法が効いたかどうかの判定を行う
                                            if (fightRandom < player.GetEffectPercent(fightEnemy, useItem.itemEffect))
                                            {
                                                //魔法が効いたときは敵のHPを0にする
                                                dungeonMessage = fightEnemy.enemyName + "の息の根を止めた！\n";
                                                fightEnemy.EnemyKill();
                                                //敵の画像を消す
                                                EventImageController.evImageController.ImagePanelClose();
                                            }
                                            else
                                            {
                                                //魔法が効かなかったとき
                                                dungeonMessage = "しかし、" + fightEnemy.enemyName + "には効かなかった！\n";
                                            }
                                            break;
                                        case ItemGenerator.ITEM_EFFECT_TYPE.FINAL_SEALED:   //姫の指輪

                                            //敵の種類のチェックを行う
                                            if (fightEnemy.enemyType == Enemy.ENEMY_TYPE.FINAL_BOSS)
                                            {
                                                //最終ボスの時はときは敵の状態を「封印（最終ボス用）」にする
                                                fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.FINAL_SEALED);
                                                dungeonMessage = fightEnemy.enemyName + "の魔法を封じた！\n";
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                //メッセージを表示する
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);

                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            break;
                        case 4:
                            //最終ボス以外に姫の指輪を使用したときの処理（時間を開けて表示するため、他の処理と分けた）
                            if (CommonMethod.TimeWait(3.0f) == true)
                            {
                                //一定時間後にメッセージを表示する
                                dungeonMessage = "しかし、何も起こらなかった。\n";
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);

                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            break;
                        default:
                            //戦闘時のアイテム使用の終了処理

                            //効果音の再生が終了したときに処理を開始する
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //カウンタの初期化
                                eventCount = 0;
                                //待機状態にする
                                fightWaitFlag = true;
                            }
                            break;
                    }
                }
                else
                {
                    //待機状態の時
                    //待機状態から指定時間が経過したとき
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //敵の死亡判定を行う
                        if (fightEnemy.DeadCheck() == true)
                        {
                            //死亡時はプレイヤー勝利へ移行する
                            fightMode = FIGHTMODE.PLAYER_WIN;
                        }
                        else
                        {   //死亡時でないときは敵の状態チェックへ移行する
                            fightMode = FIGHTMODE.E_CONDITION_CHECK;
                        }
                        //待機状態の解除
                        fightWaitFlag = false;
                    }
                }
                break;
            case FIGHTMODE.E_CONDITION_CHECK:     //敵の状態チェック
                if (fightWaitFlag == false)
                {
                    //待機状態でないとき
                    if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SLEEP)
                    {
                        //睡眠状態の時
                        //状態回復判定を行う
                        if (fightEnemy.ConditionRecoverCheck() == true)
                        {
                            //状態回復に成功したとき
                            //状態を「OK」にし、状態異常経過ターン数を初期化する
                            fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.OK);
                            fightEnemy.AbConditionReset();

                            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "は目を覚ました！\n");
                        }
                        else
                        {
                            //状態回復に失敗したとき
                            //状態異常経過ターン数を1増やす
                            fightEnemy.AbConditionCount();
                            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "は眠っている！\n");
                        }
                        //待機状態にする
                        fightWaitFlag = true;
                    }
                    else if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SEALED)
                    {
                        //封印状態の時
                        if (fightEnemy.ConditionRecoverCheck() == true)
                        {
                            //状態回復に成功したとき
                            //状態を「OK」にし、状態異常経過ターン数を初期化する
                            fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.OK);
                            fightEnemy.AbConditionReset();

                            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "の封印が解けた！\n");
                        }
                        else
                        {
                            //状態回復に失敗したとき
                            //状態異常経過ターン数を1増やす
                            fightEnemy.AbConditionCount();
                            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "は魔法を封印されている！\n");
                        }
                        //待機状態にする
                        fightWaitFlag = true;
                    }
                    else if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.POISON)
                    {
                        //毒状態の時
                        if (fightEnemy.ConditionRecoverCheck() == true)
                        {
                            //状態回復に成功したとき
                            //状態を「OK」にし、状態異常経過ターン数を初期化する
                            fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.OK);
                            fightEnemy.AbConditionReset();

                            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "の毒が消えた！\n");
                        }
                        else
                        {
                            //状態回復に失敗したとき
                            //状態異常経過ターン数を1増やす
                            fightEnemy.AbConditionCount();
                            MessageController.msgController.MessageDisp(fightEnemy.enemyName + "は毒により、" + fightEnemy.PoisonDamage() + "のダメージ！\n");
                        }
                        //待機状態にする
                        fightWaitFlag = true;
                    }
                    else if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.FINAL_SEALED)
                    {
                        //封印状態（最終ボス）の時
                        MessageController.msgController.MessageDisp(fightEnemy.enemyName + "は魔法を封印されている！\n");
                        //待機状態にする
                        fightWaitFlag = true;
                    }
                    else
                    {
                        //状態がOKの時は敵の攻撃へ移行
                        fightMode = FIGHTMODE.ENEMY_ATTACK;
                    }
                }
                else
                {
                    //待機中の時
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき
                        if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SLEEP)
                        {
                            //状態が「睡眠」の時はプレイヤーの状態チェックに移行する
                            fightMode = FIGHTMODE.P_CONDITION_CHECK;
                        }
                        else
                        {
                            //状態が「睡眠」でないとき時
                            //敵の死亡判定を行う
                            if (fightEnemy.DeadCheck() == true)
                            {
                                //死亡時はプレイヤーの勝利へ移行
                                fightMode = FIGHTMODE.PLAYER_WIN;
                            }
                            else
                            {
                                //死亡時でないときは敵の攻撃へ移行
                                fightMode = FIGHTMODE.ENEMY_ATTACK;
                            }
                        }
                        //待機状態の解除
                        fightWaitFlag = false;
                    }
                }
                break;
            case FIGHTMODE.ENEMY_ATTACK:        //敵の攻撃     
                if (fightWaitFlag == false)
                {
                    //待機状態でない時
                    switch (eventCount)
                    {
                        case 0:
                            if (fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.SLEEP)
                            {
                                //睡眠状態でないとき
                                //敵の攻撃手段を取得
                                enemyAttackMethod = fightEnemy.GetAttackMethodRandom();

                                if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.ATTACK ||
                                    enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.POISON_ATTACK)
                                {
                                    //普通の攻撃か毒攻撃の時はプレイヤーへの攻撃処理を行う

                                    //メッセージを初期化する
                                    dungeonMessage = "";

                                    //会心の一撃フラグの取得
                                    criticalFlag = fightEnemy.CriticalAttackCheck();

                                    //プレイヤーに与えるダメージの取得
                                    enemyAttackDamage = DamageCheck(player, fightEnemy, false, criticalFlag);

                                    eventCount = 1;
                                }
                                else
                                {
                                    //その他の手段の時は特殊攻撃処理を行う

                                    //乱数の取得
                                    fightRandom = Random.Range(0, Enemy.EFFECT_PAR_MAX);

                                    //メッセージを初期化する
                                    dungeonMessage = "";

                                    eventCount = 3;
                                }

                                //上記で作成したメッセージを表示する
                                MessageController.msgController.MessageDisp(dungeonMessage);

                            }
                            else
                            {
                                //睡眠状態の時
                                if (fightEnemy.ConditionRecoverCheck() == true)
                                {
                                    //状態回復に成功したとき
                                    //状態を「OK」にし、状態異常経過ターン数を初期化する
                                    fightEnemy.ChangeCondition(Enemy.ENEMY_CONDITION.OK);
                                    fightEnemy.AbConditionReset();

                                    MessageController.msgController.MessageDisp(fightEnemy.enemyName + "は目を覚ました！\n");
                                }
                                else
                                {
                                    //状態回復に失敗したとき
                                    //状態異常経過ターン数を1増やす
                                    fightEnemy.AbConditionCount();

                                    MessageController.msgController.MessageDisp(fightEnemy.enemyName + "は眠っている！\n");
                                }

                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            break;
                        case 1:
                            //敵の攻撃エフェクト
                            LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.ATTACK);

                            //敵の攻撃の効果音を再生する
                            SoundManager.soundManager.PlaySE("se_enemy_attack");
                            //敵の攻撃のメッセージを表示
                            dungeonMessage = fightEnemy.enemyName + "の攻撃！\n";
                            MessageController.msgController.MessageDisp(dungeonMessage);

                            eventCount++;
                            break;
                        case 2:
                            //敵の攻撃の効果音が終了後、次の処理を行う
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                if (player.AttackAvoidCheck(fightEnemy) == true)
                                {
                                    //プレイヤーの攻撃回避が成功したとき

                                    //攻撃回避の効果音を再生する
                                    SoundManager.soundManager.PlaySE("se_attack_avoid");

                                    //攻撃回避のメッセージを表示する
                                    dungeonMessage = player.playerName + "は素早く身をかわした！\n";
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);

                                    //敵がプレイヤーに与えるダメージを0にする
                                    enemyAttackDamage = 0;
                                }
                                else
                                {
                                    //プレイヤーの攻撃回避が失敗したとき
                                    if (enemyAttackDamage > 0)
                                    {
                                        //画面を振動させる
                                        cameraController.DungeonCameraShake();
                                        //敵の攻撃命中エフェクト
                                        LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.HIT);

                                        //ダメージが1以上の時
                                        if (criticalFlag == true)
                                        {
                                            //会心の一撃

                                            //攻撃命中（会心の一撃）の効果音を再生する
                                            SoundManager.soundManager.PlaySE("se_critical_hit");

                                            //会心の一撃のメッセージを格納
                                            dungeonMessage = "痛恨の一撃！！\n";
                                        }
                                        else
                                        {
                                            //通常のダメージ

                                            //攻撃命中の効果音を再生する
                                            SoundManager.soundManager.PlaySE("se_attack_hit");

                                            //メッセージの初期化
                                            dungeonMessage = "";
                                        }

                                        //攻撃命中メッセージを格納
                                        dungeonMessage = dungeonMessage + player.playerName + "に" + enemyAttackDamage + "のダメージ！\n";

                                        //攻撃手段が毒攻撃で、ダメージが1以上の時
                                        if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.POISON_ATTACK && enemyAttackDamage > 0)
                                        {
                                            //プレイヤーの状態が「OK」または「祝福」の時、プレイヤーを毒状態にする
                                            if (player.playerCondition == Player.PLAYER_CONDITION.OK ||
                                                player.playerCondition == Player.PLAYER_CONDITION.BLESSING)
                                            {
                                                dungeonMessage = dungeonMessage + player.playerName + "は" + "毒に侵された！\n";
                                                player.ChangeCondition(Player.PLAYER_CONDITION.POISON);
                                            }
                                        }

                                        //プレイヤーの現在HPからダメージを引く
                                        player.DeclineHp(enemyAttackDamage);
                                    }
                                    else
                                    {
                                        //ダメージが0の時
                                        //ミスの効果音を再生する
                                        SoundManager.soundManager.PlaySE("se_miss");

                                        //ミスのメッセージを格納する
                                        dungeonMessage = "ミス！\n" + player.playerName + "はダメージを受けない！\n";
                                    }

                                    //攻撃命中メッセージを表示する
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                }

                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            break;
                        case 3:
                            //メッセージの共通部分の作成
                            dungeonMessage = fightEnemy.enemyName + "は";
                            //使用した攻撃手段に応じてメッセージを追加する
                            if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.SLEEP)
                            {
                                //眠りの魔法
                                dungeonMessage = dungeonMessage + "眠りの魔法を使った！\n";

                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.SEALED)
                            {
                                //封印の魔法
                                dungeonMessage = dungeonMessage + "封印の魔法を使った！\n";
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BALL)
                            {
                                //火の玉の魔法
                                dungeonMessage = dungeonMessage + "火の玉の魔法を使った！\n";
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.THUNDER)
                            {
                                //雷の魔法
                                dungeonMessage = dungeonMessage + "雷の魔法を使った！\n";
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.DEATH_MAGIC)
                            {
                                //死の魔法
                                dungeonMessage = dungeonMessage + "死の魔法を使った！\n";
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.POISON_SPRAY)
                            {
                                //毒液噴射
                                dungeonMessage = dungeonMessage + "毒液を噴射した！\n";
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BREATH)
                            {
                                //火炎噴射
                                dungeonMessage = dungeonMessage + "炎を吐いた！\n";
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.DEATH_SICKLE)
                            {
                                //死神の鎌
                                dungeonMessage = dungeonMessage + "鎌を振り下ろしてきた！\n";
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.HEAL_MAGIC)
                            {
                                //回復魔法
                                dungeonMessage = dungeonMessage + "癒しの魔法を使った！\n";
                            }

                            //敵の特殊攻撃使用のメッセージを表示
                            MessageController.msgController.MessageDisp(dungeonMessage);

                            eventCount++;
                            break;
                        case 4:
                            //使用した攻撃手段に応じた効果音とエフェクトを再生する
                            if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.SLEEP)
                            {
                                //眠りの魔法
                                if (fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.SEALED &&
                                    fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」でない時に再生する
                                    LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.SLEEP);
                                    SoundManager.soundManager.PlaySE("se_sleep_magic");
                                }
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.SEALED)
                            {
                                //封印の魔法
                                if (fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.SEALED &&
                                    fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」でない時に再生する
                                    LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.SEALED);
                                    SoundManager.soundManager.PlaySE("se_sealed_magic");
                                }
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BALL)
                            {
                                //火の玉の魔法
                                if (fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.SEALED &&
                                    fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」でない時に再生する
                                    LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.FIRE_BALL);
                                    SoundManager.soundManager.PlaySE("se_fire_magic");
                                }
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.THUNDER)
                            {
                                //雷の魔法
                                if (fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.SEALED &&
                                    fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」でない時に再生する
                                    LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.THUNDER);
                                    SoundManager.soundManager.PlaySE("se_thunder_magic");
                                }
                                
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.DEATH_MAGIC)
                            {
                                //死の魔法
                                if (fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.SEALED &&
                                    fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」でない時に再生する
                                    LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.DEATH);
                                    SoundManager.soundManager.PlaySE("se_death_magic");
                                }
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.POISON_SPRAY)
                            {
                                //毒液噴射
                                SoundManager.soundManager.PlaySE("se_poison_spray");
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.POISON);
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BREATH)
                            {
                                //火炎噴射
                                SoundManager.soundManager.PlaySE("se_fire_breath");
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.FIRE_BREATH);
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.DEATH_SICKLE)
                            {
                                //死神の鎌
                                SoundManager.soundManager.PlaySE("se_death_sickle");
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.DEATH_SICKLE);
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.HEAL_MAGIC)
                            {
                                //回復魔法
                                if (fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.SEALED &&
                                    fightEnemy.enemyCondition != Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」でない時に再生する
                                    SoundManager.soundManager.PlaySE("se_heal_magic");
                                    EventImageController.evImageController.PlayEffect(EventImageController.ENEMY_HEAL_MAGIC);
                                }
                            }
                            eventCount++;
                            break;
                        case 5:
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //効果音が終了した後、次へ進む
                                eventCount++;
                            }
                            break;
                        case 6:
                            //使用した攻撃手段に対応した処理を行う
                            if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.SLEEP)
                            {
                                //眠りの魔法
                                if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SEALED ||
                                    fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」の時
                                    dungeonMessage = "しかし、魔法は封じられている！\n";
                                }
                                else
                                {
                                    //敵の状態が「封印」でない時
                                    if (fightRandom < fightEnemy.GetEffectPercent(player, EnemyAttackPattern.ATTACK_METHOD.SLEEP)
                                        && player.playerCondition == Player.PLAYER_CONDITION.OK)
                                    {
                                        //魔法が効いたときはプレイヤーの状態を「眠り」に変え、コマンドボタンを使用不可にする
                                        player.ChangeCondition(Player.PLAYER_CONDITION.SLEEP);
                                        dungeonMessage = player.playerName + "は眠ってしまった！\n";
                                        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.WAIT);

                                    }
                                    else
                                    {
                                        //魔法が効かなかったとき
                                        dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n";
                                    }
                                }

                                //メッセージを追加
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //カウンタを終了処理へと進める
                                eventCount = 99;

                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.SEALED)
                            {
                                //封印の魔法
                                if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SEALED ||
                                    fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」の時
                                    dungeonMessage = "しかし、魔法は封じられている！\n";
                                }
                                else
                                {
                                    if (fightRandom < fightEnemy.GetEffectPercent(player, EnemyAttackPattern.ATTACK_METHOD.SEALED)
                                        && player.playerCondition == Player.PLAYER_CONDITION.OK)
                                    {
                                        //魔法が効いたときはプレイヤーの状態を「封印」に変える
                                        player.ChangeCondition(Player.PLAYER_CONDITION.SEALED);
                                        dungeonMessage = player.playerName + "は魔法を封じられた！\n";

                                    }
                                    else
                                    {
                                        //魔法が効かなかったとき
                                        dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n";
                                    }
                                }

                                //メッセージを追加
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BALL)
                            {
                                //火の玉の魔法
                                if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SEALED ||
                                    fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」の時
                                    dungeonMessage = "しかし、魔法は封じられている！\n";

                                    //メッセージを追加
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                                else
                                {
                                    //プレイヤーに与えるダメージの取得
                                    enemyAttackDamage = fightEnemy.GetMagicDamage(player, enemyAttackMethod);
                                    if (enemyAttackDamage > 0)
                                    {
                                        //ダメージが1以上の時
                                        //カウンタを特殊攻撃命中音再生処理へと進める
                                        eventCount++;
                                    }
                                    else
                                    {
                                        //ダメージが0の時

                                        //メッセージを追加
                                        dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n";
                                        MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                        //カウンタを終了処理へと進める
                                        eventCount = 99;
                                    }
                                }

                                
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.THUNDER)
                            {
                                //雷の魔法
                                if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SEALED ||
                                    fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」の時

                                    //メッセージを追加
                                    dungeonMessage = "しかし、魔法は封じられている！\n";
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                                else
                                {
                                    //プレイヤーに与えるダメージの取得
                                    enemyAttackDamage = fightEnemy.GetMagicDamage(player, enemyAttackMethod);
                                    if (enemyAttackDamage > 0)
                                    {
                                        //ダメージが1以上の時
                                        //カウンタを特殊攻撃命中音再生処理へと進める
                                        eventCount++;
                                    }
                                    else
                                    {
                                        //ダメージが0の時

                                        //メッセージを追加
                                        dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n";
                                        MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                        //カウンタを終了処理へと進める
                                        eventCount = 99;
                                    }
                                }
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.DEATH_MAGIC)
                            {
                                //死の魔法
                                if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SEALED || 
                                    fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」の時
                                    dungeonMessage = "しかし、魔法は封じられている！\n";
                                }
                                else
                                {
                                    if (fightRandom < fightEnemy.GetEffectPercent(player, EnemyAttackPattern.ATTACK_METHOD.DEATH_MAGIC))
                                    {
                                        //魔法が効いたときはプレイヤーのHPを0にする
                                        player.PlayerKill();
                                        dungeonMessage = player.playerName + "は息の根を止められた！\n";
                                    }
                                    else
                                    {
                                        //魔法が効かなかったとき
                                        dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n";
                                    }
                                }

                                //メッセージを追加
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.POISON_SPRAY)
                            {
                                //毒液噴射
                                if (fightRandom < fightEnemy.GetEffectPercent(player, EnemyAttackPattern.ATTACK_METHOD.POISON_SPRAY)
                                    && player.playerCondition == Player.PLAYER_CONDITION.OK)
                                {
                                    //成功したときプレイヤーの状態を「毒」にする
                                    player.ChangeCondition(Player.PLAYER_CONDITION.POISON);
                                    dungeonMessage = player.playerName + "は毒に侵された！\n";
                                }
                                else
                                {
                                    //失敗したとき
                                    dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n"; 
                                }

                                //メッセージを追加
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BREATH)
                            {
                                //火炎噴射
                                //プレイヤーに与えるダメージの取得
                                enemyAttackDamage = fightEnemy.GetMagicDamage(player, enemyAttackMethod);
                                if (enemyAttackDamage > 0)
                                {
                                    //ダメージが1以上の時
                                    //カウンタを特殊攻撃命中音再生処理へと進める
                                    eventCount++;
                                }
                                else
                                {
                                    //ダメージが0の時

                                    //メッセージを追加
                                    dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n";
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.DEATH_SICKLE)
                            {
                                //死神の鎌
                                //成功率の取得
                                hitSPAttackPar = fightEnemy.GetEffectPercent(player, EnemyAttackPattern.ATTACK_METHOD.DEATH_SICKLE);

                                //勇者の装備一式を全て装備しているときは確実に失敗するようにする
                                if (player.AllEquipCheck((int)ItemGenerator.EVITEM_FIGHT.WEAPON1,
                                                            (int)ItemGenerator.EVITEM_FIGHT.ARMOR1,
                                                            (int)ItemGenerator.EVITEM_FIGHT.SHIELD1,
                                                            (int)ItemGenerator.EVITEM_FIGHT.HELM1) == true)
                                {
                                    hitSPAttackPar = 0;
                                }

                                if (fightRandom < hitSPAttackPar)
                                {
                                    //成功したとき
                                    //カウンタを特殊攻撃命中音再生処理へと進める
                                    eventCount++;

                                }
                                else
                                {
                                    //失敗したとき

                                    //メッセージを追加
                                    dungeonMessage = "しかし、" + player.playerName + "には効かなかった！\n";
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.HEAL_MAGIC)
                            {
                                //回復魔法
                                if (fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.SEALED || 
                                    fightEnemy.enemyCondition == Enemy.ENEMY_CONDITION.FINAL_SEALED)
                                {
                                    //敵の状態が「封印」の時

                                    //メッセージを追加
                                    dungeonMessage = "しかし、魔法は封じられている！\n";
                                    MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                    //カウンタを終了処理へと進める
                                    eventCount = 99;
                                }
                                else
                                {
                                    //カウンタを特殊攻撃命中音再生処理へと進める
                                    eventCount++;
                                }
                            }
                            break;
                        case 7:
                            //特殊攻撃命中音再生処理
                            if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BALL)
                            {
                                //火の玉の魔法

                                //画面を振動させる
                                cameraController.DungeonCameraShake();
                                //敵の攻撃命中エフェクト
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.HIT);

                                //攻撃命中の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_attack_hit");

                                //メッセージを追加
                                dungeonMessage = player.playerName + "に" + enemyAttackDamage + "のダメージ！\n";
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //プレイヤーの現在HPからダメージを引く
                                player.DeclineHp(enemyAttackDamage);
                                //カウンタを次の処理へと進める
                                eventCount++;
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.THUNDER)
                            {
                                //雷の魔法

                                //画面を振動させる
                                cameraController.DungeonCameraShake();
                                //敵の攻撃命中エフェクト
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.HIT);

                                //攻撃命中の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_attack_hit");

                                //メッセージを追加
                                dungeonMessage = player.playerName + "に" + enemyAttackDamage + "のダメージ！\n";
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //プレイヤーの現在HPからダメージを引く
                                player.DeclineHp(enemyAttackDamage);
                                //カウンタを次の処理へと進める
                                eventCount++;
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.FIRE_BREATH)
                            {
                                //火炎噴射

                                //画面を振動させる
                                cameraController.DungeonCameraShake();
                                //敵の攻撃命中エフェクト
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.HIT);

                                //攻撃命中の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_attack_hit");

                                //メッセージを追加
                                dungeonMessage = player.playerName + "に" + enemyAttackDamage + "のダメージ！\n";
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //プレイヤーの現在HPからダメージを引く
                                player.DeclineHp(enemyAttackDamage);
                                //カウンタを次の処理へと進める
                                eventCount++;
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.DEATH_SICKLE)
                            {
                                //死神の鎌

                                //画面を振動させる
                                cameraController.DungeonCameraShake();
                                //敵の攻撃命中エフェクト
                                LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.HIT);

                                //攻撃命中（会心の一撃）の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_critical_hit");

                                //メッセージを追加
                                dungeonMessage = player.playerName + "は息の根を止められた！\n";
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //プレイヤーのHPを0にする
                                player.PlayerKill();
                                //カウンタを次の処理へと進める
                                eventCount++;
                            }
                            else if (enemyAttackMethod == EnemyAttackPattern.ATTACK_METHOD.HEAL_MAGIC)
                            {
                                //回復魔法

                                //敵の体力回復エフェクト
                                EventImageController.evImageController.PlayEffect(EventImageController.ENEMY_HEAL);

                                //体力回復の効果音を再生する
                                SoundManager.soundManager.PlaySE("se_heal");

                                //メッセージを追加
                                dungeonMessage = fightEnemy.enemyName + "の体力が回復した！\n";
                                MessageController.msgController.MessageJoinDisp(dungeonMessage);
                                //敵のHPを回復する
                                fightEnemy.HealMagic();
                                //カウンタを次の処理へと進める
                                eventCount++;
                            }
                            break;
                        case 8:
                            if (SoundManager.soundManager.SEPlayingCheck() == false)
                            {
                                //効果音が終了した後、カウンタを終了処理へと進める
                                eventCount = 99;
                            }
                            break;
                        default:
                            //カウンタの初期化
                            eventCount = 0;
                            //待機状態にする
                            fightWaitFlag = true;
                            break;

                    }
                }
                else
                {
                    //待機中の時
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき
                        //プレイヤーの死亡判定を行う
                        if (player.DeadCheck() == true)
                        {
                            //死亡時はプレイヤー死亡へ移行
                            fightMode = FIGHTMODE.PLAYER_DEAD;
                        }
                        else
                        {
                            //死亡時でないときはプレイヤーの状態チェックへ移行
                            fightMode = FIGHTMODE.P_CONDITION_CHECK;

                        }
                        //待機状態を解除する
                        fightWaitFlag = false;
                    }
                }
                break;
            case FIGHTMODE.PLAYER_WIN:      //プレイヤーの勝利
                if (fightWaitFlag == false)
                {
                    //待機状態でない時
                    switch (eventCount)
                    {
                        case 0:
                            //BGMを停止する
                            SoundManager.soundManager.StopBGM(0.5f);

                            //コマンドボタンを使用不可にする
                            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                            eventCount++;
                            break;
                        case 1:
                            if (SoundManager.soundManager.BGMPlayingCheck() == false)
                            {
                                //戦闘勝利のジングルを再生する
                                SoundManager.soundManager.PlayBGM("bgm_win_jingle", 0.5f, false);

                                //プレイヤー勝利時の処理を行う
                                PlayerWin();

                                eventCount++;
                            }
                            break;
                        default:
                            //カウンタを初期化する
                            eventCount = 0;
                            //待機中にする
                            fightWaitFlag = true;
                            break;
                    }
                }
                else
                {
                    //待機中の時
                    if (CommonMethod.TimeWait(7.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき
                        //レベルアップ判定を行う
                        if (player.LevelUpCheck() == true)
                        {
                            //次のレベルアップに必要な経験値があるときはレベルアップへ移行
                            fightMode = FIGHTMODE.LEVEL_UP;
                        }
                        else
                        {
                            //次のレベルアップに必要な経験値がないとき

                            if (fightEnemy.enemyType == Enemy.ENEMY_TYPE.FINAL_BOSS)
                            {
                                //最終ボスを倒したときは最終ボス打倒に移行する
                                fightMode = FIGHTMODE.L_BOSS_DEFEAT;

                            }
                            else
                            {
                                //それ以外は戦闘終了に移行
                                fightMode = FIGHTMODE.FIGHT_END;
                            }
                        }
                        //待機状態を解除する
                        fightWaitFlag = false;
                    }

                }
                break;
            case FIGHTMODE.L_BOSS_DEFEAT:    //最終ボス打倒
                if (fightWaitFlag == false)
                {
                    //待機状態でない時、待機中にする
                    fightWaitFlag = true;
                }
                else
                {
                    //待機中の時
                    if (CommonMethod.TimeWait(3.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき

                        //エンディング直前のイベントに移行する
                        fightMode = FIGHTMODE.E_BEFORE_EVENT;

                        //待機状態を解除する
                        fightWaitFlag = false; 
                    }
                }
                break;
            case FIGHTMODE.E_BEFORE_EVENT:   //エンディング直前のイベント
                EndingJustBeforeEvent();
                break;
            case FIGHTMODE.PLAYER_DEAD:      //プレイヤー死亡
                //逃走回数の初期化
                player.EscapeCountReset();
                //プレイヤー死亡処理の実行
                PlayerDead();
                break;
            case FIGHTMODE.PLAYER_ESCAPE:    //プレイヤー逃走
                switch (eventCount)
                {
                    case 0:
                        //コマンドボタンを使用不可にする
                        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
                        eventCount++;
                        break;
                    case 1:
                        if (SoundManager.soundManager.SEPlayingCheck() == false)
                        {
                            //逃走のメッセージを表示する
                            MessageController.msgController.MessageDisp(player.playerName + "は逃げ出した！\n");
                            //逃走の効果音を再生する
                            SoundManager.soundManager.PlaySE("se_escape");
                            eventCount++;
                        }
                        break;
                    case 2:
                        if (CommonMethod.TimeWait(3.0f) == true)
                        {
                            //逃走成功チェックを行い、少し後にそれぞれの処理を行う
                            if (player.EscapeCheck(fightEnemy) == true)
                            {
                                //成功時
                                Escaping();
                                eventCount++;
                            }
                            else
                            {
                                //失敗時
                                EscapeFailed();
                                eventCount += 2;
                            }  
                        }
                        break;
                    case 3:
                        //逃走に成功したとき
                        if (CommonMethod.TimeWait(3.0f) == true)
                        {
                            //逃走終了時の処理を実行
                            EscapeEnd();
                            //小マップを表示する
                            cameraController.MapClose(miniMapFlag);
                            //ダンジョンのBGMを再生する
                            SoundManager.soundManager.PlayBGM(dungeonBGMArray[player.nowFloor - 1], 0.5f, true);
                            //カウンタを初期化する
                            eventCount = 0;
                        }
                        break;
                    case 4:
                        //逃走に失敗したとき
                        if (CommonMethod.TimeWait(3.0f) == true)
                        {
                            //敵のターンへ移行する処理を行う
                            EscapeFailedReturn();
                            //カウンタを初期化する
                            eventCount = 0;
                        }
                        break;

                }
                break;
            case FIGHTMODE.LEVEL_UP:         //レベルアップ
                if (fightWaitFlag == false)
                {
                    //待機状態でない時
                    switch (eventCount)
                    {
                        case 0:
                            if (SoundManager.soundManager.BGMPlayingCheck() == false)
                            {
                                //レベルアップのジングルを再生する
                                SoundManager.soundManager.PlayBGM("bgm_levelup_jingle", 0.5f, false);

                                //レベルアップの処理を行う
                                MessageController.msgController.MessageDisp(player.PlayerLevelUpMessage());
                                player.PlayerLevelUp();

                                eventCount++;
                            }
                            break;
                        default:
                            if (SoundManager.soundManager.BGMPlayingCheck() == false)
                            {
                                //待機中にする
                                fightWaitFlag = true;
                                //カウンタを初期化する
                                eventCount = 0;
                            }
                            break;
                    }
                }
                else
                {
                    //待機中の時
                    if (CommonMethod.TimeWait(5.0f) == true)
                    {
                        //待機状態から指定時間が経過したとき
                        //戦闘終了へ移行
                        fightMode = FIGHTMODE.FIGHT_END;
                        //待機状態を解除する
                        fightWaitFlag = false;
                    }

                }
                break;
            case FIGHTMODE.FIGHT_END:       //戦闘終了
                //逃走回数の初期化
                player.EscapeCountReset();
                //戦闘終了処理の実行
                FightEnd();

                //待機状態を解除する
                fightWaitFlag = false;

                //状態が毒と祝福以外の時はプレイヤーの状態をOKにする
                if (player.playerCondition != Player.PLAYER_CONDITION.POISON && 
                    player.playerCondition != Player.PLAYER_CONDITION.BLESSING)
                {
                    player.ChangeCondition(Player.PLAYER_CONDITION.OK);
                }

                //小マップを表示する
                cameraController.MapClose(miniMapFlag);

                //BGMを再生する
                SoundManager.soundManager.PlayBGM(dungeonBGMArray[player.nowFloor - 1], 0.5f, true);
                break;
            default:
                break;
        }

    }

    //戦闘開始時（プレイヤーからNPCに戦いを仕掛けた時）の敵の会話の処理を行う関数
    void FightStartEnemyTalk()
    {
        //敵に応じてメッセージを変える
        if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.NPC1)
        {
            //魔法の地図の情報を持つ冒険者
            MessageController.msgController.MessageDisp("何をする、やめろ！\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.NPC2)
        {
            //鍵売りの老人
            MessageController.msgController.MessageDisp("ひいい、お助けを！\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.NPC3)
        {
            //ミストリル鉱石の情報を持つ冒険者
            MessageController.msgController.MessageDisp("やめな、気でも狂ったのかい！\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.NPC4)
        {
            //地下3階の衛兵
            MessageController.msgController.MessageDisp("力ずくで通ろうってのか、この身の程知らずが！\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.NPC5)
        {
            //地下3階の冒険者
            MessageController.msgController.MessageDisp("魔物の手先か、覚悟しろ！\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.NPC6)
        {
            //雷の杖を持っている魔法使い
            MessageController.msgController.MessageDisp("かよわい年寄りを襲うとは感心せぬな。\nお灸をすえてやろう。\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.NPC7)
        {
            //謎の人物
            MessageController.msgController.MessageDisp("争いは好みませんが、\n降りかかる火の粉は払うしかありませんね。\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.SUCCUBUS)
        {
            //サキュバス
            MessageController.msgController.MessageDisp("どうしてわかったの！？\nでも、お前がここで死ぬことに変わりはない！！\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.DEATH)
        {
            //死神
            MessageController.msgController.MessageDisp("愚か者め、死ぬがよい。\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.LAST_BOSS)
        {
            //魔王
            MessageController.msgController.MessageDisp("死に急ぐか、こわっぱが！\nでは望みを叶えてやろう！\nこの私の恐ろしさを思い知るがいい！\n");
        }
        else if (fightEnemy.enemyId == (int)EnemyGenerator.F_ENEMY_TALK.PRINCESS)
        {
            //姫
            MessageController.msgController.MessageDisp("そんな、どうして・・・！\n");
        }

    }

    //アイテム名ラベルにマウスポインタが乗った時の処理を行う関数
    //引数
    //num:対象のアイテム名ラベルの番号
    public void ItemLabelOnMouseEnter(int num)
    {
        if (itemClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいた所持アイテムリスト番号を取得
        int click_item = num + ((itemPage - 1) * Player.ITEM_PER_PAGE);

        if (player.GetItemBoxIndex(click_item).itemId > 0)
        {
            //所持アイテムリスト番号で指定された要素内にアイテムが存在するとき

            //アイテム情報の文字色取得（マウスポインタが乗った時）
            Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);
            
            Text equipText = itemEquipLabels[num].GetComponent<Text>();
            Text nameText = itemNameLabels[num].GetComponent<Text>();
            Text countText = itemCountLabels[num].GetComponent<Text>();

            //アイテム名、数量、装備中の色変更
            equipText.color = labelColor;
            nameText.color = labelColor;
            countText.color = labelColor;

            //指定されたアイテムデータの取得
            Item item = itemGenerator.GetItemInfo(player.GetItemBoxIndex(click_item).itemId);

            //アイテム情報の表示
            Text explain = itemExplanation.GetComponent<Text>();
            explain.text = item.itemExplanation;

            //アイテム画像の表示
            ItemImageDisp(item.itemImg);
        }
    }

    //アイテム名ラベルからマウスポインタが離れた時の処理を行う関数
    //引数
    //num:対象のアイテム名ラベルの番号
    public void ItemLabelOnMouseExit(int num)
    {
        if (itemClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいた所持アイテムリスト番号を取得
        int click_item = num + ((itemPage - 1) * Player.ITEM_PER_PAGE);

        if (player.GetItemBoxIndex(click_item).itemId > 0)
        {
            //所持アイテムリスト番号で指定された要素内にアイテムが存在するとき

            //アイテム情報の文字色取得（初期値）
            Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            Text equipText = itemEquipLabels[num].GetComponent<Text>();
            Text nameText = itemNameLabels[num].GetComponent<Text>();
            Text countText = itemCountLabels[num].GetComponent<Text>();

            //アイテム名、数量、装備中の色変更
            equipText.color = labelColor;
            nameText.color = labelColor;
            countText.color = labelColor;

            //アイテム情報の初期化
            Text explain = itemExplanation.GetComponent<Text>();
            explain.text = "";

            //アイテム画像の消去
            ItemImageDisp("NoItem");
        }
    }


    //アイテム名ラベルをクリックした時の処理を行う関数
    //引数
    //num:対象のアイテム名ラベルの番号
    public void ItemLabelOnMouseClick(int num)
    {
        //アイテム名ラベルの番号の取得
        saveItemNum = num;
        //対象ラベルに基づいた所持アイテムリスト番号を取得
        int click_item = num + ((itemPage - 1) * Player.ITEM_PER_PAGE);

        if (player.GetItemBoxIndex(click_item).itemId > 0)
        {
            //所持アイテムリスト番号で指定された要素内にアイテムが存在するとき
            if (itemClickFlag == false)
            {
                //すでにクリックされていないとき

                //ボタンをクリックしたときの効果音を鳴らす
                SoundManager.soundManager.PlaySE("se_decision");

                //アイテム名クリックフラグをオン
                itemClickFlag = true;

                //指定されたアイテムデータの取得
                Item item = itemGenerator.GetItemInfo(player.GetItemBoxIndex(click_item).itemId);

                //アイテム情報の文字色取得（クリック時）
                Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

                Text equipText = itemEquipLabels[num].GetComponent<Text>();
                Text nameText = itemNameLabels[num].GetComponent<Text>();
                Text countText = itemCountLabels[num].GetComponent<Text>();

                //アイテム名、数量、装備中の色変更
                equipText.color = labelColor;
                nameText.color = labelColor;
                countText.color = labelColor;

                //アイテム使用ウィンドウを開く
                itemUsePanel.SetActive(true);
                //アイテム使用ウィンドウに対象アイテム名を表示させる
                itemUseText.text = item.itemName;

                if (fightFlag == true)
                {
                    //戦闘中の時はアイテム廃棄ボタンを使用不可にする
                    itemDiscardButton.GetComponent<Button>().interactable = false;
                }
                else
                {
                    //戦闘中でない時はアイテム廃棄ボタンを使用可能にする
                    itemDiscardButton.GetComponent<Button>().interactable = true;
                }

                //アイテムウィンドウのボタンを使用不可にする
                ItemWindowButtonInit(false);

                //取得した所持アイテムリスト番号を記憶させておく
                itemBoxNum = click_item;

            }
        }

    }

    //アイテム使用ボタンをクリックした時の処理を行う関数
    public void ItemUseButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //イベントオブジェクトの初期化
        EventObject eo = new EventObject();

        //現在地のイベント情報の取得
        eo = eventController.GetEventInfo(player.nowFloor, player.nowPosX, player.nowPosZ);

        //アイテム名クリックフラグをオフ
        itemClickFlag = false;
        //アイテム使用ウィンドウのアイテム名を初期化する
        itemUseText.text = "";

        //アイテムウィンドウのボタンを使用可能にする
        ItemWindowButtonInit(true);

        //アイテム使用ウィンドウおよびアイテムウィンドウを閉じる
        itemUsePanel.SetActive(false);
        ItemWindowClose();

        //指定されたアイテムデータの取得
        Item item = itemGenerator.GetItemInfo(player.GetItemBoxIndex(itemBoxNum).itemId);

        if (fightFlag == true)
        {
            //戦闘中
            if (fightMode == FIGHTMODE.PLAYER_WAIT)
            {
                //プレイヤーのアイテム使用へ移行
                fightMode = FIGHTMODE.PLAYER_ITEM;
                //コマンドボタンを使用不可にする
                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
            }

        }
        else if (fightFlag == false && npcFlag == true)
        {
            //NPC遭遇時
            if (item.itemAttack > 0 || item.itemEffect > ItemGenerator.ITEM_EFFECT_TYPE.NONE)
            {
                //攻撃魔法アイテムもしくは特殊魔法アイテムを使用したとき

                //NPCを敵に変更する
                fightFlag = true;
                npcFlag = false;

                //戦闘に関する状態を「戦闘開始時の敵の会話」に変更
                fightMode = FIGHTMODE.ENEMY_TALK;
                //会話後に「プレイヤーのアイテム使用」へ移行するように設定
                nextFightMode = FIGHTMODE.PLAYER_ITEM;
                //コマンドボタンを使用不可にする
                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);
            }
            else
            {
                //その他のアイテムを使用したとき

                //コマンドボタンを使用不可にする
                commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

                //プレイヤーの行動に関する状態を「アイテム使用中（戦闘時以外）」に移行する
                actMode = ACTMODE.USING_ITEM;

            }

        }
        else
        {
            //通常時

            //コマンドボタンを使用不可にする
            commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.WAIT);

            //プレイヤーの行動に関する状態を「アイテム使用中（戦闘時以外）」に移行する
            actMode = ACTMODE.USING_ITEM;
        }
    }

    //アイテム廃棄ボタンをクリックした時の処理を行う関数
    public void ItemDiscardButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //アイテム名クリックフラグをオフ
        itemClickFlag = false;
        //アイテム使用ウィンドウのアイテム名を初期化する
        itemUseText.text = "";
        //アイテムウィンドウのボタンを使用可能にする
        ItemWindowButtonInit(true);
        //アイテム使用ウィンドウおよびアイテムウィンドウを閉じる
        itemUsePanel.SetActive(false);
        ItemWindowClose();

        //指定されたアイテムデータの取得
        Item item = new Item();
        item = itemGenerator.GetItemInfo(player.GetItemBoxIndex(itemBoxNum).itemId);

        //対象アイテムが廃棄可能かどうかを判定し、廃棄可能な場合は廃棄処理を実行
        if (player.ItemDiscard(itemGenerator.GetItemList(), itemBoxNum) == true)
        {
            //廃棄可能
            MessageController.msgController.MessageDisp(player.playerName + "は" + item.itemName + "を捨てた。\n");
        }
        else
        {
            //廃棄不可
            MessageController.msgController.MessageDisp("それを捨てるなんて、とんでもない！\n");
        }
    }

    //アイテムキャンセルボタンをクリックした時の処理を行う関数
    public void ItemCancelButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //アイテム名クリックフラグをオフ
        itemClickFlag = false;
        //アイテム使用ウィンドウのアイテム名を初期化する
        itemUseText.text = "";
        //アイテム使用ウィンドウおよびアイテムウィンドウを閉じる
        ItemWindowButtonInit(true);
        itemUsePanel.SetActive(false);

        //アイテム情報の文字色取得（初期値）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text equipText = itemEquipLabels[saveItemNum].GetComponent<Text>();
        Text nameText = itemNameLabels[saveItemNum].GetComponent<Text>();
        Text countText = itemCountLabels[saveItemNum].GetComponent<Text>();

        //アイテム名、数量、装備中の色変更
        equipText.color = labelColor;
        nameText.color = labelColor;
        countText.color = labelColor;

        //選択されたアイテム名番号を保存しておく変数の初期化
        saveItemNum = -1;
    }

    //暗転処理を行う関数
    void BlackOut()
    {
        //画面を暗転させる（ダークゾーンを流用）
        DarkZoneChange(true);
    }

    //アイテムウィンドウのボタンの初期化を行う関数
    //引数
    //flag:trueの時はボタンを使用可能にし、falseの時はボタンを使用不可にする
    void ItemWindowButtonInit(bool flag)
    {
        //アイテムウィンドウのページ数を取得
        itemPageMax = player.ItemPageMaxCalc();

        //前ページ移動ボタンの設定
        if (itemPage == 1)
        {
            //最初のページの時は使用不可にする
            itemPreviousButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最初のページでないとき時は引数に応じて、使用できるかどうかを設定する
            itemPreviousButton.GetComponent<Button>().interactable = flag;
        }

        //次ページ移動ボタンの設定
        if (itemPage == itemPageMax)
        {
            //最後のページの時は使用不可にする
            itemNextButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最後のページでないとき時は引数に応じて、使用できるかどうかを設定する
            itemNextButton.GetComponent<Button>().interactable = flag;
        }
        //ウィンドウクローズボタンは引数に応じて、使用できるかどうかを設定する
        itemCloseButton.GetComponent<Button>().interactable = flag;
    }

    //マップ（小）用のアイテムの所持チェックを行う関数
    //戻り値（true:所持、false:未所持）
    bool BoxItemMiniMapCheck()
    {
        //所持アイテムリスト内をチェックする
        for (int i = 0; i < itemList.Count; i++)
        {
            for (int j = 0; j < player.GetItemBoxCount(); j++)
            {
                if (itemList[i].itemId == player.GetItemBoxIndex(j).itemId)
                {
                    if (itemList[i].itemType == ItemGenerator.ITEM_TYPE.MINI_MAP)
                    {
                        //所持しているときはtrueを返す
                        return true;
                    }
                }
            }
        }

        //未所持の時はfalseを返す
        return false;
    }

    //魔法のランタンの所持チェックを行う関数
    //戻り値（true:所持、false:未所持）
    bool BoxItemLanthanumCheck()
    {
        //所持アイテムリスト内をチェックする
        for (int i = 0; i < itemList.Count; i++)
        {
            for (int j = 0; j < player.GetItemBoxCount(); j++)
            {
                if (itemList[i].itemId == player.GetItemBoxIndex(j).itemId)
                {
                    if (itemList[i].itemType == ItemGenerator.ITEM_TYPE.LANTHANUM)
                    {
                        //所持しているときはtrueを返す
                        return true;
                    }
                }
            }
        }

        //未所持の時はfalseを返す
        return false;
    }

    //セーブウィンドウ上のオブジェクトの取得を行う関数
    void SaveWindowInit()
    {
        //ウィンドウ上のオブジェクトの取得
        savePageLabel = GameObject.FindGameObjectWithTag("SavePage");           //ページ数
        savePreviousButton = GameObject.FindGameObjectWithTag("SavePrevious");  //前ページ移動ボタン
        saveNextButton = GameObject.FindGameObjectWithTag("SaveNext");          //次ページ移動ボタン
        saveCloseButton = GameObject.FindGameObjectWithTag("SaveClose");        //クローズボタン
        saveGameEndButton = GameObject.FindGameObjectWithTag("SaveGameEnd");    //ゲーム終了ボタン
        saveWindow.SetActive(false);                                            //ウィンドウを閉じる
    }

    //セーブウィンドウの初期化を行う関数
    void SaveWindowClear()
    {
        //ページ移動ボタンを使用可能にする
        savePreviousButton.GetComponent<Button>().interactable = true;
        saveNextButton.GetComponent<Button>().interactable = true;

        //文字色（初期値）の取得
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        for (int i = 0; i < GameDataController.SAVE_PER_PAGE; i++)
        {
            Text number = saveNumberLabels[i].GetComponent<Text>();
            Text name = saveNameLabels[i].GetComponent<Text>();
            Text level = saveLevelLabels[i].GetComponent<Text>();
            Text floor = saveFloorLabels[i].GetComponent<Text>();
            Text time = saveTimeLabels[i].GetComponent<Text>();

            //テキストの初期化
            number.text = (i + 1).ToString();
            name.text = GameDataController.NO_DATA;
            level.text = "";
            floor.text = "";
            time.text = "";

            //文字色の変化
            number.color = labelColor;
            name.color = labelColor;
        }

    }

    //セーブウィンドウを開く関数
    void SaveWindowOpen()
    {
        //選択されたセーブデータ番号を保存しておく変数の初期化
        saveSaveNum = -1;
        //セーブウィンドウを開く
        saveFlag = true;
        saveWindow.SetActive(saveFlag);
        //セーブウィンドウの内容を表示する
        SaveWindowDisp();
    }

    //セーブウィンドウの内容を表示する関数
    void SaveWindowDisp()
    {
        //セーブウィンドウの初期化
        SaveWindowClear();
        SaveDataListDisp();

        //現在ページおよび最大ページを取得する
        int current = savePage;
        int page_last = savePageMax;

        //現在ページおよび最大ページを表示する
        string str = current.ToString() + " / " + page_last.ToString();
        Text page = savePageLabel.GetComponent<Text>();
        page.text = str;

        //現在のページに表示するセーブデータの開始番号の設定
        int save_start = (savePage - 1) * GameDataController.SAVE_PER_PAGE;

        //前ページ移動ボタンの設定
        if (current == 1)
        {
            //現在ページが最初の時、前ページ移動ボタンを使用不可にする
            savePreviousButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //現在ページが最初でない時、前ページ移動ボタンを使用可能にする
            savePreviousButton.GetComponent<Button>().interactable = true;
        }

        //次ページ移動ボタンの設定
        if (current == page_last)
        {
            //現在ページが最後の時、次ページ移動ボタンを使用不可にする
            saveNextButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //現在ページが最後でない時、次ページ移動ボタンを使用可能にする
            saveNextButton.GetComponent<Button>().interactable = true;
        }

        //セーブデータリストの内容を表示する
        for (int i = 0; i < GameDataController.SAVE_PER_PAGE; i++)
        {
            Text number = saveNumberLabels[i].GetComponent<Text>();
            //データ番号の表示
            number.text = (i + save_start + 1).ToString();
            //セーブデータの内容を表示
            SaveDataListDisp();
        }
    }

    //セーブデータリストの内容を表示をウィンドウに表示する関数
    void SaveDataListDisp()
    {
        //セーブデータリストへセーブデータファイルを読み込む
        GameDataController.GetSaveDataList();
        //現在のページに表示するセーブデータの開始番号の設定
        int save_start = (savePage - 1) * GameDataController.SAVE_PER_PAGE;

        //開始番号から順にセーブデータリストの内容をページごとの最大数まで表示させる
        for (int i = 0; i < GameDataController.SAVE_PER_PAGE; i++)
        {
            //セーブデータリストのデータを1つずつ取得する
            GameData data = GameDataController.GetSaveDataInfo(save_start + i);

            Text name = saveNameLabels[i].GetComponent<Text>();
            Text level = saveLevelLabels[i].GetComponent<Text>();
            Text floor = saveFloorLabels[i].GetComponent<Text>();
            Text time = saveTimeLabels[i].GetComponent<Text>();

            //取得したデータをテキストに表示する
            if (data == null)
            {
                //データがないときは空白を入れる（名前のみNO DATA）
                name.text = GameDataController.NO_DATA;
                level.text = "";
                floor.text = "";
                time.text = "";
            }
            else
            {
                //データがある時は名前、レベル、現在フロア、セーブ日時を表示する
                name.text = data.playerName;
                level.text = data.GetLevelString();
                floor.text = data.GetNowFloorString();
                time.text = data.GetSaveTimeString();
            }
        }
    }

    //セーブウィンドウを閉じる関数
    void SaveWindowClose()
    {
        //セーブウィンドウを閉じる
        saveFlag = false;
        saveWindow.SetActive(saveFlag);
        savePage = 1;
    }

    //セーブウィンドウ上のラベルの初期化を行う関数
    void SaveLabelInit()
    {
        //文字色（初期状態）の取得
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text numberText = saveNumberLabels[saveSaveNum].GetComponent<Text>();
        Text nameText = saveNameLabels[saveSaveNum].GetComponent<Text>();
        Text levelText = saveLevelLabels[saveSaveNum].GetComponent<Text>();
        Text floorText = saveFloorLabels[saveSaveNum].GetComponent<Text>();
        Text timeText = saveTimeLabels[saveSaveNum].GetComponent<Text>();

        //文字色の変更
        numberText.color = labelColor;
        nameText.color = labelColor;
        levelText.color = labelColor;
        floorText.color = labelColor;
        timeText.color = labelColor;

        //選択されたセーブデータ番号を保存しておく変数の初期化
        saveSaveNum = -1;
    }

    //セーブウィンドウ上のボタンの初期化を行う関数
    //引数
    //flag:trueの時はボタンを使用可能にし、falseの時はボタンを使用不可にする
    void SaveWindowButtonInit(bool flag)
    {
        //ウィンドウの最大ページ数を取得
        savePageMax = GameDataController.SavePageMaxCalc();

        //前ページ移動ボタンの設定
        if (savePage == 1)
        {
            //最初のページの時は前ページ移動ボタンを使用不可にする
            savePreviousButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最初のページでないとき時は引数に応じて、使用できるかどうかを設定する
            savePreviousButton.GetComponent<Button>().interactable = flag;
        }

        //次ページ移動ボタンの設定
        if (savePage == savePageMax)
        {
            //最後のページの時は使用不可にする
            saveNextButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最後のページでないとき時は引数に応じて、使用できるかどうかを設定する
            saveNextButton.GetComponent<Button>().interactable = flag;
        }

        //ウィンドウクローズボタンは引数に応じて、使用できるかどうかを設定する
        saveCloseButton.GetComponent<Button>().interactable = flag;
        //ゲーム終了ボタンは引数に応じて、使用できるかどうかを設定する
        saveGameEndButton.GetComponent<Button>().interactable = flag;

    }

    //セーブウィンドウクローズボタンをクリックしたときの処理を行う関数
    public void SaveCloseButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //コマンドボタンを使用可能にする
        commandPanel.GetComponentInChildren<ButtonController>().ButtonFlagChange(ButtonController.CMDTYPE.NORMAL);
        //セーブウィンドウを閉じる
        SaveWindowClose();

        //メッセージウィンドウを空白にする
        MessageController.msgController.MessageDisp("");
    }

    //セーブウィンドウの「前ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void SavePreviousButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ページ数を1減らす
        savePage--;
        //最初のページの時はそのままにする
        if (savePage < 1)
        {
            savePage = 1;
        }
        //ウィンドウの内容を更新する
        SaveWindowDisp();
    }

    //セーブウィンドウの「次ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void SaveNextButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ページ数を1増やす
        savePage++;
        //最後のページの時はそのままにする
        if (savePage > savePageMax)
        {
            savePage = savePageMax;
        }
        //ウィンドウの内容を更新する
        SaveWindowDisp();
    }

    //ゲーム終了ボタンをクリックしたときの処理を行う関数
    public void SaveGameEndButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //セーブウィンドウのボタンを使用不可にする
        SaveWindowButtonInit(false);
        //ゲームを終了するかどうかのYesNoウィンドウを表示する
        MessageController.msgController.MessageDisp("ゲームを終了してタイトル画面に戻りますか？\n");
        gameEndFlag = true;
        YesNoController.yesNoController.YesNoPanelOpen();
        //コマンドボタンを使用不可にする
        commandPanel.GetComponentInChildren<ButtonController>().CommandPanelOpen(ButtonController.CMDTYPE.YESNO);
    }

    //セーブウィンドウのプレイヤー名ラベルにマウスポインタが乗った時の処理を行う関数
    //引数
    //num:対象のプレイヤー名ラベルの番号
    public void SaveLabelOnMouseEnter(int num)
    {
        if (saveClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        if (gameEndFlag == true)
        {
            //ゲーム終了ボタンがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいたセーブデータリスト番号を取得
        int click_save = num + ((savePage - 1) * GameDataController.SAVE_PER_PAGE);

        //セーブデータの文字色取得（マウスポインタが乗った時）
        Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

        Text numberText = saveNumberLabels[num].GetComponent<Text>();
        Text nameText = saveNameLabels[num].GetComponent<Text>();
        Text levelText = saveLevelLabels[num].GetComponent<Text>();
        Text floorText = saveFloorLabels[num].GetComponent<Text>();
        Text timeText = saveTimeLabels[num].GetComponent<Text>();

        //データ番号、プレイヤー名、レベル、現在フロア、セーブ日時の色変更
        numberText.color = labelColor;
        nameText.color = labelColor;
        levelText.color = labelColor;
        floorText.color = labelColor;
        timeText.color = labelColor;
    }

    //セーブウィンドウのプレイヤー名ラベルからマウスポインタが離れた時の処理を行う関数
    //引数
    //num:対象のプレイヤー名ラベルの番号
    public void SaveLabelOnMouseExit(int num)
    {
        if (saveClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        if (gameEndFlag == true)
        {
            //ゲーム終了ボタンがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいたセーブデータリスト番号を取得
        int click_save = num + ((savePage - 1) * GameDataController.SAVE_PER_PAGE);

        //セーブデータの文字色取得（初期状態）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text numberText = saveNumberLabels[num].GetComponent<Text>();
        Text nameText = saveNameLabels[num].GetComponent<Text>();
        Text levelText = saveLevelLabels[num].GetComponent<Text>();
        Text floorText = saveFloorLabels[num].GetComponent<Text>();
        Text timeText = saveTimeLabels[num].GetComponent<Text>();

        //データ番号、プレイヤー名、レベル、現在フロア、セーブ日時の色変更
        numberText.color = labelColor;
        nameText.color = labelColor;
        levelText.color = labelColor;
        floorText.color = labelColor;
        timeText.color = labelColor;
    }

    //セーブウィンドウのプレイヤー名ラベルをクリックした時の処理を行う関数
    //引数
    //num:対象のプレイヤー名ラベルの番号
    public void SaveLabelOnMouseClick(int num)
    {
        //対象ラベルに基づいたセーブデータリスト番号を取得
        int click_save = num + ((savePage - 1) * GameDataController.SAVE_PER_PAGE);

        //対象セーブデータのラベル番号の取得
        saveSaveNum = num;

        string str = "";

        if (gameEndFlag == true)
        {
            //ゲーム終了ボタンがクリックされているときは何もしない
            return;
        }

        if (saveClickFlag == false)
        {
            //プレイヤー名ラベルがクリックされていないとき

            //ボタンをクリックしたときの効果音を鳴らす
            SoundManager.soundManager.PlaySE("se_decision");

            //セーブデータクリックフラグをオンにする
            saveClickFlag = true;

            //セーブデータの文字色取得（マウスポインタが乗った時）
            Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

            Text numberText = saveNumberLabels[num].GetComponent<Text>();
            Text nameText = saveNameLabels[num].GetComponent<Text>();
            Text levelText = saveLevelLabels[num].GetComponent<Text>();
            Text floorText = saveFloorLabels[num].GetComponent<Text>();
            Text timeText = saveTimeLabels[num].GetComponent<Text>();

            //データ番号、プレイヤー名、レベル、現在フロア、セーブ日時の色変更
            numberText.color = labelColor;
            nameText.color = labelColor;
            levelText.color = labelColor;
            floorText.color = labelColor;
            timeText.color = labelColor;

            //セーブウィンドウのボタンを使用不可にする
            SaveWindowButtonInit(false);

            //選択したデータの番号を取得
            str = (click_save + 1).ToString();

            //対象番号のファイルが存在しているかをチェックする
            if (GameDataController.SaveDataExistCheck(click_save) == false)
            {
                //存在していないとき
                str = str + "番にセーブしますか？\n";
            }
            else
            {
                //存在しているとき
                str = str + "番のファイルはすでに存在しますが、\n上書きしますか？\n";
            }

            //上記で作成したメッセージを表示
            MessageController.msgController.MessageDisp(str);

            //YesNoウィンドウを開く
            YesNoController.yesNoController.YesNoPanelOpen();

            //取得したセーブデータリスト番号を記憶させておく
            saveArrayNum = click_save;

        }
    }

    //フェード用イメージを初期化する関数
    //引数
    //flag:trueの時はイメージを黒にし、falseの時はイメージを透明にする
    void FadeImageInit(bool flag)
    {
        if (flag == true)
        {
            //フェード用イメージを黒に設定
            fadeImage.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        {
            //フェード用イメージを透明に設定
            fadeImage.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }
        
    }

    //ダンジョン背景を黒画面にフェードインさせる関数
    //引数
    //fadetime:フェードインにかける時間（秒）
    //戻り値（true:フェードイン終了、false:フェードイン途中）
    bool BlackFadeIn(float fadetime)
    {
        if (currentTime >= fadetime)
        {
            //フェードインが終了したとき、タイマーを初期化する
            currentTime = 0.0f;
            return true;
        }
        else if (currentTime <= 0.0f)
        {
            //フェードイン開始時

            //黒画像を透明にする
            fadeImage.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = currentTime / fadetime;
        fadeImage.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, c_color);

        return false;

    }

    //ダンジョン背景をフェードインさせる関数（被せた黒画像を透明にする）
    //引数
    //fadetime:フェードインにかける時間（秒）
    //戻り値（true:フェードイン終了、false:フェードイン途中）
    bool BlackFadeOut(float fadetime)
    {
        if (currentTime >= fadetime)
        {
            //フェードインが終了したとき、タイマーを初期化する
            currentTime = 0.0f;

            return true;
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = 1.0f - (currentTime / fadetime);
        fadeImage.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, c_color);

        return false;

    }

    //ダークゾーンの切り替えを行う関数
    //引数
    //flag:trueの時はダークゾーンをオンにし、falseの時はダークゾーンをオフにする
    void DarkZoneChange(bool flag)
    {
        if (flag == true)
        {
            darkZoneImage.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else
        {
            darkZoneImage.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }
    }

    #region デバッグ
    //----------------------------デバッグ用関数---------------------------------

    //マウスカーソルがラベルに乗った時の関数
    public void TestLabelEnter()
    {
        Debug.Log("入った");
    }

    //ラベルをクリックした時の関数
    public void TestLabelClick()
    {
        Debug.Log("押された");
    }

    //マウスカーソルがラベルから離れた時の関数
    public void TestLabelExit()
    {
        Debug.Log("出た");
    }
    #endregion

}
