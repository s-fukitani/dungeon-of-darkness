using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//YesNoウィンドウを制御するクラス
public class YesNoController : MonoBehaviour
{
    public static YesNoController yesNoController;  //操作中のウィンドウ
    public GameObject yesNoPanel;                   //YesNoウィンドウで使用するパネルオブジェクト

    //どのボタンが押されたかを示す列挙型
    public enum REPLY
    {
        EMPTY,      //押されていない
        YES,        //「はい」が押された
        NO          //「いいえ」が押された
    }

    private bool panelOpenFlag;     //YesNoウィンドウが開いているかのフラグ（true:開いている、false:閉じている）
    private REPLY yesNoReply;       //どのボタンが押されたかを示すフラグ
    private string yesNoText;       //YesNoウィンドウに表示するテキスト

    void Awake()
    {
        if (yesNoController == null)
        {
            //ウィンドウが存在しないときはウィンドウを作成し初期化する
            yesNoController = this;
            panelOpenFlag = false;
            yesNoReply = REPLY.EMPTY;
            yesNoPanel.SetActive(panelOpenFlag);
            yesNoText = "";
        }
    }

    //YesNoウィンドウを開く関数
    //引数
    //txt:YesNoウィンドウに表示するテキスト（何も入力しないときは空白を表示する）
    public void YesNoPanelOpen(string txt = "")
    {
        //ウィンドウを表示させ、どのボタンが押されたかを示すフラグを「押されていない」に設定する
        panelOpenFlag = true;
        yesNoReply = REPLY.EMPTY;
        yesNoPanel.SetActive(panelOpenFlag);
        //引数の文字列を表示させる
        Text yes_no_text = GameObject.Find("YesNoText").GetComponent<Text>();
        yes_no_text.text = txt;
    }

    //YesNoウィンドウを閉じる関数
    public void YesNoPanelClose()
    {
        //ウィンドウを非表示にする
        panelOpenFlag = false;
        yesNoPanel.SetActive(panelOpenFlag);
    }

    //「はい」ボタンがクリックされた時の処理を行う関数
    public void YesButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_decision");
        yesNoReply = REPLY.YES;
    }

    //「いいえ」ボタンがクリックされた時の処理を行う関数
    public void NoButtonClick()
    {
        //ボタンをクリックしたときの効果音を鳴らす
        SoundManager.soundManager.PlaySE("se_cancel");
        yesNoReply = REPLY.NO;
    }

    //どのボタンが押されたかを示すフラグを返す関数
    //戻り値（どのボタンが押されたかを示すフラグ）
    public REPLY GetYesNoReply()
    {
        return yesNoReply;
    }

    //YesNoウィンドウが表示されているかどうかのフラグを返す
    //戻り値（true:表示されている、false:表示されていない）
    public bool GetPanelOpenFlag()
    {
        return panelOpenFlag;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
