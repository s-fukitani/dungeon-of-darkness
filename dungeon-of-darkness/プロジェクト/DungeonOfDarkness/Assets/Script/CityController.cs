using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Common;
using PlayerClass;
using ItemClass;
using EnemyClass;
using GameDataClass;
using BarClass;

//街内においてプレイヤーの制御を行うクラス
public class CityController : MonoBehaviour
{
    //プレイヤーの街における現在地の列挙型
    public enum CITY_PLACE
    {
        ENTRANCE,       //街の入口
        WEAPON_SHOP,    //武器屋
        ITEM_SHOP,      //道具屋
        INN,            //宿屋
        CASTLE,         //城
        BAR,            //酒場
        DUNGEON,        //ダンジョン入口
        SAVE            //保存
    }

    //店での用事の列挙型
    public enum BUY_SELL
    {
        NONE,           //なし
        BUY,            //買いに来た
        SELL            //売りに来た
    }

    //街の行動に関する列挙型
    public enum CITY_ACT_MODE
    {
        NONE,               //通常
        START,              //ゲーム開始時および迷宮から戻った時
        MOVING_INNER,       //施設内への移動
        MOVING_ENTRANCE,    //街の入口への移動
        INN_STAY,           //宿屋に宿泊
        IN_DUNGEON,         //迷宮内への移動
        IN_LAST_DUNGEON,    //迷宮内（最終フロア）へ移動
        GO_TITLE,           //タイトル画面への移動
        USING_ITEM          //アイテム使用中
    }

    //定数
    private const int SHOP_PER_PAGE = 10;   //商店のメニューの1ページごとのアイテム数
    private const int INN_PRICE = 10;       //宿屋の料金の基本値

    //街のBGMファイル名の配列（要素数にはSAVEの値を流用する）
    private string[] cityBGMArray = new string[(int)CITY_PLACE.SAVE] {
        "bgm_city_entrance",
        "bgm_weapon_shop",
        "bgm_item_shop",
        "bgm_inn",
        "bgm_castle",
        "bgm_bar",
        "bgm_dungeon_entrance"
    };

    //ゲーム制御に関するオブジェクト
    private EnemyGenerator enemyGenerator;      //敵生成オブジェクト
    private ItemGenerator itemGenerator;        //アイテム生成オブジェクト    
    private EventController eventController;    //イベント制御オブジェクト

    private Player player;                  //プレイヤーオブジェクト

    private CITY_PLACE cityPlace;           //街における現在地

    private CITY_ACT_MODE cityActMode;    //街での行動に関する現在の状態

    //ステータスウィンドウ（小）※コマンド表示時に画面右上に存在
    private GameObject statusPanel;     //ステータスウィンドウ（小）オブジェクト
    private Text LvText;                //レベル
    private Text hpText;                //HP
    private Text expText;               //経験値
    private Text goldText;              //ゴールド
    private Text conditionText;         //状態

    //コマンドパネル
    public GameObject commandPanel;     //全場所共通
    public GameObject entrancePanel;    //街の入口
    public GameObject shopPanel;        //店
    public GameObject innPanel;         //宿屋
    public GameObject castlePanel;      //城
    public GameObject castle2Panel;     //城（姫救出後）
    public GameObject barPanel;         //酒場
    public GameObject dungeonPanel;     //ダンジョン入口

    //ステータスウィンドウ
    public GameObject statusWindow;     //ステータスウィンドウオブジェクト
    private Text st_nameText;           //名前
    private Text st_levelText;          //レベル
    private Text st_hpText;             //HP
    private Text st_attackText;         //攻撃力
    private Text st_defenseText;        //防御力
    private Text st_speedText;          //素早さ
    private Text st_luckText;           //運
    private Text st_conditionText;      //状態
    private Text st_expText;            //経験値
    private Text st_goldText;           //ゴールド
    private Text st_nextLevelText;      //次のレベルまでの経験値
    private Text st_weaponText;         //装備武器
    private Text st_armorText;          //装備鎧
    private Text st_shieldText;         //装備盾
    private Text st_helmetText;         //装備兜
    private bool statusFlag;            //ステータスウィンドウフラグ（true:開いている、false:閉じている） 

    //アイテムウィンドウ
    public GameObject itemWindow;           //アイテムウィンドウオブジェクト      
    [SerializeField] private GameObject[] itemNameLabels;    //アイテム名ラベル
    [SerializeField] private GameObject[] itemCountLabels;   //アイテム数ラベル
    [SerializeField] private GameObject[] itemEquipLabels;   //アイテム装備表示ラベル
    private Text itemExplanation;           //アイテム説明文
    private GameObject itemPageLabel;       //アイテムウィンドウページ
    private GameObject itemPreviousButton;  //前ページ移動ボタン
    private GameObject itemNextButton;      //次ページ移動ボタン
    private GameObject itemCloseButton;     //ウィンドウクローズボタン
    private List<Item> itemList;            //全アイテムリスト
    private bool itemClickFlag;             //アイテム名クリックフラグ（true:クリックされた、false:クリックされていない）
    private int itemBoxNum;                 //選択された所持アイテムリスト番号
    private bool itemFlag;                  //選択されたアイテム名番号を保存しておく変数
    public int itemPage;                    //アイテムウィンドウの現在ページ
    public int itemPageMax;                 //アイテムウィンドウの最大ページ数
    private int saveItemNum;                //選択されたアイテム名番号を保存しておく変数

    //アイテム使用ウィンドウ
    public GameObject itemUsePanel;         //アイテム使用ウィンドウオブジェクト         
    private Text itemUseText;               //ウィンドウメッセージ
    private GameObject itemUseButton;       //使用ボタン
    private GameObject itemDiscardButton;   //廃棄ボタン
    private GameObject itemCancelButton;    //キャンセルボタン

    //背景画像オブジェクト
    private Sprite citySprite;              //街背景スプライト
    private Image cityImage;                //街背景イメージ

    //場所名ラベル画像オブジェクト
    private Sprite placeLabelSprite;        //場所名スプライト
    private Image placeLabelImage;          //場所名イメージ

    private List<string> weaponShopDatas;   //武器屋の商品一覧データのCSVの中身を入れるリスト
    private List<string> itemShopDatas;     //道具屋の商品一覧データのCSVの中身を入れるリスト

    private List<int> weaponShopList;       //武器屋の商品一覧（アイテムID）    
    private List<int> itemShopList;         //道具屋の商品一覧（アイテムID）

    private List<int> shopList;             //商品一覧（ショップウィンドウ表示用）

    private List<string[]> barDatas;        //酒場イベント一覧データのCSVの中身を入れるリスト

    private List<Bar> barList;              //酒場のイベント一覧
    private int barBeforeIndex;             //前に選択された酒場イベント一覧のインデックス番号

    //ショップウィンドウ
    public GameObject shopWindow;                               //ショップウィンドウオブジェクト
    [SerializeField] private GameObject[] shopNameLabels;       //アイテム名ラベル
    [SerializeField] private GameObject[] shopPriceLabels;      //価格ラベル
    private Text shopExplanation;                               //アイテム説明文
    private GameObject shopPageLabel;                           //ショップウィンドウページ
    private GameObject shopPreviousButton;                      //前ページ移動ボタン
    private GameObject shopNextButton;                          //次ページ移動ボタン
    private GameObject shopCloseButton;                         //ウィンドウクローズボタン
    public int shopPage;                                        //ショップウィンドウの現在ページ
    public int shopPageMax;                                     //ショップウィンドウの最大ページ数
    private int shopListNum;                                    //選択されたショップアイテムリスト番号
    private int saveShopNum;                                    //選択されたショップアイテム名番号を保存しておく変数
    private BUY_SELL buySellFlag;                               //店での用事フラグ

    //セーブウィンドウ
    public GameObject saveWindow;                               //セーブウィンドウオブジェクト
    [SerializeField] private GameObject[] saveNumberLabels;     //セーブデータ番号
    [SerializeField] private GameObject[] saveNameLabels;       //プレイヤー名
    [SerializeField] private GameObject[] saveLevelLabels;      //プレイヤーレベル
    [SerializeField] private GameObject[] saveFloorLabels;      //現在フロア
    [SerializeField] private GameObject[] saveTimeLabels;       //セーブ日時
    private GameObject savePageLabel;                           //セーブウィンドウページ
    private GameObject savePreviousButton;                      //前ページ移動ボタン
    private GameObject saveNextButton;                          //次ページ移動ボタン
    private GameObject saveCloseButton;                         //ウィンドウクローズボタン
    private GameObject saveGameEndButton;                       //ゲーム終了ボタン
    private bool saveFlag;                                      //セーブウィンドウフラグ（true:開いている、false:閉じている）
    public int savePage;                                        //セーブウィンドウの現在ページ
    public int savePageMax;                                     //セーブウィンドウの最大ページ数
    private bool saveClickFlag;                                 //セーブデータクリックフラグ（true:クリックされた、false:クリックされていない）
    private int saveArrayNum;                                   //選択されたセーブデータリスト番号
    private int saveSaveNum;                                    //選択されたセーブデータ番号を保存しておく変数
    private bool gameEndFlag;                                   //ゲーム終了ボタンクリックフラグ（true:クリックされている、false:クリックされていない）

    private int useItemId;                      //使用したアイテムのID
    private int preEquipBoxNumber;              //装備変更前に装備していたアイテムの所持アイテムリスト番号
    private int equipNumber;                    //装備アイテム種類番号
    private Item useItem;                       //使用したアイテムの情報

    private string cityMessage;                 //街内でのメッセージを収納する文字列

    private float currentTime = 0.0f;           //タイマーの現在時間（フェードイン、フェードアウトに使用）
    private int switchingCount = 0;             //街画面の切り替え演出の進行状況を示すカウンタ

    // Start is called before the first frame update
    void Start()
    {
        //乱数の初期化
        Random.InitState(System.DateTime.Now.Millisecond);

        //ゲーム制御に関するオブジェクトの取得
        enemyGenerator = GameObject.FindGameObjectWithTag("EnemyGenerator").GetComponent<EnemyGenerator>();
        itemGenerator = GameObject.FindGameObjectWithTag("ItemGenerator").GetComponent<ItemGenerator>();
        eventController = GameObject.FindGameObjectWithTag("EventController").GetComponent<EventController>();

        //フラグの初期化
        statusFlag = false;
        itemFlag = false;
        itemClickFlag = false;
        saveFlag = false;
        saveClickFlag = false;
        gameEndFlag = false;

        //全アイテムリストのデータ取得
        itemList = itemGenerator.GetItemList();
        //選択された所持アイテムリスト番号の初期化
        itemBoxNum = 0;
        //アイテム使用に関する変数、オブジェクトの初期化
        useItemId = 0;
        preEquipBoxNumber = -1;
        equipNumber = -1;
        useItem = new Item();
        //メッセージの初期化
        cityMessage = "";

        if (GameDataController.newGameFlag == true)
        {
            //ニューゲーム時

            //タイトル画面で作成したプレイヤーのデータを取得
            player = GameDataController.GetPlayerData();
            if (player == null)
            {
                #region デバッグ
                //街のシーンから始めた時はプレイヤーの初期化を行う（デバッグ用）
                player = new Player();

                //デバッグ用アイテム取得
                player.ItemDebugDataInput();

                //デバッグ用レベルアップ
                player.DebugLevelUp(19615);
                //player.DebugLevelUp(0);

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
                #endregion
            }
            //プレイヤーのダンジョン内イベントのフラグの初期化
            player.SetEventFlag(eventController.GetEventData());

            //街のイベントフラグ配列を初期化する
            eventController.CityEventFlagInit();

            #region デバッグ
            //衛兵フラグおよび姫救出フラグを立てる（デバッグ用）
            //eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.GUARD, 2);
            //eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS, 1);
            //状態異常設定（デバッグ用）
            //player.playerCondition = Player.PLAYER_CONDITION.SEALED;


            //初期カルマの設定（デバッグ用）
            //player.playerKarma = 0;

            #endregion

            GameDataController.newGameFlag = false;
        }
        else
        {
            //ニューゲームでないとき（ロード時もしくはダンジョンから戻った時）
            player = GameDataController.GetPlayerData();
            eventController.SetEventFlag(player);
            eventController.SetCityEventFlag(player);
        }

        //アイテムウィンドウのページの初期化
        itemPage = 1;
        itemPageMax = player.ItemPageMaxCalc();

        //セーブウィンドウのページの初期化
        savePage = 1;
        savePageMax = GameDataController.SavePageMaxCalc();

        cityPlace = CITY_PLACE.ENTRANCE;
        cityActMode = CITY_ACT_MODE.START;

        //ステータスウィンドウ（小）とコマンドパネルの初期化
        StatusInit();
        //ステータスウィンドウの初期化
        StatusWindowInit();
        //店での用事フラグの初期化
        buySellFlag = BUY_SELL.NONE;
        //アイテムウィンドウの初期化
        ItemWindowInit();
        //ショップウィンドウの初期化
        ShopWindowInit();
        //セーブウィンドウの初期化
        SaveWindowInit();

        //ショップアイテムファイルを読み込む
        ShopFileRead();
        //酒場情報データファイルを読み込む
        BarFileRead();

        //前に選択された酒場イベント一覧のインデックス番号の初期化
        barBeforeIndex = -1;

        //各施設用のコマンドパネルの初期化を行う
        CityPanelInit();

        //コマンドパネルを非表示にする
        commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelClose();
    }

