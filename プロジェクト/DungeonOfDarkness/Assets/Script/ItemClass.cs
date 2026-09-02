using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnemyClass;

//アイテム関連のネームスペース
namespace ItemClass
{
    //アイテムクラス
    public class Item
    {
        //定数
        public const int FULL_HEAL = 999;   //HP完全回復アイテムを使用したときのHP回復量

        //フィールド
        private int id;                                         //ID
        private string name;                                    //アイテム名
        private ItemGenerator.ITEM_TYPE type;                   //アイテム種類
        private int wep_attack;                                 //武器攻撃力
        private int arm_defense;                                //防具防御力
        private int item_recover;                               //道具回復力
        private int item_attack;                                //道具攻撃力
        private ItemGenerator.ITEM_EFFECT_TYPE item_effect;     //道具特殊効果
        private int hp_cost;                                    //使用時の消費HP
        private ItemGenerator.ITEM_EFFECT_TYPE recover_type;    //回復状態異常種類
        private bool discard_flag;                              //廃棄不可フラグ（true:廃棄不可、false:廃棄可能）
        private int buy_price;                                  //購入価格
        private int sale_price;                                 //売却価格
        private string img;                                     //アイテム画像
        private string explanation;                             //アイテム説明文

        //コンストラクタ
        public Item()
        {
            id = 0;
            name = "";
            type = ItemGenerator.ITEM_TYPE.NONE;
            wep_attack = 0;
            arm_defense = 0;
            item_recover = 0;
            item_attack = 0;
            item_effect = ItemGenerator.ITEM_EFFECT_TYPE.NONE;
            hp_cost = 0;
            recover_type = ItemGenerator.ITEM_EFFECT_TYPE.NONE;
            discard_flag = false;
            buy_price = 0;
            sale_price = 0;
            img = "";
            explanation = "";
        }

        //コンストラクタ（引数あり）
        //引数
        //id:ID
        //name:アイテム名
        //type:アイテム種類
        //img:アイテム画像
        //explanation:アイテム説明文
        //wep_attack:武器攻撃力
        //arm_defense:防具防御力
        //item_recover:道具回復力
        //item_attack:道具攻撃力
        //item_effect:道具特殊効果
        //hp_cost:使用時の消費HP
        //recover_type:回復状態異常種類
        //discard_flag:廃棄不可フラグ
        //buy_price:購入価格
        //sale_price:売却価格
        public Item(int id, string name, ItemGenerator.ITEM_TYPE type, string img, string explanation,
                    int wep_attack = 0, int arm_defense = 0, int item_recover = 0,
                    int item_attack = 0, ItemGenerator.ITEM_EFFECT_TYPE item_effect = ItemGenerator.ITEM_EFFECT_TYPE.NONE,
                    int hp_cost = 0, ItemGenerator.ITEM_EFFECT_TYPE recover_type = ItemGenerator.ITEM_EFFECT_TYPE.NONE,
                    bool discard_flag = false, int buy_price = 0, int sale_price = 0)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.wep_attack = wep_attack;
            this.arm_defense = arm_defense;
            this.item_recover = item_recover;
            this.item_attack = item_attack;
            this.item_effect = item_effect;
            this.hp_cost = hp_cost;
            this.recover_type = recover_type;
            this.discard_flag = discard_flag;
            this.buy_price = buy_price;
            this.sale_price = sale_price;
            this.img = img;
            this.explanation = explanation;
        }

        //プロパティ

        //ID
        public int itemId
        {
            set { id = value; }
            get { return id; }
        }

        //アイテム名
        public string itemName
        {
            set { name = value; }
            get { return name; }
        }

        //アイテム種類
        public ItemGenerator.ITEM_TYPE itemType
        {
            set { type = value; }
            get { return type; }
        }

        //武器攻撃力
        public int weaponAttack
        {
            set { wep_attack = value; }
            get { return wep_attack; }
        }

        //防具防御力
        public int armorDefense
        {
            set { arm_defense = value; }
            get { return arm_defense; }
        }

        //道具回復力
        public int itemRecover
        {
            set { item_recover = value; }
            get { return item_recover; }
        }

        //道具攻撃力
        public int itemAttack
        {
            set { item_attack = value; }
            get { return item_attack; }
        }

        //道具特殊効果
        public ItemGenerator.ITEM_EFFECT_TYPE itemEffect
        {
            set { item_effect = value; }
            get { return item_effect; }
        }

        //使用時の消費HP
        public int hpCost
        {
            set { hp_cost = value; }
            get { return hp_cost; }
        }

        //回復状態異常種類
        public ItemGenerator.ITEM_EFFECT_TYPE recoverType
        {
            set { recover_type = value; }
            get { return recover_type; }
        }

        //廃棄不可フラグ
        public bool dicardFlag
        {
            set { discard_flag = value; }
            get { return discard_flag; }
        }

        //購入価格
        public int buyPrice
        {
            set { buy_price = value; }
            get { return buy_price; }
        }

        //売却価格
        public int salePrice
        {
            set { sale_price = value; }
            get { return sale_price; }
        }

        //アイテム画像
        public string itemImg
        {
            set { img = value; }
            get { return img; }
        }

        //アイテム説明文
        public string itemExplanation
        {
            set { explanation = value; }
            get { return explanation; }
        }

        //攻撃アイテムのダメージを算出する関数
        //引数
        //enemy:攻撃対象の敵データ
        //戻り値（算出されたダメージ）
        public int AttackItemDamage(Enemy enemy)
        {
            //対象アイテムの道具攻撃力を取得
            int damage = item_attack;
            //ランダム性を持たせるため、乱数を加算する
            float rnd = Random.Range(-20.0f, 20.0f);
            int correct = (int)(damage * rnd / 100.0f);
            damage = damage + correct;

            //敵が持つ攻撃アイテムに対する耐性にに応じてダメージを修正する
            if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.ATTACK_MAGIC] == Enemy.ENEMY_RESIST.WEAK)
            {
                //弱い
                damage = (int)(damage * 1.5f);
            }
            else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.ATTACK_MAGIC] == Enemy.ENEMY_RESIST.STRONG)
            {
                //強い
                damage = damage / 2;
            }
            else if (enemy.enemyResist[(int)Enemy.RESIST_TYPE.ATTACK_MAGIC] == Enemy.ENEMY_RESIST.INVALID)
            {
                //無効
                damage = 0;
            }

            if (damage < 0)
            {
                //ダメージが0未満の時は0を入れる
                damage = 0;
            }

            return damage;
        }
    }
}
