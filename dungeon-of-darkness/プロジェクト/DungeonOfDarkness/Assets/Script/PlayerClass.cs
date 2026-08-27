using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Common;
using ItemClass;
using EnemyClass;
using EventClass;

//プレイヤー関連のネームスペース
namespace PlayerClass
{
    //プレイヤーのレベルアップに関するクラス
    public class LevelUpParam
    {
        //レベルアップの項目に関する列挙型
        public enum LEVELUP_HEADER
        {
            NEED_EXP,   //次のレベルに必要な経験値
            UP_HPMAX,   //上がる最大HP
            UP_ATTACK,  //上がる攻撃力
            UP_DEFENSE, //上がる防御力
            UP_SPEED,   //上がる素早さ
            UP_LUCK     //上がる運
        }

        //定数
        public const int LEVEL_MAX = 30;       //最大レベル

        //フィールド
        private int need_exp;   //次のレベルに必要な経験値
        private int up_hp_max;  //上がる最大HP
        private int up_attack;  //上がる攻撃力
        private int up_defense; //上がる防御力
        private int up_speed;   //上がる素早さ
        private int up_luck;    //上がる運

        //コンストラクタ（テスト用）
        public LevelUpParam()
        {
            need_exp = 0;
            up_hp_max = 0;
            up_attack = 0;
            up_defense = 0;
            up_defense = 0;
        }

        //コンストラクタ
        public LevelUpParam(int need_exp, int up_hp_max, int up_attack, int up_defense, int up_speed, int up_luck)
        {
            this.need_exp = need_exp;
            this.up_hp_max = up_hp_max;
            this.up_attack = up_attack;
            this.up_defense = up_defense;
            this.up_speed = up_speed;
            this.up_luck = up_luck;
        }

        //プロパティ

        //次のレベルに必要な経験値
        public int needExp
        {
            get
            {
                return need_exp;
            }

            set
            {
                need_exp = value;
            }
        }

        //上がる最大HP
        public int upHpMax
        {
            get
            {
                return up_hp_max;
            }

            set
            {
                up_hp_max = value;
            }
        }

        //上がる攻撃力
        public int upAttack
        {
            get
            {
                return up_attack;
            }

            set
            {
                up_attack = value;
            }
        }

        //上がる防御力
        public int upDefense
        {
            get
            {
                return up_defense;
            }

            set
            {
                up_defense = value;
            }
        }

        //上がる素早さ
        public int upSpeed
        {
            get
            {
                return up_speed;
            }

            set
            {
                up_speed = value;
            }
        }

        //上がる運
        public int upLuck
        {
            get
            {
                return up_luck;
            }

            set
            {
                up_luck = value;
            }
        }
    }

    //プレイヤークラス
    public class Player
    {
        //方角に関する列挙型
        public enum DIRECTION
        {
            NORTH,  //北
            EAST,   //東
            SOUTH,  //南
            WEST    //西
        }

        //プレイヤーの状態に関する列挙型
        public enum PLAYER_CONDITION
        {
            OK,         //OK
            SLEEP,      //睡眠状態
            SEALED,     //封印状態
            POISON,     //毒状態
            BLESSING    //祝福状態
        }

        //購入の成否を表す列挙型
        public enum BUY_COMPLETION 
        {
            OK,             //購入成功
            NG_MONEY,       //資金不足
            NG_BOX_MAX,     //アイテム満タン
            NG_ITEM_MAX     //購入対象の使い捨てアイテム満タン
        }

        //取得の成否を表す列挙型
        public enum PICK_COMPLETION
        {
            OK,             //取得成功
            NG_BOX_MAX,     //アイテム満タン
            NG_ITEM_MAX     //取得対象の使い捨てアイテム満タン
        }

        //定数
        private const int GOLD_MAX = 99999;    //ゴールドの最大値
        public const int EQUIP_MAX = 4;        //装備の種類数

        //アイテム所持数の最大値（デバッグ時は60、本番時は40に設定）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const int ITEM_MAX = 60;
#endif
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        public const int ITEM_MAX = 40;
#endif
        public const int ITEM_PER_PAGE = 20;   //1ページごとのアイテム数
        public const int NORMAL_ITEM_MAX = 9;   //一回使い切りアイテム保有最大値

        public const int EFFECT_PAR_MAX = 100;          //敵を状態異常にする確率の計算に使用する乱数の最大値
        private const int SLEEP_PAR_BASE = 80;          //敵を睡眠状態にする確率の計算に使用する基本値（プレイヤーの運で加算した後、敵の運を減算して使用）
        private const int SEALED_PAR_BASE = 65;         //敵を封印状態にする確率の計算に使用する基本値（プレイヤーの運で加算した後、敵の運を減算して使用）
        private const int POISON_PAR_BASE = 50;         //敵を毒状態にする確率の計算に使用する基本値（プレイヤーの運で加算した後、敵の運を減算して使用）
        private const int DEATH_PAR_BASE = 30;          //敵を即死させる確率の計算に使用する基本値（プレイヤーの運で加算した後、敵の運を減算して使用）
        private const int POISON_ATTACK_PAR_BASE = 25;  //敵を攻撃で毒状態にする確率の計算に使用する基本値（プレイヤーの運で加算した後、敵の運を減算して使用）

        public const int RECOVER_PAR_MAX = 100;             //状態異常から回復する確率の計算に使用する乱数の最大値
        private const int RECOVER_SLEEP = 20;               //睡眠状態から回復する確率の計算に使用する基本値
        private const int RECOVER_SEALED = 15;              //封印状態から回復する確率の計算に使用する基本値
        private const int RECOVER_SLEEP_CORRECTION = 8;     //睡眠状態から回復する確率の計算に使用する補正値（経過ターン数に乗算する）
        private const int RECOVER_SEALED_CORRECTION = 5;    //封印状態から回復する確率の計算に使用する補正値（経過ターン数に乗算する）
        private const int RECOVER_LUCK_BASE = 10;       //レベル1の時の運
        private const float LUCK_UP_AVG = 2.69f;        //レベルが1上がるごとにアップする運の平均値　※(最大レベルの時の運 - レベル1の時の運) ÷ (最大レベル - 1)で算出　
        private const int RECOVER_PAR_MIN = 5;          //状態異常から回復する確率の下限値 
        private const int RECOVER_PAR_LIMIT = 90;       //状態異常から回復する確率の上限値 

        private const int POISON_DAMAGE = 20;   //戦闘中の毒によるダメージ（１ターンごとに最大HPをこの定数で割った数だけダメージを受ける）

        private const int BLESSING_HEAL = 1;        //祝福により1歩移動するごとに回復するHPの数値
        private const int BLESSING_HEAL_FIGHT = 15; //戦闘中の祝福によるHP回復量（１ターンごとに最大HPをこの定数で割った数だけ回復する）

        private const int ESCAPE_BASE = 35;  //逃走に成功する確率の計算に使用する乱数の基本値
        private const int ESCAPE_MIN = 5;    //逃走に成功する確率の計算に使用する乱数の最小値
        private const int ESCAPE_MAX = 100;  //逃走に成功する確率の計算に使用する乱数の最大値

        private const int CRITICAL_PAR_BASE = 6;     //会心の一撃が出る確率の計算に使用する基本値
        private const int CRITICAL_PAR_MAX = 100;    //会心の一撃が出る確率の計算に使用する乱数の最大値

        private const int AVOID_BASE_MIN = 5;   //攻撃回避の確率の計算に使用する基本値（最小）
        private const int AVOID_BASE_MAX = 60;  //攻撃回避の確率の計算に使用する基本値（最大）
        private const int AVOID_MAX = 100;      //攻撃回避の確率の計算に使用する最大値

        private const int KARMA_RATE = 8000;    //カルマを1減少させるのに必要なゴールド

        //フィールド
        private string name;                            //名前
        private int lv;                                 //レベル
        private int hp;                                 //HP
        private int hpMax;                              //最大HP
        private int exp;                                //経験値
        private int gold;                               //ゴールド
        private PLAYER_CONDITION condition;             //状態
        private int attack;                             //攻撃力
        private int defense;                            //防御力
        private int speed;                              //素早さ
        private int luck;                               //運
        private int karma;                              //カルマ
        private int now_floor;                          //現在フロア（街の入口は-1）
        private int reach_floor;                        //プレイヤーがこれまでに訪れた最下フロア（初期値は0）
        private int now_x;                              //現在位置（x座標）
        private int now_z;                              //現在位置（z座標）
        private DIRECTION now_direction;                //現在向いている方角
        private int ab_condition_turn;                  //状態異常になってから経過したターン
        private int escape_count;                       //戦闘時の逃走回数
        private LevelUpParam[] levelUpParam;            //レベルアップ情報
        private int[] equip_array;                      //装備アイテム配列
        private List<ItemBox> item_box;                 //所持アイテムリスト
        private List<List<List<int>>> event_map_flag;   //ダンジョンにおけるイベントの進行を表すフラグのリスト
        private int[] city_event_flag;                  //街内のイベントフラグ配列


