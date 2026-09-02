using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Common;
using PlayerClass;
using EnemyClass;

//敵のデータ読込および出現時の処理を行うクラス
public class EnemyGenerator : MonoBehaviour
{
    //敵データの見出しの列挙型
    public enum ENEMY_HEADING
    {
        ID,             //ID
        NAME,           //名前
        HPMAX,          //最大HP
        ATTACK,         //攻撃力
        DEFENSE,        //防御力
        SPEED,          //素早さ
        LUCK,           //運
        EXP,            //倒したときに得られる経験値
        GOLD,           //倒したときに得られるゴールド
        ITEM_ID,        //倒したときに得られるアイテムのID
        IMG,            //画像ファイル名
        NPCFLAG,        //NPCフラグ
        ENEMY_TYPE,     //敵の種類
        CRITICAL,       //痛恨の一撃が出る確率
        RESIST_01,      //耐性1（攻撃魔法）
        RESIST_02,      //耐性2（毒）
        RESIST_03,      //耐性3（眠り）
        RESIST_04,      //耐性4（封印）
        RESIST_05,      //耐性5（即死）
        PATTERN_01,     //攻撃手段名1
        PERCENT_01,     //攻撃手段1が出る確率
        PATTERN_02,     //攻撃手段名2
        PERCENT_02,     //攻撃手段2が出る確率
        PATTERN_03,     //攻撃手段名3
        PERCENT_03,     //攻撃手段3が出る確率
        PATTERN_04,     //攻撃手段名4
        PERCENT_04      //攻撃手段4が出る確率
    }

    //攻撃を受けた時に会話する敵のID
    public enum F_ENEMY_TALK
    {
        NPC1 = 45,           //冒険者
        NPC2 = 46,           //鍵売りの老人
        NPC3 = 47,           //ミストリル鉱石の情報を持つ冒険者
        NPC4 = 48,           //地下3階の衛兵
        NPC5 = 49,           //地下3階の冒険者
        NPC6 = 50,           //雷の杖を持っている魔法使い
        NPC7 = 51,           //謎の人物
        SUCCUBUS = 42,       //サキュバス
        DEATH = 43,          //死神
        LAST_BOSS = 44,      //魔王
        F_PRINCESS = 52,     //姫（偽者）
        PRINCESS = 53        //姫
    }

    private List<Enemy> enemyList;      //全敵データリスト
    private TextAsset csvEnemyFile;     //敵データのCSVファイル
    private List<string[]> enemyDatas;  //敵データのCSVの中身を入れるリスト

    private const int ENCOUNT_MAX = 10000;  //敵の出現率計算に使用する最大値

    private const int STEPS_MIN = 16;       //敵の出現率計算に使用するプレイヤーの歩数の最小値（この歩数までは絶対に出ない）
    private const int STEPS_MAX = 56;       //敵の出現率計算に使用するプレイヤーの歩数の最大値（この歩数で必ず出る）
    private const float CURVE_POWER = 2.0f; //敵の出現率カーブを調整する数値（1:線形, 2:後半に急上昇）

