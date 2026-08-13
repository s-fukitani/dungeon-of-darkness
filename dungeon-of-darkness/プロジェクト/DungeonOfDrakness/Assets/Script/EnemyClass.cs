using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//敵関連のネームスペース
namespace EnemyClass
{
    //敵の攻撃手段に関するクラス
    public class EnemyAttackPattern
    {
        //敵の攻撃手段を表す列挙型
        public enum ATTACK_METHOD
        {
            NONE,               //なし
            ATTACK,             //普通の攻撃
            SLEEP,              //眠りの魔法
            FIRE_BALL,          //火の玉の魔法
            THUNDER,            //雷の魔法
            SEALED,             //封印の魔法
            POISON_ATTACK,      //毒攻撃
            DEATH_MAGIC,        //死の魔法
            POISON_SPRAY,       //毒液噴射
            FIRE_BREATH,        //火炎噴射
            DEATH_SICKLE,       //死神の鎌
            HEAL_MAGIC          //回復魔法
        }

        public const int PERCENT_MAX = 100;     //確率計算に使用する最大値

        private ATTACK_METHOD method;           //敵の攻撃手段
        private int percent;                    //対象の攻撃手段が実行される確率

        //コンストラクタ
        public EnemyAttackPattern()
        {
            this.method = ATTACK_METHOD.NONE;
            this.percent = 0;
        }

        //コンストラクタ（引数あり）
        //引数
        //method:攻撃手段
        //percent:実行される確率
        public EnemyAttackPattern(ATTACK_METHOD method, int percent)
        {
            this.method = method;
            this.percent = percent;
        }

        //プロパティ

        //敵の攻撃手段
        public ATTACK_METHOD attackMethod
        {
            set
            {
                method = value;
            }

            get
            {
                return method;
            }
        }

        //対象の攻撃手段が実行される確率
        public int attackPercent
        {
            set
            {
                percent = value;
            }

            get
            {
                return percent;
            }
        }

        //攻撃手段および確率を設定するメソッド
        //引数
        //method:攻撃手段
        //percent:実行される確率
        public void SetAttackPattern(ATTACK_METHOD method, int percent)
        {
            this.method = method;
            this.percent = percent;
        }
    }

    //敵クラス
    public class Enemy
    {
        //敵の状態に関する列挙型
        public enum ENEMY_CONDITION
        {
            OK,             //OK
            SLEEP,          //睡眠状態
            SEALED,         //封印状態
            POISON,         //毒状態
            FINAL_SEALED    //封印状態（最終ボス）
        }

        //敵の種類に関する列挙型
        public enum ENEMY_TYPE
        {
            NORMAL,         //雑魚
            BOSS,           //中ボス
            FINAL_BOSS      //最終ボス  
        }

        //敵の耐性に関する列挙型
        public enum ENEMY_RESIST
        {
            NORMAL,   //普通
            WEAK,     //弱い
            STRONG,   //強い
            INVALID   //無効
        }

        //耐性の種類に関する列挙型
        public enum RESIST_TYPE
        {
            ATTACK_MAGIC,   //攻撃魔法
            POISON,         //毒
            SLEEP,          //眠り
            SEALED,         //封印
            DEATH,          //即死
            RESIST_MAX      //耐性の種類の最大値（配列の長さ設定に使用）
        }


        //定数
        public const int EFFECT_PAR_MAX = 100;      //プレイヤーを状態異常にする確率の計算に使用する乱数の最大値
        private const int SLEEP_PAR_BASE = 60;      //プレイヤーを睡眠状態にする確率の計算に使用する基本値（敵の運で加算した後、プレイヤーの運を減算して使用）
        private const int SEALED_PAR_BASE = 50;     //プレイヤーを封印状態にする確率の計算に使用する基本値（敵の運で加算した後、プレイヤーの運を減算して使用）
        private const int POISON_PAR_BASE = 35;     //プレイヤーを毒状態にする確率の計算に使用する基本値（敵の運で加算した後、プレイヤーの運を減算して使用）
        private const int DEATH_PAR_BASE = 10;      //プレイヤーを即死させる確率の計算に使用する基本値（敵の運で加算した後、プレイヤーの運を減算して使用）
        private const int D_SICKLE_PAR_BASE = 65;   //プレイヤーを即死させる（死神の鎌）確率の計算に使用する基本値（敵の運で加算した後、プレイヤーの運を減算して使用）

