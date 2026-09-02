using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Common;

//エンディングおよびゲームオーバー画面の制御を行うクラス
public class EndingController : MonoBehaviour 
{ 
    //エンディングの数（ゲームオーバー含む）
    private const int ENDING_MAX = 6;

    //エンディングおよびゲームオーバーのBGMファイル名の配列
    private string[] endBGMArray = new string[ENDING_MAX] {
        "bgm_gameover",
        "bgm_ending1",
        "bgm_ending2",
        "bgm_ending3",
        "bgm_ending4",
        "bgm_ending5"
    };
    public GameObject endingPanel;          //エンディング画面オブジェクト

    private Sprite endingSprite;            //エンディング背景スプライト
    private Image endingImage;              //エンディング背景イメージ
    private Sprite endingTextSprite;        //エンディングテキストスプライト
    private Image endingTextImage;          //エンディングテキストイメージ
    private float currentTime = 0.0f;       //タイマーの現在時間（フェードイン、フェードアウトに使用）
    private int endingCount = 0;            //エンディング画面の表示演出の進行状況を示すカウンタ
    private float endingTextPosY = 0.0f;    //エンディングテキストイメージのY座標
    private string endingBackFile = "";     //エンディング背景ファイル名
    private string endingTextFile = "";     //エンディングテキストファイル名
    private float bgmLength = 0.0f;         //音声データの長さ（秒）

    // Start is called before the first frame update
    void Start()
    {
        #region デバッグ
        //エンディングフラグを設定（デバッグ用）
        //GameDataController.SetEndingFlag(5);
        #endregion

        //使用する背景画像及び文字画像の設定
        if (GameDataController.endingFlag == 0)
        {
            //ゲームオーバーの時
            endingBackFile = "gameover";

            //エンディング文字画像を非表示にする
            TextImageHidden();
        }
        else
        {
            //エンディングの時

            //エンディングによって使用する背景画像および文字画像を変更する
            int ending_flag = GameDataController.endingFlag;
            endingBackFile = "ending" + ending_flag.ToString("D2");
            endingTextFile = "ending" + ending_flag.ToString("D2") + "_text";

            //エンディング文字画像を初期位置に設定
            TextImageDisp(endingTextFile);
        }

    }

    // Update is called once per frame
    void Update()
    {
        //Xキーが押された時
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (SoundManager.soundManager.BGMPlayingCheck() == true)
            {
                //BGM再生中の時はBGMを停止する
                SoundManager.soundManager.StopBGM(0.5f);

            }

            //エンディングをスキップする
            endingCount = 99;
        }