        //コンストラクタ（テスト用）
        public Player()
        {
            name = "主人公";
            lv = 1;

            
            hpMax = 25;
            hp = hpMax;
            exp = 0;
            gold = 0;
            condition = PLAYER_CONDITION.OK;
            attack = 10;
            defense = 10;
            speed = 10;
            luck = 10;
            
            /*
            hpMax = 200;
            hp = hpMax;
            exp = 0;
            gold = 0;
            condition = PLAYER_CONDITION.OK;
            attack = 80;
            defense = 80;
            speed = 60;
            luck = 60;
            */


            karma = 0;
            now_floor = -1;     //町の入口に設定する
            reach_floor = 0;
            now_x = 0;
            now_z = 0;
            now_direction = DIRECTION.NORTH;
            ab_condition_turn = 0;
            escape_count = 0;

            equip_array = new int[EQUIP_MAX];

            for (int i = 0; i < EQUIP_MAX; i++)
            {
                equip_array[i] = 0;
            }

            item_box = new List<ItemBox>();

            levelUpParam = new LevelUpParam[LevelUpParam.LEVEL_MAX - 1];

            for (int i = 0; i < LevelUpParam.LEVEL_MAX - 1; i++)
            {
                levelUpParam[i] = new LevelUpParam();
            }

            TextAsset csvLevelUpFile;    // CSVファイル
            List<string[]> levelUpDatas; // CSVの中身を入れるリスト

            levelUpDatas = new List<string[]>();

            csvLevelUpFile = Resources.Load(GlobalConst.DATA_DIR + "levelupFile") as TextAsset;
            StringReader readerLevelUp = new StringReader(csvLevelUpFile.text);

            while (readerLevelUp.Peek() != -1)
            {
                string line = readerLevelUp.ReadLine();
                levelUpDatas.Add(line.Split(','));
            }

            for (int i = 0; i < levelUpDatas.Count; i++)
            {
                LevelUPParamSetFromFile(i, levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.NEED_EXP],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_HPMAX],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_ATTACK],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_DEFENSE],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_SPEED],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_LUCK]);
            }

            event_map_flag = new List<List<List<int>>>();

            city_event_flag = new int[(int)EventController.CITY_EV_FLAG.EV_MAX];

            
        }

        //コンストラクタ
        public Player(string name)
        {
            this.name = name;
            lv = 1;
            hpMax = 25;
            hp = hpMax;
            exp = 0;
            gold = 0;
            condition = PLAYER_CONDITION.OK;
            attack = 10;
            defense = 10;
            speed = 10;
            luck = 10;
            karma = 0;
            now_floor = -1;     //町の入口に設定する
            reach_floor = 0;
            now_x = 0;
            now_z = 0;
            now_direction = DIRECTION.NORTH;
            ab_condition_turn = 0;
            escape_count = 0;

            //装備配列の初期化
            equip_array = new int[EQUIP_MAX];

            //全ての装備欄を未装備にする
            for (int i = 0; i < EQUIP_MAX; i++)
            {
                equip_array[i] = 0;
            }

            //所持アイテムリストの初期化
            item_box = new List<ItemBox>();

            //レベルアップ情報配列の初期化
            levelUpParam = new LevelUpParam[LevelUpParam.LEVEL_MAX - 1];
            for (int i = 0; i < LevelUpParam.LEVEL_MAX - 1; i++)
            {
                levelUpParam[i] = new LevelUpParam();
            }

            TextAsset csvLevelUpFile;       //レベルアップデータのCSVファイル
            List<string[]> levelUpDatas;    //レベルアップデータのCSVの中身を入れるリスト

            //レベルアップデータのCSVの中身を入れるリストの初期化
            levelUpDatas = new List<string[]>();

            //レベルアップデータのCSVファイルの読込
            csvLevelUpFile = Resources.Load(GlobalConst.DATA_DIR + "levelupFile") as TextAsset;
            StringReader readerLevelUp = new StringReader(csvLevelUpFile.text);

            //ファイルの中身をリストに入れる
            while (readerLevelUp.Peek() != -1)
            {
                string line = readerLevelUp.ReadLine();
                levelUpDatas.Add(line.Split(','));
            }

            //リストのデータをレベルアップ情報配列に入れる
            for (int i = 0; i < levelUpDatas.Count; i++)
            {
                LevelUPParamSetFromFile(i, levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.NEED_EXP],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_HPMAX],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_ATTACK],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_DEFENSE],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_SPEED],
                                               levelUpDatas[i][(int)LevelUpParam.LEVELUP_HEADER.UP_LUCK]);
            }

            //ダンジョンにおけるイベントの進行を表すフラグのリスト
            event_map_flag = new List<List<List<int>>>();

            //街におけるイベントの進行を表すフラグの配列の初期化
            city_event_flag = new int[(int)EventController.CITY_EV_FLAG.EV_MAX];

            for (int i = 0; i < city_event_flag.Length; i++)
            {
                city_event_flag[i] = 0;
            }

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

        //プレイヤーが訪れた最下フロア
        public int reachFloor
        {
            set { reach_floor = value; }
            get { return reach_floor; }
        }

        //現在位置（x座標）
        public int nowPosX
        {
            set { now_x = value; }
            get { return now_x; }
        }

        //現在位置（z座標）
        public int nowPosZ
        {
            set { now_z = value; }
            get { return now_z; }
        }

        //現在向いている方角
        public DIRECTION nowDirection
        {
            set { now_direction = value; }
            get { return now_direction; }
        }

        //状態
        public PLAYER_CONDITION playerCondition
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

        //装備
        public int[] equipArray
        {
            set { equip_array = value; }
            get { return equip_array; }
        }

        //レベルアップ情報
        public LevelUpParam[] lvUpParams
        {
            set { levelUpParam = value; }
            get { return levelUpParam; }
        }

        //逃走回数
        public int escapeCount
        {
            set { escape_count = value; }
            get { return escape_count; }
        }

        //街のイベントフラグ
        public int[] cityEventFlag
        {
            set { city_event_flag = value; }
            get { return city_event_flag; }
        }


        //ダンジョンのイベント情報リストからダンジョンにおけるイベントの進行を表すフラグのリストにデータを渡すメソッド
        //引数
        //evdata:ダンジョンのイベント情報リスト
        public void SetEventFlag(List<List<List<EventObject>>> evdata)
        {
            event_map_flag = new List<List<List<int>>>();

            for (int f = 0; f < evdata.Count; f++)
            {
                event_map_flag.Add(new List<List<int>>());
                for (int z = 0; z < evdata[0].Count; z++)
                {
                    event_map_flag[f].Add(new List<int>());
                    for (int x = 0; x < evdata[0][0].Count; x++)
                    {
                        event_map_flag[f][z].Add(evdata[f][z][x].eventFlag);
                    }
            }
                }
        }

        //ダンジョンにおけるイベントの進行を表すフラグのリストを返すメソッド
        //戻り値（ダンジョンにおけるイベントの進行を表すフラグのリスト）
        public List<List<List<int>>> GetEventMapFlagList()
        {
            return event_map_flag;
        }

        //ダンジョンにおけるイベントの進行を表すフラグのリストにデータを設定するメソッド
        //引数
        //map_flag:イベントの進行を表すフラグのリスト
        public void SetEventMapFlagList(List<List<List<int>>> map_flag)
        {
            event_map_flag = map_flag;
        }

        //指定場所のダンジョンにおけるイベントの進行を表すフラグを返すメソッド
        //引数
        //f:現在フロア
        //z:Z座標
        //x:X座標
        //戻り値（対象場所のダンジョンにおけるイベントの進行を表すフラグ）
        public int GetEventMapFlag(int f, int z, int x)
        {
            return event_map_flag[f][z][x];
        }

        //街におけるイベントの進行を表すフラグの配列を返すメソッド
        //戻り値（街におけるイベントの進行を表すフラグの配列）
        public int[] GetCityEventFlagArray()
        {
            return city_event_flag;
        }

        //街におけるイベントの進行を表すフラグの配列にデータを設定するメソッド
        //引数
        //city_flag:イベントの進行を表すフラグの配列
        public void SetCityEventFlagArray(int[] city_flag)
        {
            for (int i = 0; i < city_event_flag.Length; i++)
            {
                city_event_flag[i] = city_flag[i];
            }
        }

        //指定した街のイベントの進行を表すフラグを返すメソッド
        //引数
        //c_ev:指定対象のイベントフラグの要素番号（街のイベントフラグの列挙型を使用）
        //戻り値（指定した街のイベントの進行を表すフラグ）
        public int GetCityEventFlag(EventController.CITY_EV_FLAG c_ev)
        {
            return city_event_flag[(int)c_ev];
        }

        //所持アイテムリストの指定されたアイテム情報を返すメソッド
        //引数
        //index:所持アイテムリストの番号
        //戻り値（番号で指定された所持アイテムの情報）
        public ItemBox GetItemBoxIndex(int index)
        {
            return item_box[index];
        }

        //現在のフロアを変更するメソッド
        //引数
        //f:変更先フロア
        public void NowFloorChange(int f)
        {
            now_floor = f;

            //現在フロアがプレイヤーがこれまでに訪れた最下フロアを超えた場合
            if (now_floor > reach_floor)
            {
                //最下フロアの更新を行う
                reach_floor = now_floor;
            }
        }

        //現在の位置を変更するメソッド
        //引数
        //x:変更先X座標
        //z:変更先Z座標
        public void NowPositionSet(int x, int z)
        {
            now_x = x;
            now_z = z;
        }

        //現在向いている方角を変更するメソッド
        //引数
        //dir:変更先方角
        public void NowDirectionSet(DIRECTION dir)
        {
            now_direction = dir;
        }

        //フロアを1階上下するメソッド
        //引数
        //flag:上下フラグ、trueが上り、falseが下り
        public void FloorUpDown(bool flag)
        {
            if (flag == true)
            {
                //上りの時
                if (now_floor <= 0)
                {
                    //地下1階より上の時は何もしない
                    return;
                }
                //現在フロアを上げる
                now_floor--;
            }
            else
            {
                //下りの時
                if (now_floor >= GlobalConst.FLOOR_MAX)
                {
                    //最大フロアをオーバーしたときは何もしない
                    return;
                }
                //現在フロアを下げる
                now_floor++;

                //現在フロアがプレイヤーがこれまでに訪れた最下フロアを超えた場合
                if (now_floor > reach_floor)
                {
                    //最下フロアの更新を行う
                    reach_floor = now_floor;
                }
            }

        }

        //所持アイテムリストを返すメソッド
        //戻り値（所持アイテムリスト）
        public List<ItemBox> GetItemBox()
        {
            return item_box;
        }

        //所持アイテムリストのデータを受け取るメソッド
        //引数
        //box:所持アイテムのデータを渡すリスト
        public void SetItemBox(List<ItemBox> box)
        {
            item_box = box;
        }

        //所持アイテムリスト内にあるアイテム数を返すメソッド
        //戻り値（所持アイテムリスト内にあるアイテム数）
        public int GetItemBoxCount()
        {
            return item_box.Count;
        }

        //指定されている武器防具が全て装備されているかをチェックするメソッド
        //引数
        //weapon:指定された武器ID
        //armor:指定された鎧ID
        //shield:指定された盾ID
        //helm:指定された兜ID
        //戻り値（true:装備されている、false:装備されていない）
        public bool AllEquipCheck(int weapon, int armor, int shield, int helm)
        {
            if (equip_array[(int)(ItemGenerator.ITEM_TYPE.WEAPON) - 2] == weapon && 
                equip_array[(int)(ItemGenerator.ITEM_TYPE.ARMOR) - 2] == armor &&
                equip_array[(int)(ItemGenerator.ITEM_TYPE.SHIELD) - 2] == shield && 
                equip_array[(int)(ItemGenerator.ITEM_TYPE.HELM) - 2] == helm)
            {
                return true;
            }

            return false;
        }

        //カルマの増減を行うメソッド
        //引数
        //karma:増減するカルマの数値（増やすときは正の数を、減らすときは負の数を入れる）
        public void KarmaAddSub(int karma)
        {
            this.karma += karma;

            //カルマが0未満の時は0にする
            if (this.karma < 0)
            {
                this.karma = 0;
            }
        }

        //カルマの初期化を行うメソッド
        public void KarmaInit()
        {
            this.karma = 0;
        }

        //カルマを0にするのに必要なゴールドを算出するメソッド
        public int GetKarmaGold()
        {
            return this.karma * KARMA_RATE;
        }

        //カルマを1減らすのに必要なゴールドを取得するメソッド
        public int GetOneKarmaGold()
        {
            return KARMA_RATE;
        }

        //カルマを0にするのに必要なゴールドが所持ゴールド以上かをチェックするメソッド
        //戻り値（true:所持ゴールド以上、false:所持ゴールド未満）
        public bool CheckKarmaGold()
        {
            if (this.gold - this.GetKarmaGold() >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //カルマを1減らすのに必要なゴールドが所持ゴールド以上かをチェックするメソッド
        //戻り値（true:所持ゴールド以上、false:所持ゴールド未満）
        public bool CheckOneKarmaGold()
        {
            if (this.gold - KARMA_RATE >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //状態名のテキストを返すメソッド
        //戻り値（状態名のテキスト）
        public string ConditionTextGet()
        {
            if (condition == PLAYER_CONDITION.OK)
            {
                return "OK";
            }
            else if (condition == PLAYER_CONDITION.SLEEP)
            {
                return "眠り";
            }
            else if (condition == PLAYER_CONDITION.SEALED)
            {
                return "封印";
            }
            else if (condition == PLAYER_CONDITION.POISON)
            {
                return "毒";
            }
            else if (condition == PLAYER_CONDITION.BLESSING)
            {
                return "祝福";
            }
            else
            {
                //該当する状態がないときはOKを返す
                return "OK";
            }
        }

        //状態異常になってから経過したターンを1増やすメソッド
        public void AbConditionCount()
        {
            ab_condition_turn++;
        }

        //状態異常になってから経過したターンを初期化するメソッド
        public void AbConditionReset()
        {
            ab_condition_turn = 0;
        }

        //逃走回数を１増やすメソッド
        public void EscapeCountAdd()
        {
            escape_count++;
        }

        //逃走回数を初期化するメソッド
        public void EscapeCountReset()
        {
            escape_count = 0;
        }

        //状態異常回復判定を行うメソッド
        //戻り値（true:回復成功、false:回復失敗）
        public bool ConditionRecoverCheck()
        {
            //乱数の取得
            int rnd = Random.Range(0, RECOVER_PAR_MAX);

            //基準運の取得
            int luck_base = (int)System.Math.Round(RECOVER_LUCK_BASE + ((lv - 1) * LUCK_UP_AVG), 0, System.MidpointRounding.AwayFromZero);

            //状態異常回復率の初期化
            int per = (int)((luck - luck_base) * 0.5f);

            //状態異常回復率にそれぞれの状態に応じた基本値およびターン補正値を加算する
            if (condition == PLAYER_CONDITION.SLEEP)
            {
                //睡眠状態
                per += RECOVER_SLEEP;
                per += ab_condition_turn * RECOVER_SLEEP_CORRECTION;
            }
            else if (condition == PLAYER_CONDITION.SEALED)
            {
                //封印状態
                per += RECOVER_SEALED;
                per += ab_condition_turn * RECOVER_SEALED_CORRECTION;
            }
            else
            {
                //該当する状態がない場合はtrueを返す
                return true;
            }

            //状態異常回復率が下限値未満の時は下限値を設定
            if (per < RECOVER_PAR_MIN)
            {
                per = RECOVER_PAR_MIN;
            }

            //状態異常回復率が上限値を超えた時は上限値を設定
            if (per > RECOVER_PAR_LIMIT)
            {
                per = RECOVER_PAR_LIMIT;
            }

            //状態異常回復の成否の判定を行う
            if ((ab_condition_turn > 0) && (rnd < per))
            {
                //状態異常になってから1ターン以上経っており、その上で乱数が確率未満であるときは成功
                return true;
            }
            else
            {
                //上の条件に該当しないときは失敗
                return false;
            }
        }

        //プレイヤーの死亡チェックを行うメソッド
        //戻り値（true:死亡している、false:死亡していない）
        public bool DeadCheck()
        {
            if (hp <= 0)
            {
                //HPが0以下の時、trueを返す
                return true;
            }

            return false;
        }

        //倒した敵の経験値及びゴールドを受け取るメソッド
        //引数
        //enemy:倒した敵
        public void ExpGoldGet(Enemy enemy)
        {
            //経験値の取得
            exp += enemy.enemyExp;
            //最大値を超えた時は最大値を代入する
            if (exp > levelUpParam[LevelUpParam.LEVEL_MAX - 2].needExp)
            {
                exp = levelUpParam[LevelUpParam.LEVEL_MAX - 2].needExp;
            }

            //ゴールドの取得
            gold += enemy.enemyGold;
            //最大値を超えた時は最大値を代入する
            if (gold > GOLD_MAX)
            {
                gold = GOLD_MAX;
            }

        }

        //ゴールドを受け取るメソッド
        //引数
        //goid:受け取るゴールド
        //戻り値（受け取るゴールド）※取得したゴールド額をメッセージで表示するため
        public int GoldGet(int gold)
        {
            //ゴールドの取得
            this.gold += gold;
            //最大値を超えた時は最大値を代入する
            if (this.gold > GOLD_MAX)
            {
                this.gold = GOLD_MAX;
            }

            //取得したゴールドを返す
            return gold;
        }

        //ゴールドを失うメソッド
        //引数
        //goid:失うゴールド
        public void GoldLost(int gold)
        {
            //ゴールドの喪失
            this.gold -= gold;
            //0未満の時は0を代入する
            if (this.gold < 0)
            {
                this.gold = 0;
            }
        }

        //アイテム獲得時の処理を行うメソッド
        //引数
        //item:獲得アイテム
        public void ItemGet(Item item)
        {
            if (item.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE
                || item.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE)
            {
                //使い捨てアイテムの時
                bool have_flag = false;
                for (int i = 0; i < this.GetItemBoxCount(); i++)
                {
                    if (this.GetItemBoxIndex(i).itemId == item.itemId)
                    {
                        //1個以上持っているときは対象アイテムの数量を増やす
                        have_flag = true;
                        this.GetItemBoxIndex(i).itemCount++;
                    }
                }

                if (have_flag == false)
                {
                    //1個も持っていないときは所持アイテムリストに追加する
                    ItemBox box = new ItemBox(item.itemId, 1);
                    this.GetItemBox().Add(box);
                }
            }
            else
            {
                //使い捨てアイテムでないときは所持アイテムリストに追加する
                ItemBox box = new ItemBox(item.itemId, 1);
                this.GetItemBox().Add(box);
            }
        }

        //アイテム購入処理を行うメソッド
        //引数
        //item:購入アイテム
        //戻り値（購入の成否を表す列挙型）
        public BUY_COMPLETION ItemBuy(Item item)
        {
            if (gold < item.buyPrice)
            {
                //ゴールドが足りないとき
                return BUY_COMPLETION.NG_MONEY;
            }
            else
            {
                //所持アイテム数が最大かどうかのチェックを行う
                if (this.ItemMaxCheck() == true)
                {
                    //所持アイテム数が最大の時
                    if (item.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE
                        || item.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE)
                    {
                        //購入予定のアイテムが使い捨てアイテムの時
                        if (this.ItemHaveCheck(item) == false)
                        {
                            //購入予定のアイテムを1個も持っていないときは「アイテム満タン」を返す
                            return BUY_COMPLETION.NG_BOX_MAX;
                        }
                        else
                        {
                            //購入予定のアイテムを1個以上持っているとき
                            if (this.NormalItemNotMaxCheck(item) == false)
                            {
                                //購入予定のアイテムを最大数もっているときは「購入対象の使い捨てアイテム満タン」を返す
                                return BUY_COMPLETION.NG_ITEM_MAX;
                            }
                            else
                            {
                                //購入予定のアイテムをもっているが最大数でないときはアイテム購入処理を行い、「購入成功」を返す
                                this.ItemGet(item);
                                gold -= item.buyPrice;
                                return BUY_COMPLETION.OK;
                            }
                        }
                    }
                    else
                    {
                        //使い捨てアイテムでないときは「アイテム満タン」を返す
                        return BUY_COMPLETION.NG_BOX_MAX;
                    }

                }
                else
                {
                    //所持アイテム数が最大でない時
                    if (item.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE
                        || item.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE)
                    {
                        //購入予定のアイテムが使い捨てアイテムの時
                        if (this.ItemHaveCheck(item) == false)
                        {
                            //購入予定のアイテムを1個も持っていないときはアイテム購入処理を行い、「購入成功」を返す
                            this.ItemGet(item);
                            gold -= item.buyPrice;
                            return BUY_COMPLETION.OK;
                        }
                        else
                        {
                            //購入予定のアイテムを1個以上持っているとき
                            if (this.NormalItemNotMaxCheck(item) == false)
                            {
                                //購入予定のアイテムを最大数もっているときは「購入対象の使い捨てアイテム満タン」を返す
                                return BUY_COMPLETION.NG_ITEM_MAX;
                            }
                            else
                            {
                                //購入予定のアイテムをもっているが最大数でないときはアイテム購入処理を行い、「購入成功」を返す
                                this.ItemGet(item);
                                gold -= item.buyPrice;
                                return BUY_COMPLETION.OK;
                            }
                        }
                    }
                    else
                    {
                        //使い捨てアイテムでないときはアイテム購入処理を行い、「購入成功」を返す
                        this.ItemGet(item);
                        gold -= item.buyPrice;
                        return BUY_COMPLETION.OK;
                    }

                }
            }
        }

        //アイテム取得処理を行うメソッド
        //引数
        //item:取得アイテム
        //戻り値（取得の成否を表す列挙型）
        public PICK_COMPLETION ItemPick(Item item)
        {
            //所持アイテム数が最大かどうかのチェックを行う
            if (this.ItemMaxCheck() == true)
            {
                //所持アイテム数が最大の時
                if (item.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE
                    || item.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE)
                {
                    //取得予定のアイテムが使い捨てアイテムの時
                    if (this.ItemHaveCheck(item) == false)
                    {
                        //取得予定のアイテムを1個も持っていないときは「アイテム満タン」を返す
                        return PICK_COMPLETION.NG_BOX_MAX;
                    }
                    else
                    {
                        //取得予定のアイテムを1個以上持っているとき
                        if (this.NormalItemNotMaxCheck(item) == false)
                        {
                            //取得予定のアイテムを最大数もっているときは「取得対象の使い捨てアイテム満タン」を返す
                            return PICK_COMPLETION.NG_ITEM_MAX;
                        }
                        else
                        {
                            //取得予定のアイテムをもっているが最大数でないときはアイテム取得処理を行い、「取得成功」を返す
                            this.ItemGet(item);
                            return PICK_COMPLETION.OK;
                        }
                    }
                }
                else
                {
                    //使い捨てアイテムでないときは「アイテム満タン」を返す
                    return PICK_COMPLETION.NG_BOX_MAX;
                }

            }
            else
            {
                //所持アイテム数が最大でない時
                if (item.itemType == ItemGenerator.ITEM_TYPE.NORMAL_ONCE
                    || item.itemType == ItemGenerator.ITEM_TYPE.EVENT_ONCE)
                {
                    //取得予定のアイテムが使い捨てアイテムの時
                    if (this.ItemHaveCheck(item) == false)
                    {
                        //取得予定のアイテムを1個も持っていないときはアイテム取得処理を行い、「取得成功」を返す
                        this.ItemGet(item);
                        return PICK_COMPLETION.OK;
                    }
                    else
                    {
                        //取得予定のアイテムを1個以上持っているとき
                        if (this.NormalItemNotMaxCheck(item) == false)
                        {
                            //取得予定のアイテムを最大数もっているときは「取得対象の使い捨てアイテム満タン」を返す
                            return PICK_COMPLETION.NG_ITEM_MAX;
                        }
                        else
                        {
                            //取得予定のアイテムをもっているが最大数でないときはアイテム取得処理を行い、「取得成功」を返す
                            this.ItemGet(item);
                            return PICK_COMPLETION.OK;
                        }
                    }
                }
                else
                {
                    //使い捨てアイテムでないときはアイテム取得処理を行い、「取得成功」を返す
                    this.ItemGet(item);
                    return PICK_COMPLETION.OK;
                }

            }
        }

        //アイテム売却処理を行うメソッド
        //引数
        //list:全アイテムリスト
        //num:売却予定アイテムの所持アイテムリスト上での番号
        public void ItemSell(List<Item> list, int num)
        {
            //売却予定のアイテムのIDを取得
            int id = this.GetItemBoxIndex(num).itemId;

            //取得したIDに基づいた、アイテム情報の取得
            Item item = new Item();
            for (int i = 0; i < list.Count; i++)
            {
                if (id == list[i].itemId)
                {
                    item = list[i];
                    break;
                }
            }

            if (this.GetItemBoxIndex(num).itemEquiped == true)
            {
                //売却予定のアイテムが装備中の場合は装備を解除する
                this.UnEquip(item, num);
            }
            //アイテム売却処理を行う
            gold += item.salePrice;
            this.BoxItemSubtraction(num);

        }

        //アイテム廃棄処理を行うメソッド
        //引数
        //list:全アイテムリスト
        //num:廃棄予定アイテムの所持アイテムリスト上での番号
        //戻り値（true:廃棄完了、false:廃棄不可）
        public bool ItemDiscard(List<Item> list, int num)
        {
            //廃棄予定のアイテムのIDを取得
            int id = this.GetItemBoxIndex(num).itemId;

            //取得したIDに基づいた、アイテム情報の取得
            Item item = new Item();
            for (int i = 0; i < list.Count; i++)
            {
                if (id == list[i].itemId)
                {
                    item = list[i];
                    break;
                }
            }

            if (item.dicardFlag == true)
            {
                //廃棄予定のアイテムが廃棄不可の時
                return false;
            }
            else
            {
                //廃棄予定のアイテムが廃棄可能の時
                if (this.GetItemBoxIndex(num).itemEquiped == true)
                {
                    //廃棄予定のアイテムが装備中の場合は装備を解除する
                    this.UnEquip(item, num);
                }

                //アイテム廃棄処理を行う
                this.GetItemBox().RemoveAt(num);

                return true;
            }

        }

        //使い捨てアイテムの所持数が最大値に達しているかのチェックを行うメソッド
        //引数
        //item:対象アイテム
        //戻り値（true:最大値に達していない、false:最大値に達している）
        public bool NormalItemNotMaxCheck(Item item)
        {
            for (int i = 0; i < this.GetItemBoxCount(); i++)
            {
                if (item.itemId == this.GetItemBoxIndex(i).itemId)
                {
                    //対象アイテムを所持しているとき、チェックを行う
                    if (this.GetItemBoxIndex(i).itemCount < NORMAL_ITEM_MAX)
                    {
                        //最大値に達していない時
                        return true;
                    }
                    else
                    {
                        //最大値に達している時
                        return false;
                    }
                }
            }

            //対象アイテムを持っていないとき
            return true;
        }

        //対象アイテムを所持しているかのチェックを行うメソッド
        //引数
        //item:対象アイテム
        //戻り値（true:所持している、false:所持していない）
        public bool ItemHaveCheck(Item item)
        {
            for (int i = 0; i < this.GetItemBoxCount(); i++)
            {
                if (item.itemId == this.GetItemBoxIndex(i).itemId)
                {
                    //対象アイテムを所持しているとき
                    return true;
                }
            }

            //対象アイテムを所持していないとき
            return false;
        }

        //指定したアイテムが格納されている所持アイテムリストのインデックス番号を取得するメソッド
        //引数
        //item:指定したアイテムのオブジェクト
        //戻り値（指定したアイテムが格納されている所持アイテムリストのインデックス番号、
        //        格納されていないときは-1を返し、複数格納されているときは一番先頭に近いものを返す）
        public int GetItemBoxSpecifyIndex(Item item)
        {
            for (int i = 0; i < this.item_box.Count; i++)
            {
                if (item.itemId == this.GetItemBoxIndex(i).itemId)
                {
                    //指定されたアイテムが格納されている場合、対象のインデックス番号を返す
                    return i;
                }
            }

            //指定されたアイテムが格納されていない場合、-1を返す
            return -1;
        }

        //アイテム所持数が最大値に達しているかどうかのチェックを行うメソッド
        //戻り値（true:達している、false:達していない）
        public bool ItemMaxCheck()
        {
            if (this.GetItemBoxCount() >= ITEM_MAX)
            {
                return true;
            }

            return false;
        }

        //所持アイテムリストから、対象アイテムの削除を行うメソッド
        //引数
        //num:削除予定アイテムの所持アイテムリストのインデックス番号
        public void BoxItemDelete(int num)
        {
            if (num >= 0 && num < this.GetItemBoxCount())
            {
                this.GetItemBox().RemoveAt(num);
            }
        }

        //所持アイテムリストから、対象使い捨てアイテムの減少を行うメソッド
        //引数
        //num:減少予定アイテムの所持アイテムリスト上での番号
        public void BoxItemSubtraction(int num)
        {
            if (this.GetItemBoxIndex(num).itemCount > 1)
            {
                //所持数が2以上の時は減少させる
                this.GetItemBoxIndex(num).itemCount--;
            }
            else
            {
                //所持数が1の時は削除する
                this.BoxItemDelete(num);
            }
        }

        //アイテムウィンドウのページ数を算出するメソッド
        //戻り値（アイテムウィンドウのページ数）
        public int ItemPageMaxCalc()
        {
            //所持アイテム数÷1ページごとのアイテム表示数でページ数を算出する
            int ret = this.GetItemBoxCount() / ITEM_PER_PAGE;

            if (this.GetItemBoxCount() % ITEM_PER_PAGE > 0)
            {
                //上記の計算で余りが出るときは1ページ増やす
                ret++;
            }

            return ret;
        }

        //装備アイテムの装備を行うメソッド
        //引数
        //item:対象アイテム
        //num:装備予定アイテムの所持アイテムリスト上での番号
        public void Equip(Item item, int num)
        {
            //装備種類番号の取得
            int equipNum = (int)(item.itemType - ItemGenerator.ITEM_TYPE.EQUIP) - 1;
            //対象の所持アイテムを装備状態にする
            this.GetItemBoxIndex(num).itemEquiped = true;
            //装備アイテム配列に対象アイテムのIDを代入する
            this.equipArray[equipNum] = this.GetItemBoxIndex(num).itemId;

            if (item.itemType > ItemGenerator.ITEM_TYPE.EQUIP && item.itemType < ItemGenerator.ITEM_TYPE.EQUIP_END)
            {
                if (item.itemType == ItemGenerator.ITEM_TYPE.WEAPON)
                {
                    //武器の場合は攻撃力を上げる
                    attack += item.weaponAttack;
                }
                else if (item.itemType >= ItemGenerator.ITEM_TYPE.ARMOR && item.itemType <= ItemGenerator.ITEM_TYPE.HELM)
                {
                    //防具の場合は防御力を上げる
                    defense += item.armorDefense;
                }
            }
        }

        //装備アイテムの装備解除を行うメソッド
        //引数
        //item:対象アイテム
        //num:装備解除予定アイテムの所持アイテムリスト上での番号
        public void UnEquip(Item item, int num)
        {
            //装備種類番号の取得
            int equipNum = (int)(item.itemType - ItemGenerator.ITEM_TYPE.EQUIP) - 1;
            //対象の所持アイテムを未装備状態にする
            this.GetItemBoxIndex(num).itemEquiped = false;
            //装備アイテム配列の内容を初期化する
            this.equipArray[equipNum] = 0;

            if (item.itemType > ItemGenerator.ITEM_TYPE.EQUIP && item.itemType < ItemGenerator.ITEM_TYPE.EQUIP_END)
            {
                if (item.itemType == ItemGenerator.ITEM_TYPE.WEAPON)
                {
                    //武器の場合は攻撃力を下げる
                    attack -= item.weaponAttack;
                }
                else if (item.itemType >= ItemGenerator.ITEM_TYPE.ARMOR && item.itemType <= ItemGenerator.ITEM_TYPE.HELM)
                {
                    //防具の場合は防御力を下げる
                    defense -= item.armorDefense;
                }
            }
        }

        //装備変更前に装備していた装備アイテムの所持アイテムリスト上での番号を返すメソッド
        //引数
        //type:アイテムの種類
        public int GetPreEquipNumber(ItemGenerator.ITEM_TYPE type)
        {
            if (type <= ItemGenerator.ITEM_TYPE.EQUIP && type >= ItemGenerator.ITEM_TYPE.EQUIP_END)
            {
                //装備アイテムでない場合は-1を返す
                return -1;
            }

            //装備種類番号の取得
            int num = (int)(type - ItemGenerator.ITEM_TYPE.EQUIP) - 1;

            for (int i = 0; i < this.GetItemBoxCount(); i++)
            {
                if (this.equipArray[num] == this.GetItemBoxIndex(i).itemId && this.GetItemBoxIndex(i).itemEquiped == true)
                {
                    //装備変更前に装備していた装備アイテムがある場合は、そのアイテムの所持アイテムリスト上での番号を返す
                    return i;
                }
            }

            //何も装備していないときは場合は-1を返す
            return -1;
        }

        //プレイヤーのHPを0にするメソッド
        public void PlayerKill()
        {
            this.hp = 0;
        }

        //HP回復処理を行うメソッド
        //引数
        //hp:HPの回復量
        public void RecoverHp(int hp)
        {
            this.hp += hp;
            
            if (this.hpMax < this.hp)
            {
                //最大HPを超えるときは最大HPを代入する
                this.hp = hpMax;
            }
        }

        //HP減少処理を行うメソッド
        //引数
        //hp:HPの減少量
        public void DeclineHp(int hp)
        {
            this.hp -= hp;

            if (this.hp < 0)
            {
                //0未満になるときは0を代入する
                this.hp = 0;
            }
        }

        //状態の変更を行うメソッド
        //引数
        //condition:変更する状態
        public void ChangeCondition(PLAYER_CONDITION condition)
        {
            this.condition = condition;
        }

        //特殊魔法アイテム（毒を持った武器での攻撃を含む）使用成功率の算出を行うメソッド
        //引数
        //enemy:使用対象の敵
        //type:特殊魔法アイテム種類
        //戻り値（特殊魔法アイテム使用成功率）
        public int GetEffectPercent(Enemy enemy, ItemGenerator.ITEM_EFFECT_TYPE type)
        {
            //成功率を表す変数の初期化
            int per = (luck / 2) - (enemy.enemyLuck / 2);

            if (type == ItemGenerator.ITEM_EFFECT_TYPE.SLEEP)
            {
                //眠りの魔法

                //成功率を算出する
                per += SLEEP_PAR_BASE;

                //敵の持つ睡眠耐性に応じて成功率を修正する
                if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.SLEEP] == Enemy.ENEMY_RESIST.WEAK)
                {
                    //弱い
                    per = (int)(per * 1.5f);
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.SLEEP] == Enemy.ENEMY_RESIST.STRONG)
                {
                    //強い
                    per = per / 2;
                }
                else if(enemy.enemyResist[(int)Enemy.RESIST_TYPE.SLEEP] == Enemy.ENEMY_RESIST.INVALID)
                {
                    //無効
                    per = 0;
                }

                if (per < 0)
                {
                    //成功率が0未満の時は0を入れる
                    per = 0;
                }
                if (per > EFFECT_PAR_MAX)
                {
                    //成功率が最大値を超える時は最大値を入れる
                    per = EFFECT_PAR_MAX;
                }

                
            }
            else if (type == ItemGenerator.ITEM_EFFECT_TYPE.SEALED)
            {
                //封印の魔法

                //成功率を算出する
                per += SEALED_PAR_BASE;

                //敵の持つ封印耐性に応じて成功率を修正する
                if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.SEALED] == Enemy.ENEMY_RESIST.WEAK)
                {
                    //弱い
                    per = (int)(per * 1.5f);
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.SEALED] == Enemy.ENEMY_RESIST.STRONG)
                {
                    //強い
                    per = per / 2;
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.SEALED] == Enemy.ENEMY_RESIST.INVALID)
                {
                    //無効
                    per = 0;
                }

                if (per < 0)
                {
                    //成功率が0未満の時は0を入れる
                    per = 0;
                }
                if (per > EFFECT_PAR_MAX)
                {
                    //成功率が最大値を超える時は最大値を入れる
                    per = EFFECT_PAR_MAX;
                }

            }
            else if (type == ItemGenerator.ITEM_EFFECT_TYPE.POISON)
            {
                //毒の魔法

                //成功率を算出する
                per += POISON_PAR_BASE;

                //敵の持つ毒耐性に応じて成功率を修正する
                if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.POISON] == Enemy.ENEMY_RESIST.WEAK)
                {
                    //弱い
                    per = (int)(per * 1.5f);
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.POISON] == Enemy.ENEMY_RESIST.STRONG)
                {
                    //強い
                    per = per / 2;
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.POISON] == Enemy.ENEMY_RESIST.INVALID)
                {
                    //無効
                    per = 0;
                }

                if (per < 0)
                {
                    //成功率が0未満の時は0を入れる
                    per = 0;
                }
                if (per > EFFECT_PAR_MAX)
                {
                    //成功率が最大値を超える時は最大値を入れる
                    per = EFFECT_PAR_MAX;
                }

            }
            else if (type == ItemGenerator.ITEM_EFFECT_TYPE.DEATH)
            {
                //死の魔法

                //成功率を算出する
                per += DEATH_PAR_BASE;

                //敵の持つ即死耐性に応じて成功率を修正する
                if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.DEATH] == Enemy.ENEMY_RESIST.WEAK)
                {
                    //弱い
                    per = (int)(per * 1.5f);
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.DEATH] == Enemy.ENEMY_RESIST.STRONG)
                {
                    //強い
                    per = per / 2;
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.DEATH] == Enemy.ENEMY_RESIST.INVALID)
                {
                    //無効
                    per = 0;
                }

                if (per < 0)
                {
                    //成功率が0未満の時は0を入れる
                    per = 0;
                }
                if (per > EFFECT_PAR_MAX)
                {
                    //成功率が最大値を超える時は最大値を入れる
                    per = EFFECT_PAR_MAX;
                }

            }
            else if (type == ItemGenerator.ITEM_EFFECT_TYPE.FINAL_SEALED)
            {
                //封印の魔法（最終ボス用）

                //敵の種類をチェックする
                if (enemy.enemyType == Enemy.ENEMY_TYPE.FINAL_BOSS)
                {
                    //最終ボスの時は確実に成功する
                    per = EFFECT_PAR_MAX;
                }
                else
                {
                    //最終ボス以外の時は確実に失敗する
                    per = 0;
                }
            }
            else if (type == ItemGenerator.ITEM_EFFECT_TYPE.POISON_ATTACK)
            {
                //毒攻撃

                //成功率を算出する
                per += POISON_ATTACK_PAR_BASE;

                //敵の持つ毒耐性に応じて成功率を修正する
                if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.POISON] == Enemy.ENEMY_RESIST.WEAK)
                {
                    //弱い
                    per = (int)(per * 1.5f);
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.POISON] == Enemy.ENEMY_RESIST.STRONG)
                {
                    //強い
                    per = per / 2;
                }
                else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.POISON] == Enemy.ENEMY_RESIST.INVALID)
                {
                    //無効
                    per = 0;
                }

                if (per < 0)
                {
                    //成功率が0未満の時は0を入れる
                    per = 0;
                }
                if (per > EFFECT_PAR_MAX)
                {
                    //成功率が最大値を超える時は最大値を入れる
                    per = EFFECT_PAR_MAX;
                }

            }
            else
            {
                //該当する魔法がない時は-1を設定
                per = -1;
            }

            //成功率を返す
            return per;
        }

        //毒によるダメージを算出するメソッド
        //引数
        //fight_flag:戦闘中フラグ（true:戦闘中、false:戦闘中でない）
        //戻り値（毒によるダメージ）
        public int PoisonDamage(bool fight_flag)
        {
            int damage = 0;

            //毒のダメージの算出
            if (fight_flag == true)
            {
                //戦闘中
                damage = this.hpMax / POISON_DAMAGE;

                if (damage < 1)
                {
                    //計算結果が1未満の時は1にする
                    damage = 1;
                }
            }
            else
            {
                //戦闘中でないとき
                damage = 1;
            }

            //現在HPから毒のダメージを引く
            this.hp = this.hp - damage;
            if (this.hp < 0)
            {
                this.hp = 0;
            }

            //毒によるダメージを返す（戦闘中のメッセージに使用するため）
            return damage;
        }

        //祝福によるHPの回復を行うメソッド
        public void BlessingHpHeal()
        {
            this.hp += BLESSING_HEAL;

            if (this.hp > this.hpMax)
            {
                this.hp = this.hpMax;
            }
        }

        //戦闘中に祝福によるHPの回復を行うメソッド
        public void BlessingHpHealFight()
        {
            //祝福の回復HPの算出
            int heal_hp = this.hpMax / BLESSING_HEAL_FIGHT;

            if (heal_hp < 1)
            {
                //計算結果が1未満の時は1にする
                heal_hp = 1;
            }

            //現在HPに回復HPを加算する
            this.hp = this.hp + heal_hp;
            if (this.hp > this.hpMax)
            {
                this.hp = this.hpMax;
            }
        }


        //会心の一撃が出たかどうかを判定するメソッド
        //戻り値（true:会心の一撃、false:通常攻撃）
        public bool CriticalAttackCheck()
        {
            //乱数を取得
            int rnd = Random.Range(0, CRITICAL_PAR_MAX);

            if (rnd < CRITICAL_PAR_BASE)
            {
                //会心の一撃
                return true;
            }
            else
            {
                //通常攻撃
                return false;
            }
        }

        //敵が先制攻撃を仕掛けてくるかどうかを判定するメソッド
        //引数
        //enemy:戦闘対象の敵
        //戻り値（true:仕掛けてくる、false:仕掛けてこない）
        public bool FightTurnDecision(Enemy enemy)
        {
            //プレイヤーと敵の素早さスコアを算出する
            int p_score = speed + Mathf.RoundToInt(Random.Range(-speed / 3.0f, speed / 3.0f));
            int e_score = enemy.enemySpeed + Mathf.RoundToInt(Random.Range(-enemy.enemySpeed / 3.0f, enemy.enemySpeed / 3.0f));

            if (p_score >= e_score)
            {
                //プレイヤーの素早さスコアが敵の素早さスコア以上の時
                //通常の攻撃
                return true;
            }
            else
            {
                //プレイヤーの素早さスコアが敵の素早さスコア未満の時
                //敵の先制攻撃
                return false;
            }
        }

        //敵の攻撃をかわすかどうかを判定するメソッド
        //引数
        //enemy:戦闘対象の敵
        //戻り値（true:かわす、false:かわさない）
        public bool AttackAvoidCheck(Enemy enemy)
        {
            if (condition == PLAYER_CONDITION.SLEEP)
            {
                //睡眠状態の時は回避失敗
                return false;
            }

            //回避率の取得
            //int avoid_par = Mathf.RoundToInt((speed - enemy.enemySpeed) / 10.0f);
            float f_avoid = (speed * 0.8f) - (enemy.enemySpeed * 0.4f) + (luck * 0.3f) - (enemy.enemyLuck * 0.2f);
            //float f_avoid = (speed * 0.8f) - (enemy.enemySpeed * 0.4f) + (luck * 0.2f) - (enemy.enemyLuck * 0.1f);
            int avoid_par = Mathf.RoundToInt(f_avoid);
            avoid_par += AVOID_BASE_MIN;

            if (avoid_par < AVOID_BASE_MIN)
            {
                //回避率が最小値を下回るときは、最小値を設定する
                avoid_par = AVOID_BASE_MIN;
            }

            if (avoid_par > AVOID_BASE_MAX)
            {
                //回避率が最大値を上回るときは、最大値を設定する
                avoid_par = AVOID_BASE_MAX;
            }

            //乱数を取得する
            int rnd = Random.Range(0, AVOID_MAX);

            if (rnd < avoid_par)
            {
                //回避成功
                return true;
            }
            else
            {
                //回避失敗
                return false;
            }

        }

        //逃走の成否を判定するメソッド
        //引数
        //enemy:戦闘対象の敵
        //戻り値（true:成功、false:失敗）
        public bool EscapeCheck(Enemy enemy)
        {
            if (enemy.enemyType == Enemy.ENEMY_TYPE.BOSS ||
                enemy.enemyType == Enemy.ENEMY_TYPE.FINAL_BOSS)
            {
                //敵の種類が中ボスもしくは最終ボスの時は確実に失敗
                return false;
            }

            if (enemy.enemyCondition == Enemy.ENEMY_CONDITION.SLEEP)
            {
                //敵が睡眠中は確実に成功
                return true;
            }

            //逃走成功率を算出
            float rate_work = ESCAPE_BASE + (speed * 0.7f) - (enemy.enemySpeed * 0.5f) +
                            (luck * 0.3f) - (enemy.enemyLuck * 0.2f);
            int rate = Mathf.RoundToInt(rate_work);

            //最小値未満の場合は最小値に設定する
            if (rate < ESCAPE_MIN)
            {
                rate = ESCAPE_MIN;
            }

            //乱数を取得
            int rnd = Random.Range(0, ESCAPE_MAX);

            //プレイヤーのHP減少率に応じて成功率を上げる
            float hp_float = (float)hp / (float)hpMax;
            float hp_correct = (1.0f - hp_float) * 20.0f;
            rate = rate + Mathf.RoundToInt(hp_correct);

            //逃走を試みた回数だけ成功率を上げる
            rate = rate + (escape_count * 10);

            //（最大値-最小値）を超えた場合は（最大値-最小値）に設定する
            if (rate > ESCAPE_MAX - ESCAPE_MIN)
            {
                rate = ESCAPE_MAX - ESCAPE_MIN;
            }

            if (rnd < rate)
            {
                //乱数が確率未満の時は成功
                return true;
            }
            else
            {
                //乱数が確率以上の時は失敗
                return false;
            }
        }

        //宿屋に泊まった時の処理を行うメソッド
        //引数
        //price:宿代
        public void InnStayHeal(int price)
        {
            //所持ゴールドから宿代を引く
            gold -= price;
            
            if (condition != PLAYER_CONDITION.BLESSING)
            {
                //状態が祝福以外の時のみ状態異常の回復を行う
                condition = PLAYER_CONDITION.OK;
            }

            //HPの回復する
            hp = hpMax;
        }

        //ファイルから読み込んだレベルアップ情報（文字列）を数値に変換するメソッド
        //引数
        //num:番号（レベル2を0とする）
        //need_exp:必要経験値
        //up_hp_max:最大HP
        //up_attack:攻撃力
        //up_defense:防御力
        //up_speed:素早さ
        //up_luck:運
        public void LevelUPParamSetFromFile(int num, string need_exp, string up_hp_max, string up_attack, string up_defense, string up_speed, string up_luck)
        {
            levelUpParam[num].needExp = int.Parse(need_exp);
            levelUpParam[num].upHpMax = int.Parse(up_hp_max);
            levelUpParam[num].upAttack = int.Parse(up_attack);
            levelUpParam[num].upDefense = int.Parse(up_defense);
            levelUpParam[num].upSpeed = int.Parse(up_speed);
            levelUpParam[num].upLuck = int.Parse(up_luck);
        }

        //経験値が次のレベルアップに必要な量に達しているかどうかを判定するメソッド
        //戻り値（true:達している、false:達していない）
        public bool LevelUpCheck()
        {
            if (lv >= LevelUpParam.LEVEL_MAX)
            {
                //レベルが最大の時
                return false;
            }

            if (exp >= levelUpParam[lv - 1].needExp)
            {
                //次のレベルアップに必要な量に達しているとき
                return true;
            }
            else
            {
                //次のレベルアップに必要な量に達していないとき
                return false;
            }
        }

        //レベルアップ時のパラメータ上昇処理を行うメソッド
        public void PlayerLevelUp()
        {
            hpMax += levelUpParam[lv - 1].upHpMax;
            attack += levelUpParam[lv - 1].upAttack;
            defense += levelUpParam[lv - 1].upDefense;
            speed += levelUpParam[lv - 1].upSpeed;
            luck += levelUpParam[lv - 1].upLuck;
            lv++;
        }

        //レベルアップ時のメッセージを返すメソッド
        //戻り値（レベルアップ時のメッセージ）
        public string PlayerLevelUpMessage()
        {
            string str = name + "はレベルが上がった！\n";
            str = str + "最大HPが" + levelUpParam[lv - 1].upHpMax.ToString()
                    + "、攻撃力が" + levelUpParam[lv - 1].upAttack.ToString()
                    + "、防御力が" + levelUpParam[lv - 1].upDefense.ToString() + "、\n"
                    + "素早さが" + levelUpParam[lv - 1].upSpeed.ToString()
                    + "、運が" + levelUpParam[lv - 1].upLuck.ToString() + "上がった！\n";

            return str;
        }

        //ステータスウィンドウに表示する次のレベルに必要な経験値を返すメソッド
        //戻り値（次のレベルに必要な経験値）
        public string PlayerNextLevelExp()
        {
            if (lv >= LevelUpParam.LEVEL_MAX)
            {
                //最大レベルの時
                return "Level Max";
            }
            else
            {
                //最大レベルでないとき
                return (levelUpParam[lv - 1].needExp - exp).ToString();
            }
        }

