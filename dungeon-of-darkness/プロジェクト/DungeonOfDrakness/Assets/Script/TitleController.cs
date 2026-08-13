using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Common;
using PlayerClass;
using GameDataClass;

//タイトル画面の制御を行うクラス
public class TitleController : MonoBehaviour
{
    //タイトル画面の状態を示す列挙型
    public enum TITLE_MODE
    {
        START_FADE_IN,          //ゲーム起動時のフェードイン
        NEW_CLICK_FADE_OUT,     //NEW GAMEクリック時のフェードアウト
        LOAD_CLICK_FADE_OUT,    //LOAD GAMEクリック時のフェードアウト
        EXIT_CLICK,             //EXITをクリックしたときの処理
        NEW_RETURN_FADE_IN,     //NEW GAMEからタイトル画面へ戻るフェードイン
        LOAD_RETURN_FADE_IN,    //LOAD GAMEからタイトル画面へ戻るフェードイン
        EXIT_RETURN,            //EXITからタイトル画面へ戻る処理
        TITLE,                  //タイトル画面表示中
        NEW_GAME,               //ニューゲーム選択中
        LOAD_GAME,              //ロードゲーム選択中
        EXIT,                   //ゲーム終了選択中
        NEW_SCENE_FADE_OUT,     //NEW GAMEでのシーン移動のフェードアウト
        LOAD_SCENE_FADE_OUT,    //LOAD GAMEでのシーン移動のフェードアウト
        END_FADE_OUT            //ゲーム終了のフェードアウト
    }

    private float currentTime = 0.0f;       //タイマーの現在時間（フェードイン、フェードアウトに使用）
    private int switchingCount = 0;         //タイトル画面の切り替え演出の進行状況を示すカウンタ         

    //タイトル画面
    public GameObject titlePanel;           //タイトル画面オブジェクト
    private GameObject titleNewGameLabel;   //ニューゲームラベル
    private GameObject titleLoadGameLabel;  //ロードゲームラベル
    private GameObject titleExitLabel;      //ゲーム終了ラベル
    private GameObject titleLogoImage;      //タイトルロゴオブジェクト
    private TITLE_MODE titleMode;           //タイトル画面の状態を示すフラグ
    private Sprite backSprite;              //タイトル背景スプライト
    private Image backImage;                //タイトル背景イメージ
    private Sprite logoSprite;              //タイトルロゴスプライト
    private Image logoImage;                //タイトルロゴイメージ

    //名前入力ウィンドウ
    private GameObject nameEntryPanel;          //名前入力ウィンドウオブジェクト
    private GameObject nameEntryText;           //名前入力テキストオブジェクト
    private GameObject nameEntryOkButton;       //決定ボタン
    private GameObject nameEntryClearButton;    //クリアボタン
    private GameObject nameEntryGoTitleButton;  //タイトルへ戻るボタン
    private GameObject nameEntryMessage;        //メッセージ表示ラベル

    //ロードウィンドウ
    public GameObject loadWindow;           //ロードウィンドウオブジェクト               
    [SerializeField] private GameObject[] loadNumberLabels;  //セーブデータ番号
    [SerializeField] private GameObject[] loadNameLabels;    //プレイヤー名
    [SerializeField] private GameObject[] loadLevelLabels;   //プレイヤーレベル
    [SerializeField] private GameObject[] loadFloorLabels;   //現在フロア
    [SerializeField] private GameObject[] loadTimeLabels;    //セーブ日時
    private GameObject loadPageLabel;       //ロードウィンドウページ
    private GameObject loadPreviousButton;  //前ページ移動ボタン
    private GameObject loadNextButton;      //次ページ移動ボタン
    private GameObject loadGoTitleButton;   //タイトルへ戻るボタン
    public int loadPage;                    //ロードウィンドウの現在ページ
    public int loadPageMax;                 //ロードウィンドウの最大ページ数
    private bool loadFlag;                  //ロードウィンドウフラグ（true:開いている、false:閉じている）
    private bool loadClickFlag;             //セーブデータクリックフラグ（true:クリックされた、false:クリックされていない）
    private int loadArrayNum;               //選択されたセーブデータリスト番号
    private int loadSaveNum;                //選択されたセーブデータ番号を保存しておく変数