    // Update is called once per frame
    void Update()
    {
        if (cityActMode == CITY_ACT_MODE.NONE)
        {
            //ステータスウィンドウ（小）を表示する
            StatusDisp();

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
                            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.SAVE);
                            saveClickFlag = false;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときはセーブをキャンセルする
                            SaveWindowButtonInit(true);
                            SaveLabelInit();
                            MessageController.msgController.MessageDisp("何番にセーブしますか？\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.SAVE);
                            saveClickFlag = false;
                        }
                    }
                    else
                    {
                        //ゲーム終了選択時
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したときはタイトル画面に戻る

                            //街での行動に関する現在の状態を「タイトル画面への移動」にする
                            cityActMode = CITY_ACT_MODE.GO_TITLE;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したときはセーブ対象ファイル選択へ戻る
                            SaveWindowButtonInit(true);
                            MessageController.msgController.MessageDisp("何番にセーブしますか？\n");
                            YesNoController.yesNoController.YesNoPanelClose();
                            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.SAVE);
                            gameEndFlag = false;
                        }
                    }
                }
                else
                {
                    //データセーブおよびゲーム終了以外
                    YesNoEvent();
                }
            }
            else
            {
                //YesNoウィンドウが開いていないときの処理

            }
        }
        else if (cityActMode == CITY_ACT_MODE.START)
        {
            //ゲーム開始時および迷宮から戻った時（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //街の入口背景フェードイン
                    if (BackImageFadeIn(1.0f, cityPlace) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    if (CommonMethod.TimeWait(1.0f) == true)
                    {
                        //街の入口を示すラベル画像を表示する
                        PlaceLabelImageDisp();
                        //コマンドボタンを表示する
                        commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
                        //各施設用のコマンドパネルを街の入り口用に設定する
                        CityPanelChange();
                        //ステータスウィンドウ（小）を表示する
                        StatusDisp();
                        //街の入口であることを知らせるメッセージを表示する
                        EnterPlace();
                        //街の入口のBGMを再生する
                        SoundManager.soundManager.PlayBGM(GetCityBGMFileName(cityPlace), 0.5f, true);
                        //街での行動に関する現在の状態を「通常」にする
                        cityActMode = CITY_ACT_MODE.NONE;
                        //カウンタの初期化
                        switchingCount = 0;

                    }
                    break;
            }

        }
        else if (cityActMode == CITY_ACT_MODE.MOVING_INNER)
        {
            //施設内への移動（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //BGMを停止する
                    SoundManager.soundManager.StopBGM(0.5f);
                    //各施設用のコマンドパネルの初期化を行う
                    CityPanelInit();
                    //コマンドパネルを非表示にする
                    commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelClose();
                    //ステータスウィンドウ（小）を非表示にする
                    StatusHidden();
                    //現在場所を示すラベル画像を非表示にする
                    PlaceLabelImageHidden();
                    //メッセージウィンドウを閉じる
                    MessageController.msgController.MessagePanelClose();

                    switchingCount++;
                    break;
                case 1:
                    //街の現在場所の背景のフェードアウト
                    if (BackImageFadeOut(0.5f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 2:
                    //移動先の背景をフェードイン
                    if (BackImageFadeIn(0.5f, cityPlace) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        //移動先を示すラベル画像を表示する
                        PlaceLabelImageDisp();
                        //コマンドボタンを表示する
                        if (cityPlace == CITY_PLACE.ENTRANCE)
                        {
                            //移動先が街の入口の時
                            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
                        }
                        else
                        {
                            //移動先が街の入口以外の時
                            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.INNER);
                        }
                        //各施設用のコマンドパネルを街の入り口用に設定する
                        CityPanelChange();
                        //ステータスウィンドウ（小）を表示する
                        StatusDisp();
                        //移動先のメッセージを表示する
                        EnterPlace();
                        //移動先のBGMを再生する
                        SoundManager.soundManager.PlayBGM(GetCityBGMFileName(cityPlace), 0.5f, true);
                        //街での行動に関する現在の状態を「通常」にする
                        cityActMode = CITY_ACT_MODE.NONE;
                        //カウンタの初期化
                        switchingCount = 0;
                    }
                    break;
            }
        }
        else if (cityActMode == CITY_ACT_MODE.MOVING_ENTRANCE)
        {
            //街の入口への移動（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //各施設用のコマンドパネルの初期化を行う
                    CityPanelInit();
                    //退出時のメッセージを表示する
                    LeavePlaceMessage();
                    //現在位置をチェックする
                    switch (cityPlace)
                    {
                        case CITY_PLACE.WEAPON_SHOP:
                        case CITY_PLACE.ITEM_SHOP:
                        case CITY_PLACE.INN:
                        case CITY_PLACE.BAR:
                            //武器屋、道具屋、宿屋、酒場の時は少し待ってから次の処理へ移る
                            if (CommonMethod.TimeWait(3.0f) == true)
                            {
                                switchingCount++;
                            }
                            break;
                        default:
                            //その他の場合は直ちに街の入口へ次の処理へ移る
                            switchingCount++;
                            break;
                    }
                    break;
                case 1:
                    //現在位置をチェックする
                    switch (cityPlace)
                    {
                        case CITY_PLACE.WEAPON_SHOP:
                        case CITY_PLACE.ITEM_SHOP:
                        case CITY_PLACE.INN:
                        case CITY_PLACE.BAR:
                            //武器屋、道具屋、宿屋、酒場の時はイベント画像を非表示にする
                            //イベント画像を非表示にする
                            EventImageController.evImageController.ImagePanelClose();
                            break;
                        default:
                            //その他の場合は何もしない
                            switchingCount++;
                            break;
                    }

                    //コマンドパネルを非表示にする
                    commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelClose();
                    //ステータスウィンドウ（小）を非表示にする
                    StatusHidden();
                    //現在場所を示すラベル画像を非表示にする
                    PlaceLabelImageHidden();
                    //メッセージウィンドウを閉じる
                    MessageController.msgController.MessagePanelClose();
                    //現在フロアを街の入口に設定する
                    player.NowFloorChange(-1);
                    //現在場所を「街の入口」にする
                    cityPlace = CITY_PLACE.ENTRANCE;
                    //BGMを停止する
                    SoundManager.soundManager.StopBGM(0.5f);

                    switchingCount++;
                    break;
                case 2:
                    //街の現在場所の背景のフェードアウト
                    if (BackImageFadeOut(0.5f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 3:
                    //移動先の背景をフェードイン
                    if (BackImageFadeIn(0.5f, cityPlace) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    //移動先を示すラベル画像を表示する
                    PlaceLabelImageDisp();
                    //コマンドボタンを表示する
                    commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
                    //各施設用のコマンドパネルを街の入り口用に設定する
                    CityPanelChange();
                    //ステータスウィンドウ（小）を表示する
                    StatusDisp();
                    //移動先のメッセージを表示する
                    EnterPlace();
                    //移動先のBGMを再生する
                    SoundManager.soundManager.PlayBGM(GetCityBGMFileName(cityPlace), 0.5f, true);
                    //街での行動に関する現在の状態を「通常」にする
                    cityActMode = CITY_ACT_MODE.NONE;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (cityActMode == CITY_ACT_MODE.INN_STAY)
        {
            //宿屋に宿泊（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //YesNoウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();
                    //宿泊時のメッセージを表示する
                    MessageController.msgController.MessageDisp("それでは、ごゆっくりおやすみください。\n");
                    //BGMを停止する
                    SoundManager.soundManager.StopBGM(0.5f);
                    switchingCount++;
                    break;
                case 1:
                    if (CommonMethod.TimeWait(0.2f) == true)
                    {
                        //イベント画像を消去する
                        EventImageController.evImageController.ImagePanelClose();
                        switchingCount++;
                    }
                    break;
                case 2:
                    //宿屋の背景のフェードアウト
                    if (BackImageFadeOut(1.0f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 3:
                    //宿泊のジングルを鳴らす
                    SoundManager.soundManager.PlayBGM("bgm_inn_jingle", 0.1f, false);
                    switchingCount++;
                    break;
                case 4:
                    //ジングルが鳴り終わるまで待機
                    if (CommonMethod.TimeWait(6.0f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 5:
                    //メッセージを消去する
                    MessageController.msgController.MessageDisp("");
                    switchingCount++;
                    break;
                case 6:
                    //宿屋の背景をフェードイン
                    if (BackImageFadeIn(1.0f, cityPlace) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    //宿屋のBGMを再生する
                    SoundManager.soundManager.PlayBGM(GetCityBGMFileName(cityPlace), 0.5f, true);
                    //イベント画像を表示する
                    EventImageController.evImageController.ImageDisp("inn_master");
                    //宿泊処理を実行する
                    InnStayProcess();
                    //街での行動に関する現在の状態を「通常」にする
                    cityActMode = CITY_ACT_MODE.NONE;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (cityActMode == CITY_ACT_MODE.GO_TITLE)
        {
            //タイトル画面への移動（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //YesNoウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();
                    //セーブウィンドウを閉じる
                    SaveWindowClose();
                    //BGMを停止する
                    SoundManager.soundManager.StopBGM(0.5f);
                    //各施設用のコマンドパネルの初期化を行う
                    CityPanelInit();
                    //コマンドパネルを非表示にする
                    commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelClose();
                    //ステータスウィンドウ（小）を非表示にする
                    StatusHidden();
                    //現在場所を示すラベル画像を非表示にする
                    PlaceLabelImageHidden();
                    //メッセージウィンドウを閉じる
                    MessageController.msgController.MessagePanelClose();

                    switchingCount++;
                    break;
                case 1:
                    //街の現在場所の背景のフェードアウト
                    if (BackImageFadeOut(0.5f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    //街での行動に関する現在の状態を「通常」にする
                    cityActMode = CITY_ACT_MODE.NONE;
                    //カウンタの初期化
                    switchingCount = 0;
                    //タイトル画面へ戻る
                    GoToTitle();
                    break;
            }
        }
        else if (cityActMode == CITY_ACT_MODE.IN_DUNGEON)
        {
            //迷宮内への移動（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //BGMを停止する
                    SoundManager.soundManager.StopBGM(0.5f);
                    //各施設用のコマンドパネルの初期化を行う
                    CityPanelInit();
                    //コマンドパネルを非表示にする
                    commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelClose();
                    //ステータスウィンドウ（小）を非表示にする
                    StatusHidden();
                    //現在場所を示すラベル画像を非表示にする
                    PlaceLabelImageHidden();
                    //メッセージウィンドウを閉じる
                    MessageController.msgController.MessagePanelClose();

                    switchingCount++;
                    break;
                case 1:
                    //迷宮に入る効果音を鳴らす
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        SoundManager.soundManager.PlaySE("se_stairs");
                        switchingCount++;
                    }
                    break;
                case 2:
                    //街の現在場所の背景のフェードアウト
                    if (BackImageFadeOut(0.75f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    //街での行動に関する現在の状態を「通常」にする
                    cityActMode = CITY_ACT_MODE.NONE;
                    //カウンタの初期化
                    switchingCount = 0;
                    //ダンジョンへ入る時の処理を実行
                    EnterDungeon(true);
                    break;
            }
        }
        else if (cityActMode == CITY_ACT_MODE.IN_LAST_DUNGEON)
        {
            //迷宮内（最終フロア）への移動（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //BGMを停止する
                    SoundManager.soundManager.StopBGM(0.5f);
                    //コマンドパネルのボタンを使用不可にする
                    commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.WAIT);
                    //迷宮入り口のコマンドパネルを非表示にする
                    dungeonPanel.SetActive(false);

                    switchingCount++;
                    break;
                case 1:
                    //最終フロアへ移動する効果音とエフェクトを再生
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        LargeEffectController.largeEffectController.PlayLargeEffect(LargeEffectController.MOVE_LAST);
                        SoundManager.soundManager.PlaySE("se_move_last");
                        switchingCount++;
                    }
                    break;
                case 2:
                    //効果音が鳴り終わるまで待機
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        //メッセージを消す
                        MessageController.msgController.MessageDisp("");
                        switchingCount++;
                    }
                    break;
                case 3:
                    //街の現在場所の背景のフェードアウト
                    if (BackImageFadeOut(0.5f) == true)
                    {
                        //フェードアウトが終わったらメッセージを表示する
                        MessageController.msgController.MessageDisp("すると、一瞬で辺りが闇に覆われ、\n何も見えなくなった！\n");
                        switchingCount++;
                    }
                    break;
                default:
                    if (CommonMethod.TimeWait(2.0f) == true)
                    {
                        //コマンドパネルを非表示にする
                        commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelClose();
                        //ステータスウィンドウ（小）を非表示にする
                        StatusHidden();
                        //現在場所を示すラベル画像を非表示にする
                        PlaceLabelImageHidden();
                        //メッセージウィンドウを閉じる
                        MessageController.msgController.MessagePanelClose();
                        //街での行動に関する現在の状態を「通常」にする
                        cityActMode = CITY_ACT_MODE.NONE;
                        //カウンタの初期化
                        switchingCount = 0;
                        //ダンジョンへ入る時の処理を実行
                        EnterDungeon(false);
                    }
                    break;
            }
        }
        else if (cityActMode == CITY_ACT_MODE.USING_ITEM)
        {
            //アイテム使用中（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //街入口もしくは迷宮入口の専用コマンドパネルを非表示にする
                    if (cityPlace == CITY_PLACE.ENTRANCE)
                    {
                        entrancePanel.SetActive(false);
                    }
                    else if (cityPlace == CITY_PLACE.DUNGEON)
                    {
                        dungeonPanel.SetActive(false);
                    }

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

                    switchingCount++;
                    break;
                case 1:
                    //アイテム使用のメッセージを表示する
                    //（装備アイテムの場合は装備処理も行う）

                    //アイテム使用メッセージの共通部分を格納
                    cityMessage = player.playerName + "は";

                    if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE)
                    {
                        //一般使い捨てアイテムの時
                        cityMessage = cityMessage + useItem.itemName + "を使った！\n";

                        switchingCount++;
                    }
                    else if (useItem.itemType == ItemGenerator.ITEM_TYPE.NORMAL)
                    {
                        //一般アイテムの時
                        cityMessage = "今使用する必要はない。\n";

                        //カウンタを終了処理へと進める
                        switchingCount = 99;
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
                            cityMessage = cityMessage + useItem.itemName + "を装備した！\n";
                        }
                        else
                        {
                            if (itemBoxNum == preEquipBoxNumber)
                            {
                                //現在装備中のアイテムが選択された時
                                //装備解除の処理を実行する
                                player.UnEquip(useItem, itemBoxNum);
                                cityMessage = cityMessage + useItem.itemName + "を装備から外した！\n";
                            }
                            else
                            {
                                //既に別の装備アイテムが装備されていた時
                                //前の装備アイテムの装備を解除する
                                Item unEquipItem = itemGenerator.GetItemInfo(player.GetItemBoxIndex(preEquipBoxNumber).itemId);
                                player.UnEquip(unEquipItem, preEquipBoxNumber);
                                //選択した装備アイテムの装備処理を実行する
                                player.Equip(useItem, itemBoxNum);
                                cityMessage = cityMessage + useItem.itemName + "を装備した！\n";
                            }
                        }
                        //カウンタを終了処理へと進める
                        switchingCount = 99;
                    }
                    else if (useItem.itemType == ItemGenerator.ITEM_TYPE.LANTHANUM ||
                                 useItem.itemType == ItemGenerator.ITEM_TYPE.TRUTH_BANGLE)
                    {
                        //ランタン、真実の腕輪のどれかの時
                        cityMessage = useItem.itemName + "は持っているだけで\n効果を発揮する。\n";
                        //カウンタを終了処理へと進める
                        switchingCount = 99;
                    }
                    else if (useItem.itemType == ItemGenerator.ITEM_TYPE.MINI_MAP || 
                                useItem.itemType == ItemGenerator.ITEM_TYPE.MAP)
                    {
                        //小型の地図、地図のどれかの時
                        cityMessage = "今使用する必要はない。\n";
                        //カウンタを終了処理へと進める
                        switchingCount = 99;
                    }
                    else if (useItem.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE ||
                                 useItem.itemType == ItemGenerator.ITEM_TYPE.EVENT)
                    {
                        //イベントアイテムの時
                        if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY4 &&
                            cityPlace == CITY_PLACE.DUNGEON)
                        {
                            //悪魔の像を迷宮入口で使用したとき
                            cityMessage = cityMessage + useItem.itemName + "を使った！\n";

                            //カウンタを終了処理へと進める
                            switchingCount = 99;
                        }
                        else
                        {
                            //上の条件以外の時
                            cityMessage = "今使用する必要はない。\n";

                            //カウンタを終了処理へと進める
                            switchingCount = 99;
                        }
                    }
                    else
                    {
                        //その他のアイテム
                        cityMessage = cityMessage + useItem.itemName + "を使った！\nしかし、何も起こらなかった。\n";
                        //カウンタを終了処理へと進める
                        switchingCount = 99;
                    }

                    //アイテム使用のメッセージを表示する
                    MessageController.msgController.MessageDisp(cityMessage);
                    break;
                case 2:
                    //一般使い捨てアイテムの時のみ音声およびエフェクト再生直前に間隔をあける

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
                                    switchingCount++;
                                }
                            }

                            if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.SEALED)
                            {
                                //封印状態回復アイテムの時

                                //一定時間後にカウンタを次の処理へと進める
                                if (CommonMethod.TimeWait(0.5f) == true)
                                {
                                    switchingCount++;
                                }
                            }

                            if (useItem.recoverType == ItemGenerator.ITEM_EFFECT_TYPE.POISON)
                            {
                                //毒状態回復アイテムの時

                                //一定時間後にカウンタを次の処理へと進める
                                if (CommonMethod.TimeWait(0.5f) == true)
                                {
                                    switchingCount++;
                                }
                            }
                        }
                    }
                    break;
                case 3:
                    //一般使い捨てアイテムの時のみエフェクトおよび音声の再生を行った後、メッセージの表示と使用時の処理を行う

                    //効果音の再生が終了したときに処理を開始する
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
                            switchingCount = 99;
                        }
                    }
                    break;
                default:
                    //アイテム使用の終了処理

                    //効果音の再生が終了したときに処理を開始する
                    if (SoundManager.soundManager.SEPlayingCheck() == false)
                    {
                        if (useItem.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.KEY4 &&
                            cityPlace == CITY_PLACE.DUNGEON)
                        {
                            //悪魔の像を迷宮入口で使用したとき
                            //街での行動に関する現在の状態を「迷宮内（最終フロア）へ移動」にする
                            cityActMode = CITY_ACT_MODE.IN_LAST_DUNGEON;
                        }
                        else
                        {
                            //上の条件以外の時

                            //街での行動に関する現在の状態を「通常」にする
                            cityActMode = CITY_ACT_MODE.NONE;

                            //街入口もしくは迷宮入口の時、コマンドパネルのボタンを使用可能にした後、
                            //それぞれの専用コマンドパネルを表示する
                            if (cityPlace == CITY_PLACE.ENTRANCE)
                            {
                                commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
                                entrancePanel.SetActive(true);
                            }
                            else if (cityPlace == CITY_PLACE.DUNGEON)
                            {
                                commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.INNER);
                                dungeonPanel.SetActive(true);
                            }
                        }
                        //カウンタの初期化
                        switchingCount = 0;
                    }
                    break;
            }
        }
    }

    //タイトル画面へ戻る関数
    void GoToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    //ステータスウィンドウ（小）を初期化する関数
    void StatusInit()
    {
        //ステータスウィンドウ（小）上のオブジェクト取得
        statusPanel = GameObject.Find("StatusPanel");
        LvText = GameObject.Find("LevelText").GetComponent<Text>();
        hpText = GameObject.Find("HPText").GetComponent<Text>();
        expText = GameObject.Find("ExpText").GetComponent<Text>();
        goldText = GameObject.Find("GoldText").GetComponent<Text>();
        conditionText = GameObject.Find("ConditionText").GetComponent<Text>();

        //ステータスウィンドウ（小）を非表示にする
        statusPanel.SetActive(false);
    }

    //ステータスウィンドウを初期化する関数
    void StatusWindowInit()
    {
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

        //ステータスウィンドウを表示もしくは非表示（フラグによる）にする
        statusWindow.SetActive(statusFlag);
    }

    //ステータスウィンドウ（小）の情報表示関数
    void StatusDisp()
    {
        //ステータスウィンドウ（小）を表示する
        statusPanel.SetActive(true);

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

    //ステータスウィンドウ（小）を非表示にする関数
    void StatusHidden()
    {
        statusPanel.SetActive(false);
    }

    //アイテムウィンドウを初期化する関数
    void ItemWindowInit()
    {
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

        //アイテムウィンドウを表示もしくは非表示（フラグによる）にする
        itemWindow.SetActive(itemFlag);
        //アイテム使用ウィンドウを非表示にする
        itemUsePanel.SetActive(false);
    }

    //セーブウィンドウを初期化する関数
    void SaveWindowInit()
    {
        //セーブウィンドウ上のオブジェクト取得
        savePageLabel = GameObject.FindGameObjectWithTag("SavePage");
        savePreviousButton = GameObject.FindGameObjectWithTag("SavePrevious");
        saveNextButton = GameObject.FindGameObjectWithTag("SaveNext");
        saveCloseButton = GameObject.FindGameObjectWithTag("SaveClose");
        saveGameEndButton = GameObject.FindGameObjectWithTag("SaveGameEnd");

        //セーブウィンドウを非表示にする
        saveWindow.SetActive(false);
    }

    //酒場情報データファイルを読み込む処理を行う関数
    void BarFileRead()
    {
        TextAsset csvBarFile;   //酒場のイベント一覧データのCSVファイル

        //酒場のイベント一覧データのCSVの中身を入れるリストの初期化
        barDatas = new List<string[]>();

        //酒場のイベント一覧データのCSVファイルの読込
        csvBarFile = Resources.Load(GlobalConst.DATA_DIR + "barfile") as TextAsset;
        StringReader readerBar = new StringReader(csvBarFile.text);

        //ファイルの中身をリストに入る
        while (readerBar.Peek() != -1)
        {
            string line = readerBar.ReadLine();
            barDatas.Add(line.Split(','));
        }

        //酒場のイベント一覧の初期化
        barList = new List<Bar>();

        int count = barDatas.Count;

        //リストのデータを酒場のイベント一覧に入れる
        for (int i = 0; i < count; i++)
        {
            Bar bar = new Bar();

            bar.barFloor = int.Parse(barDatas[i][(int)BarClass.BAR_HEADING.FLOOR]);
            //情報は表示時に改行が行われるようにする
            bar.barInfo = Regex.Unescape(barDatas[i][(int)BarClass.BAR_HEADING.INFO]);
            
            barList.Add(bar);
        }
    }

    //酒場のイベント設定の初期化を行う関数
    void BarEventInit()
    {
        //全ての酒場のイベントをチェックする
        for (int i = 0; i < barList.Count; i++)
        {
            //プレイヤーがこれまでに訪れた最下フロアと酒場のイベントの対象フロアが違う場合
            if (barList[i].barFloor != player.reachFloor)
            {
                //酒場のイベントフラグをオフにする
                barList[i].BarFlagChange(false);
            }
        }

        //全ての酒場のイベントをチェックする
        for (int i = 0; i < barList.Count; i++)
        {
            //酒場のイベントのフロアが-1（汎用）の時
            if (barList[i].barFloor == -1)
            {
                //酒場のイベントフラグをオンにする
                barList[i].BarFlagChange(true);
            }
        }

        //特定の酒場のイベントをチェックして、条件に該当したときはフラグを変更する

        //王様に会ったとき
        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.KING) > 0)
        {
            barList[0].BarFlagChange(false);
        }

        //ミストリル鉱石を渡すか到達フロアが地下3階ではなくなったとき
        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.WEAPON_MASTER) > 0 ||
            player.reachFloor > 3)
        {
            barList[13].BarFlagChange(false);
            barList[14].BarFlagChange(false);
        }

        //悪魔の像の情報を取得
        Item item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.KEY4);

        
        if (player.ItemHaveCheck(item) == true && player.reachFloor == 7)
        {
            //到達フロアが地下7階で悪魔の像を手に入れた時
            barList[29].BarFlagChange(true);
        }
        else
        {
            //上の条件に該当していないとき
            barList[29].BarFlagChange(false);
        }

        if (player.playerKarma > 0 || 
            eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) >= 3)
        {
            //プレイヤーのカルマが1以上あるとき、もしくは姫が死亡しているとき
            barList[33].BarFlagChange(true);
        }
        else
        {
            //上の条件に該当していないとき
            barList[33].BarFlagChange(false);
        }

        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) >= 3)
        {
            //姫が死亡しているとき
            barList[34].BarFlagChange(true);
        }
        else
        {
            //姫が生存しているとき
            barList[34].BarFlagChange(false);
        }

        if (player.playerKarma > 0)
        {
            //プレイヤーのカルマが1以上あるとき
            barList[35].BarFlagChange(true);
        }
        else
        {
            //プレイヤーのカルマが0のとき
            barList[35].BarFlagChange(false);
        }
    }

    //酒場のイベント一覧からランダムで酒場情報を返す関数
    //戻り値（酒場情報）
    string GetBarEventInfoRandom()
    {
        //前回の酒場情報とは違うフラグがオンの酒場情報が出るまでループを続ける
        while (true)
        {
            int rnd = Random.Range(0, barList.Count);
            if (barBeforeIndex != rnd)
            {
                if (barList[rnd].barFlag == true)
                {
                    barBeforeIndex = rnd;
                    return barList[rnd].barInfo;
                }
            }
        } 
    }

    //「武器屋」ボタンをクリックしたときの処理を行う関数
    public void WeaponShopButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //現在場所を「武器屋」にする
        cityPlace = CITY_PLACE.WEAPON_SHOP;
        //街での行動に関する現在の状態を「施設内への移動」にする
        cityActMode = CITY_ACT_MODE.MOVING_INNER;

    }

    //「道具屋」ボタンをクリックしたときの処理を行う関数
    public void ItemShopButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //現在場所を「道具屋」にする
        cityPlace = CITY_PLACE.ITEM_SHOP;
        //街での行動に関する現在の状態を「施設内への移動」にする
        cityActMode = CITY_ACT_MODE.MOVING_INNER;
    }

    //「宿屋」ボタンをクリックしたときの処理を行う関数
    public void InnButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //現在場所を「宿屋」にする
        cityPlace = CITY_PLACE.INN;
        //街での行動に関する現在の状態を「施設内への移動」にする
        cityActMode = CITY_ACT_MODE.MOVING_INNER;
    }

    //「お城」ボタンをクリックしたときの処理を行う関数
    public void CastleButtonClick()
    {
        Debug.Log("お城");
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //現在場所を「お城」にする
        cityPlace = CITY_PLACE.CASTLE;
        //街での行動に関する現在の状態を「施設内への移動」にする
        cityActMode = CITY_ACT_MODE.MOVING_INNER;
    }

    //「酒場」ボタンをクリックしたときの処理を行う関数
    public void BarButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //現在場所を「酒場」にする
        cityPlace = CITY_PLACE.BAR;
        //街での行動に関する現在の状態を「施設内への移動」にする
        cityActMode = CITY_ACT_MODE.MOVING_INNER;
    }

    //「迷宮入口」ボタンをクリックしたときの処理を行う関数
    public void DungeonButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.GUARD) == 0)
        {
            //王様に許可をもらっていないとき

            //衛兵の画像を表示
            EventImageController.evImageController.ImageDisp("guard");
            MessageController.msgController.MessageDisp("王の許可がない者を通すわけにはいきません。\n");
            //コマンドパネルのボタンを使用不可にする
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.WAIT);
            //街の入口のコマンドパネルを非表示にする
            entrancePanel.SetActive(false);
            //少ししてから、衛兵との会話の終了処理を行う
            Invoke("GuardTalkEnd", 3.0f);
        }
        else if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.GUARD) == 1)
        {
            //王様に許可をもらったとき（初めて）

            //衛兵の画像を表示
            EventImageController.evImageController.ImageDisp("guard");
            MessageController.msgController.MessageDisp("ご武運をお祈りいたします。\n");
            //コマンドパネルのボタンを使用不可にする
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.WAIT);
            //街の入口のコマンドパネルを非表示にする
            entrancePanel.SetActive(false);
            //少ししてから、衛兵との会話の終了処理を行う
            Invoke("GuardTalkEnd", 3.0f);
        }
        else
        {
            //王様に許可をもらったとき（2度目以降）

            //コマンドパネルのボタンを使用不可にする
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.WAIT);
            //現在フロアを0（迷宮入口）に設定する
            player.NowFloorChange(0);
            //現在場所を「迷宮入口」にする
            cityPlace = CITY_PLACE.DUNGEON;
            //街での行動に関する現在の状態を「施設内への移動」にする
            cityActMode = CITY_ACT_MODE.MOVING_INNER;

        }
    }

    //「保存／終了」ボタンをクリックしたときの処理を行う関数
    public void SaveButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //コマンドパネルのボタンを使用不可にする
        commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.SAVE);
        //セーブウィンドウを開く
        SaveWindowOpen();
        //メッセージを表示する
        MessageController.msgController.MessageDisp("何番にセーブしますか？\n");
    }

    //「出る」ボタンをクリックしたときの処理を行う関数
    public void LeaveButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");
        //街での行動に関する現在の状態を「施設内への移動」にする
        cityActMode = CITY_ACT_MODE.MOVING_ENTRANCE;
    }

    //背景画像を表示する処理を行う関数
    public void BackImageDisp()
    {
        string img = "";

        //現在位置をチェックして、その場所の背景画像ファイル名を取得する
        switch (cityPlace)
        {
            case CITY_PLACE.ENTRANCE:   //街の入口
                img = "entranceback";
                break;
            case CITY_PLACE.WEAPON_SHOP://武器屋
                img = "weaponback";
                break;
            case CITY_PLACE.ITEM_SHOP:  //道具屋
                img = "itemback";
                break;
            case CITY_PLACE.INN:        //宿屋
                img = "innback";
                break;
            case CITY_PLACE.CASTLE:     //お城
                img = "castleback";
                break;
            case CITY_PLACE.BAR:        //酒場
                img = "barback";
                break;
            case CITY_PLACE.DUNGEON:    //迷宮入口
                img = "dungeonback";
                break;
            default:
                //該当する場所がない時は街の入口のファイル名を取得する
                img = "entranceback";
                break;
        }

        //対象の背景画像をロードする
        string dispImg = GlobalConst.IMG_DIR + img;
        citySprite = Resources.Load<Sprite>(dispImg) as Sprite;
        //ロードした背景画像を表示する
        GameObject ob = GameObject.Find("CityImage");
        cityImage = ob.GetComponent<Image>();
        cityImage.sprite = citySprite;
    }

    //画面左上の場所名ラベル画像を表示する関数
    public void PlaceLabelImageDisp()
    {
        string img = "";
        //現在位置をチェックして、該当する場所名ラベル画像ファイル名を取得する
        switch (cityPlace)
        {
            case CITY_PLACE.ENTRANCE:   //街の入口
                img = "entrance_label";
                break;
            case CITY_PLACE.WEAPON_SHOP://武器屋
                img = "weapon_label";
                break;
            case CITY_PLACE.ITEM_SHOP:  //道具屋
                img = "item_label";
                break;
            case CITY_PLACE.INN:        //宿屋
                img = "inn_label";
                break;
            case CITY_PLACE.CASTLE:     //お城
                img = "castle_label";
                break;
            case CITY_PLACE.BAR:        //酒場
                img = "bar_label";
                break;
            case CITY_PLACE.DUNGEON:    //迷宮入口
                img = "dungeon_label";
                break;
            default:
                //該当する場所がない時は街の入口のファイル名を取得する
                img = "entrance_label";
                break;
        }

        //対象の場所名ラベル画像をロードする
        string dispImg = GlobalConst.IMG_DIR + img;
        placeLabelSprite = Resources.Load<Sprite>(dispImg) as Sprite;
        //ロードした場所名ラベル画像を表示する
        GameObject ob = GameObject.Find("PlaceLabelImage");
        placeLabelImage = ob.GetComponent<Image>();
        placeLabelImage.sprite = placeLabelSprite;
        placeLabelImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    }

    //画面左上の場所名ラベル画像を非表示にする関数
    public void PlaceLabelImageHidden()
    {
        placeLabelImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }

    //各施設用のコマンドパネルの初期化を行う関数
    void CityPanelInit()
    {
        //全てのコマンドパネルを非表示にする
        entrancePanel.SetActive(false);
        shopPanel.SetActive(false);
        innPanel.SetActive(false);
        castlePanel.SetActive(false);
        castle2Panel.SetActive(false);
        barPanel.SetActive(false);
        dungeonPanel.SetActive(false);
    }

    //各施設用のコマンドパネルの表示と非表示を行う関数
    void CityPanelChange()
    {
        //現在位置をチェックして、該当する施設用のコマンドパネルを表示し、それ以外を非表示にする
        switch (cityPlace)
        {
            case CITY_PLACE.ENTRANCE:   //街の入口
                entrancePanel.SetActive(true);
                shopPanel.SetActive(false);
                innPanel.SetActive(false);
                castlePanel.SetActive(false);
                castle2Panel.SetActive(false);
                barPanel.SetActive(false);
                dungeonPanel.SetActive(false);
                break;
            case CITY_PLACE.WEAPON_SHOP://武器屋
                entrancePanel.SetActive(false);
                shopPanel.SetActive(true);
                innPanel.SetActive(false);
                castlePanel.SetActive(false);
                castle2Panel.SetActive(false);
                barPanel.SetActive(false);
                dungeonPanel.SetActive(false);
                break;
            case CITY_PLACE.ITEM_SHOP:  //道具屋
                entrancePanel.SetActive(false);
                shopPanel.SetActive(true);
                innPanel.SetActive(false);
                castlePanel.SetActive(false);
                castle2Panel.SetActive(false);
                barPanel.SetActive(false);
                dungeonPanel.SetActive(false);
                break;
            case CITY_PLACE.INN:        //宿屋
                entrancePanel.SetActive(false);
                shopPanel.SetActive(false);
                innPanel.SetActive(true);
                castlePanel.SetActive(false);
                castle2Panel.SetActive(false);
                barPanel.SetActive(false);
                dungeonPanel.SetActive(false);
                break;
            case CITY_PLACE.CASTLE:     //お城
                entrancePanel.SetActive(false);
                shopPanel.SetActive(false);
                innPanel.SetActive(false);
                if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.KING) < 2)
                {
                    //姫救出前
                    castlePanel.SetActive(true);
                    castle2Panel.SetActive(false);
                }
                else
                {
                    //姫救出後
                    castlePanel.SetActive(false);
                    castle2Panel.SetActive(true);
                }
                barPanel.SetActive(false);
                dungeonPanel.SetActive(false);
                break;
            case CITY_PLACE.BAR:        //酒場
                entrancePanel.SetActive(false);
                shopPanel.SetActive(false);
                innPanel.SetActive(false);
                castlePanel.SetActive(false);
                castle2Panel.SetActive(false);
                barPanel.SetActive(true);
                dungeonPanel.SetActive(false);
                break;
            case CITY_PLACE.DUNGEON:    //迷宮入口
                entrancePanel.SetActive(false);
                shopPanel.SetActive(false);
                innPanel.SetActive(false);
                castlePanel.SetActive(false);
                castle2Panel.SetActive(false);
                barPanel.SetActive(false);
                dungeonPanel.SetActive(true);
                break;
            default:
                //該当する場所がない時は街の入口用のコマンドパネルを表示する
                entrancePanel.SetActive(true);
                shopPanel.SetActive(false);
                innPanel.SetActive(false);
                castlePanel.SetActive(false);
                castle2Panel.SetActive(false);
                barPanel.SetActive(false);
                dungeonPanel.SetActive(false);
                break;
        }
    }

    //「強さ」ボタンクリック時の処理を行う関数
    public void StatusWindowButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ステータスウィンドウを開く
        statusFlag = true;
        statusWindow.SetActive(statusFlag);
        StatusWindowDisp();
        //コマンドパネルのボタンを使用不可にする
        commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.STATUS);

        //迷宮入口および街入口の時はそれぞれの専用コマンドパネルを非表示にする
        if (cityPlace == CITY_PLACE.ENTRANCE)
        {
            entrancePanel.SetActive(!statusFlag);
        }
        else if (cityPlace == CITY_PLACE.DUNGEON)
        {
            dungeonPanel.SetActive(!statusFlag);
        }
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

    //ステータスウィンドウを閉じる関数
    void StatusWindowClose()
    {
        //ステータスウィンドウを閉じる
        statusFlag = false;
        statusWindow.SetActive(statusFlag);

        //迷宮入口もしくは街入口の時、それぞれの専用コマンドパネルを表示した後、
        //コマンドパネルのボタンを使用可能にする
        if (cityPlace == CITY_PLACE.ENTRANCE)
        {
            entrancePanel.SetActive(!statusFlag);
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
        }
        else if (cityPlace == CITY_PLACE.DUNGEON)
        {
            dungeonPanel.SetActive(!statusFlag);
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.INNER);
        }


    }

    //ステータスウィンドウの「閉じる」ボタンがクリックされた時の処理を行う関数
    public void StCloseButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");
        //ステータスウィンドウを閉じる
        StatusWindowClose();
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

    //アイテムウィンドウを開く関数
    void ItemWindowOpen()
    {
        //選択されたアイテム名番号を保存しておく変数の初期化（最初の番号が0のため初期値を-1に設定している）
        saveItemNum = -1;

        itemFlag = true;
        Text button = itemCloseButton.GetComponent<Button>().GetComponentInChildren<Text>();

        if (buySellFlag == BUY_SELL.NONE)
        {
            //買い物中でないとき

            //アイテムウィンドウクローズボタンのテキストを「閉じる」に変更する
            button.text = "閉じる";
            //コマンドパネルのボタンを使用不可にする
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.ITEM);
            //迷宮入口および街入口の時はそれぞれの専用コマンドパネルを非表示にする
            if (cityPlace == CITY_PLACE.ENTRANCE)
            {
                entrancePanel.SetActive(!itemFlag);
            }
            else if (cityPlace == CITY_PLACE.DUNGEON)
            {
                dungeonPanel.SetActive(!itemFlag);
            }
        }
        else
        {
            //買い物中のとき

            //アイテムウィンドウクローズボタンのテキストを「やめる」に変更する
            button.text = "やめる";
            //武器屋もしくは道具屋の時、店専用コマンドパネルを表示する
            if (cityPlace == CITY_PLACE.WEAPON_SHOP ||
                cityPlace == CITY_PLACE.ITEM_SHOP)
            {
                shopPanel.SetActive(!itemFlag);
            }
        }

        //アイテムウィンドウを表示する
        itemWindow.SetActive(itemFlag);
        //アイテムウィンドウの最大ページ数を取得する
        itemPageMax = player.ItemPageMaxCalc();
        //内容を表示する
        ItemWindowDisp();
    }

    //アイテムウィンドウを閉じる関数
    void ItemWindowClose()
    {
        //アイテムウィンドウを非表示にする
        itemFlag = false;
        itemWindow.SetActive(itemFlag);

        if (buySellFlag != BUY_SELL.NONE)
        {
            //買い物中の時はメッセージを表示して、買い物中状態を解除する

            //店の種類によって表示メッセージを変える
            if (cityPlace == CITY_PLACE.WEAPON_SHOP)
            {
                //武器屋
                MessageController.msgController.MessageDisp("他に用はあるかい？\n");
            }
            else if (cityPlace == CITY_PLACE.ITEM_SHOP)
            {
                //道具屋
                MessageController.msgController.MessageDisp("他にご用はありますか？\n");
            }
            buySellFlag = BUY_SELL.NONE;
        }

        //武器屋もしくは道具屋の時、店専用コマンドパネルを表示する
        if (cityPlace == CITY_PLACE.WEAPON_SHOP ||
            cityPlace == CITY_PLACE.ITEM_SHOP)
        {
            shopPanel.SetActive(!itemFlag);
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

    //アイテムウィンドウの内容を表示する関数
    void ItemWindowDisp()
    {
        #region デバッグ
        //-------------------------デバッグ---------------
        int cnt = 1;
        foreach (ItemBox box in player.GetItemBox())
        {
            Debug.Log(cnt.ToString() + ":" + box.itemEquiped);
            cnt++;
        }
        //-------------------------------------------------
        #endregion

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
                    //プレイヤーの所持アイテム数が1ページ当たりの表示アイテム数で割り切れるとき
                    //終端番号に所持アイテムラベル配列の長さを設定
                    label_end = itemNameLabels.Length;
                }
                else
                {
                    //プレイヤーの所持アイテム数が1ページ当たりの表示アイテム数で割り切れないとき
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

    //アイテム画像を表示する関数
    //引数
    //img:画像ファイル名
    void ItemImageDisp(string img)
    {
        Image itemImage;
        Sprite itemSprite;

        //指定した画像ファイルをロードする
        itemSprite = Resources.Load<Sprite>(GlobalConst.IMG_DIR + img) as Sprite;

        GameObject ob;

        //イメージオブジェクトを取得する
        if (shopWindow.activeSelf == true)
        {
            //ショップウィンドウが表示されているとき
            ob = GameObject.Find("ShopImage");
        }
        else
        {
            //アイテムウィンドウが表示されているとき
            ob = GameObject.Find("ItemImage");
        }

        //取得したイメージオブジェクトにアイテム画像を表示する
        itemImage = ob.GetComponent<Image>();
        itemImage.sprite = itemSprite;
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

    //アイテムウィンドウの「閉じる」ボタンがクリックされた時の処理を行う関数
    public void ItemCloseButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //アイテムウィンドウを閉じる
        ItemWindowClose();
        
        //街入口もしくは迷宮入口の時、以下の処理を行う
        //1.コマンドパネルのボタンを使用可能にする
        //2.それぞれの専用コマンドパネルを表示にする
        //3.メッセージウィンドウを空白にする
        if (cityPlace == CITY_PLACE.ENTRANCE)
        {
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
            entrancePanel.SetActive(true);
            MessageController.msgController.MessageDisp("");
        }
        else if (cityPlace == CITY_PLACE.DUNGEON)
        {
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.INNER);
            dungeonPanel.SetActive(true);
            MessageController.msgController.MessageDisp("");
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
        string str = "";

        //対象ラベルに基づいた所持アイテムリスト番号を取得
        int click_item = num + ((itemPage - 1) * Player.ITEM_PER_PAGE);

        //アイテム名ラベルの番号の取得
        saveItemNum = num;

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

                if (cityPlace == CITY_PLACE.WEAPON_SHOP || cityPlace == CITY_PLACE.ITEM_SHOP)
                {
                    //武器屋もしくは道具屋の時
                    if (item.salePrice > 0)
                    {
                        //対象アイテムが売却可能な時
                        if (cityPlace == CITY_PLACE.WEAPON_SHOP)
                        {
                            //武器屋の時
                            str = item.itemName + "は" + item.salePrice.ToString() + "ゴールドになるけど、\nそれでいいかい？\n";
                        }
                        else
                        {
                            //道具屋の時
                            str = item.itemName + "は" + item.salePrice.ToString() + "ゴールドになりますが、\nよろしいですか？\n";
                        }
                        //上記で作成したメッセージを表示する
                        MessageController.msgController.MessageDisp(str);
                        //YesNoウィンドウを開く
                        YesNoController.yesNoController.YesNoPanelOpen();
                        //アイテムウィンドウのボタンを使用不可にする
                        ItemWindowButtonInit(false);
                    }
                    else
                    {
                        //対象アイテムが売却不可な時
                        if (cityPlace == CITY_PLACE.WEAPON_SHOP)
                        {
                            //武器屋の時
                            if (item.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.ORE && 
                                eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.WEAPON_MASTER) == 0)
                            {
                                //ミストリル鉱石を選択したとき
                                str = "気が変わったんだな、前にも言ったが\n" + EventController.ORE_PRICE + "ゴールドで譲っちゃくれねえか？";

                                //上記で作成したメッセージを表示する
                                MessageController.msgController.MessageDisp(str);
                                //YesNoウィンドウを開く
                                YesNoController.yesNoController.YesNoPanelOpen();
                                //アイテムウィンドウのボタンを使用不可にする
                                ItemWindowButtonInit(false);
                            }
                            else
                            {
                                //その他
                                str = "すまねえが、\nうちでそれは引き取れねえな。\n";

                                //上記で作成したメッセージを表示する
                                MessageController.msgController.MessageDisp(str);
                                //アイテムウィンドウのボタンを使用可能にする
                                ItemWindowButtonInit(true);
                                //アイテム名クリックフラグをオフ
                                itemClickFlag = false;
                            }
                            
                        }
                        else
                        {
                            //道具屋の時
                            str = "申し訳ございませんが、\nうちでそれは引き取れません。\n";

                            //上記で作成したメッセージを表示する
                            MessageController.msgController.MessageDisp(str);
                            //アイテムウィンドウのボタンを使用可能にする
                            ItemWindowButtonInit(true);
                            //アイテム名クリックフラグをオフ
                            itemClickFlag = false;
                        }

                    }

                }
                else
                {
                    //その他の時

                    ////アイテム使用ウィンドウを開く
                    itemUsePanel.SetActive(true);
                    //アイテム使用ウィンドウに対象アイテム名を表示させる
                    itemUseText.text = item.itemName;
                    //アイテムウィンドウのボタンを使用不可にする
                    ItemWindowButtonInit(false);
                }

                //取得した所持アイテムリスト番号を記憶させておく
                itemBoxNum = click_item;

            }
        }

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

    //アイテムウィンドウ上のラベルの初期化を行う関数
    void ItemLabelInit()
    {
        //アイテム情報の文字色取得（初期値）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text equipText = itemEquipLabels[saveItemNum].GetComponent<Text>();
        Text nameText = itemNameLabels[saveItemNum].GetComponent<Text>();
        Text countText = itemCountLabels[saveItemNum].GetComponent<Text>();

        //アイテム名、数量、装備中の色変更
        equipText.color = labelColor;
        nameText.color = labelColor;
        countText.color = labelColor;

        //アイテム情報の初期化
        Text explain = itemExplanation.GetComponent<Text>();
        explain.text = "";

        //アイテム画像の初期化
        ItemImageDisp("NoItem");

        //選択されたアイテム名番号を保存しておく変数の初期化（最初の番号が0のため初期値を-1に設定している）
        saveItemNum = -1;
    }

    //アイテム使用ボタンをクリックした時の処理を行う関数
    public void ItemUseButtonClick()
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
        //街での行動に関する現在の状態を「アイテム使用中」にする
        cityActMode = CITY_ACT_MODE.USING_ITEM;
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

        //街入口もしくは迷宮入口の時、コマンドパネルのボタンを使用可能にした後、
        //それぞれの専用コマンドパネルを表示にする
        if (cityPlace == CITY_PLACE.ENTRANCE)
        {
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
            entrancePanel.SetActive(true);
        }
        else if (cityPlace == CITY_PLACE.DUNGEON)
        {
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.INNER);
            dungeonPanel.SetActive(true);
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
    }

    //ダンジョンへ入る時の処理を実行する関数
    //引数
    //start_floor:迷宮に入ったときの開始フロアが地下1階かどうかを判定するフラグ（true:地下1階、false:最終フロア）
    void EnterDungeon(bool start_floor)
    {
        if (start_floor == true)
        {
            //開始フロアが地下1階の時

            //現在フロアを地下1階に設定する
            player.NowFloorChange(1);
            //ダンジョンに入った直後の現在座標と向いている方角を設定する
            player.NowPositionSet(1, 24);
            player.NowDirectionSet(Player.DIRECTION.NORTH);
        }
        else
        {
            //開始フロアが最終フロアの時

            //現在フロアを最終フロアに設定する
            player.NowFloorChange(GlobalConst.FLOOR_MAX);
            //ダンジョンに入った直後の現在座標と向いている方角を設定する
            player.NowPositionSet(12, 24);
            player.NowDirectionSet(Player.DIRECTION.NORTH);
        }
        
        //プレイヤーのダンジョン内イベントのフラグの更新
        player.SetEventFlag(eventController.GetEventData());
        //プレイヤーの街内イベントフラグの更新
        player.SetCityEventFlagArray(eventController.GetCityEventFlagArray());
        //シーン移行時のプレイヤーデータの受け渡しの準備を行う
        GameDataController.SetPlayerData(player);
        //ダンジョンへ移動する
        SceneManager.LoadScene("DungeonScene");
    }

    //「迷宮に入る」ボタンをクリックしたときの処理を行う関数
    public void EnterButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //街での行動に関する現在の状態を「迷宮内への移動」にする
        cityActMode = CITY_ACT_MODE.IN_DUNGEON;
    }

    //「王様に会う」ボタンをクリックしたときの処理を行う関数
    public void KingButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //王様に会う時の処理を実行
        KingAudience();
    }

    //「姫に会う」ボタンをクリックしたときの処理を行う関数
    public void PrincessButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //姫に会う時の処理を実行
        PrincessAudience();
    }

    //「話す」ボタンをクリックしたときの処理を行う関数
    public void BarTalkButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //酒場情報を取得する
        string msg = GetBarEventInfoRandom();
        //メッセージを表示する
        MessageController.msgController.MessageDisp(msg);
    }

    //王様に会う時の処理を行う関数
    void KingAudience()
    {
        //王様の画像を表示
        EventImageController.evImageController.ImageDisp("king");
        //王様の台詞を表示
        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.KING) == 0)
        {
            //初対面の時
            MessageController.msgController.MessageDisp("迷宮に挑むのだな、衛兵には許可を出しておこう。\n" + 
                                            "そなたに" + EventController.FROM_KING + 
                                            "ゴールドを渡そう。これで準備を\n" + 
                                            "整えてくれ。後、この水晶玉も授ける。きっと役に\n" +
                                            "立つであろう。この国とエリナを救ってくれ、頼んだぞ。\n");

            //金と水晶玉を渡す
            player.GoldGet(EventController.FROM_KING);
            Item item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.CRYSTAL);
            player.ItemGet(item);

            //王様フラグを1（2回目以上の謁見）に変更する
            eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.KING, 1);
            //衛兵フラグを1（通行可能にする）
            eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.GUARD, 1);
        }
        else if(eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.KING) == 1)
        {
            //初対面でないとき
            if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) == 0)
            {
                //姫救出前
                MessageController.msgController.MessageDisp("エリナはまだ見つからないのか、早く頼むぞ。\n" + "娘のことを思うと夜も眠れぬ。\n");
            }
            else if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) == 1)
            {
                //姫救出後（初めて）

                MessageController.msgController.MessageDisp("よくぞ、娘を助け出してくれた、礼を言う。\n" + 
                                                "後は魔物達の王を倒すだけじゃ、頼んだぞ。\n");
                //王様フラグを2（姫救出後）に変更する
                eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.KING, 2);
            }
            else if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) == 3)
            {
                //姫死亡時
                MessageController.msgController.MessageDisp("エリナはまだ見つからないのか、早く頼むぞ。\n" + "娘のことを思うと夜も眠れぬ。\n");
            }
            
        }
        else if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.KING) == 2)
        {
            //姫救出後（2回目以上）
            MessageController.msgController.MessageDisp("娘がそなたのことを心配しておる。\n時々でよいので会いに行ってはくれぬか。\n");
        }

        //お城用コマンドパネルを非表示にする
        castlePanel.SetActive(false);
        castle2Panel.SetActive(false);
        //少し後に謁見終了処理を実行
        Invoke("AudienceEnd", 6.0f);
    }

    //姫に会う時の処理を行う関数
    void PrincessAudience()
    {
        //姫の画像を表示
        EventImageController.evImageController.ImageDisp("princess");

        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) == 1)
        {
            //お城で初めて会う時

            //指輪の情報を取得
            Item item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_FIGHT.RING);

            //姫の基本メッセージを作成する
            string msg = "助けていただきありがとうございます。\n" +
                         "お礼にこの指輪を差し上げます。\n";

            Player.PICK_COMPLETION pick = player.ItemPick(item);
            if (pick == Player.PICK_COMPLETION.OK)
            {
                //持ち物に空きがある時は指輪を取得する

                msg = msg + "この指輪には魔を祓う聖なる力があります。\n" +
                            "どうか、無事に帰ってきてください。\n";

                //姫フラグを2に変更する
                eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS, 2);
            }
            else
            {
                //持ち物が一杯の時

                msg = msg + "でも、これ以上お持ちになれないようですね。\n" + 
                            "後ほどまたいらしてください。\n";
            }

            //メッセージを表示する
            MessageController.msgController.MessageDisp(msg);

        }
        else if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.PRINCESS) == 2)
        {
            //お城で会うのが2度目以上の時

            MessageController.msgController.MessageDisp("あなたの無事をお祈りしております。\n");

            //カルマが一定数未満の時、状態を祝福にする
            if (player.playerKarma < 4)
            {
                player.ChangeCondition(Player.PLAYER_CONDITION.BLESSING);
            }
            
        }

        //お城用コマンドパネルを非表示にする
        castlePanel.SetActive(false);
        castle2Panel.SetActive(false);
        //少し後に謁見終了処理を実行
        Invoke("AudienceEnd", 5.0f);
    }

    //謁見終了処理を行う関数
    void AudienceEnd()
    {
        //画像を消去
        EventImageController.evImageController.ImagePanelClose();
        //台詞を消去
        MessageController.msgController.MessageClear();
        //お城用コマンドパネルを表示する
        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.KING) < 2)
        {
            //姫救出前
            castlePanel.SetActive(true);
            castle2Panel.SetActive(false);
        }
        else
        {
            //姫救出後
            castlePanel.SetActive(false);
            castle2Panel.SetActive(true);
        }
    }

    //衛兵との会話の終了処理を行う関数
    void GuardTalkEnd()
    {
        //衛兵の画像を消去
        EventImageController.evImageController.ImagePanelClose();
        //衛兵の台詞を消去
        MessageController.msgController.MessageClear();

        if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.GUARD) == 0)
        {
            //王様に許可をもらっていないとき

            //コマンドパネルのボタンを使用可能にする
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
            //街の入口のコマンドパネルを表示する
            entrancePanel.SetActive(true);
        }
        else if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.GUARD) == 1)
        {
            //王様に許可をもらったとき（初めて）

            //コマンドパネルのボタンを使用不可にする
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.WAIT);

            //現在フロアを0（迷宮入口）に設定する
            player.NowFloorChange(0);
            //現在場所を「迷宮入口」にする
            cityPlace = CITY_PLACE.DUNGEON;
            //街での行動に関する現在の状態を「施設内への移動」にする
            cityActMode = CITY_ACT_MODE.MOVING_INNER;

            //衛兵フラグを2（無人）に変更する
            eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.GUARD, 2);

        }

    }

    //街のそれぞれの場所に入った時の処理を行う関数
    void EnterPlace()
    {
        //それぞれの場所に対応した処理を行う
        switch (cityPlace)
        {
            case CITY_PLACE.ENTRANCE:   //街の入口
                MessageController.msgController.MessageDisp("街の入口だ。\n多くの人々が行き交っている。\n");
                break;
            case CITY_PLACE.WEAPON_SHOP://武器屋
                //店主の画像の表示
                EventImageController.evImageController.ImageDisp("weapon_master");

                Item item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.ORE);
                bool have_flag = player.ItemHaveCheck(item);

                //フラグによるイベント分岐
                if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.WEAPON_MASTER) == 0 && 
                    have_flag == true)
                {
                    //ミストリル鉱石を渡していない状態でミストリル鉱石を所持している場合
                    MessageController.msgController.MessageDisp("おい、あんたの持ってるそれ、\n" +
                        "あの" + item.itemName + "じゃねえか。これがあれば、\nもっといい武器防具が作れる。どうだい、\n" +
                        EventController.ORE_PRICE + "ゴールドで譲っちゃくれねえか？\n");
                    //YesNoウィンドウを開く
                    YesNoController.yesNoController.YesNoPanelOpen();
                    //店用のコマンドパネルを非表示にする
                    shopPanel.SetActive(false);
                }
                else
                {
                    //その他
                    MessageController.msgController.MessageDisp("へい、らっしゃい！\nここは武器防具の店だ。\nお客さん、何の用だい？");
                }

                //ショップリストを作成する
                ShopListMake(cityPlace);
                break;
            case CITY_PLACE.ITEM_SHOP:  //道具屋
                EventImageController.evImageController.ImageDisp("item_master");
                MessageController.msgController.MessageDisp("いらっしゃいませ、ここは道具屋です。\nどの様なご用でしょうか？\n");
                //ショップリストを作成する
                ShopListMake(cityPlace);
                break;
            case CITY_PLACE.INN:        //宿屋
                EventImageController.evImageController.ImageDisp("inn_master");
                MessageController.msgController.MessageDisp("いらっしゃいませ、宿屋へようこそ。\nお泊りになりますか？\n");
                break;
            case CITY_PLACE.CASTLE:     //お城
                MessageController.msgController.MessageDisp("お城の中だ。\n荘厳な雰囲気が漂っている。");
                break;
            case CITY_PLACE.BAR:        //酒場
                EventImageController.evImageController.ImageDisp("bar_master");
                MessageController.msgController.MessageDisp("いらっしゃい、ここは酒場だ。\nゆっくりしていってくんな。\n");
                //酒場のイベント設定の初期化
                BarEventInit();
                break;
            case CITY_PLACE.DUNGEON:    //迷宮入口
                MessageController.msgController.MessageDisp("迷宮の入口だ。\n奥から不気味な気配が漂ってくる。\n");
                break;
            default:
                break;
        }
    }

    //街のそれぞれの場所から出た時の処理を行う関数
    void LeavePlaceMessage()
    {
        //それぞれの場所に対応したメッセージを表示する
        switch (cityPlace)
        {
            case CITY_PLACE.ENTRANCE:   //街の入口
                break;
            case CITY_PLACE.WEAPON_SHOP://武器屋
                MessageController.msgController.MessageDisp("また来いよ！\n");
                break;
            case CITY_PLACE.ITEM_SHOP:  //道具屋
                MessageController.msgController.MessageDisp("またのご来店をお待ちしております。\n");
                break;
            case CITY_PLACE.INN:        //宿屋
                MessageController.msgController.MessageDisp("またのお越しをお待ちしております。\n");
                break;
            case CITY_PLACE.CASTLE:     //お城
                break;
            case CITY_PLACE.BAR:        //酒場
                MessageController.msgController.MessageDisp("また来てくんな！\n");
                break;
            case CITY_PLACE.DUNGEON:    //迷宮入口
                break;
            default:
                break;
        }

    }

    //「はい」と「いいえ」の選択があるイベントの処理を行う関数
    void YesNoEvent()
    {
        switch (cityPlace)
        {
            case CITY_PLACE.INN:
                //宿屋
                if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                {
                    //「はい」が選択された時
                    if (player.playerGold >= InnPriceCalc())
                    {
                        //ゴールドが宿代以上の時

                        //街での行動に関する現在の状態を「宿屋に宿泊」にする
                        cityActMode = CITY_ACT_MODE.INN_STAY;
                    }
                    else
                    {
                        //ゴールドが宿代未満の時

                        YesNoController.yesNoController.YesNoPanelClose();
                        MessageController.msgController.MessageDisp("申し訳ありませんが、\nお金が足りないようですね。\n");
                        //宿屋用コマンドパネルを表示する
                        innPanel.SetActive(true);
                    }

                }
                else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                {
                    //「いいえ」が選択された時

                    YesNoController.yesNoController.YesNoPanelClose();
                    MessageController.msgController.MessageDisp("では、またの機会にお越しください。\n");
                    //宿屋用コマンドパネルを表示する
                    innPanel.SetActive(true);
                }
                break;
            case CITY_PLACE.WEAPON_SHOP:
                //武器屋
                if (buySellFlag == BUY_SELL.BUY)
                {
                    //買いに来た
                    if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                    {
                        //「はい」が選択された時

                        //購入アイテムのデータを取得
                        Item item = itemGenerator.GetItemInfo(shopList[shopListNum]);

                        YesNoController.yesNoController.YesNoPanelClose();

                        //購入の成否情報を取得する
                        Player.BUY_COMPLETION buy_comp = player.ItemBuy(item);

                        //それぞれの状況に応じたメッセージを表示する
                        if (buy_comp == Player.BUY_COMPLETION.OK)
                        {
                            //購入成功の時
                            MessageController.msgController.MessageDisp("まいどあり！\n他にも何か買うかい？\n");
                        }
                        else if (buy_comp == Player.BUY_COMPLETION.NG_MONEY)
                        {
                            //ゴールドが足りなかった時
                            MessageController.msgController.MessageDisp("悪いがそれを買うには金が足りねえな。\n");
                        }
                        else if (buy_comp == Player.BUY_COMPLETION.NG_BOX_MAX)
                        {
                            //アイテム所持数が最大値に達していた時
                            MessageController.msgController.MessageDisp("悪いがそれ以上持てねえようだな。\n" + 
                                                            "持ち物を整理してからまた来な。\n");
                        }
                        else if (buy_comp == Player.BUY_COMPLETION.NG_ITEM_MAX)
                        {
                            //購入予定のアイテム（使い捨てアイテム）所持数が最大値に達していた時
                            string str = "悪いが" + item.itemName + "は\n" + 
                                        "これ以上持てねえようだな。\n" +
                                        "持ち物を整理してからまた来な。\n";
                            MessageController.msgController.MessageDisp(str);
                        }

                        //ショップウィンドウ上のラベルの初期化
                        ShopLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ShopWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                    else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                    {
                        //「いいえ」が選択された時

                        YesNoController.yesNoController.YesNoPanelClose();
                        MessageController.msgController.MessageDisp("そうかい、他に何か買うかい？\n");

                        //ショップウィンドウ上のラベルの初期化
                        ShopLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ShopWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                }
                else if (buySellFlag == BUY_SELL.SELL)
                {
                    //売りに来た

                    //売却アイテムのデータを取得
                    Item item = itemGenerator.GetItemInfo(player.GetItemBoxIndex(itemBoxNum).itemId);

                    if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                    {
                        //「はい」が選択された時

                        YesNoController.yesNoController.YesNoPanelClose();

                        if (item.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.ORE)
                        {
                            //ミストリル鉱石が選択された時

                            MessageController.msgController.MessageDisp("ありがとうよ、次にあんたが店に来るまでに\n" +
                                                            "作っておくから、待っててくれ。これが礼の\n" +
                                                            EventController.ORE_PRICE + "ゴールドだ、受け取りな！\n" +
                                                            "他に売るものはあるかい？\n");

                            //ミストリル鉱石をアイテム欄より削除する
                            int box_index = player.GetItemBoxSpecifyIndex(item);
                            player.BoxItemDelete(box_index);

                            //ゴールドを受け取る
                            player.GoldGet(EventController.ORE_PRICE);

                            //武器屋の店主フラグを1にする（ミストリル鉱石譲渡済にする）
                            eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.WEAPON_MASTER, 1);

                        }
                        else
                        {
                            //その他

                            //売却処理を行う
                            player.ItemSell(itemGenerator.GetItemList(), itemBoxNum);
                            MessageController.msgController.MessageDisp("まいどあり！\n他に売るものはあるかい？\n");
                        }
                        

                        //アイテムウィンドウの内容を更新する
                        ItemWindowDisp();
                        //ショップウィンドウ上のラベルの初期化
                        ItemLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ItemWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                    else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                    {
                        //「いいえ」が選択された時

                        YesNoController.yesNoController.YesNoPanelClose();

                        if (item.itemId == (int)ItemGenerator.EVITEM_NONE_FIGHT.ORE)
                        {
                            //ミストリル鉱石が選択された時
                            MessageController.msgController.MessageDisp("そうかい、それは残念だ。\n気が変わったら、また来てくれよ。\n" +
                                                            "他に売るものはあるかい？\n");

                        }
                        else
                        {
                            //その他
                            MessageController.msgController.MessageDisp("そうかい、\n他に売るものはあるかい？\n");
                        }

                        //ショップウィンドウ上のラベルの初期化
                        ItemLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ItemWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                }
                else
                {
                    //その他
                    if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.WEAPON_MASTER) == 0)
                    {
                        //ミストリル鉱石を渡していない状態で、ミストリル鉱石を所持している場合
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」が選択された時

                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessageDisp("ありがとうよ、次にあんたが店に来るまでに\n" + 
                                                            "作っておくから、待っててくれ。これが礼の\n" +  
                                                            EventController.ORE_PRICE + "ゴールドだ、受け取りな！\n" +
                                                            "他に用はあるかい？\n");

                            //ミストリル鉱石をアイテム欄より削除する
                            Item item = itemGenerator.GetItemInfo((int)ItemGenerator.EVITEM_NONE_FIGHT.ORE);
                            int box_index = player.GetItemBoxSpecifyIndex(item);
                            player.BoxItemDelete(box_index);

                            //ゴールドを受け取る
                            player.GoldGet(EventController.ORE_PRICE);

                            //武器屋の店主フラグを1にする（ミストリル鉱石譲渡済にする）
                            eventController.ChangeCityEventFlag(EventController.CITY_EV_FLAG.WEAPON_MASTER, 1);

                            //店用のコマンドパネルを表示する
                            shopPanel.SetActive(true);

                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」が選択された時

                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessageDisp("そうかい、それは残念だ。\n気が変わったら、また来てくれよ。\n" + 
                                                            "他に用はあるかい？\n");

                            //店用のコマンドパネルを表示する
                            shopPanel.SetActive(true);
                        }
                    }

                }
                break;
            case CITY_PLACE.ITEM_SHOP:
                //道具屋
                if (buySellFlag == BUY_SELL.BUY)
                {
                    //買いに来た
                    if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                    {
                        //「はい」が選択された時

                        //購入アイテムのデータを取得
                        Item item = itemGenerator.GetItemInfo(shopList[shopListNum]);

                        YesNoController.yesNoController.YesNoPanelClose();

                        //購入の成否情報を取得する
                        Player.BUY_COMPLETION buy_comp = player.ItemBuy(item);

                        //それぞれの状況に応じたメッセージを表示する
                        if (buy_comp == Player.BUY_COMPLETION.OK)
                        {
                            //購入成功の時
                            MessageController.msgController.MessageDisp("お買い上げありがとうございます。\n他にも何かお買いになりますか？\n");
                        }
                        else if (buy_comp == Player.BUY_COMPLETION.NG_MONEY)
                        {
                            //ゴールドが足りなかった時
                            MessageController.msgController.MessageDisp("それをお買いになるにはお金が足りないようです。\n");
                        }
                        else if (buy_comp == Player.BUY_COMPLETION.NG_BOX_MAX)
                        {
                            //アイテム所持数が最大値に達していた時
                            MessageController.msgController.MessageDisp("失礼ですが、これ以上お持ちに\n" +
                                                           "なれないようですね。\n" + 
                                                           "持ち物を整理してから、またお越し下さい。\n");
                        }
                        else if (buy_comp == Player.BUY_COMPLETION.NG_ITEM_MAX)
                        {
                            //購入予定のアイテム（使い捨てアイテム）所持数が最大値に達していた時
                            string str = "失礼ですが、これ以上" + item.itemName + "は\n" +
                                         "お持になれないようですね。\n" + 
                                         "持ち物を整理してから、またお越し下さい。\n";
                            MessageController.msgController.MessageDisp(str);
                        }

                        //ショップウィンドウ上のラベルの初期化
                        ShopLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ShopWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                    else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                    {
                        //「いいえ」が選択された時

                        YesNoController.yesNoController.YesNoPanelClose();
                        MessageController.msgController.MessageDisp("そうですか、\n他には何かお買いになりますか？\n");
                        //ショップウィンドウ上のラベルの初期化
                        ShopLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ShopWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                }
                else if (buySellFlag == BUY_SELL.SELL)
                {
                    //売りに来た
                    if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                    {
                        //「はい」が選択された時

                        //売却アイテムのデータを取得
                        Item item = itemGenerator.GetItemInfo(player.GetItemBoxIndex(itemBoxNum).itemId);

                        YesNoController.yesNoController.YesNoPanelClose();

                        //売却処理を行う
                        player.ItemSell(itemGenerator.GetItemList(), itemBoxNum);
                        MessageController.msgController.MessageDisp("ありがとうございます。\n他にも何かお売りいただけますか？\n");

                        //アイテムウィンドウの内容を更新する
                        ItemWindowDisp();
                        //ショップウィンドウ上のラベルの初期化
                        ItemLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ItemWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                    else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                    {
                        //「いいえ」が選択された時

                        YesNoController.yesNoController.YesNoPanelClose();
                        MessageController.msgController.MessageDisp("そうですか、\n他には何かお売りいただけますか？\n");

                        //ショップウィンドウ上のラベルの初期化
                        ItemLabelInit();
                        //ショップウィンドウのボタンを使用可能にする
                        ItemWindowButtonInit(true);
                        //アイテム名クリックフラグをオフ
                        itemClickFlag = false;
                    }
                }
                break;
            default:
                break;
        }
    }

    //「泊まる」ボタンをクリックしたときの処理を行う関数
    public void StayButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        string str = "";
        //宿代を算出する
        int price = InnPriceCalc();
        //メッセージを表示して、YesNoウィンドウを開く
        str = "一晩" + price.ToString() + "ゴールドになりますが、\nよろしいでしょうか？\n";
        MessageController.msgController.MessageDisp(str);
        YesNoController.yesNoController.YesNoPanelOpen();
        //宿屋用コマンドパネルを非表示にする
        innPanel.SetActive(false);
    }

    //宿代を算出する処理を行う関数
    //戻り値（宿代）
    int InnPriceCalc()
    {
        //宿代の基本値を取得
        int price = INN_PRICE;

        //毒状態の時は宿代が1.5倍になる
        if (player.playerCondition == Player.PLAYER_CONDITION.POISON)
        {
            price += INN_PRICE / 2;
        }

        return price;
    }

    //宿泊処理を実行する関数
    void InnStayProcess()
    {
        //プレイヤーの宿泊処理を実行する
        player.InnStayHeal(InnPriceCalc());

        //宿泊した後のメッセージを表示する
        MessageController.msgController.MessageDisp("おはようございます。\nでは、いってらっしゃいませ。\n");

        //宿屋用コマンドパネルを表示する
        innPanel.SetActive(true);
    }

    //商品一覧データファイルを読み込む処理を行う関数
    void ShopFileRead()
    {
        TextAsset csvWeaponShopFile; //武器屋の商品一覧データのCSVファイル

        //武器屋の商品一覧データのCSVの中身を入れるリストの初期化
        weaponShopDatas = new List<string>();
        //武器屋の商品一覧データのCSVファイルの読込
        csvWeaponShopFile = Resources.Load(GlobalConst.DATA_DIR + "weaponshopFile") as TextAsset;
        StringReader readerWeaponShop = new StringReader(csvWeaponShopFile.text);

        //ファイルの中身をリストに入れる
        while (readerWeaponShop.Peek() != -1)
        {
            string line = readerWeaponShop.ReadLine();
            weaponShopDatas.Add(line);
        }

        //リストのデータを数値に変換して武器屋の商品一覧に入れる
        weaponShopList = new List<int>();

        int count = weaponShopDatas.Count;

        for (int i = 0; i < count; i++)
        {
            weaponShopList.Add(int.Parse(weaponShopDatas[i]));
        }

        TextAsset csvItemShopFile; //道具屋の商品一覧データのCSVファイル

        //道具屋の商品一覧データのCSVの中身を入れるリストの初期化
        itemShopDatas = new List<string>();
        //道具屋の商品一覧データのCSVファイルの読込
        csvItemShopFile = Resources.Load(GlobalConst.DATA_DIR + "itemshopFile") as TextAsset;
        StringReader readerItemShop = new StringReader(csvItemShopFile.text);

        //ファイルの中身をリストに入れる
        while (readerItemShop.Peek() != -1)
        {
            string line = readerItemShop.ReadLine();
            itemShopDatas.Add(line);
        }

        //リストのデータを数値に変換して道具屋の商品一覧に入れる
        itemShopList = new List<int>();

        count = itemShopDatas.Count;

        for (int i = 0; i < count; i++)
        {
            itemShopList.Add(int.Parse(itemShopDatas[i]));
        }
    }

    //「買いに来た」ボタンをクリックしたときの処理を行う関数
    public void BuyButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //店の種類によって表示メッセージを変える
        if (cityPlace == CITY_PLACE.WEAPON_SHOP)
        {
            //武器屋
            MessageController.msgController.MessageDisp("どれを買うんだい？\n");
        }
        else if (cityPlace == CITY_PLACE.ITEM_SHOP)
        {
            //道具屋
            MessageController.msgController.MessageDisp("何をお買いになりますか？\n");
        }

        //店での用事フラグを買いに来たに設定
        buySellFlag = BUY_SELL.BUY;
        //ショップウィンドウを開く
        ShopWindowOpen();
    }

    //「売りに来た」ボタンをクリックしたときの処理を行う関数
    public void SellButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        if (player.GetItemBoxCount() > 0)
        {
            //アイテム1個でも持っている時

            //店での用事フラグを売りに来たに設定
            buySellFlag = BUY_SELL.SELL;

            //店の種類によって表示メッセージを変える
            if (cityPlace == CITY_PLACE.WEAPON_SHOP)
            {
                //武器屋
                MessageController.msgController.MessageDisp("どれを売ってくれるんだい？\n");
            }
            else if (cityPlace == CITY_PLACE.ITEM_SHOP)
            {
                //道具屋
                MessageController.msgController.MessageDisp("どの品物を売っていただけるのでしょうか？\n");
            }
            //アイテムウィンドウを表示する
            itemPage = 1;
            ItemWindowOpen();
        }
        else
        {
            //アイテムを何も持っていない時

            //店の種類によって表示メッセージを変える
            if (cityPlace == CITY_PLACE.WEAPON_SHOP)
            {
                //武器屋
                MessageController.msgController.MessageDisp("何も持ってねえじゃねえか！\n他に用はあるかい？\n");
            }
            else if (cityPlace == CITY_PLACE.ITEM_SHOP)
            {
                //道具屋
                MessageController.msgController.MessageDisp("何もお持ちでないようですが・・・。\n他にご用はありますか？\n");
            }
        }
    }

    //ショップウィンドウを初期化する関数
    void ShopWindowInit()
    {
        //ショップウィンドウ上のオブジェクト取得
        shopExplanation = GameObject.Find("ShopExplainText").GetComponent<Text>();
        shopPageLabel = GameObject.FindGameObjectWithTag("ShopPage");
        shopPreviousButton = GameObject.FindGameObjectWithTag("ShopPrevious");
        shopNextButton = GameObject.FindGameObjectWithTag("ShopNext");
        shopCloseButton = GameObject.FindGameObjectWithTag("ShopClose");

        //ショップウィンドウを非表示にする
        shopWindow.SetActive(false);
    }

    //ショップリストを作成する関数
    //引数
    //place:現在の場所
    void ShopListMake(CITY_PLACE place)
    {
        //店の種類によって表示する商品一覧を変える
        if (cityPlace == CITY_PLACE.WEAPON_SHOP)
        {
            //武器屋の時
            shopList = new List<int>();

            //武器屋の品数を算出する
            int count = weaponShopList.Count;
            if (eventController.GetCityEventFlag(EventController.CITY_EV_FLAG.WEAPON_MASTER) == 0)
            {
                //ミストリル鉱石を武器屋の店主に渡していない時は、その分の品数を引く
                count -= 4;
            }

            //武器屋の商品一覧リストからショップウィンドウリストにデータを移す
            for (int i = 0; i < count; i++)
            {
                shopList.Add(weaponShopList[i]);
            }
        }
        else if (cityPlace == CITY_PLACE.ITEM_SHOP)
        {
            //道具屋の時
            shopList = new List<int>(itemShopList);
        }
    }

    //ショップウィンドウを開く関数
    void ShopWindowOpen()
    {
        //ショップウィンドウページを最初に設定する
        shopPage = 1;
        //選択されたショップアイテムリスト番号の初期化
        shopListNum = 0;
        //選択されたショップアイテム名番号を保存しておく変数の初期化（最初の番号が0のため初期値を-1に設定している）
        saveShopNum = -1;
        //店用のコマンドパネルを非表示にする
        shopPanel.SetActive(false);
        //ショップウィンドウを表示する
        shopWindow.SetActive(true);
        //ショップウィンドウの最大ページ数を算出する
        ShopPageMaxCalc();
        //ウインドウの内容を表示する
        ShopWindowDisp();

    }

    //ショップウィンドウを閉じる関数
    void ShopWindowClose()
    {
        //店での用事フラグをなしに設定
        buySellFlag = BUY_SELL.NONE;
        //店用のコマンドパネルを表示する
        shopPanel.SetActive(true);
        //ショップウィンドウを非表示にする
        shopWindow.SetActive(false);
    }

    //ショップウィンドウの最大ページ数を算出する関数
    void ShopPageMaxCalc()
    {
        //商品一覧の商品数を1ページ毎の商品数で割る
        shopPageMax = shopList.Count / SHOP_PER_PAGE;
        if (shopList.Count % SHOP_PER_PAGE > 0)
        {
            //上記の計算で余りが出た時はページ数を1加算する
            shopPageMax++;
        }
    }

    //ショップウィンドウのラベルおよびボタンを初期化する関数
    void ShopWindowClear()
    {
        //改ページボタンの初期化
        shopPreviousButton.GetComponent<Button>().interactable = true;
        shopNextButton.GetComponent<Button>().interactable = true;

        //アイテム情報の文字色取得（初期値）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        //アイテムウィンドウページ数ラベルの初期化
        Text page = shopPageLabel.GetComponent<Text>();
        page.text = "";

        //アイテム名、数量のラベルの初期化
        for (int i = 0; i < shopNameLabels.Length; i++)
        {
            Text name = shopNameLabels[i].GetComponent<Text>();
            Text price = shopPriceLabels[i].GetComponent<Text>();

            //テキストの初期化
            name.text = "";
            price.text = "";

            //色の初期化
            name.color = labelColor;
            price.color = labelColor;

            shopNameLabels[i].SetActive(false);
        }

        //アイテム情報の初期化
        Text explain = shopExplanation.GetComponent<Text>();
        explain.text = "";

        //アイテム画像の初期化
        ShopImageDisp("NoItem");
    }

    //ショップウィンドウにアイテム画像を表示する関数
    //引数
    //img:画像ファイル名
    void ShopImageDisp(string img)
    {
        Image shopImage;
        Sprite shopSprite;

        //指定した画像ファイルをロードする
        shopSprite = Resources.Load<Sprite>(GlobalConst.IMG_DIR + img) as Sprite;

        //取得したイメージオブジェクトにアイテム画像を表示する
        GameObject ob = GameObject.Find("ShopImage");
        shopImage = ob.GetComponent<Image>();
        shopImage.sprite = shopSprite;
    }

    //ショップウィンドウの内容を表示する関数
    void ShopWindowDisp()
    {
        //ウィンドウの表示内容の初期化
        ShopWindowClear();

        //現在ページおよび最大ページを表示する
        int current = shopPage;
        int page_last = shopPageMax;
        string str = current.ToString() + " / " + page_last.ToString();
        Text page = shopPageLabel.GetComponent<Text>();
        page.text = str;

        //現在のページに表示する販売アイテムの開始番号の設定
        int shop_start = (shopPage - 1) * SHOP_PER_PAGE;

        //現在のページに表示する販売アイテムラベル配列の終端番号の設定
        int label_end;

        if (current == page_last)
        {
            //現在ページが最終ページの時
            if (page_last == 1)
            {
                //最大ページが1のときは終端番号に販売アイテム数を設定
                label_end = shopList.Count;
            }
            else
            {
                //最大ページが2以上のとき
                if (shopList.Count % SHOP_PER_PAGE == 0)
                {
                    //販売アイテム数が1ページ当たりの表示アイテム数で割り切れるとき
                    //終端番号に販売アイテムラベル配列の長さを設定
                    label_end = shopNameLabels.Length;
                }
                else
                {
                    //プレイヤーの販売アイテム数が1ページ当たりの表示アイテム数で割り切れないとき
                    //終端番号に上記の余りを設定
                    label_end = shopList.Count % SHOP_PER_PAGE;
                }
            }
        }
        else
        {
            //現在ページが最終ページでないの時は終端番号に販売アイテムラベル配列の長さを設定
            label_end = shopNameLabels.Length;
        }

        if (current == 1)
        {
            //最初のページの時前ページ移動ボタンを使用不可にする
            shopPreviousButton.GetComponent<Button>().interactable = false;
        }

        if (current == page_last)
        {
            //最後のページの時次ページ移動ボタンを使用不可にする
            shopNextButton.GetComponent<Button>().interactable = false;
        }

        //ウィンドウに販売アイテム一覧を表示する
        for (int i = 0; i < label_end; i++)
        {
            foreach (Item item in itemList)
            {
                if (shopList[i + shop_start] == item.itemId && shopList[i + shop_start] != 0)
                {
                    //販売アイテムのIDと全アイテムリストのIDを照合してアイテム情報を取得
                    shopNameLabels[i].SetActive(true);
                    Text name = shopNameLabels[i].GetComponent<Text>();
                    Text price = shopPriceLabels[i].GetComponent<Text>();

                    //アイテム名を表示
                    name.text = item.itemName;
                    //アイテム価格の表示
                    price.text = item.buyPrice.ToString();
                }
            }
        }
    }

    //ショップウィンドウの「前ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void ShopPreviousButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //ページ数を1減らす
        shopPage--;
        //最初のページの時はそのままにする
        if (shopPage < 1)
        {
            shopPage = 1;
        }
        //ウィンドウの内容を更新する
        ShopWindowDisp();
    }

    //ショップウィンドウの「次ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void ShopNextButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        //ページ数を1増やす
        shopPage++;
        //最後のページの時はそのままにする
        if (shopPage > shopPageMax)
        {
            shopPage = shopPageMax;
        }
        //ウィンドウの内容を更新する
        ShopWindowDisp();
    }

    //ショップウィンドウの「やめる」ボタンがクリックされた時の処理を行う関数
    public void ShopCloseButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //店の種類によって表示するメッセージを変える
        if (cityPlace == CITY_PLACE.WEAPON_SHOP)
        {
            //武器屋の時
            MessageController.msgController.MessageDisp("他に用はあるかい？\n");
        }
        else if (cityPlace == CITY_PLACE.ITEM_SHOP)
        {
            //道具屋の時
            MessageController.msgController.MessageDisp("他にご用はありますか？\n");
        }
        //ショップウィンドウを閉じる
        ShopWindowClose();
    }

    //ショップウィンドウにてアイテム名ラベルにマウスポインタが乗った時の処理を行う関数
    //引数
    //num:対象のアイテム名ラベルの番号
    public void ShopLabelOnMouseEnter(int num)
    {
        if (itemClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいた販売アイテムリスト番号を取得
        int click_item = num + ((shopPage - 1) * SHOP_PER_PAGE);

        if (shopList[click_item] > 0)
        {
            //販売アイテムリスト番号で指定された要素内にアイテムが存在するとき

            //アイテム情報の文字色取得（マウスポインタが乗った時）
            Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

            Text nameText = shopNameLabels[num].GetComponent<Text>();
            Text priceText = shopPriceLabels[num].GetComponent<Text>();

            //アイテム名、価格の色変更
            nameText.color = labelColor;
            priceText.color = labelColor;

            //指定されたアイテムデータの取得
            Item item = itemGenerator.GetItemInfo(shopList[click_item]);

            //アイテム情報の表示
            Text explain = shopExplanation.GetComponent<Text>();
            explain.text = item.itemExplanation;

            //アイテム画像の表示
            ItemImageDisp(item.itemImg);
        }
    }

    //ショップウィンドウにてアイテム名ラベルからマウスポインタが離れた時の処理を行う関数
    //引数
    //num:対象のアイテム名ラベルの番号
    public void ShopLabelOnMouseExit(int num)
    {
        if (itemClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいた販売アイテムリスト番号を取得
        int click_item = num + ((shopPage - 1) * SHOP_PER_PAGE);

        if (shopList[click_item] > 0)
        {
            //販売アイテムリスト番号で指定された要素内にアイテムが存在するとき

            //アイテム情報の文字色取得（初期値）
            Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            Text nameText = shopNameLabels[num].GetComponent<Text>();
            Text priceText = shopPriceLabels[num].GetComponent<Text>();

            //アイテム名、価格の色変更
            nameText.color = labelColor;
            priceText.color = labelColor;

            //アイテム情報の初期化
            Text explain = shopExplanation.GetComponent<Text>();
            explain.text = "";

            //アイテム画像の消去
            ItemImageDisp("NoItem");
        }
    }

    //ショップウィンドウにてアイテム名ラベルをクリックした時の処理を行う関数
    //引数
    //num:対象のアイテム名ラベルの番号
    public void ShopLabelOnMouseClick(int num)
    {
        //アイテム名ラベルの番号の取得
        saveShopNum = num;
        //対象ラベルに基づいたショップアイテムリスト番号を取得
        int click_item = num + ((shopPage - 1) * SHOP_PER_PAGE);

        string str = "";

        if (shopList[click_item] > 0)
        {
            //ショップアイテムリスト番号で指定された要素内にアイテムが存在するとき
            if (itemClickFlag == false)
            {
                //すでにクリックされていないとき

                //ボタンをクリックしたときの効果音を鳴らす
                SoundManager.soundManager.PlaySE("se_decision");

                //アイテム名クリックフラグをオン
                itemClickFlag = true;

                //指定されたアイテムデータの取得
                Item item = itemGenerator.GetItemInfo(shopList[click_item]);

                //アイテム情報の文字色取得（クリック時）
                Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

                Text nameText = shopNameLabels[num].GetComponent<Text>();
                Text priceText = shopPriceLabels[num].GetComponent<Text>();

                //アイテム名、価格の色変更
                nameText.color = labelColor;
                priceText.color = labelColor;

                //ショップウィンドウのボタンを使用不可にする
                ShopWindowButtonInit(false);

                //取得したショップアイテムリスト番号を記憶させておく
                shopListNum = click_item;

                //メッセージを表示する
                if (cityPlace == CITY_PLACE.WEAPON_SHOP)
                {
                    //武器屋の時
                    str = item.itemName + "は" + item.buyPrice.ToString() + "ゴールドになるけど、\nそれでいいかい？\n";
                }
                else if (cityPlace == CITY_PLACE.ITEM_SHOP)
                {
                    //道具屋の時
                    str = item.itemName + "は" + item.buyPrice.ToString() + "ゴールドになりますが、\nよろしいですか？\n";
                }
                
                MessageController.msgController.MessageDisp(str);

                //YesNoウィンドウを開く
                YesNoController.yesNoController.YesNoPanelOpen();
            }
        }

    }

    //ショップウィンドウのボタンの初期化を行う関数
    //引数
    //flag:trueの時はボタンを使用可能にし、falseの時はボタンを使用不可にする
    void ShopWindowButtonInit(bool flag)
    {
        //前ページ移動ボタンの設定
        if (shopPage == 1)
        {
            //最初のページの時は使用不可にする
            shopPreviousButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最初のページでないとき時は引数に応じて、使用できるかどうかを設定する
            shopPreviousButton.GetComponent<Button>().interactable = flag;
        }

        //次ページ移動ボタンの設定
        if (shopPage == shopPageMax)
        {
            //最後のページの時は使用不可にする
            shopNextButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最後のページでないとき時は引数に応じて、使用できるかどうかを設定する
            shopNextButton.GetComponent<Button>().interactable = flag;
        }
        //ウィンドウクローズボタンは引数に応じて、使用できるかどうかを設定する
        shopCloseButton.GetComponent<Button>().interactable = flag;
    }

    //ショップウィンドウ上のラベルの初期化を行う関数
    void ShopLabelInit()
    {
        //アイテム情報の文字色取得（初期値）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text nameText = shopNameLabels[saveShopNum].GetComponent<Text>();
        Text priceText = shopPriceLabels[saveShopNum].GetComponent<Text>();

        //アイテム名、価格の色変更
        nameText.color = labelColor;
        priceText.color = labelColor;

        //アイテム情報の初期化
        Text explain = shopExplanation.GetComponent<Text>();
        explain.text = "";

        //アイテム画像の初期化
        ItemImageDisp("NoItem");

        //選択されたアイテム名番号を保存しておく変数の初期化（最初の番号が0のため初期値を-1に設定している）
        saveShopNum = -1;
    }

    //セーブウィンドウを初期化する関数
    void SaveWindowClear()
    {
        //改ページボタンの初期化
        savePreviousButton.GetComponent<Button>().interactable = true;
        saveNextButton.GetComponent<Button>().interactable = true;

        //セーブデータ情報の文字色取得（初期値）
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
        //街の入口専用コマンドパネルを非表示にする
        entrancePanel.SetActive(!saveFlag);
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
        //街の入口専用コマンドパネルを表示する
        entrancePanel.SetActive(!saveFlag);

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
        //コマンドパネルのボタンを使用可能にする
        commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.NORMAL);
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
        //コマンドパネルのボタンを使用不可にする
        commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.YESNO);
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
            //コマンドパネルのボタンを使用不可にする
            commandPanel.GetComponentInChildren<CityButtonController>().CommandPanelOpen(CityButtonController.CMDTYPE.YESNO);

            //取得したセーブデータリスト番号を記憶させておく
            saveArrayNum = click_save;

        }
    }

    //街のBGMのファイル名を取得する関数
    //引数
    //place:現在の場所
    //戻り値（指定した場所のBGMファイル名）
    string GetCityBGMFileName(CITY_PLACE place)
    {
        int num;

        //列挙型を整数に変換する
        if (place >= CITY_PLACE.SAVE)
        {
            //指定した場所が保存の時は街の入り口の値を変換する
            num = (int)CITY_PLACE.ENTRANCE;
        }
        else
        {
            //指定した場所が保存以外の時は指定した場所の値を変換する
            num = (int)place;
        }

        return cityBGMArray[num];
    }

    //街背景のフェードインを行う関数
    //引数
    //fadetime:フェードインにかける時間（秒）
    //place:指定した街における場所
    //戻り値（true:フェードイン終了、false:フェードイン途中）
    bool BackImageFadeIn(float fadetime, CITY_PLACE place)
    {
        if (currentTime >= fadetime)
        {
            //フェードインが終了したとき、タイマーを初期化する
            currentTime = 0.0f;
            return true;
        }
        else if (currentTime <= 0.0f)
        {
            //フェードイン開始時は背景画像を黒くして表示する
            citySprite = Resources.Load<Sprite>(GetBackImageFilePath(place)) as Sprite;
            GameObject ob = GameObject.Find("CityImage");
            cityImage = ob.GetComponent<Image>();
            cityImage.color = new Color(0, 0, 0, 1.0f);
            cityImage.sprite = citySprite;
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = currentTime / fadetime;
        cityImage.color = new Color(c_color, c_color, c_color, 1.0f);

        return false;

    }

    //街背景のフェードアウトを行う関数
    //引数
    //fadetime:フェードアウトにかける時間（秒）
    //戻り値（true:フェードアウト終了、false:フェードアウト途中）
    bool BackImageFadeOut(float fadetime)
    {
        if (currentTime >= fadetime)
        {
            //フェードアウトが終了したとき、タイマーを初期化する
            currentTime = 0.0f;
            return true;
        }
        else if (currentTime <= 0.0f)
        {
            //フェードアウト開始時は背景イメージオブジェクトを取得する
            GameObject ob = GameObject.Find("CityImage");
            cityImage = ob.GetComponent<Image>();
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = 1.0f - (currentTime / fadetime);
        cityImage.color = new Color(c_color, c_color, c_color, 1.0f);

        return false;
    }

    //指定した場所の背景画像のファイルパスを取得する関数
    //引数
    //place:指定した場所
    //戻り値（true:フェードアウト終了、false:フェードアウト途中）
    string GetBackImageFilePath(CITY_PLACE place)
    {
        string img = "";

        //引数をチェックして、その場所の背景画像ファイル名を取得する
        switch (place)
        {
            case CITY_PLACE.ENTRANCE:   //街の入口
                img = "entranceback";
                break;
            case CITY_PLACE.WEAPON_SHOP://武器屋
                img = "weaponback";
                break;
            case CITY_PLACE.ITEM_SHOP:  //道具屋
                img = "itemback";
                break;
            case CITY_PLACE.INN:        //宿屋
                img = "innback";
                break;
            case CITY_PLACE.CASTLE:     //お城
                img = "castleback";
                break;
            case CITY_PLACE.BAR:        //酒場
                img = "barback";
                break;
            case CITY_PLACE.DUNGEON:    //迷宮入口
                img = "dungeonback";
                break;
            default:
                //該当する場所がない時は街の入口のファイル名を取得する
                img = "entranceback";
                break;
        }

        //対象の背景画像のファイルパスを作成する
        img = GlobalConst.IMG_DIR + img;

        return img;
    }
}
