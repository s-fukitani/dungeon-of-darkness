using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Common;

//イベント画像パネルを制御するクラス
public class EventImageController : MonoBehaviour
{
    public static EventImageController evImageController;   //操作中のパネル
    public GameObject eventPanel;                           //イベント画像で使用するパネルオブジェクト
    public Image eventImage;                                //イベント画像イメージ
    private Sprite eventSprite;                             //イベント画像スプライト
    private bool panelOpenFlag;                             //イベント画像が表示中かどうかのフラグ（true:表示中、false:閉じている）
    [SerializeField] private Animator effectAnim;           //エフェクト用のアニメーターオブジェクト（主に敵に対する攻撃に使用）

    //エフェクトの定数
    public const string ATTACK = "Attack";                      //敵への攻撃
    public const string HIT = "Hit";                            //敵への攻撃命中
    public const string FIRE_BALL = "FireBall";                 //魔法の杖使用
    public const string THUNDER = "Thunder";                    //雷の杖使用
    public const string POISON = "Poison";                      //毒蛇の香使用
    public const string SLEEP = "Sleep";                        //眠りの鈴使用
    public const string SEALED = "Sealed";                      //魔封じの護符使用
    public const string DEATH = "Death";                        //死の首飾り使用
    public const string ENEMY_HEAL = "EnemyHeal";               //敵の体力回復
    public const string ENEMY_HEAL_MAGIC = "EnemyHealMagic";    //敵の回復魔法使用

    void Awake()
    {
        if (evImageController == null)
        {
            //パネルが存在しないときはパネルを作成し初期化する
            evImageController = this;
            ImagePanelClose();
            //ImageClear();
            panelOpenFlag = false;
            eventPanel.SetActive(panelOpenFlag);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //イベント画像パネルを閉じる関数
    public void ImagePanelClose()
    {
        //ImageClear();
        if (panelOpenFlag == true)
        {
            //イベント画像パネルを非表示にする
            panelOpenFlag = false;
            eventPanel.SetActive(panelOpenFlag);
        }

    }

    //イベント画像パネルを初期化する関数
    public void ImageClear()
    {
        //空白画像ファイルをロードして表示する
        eventSprite = Resources.Load<Sprite>("None");
        eventImage = this.GetComponent<Image>();
        eventImage.sprite = eventSprite;
    }

    //イベント画像パネルに画像を表示する関数
    //引数
    //img:イベント画像のファイル名
    public void ImageDisp(string img)
    {
        if (panelOpenFlag == false)
        {
            //パネルが表示されていないときは表示する
            panelOpenFlag = true;
            eventPanel.SetActive(panelOpenFlag);
        }

        //指定した画像ファイルをロードして表示する
        string dispImg = GlobalConst.IMG_DIR + img;
        eventSprite = Resources.Load<Sprite>(dispImg) as Sprite;
        GameObject ob = GameObject.Find("EventImage");
        eventImage = ob.GetComponent<Image>();
        eventImage.sprite = eventSprite;
    }

    //イベント画像にエフェクトを起こす関数（迷宮のみ使用）
    //引数
    //trigger:エフェクトのトリガー
    public void PlayEffect(string trigger)
    {
        if (trigger == ATTACK)
        {
            //敵を攻撃
            effectAnim.SetTrigger(ATTACK);
        }
        else if (trigger == HIT)
        {
            //敵に攻撃命中（攻撃魔法アイテム含む）
            effectAnim.SetTrigger(HIT);
        }
        else if (trigger == FIRE_BALL)
        {
            //魔法の杖
            effectAnim.SetTrigger(FIRE_BALL);
        }
        else if (trigger == THUNDER)
        {
            //雷の杖
            effectAnim.SetTrigger(THUNDER);
        }
        else if (trigger == POISON)
        {
            //毒蛇の香
            effectAnim.SetTrigger(POISON);
        }
        else if (trigger == SLEEP)
        {
            //眠りの鈴
            effectAnim.SetTrigger(SLEEP);
        }
        else if (trigger == SEALED)
        {
            //魔封じの護符
            effectAnim.SetTrigger(SEALED);
        }
        else if (trigger == DEATH)
        {
            //死の首飾り
            effectAnim.SetTrigger(DEATH);
        }
        else if (trigger == ENEMY_HEAL)
        {
            //敵の体力回復
            effectAnim.SetTrigger(ENEMY_HEAL);
        }
        else if (trigger == ENEMY_HEAL_MAGIC)
        {
            //敵の回復魔法使用
            effectAnim.SetTrigger(ENEMY_HEAL_MAGIC);
        }
    }



}