        if (GameDataController.endingFlag == 0)
        {
            //ゲームオーバーの時
            switch (endingCount)
            {
                case 0:     //ゲームオーバー背景画像フェードイン
                    if (BackImageFadeIn(endingBackFile, 2.0f) == true)
                    {
                        endingCount++;
                    }
                    break;
                case 1:     //ゲームオーバーのBGMを鳴らす
                    PlayEndingBGM();
                    endingCount++;
                    break;
                case 2:     //ゲームオーバー画面待機（BGMが再生中の間）
                    if (CommonMethod.TimeWait(bgmLength) == true)
                    {
                        endingCount++;
                    }
                    break;
                case 3:     //ゲームオーバー背景画像フェードアウト
                    if (BackImageFadeOut(3.0f) == true)
                    {
                        endingCount++;
                    }
                    break;
                default:    //ゲームオーバー終了後
                    //エンディングフラグを初期化する
                    GameDataController.SetEndingFlag(0);
                    //タイトル画面へ移動する
                    SceneManager.LoadScene("TitleScene");
                    break;
            }
        }
        else
        {
            //エンディングの時

            //エンディング画面演出中（演出が1つ終わるごとにカウンタを1加算していく）
            switch (endingCount)
            {
                case 0:     //エンディング背景画像フェードイン
                    if (BackImageFadeIn(endingBackFile, 2.0f) == true)
                    {
                        endingCount++;
                    }
                    break;
                case 1:     //エンディング文字画像を初期位置に表示
                    endingTextImage.enabled = true;
                    endingCount++;
                    break;
                case 2:     //エンディングのBGMを鳴らす
                    PlayEndingBGM();
                    endingCount++;
                    break;
                case 3:     //エンディング文字画像を下から上に移動させる（移動時間はBGM再生時間に合わせる）
                    if (MoveTextImageTime(bgmLength) == true)
                    {
                        endingCount++;
                    }
                    break;
                case 4:     //エンディング文字画像を非表示にする
                    endingTextImage.enabled = false;
                    endingCount++;
                    break;
                case 5:     //エンディング背景画像フェードアウト
                    if (BackImageFadeOut(2.0f) == true)
                    {
                        endingCount++;
                    }
                    break;
                case 6:     //エンディング終了画像フェードイン
                    if (BackImageFadeIn("ending_end", 2.0f) == true)
                    {
                        endingCount++;
                    }
                    break;
                case 7:     //エンディング終了画面待機
                    if (CommonMethod.TimeWait(20.0f) == true)
                    {
                        endingCount++;
                    }
                    break;
                case 8:     //エンディング終了画像フェードアウト
                    if (BackImageFadeOut(3.0f) == true)
                    {
                        endingCount++;
                    }
                    break;
                default:    //エンディング終了後
                    //エンディングフラグを初期化する
                    GameDataController.SetEndingFlag(0);
                    //タイトル画面へ移動する
                    SceneManager.LoadScene("TitleScene");
                    break;
            }
        }


    }

    //エンディング背景画像を表示する処理を行う関数
    //引数
    //img:画像ファイル名
    void BackImageDisp(string img)
    {
        //対象の背景画像をロードする
        string dispImg = GlobalConst.IMG_DIR + img;
        endingSprite = Resources.Load<Sprite>(dispImg) as Sprite;
        //ロードした背景画像を表示する
        GameObject ob = GameObject.Find("EndingImage");
        endingImage = ob.GetComponent<Image>();
        endingImage.sprite = endingSprite;
    }

    //エンディング文字画像を初期位置に設定する処理を行う関数
    //引数
    //img:画像ファイル名
    void TextImageDisp(string img)
    {
        //対象のテキスト画像をロードする
        string dispImg = GlobalConst.IMG_DIR + img;
        endingTextSprite = Resources.Load<Sprite>(dispImg) as Sprite;

        endingTextPosY = -(endingTextSprite.texture.height);

        //ロードしたテキスト画像を表示する
        GameObject ob = GameObject.Find("EndingTextImage");
        endingTextImage = ob.GetComponent<Image>();
        endingTextImage.SetNativeSize();
        endingTextImage.sprite = endingTextSprite;

        //位置を初期化する
        endingTextImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, endingTextPosY);

        //背景画像のフェードインを行うため非表示にしておく
        endingTextImage.enabled = false;
    }

    //エンディング文字画像を初非表示にする処理を行う関数（ゲームオーバー画面では不要なため）
    void TextImageHidden()
    {
        GameObject ob = GameObject.Find("EndingTextImage");
        endingTextImage = ob.GetComponent<Image>();
        endingTextImage.enabled = false;
    }

    //エンディングの文字画像を指定した時間で下から上に移動させる関数
    //引数
    //movetime:移動にかける時間（秒）
    //戻り値（true:移動終了、false:移動途中）
    bool MoveTextImageTime(float movetime)
    {
        if (currentTime >= movetime)
        {
            //移動が終了したとき、タイマーを初期化する
            currentTime = 0.0f;
            return true;
        }

        currentTime += Time.deltaTime;
        if (currentTime >= movetime)
        {
            currentTime = movetime;

        }

        //移動量を算出する
        float move_height = -(endingTextPosY * (2.0f * (currentTime / movetime)));

        //現在のY座標を取得する
        float now_pos_y = endingTextPosY + move_height;

        endingTextImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, now_pos_y);

        return false;
    }

    //エンディング背景のフェードインを行う関数
    //引数
    //img:画像ファイル名
    //fadetime:フェードインにかける時間（秒）
    //戻り値（true:フェードイン終了、false:フェードイン途中）
    bool BackImageFadeIn(string img, float fadetime)
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

            //対象の背景画像をロードする
            string dispImg = GlobalConst.IMG_DIR + img;
            endingSprite = Resources.Load<Sprite>(dispImg) as Sprite;
            //背景イメージオブジェクトを取得する
            GameObject ob = GameObject.Find("EndingImage");
            endingImage = ob.GetComponent<Image>();
            endingImage.sprite = endingSprite;
            endingImage.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = currentTime / fadetime;
        endingImage.color = new Color(c_color, c_color, c_color, 1.0f);

        return false;

    }

    //エンディング背景のフェードアウトを行う関数
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

        currentTime += Time.deltaTime;
        if (currentTime >= fadetime)
        {
            currentTime = fadetime;

        }

        //現在の背景イメージオブジェクトの色を設定する
        float c_color = 1.0f - (currentTime / fadetime);
        endingImage.color = new Color(c_color, c_color, c_color, 1.0f);

        return false;

    }

    //エンディング（ゲームオーバー含む）のBGMを鳴らす関数
    void PlayEndingBGM()
    {
        bgmLength = SoundManager.soundManager.GetBGMLength(endBGMArray[GameDataController.endingFlag]);
        SoundManager.soundManager.PlayBGM(endBGMArray[GameDataController.endingFlag], 0.5f, false);
    }
}
