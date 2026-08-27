using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//メッセージウィンドウを制御するクラス
public class MessageController : MonoBehaviour
{
    public static MessageController msgController;  //操作中のウィンドウ
    public GameObject messagePanel;                 //メッセージウィンドウで使用するパネルオブジェクト
    public Text messageText;                        //メッセージ表示用のテキスト
    private bool panelOpenFlag;                     //メッセージウィンドウが開いているかのフラグ（true:開いている、false:閉じている）

    void Awake()
    {
        if (msgController == null)
        {
            //ウィンドウが存在しないときはウィンドウを作成し初期化する
            msgController = this;
            MessageClear();
            panelOpenFlag = false;
            messagePanel.SetActive(panelOpenFlag);
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

    //メッセージウィンドウを閉じる関数
    public void MessagePanelClose()
    {
        //表示メッセージを消去する
        MessageClear();
        //メッセージウィンドウを非表示にする
        panelOpenFlag = false;
        messagePanel.SetActive(panelOpenFlag);
    }

    //表示メッセージを消去する関数
    public void MessageClear()
    {
        messageText.text = "";
    }

    //メッセージウィンドウにメッセージを表示する関数
    //（既にメッセージが存在するときは前のメッセージは消去する）
    //引数
    //msg:ウィンドウに表示するメッセージ
    public void MessageDisp(string msg)
    {
        if (panelOpenFlag == false)
        {
            //ウィンドウが表示されていないときは表示する
            panelOpenFlag = true;
            messagePanel.SetActive(panelOpenFlag);
        }

        //メッセージを表示する
        messageText.text = msg;
    }

    //メッセージウィンドウにメッセージを表示する関数
    //（既にメッセージが存在するときは前のメッセージの続きに表示する）
    //引数
    //msg:ウィンドウに表示するメッセージ
    public void MessageJoinDisp(string msg)
    {
        if (panelOpenFlag == false)
        {
            //ウィンドウが表示されていないときは表示する
            panelOpenFlag = true;
            messagePanel.SetActive(panelOpenFlag);
        }

        if (messageText.text == "")
        {
            //前のメッセージがない時はそのまま表示する
            messageText.text = msg;
        }
        else
        {
            //前のメッセージがあるときは前のメッセージの後に連結して表示する
            messageText.text = messageText.text + msg;
        }
    }
}
