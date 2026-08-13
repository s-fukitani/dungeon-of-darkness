using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Common;
using ItemClass;

//アイテムのデータ読込および受け渡しの処理を行うクラス
public class ItemGenerator : MonoBehaviour
{
    //アイテム種類の列挙型
    public enum ITEM_TYPE
    {
        NONE,           //なし
        EQUIP,          //装備品開始（使用しない）
        WEAPON,         //武器
        ARMOR,          //鎧
        SHIELD,         //盾
        HELM,           //兜
        EQUIP_END,      //装備品終端（使用しない）
        NORMAL_ONCE,    //通常アイテム（１回）
        NORMAL,         //通常アイテム（無限）
        EVENT_ONCE,     //イベントアイテム（１回）
        EVENT,          //イベントアイテム（無限）
        MAP,            //地図
        MINI_MAP,       //小型の地図
        MITHRIL_ORE,    //ミストリル鉱石
        LANTHANUM,      //ランタン
        TRUTH_BANGLE    //真実の腕輪
    }

    //アイテムデータの見出しの列挙型
    public enum ITEM_HEADING
    {
        ID,               //アイテムID
        NAME,             //アイテム名
        TYPE,             //アイテムの種類
        WEAPON_ATTACK,    //武器攻撃力
        ARMOR_DEFENSE,    //防具防御力
        ITEM_RECOVER,     //道具回復力
        ITEM_ATTACK,      //道具攻撃力
        ITEM_EFFECT,      //道具特殊効果
        HP_COST,          //使用時の消費HP
        RECOVER_TYPE,     //回復状態異常種類
        DISCARD_FLAG,     //廃棄不可フラグ（true：廃棄不可、false：廃棄可能）
        BUY_PRICE,        //購入価格（非売品の場合は0）
        SALE_PRICE,       //売却価格（売却不可の場合は0）
        ITEM_IMG,         //アイテム画像
        ITEM_EXPLAIN      //アイテム説明文
    }

    //道具特殊効果の種類を表す列挙型
    public enum ITEM_EFFECT_TYPE
    {
        NONE,             //なし
        SLEEP,            //眠り
        SEALED,           //魔法封印
        POISON,           //毒
        DEATH,            //即死
        FINAL_SEALED,     //最終ボス魔法封印
        POISON_ATTACK     //攻撃による毒
    }

    //攻撃魔法アイテムの種類を表す列挙型
    public enum ITEM_ATTACK
    {
        MAGIC_WAND = 31,    //魔法の杖
        THUNDER_STAFF = 34  //雷の杖
    }

    //戦闘で使用しないイベントアイテムのID
    public enum EVITEM_NONE_FIGHT
    {
        KEY1 = 29,         //銀のカギ
        KEY2 = 40,         //金のカギ
        KEY3 = 46,         //勇者の紋章
        KEY4 = 45,         //悪魔の像
        MAP = 39,          //魔法の地図
        ORE = 41,          //ミストリル鉱石
        RETURN = 36,       //扉の絵
        CRYSTAL = 38,      //水晶玉
        LANTHANUM = 42,    //魔法のランタン
        MIRROR = 43,       //解呪の手鏡
        BANGLE = 44,       //真実の腕輪
        FLOWER = 48        //命の花
    }

    //戦闘で使用するイベントアイテムのID
    public enum EVITEM_FIGHT
    {
        WEAPON1 = 8,       //勇者の剣
        ARMOR1 = 14,       //勇者の鎧
        SHIELD1 = 19,      //勇者の盾
        HELM1 = 24,        //勇者の兜
        WAND1 = 34,        //雷の杖
        D_NECKLACE = 35,   //死の首飾り
        RING = 37          //姫の指輪
    }

    private List<Item> itemList;        //全アイテムデータリスト
    private TextAsset csvItemFile;      //アイテムデータのCSVファイル
    private List<string[]> itemDatas;   //アイテムデータのCSVの中身を入れるリスト