    // Start is called before the first frame update
    void Awake()
    {
        //タイトル画面上のオブジェクトの取得
        titleNewGameLabel = GameObject.FindGameObjectWithTag("NewGame");
        titleLoadGameLabel = GameObject.FindGameObjectWithTag("LoadGame");
        titleExitLabel = GameObject.FindGameObjectWithTag("Exit");
        titleLogoImage = GameObject.FindGameObjectWithTag("TitleLogo");

        //名前入力ウィンドウ上のオブジェクトの取得
        nameEntryPanel = GameObject.FindGameObjectWithTag("NameEntryPanel");
        nameEntryText = GameObject.FindGameObjectWithTag("NameEntryText");
        nameEntryMessage = GameObject.FindGameObjectWithTag("NameEntryMessage");
        nameEntryOkButton = GameObject.FindGameObjectWithTag("NameEntryOk");
        nameEntryClearButton = GameObject.FindGameObjectWithTag("NameEntryClear");
        nameEntryGoTitleButton = GameObject.FindGameObjectWithTag("NameEntryGoTitle");
        //名前入力テキストの初期化
        NameEntryClear();
        //名前入力ウィンドウを非表示にする
        nameEntryPanel.SetActive(false);

        //ロードウィンドウの初期化
        loadPage = 1;
        loadPageMax = GameDataController.SavePageMaxCalc();
        LoadWindowInit();
        loadFlag = false;
        loadClickFlag = false;
        //メッセージウィンドウ（ロード時に使用）を非表示にする
        MessageController.msgController.MessagePanelClose();
        //ロードフラグの初期化
        GameDataController.loadFlag = false;
        //タイトル画面の全ラベルの初期化
        titleNewGameLabel.SetActive(false);
        titleLoadGameLabel.SetActive(false);
        titleExitLabel.SetActive(false);
        //タイトル画面の状態をフェードインにする
        titleMode = TITLE_MODE.START_FADE_IN;

    }

