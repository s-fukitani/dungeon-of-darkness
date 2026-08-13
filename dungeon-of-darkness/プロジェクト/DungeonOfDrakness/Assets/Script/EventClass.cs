using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//イベント関連のネームスペース
namespace EventClass
{
    //ダンジョン内のイベントクラス
    public class EventObject
    {
        //フィールド
        private EventController.EVTYPE type;                //イベントの種類
        private int number;                                 //イベント番号
        private int flag;                                   //イベントの進行状況を示すフラグ
        private EventController.EVACTIVATION activation;    //イベントの発動条件
        private bool dark;                                  //ダークゾーンフラグ（true:ダークゾーン、false:ダークゾーンでない）

        //コンストラクタ
        public EventObject()
        {
            type = EventController.EVTYPE.NOTHING;
            number = 0;
            flag = 0;
            activation = EventController.EVACTIVATION.NOTHING;
            dark = false;
        }

        //プロパティ

        //イベントの種類
        public EventController.EVTYPE eventType
        {
            set { type = value; }
            get { return type; }
        }

        //イベント番号
        public int eventNumber
        {
            set { number = value; }
            get { return number; }
        }

        //イベントの進行状況を示すフラグ
        public int eventFlag
        {
            set { flag = value; }
            get { return flag; }
        }

        //イベントの発動条件
        public EventController.EVACTIVATION evActivation
        {
            set { activation = value; }
            get { return activation; }
        }

        //ダークゾーンの有無
        public bool darkZoneFlag
        {
            set { dark = value; }
            get { return dark; }
        }
    }
}
