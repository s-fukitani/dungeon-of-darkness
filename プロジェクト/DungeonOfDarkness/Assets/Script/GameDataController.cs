using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayerClass;
using GameDataClass;
using Common;

//ゲームデータの管理を行うためのクラス
public static class GameDataController
{
    public const int SAVE_MAX = 20;                                     //最大セーブデータ数
    public const int SAVE_PER_PAGE = 10;                                //1ページ毎に表示する最大セーブデータ数
    public const string NO_DATA = "NO DATA";                            //セーブデータファイルがない時に表示する文字列
    private static Player playerData = null;                            //プレイヤーデータ（ロード時およびシーン切り替え時のデータ受け渡しに使用）
    private static bool newgame_flag = true;                            //ニューゲームフラグ（true:ニューゲーム選択時、false:ロードゲーム選択時）
    private static DateTime save_time = DateTime.Now;                   //セーブ日時             
    [HideInInspector] public static GameData savedata = null;           //セーブデータ（セーブデータファイル作成時に使用）
    private static GameData[] gameDataArray = new GameData[SAVE_MAX];   //セーブデータ配列（セーブウィンドウおよびロードウィンドウに一覧を表示する際に使用）
    private static bool load_flag = false;                              //ロードフラグ（true:プレイヤーがダンジョン内にいるデータロードしたとき、false:その他）
                                                                        //※データロード直後に遭遇型イベントが始まらないようにするために使用
    private static int ending_flag = 0;                                 //エンディング分岐フラグ（0のときはエンディングに移行しない）

    //セーブウィンドウおよびロードウィンドウの最大ページ数を算出する関数
    //戻り値（最大ページ数）
    public static int SavePageMaxCalc()
    {
        return SAVE_MAX / SAVE_PER_PAGE;
    }

    //プレイヤーデータを受け取る関数
    //引数
    //player:受け取るプレイヤーデータ
    public static void SetPlayerData(Player player)
    {
        playerData = player;
    }

    //プレイヤーデータを渡す関数
    //戻り値（渡すプレイヤーデータ）
    public static Player GetPlayerData()
    {
        return playerData;
    }

    //プロパティ

    //ニューゲームフラグ
    public static bool newGameFlag
    {
        get
        {
            return newgame_flag;
        }
        set
        {
            newgame_flag = value;
        }
    }

    //セーブ日時
    public static DateTime saveTime
    {
        get
        { 
            return save_time;
        }
        set
        { 
            save_time = value;
        }
    }

    //ロードフラグ
    public static bool loadFlag
    {
        get
        {
            return load_flag;
        }
        set
        {
            load_flag = value;
        }
    }

    //エンディングフラグ
    public static int endingFlag
    {
        get
        {
            return ending_flag;
        }
        set
        {
            ending_flag = value;
        }
    }

    //エンディングフラグを設定する関数
    //引数
    //flag:エンディングフラグ
    public static void SetEndingFlag(int flag)
    {
        ending_flag = flag;
    }