    // Update is called once per frame
    void Update()
    {
        if (titleMode == TITLE_MODE.START_FADE_IN)
        {
            //ゲーム起動時のフェードイン（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //タイトル背景フェードイン
                    if (BackImageFadeIn(2.0f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 1:
                    //タイトル背景フェードイン後の待機
                    if (CommonMethod.TimeWait(1.0f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 2:
                    //タイトルロゴのフェードイン
                    if (LogoImageFadeIn(0.5f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 3:
                    //タイトル画面の全ラベルの表示
                    titleNewGameLabel.SetActive(true);
                    titleLoadGameLabel.SetActive(true);
                    titleExitLabel.SetActive(true);

                    switchingCount++;
                    break;
                case 4:
                    //タイトル画面のBGMを再生する
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        SoundManager.soundManager.PlayBGM("bgm_title", 0.5f, true);

                        switchingCount++;
                    }
                    break;
                default:
                    //タイトル画面の状態をタイトル画面表示中にする
                    titleMode = TITLE_MODE.TITLE;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.NEW_CLICK_FADE_OUT)
        {
            ///NEW GAMEクリック時のフェードアウト（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //BGMの停止
                    SoundManager.soundManager.StopBGM(0.5f);
                    //ボタンをクリックしたときの効果音を鳴らす
                    SoundManager.soundManager.PlaySE("se_decision");
                    //タイトル画面上の全ラベルの文字色を初期化する
                    TitleLabelColorInit();

                    switchingCount++;
                    break;
                case 1:
                    //タイトル画面の全ラベルの非表示
                    titleNewGameLabel.SetActive(false);
                    titleLoadGameLabel.SetActive(false);
                    titleExitLabel.SetActive(false);

                    switchingCount++;
                    break;
                case 2:
                    //タイトルロゴのフェードアウト
                    if (LogoImageFadeOut(0.5f) == true)
                    {
                        //タイトルロゴを非表示にする
                        titleLogoImage.SetActive(false);

                        switchingCount++;
                    }
                    break;
                case 3:
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        //名前入力ウィンドウを表示する
                        nameEntryPanel.SetActive(true);
                        //名前入力テキストを初期化する
                        NameEntryMessageDisp("");

                        switchingCount++;
                    }
                    break;
                default:
                    //タイトル画面の状態をニューゲーム選択中にする
                    titleMode = TITLE_MODE.NEW_GAME;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.LOAD_CLICK_FADE_OUT)
        {
            ///LOAD GAMEクリック時のフェードアウト（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //BGMの停止
                    SoundManager.soundManager.StopBGM(0.5f);
                    //ボタンをクリックしたときの効果音を鳴らす
                    SoundManager.soundManager.PlaySE("se_decision");
                    //タイトル画面上の全ラベルの文字色を初期化する
                    TitleLabelColorInit();

                    switchingCount++;
                    break;
                case 1:
                    //タイトル画面の全ラベルの非表示
                    titleNewGameLabel.SetActive(false);
                    titleLoadGameLabel.SetActive(false);
                    titleExitLabel.SetActive(false);

                    switchingCount++;
                    break;
                case 2:
                    //タイトルロゴのフェードアウト
                    if (LogoImageFadeOut(0.5f) == true)
                    {
                        //タイトルロゴを非表示にする
                        titleLogoImage.SetActive(false);

                        switchingCount++;
                    }
                    break;
                case 3:
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        //ロードウィンドウを開く
                        LoadWindowOpen();

                        //メッセージを表示する
                        MessageController.msgController.MessageDisp("どのデータをロードしますか？\n");

                        switchingCount++;
                    }
                    break;
                default:
                    //タイトル画面の状態をニューゲーム選択中にする
                    titleMode = TITLE_MODE.LOAD_GAME;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.EXIT_CLICK)
        {
            //EXITをクリックしたときの処理（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //ボタンをクリックしたときの効果音を鳴らす
                    SoundManager.soundManager.PlaySE("se_decision");
                    //BGMの停止
                    SoundManager.soundManager.StopBGM(0.5f);

                    switchingCount++;
                    break;
                case 1:
                    //YesNoウィンドウを開く
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        YesNoController.yesNoController.YesNoPanelOpen("終了しますか？");

                        switchingCount++;
                    }
                    break;
                default:
                    //タイトル画面の状態をゲーム終了選択中にする
                    titleMode = TITLE_MODE.EXIT;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.NEW_RETURN_FADE_IN)
        {
            //NEW GAMEからタイトル画面へ戻るフェードイン（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //ボタンをクリックしたときの効果音を鳴らす
                    SoundManager.soundManager.PlaySE("se_cancel");

                    //名前入力テキストを初期化する
                    NameEntryClear();
                    //名前入力ウィンドウを非表示にする
                    nameEntryPanel.SetActive(false);
                    //タイトルロゴを表示させる
                    titleLogoImage.SetActive(true);

                    switchingCount++;
                    break;
                case 1:
                    //タイトルロゴのフェードイン
                    if (LogoImageFadeIn(0.5f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 2:
                    //タイトル画面の全ラベルを表示する
                    titleNewGameLabel.SetActive(true);
                    titleLoadGameLabel.SetActive(true);
                    titleExitLabel.SetActive(true);

                    switchingCount++;
                    break;
                case 3:
                    //タイトル画面のBGMを再生する
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        SoundManager.soundManager.PlayBGM("bgm_title", 0.5f, true);

                        switchingCount++;
                    }
                    break;
                default:
                    //タイトル画面の状態をタイトル画面表示中にする
                    titleMode = TITLE_MODE.TITLE;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.LOAD_RETURN_FADE_IN)
        {
            //LOAD GAMEからタイトル画面へ戻るフェードイン（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //ボタンをクリックしたときの効果音を鳴らす
                    SoundManager.soundManager.PlaySE("se_cancel");

                    //ロードウィンドウを閉じる
                    LoadWindowClose();
                    //メッセージウィンドウを閉じる
                    MessageController.msgController.MessagePanelClose();
                    //タイトルロゴを表示させる
                    titleLogoImage.SetActive(true);

                    switchingCount++;
                    break;
                case 1:
                    //タイトルロゴのフェードイン
                    if (LogoImageFadeIn(0.5f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                case 2:
                    //タイトル画面の全ラベルを表示する
                    titleNewGameLabel.SetActive(true);
                    titleLoadGameLabel.SetActive(true);
                    titleExitLabel.SetActive(true);

                    switchingCount++;
                    break;
                case 3:
                    //タイトル画面のBGMを再生する
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        SoundManager.soundManager.PlayBGM("bgm_title", 0.5f, true);

                        switchingCount++;
                    }
                    break;
                default:
                    //タイトル画面の状態をタイトル画面表示中にする
                    titleMode = TITLE_MODE.TITLE;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.EXIT_RETURN)
        {
            //EXITからタイトル画面へ戻る処理（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //ボタンをクリックしたときの効果音を鳴らす
                    SoundManager.soundManager.PlaySE("se_cancel");

                    //YesNoウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();

                    //タイトル画面上の全ラベルの文字色を初期化する
                    TitleLabelColorInit();

                    switchingCount++;
                    break;
                case 1:
                    //タイトル画面のBGMを再生する
                    if (CommonMethod.TimeWait(0.5f) == true)
                    {
                        SoundManager.soundManager.PlayBGM("bgm_title", 0.5f, true);

                        switchingCount++;
                    }
                    break;
                default:
                    //タイトル画面の状態をタイトル画面表示中にする
                    titleMode = TITLE_MODE.TITLE;
                    //カウンタの初期化
                    switchingCount = 0;
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.NEW_SCENE_FADE_OUT)
        {
            //NEW GAMEでのシーン移動のフェードアウト（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //名前入力テキストに入力された文字列を取得する
                    string name_text = nameEntryText.GetComponent<InputField>().text;
                    //入力した名前を使用してプレイヤーデータを作成する
                    Player player = new Player(name_text);
                    //シーン移行時のプレイヤーデータの受け渡しの準備を行う
                    GameDataController.SetPlayerData(player);
                    GameDataController.newGameFlag = true;
                    //YesNoウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();

                    switchingCount++;
                    break;
                case 1:
                    //名前入力テキストを初期化する
                    NameEntryClear();
                    //名前入力ウィンドウを非表示にする
                    nameEntryPanel.SetActive(false);

                    switchingCount++;
                    break;
                case 2:
                    //タイトル背景フェードアウト
                    if (BackImageFadeOut(2.0f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    switchingCount = 0;

                    //街へ移動する
                    SceneManager.LoadScene("CityScene");
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.LOAD_SCENE_FADE_OUT)
        {
            //LOAD GAMEでのシーン移動のフェードアウト（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //YesNoウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();

                    //ロードウィンドウを閉じる
                    LoadWindowClose();
                    //メッセージウィンドウを閉じる
                    MessageController.msgController.MessagePanelClose();

                    switchingCount++;
                    break;
                case 1:
                    //タイトル背景フェードアウト
                    if (BackImageFadeOut(2.0f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    switchingCount = 0;
                    //指定したファイルのデータをロードする
                    GameDataController.DataLoad(loadArrayNum);
                    break;
            }
        }
        else if (titleMode == TITLE_MODE.END_FADE_OUT)
        {
            //ゲーム終了のフェードアウト（演出が1つ終わるごとにカウンタを1加算していく）
            switch (switchingCount)
            {
                case 0:
                    //YesNoウィンドウを閉じる
                    YesNoController.yesNoController.YesNoPanelClose();

                    switchingCount++;
                    break;
                case 1:
                    //タイトル画面の全ラベルの非表示
                    titleNewGameLabel.SetActive(false);
                    titleLoadGameLabel.SetActive(false);
                    titleExitLabel.SetActive(false);

                    switchingCount++;
                    break;
                case 2:
                    //タイトルロゴのフェードアウト
                    if (LogoImageFadeOut(0.5f) == true)
                    {
                        //タイトルロゴを非表示にする
                        titleLogoImage.SetActive(false);

                        switchingCount++;
                    }
                    break;
                case 3:
                    //タイトル背景フェードアウト
                    if (BackImageFadeOut(2.0f) == true)
                    {
                        switchingCount++;
                    }
                    break;
                default:
                    //カウンタの初期化
                    switchingCount = 0;
                    //ゲーム終了
                    GameEnd();
                    break;
            }
        }
        else
        {
            //タイトル画面演出完了後
            if (YesNoController.yesNoController.GetPanelOpenFlag() == true)
            {
                switch (titleMode)
                {
                    case TITLE_MODE.NEW_GAME:   //ニューゲーム選択中
                        //YesNoウィンドウが開いているときの処理
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //タイトル画面の状態をNEW GAMEでのシーン移動のフェードアウトに移行する
                            titleMode = TITLE_MODE.NEW_SCENE_FADE_OUT;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //メッセージ表示ラベルの初期化
                            NameEntryMessageDisp();

                            //YesNoウィンドウを閉じる
                            YesNoController.yesNoController.YesNoPanelClose();
                            //名前入力ウィンドウの全ボタンおよび入力テキストを使用可能にする
                            nameEntryOkButton.GetComponent<Button>().interactable = true;
                            nameEntryClearButton.GetComponent<Button>().interactable = true;
                            nameEntryGoTitleButton.GetComponent<Button>().interactable = true;
                            nameEntryText.GetComponent<InputField>().interactable = true;
                        }

                        break;
                    case TITLE_MODE.LOAD_GAME:     //ロードゲーム選択中
                        //YesNoウィンドウが開いているときの処理
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //タイトル画面の状態をLOAD GAMEでのシーン移動のフェードアウトに移行する
                            titleMode = TITLE_MODE.LOAD_SCENE_FADE_OUT;
                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //セーブデータクリックフラグをオフにする
                            loadClickFlag = false;
                            //ロードウィンドウ上のボタンを使用可能にする
                            LoadWindowButtonInit(true);

                            YesNoController.yesNoController.YesNoPanelClose();
                            MessageController.msgController.MessageDisp("どのデータをロードしますか？\n");
                            //ロードウィンドウ上のラベルの初期化
                            LoadLabelInit();
                        }
                        break;
                    case TITLE_MODE.EXIT:           //ゲーム終了選択中
                        if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.YES)
                        {
                            //「はい」を選択したとき

                            //タイトル画面の状態をEXITからタイトル画面へ戻る処理に移行する
                            titleMode = TITLE_MODE.END_FADE_OUT;

                        }
                        else if (YesNoController.yesNoController.GetYesNoReply() == YesNoController.REPLY.NO)
                        {
                            //「いいえ」を選択したとき

                            //タイトル画面の状態をEXITからタイトル画面へ戻る処理に移行する
                            titleMode = TITLE_MODE.EXIT_RETURN;
                        }
                        break;
                    default:
                        //タイトル画面表示中は何もしない
                        break;
                }
            }
        }

    }

    //ゲーム終了処理を行う関数
    void GameEnd()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    //タイトル背景のフェードインを行う関数
    //引数
    //fadetime:フェードインにかける時間（秒）
    //戻り値（true:フェードイン終了、false:フェードイン途中）
    bool BackImageFadeIn(float fadetime)
    {
        if (currentTime >= fadetime)
        {
            //フェードインが終了したとき、タイマーを初期化する
            currentTime = 0.0f;
            return true;
        }
        else if (currentTime <= 0.0f)
        {
            //フェードイン開始時は背景イメージオブジェクトを取得する
            GameObject ob = GameObject.Find("TitleBackImage");
            backImage = ob.GetComponent<Image>();
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = currentTime / fadetime;
        backImage.color = new Color(c_color, c_color, c_color, 1.0f);

        return false;

    }

    //タイトルロゴのフェードインを行う関数
    //引数
    //fadetime:フェードインにかける時間（秒）
    //戻り値（true:フェードイン終了、false:フェードイン途中）
    bool LogoImageFadeIn(float fadetime)
    {
        if (currentTime >= fadetime)
        {
            //フェードインが終了したとき、タイマーを初期化する
            currentTime = 0.0f;
            return true;
        }
        else if (currentTime <= 0.0f)
        {
            //フェードイン開始時はロゴイメージオブジェクトを取得する
            GameObject ob = GameObject.Find("TitleLogoImage");
            logoImage = ob.GetComponent<Image>();
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在のロゴイメージオブジェクトの色を設定する
        float c_color = currentTime / fadetime;
        logoImage.color = new Color(1.0f, 1.0f, 1.0f, c_color);

        return false;

    }

    //タイトル背景のフェードアウトを行う関数
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
            GameObject ob = GameObject.Find("TitleBackImage");
            backImage = ob.GetComponent<Image>();
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = 1.0f - (currentTime / fadetime);
        backImage.color = new Color(c_color, c_color, c_color, 1.0f);

        return false;
    }

    //タイトルロゴのフェードアウトを行う関数
    //引数
    //fadetime:フェードアウトにかける時間（秒）
    //戻り値（true:フェードアウト終了、false:フェードアウト途中）
    bool LogoImageFadeOut(float fadetime)
    {
        if (currentTime >= fadetime)
        {
            //フェードアウトが終了したとき、タイマーを初期化する
            currentTime = 0.0f;
            return true;
        }
        else if (currentTime <= 0.0f)
        {
            //フェードアウト開始時はロゴイメージオブジェクトを取得する
            GameObject ob = GameObject.Find("TitleLogoImage");
            logoImage = ob.GetComponent<Image>();
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = 1.0f - (currentTime / fadetime);
        logoImage.color = new Color(1.0f, 1.0f, 1.0f, c_color);

        return false;
    }

    //ニューゲームラベルにマウスポインタが乗った時の処理を行う関数
    public void NewGameLabelEnter()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //ニューゲームラベルの文字色取得（マウスポインタが乗った時）
        Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

        Text new_game = titleNewGameLabel.GetComponent<Text>();

        //ニューゲームラベルの色変更
        new_game.color = labelColor;
    }

    //ニューゲームラベルからマウスポインタが離れた時の処理を行う関数
    public void NewGameLabelExit()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //ニューゲームラベルの文字色取得（初期状態）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text new_game = titleNewGameLabel.GetComponent<Text>();

        //ニューゲームラベルの色変更
        new_game.color = labelColor;
    }

    //ニューゲームラベルをクリックした時の処理を行う関数
    public void NewGameLabelClick()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //タイトル画面の状態をNEW GAMEクリック時のフェードアウトに移行する
        titleMode = TITLE_MODE.NEW_CLICK_FADE_OUT;
    }

    //ロードゲームラベルにマウスポインタが乗った時の処理を行う関数
    public void LoadGameLabelEnter()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //ロードゲームラベルの文字色取得（マウスポインタが乗った時）
        Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

        Text load_game = titleLoadGameLabel.GetComponent<Text>();

        //ロードゲームラベルの色変更
        load_game.color = labelColor;
    }

    //ロードゲームラベルからマウスポインタが離れた時の処理を行う関数
    public void LoadGameLabelExit()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //ロードゲームラベルの文字色取得（初期状態）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text load_game = titleLoadGameLabel.GetComponent<Text>();

        //ロードゲームラベルの色変更
        load_game.color = labelColor;
    }

    //ロードゲームラベルをクリックした時の処理を行う関数
    public void LoadGameLabelClick()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //タイトル画面の状態をLOAD GAMEクリック時のフェードアウトに移行する
        titleMode = TITLE_MODE.LOAD_CLICK_FADE_OUT;
    }