    void Awake()
    {
        //アイテムデータファイルの読込
        ItemFileRead();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //アイテムデータファイルの読込を行う関数
    void ItemFileRead()
    {
        //アイテムデータファイルを読み込む
        itemDatas = new List<string[]>();
        csvItemFile = Resources.Load(GlobalConst.DATA_DIR + "itemfile") as TextAsset;
        StringReader readerItem = new StringReader(csvItemFile.text);

        //ファイルの中身をリストに入れる
        while (readerItem.Peek() != -1)
        {
            string line = readerItem.ReadLine();
            itemDatas.Add(line.Split(','));
        }

        //全アイテムデータリスト初期化
        itemList = new List<Item>();

        //リストのデータを全アイテムデータリストに入れる
        for (int i = 0; i < itemDatas.Count; i++)
        {
            Item item = new Item();

            item.itemId = int.Parse(itemDatas[i][(int)ITEM_HEADING.ID]);
            item.itemName = itemDatas[i][(int)ITEM_HEADING.NAME];
            item.itemType = (ITEM_TYPE)(int.Parse(itemDatas[i][(int)ITEM_HEADING.TYPE]));
            item.weaponAttack = int.Parse(itemDatas[i][(int)ITEM_HEADING.WEAPON_ATTACK]);
            item.armorDefense = int.Parse(itemDatas[i][(int)ITEM_HEADING.ARMOR_DEFENSE]);
            item.itemRecover = int.Parse(itemDatas[i][(int)ITEM_HEADING.ITEM_RECOVER]);
            item.itemAttack = int.Parse(itemDatas[i][(int)ITEM_HEADING.ITEM_ATTACK]);
            item.itemEffect = (ITEM_EFFECT_TYPE)(int.Parse(itemDatas[i][(int)ITEM_HEADING.ITEM_EFFECT]));
            item.hpCost = int.Parse(itemDatas[i][(int)ITEM_HEADING.HP_COST]);
            item.recoverType = (ITEM_EFFECT_TYPE)(int.Parse(itemDatas[i][(int)ITEM_HEADING.RECOVER_TYPE]));
            //廃棄不可フラグはリストのデータが0の時はfalseを、1の時はtrueを入れる
            if (int.Parse(itemDatas[i][(int)ITEM_HEADING.DISCARD_FLAG]) == 0)
            {
                item.dicardFlag = false;
            }
            else
            {
                item.dicardFlag = true;
            }
            item.buyPrice = int.Parse(itemDatas[i][(int)ITEM_HEADING.BUY_PRICE]);
            item.salePrice = int.Parse(itemDatas[i][(int)ITEM_HEADING.SALE_PRICE]);
            item.itemImg = itemDatas[i][(int)ITEM_HEADING.ITEM_IMG];
            //説明文はテキスト表示時に改行が行われるようにする
            item.itemExplanation = Regex.Unescape(itemDatas[i][(int)ITEM_HEADING.ITEM_EXPLAIN]);

            itemList.Add(item);

        }
    }

    //指定されたIDのアイテム情報を返す関数
    //引数
    //id:アイテムID
    //戻り値（アイテム情報）
    public Item GetItemInfo(int id)
    {
        Item info = new Item();

        if (itemList.Count == 0)
        {
            //全アイテムデータリストのデータ数が0の時は、初期化した値を返す
            return info;
        }
        else
        {
            //全アイテムデータリストのデータ数が0でない時は、指定されたIDで検索する
            foreach (Item item in itemList)
            {
                if (id == item.itemId)
                {
                    //IDに合致するアイテムデータがあった場合はそのデータを返し、
                    //見つからなかった場合は初期化した値を返す
                    info = item;
                    break;
                }
            }
            return info;
        }
    }

    //全アイテムデータリストを渡す関数
    //戻り値（全アイテムデータリスト）
    public List<Item> GetItemList()
    {
        return itemList;
    }

    //装備種類番号を返す関数
    //引数
    //type:アイテム種類の列挙型
    //戻り値（装備種類番号）
    public int GetEquipNumber(ITEM_TYPE type)
    {
        return (int)(type - ITEM_TYPE.EQUIP) - 1;
    }

}
