using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//共通で使用する定数および関数
namespace Common
{
    public static class GlobalConst
    {
        //定数
        public const string SAVE_DIR = "savedata/";         //セーブデータフォルダ
        public const string IMG_DIR = "img/";               //画像ファイルフォルダ
        public const string DATA_DIR = "data/";             //データファイルフォルダ
        public const string IMG_UPSTAIRS = "upstairs";      //上り階段画像ファイル
        public const string IMG_DOWNSTAIRS = "downstairs";  //下り階段画像ファイル
        public const string GET_STRING = "get_";            //アイテム取得時に表示させるアイテム画像名に付け足す文字列
        public const string GOLD_STRING = "gold";           //ゴールド取得時に表示させる画像名に付け足す文字列
        public const int FLOOR_MAX = 8;                     //ダンジョンの最大階層

        //現在フロアを文字列化する関数（フォーマットは"00"）
        //引数
        //floor:現在フロア
        //戻り値（文字列化した現在フロア）
        public static string GetFloorString(int floor)
        {
            return floor.ToString("D2");
        }
    }

    public static class CommonMethod
    {
        //待機用のタイマー用変数
        private static bool timerInitFlag = false;      //タイマー初期化フラグ（true:初期化済み、false:未初期化）
        private static float waitTimer = 0.0f;          //待機用タイマー（秒）

        //待機用のタイマーの関数
        //引数
        //sec:待機時間（秒）
        //戻り値（true:指定時間に達している、false:指定時間に達していない）
        public static bool TimeWait(float sec)
        {
            if (timerInitFlag == false)
            {
                //タイマーの初期化
                waitTimer = 0.0f;
                timerInitFlag = true;
                return false;
            }
            else
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= sec)
                {
                    //指定時間に達したら、初期化フラグをオフにする
                    timerInitFlag = false;
                    return true;
                }
                return false;
            }
        }

        //ゲームオブジェクト配列のソートを行うメソッド
        //引数
        //gameObjects:ソート対象のゲームオブジェクト配列
        //戻り値（ソート後のゲームオブジェクト配列）
        public static GameObject[] ListSort(GameObject[] gameObjects)
        {
            var results = new List<GameObject>();
            results.AddRange(gameObjects);
            results.Sort((g1, g2) => g2.transform.position.sqrMagnitude.CompareTo(g1.transform.position.sqrMagnitude));

            return results.ToArray();
        }
    }
}