    //ゲーム終了ラベルをクリックした時の処理を行う関数
    public void ExitLabelEnter()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //ゲーム終了ラベルの文字色取得（マウスポインタが乗った時）
        Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

        Text game_exit = titleExitLabel.GetComponent<Text>();

        //ゲーム終了ラベルの色変更
        game_exit.color = labelColor;
    }

    //ゲーム終了ラベルからマウスポインタが離れた時の処理を行う関数
    public void ExitLabelExit()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //ゲーム終了ラベルの文字色取得（初期状態）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text game_exit = titleExitLabel.GetComponent<Text>();

        //ゲーム終了ラベルの色変更
        game_exit.color = labelColor;
    }

    //ゲーム終了ラベルをクリックした時の処理を行う関数
    public void ExitLabelClick()
    {
        if (titleMode == TITLE_MODE.EXIT)
        {
            //ゲーム終了選択中は何もしない
            return;
        }

        //タイトル画面の状態をEXITをクリックしたときの処理に移行する
        titleMode = TITLE_MODE.EXIT_CLICK;
    }

    //名前入力ウィンドウの決定ボタンをクリックしたときの処理を行う関数
    public void NameEntryOkButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //名前入力テキストに入力された文字列を取得する
        InputField name = nameEntryText.GetComponent<InputField>();
        if (name.text.Length == 0)
        {
            //名前が入力されていないとき
            NameEntryMessageDisp("名前が入力されていません。");
        }
        else
        {
            //名前が入力されているとき

            //メッセージを表示し、YesNoウィンドウを開く
            NameEntryMessageDisp("この名前でゲームを開始しますか？");
            YesNoController.yesNoController.YesNoPanelOpen();
            //名前入力ウィンドウの全ボタンおよび入力テキストを使用不可にする
            nameEntryOkButton.GetComponent<Button>().interactable = false;
            nameEntryClearButton.GetComponent<Button>().interactable = false;
            nameEntryGoTitleButton.GetComponent<Button>().interactable = false;
            nameEntryText.GetComponent<InputField>().interactable = false;
        }

    }

    //名前入力ウィンドウのクリアボタンをクリックしたときの処理を行う関数
    public void NameEntryClearButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");

        //名前入力テキストを初期化する
        NameEntryClear();
    }

    //名前入力ウィンドウのタイトルへ戻るボタンをクリックしたときの処理
    public void NameEntryGoTitleButtonClick()
    {
        //タイトル画面の状態をタイトル画面表示中に移行する
        titleMode = TITLE_MODE.NEW_RETURN_FADE_IN;
    }

    //タイトル画面上の全ラベルの文字色を初期化する関数
    void TitleLabelColorInit()
    {
        //全ラベルのラベルの文字色取得（初期状態）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text new_game = titleNewGameLabel.GetComponent<Text>();
        Text load_game = titleLoadGameLabel.GetComponent<Text>();
        Text game_exit = titleExitLabel.GetComponent<Text>();

        //全ラベルの色変更
        new_game.color = labelColor;
        load_game.color = labelColor;
        game_exit.color = labelColor;
    }

    //名前入力テキストの初期化を行う関数
    void NameEntryClear()
    {
        InputField name = nameEntryText.GetComponent<InputField>();
        name.text = "";
    }

    //名前入力ウィンドウのメッセージ表示ラベルに文字列を表示する関数
    //引数
    //str:表示する文字列（何も入力しないときは空白が入る）
    void NameEntryMessageDisp(string str = "")
    {
        Text msg = nameEntryMessage.GetComponent<Text>();
        msg.text = str;
    }



    //ロードウィンドウを初期化する関数
    void LoadWindowInit()
    {
        //ロードウィンドウ上のオブジェクト取得
        loadPageLabel = GameObject.FindGameObjectWithTag("LoadPage");
        loadPreviousButton = GameObject.FindGameObjectWithTag("LoadPrevious");
        loadNextButton = GameObject.FindGameObjectWithTag("LoadNext");
        loadGoTitleButton = GameObject.FindGameObjectWithTag("LoadGoTitle");

        //ロードウィンドウを非表示にする
        loadWindow.SetActive(false);
    }

    //ロードウィンドウのラベルおよびボタンを初期化する関数
    void LoadWindowClear()
    {
        //改ページボタンの初期化
        loadPreviousButton.GetComponent<Button>().interactable = true;
        loadNextButton.GetComponent<Button>().interactable = true;

        //セーブデータ情報の文字色取得（初期値）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        for (int i = 0; i < GameDataController.SAVE_PER_PAGE; i++)
        {
            Text number = loadNumberLabels[i].GetComponent<Text>();
            Text name = loadNameLabels[i].GetComponent<Text>();
            Text level = loadLevelLabels[i].GetComponent<Text>();
            Text floor = loadFloorLabels[i].GetComponent<Text>();
            Text time = loadTimeLabels[i].GetComponent<Text>();

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

    //ロードウィンドウを開く関数
    void LoadWindowOpen()
    {
        //選択されたセーブデータ番号を保存しておく変数の初期化
        loadSaveNum = -1;
        //ロードウィンドウを開く
        loadFlag = true;
        loadWindow.SetActive(loadFlag);
        //ロードウィンドウの内容を表示する
        LoadWindowDisp();
    }

    //ロードウィンドウの内容を表示する関数
    void LoadWindowDisp()
    {
        //ロードウィンドウの初期化
        LoadWindowClear();
        LoadDataListDisp();

        //現在ページおよび最大ページを取得する
        int current = loadPage;
        int page_last = loadPageMax;

        //現在ページおよび最大ページを表示する
        string str = current.ToString() + " / " + page_last.ToString();
        Text page = loadPageLabel.GetComponent<Text>();
        page.text = str;

        //現在のページに表示するセーブデータの開始番号の設定
        int load_start = (loadPage - 1) * GameDataController.SAVE_PER_PAGE;

        //前ページ移動ボタンの設定
        if (current == 1)
        {
            //現在ページが最初の時、前ページ移動ボタンを使用不可にする
            loadPreviousButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //現在ページが最初でない時、前ページ移動ボタンを使用可能にする
            loadPreviousButton.GetComponent<Button>().interactable = true;
        }

        //次ページ移動ボタンの設定
        if (current == page_last)
        {
            //現在ページが最後の時、次ページ移動ボタンを使用不可にする
            loadNextButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //現在ページが最後でない時、次ページ移動ボタンを使用可能にする
            loadNextButton.GetComponent<Button>().interactable = true;
        }

        //セーブデータリストの内容を表示する
        for (int i = 0; i < GameDataController.SAVE_PER_PAGE; i++)
        {
            Text number = loadNumberLabels[i].GetComponent<Text>();
            //データ番号の表示
            number.text = (i + load_start + 1).ToString();
            //セーブデータの内容を表示
            LoadDataListDisp();
        }
    }

    //セーブデータリスト（データロード用）の内容を表示をウィンドウに表示する関数
    void LoadDataListDisp()
    {
        //セーブデータリストへセーブデータファイルを読み込む
        GameDataController.GetSaveDataList();
        //現在のページに表示するセーブデータの開始番号の設定
        int load_start = (loadPage - 1) * GameDataController.SAVE_PER_PAGE;

        //開始番号から順にセーブデータリストの内容をページごとの最大数まで表示させる
        for (int i = 0; i < GameDataController.SAVE_PER_PAGE; i++)
        {
            //セーブデータリストのデータを1つずつ取得する
            GameData data = GameDataController.GetSaveDataInfo(load_start + i);

            Text name = loadNameLabels[i].GetComponent<Text>();
            Text level = loadLevelLabels[i].GetComponent<Text>();
            Text floor = loadFloorLabels[i].GetComponent<Text>();
            Text time = loadTimeLabels[i].GetComponent<Text>();

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

    //ロードウィンドウを閉じる関数
    void LoadWindowClose()
    {
        //ロードウィンドウを閉じる
        loadFlag = false;
        loadWindow.SetActive(loadFlag);
        loadPage = 1;
    }

    //ロードウィンドウ上のラベルの初期化を行う関数
    void LoadLabelInit()
    {
        //文字色（初期状態）の取得
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text numberText = loadNumberLabels[loadSaveNum].GetComponent<Text>();
        Text nameText = loadNameLabels[loadSaveNum].GetComponent<Text>();
        Text levelText = loadLevelLabels[loadSaveNum].GetComponent<Text>();
        Text floorText = loadFloorLabels[loadSaveNum].GetComponent<Text>();
        Text timeText = loadTimeLabels[loadSaveNum].GetComponent<Text>();

        //文字色の変更
        numberText.color = labelColor;
        nameText.color = labelColor;
        levelText.color = labelColor;
        floorText.color = labelColor;
        timeText.color = labelColor;

        //選択されたセーブデータ番号を保存しておく変数の初期化
        loadSaveNum = -1;
    }

    //ロードウィンドウ上のボタンの初期化を行う関数
    //引数
    //flag:trueの時はボタンを使用可能にし、falseの時はボタンを使用不可にする
    void LoadWindowButtonInit(bool flag)
    {
        //ウィンドウの最大ページ数を取得
        loadPageMax = GameDataController.SavePageMaxCalc();

        //前ページ移動ボタンの設定
        if (loadPage == 1)
        {
            //最初のページの時は前ページ移動ボタンを使用不可にする
            loadPreviousButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最初のページでないとき時は引数に応じて、使用できるかどうかを設定する
            loadPreviousButton.GetComponent<Button>().interactable = flag;
        }

        //次ページ移動ボタンの設定
        if (loadPage == loadPageMax)
        {
            //最後のページの時は使用不可にする
            loadNextButton.GetComponent<Button>().interactable = false;
        }
        else
        {
            //最後のページでないとき時は引数に応じて、使用できるかどうかを設定する
            loadNextButton.GetComponent<Button>().interactable = flag;
        }

        //タイトルへ戻るボタンは引数に応じて、使用できるかどうかを設定する
        loadGoTitleButton.GetComponent<Button>().interactable = flag;
    }

    //ロードウィンドウの「前ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void LoadPreviousButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ページ数を1減らす
        loadPage--;
        //最初のページの時はそのままにする
        if (loadPage < 1)
        {
            loadPage = 1;
        }
        //ウィンドウの内容を更新する
        LoadWindowDisp();
    }

    //ロードウィンドウの「次ページへ移動」ボタンがクリックされた時の処理を行う関数
    public void LoadNextButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");

        //ページ数を1増やす
        loadPage++;
        //最後のページの時はそのままにする
        if (loadPage > loadPageMax)
        {
            loadPage = loadPageMax;
        }
        //ウィンドウの内容を更新する
        LoadWindowDisp();
    }

    //ロードウィンドウの「タイトルへ戻る」ボタンがクリックされた時の処理を行う関数
    public void LoadGoTitleButtonClick()
    {
        //タイトル画面の状態をLOAD GAMEからタイトル画面へ戻るフェードインにする
        titleMode = TITLE_MODE.LOAD_RETURN_FADE_IN;
    }

    //ロードウィンドウのプレイヤー名ラベルにマウスポインタが乗った時の処理を行う関数
    //引数
    //num:対象のプレイヤー名ラベルの番号
    public void LoadLabelOnMouseEnter(int num)
    {
        if (loadClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいたセーブデータリスト番号を取得
        int click_load = num + ((loadPage - 1) * GameDataController.SAVE_PER_PAGE);

        //セーブデータの文字色取得（マウスポインタが乗った時）
        Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

        Text numberText = loadNumberLabels[num].GetComponent<Text>();
        Text nameText = loadNameLabels[num].GetComponent<Text>();
        Text levelText = loadLevelLabels[num].GetComponent<Text>();
        Text floorText = loadFloorLabels[num].GetComponent<Text>();
        Text timeText = loadTimeLabels[num].GetComponent<Text>();

        //データ番号、プレイヤー名、レベル、現在フロア、セーブ日時の色変更
        numberText.color = labelColor;
        nameText.color = labelColor;
        levelText.color = labelColor;
        floorText.color = labelColor;
        timeText.color = labelColor;
    }

    //ロードウィンドウのプレイヤー名ラベルからマウスポインタが離れた時の処理を行う関数
    //引数
    //num:対象のプレイヤー名ラベルの番号
    public void LoadLabelOnMouseExit(int num)
    {
        if (loadClickFlag == true)
        {
            //対象ラベルがクリックされているときは何もしない
            return;
        }

        //対象ラベルに基づいたセーブデータリスト番号を取得
        int click_load = num + ((loadPage - 1) * GameDataController.SAVE_PER_PAGE);

        //セーブデータの文字色取得（初期状態）
        Color labelColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        Text numberText = loadNumberLabels[num].GetComponent<Text>();
        Text nameText = loadNameLabels[num].GetComponent<Text>();
        Text levelText = loadLevelLabels[num].GetComponent<Text>();
        Text floorText = loadFloorLabels[num].GetComponent<Text>();
        Text timeText = loadTimeLabels[num].GetComponent<Text>();

        //データ番号、プレイヤー名、レベル、現在フロア、セーブ日時の色変更
        numberText.color = labelColor;
        nameText.color = labelColor;
        levelText.color = labelColor;
        floorText.color = labelColor;
        timeText.color = labelColor;
    }

    //ロードウィンドウのプレイヤー名ラベルをクリックした時の処理を行う関数
    //引数
    //num:対象のプレイヤー名ラベルの番号
    public void LoadLabelOnMouseClick(int num)
    {
        //対象ラベルに基づいたセーブデータリスト番号を取得
        int click_load = num + ((loadPage - 1) * GameDataController.SAVE_PER_PAGE);

        string str = "";

        //対象セーブデータのラベル番号の取得
        loadSaveNum = num;

        if (loadClickFlag == false)
        {
            //プレイヤー名ラベルがクリックされていないとき

            //ボタンをクリックしたときの効果音を鳴らす
            SoundManager.soundManager.PlaySE("se_decision");

            //セーブデータクリックフラグをオンにする
            loadClickFlag = true;

            //セーブデータの文字色取得（マウスポインタが乗った時）
            Color labelColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

            Text numberText = loadNumberLabels[num].GetComponent<Text>();
            Text nameText = loadNameLabels[num].GetComponent<Text>();
            Text levelText = loadLevelLabels[num].GetComponent<Text>();
            Text floorText = loadFloorLabels[num].GetComponent<Text>();
            Text timeText = loadTimeLabels[num].GetComponent<Text>();

            //データ番号、プレイヤー名、レベル、現在フロア、セーブ日時の色変更
            numberText.color = labelColor;
            nameText.color = labelColor;
            levelText.color = labelColor;
            floorText.color = labelColor;
            timeText.color = labelColor;

            //ロードウィンドウのボタンを使用不可にする
            LoadWindowButtonInit(false);

            //選択したデータの番号を取得
            str = (click_load + 1).ToString();

            //対象番号のファイルが存在しているかをチェックする
            if (GameDataController.SaveDataExistCheck(click_load) == false)
            {
                //存在していないとき
                str = "データがありません。\n他のデータを選択してください。\n";
                //セーブデータクリックフラグをオフにする
                loadClickFlag = false;
                LoadWindowButtonInit(true);
            }
            else
            {
                //存在しているとき
                str = str + "番のデータをロードしますか？\n";
                //YesNoウィンドウを開く
                YesNoController.yesNoController.YesNoPanelOpen();
                //取得したセーブデータリスト番号を記憶させておく
                loadArrayNum = click_load;
            }

            //上記で作成したメッセージを表示
            MessageController.msgController.MessageDisp(str);

        }
    }

}
