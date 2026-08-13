using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//街用コマンドボタンの制御を行うクラス
public class CityButtonController : MonoBehaviour
{
    //ボタンの種類を表す列挙型
    public enum BTNNAME
    {
        WEAPONS,    //武器屋
        ITEMS,      //道具屋
        INN,        //宿屋
        CASTLE,     //お城
        BAR,        //酒場
        DUNGEON,    //迷宮入口
        SAVE        //保存／終了
    }

    //現在の状況を示す列挙型
    public enum CMDTYPE
    {
        NORMAL,     //通常
        INNER,      //施設内
        STATUS,     //ステータスウィンドウ表示中
        ITEM,       //アイテムウィンドウ表示中
        SAVE,       //セーブウィンドウ表示中
        YESNO,      //YesNoウィンドウ表示中
        WAIT        //待機中
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
    public void ButtonFlagChange(CityButtonController.CMDTYPE ctype)
    {
        if (ctype == CMDTYPE.NORMAL)
        {
            //通常
            buttonFlag[(int)BTNNAME.WEAPONS] = true;
            buttonFlag[(int)BTNNAME.ITEMS] = true;
            buttonFlag[(int)BTNNAME.INN] = true;
            buttonFlag[(int)BTNNAME.CASTLE] = true;
            buttonFlag[(int)BTNNAME.BAR] = true;
            buttonFlag[(int)BTNNAME.DUNGEON] = true;
            buttonFlag[(int)BTNNAME.SAVE] = true;

        }
        else if (ctype == CMDTYPE.INNER)
        {
            //施設内
            buttonFlag[(int)BTNNAME.WEAPONS] = false;
            buttonFlag[(int)BTNNAME.ITEMS] = false;
            buttonFlag[(int)BTNNAME.INN] = false;
            buttonFlag[(int)BTNNAME.CASTLE] = false;
            buttonFlag[(int)BTNNAME.BAR] = false;
            buttonFlag[(int)BTNNAME.DUNGEON] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.STATUS)
        {
            //ステータスウィンドウ表示中
            buttonFlag[(int)BTNNAME.WEAPONS] = false;
            buttonFlag[(int)BTNNAME.ITEMS] = false;
            buttonFlag[(int)BTNNAME.INN] = false;
            buttonFlag[(int)BTNNAME.CASTLE] = false;
            buttonFlag[(int)BTNNAME.BAR] = false;
            buttonFlag[(int)BTNNAME.DUNGEON] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.ITEM)
        {
            //アイテムウィンドウ表示中
            buttonFlag[(int)BTNNAME.WEAPONS] = false;
            buttonFlag[(int)BTNNAME.ITEMS] = false;
            buttonFlag[(int)BTNNAME.INN] = false;
            buttonFlag[(int)BTNNAME.CASTLE] = false;
            buttonFlag[(int)BTNNAME.BAR] = false;
            buttonFlag[(int)BTNNAME.DUNGEON] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.SAVE)
        {
            //セーブウィンドウ表示中
            buttonFlag[(int)BTNNAME.WEAPONS] = false;
            buttonFlag[(int)BTNNAME.ITEMS] = false;
            buttonFlag[(int)BTNNAME.INN] = false;
            buttonFlag[(int)BTNNAME.CASTLE] = false;
            buttonFlag[(int)BTNNAME.BAR] = false;
            buttonFlag[(int)BTNNAME.DUNGEON] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.YESNO)
        {
            //YesNoウィンドウ表示中
            buttonFlag[(int)BTNNAME.WEAPONS] = false;
            buttonFlag[(int)BTNNAME.ITEMS] = false;
            buttonFlag[(int)BTNNAME.INN] = false;
            buttonFlag[(int)BTNNAME.CASTLE] = false;
            buttonFlag[(int)BTNNAME.BAR] = false;
            buttonFlag[(int)BTNNAME.DUNGEON] = false;
            buttonFlag[(int)BTNNAME.SAVE] = false;
        }
        else if (ctype == CMDTYPE.WAIT)
        {
            //待機中
            buttonFlag[(int)BTNNAME.WEAPONS] = false;
            buttonFlag[(int)BTNNAME.ITEMS] = false;
            buttonFlag[(int)BTNNAME.INN] = false;
            buttonFlag[(int)BTNNAME.CASTLE] = false;
            buttonFlag[(int)BTNNAME.BAR] = false;
            buttonFlag[(int)BTNNAME.DUNGEON] = false;
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