    //データをセーブする関数
    //引数
    //player:プレイヤーデータ
    //num:選択したセーブデータのラベル番号（ラベル番号に1足した番号をデータファイル名に使用）
    public static void DataSave(Player player, int num)
    {
        //セーブデータフォルダパスの作成
        string file_path = Application.dataPath + "/" + GlobalConst.SAVE_DIR;

        //セーブデータフォルダが存在するかをチェックする
        if (System.IO.Directory.Exists(file_path) == false)
        {
            //存在しないときはフォルダを作成する
            System.IO.Directory.CreateDirectory(file_path);
        }

        //ファイルパスの作成
        file_path = file_path + "data" + (num + 1).ToString("D2") + ".dat";

        //セーブ用データの作成
        save_time = DateTime.Now;
        savedata = new GameData(player, newgame_flag, save_time);
        
        //データセーブ実行
        FileStream fileStream = new FileStream(file_path, FileMode.Create);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fileStream, savedata);
        fileStream.Close();
    }

    //セーブデータ配列に一覧用データを格納する関数
    public static void GetSaveDataList()
    {
        //セーブデータ配列の初期化
        gameDataArray = new GameData[SAVE_MAX];
        //セーブデータフォルダに存在する全てのセーブデータファイル情報を格納する
        for (int i = 1; i <= SAVE_MAX; i++)
        {
            //ファイルパスの作成
            string file_path = Application.dataPath + "/" + GlobalConst.SAVE_DIR;
            file_path = file_path + "data" + i.ToString("D2") + ".dat";
            
            if (System.IO.File.Exists(file_path) == true)
            {
                //対象のファイルが存在するとき、ファイルをロードしセーブデータファイル情報を配列に格納
                FileStream fileStream = new FileStream(file_path, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                gameDataArray[i - 1] = (GameData)bf.Deserialize(fileStream);
                fileStream.Close();
            }
        }

    }

    //選択したセーブデータファイルの情報を渡す関数
    //引数
    //num:選択したセーブデータのラベル番号（ラベル番号に1足した番号をデータファイル名に使用）
    public static GameData GetSaveDataInfo(int num)
    {
        //ファイルパスの作成
        string file_path = Application.dataPath + "/" + GlobalConst.SAVE_DIR;
        file_path = file_path + "data" + (num + 1).ToString("D2") + ".dat";

        if (System.IO.File.Exists(file_path) == false)
        {
            //対象のファイルが存在しないときはnullを返す
            return null;
        }
        else
        {
            //対象のファイルがするときは対象のファイル情報を返す
            return gameDataArray[num];
        }
    }

    //選択したセーブデータファイルが存在するかをチェックする関数
    //引数
    //num:選択したセーブデータのラベル番号（ラベル番号に1足した番号をデータファイル名に使用）
    //戻り値（true:存在する、false:存在しない）
    public static bool SaveDataExistCheck(int num)
    {
        //ファイルパスの作成
        string file_path = Application.dataPath + "/" + GlobalConst.SAVE_DIR;
        file_path = file_path + "data" + (num + 1).ToString("D2") + ".dat";
        //ファイルのチェック結果を返す
        return System.IO.File.Exists(file_path);
    }

    //データをロードする関数
    //引数
    //num:選択したセーブデータのラベル番号（ラベル番号に1足した番号をデータファイル名に使用）
    public static void DataLoad(int num)
    {
        //ファイルパスの作成
        string file_path = Application.dataPath + "/" + GlobalConst.SAVE_DIR;
        file_path = file_path + "data" + (num + 1).ToString("D2") + ".dat";

        //データロード実行
        FileStream fileStream = new FileStream(file_path, FileMode.Open);
        BinaryFormatter bf = new BinaryFormatter();
        GameData loaddata = (GameData)bf.Deserialize(fileStream);
        fileStream.Close();

        //シーン移行後にデータを渡す準備を行う
        playerData = loaddata.GetPlayerData();
        newGameFlag = false;

        //シーンの移行
        if (playerData.nowFloor == -1)
        {
            //街の入口
            SceneManager.LoadScene("CityScene");
        }
        else
        {
            //ダンジョン内
            load_flag = true;
            SceneManager.LoadScene("DungeonScene");
        }

    }

    #region デバッグ
    //----------------------------デバッグ用関数---------------------------------

    //データをロードする関数（テスト用）
    public static Player DataLoadTest()
    {
        string file_path = Application.dataPath + "/" + GlobalConst.SAVE_DIR;
        file_path = file_path + "data01.dat";
        Player pl;

        FileStream fileStream = new FileStream(file_path, FileMode.Open);
        BinaryFormatter bf = new BinaryFormatter();

        GameData loaddata = (GameData)bf.Deserialize(fileStream);
        fileStream.Close();
        pl = loaddata.GetPlayerData();

        return pl;

    }
    #endregion

}