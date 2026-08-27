using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//酒場関連のネームスペース
namespace BarClass
{
    //酒場のイベントデータの見出しの列挙型
    public enum BAR_HEADING
    {
        FLOOR,      //到達フロア
        INFO        //情報
    }

    //酒場クラス
    public class Bar
    {
        //フィールド
        private int floor;      //酒場到達フロア
        private string info;    //酒場情報
        private bool flag;      //酒場情報フラグ（true:表示、falase:非表示）

        //コンストラクタ（初期化用）
        public Bar()
        {
            floor = 0;
            info = "";
            flag = true;
        }

        //コンストラクタ
        public Bar(int floor, string info)
        {
            this.floor = floor;
            this.info = info;
            this.flag = true;
        }

        //プロパティ

        //酒場到達フロア
        public int barFloor
        {
            get
            {
                return floor;
            }

            set
            {
                floor = value;
            }
        }

        //酒場情報
        public string barInfo
        {
            get
            {
                return info;
            }

            set
            {
                info = value;
            }
        }


        public bool barFlag
        {
            get
            {
                return flag;
            }

            set
            {
                flag = value;
            }
        }

        //酒場情報フラグの変更を行うメソッド
        //引数
        //flag:変更後のフラグ
        public void BarFlagChange(bool flag)
        {
            this.flag = flag;
        }
    }
}