        public const int RECOVER_PAR_MAX = 100;             //状態異常から回復する確率の計算に使用する乱数の最大値
        private const int RECOVER_SLEEP = 20;               //睡眠状態から回復する確率の計算に使用する基本値
        private const int RECOVER_SEALED = 15;              //封印状態から回復する確率の計算に使用する基本値
        private const int RECOVER_POISON = 8;               //毒状態から回復する確率の計算に使用する基本値
        private const int RECOVER_SLEEP_CORRECTION = 8;     //睡眠状態から回復する確率の計算に使用する補正値（経過ターン数に乗算する）
        private const int RECOVER_SEALED_CORRECTION = 5;    //封印状態から回復する確率の計算に使用する補正値（経過ターン数に乗算する）
        private const int RECOVER_POISON_CORRECTION = 3;    //毒状態から回復する確率の計算に使用する補正値（経過ターン数に乗算する）
        private const int RECOVER_PAR_MIN = 5;      //状態異常から回復する確率の下限値
        private const int RECOVER_PAR_LIMIT = 90;   //状態異常から回復する確率の上限値

        private const int FIRE_BALL_DAMAGE = 15; //火の玉の魔法のダメージ
        private const int THUNDER_DAMAGE = 30;   //雷の魔法のダメージ
        private const int F_BREATH_DAMAGE = 40;  //火炎噴射のダメージ

        private const int POISON_DAMAGE = 20;   //戦闘中の毒によるダメージ（１ターンごとに最大HPをこの定数で割った数だけダメージを受ける）

        public const int PATTERN_MAX = 4;   //攻撃手段最大値

        private const int CRITICAL_PAR_MAX = 100;    //痛恨の一撃が出る確率の計算に使用する乱数の最大値

        private const int AVOID_BASE_MIN = 5;   //攻撃回避の確率の計算に使用する基本値（最小）
        private const int AVOID_BASE_MAX = 60;  //攻撃回避の確率の計算に使用する基本値（最大）
        private const int AVOID_MAX = 100;      //攻撃回避の確率の計算に使用する最大値

        //フィールド
        private int id;                                 //ID
        private string name;                            //名前
        private int hpMax;                              //最大HP
        private int hp;                                 //HP
        private int attack;                             //攻撃力
        private int defense;                            //防御力
        private int speed;                              //素早さ
        private int luck;                               //運
        private ENEMY_CONDITION condition;              //状態
        private int ab_condition_turn;                  //状態異常になってから経過したターン
        private int exp;                                //倒すと得られる経験値
        private int gold;                               //倒すと得られるゴールド
        private int item_id;                            //倒すと得られるアイテムのID
        private string img;                             //画像ファイル名
        private bool npcFlag;                           //NPCフラグ（true:NPC、false:NPCでない）※trueの場合はプレイヤーが攻撃を仕掛けると敵になる
        private ENEMY_TYPE e_type;                      //敵の種類
        private int critical;                           //痛恨の一撃が出る確率
        private ENEMY_RESIST[] e_resist;                //敵の耐性
        private EnemyAttackPattern[] attack_pattern;    //攻撃手段

        //コンストラクタ
        public Enemy()
        {
            id = 0;
            name = "敵１";
            hpMax = 30;
            hp = hpMax;
            attack = 30;
            defense = 5;
            speed = 5;
            luck = 5;
            condition = ENEMY_CONDITION.OK;
            ab_condition_turn = 0;
            exp = 10;
            gold = 10;
            item_id = 0;
            img = "ev_enemy";
            npcFlag = false;
            e_type = ENEMY_TYPE.NORMAL;
            critical = 0;

            e_resist = new ENEMY_RESIST[(int)RESIST_TYPE.RESIST_MAX];

            for (int i = 0; i < (int)RESIST_TYPE.RESIST_MAX; i++)
            {
                e_resist[i] = ENEMY_RESIST.NORMAL;
            }

            attack_pattern = new EnemyAttackPattern[PATTERN_MAX];

            for (int i = 0; i < PATTERN_MAX; i++)
            {
                attack_pattern[i] = new EnemyAttackPattern();
            }
        }

        //プロパティ

