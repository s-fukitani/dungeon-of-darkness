using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Common;

//画面全体用エフェクトの制御を行うクラス
public class LargeEffectController : MonoBehaviour
{
    public static LargeEffectController largeEffectController;  //操作中のパネル
    [SerializeField] private GameObject largeEffectPanel;       //画面全体用エフェクトで使用するパネルオブジェクト
    [SerializeField] private Animator effectAnim;               //エフェクト用のアニメーターオブジェクト（主にプレイヤーに対する攻撃に使用）

    //エフェクトの定数
    public const string ATTACK = "Attack";              //敵の攻撃
    public const string HIT = "Hit";                    //プレイヤーへの攻撃命中
    public const string FIRE_BALL = "FireBall";         //火の玉の魔法使用
    public const string THUNDER = "Thunder";            //雷の魔法使用
    public const string POISON = "Poison";              //毒液噴射
    public const string SLEEP = "Sleep";                //眠りの魔法使用
    public const string SEALED = "Sealed";              //封印の魔法使用
    public const string DEATH = "Death";                //死の魔法使用
    public const string FIRE_BREATH = "FireBreath";     //火炎噴射
    public const string DEATH_SICKLE = "DeathSickle";   //死神の鎌
    public const string PLAYER_HEAL = "PlayerHeal";     //プレイヤーの体力回復
    public const string MOVE_LAST = "MoveLast";         //街の入口で悪魔の像を使用

    void Awake()
    {
        if (largeEffectController == null)
        {
            //パネルが存在しないときはパネルを作成し初期化する
            largeEffectController = this;
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

    //画面全体用エフェクトパネルを表示する関数
    public void LargeEffectPanelOpen()
    {
        largeEffectPanel.SetActive(true);
    }

    //画面全体用エフェクトパネルを非表示にする関数
    public void LargeEffectPanelClose()
    {
        largeEffectPanel.SetActive(false);
    }

    //画面全体にエフェクトを起こす関数（迷宮のみ使用）
    //引数
    //trigger:エフェクトのトリガー
    public void PlayLargeEffect(string trigger)
    {
        if (trigger == ATTACK)
        {
            //敵の攻撃
            effectAnim.SetTrigger(ATTACK);
        }
        else if (trigger == HIT)
        {
            //プレイヤーへの攻撃命中
            effectAnim.SetTrigger(HIT);
        }
        else if (trigger == FIRE_BALL)
        {
            //火の玉の魔法使用
            effectAnim.SetTrigger(FIRE_BALL);
        }
        else if (trigger == THUNDER)
        {
            //雷の魔法使用
            effectAnim.SetTrigger(THUNDER);
        }
        else if (trigger == POISON)
        {
            //毒液噴射
            effectAnim.SetTrigger(POISON);
        }
        else if (trigger == SLEEP)
        {
            //眠りの魔法使用
            effectAnim.SetTrigger(SLEEP);
        }
        else if (trigger == SEALED)
        {
            //封印の魔法使用
            effectAnim.SetTrigger(SEALED);
        }
        else if (trigger == DEATH)
        {
            //死の魔法使用
            effectAnim.SetTrigger(DEATH);
        }
        else if (trigger == FIRE_BREATH)
        {
            //火炎噴射
            effectAnim.SetTrigger(FIRE_BREATH);
        }
        else if (trigger == DEATH_SICKLE)
        {
            //死神の鎌
            effectAnim.SetTrigger(DEATH_SICKLE);
        }
        else if (trigger == PLAYER_HEAL)
        {
            //プレイヤーの体力回復
            effectAnim.SetTrigger(PLAYER_HEAL);
        }
        else if (trigger == MOVE_LAST)
        {
            //街の入口で悪魔の像を使用
            effectAnim.SetTrigger(MOVE_LAST);
        }

    }
}
