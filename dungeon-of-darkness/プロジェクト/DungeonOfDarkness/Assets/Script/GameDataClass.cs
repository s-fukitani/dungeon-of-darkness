using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using PlayerClass;
using Common;

//ゲームデータ関連のスペース
namespace GameDataClass
{
    //ゲームデータクラス
    [System.Serializable]
    public class GameData
    {
        //フィールド

        //プレイヤーデータ以外
        [SerializeField] private bool newgame_flag;                     //ニューゲームフラグ（true:ニューゲームで始めた直後、false:その他）
        [SerializeField] private DateTime save_time;                    //セーブ日時

        //プレイヤーデータ
        [SerializeField] private string name;                           //名前
        [SerializeField] private int lv;                                //レベル
        [SerializeField] private int hp;                                //HP
        [SerializeField] private int hpMax;                             //最大HP
        [SerializeField] private int exp;                               //経験値
        [SerializeField] private int gold;                              //ゴールド
        [SerializeField] private Player.PLAYER_CONDITION condition;     //状態
        [SerializeField] private int attack;                            //攻撃力
        [SerializeField] private int defense;                           //防御力
        [SerializeField] private int speed;                             //素早さ
        [SerializeField] private int luck;                              //運
        [SerializeField] private int karma;                             //カルマ
        [SerializeField] private int now_floor;                         //現在フロア
        [SerializeField] private int reach_floor;                       //プレイヤーがこれまでに訪れた最下フロア（初期値は0）
        [SerializeField] private int now_x;                             //現在のX座標
        [SerializeField] private int now_z;                             //現在のZ座標
        [SerializeField] private Player.DIRECTION now_direction;        //現在向いている方向
        [SerializeField] private int[] equip_array;                     //装備アイテム配列
        [SerializeField] private List<ItemBox> item_box;                //所持アイテムリスト
        [SerializeField] private List<List<List<int>>> event_map_flag;  //ダンジョンにおけるイベントの進行を表すフラグのリスト
        [SerializeField] private int[] city_event_flag;                 //街内のイベントフラグ配列

        //コンストラクタ
        public GameData()
        {
            //フィールドの初期化
            name = "";
            lv = 1;
            hpMax = 0;
            hp = hpMax;
            exp = 0;
            gold = 0;
            condition = Player.PLAYER_CONDITION.OK;
            attack = 0;
            defense = 0;
            speed = 0;
            luck = 0;
            karma = 0;
            now_floor = -1;
            reach_floor = 0;
            now_x = 0;
            now_z = 0;
            now_direction = Player.DIRECTION.NORTH;

            equip_array = new int[Player.EQUIP_MAX];

            for (int i = 0; i < Player.EQUIP_MAX; i++)
            {
                equip_array[i] = 0;
            }

            item_box = new List<ItemBox>();

            event_map_flag = new List<List<List<int>>>();

            city_event_flag = new int[(int)EventController.CITY_EV_FLAG.EV_MAX];

            for (int i = 0; i < city_event_flag.Length; i++)
            {
                city_event_flag[i] = 0;
            }

            newgame_flag = true;
            save_time = DateTime.MinValue;
        }

        //コンストラクタ（引数あり）
        //引数
        //player:プレイヤーデータ
        //n_game:ニューゲームフラグ
        //s_time:セーブ日時
        public GameData(Player player, bool n_game, DateTime s_time)
        {
            name = player.playerName;
            lv = player.playerLv;
            hpMax = player.playerHpMax;
            hp = player.playerHp;
            exp = player.playerExp;
            gold = player.playerGold;
            condition = player.playerCondition;
            attack = player.playerAttack;
            defense = player.playerDefense;
            speed = player.playerSpeed;
            luck = player.playerLuck;
            karma = player.playerKarma;
            now_floor = player.nowFloor;
            reach_floor = player.reachFloor;
            now_x = player.nowPosX;
            now_z = player.nowPosZ;
            now_direction = player.nowDirection;

            equip_array = new int[Player.EQUIP_MAX];

            for (int i = 0; i < Player.EQUIP_MAX; i++)
            {
                equip_array[i] = player.equipArray[i];
            }

            item_box = new List<ItemBox>();
            item_box = player.GetItemBox();

            event_map_flag = new List<List<List<int>>>();
            List<List<List<int>>> flag_list = player.GetEventMapFlagList();

            for (int f = 0; f < flag_list.Count; f++)
            {
                event_map_flag.Add(new List<List<int>>());
                for (int z = 0; z < flag_list[0].Count; z++)
                {
                    event_map_flag[f].Add(new List<int>());
                    for (int x = 0; x < flag_list[0][0].Count; x++)
                    {
                        event_map_flag[f][z].Add(flag_list[f][z][x]);
                    }
                }
            }

            city_event_flag = new int[(int)EventController.CITY_EV_FLAG.EV_MAX];

            for (int i = 0; i < city_event_flag.Length; i++)
            {
                city_event_flag[i] = player.cityEventFlag[i];
            }

            newgame_flag = n_game;
            save_time = s_time;
        }

        //プロパティ

        //名前
        public string playerName
        {
            set { name = value; }
            get { return name; }
        }

        //レベル
        public int playerLv
        {
            set { lv = value; }
            get { return lv; }
        }

        //HP
        public int playerHp
        {
            set { hp = value; }
            get { return hp; }
        }

