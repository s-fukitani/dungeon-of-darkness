using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//ダンジョン用コマンドボタンの制御を行うクラス
public class ButtonController : MonoBehaviour
{
    //ボタンの種類を表す列挙型
    public enum BTNNAME
    {
        CHECK,      //調べる
        TALK,       //話す
        FIGHT,      //戦う
        ESCAPE,     //逃げる
        ITEM,       //道具
        STATUS,     //強さ
        SAVE        //保存／終了
    }

    //現在の状況を示す列挙型
    public enum CMDTYPE
    {
        NORMAL,     //通常
        TALK,       //会話中
        FIGHT,      //戦闘中
        WAIT,       //待機中
        STATUS,     //ステータスウィンドウ表示中
        ITEM,       //アイテムウィンドウ表示中
        SAVE,       //セーブウィンドウ表示中
        YESNO       //YesNoウィンドウ表示中
    }

    private bool[] buttonFlag;          //ボタン使用可能フラグ（true:使用可能、false:使用不可）
    private GameObject commandPanel;    //コマンドパネルオブジェクト
    private Transform[] commandButtons; //コマンドボタンオブジェクト

    // Start is called before the first frame update
    void Start()
    {

    }

    void Awake()
    {
        //コマンドパネルオブジェクトの取得
        commandPanel = GameObject.Find("CommandPanel");
        //ボタンの数だけコマンドボタンオブジェクトとボタン使用可能フラグを初期化する
        commandButtons = new Transform[commandPanel.transform.childCount];
        buttonFlag = new bool[commandPanel.transform.childCount];

        for (int i = 0; i < commandPanel.transform.childCount; i++)
        {
            //コマンドボタンオブジェクトを取得する
            commandButtons[i] = commandPanel.transform.GetChild(i);
            //全ボタンのボタン使用可能フラグを使用不可にする
            buttonFlag[i] = false;
        }
    }

    //状況に応じて使用可能ボタンを変更する関数
    //引数
    //ctype:現在の状況
    public void ButtonFlagChange(ButtonController.CMDTYPE ctype)
    {
        if (ctype == CMDTYPE.NORMAL)
        {
            //通常
            buttonFlag[(int)BTNNAME.CHECK] = true;
            buttonFlag[(int)BTNNAME.TALK] = false;
            buttonFlag[(int)BTNNAME.FIGHT] = false;
            buttonFlag[(int)BTNNAME.ESCAPE] = false;
            buttonFlag[(int)BTNNAME.ITEM] = true;
            buttonFlag[(int)BTNNAME.STATUS] = true;
            buttonFlag[(int)BTNNAME.SAVE] = true;

        }
        else if (ctype == CMDTYPE.TALK)
        {
            //会話中
            buttonFlag[(int)BTNNAME.CHECK] = false;
            buttonFlag[(int)BTNNAME.TALK] = true;
            buttonFlag[(int)BTNNAME.FIGHT] = true;
            buttonFlag[(int)BTNNAME.ESCAPE] = false;
            buttonFlag[(int)BTNNAME.ITEM] = true;
            buttonFlag[(int)BTNNAME.STATUS] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.FIGHT)
        {
            //戦闘中
            buttonFlag[(int)BTNNAME.CHECK] = false;
            buttonFlag[(int)BTNNAME.TALK] = false;
            buttonFlag[(int)BTNNAME.FIGHT] = true;
            buttonFlag[(int)BTNNAME.ESCAPE] = true;
            buttonFlag[(int)BTNNAME.ITEM] = true;
            buttonFlag[(int)BTNNAME.STATUS] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.WAIT)
        {
            //待機中
            buttonFlag[(int)BTNNAME.CHECK] = false;
            buttonFlag[(int)BTNNAME.TALK] = false;
            buttonFlag[(int)BTNNAME.FIGHT] = false;
            buttonFlag[(int)BTNNAME.ESCAPE] = false;
            buttonFlag[(int)BTNNAME.ITEM] = false;
            buttonFlag[(int)BTNNAME.STATUS] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if(ctype == CMDTYPE.STATUS)
        {
            //ステータスウィンドウ表示中
            buttonFlag[(int)BTNNAME.CHECK] = false;
            buttonFlag[(int)BTNNAME.TALK] = false;
            buttonFlag[(int)BTNNAME.FIGHT] = false;
            buttonFlag[(int)BTNNAME.ESCAPE] = false;
            buttonFlag[(int)BTNNAME.ITEM] = false;
            buttonFlag[(int)BTNNAME.STATUS] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.ITEM)
        {
            //アイテムウィンドウ表示中
            buttonFlag[(int)BTNNAME.CHECK] = false;
            buttonFlag[(int)BTNNAME.TALK] = false;
            buttonFlag[(int)BTNNAME.FIGHT] = false;
            buttonFlag[(int)BTNNAME.ESCAPE] = false;
            buttonFlag[(int)BTNNAME.ITEM] = false;
            buttonFlag[(int)BTNNAME.STATUS] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.SAVE)
        {
            //セーブウィンドウ表示中
            buttonFlag[(int)BTNNAME.CHECK] = false;
            buttonFlag[(int)BTNNAME.TALK] = false;
            buttonFlag[(int)BTNNAME.FIGHT] = false;
            buttonFlag[(int)BTNNAME.ESCAPE] = false;
            buttonFlag[(int)BTNNAME.ITEM] = false;
            buttonFlag[(int)BTNNAME.STATUS] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.YESNO)
        {
            //YesNoウィンドウ表示中
            buttonFlag[(int)BTNNAME.CHECK] = false;
            buttonFlag[(int)BTNNAME.TALK] = false;
            buttonFlag[(int)BTNNAME.FIGHT] = false;
            buttonFlag[(int)BTNNAME.ESCAPE] = false;
            buttonFlag[(int)BTNNAME.ITEM] = false;
            buttonFlag[(int)BTNNAME.STATUS] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }

        //使用可能フラグに応じてそれぞれのボタンを使用可能にするかの設定を行う
        for (int i = 0; i < commandPanel.transform.childCount; i++)
        {
            commandButtons[i].GetComponent<Button>().interactable = buttonFlag[i];
        }

    }

    //コマンドパネルを開く関数
    //引数
    //ctype:現在の状況
    public void CommandPanelOpen(CMDTYPE ctype)
    {
        //コマンドパネルを表示する
        commandPanel.SetActive(true);
        //状況に応じて使用可能なボタンを変更する
        ButtonFlagChange(ctype);
    }

    //コマンドパネルを閉じる関数
    public void CommandPanelClose()
    {
        //コマンドパネルを非表示にする
        commandPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