    void Awake()
    {
        //敵データファイルの読込
        EnemyFileRead();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //指定した敵データを渡す関数
    //引数
    //id:対象の敵ID
    //戻り値（指定した敵データ）
    public Enemy GetEnemy(int id)
    {
        Enemy e = new Enemy();

        e = enemyList[id - 1];

        return e;
    }

    //敵データファイルの読込を行う関数
    void EnemyFileRead()
    {
        //敵データファイルを読み込む
        csvEnemyFile = Resources.Load(GlobalConst.DATA_DIR + "enemyfile") as TextAsset;
        StringReader readerEnemy = new StringReader(csvEnemyFile.text);
        enemyDatas = new List<string[]>();

        //ファイルの中身をリストに入れる
        while (readerEnemy.Peek() != -1)
        {
            string line = readerEnemy.ReadLine();
            enemyDatas.Add(line.Split(','));
        }

        //全敵データリスト初期化
        enemyList = new List<Enemy>();

        //リストのデータを全敵データリストに入れる
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            Enemy e = new Enemy();

            e.enemyId = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.ID]);
            e.enemyName = enemyDatas[i][(int)ENEMY_HEADING.NAME];
            e.enemyHpMax = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.HPMAX]);
            e.enemyHp = e.enemyHpMax;
            e.enemyAttack = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.ATTACK]);
            e.enemyDefense = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.DEFENSE]);
            e.enemySpeed = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.SPEED]);
            e.enemyLuck = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.LUCK]);
            e.enemyCondition = Enemy.ENEMY_CONDITION.OK;
            e.abConditionTurn = 0;
            e.enemyExp = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.EXP]);
            e.enemyGold = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.GOLD]);
            e.enemyItemId = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.ITEM_ID]);
            e.enemyImg = enemyDatas[i][(int)ENEMY_HEADING.IMG];
            //NPCフラグはリストのデータが0の時はfalseを、1の時はtrueを入れる
            if (enemyDatas[i][(int)ENEMY_HEADING.NPCFLAG] == "0")
            {
                e.enamyNpcFlag = false;
            }
            else
            {
                e.enamyNpcFlag = true;
            }
            e.enemyType = (Enemy.ENEMY_TYPE)System.Enum.Parse(typeof(Enemy.ENEMY_TYPE),
                                                enemyDatas[i][(int)ENEMY_HEADING.ENEMY_TYPE], true);
            e.enemyCritical = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.CRITICAL]);

            e.enemyResist[(int)Enemy.RESIST_TYPE.ATTACK_MAGIC] = (Enemy.ENEMY_RESIST)System.Enum.Parse(typeof(Enemy.ENEMY_RESIST),
                                                enemyDatas[i][(int)ENEMY_HEADING.RESIST_01], true);

            e.enemyResist[(int)Enemy.RESIST_TYPE.POISON] = (Enemy.ENEMY_RESIST)System.Enum.Parse(typeof(Enemy.ENEMY_RESIST),
                                               enemyDatas[i][(int)ENEMY_HEADING.RESIST_02], true);

            e.enemyResist[(int)Enemy.RESIST_TYPE.SLEEP] = (Enemy.ENEMY_RESIST)System.Enum.Parse(typeof(Enemy.ENEMY_RESIST),
                                               enemyDatas[i][(int)ENEMY_HEADING.RESIST_03], true);

            e.enemyResist[(int)Enemy.RESIST_TYPE.SEALED] = (Enemy.ENEMY_RESIST)System.Enum.Parse(typeof(Enemy.ENEMY_RESIST),
                                               enemyDatas[i][(int)ENEMY_HEADING.RESIST_04], true);

            e.enemyResist[(int)Enemy.RESIST_TYPE.DEATH] = (Enemy.ENEMY_RESIST)System.Enum.Parse(typeof(Enemy.ENEMY_RESIST),
                                               enemyDatas[i][(int)ENEMY_HEADING.RESIST_05], true);

            e.attackPattern[0].attackMethod = (EnemyAttackPattern.ATTACK_METHOD)System.Enum.Parse(typeof(EnemyAttackPattern.ATTACK_METHOD),
                                                enemyDatas[i][(int)ENEMY_HEADING.PATTERN_01], true);
            e.attackPattern[0].attackPercent = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.PERCENT_01]);

            e.attackPattern[1].attackMethod = (EnemyAttackPattern.ATTACK_METHOD)System.Enum.Parse(typeof(EnemyAttackPattern.ATTACK_METHOD),
                                    enemyDatas[i][(int)ENEMY_HEADING.PATTERN_02], true);
            e.attackPattern[1].attackPercent = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.PERCENT_02]);

            e.attackPattern[2].attackMethod = (EnemyAttackPattern.ATTACK_METHOD)System.Enum.Parse(typeof(EnemyAttackPattern.ATTACK_METHOD),
                                    enemyDatas[i][(int)ENEMY_HEADING.PATTERN_03], true);
            e.attackPattern[2].attackPercent = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.PERCENT_03]);

            e.attackPattern[3].attackMethod = (EnemyAttackPattern.ATTACK_METHOD)System.Enum.Parse(typeof(EnemyAttackPattern.ATTACK_METHOD),
                                    enemyDatas[i][(int)ENEMY_HEADING.PATTERN_04], true);
            e.attackPattern[3].attackPercent = int.Parse(enemyDatas[i][(int)ENEMY_HEADING.PERCENT_04]);

            enemyList.Add(e);
        }
    }

    //出現する敵を決める（ランダム）処理を行う関数
    //引数
    //min:敵IDの最小値
    //max:敵IDの最大値
    //戻り値（出現する敵）
    public Enemy RandomGetEnemy(int min, int max)
    {
        Enemy e = new Enemy();
        //乱数の取得
        int rnd = Random.Range(min, max + 1);
        //指定された敵データの取得
        e = enemyList[rnd - 1];

        return e;
    }

    //ランダムエンカウント処理を行う関数
    //引数
    //min:敵IDの最小値
    //max:敵IDの最大値
    //per:敵が出現する確率
    //戻り値（出現する敵）
    public Enemy RandomEncount(int min, int max, int per)
    {
        //乱数の取得
        int rnd = Random.Range(0, ENCOUNT_MAX);

        if(rnd < per)
        {
            //敵が出現するときはどの敵は出現するかをランダムで選出する
            return RandomGetEnemy(min, max);
        }

        //敵が出現しないときはnullを返す
        return null;
    }

    //ランダムエンカウント処理を行う関数（歩数を使用）
    //引数
    //min:敵IDの最小値
    //max:敵IDの最大値
    //steps:プレイヤーの歩数
    //戻り値（出現する敵）
    public Enemy RandomEncount2(int min, int max, int steps)
    {
        //最小歩数未満なら出ない
        if (steps < STEPS_MIN)
        {
            //敵が出現しないときはnullを返す
            return null;
        }
            
        //最大歩数以上の時は確実に出る
        if (steps >= STEPS_MAX)
        {
            //敵が出現するときはどの敵は出現するかをランダムで選出する
            return RandomGetEnemy(min, max);
        }

        //確率計算
        float work = (float)(steps - STEPS_MIN) / (STEPS_MAX - STEPS_MIN);
        float per = Mathf.Pow(work, CURVE_POWER);

        //乱数が確率未満の時は敵が出現する
        if (Random.value < per)
        {
            //敵が出現するときはどの敵は出現するかをランダムで選出する
            return RandomGetEnemy(min, max);
        }

        //敵が出現しないときはnullを返す
        return null;
    }
}