        //最大HP
        public int playerHpMax
        {
            set { hpMax = value; }
            get { return hpMax; }
        }

        //攻撃力
        public int playerAttack
        {
            set { attack = value; }
            get { return attack; }
        }

        //防御力
        public int playerDefense
        {
            set { defense = value; }
            get { return defense; }
        }

        //素早さ
        public int playerSpeed
        {
            set { speed = value; }
            get { return speed; }
        }

        //運
        public int playerLuck
        {
            set { luck = value; }
            get { return luck; }
        }

        //カルマ
        public int playerKarma
        {
            set { karma = value; }
            get { return karma; }
        }

        //現在フロア
        public int nowFloor
        {
            set { now_floor = value; }
            get { return now_floor; }
        }

        //プレイヤーがこれまでに訪れた最下フロア
        public int reachFloor
        {
            set { reach_floor = value; }
            get { return reach_floor; }
        }

        //現在X座標
        public int nowPosX
        {
            set { now_x = value; }
            get { return now_x; }
        }

        //現在Z座標
        public int nowPosZ
        {
            set { now_z = value; }
            get { return now_z; }
        }

        //現在向いている方向
        public Player.DIRECTION nowDirection
        {
            set { now_direction = value; }
            get { return now_direction; }
        }

        //状態
        public Player.PLAYER_CONDITION playerCondition
        {
            set { condition = value; }
            get { return condition; }
        }

        //経験値
        public int playerExp
        {
            set { exp = value; }
            get { return exp; }
        }

        //ゴールド
        public int playerGold
        {
            set { gold = value; }
            get { return gold; }
        }

        //装備アイテム配列
        public int[] equipArray
        {
            set { equip_array = value; }
            get { return equip_array; }
        }

        //街のイベントフラグ
        public int[] cityEventFlag
        {
            set { city_event_flag = value; }
            get { return city_event_flag; }
        }

        //ダンジョンにおけるイベントの進行を表すフラグのリストを返すメソッド
        //戻り値（ダンジョンにおけるイベントの進行を表すフラグのリスト）
        public List<List<List<int>>> GetEventMapFlagList()
        {
            return event_map_flag;
        }

        //指定場所のダンジョンにおけるイベントの進行を表すフラグを返すメソッド
        //引数
        //f:現在フロア
        //z:Z座標
        //x:X座標
        //戻り値（指定場所のダンジョンにおけるイベントの進行を表すフラグ）
        public int GetEventMapFlag(int f, int z, int x)
        {
            return event_map_flag[f][z][x];
        }

        //街のイベントの進行を表すフラグの配列を返すメソッド
        //戻り値（街のイベントの進行を表すフラグの配列）
        public int[] GetCityEventFlagArray()
        {
            return city_event_flag;
        }

        //指定した街のイベントの進行を表すフラグを返すメソッド
        //引数
        //c_ev:指定対象のイベントフラグの要素番号（街のイベントフラグの列挙型を使用）
        //戻り値（指定した街のイベントの進行を表すフラグ）
        public int GetCityEventFlag(EventController.CITY_EV_FLAG c_ev)
        {
            return city_event_flag[(int)c_ev];
        }

        //セーブウィンドウに表示するセーブデータのレベルを返すメソッド
        //戻り値（表示するレベル、数値のフォーマットは"00"にする）
        public string GetLevelString()
        {
            string str = "";
            str = "LV:" + lv.ToString("D2");

            return str;
        }

        //セーブウィンドウに表示するセーブデータの現在フロアを返すメソッド
        //戻り値（表示する現在フロア、数値のフォーマットは"00"にする）
        public string GetNowFloorString()
        {
            string str = "";
            if (now_floor == -1)
            {
                //現在フロアが-1の時は「街の入口」を返す
                str = "街の入口";
            }
            else if (now_floor == GlobalConst.FLOOR_MAX)
            {
                //最終フロアの時
                str = "？？？？";
            }
            else
            {
                //その他のフロアの時
                str = "地下" + nowFloor.ToString("D2") + "階";
            }

            return str;
        }

        //セーブウィンドウに表示するセーブ日時を文字列化して返すメソッド
        //戻り値（表示するセーブ日時）
        public string GetSaveTimeString()
        {
            return save_time.ToString();
        }

        //プレイヤーデータを返すメソッド
        //戻り値（プレイヤーデータ）
        public Player GetPlayerData()
        {
            Player pl = new Player();
            pl.playerName = name;
            pl.playerLv = lv;
            pl.playerHpMax = hpMax;
            pl.playerHp = hp;
            pl.playerExp = exp;
            pl.playerGold = gold;
            pl.playerCondition = condition;
            pl.playerAttack = attack;
            pl.playerDefense = defense;
            pl.playerSpeed = speed;
            pl.playerLuck = luck;
            pl.playerKarma = karma;
            pl.nowFloor = now_floor;
            pl.reachFloor = reach_floor;
            pl.nowPosX = now_x;
            pl.nowPosZ = now_z;
            pl.nowDirection = now_direction;

            for (int i = 0; i < Player.EQUIP_MAX; i++)
            {
                pl.equipArray[i] = equip_array[i];
            }

            pl.SetItemBox(item_box);
            pl.SetEventMapFlagList(event_map_flag);
            pl.SetCityEventFlagArray(city_event_flag);

            return pl;

        }
    }
}