        //ID
        public int enemyId
        {
            set { id = value; }
            get { return id; }
        }

        //名前
        public string enemyName
        {
            set { name = value; }
            get { return name; }
        }

        //最大HP
        public int enemyHpMax
        {
            set { hpMax = value; }
            get { return hpMax; }
        }

        //HP
        public int enemyHp
        {
            set { hp = value; }
            get { return hp; }
        }

        //攻撃力
        public int enemyAttack
        {
            set { attack = value; }
            get { return attack; }
        }

        //防御力
        public int enemyDefense
        {
            set { defense = value; }
            get { return defense; }
        }

        //素早さ
        public int enemySpeed
        {
            set { speed = value; }
            get { return speed; }
        }

        //運
        public int enemyLuck
        {
            set { luck = value; }
            get { return luck; }
        }

        //状態
        public ENEMY_CONDITION enemyCondition
        {
            set { condition = value; }
            get { return condition; }
        }

        //状態異常になってから経過したターン
        public int abConditionTurn
        {
            set { ab_condition_turn = value; }
            get { return ab_condition_turn; }
        }

        //倒すと得られる経験値
        public int enemyExp
        {
            set { exp = value; }
            get { return exp; }
        }

        //倒すと得られるゴールド
        public int enemyGold
        {
            set { gold = value; }
            get { return gold; }
        }

        //倒すと得られるアイテムのID
        public int enemyItemId
        {
            set { item_id = value; }
            get { return item_id; }
        }

        //画像ファイル名
        public string enemyImg
        {
            set { img = value; }
            get { return img; }
        }

        //NPCフラグ
        public bool enamyNpcFlag
        {
            set { npcFlag = value; }
            get { return npcFlag; }
        }

        //敵の種類
        public ENEMY_TYPE enemyType
        {
            set { e_type = value; }
            get { return e_type; }
        }

        //痛恨の一撃が出る確率
        public int enemyCritical
        {
            set { critical = value; }
            get { return critical; }
        }

        //敵の耐性
        public ENEMY_RESIST[] enemyResist
        {
            get { return e_resist; }
            set { e_resist = value; }
        }

        //攻撃手段
        public EnemyAttackPattern[] attackPattern
        {
            get { return attack_pattern; }
            set { attack_pattern = value; }
        }

        //敵のデータを受け取るメソッド
        //引数
        //enemy:受け取る敵のデータ
        public void EnemyDataSet(Enemy enemy)
        {
            id = enemy.enemyId;
            name = enemy.enemyName;
            hpMax = enemy.enemyHpMax;
            hp = hpMax;
            attack = enemy.enemyAttack;
            defense = enemy.enemyDefense;
            speed = enemy.enemySpeed;
            luck = enemy.enemyLuck;
            condition = ENEMY_CONDITION.OK;
            ab_condition_turn = 0;
            exp = enemy.enemyExp;
            gold = enemy.enemyGold;
            item_id = enemy.enemyItemId;
            img = enemy.enemyImg;
            npcFlag = enemy.enamyNpcFlag;
            e_type = enemy.enemyType;
            critical = enemy.enemyCritical;

            for (int i = 0; i < (int)RESIST_TYPE.RESIST_MAX; i++)
            {
                e_resist[i] = enemy.enemyResist[i];
            }

            for (int i = 0; i < PATTERN_MAX; i++)
            {
                attack_pattern[i].SetAttackPattern(enemy.attackPattern[i].attackMethod,
                                                   enemy.attackPattern[i].attackPercent);
            }
        }
        //敵の死亡チェックを行うメソッド
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