#region デバッグ
        //----------------------------デバッグ用メンバ関数---------------------------------

        //所持アイテムを設定するメソッド
        public void ItemDebugDataInput()
        {
            ItemBox box1 = new ItemBox(1, 1, false);
            ItemBox box2 = new ItemBox(2, 1, false);
            ItemBox box3 = new ItemBox(3, 1, false);
            ItemBox box4 = new ItemBox(4, 1, false);
            ItemBox box5 = new ItemBox(5, 1, false);
            ItemBox box6 = new ItemBox(6, 1, false);
            ItemBox box7 = new ItemBox(7, 1, false);
            ItemBox box8 = new ItemBox(8, 1, false);
            ItemBox box9 = new ItemBox(9, 1, false);
            ItemBox box10 = new ItemBox(10, 1, false);
            ItemBox box11 = new ItemBox(11, 1, false);
            ItemBox box12 = new ItemBox(12, 1, false);
            ItemBox box13 = new ItemBox(13, 1, false);
            ItemBox box14 = new ItemBox(14, 1, false);
            ItemBox box15 = new ItemBox(15, 1, false);
            ItemBox box16 = new ItemBox(16, 1, false);
            ItemBox box17 = new ItemBox(17, 1, false);
            ItemBox box18 = new ItemBox(18, 1, false);
            ItemBox box19 = new ItemBox(19, 1, false);
            ItemBox box20 = new ItemBox(20, 1, false);
            ItemBox box21 = new ItemBox(21, 1, false);
            ItemBox box22 = new ItemBox(22, 1, false);
            ItemBox box23 = new ItemBox(23, 1, false);
            ItemBox box24 = new ItemBox(24, 1, false);
            ItemBox box25 = new ItemBox(25, 9);
            ItemBox box26 = new ItemBox(26, 8);
            ItemBox box27 = new ItemBox(27, 8);
            ItemBox box28 = new ItemBox(28, 9);
            ItemBox box29 = new ItemBox(29, 9);
            ItemBox box30 = new ItemBox(30, 1);
            ItemBox box31 = new ItemBox(31, 1);
            ItemBox box32 = new ItemBox(32, 1);
            ItemBox box33 = new ItemBox(33, 1);
            ItemBox box34 = new ItemBox(34, 1);
            ItemBox box35 = new ItemBox(35, 1);

            ItemBox box36 = new ItemBox(36, 1);
            ItemBox box37 = new ItemBox(37, 1);
            ItemBox box38 = new ItemBox(38, 1);
            ItemBox box39 = new ItemBox(39, 1);
            ItemBox box40 = new ItemBox(40, 1);

            ItemBox box41 = new ItemBox(41, 1);
            ItemBox box42 = new ItemBox(42, 1);
            ItemBox box43 = new ItemBox(43, 1);
            ItemBox box44 = new ItemBox(44, 1);
            ItemBox box45 = new ItemBox(45, 1);
            ItemBox box46 = new ItemBox(46, 1);

            ItemBox box47 = new ItemBox(47, 4);
            ItemBox box48 = new ItemBox(48, 1);


            ItemBoxInfoDebugSet(box1, 0);
            ItemBoxInfoDebugSet(box2, 1);
            ItemBoxInfoDebugSet(box3, 2);
            ItemBoxInfoDebugSet(box4, 3);
            ItemBoxInfoDebugSet(box5, 4);
            ItemBoxInfoDebugSet(box6, 5);
            ItemBoxInfoDebugSet(box7, 6);
            ItemBoxInfoDebugSet(box8, 7);
            ItemBoxInfoDebugSet(box9, 8);
            ItemBoxInfoDebugSet(box10, 9);

            ItemBoxInfoDebugSet(box11, 10);
            ItemBoxInfoDebugSet(box12, 11);
            ItemBoxInfoDebugSet(box13, 12);
            ItemBoxInfoDebugSet(box14, 13);
            ItemBoxInfoDebugSet(box15, 14);
            ItemBoxInfoDebugSet(box16, 15);
            ItemBoxInfoDebugSet(box17, 16);
            ItemBoxInfoDebugSet(box18, 17);
            ItemBoxInfoDebugSet(box19, 18);
            ItemBoxInfoDebugSet(box20, 19);

            ItemBoxInfoDebugSet(box21, 20);
            ItemBoxInfoDebugSet(box22, 21);
            ItemBoxInfoDebugSet(box23, 22);
            ItemBoxInfoDebugSet(box24, 23);
            ItemBoxInfoDebugSet(box25, 24);
            ItemBoxInfoDebugSet(box26, 25);
            ItemBoxInfoDebugSet(box27, 26);
            ItemBoxInfoDebugSet(box28, 27);
            ItemBoxInfoDebugSet(box29, 28);
            ItemBoxInfoDebugSet(box30, 29);

            ItemBoxInfoDebugSet(box31, 30);
            ItemBoxInfoDebugSet(box32, 31);
            ItemBoxInfoDebugSet(box33, 32);
            ItemBoxInfoDebugSet(box34, 33);
            ItemBoxInfoDebugSet(box35, 34);
            ItemBoxInfoDebugSet(box36, 35);
            ItemBoxInfoDebugSet(box37, 36);
            ItemBoxInfoDebugSet(box38, 37);
            ItemBoxInfoDebugSet(box39, 38);
            ItemBoxInfoDebugSet(box40, 39);

            ItemBoxInfoDebugSet(box41, 40);
            //ItemBoxInfoDebugSet(box2, 41);
            ItemBoxInfoDebugSet(box42, 41);
            ItemBoxInfoDebugSet(box43, 42);
            //ItemBoxInfoDebugSet(box2, 42);
            //ItemBoxInfoDebugSet(box44, 43);
            ItemBoxInfoDebugSet(box2, 43);
            ItemBoxInfoDebugSet(box45, 44);
            //ItemBoxInfoDebugSet(box2, 44);
            ItemBoxInfoDebugSet(box46, 45);
            ItemBoxInfoDebugSet(box47, 46);
            ItemBoxInfoDebugSet(box48, 47);
            //ItemBoxInfoDebugSet(box2, 47);
            ItemBoxInfoDebugSet(box2, 48);
            ItemBoxInfoDebugSet(box2, 49);

            ItemBoxInfoDebugSet(box2, 50);
            ItemBoxInfoDebugSet(box2, 51);
            ItemBoxInfoDebugSet(box2, 52);
            ItemBoxInfoDebugSet(box2, 53);
            ItemBoxInfoDebugSet(box2, 54);
            ItemBoxInfoDebugSet(box2, 55);
            ItemBoxInfoDebugSet(box2, 56);
            ItemBoxInfoDebugSet(box2, 57);
            ItemBoxInfoDebugSet(box2, 58);
            //ItemBoxInfoDebugSet(box2, 59);
        }

        //所持アイテムリストにアイテム情報を追加するメソッド
        //引数
        //box:追加するアイテム情報
        //num:読込先の所持アイテムリストの要素番号
        void ItemBoxInfoDebugSet(ItemBox box, int num)
        {
            //所持アイテムリストの要素数を1増加する
            ItemBox b = new ItemBox();
            item_box.Add(b);

            //アイテムID、所持数、装備フラグを所持アイテムリストに追加
            item_box[num].itemId = box.itemId;
            item_box[num].itemCount = box.itemCount;
            item_box[num].itemEquiped = box.itemEquiped;
        }

        //設定した経験値に基づいてプレイヤーのレベルを設定するメソッド
        //引数
        //exp:経験値
        public void DebugLevelUp(int exp = 0)
        {
            this.exp = exp;

            //引数で設定した経験値に応じてレベルを上げていく
            for (int i = 0; i < LevelUpParam.LEVEL_MAX - 1; i++)
            {
                if (this.exp >= levelUpParam[i].needExp)
                {
                    //設定した経験値が次のレベルに必要な経験値以上の時、レベルを1上げて、各能力値を上げる
                    this.lv++;
                    this.hpMax += levelUpParam[i].upHpMax;
                    this.hp = this.hpMax;
                    this.attack += levelUpParam[i].upAttack;
                    this.defense += levelUpParam[i].upDefense;
                    this.speed += levelUpParam[i].upSpeed;
                    this.luck += levelUpParam[i].upLuck;
                }
                else
                {
                    //設定した経験値が次のレベルに必要な経験値未満の時は処理を終了する
                    break;
                }
            }

        }
        //----------------------------------------------------------------------------------
#endregion

    }

    //所持アイテムクラス
    [System.Serializable]
    public class ItemBox
    {
        //フィールド
        [SerializeField] private int id;        //アイテムID
        [SerializeField] private int count;     //所持数
        [SerializeField] private bool equiped;  //装備フラグ（true:装備している、false:装備していない）

        //コンストラクタ（値なし）
        public ItemBox()
        {
            this.id = 0;
            this.count = 0;
            this.equiped = false;
        }

        //コンストラクタ（IDのみ設定）
        public ItemBox(int id)
        {
            this.id = id;
            this.count = 0;
            this.equiped = false;
        }

        //コンストラクタ（ID、所持数、装備フラグを設定）
        public ItemBox(int id, int count, bool equiped = false)
        {
            this.id = id;
            this.count = count;
            this.equiped = equiped;
        }

        //プロパティ
        //アイテムID
        public int itemId
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        //所持数
        public int itemCount
        {
            get
            {
                return count;
            }

            set
            {
                count = value;
            }
        }

        //装備フラグ
        public bool itemEquiped
        {
            get
            {
                return equiped;
            }

            set
            {
                equiped = value;
            }
        }

    }
}