        //状態異常回復判定を行うメソッド
        //戻り値（true:回復成功、false:回復失敗）
        public bool ConditionRecoverCheck()
        {
            //乱数の取得
            int rnd = Random.Range(0, RECOVER_PAR_MAX);

            //状態異常回復率の初期化
            int per = (int)System.Math.Round(luck * 0.3f, 0, System.MidpointRounding.AwayFromZero);

            //状態異常回復率にそれぞれの状態に応じた基本値およびターン補正値を加算する
            if (condition == ENEMY_CONDITION.SLEEP)
            {
                //睡眠状態
                per += RECOVER_SLEEP;
                per += ab_condition_turn * RECOVER_SLEEP_CORRECTION;
            }
            else if (condition == ENEMY_CONDITION.SEALED)
            {
                //封印状態
                per += RECOVER_SEALED;
                per += ab_condition_turn * RECOVER_SEALED_CORRECTION;
            }
            else if (condition == ENEMY_CONDITION.POISON)
            {
                //毒状態
                per += RECOVER_POISON;
                per += ab_condition_turn * RECOVER_POISON_CORRECTION;
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

        //毒によって受けたダメージを返すメソッド
        //戻り値（毒によって受けたダメージ）
        public int PoisonDamage()
        {
            //毒によって受けたダメージの算出
            int damage = this.hpMax / POISON_DAMAGE;

            if (damage < 1)
            {
                //計算結果が1未満の時は1にする
                damage = 1;
            }

            //HPからダメージを引く
            this.hp = this.hp - damage;
            if (this.hp < 0)
            {
                //HPが0未満の時は0を設定
                this.hp = 0;
            }

            return damage;
        }

        //敵の攻撃手段の選択する（ランダム）メソッド
        //戻り値（選択された攻撃手段）
        public EnemyAttackPattern.ATTACK_METHOD GetAttackMethodRandom()
        {
            //乱数を取得する
            int rnd = Random.Range(0, EnemyAttackPattern.PERCENT_MAX);
            //最小値と最大値を普通の攻撃が出る確率判定用に設定する
            int per_min = 0;
            int per_max = attack_pattern[0].attackPercent;

            //普通の攻撃が出る確率判定
            if (rnd < per_max)
            {
                //普通の攻撃を返す
                return attack_pattern[0].attackMethod;
            }

            //特殊攻撃が出る確率判定（その敵が持つ全ての特殊攻撃で行う）
            for (int i = 1; i < PATTERN_MAX; i++)
            {
                //最小値と最大値をそれぞれの特殊攻撃が出る確率判定用に設定する
                per_min = per_max;
                per_max = per_min + attack_pattern[i].attackPercent;

                //該当する特殊攻撃を返す
                if (rnd >= per_min && rnd < per_max)
                {
                    return attack_pattern[i].attackMethod;
                }
            }

            //該当する特殊攻撃がない場合は普通の攻撃を返す
            return attack_pattern[0].attackMethod;
        }

        //特殊攻撃の成功率の算出を行うメソッド
        //引数
        //player:攻撃対象のプレイヤー
        //method:特殊攻撃の種類
        //戻り値（特殊攻撃成功率）
        public int GetEffectPercent(PlayerClass.Player player, EnemyAttackPattern.ATTACK_METHOD method)
        {
            //成功率を表す変数の初期化
            int per = (luck / 2) - (player.playerLuck / 2);

            if (method == EnemyAttackPattern.ATTACK_METHOD.SLEEP)
            {
                //眠りの魔法

                //成功率を算出する
                per += SLEEP_PAR_BASE;

                //プレイヤーが祝福状態の時で敵が最終ボスではない時は成功率を半減させる
                if (player.playerCondition == PlayerClass.Player.PLAYER_CONDITION.BLESSING && 
                    e_type != ENEMY_TYPE.FINAL_BOSS)
                {
                    per /= 2;
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
            else if (method == EnemyAttackPattern.ATTACK_METHOD.SEALED)
            {
                //封印の魔法
                
                //成功率を算出する
                per += SEALED_PAR_BASE;

                //プレイヤーが祝福状態の時で敵が最終ボスではない時は成功率を半減させる
                if (player.playerCondition == PlayerClass.Player.PLAYER_CONDITION.BLESSING && 
                    e_type != ENEMY_TYPE.FINAL_BOSS)
                {
                    per /= 2;
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
            else if (method == EnemyAttackPattern.ATTACK_METHOD.DEATH_MAGIC)
            {
                //死の魔法

                //成功率を算出する
                per += DEATH_PAR_BASE;

                //プレイヤーが祝福状態の時で敵が最終ボスではない時は成功率を半減させる
                if (player.playerCondition == PlayerClass.Player.PLAYER_CONDITION.BLESSING && 
                    e_type != ENEMY_TYPE.FINAL_BOSS)
                {
                    per /= 2;
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
            else if (method == EnemyAttackPattern.ATTACK_METHOD.POISON_SPRAY)
            {
                //毒液噴射

                //成功率を算出する
                per += POISON_PAR_BASE;

                //プレイヤーが祝福状態の時で敵が最終ボスではない時は成功率を半減させる
                if (player.playerCondition == PlayerClass.Player.PLAYER_CONDITION.BLESSING && 
                    e_type != ENEMY_TYPE.FINAL_BOSS)
                {
                    per /= 2;
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
            else if (method == EnemyAttackPattern.ATTACK_METHOD.DEATH_SICKLE)
            {
                //死神の鎌

                //成功率を算出する
                per += D_SICKLE_PAR_BASE;

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
                //該当する攻撃手段がない時は-1を入れる
                per = -1;
            }

            return per;
        }

        //敵の攻撃魔法のダメージを返すメソッド
        //引数
        //player:攻撃対象のプレイヤー
        //method:攻撃魔法の種類
        //戻り値（攻撃魔法のダメージ）
        public int GetMagicDamage(PlayerClass.Player player, EnemyAttackPattern.ATTACK_METHOD method)
        {
            int damage;

            //ダメージの基本値を取得
            if (method == EnemyAttackPattern.ATTACK_METHOD.FIRE_BALL)
            {
                //火の玉の魔法
                damage = (int)FIRE_BALL_DAMAGE;
            }
            else if (method == EnemyAttackPattern.ATTACK_METHOD.THUNDER)
            {
                //雷の魔法
                damage = (int)THUNDER_DAMAGE;
            }
            else if (method == EnemyAttackPattern.ATTACK_METHOD.FIRE_BREATH)
            {
                //火炎噴射
                damage = (int)F_BREATH_DAMAGE;
            }
            else
            {
                //該当する魔法がないときは0を返す
                return 0;
            }

            //乱数の取得
            float rnd = Random.Range(-20.0f, 20.0f);
            //乱数を加算することにより、ダメージにランダム性を持たせる
            int correct = (int)(damage * rnd / 100.0f);
            damage = damage + correct;

            //プレイヤーが祝福状態の時で敵が最終ボスではない時はダメージを軽減させる（ダメージを3分の2にする）
            if (player.playerCondition == PlayerClass.Player.PLAYER_CONDITION.BLESSING &&　
                e_type != ENEMY_TYPE.FINAL_BOSS)
            {
                damage = damage * 2 / 3;
            }

            if (damage < 0)
            {
                //ダメージが0未満の時は0を返す
                damage = 0;
            }

            return damage;
        }

        //敵が使用する回復魔法のメソッド
        public void HealMagic()
        {
            //敵のHPを回復する(現在のHPに最大HPの3分の1を加算する)
            hp += hpMax / 3;

            //最大HPを超えないようにする
            if (hp >= hpMax)
            {
                hp = hpMax;
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

        //敵のHPを0にするメソッド
        public void EnemyKill()
        {
            this.hp = 0;
        }

        //状態の変更を行うメソッド
        //引数
        //condition:変更する状態
        public void ChangeCondition(ENEMY_CONDITION condition)
        {
            this.condition = condition;
        }

        //痛恨の一撃が出たかどうかを判定するメソッド
        //戻り値（true:痛恨の一撃、false:通常攻撃）
        public bool CriticalAttackCheck()
        {
            //乱数を取得
            int rnd = Random.Range(0, CRITICAL_PAR_MAX);

            if (rnd < critical)
            {
                //痛恨の一撃
                return true;
            }
            else
            {
                //通常攻撃
                return false;
            }
        }

        //プレイヤーの攻撃をかわすかどうかを判定するメソッド
        //引数
        //player:戦闘対象のプレイヤー
        //戻り値（true:かわす、false:かわさない）
        public bool AttackAvoidCheck(PlayerClass.Player player)
        {
            if (condition == ENEMY_CONDITION.SLEEP)
            {
                //睡眠状態の時は回避失敗
                return false;
            }

            //回避率の取得
            //int avoid_par = Mathf.RoundToInt((speed - player.playerSpeed) / 10.0f);
            float f_avoid = (speed * 0.8f) - (player.playerSpeed * 0.4f) + (luck * 0.3f) - (player.playerLuck * 0.2f);
            //float f_avoid = (speed * 0.8f) - (player.playerSpeed * 0.4f) + (luck * 0.2f) - (player.playerLuck * 0.1f);
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
    }
}

