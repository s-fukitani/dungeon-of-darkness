using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Common;
using PlayerClass;

//ダンジョンを作成するクラス
public class MapGenerator : MonoBehaviour
{
    //ダンジョンパーツの列挙型
    public enum MAPINFO
    {
        NOTHING,        //なし
        OUTERWALL,      //外壁
        INNERWALL,      //内壁
        DOOR,           //扉
    }

    private TextAsset csvMapFile;                                       //ダンジョンのマップデータのCSVファイル
    private List<List<string[]>> mapDatas = new List<List<string[]>>(); //ダンジョンのマップデータのCSVの中身を入れるリスト
    public GameObject wallPrefab;                                       //ダンジョンの内壁のプレハブ
	public GameObject defaultWallPrefab;                                //ダンジョンの外壁のプレハブ
    public GameObject doorPrefab;                                       //ダンジョンの扉のプレハブ
    public GameObject ceilingPrefab;                                    //ダンジョンの天井のプレハブ                                    
    public GameObject floorPrefab;                                      //ダンジョンの床のプレハブ
    private int map_x_max;                                              //ダンジョンのX座標の最大値
    private int map_z_max;                                              //ダンジョンのZ座標の最大値
    public GameObject playerPrefab;                                     //プレイヤーのプレハブ

    void Awake()
    {
        //プレイヤーデータの取得
        Player pl = GameDataController.GetPlayerData();

        if (pl == null)
        {
            //プレイヤーデータが取得されなかった場合、プレイヤーデータを作成する
            pl = new Player();
            pl.NowFloorChange(1);
            pl.NowPositionSet(1, 24);
            pl.NowDirectionSet(Player.DIRECTION.NORTH);
        }

        //ファイルを読み込む
        ReadMapFile();
        //マップ生成する
        CreateMap(pl.nowFloor);
        //プレイヤーを設置する
        PlayerSet(pl.nowPosX, pl.nowPosZ, pl.nowDirection);
    }

    // Use this for initialization
    void Start()
	{

	}

    //マップファイルを読み込む関数
    void ReadMapFile()
    {
        //全フロア分のマップファイルを読み込む
        for (int i = 1; i <= GlobalConst.FLOOR_MAX; i++)
        {
            //1フロアずつファイルを読み込む
            string strFloor = GlobalConst.GetFloorString(i);
            csvMapFile = Resources.Load(GlobalConst.DATA_DIR + "mapfile" + strFloor) as TextAsset;
            StringReader reader = new StringReader(csvMapFile.text);
            //1フロアずつリストを初期化する
            mapDatas.Add(new List<string[]>());

            //ファイルの中身をリストに入れる
            while (reader.Peek() != -1)
            {
                string line = reader.ReadLine();
                mapDatas[i - 1].Add(line.Split(','));
            }
        }
    }

	//ダンジョンを作る関数
    //引数
    //f:指定フロア
	public void CreateMap(int f)
	{
        //天井プレハブをインスタンスを作成
        Instantiate(ceilingPrefab, new Vector3(12.5f, 1.0f, 12.5f), Quaternion.identity);

        //Z座標の最大値の取得
        map_z_max = mapDatas[f - 1].Count - 1;

        //ダンジョンの作成
        for (int z = 0; z < mapDatas[f - 1].Count; z++)
        {
            //X座標の最大値の取得
            map_x_max = mapDatas[f - 1][z].Length - 1;

            for (int x = 0; x < mapDatas[f - 1][z].Length; x++)
            {
                //現在座標のマップ情報を取得する
                int x_map = int.Parse(mapDatas[f - 1][z][x]);

                //情報によって作成するプレハブのインスタンスを決める
                if (x_map == (int)MAPINFO.OUTERWALL)
                {
                    //外壁
                    Instantiate(defaultWallPrefab, new Vector3(x, 0, map_z_max - z), Quaternion.identity);
                }
                else if (x_map == (int)MAPINFO.INNERWALL)
                {
                    //内壁
                    Instantiate(wallPrefab, new Vector3(x, 0, map_z_max - z), Quaternion.identity);
                }
                else if (x_map == (int)MAPINFO.DOOR)
                {
                    //扉
                    Instantiate(doorPrefab, new Vector3(x, 0, map_z_max - z), Quaternion.identity);
                }

            }
        }

        //床プレハブをインスタンスを作成
        Instantiate(floorPrefab, new Vector3(12.5f, -0.5f, 12.5f), Quaternion.identity);
    }

    //プレイヤーを設置する関数
    //引数
    //x:X座標
    //z:Z座標
    //dir:プレイヤーの向き
    public void PlayerSet(int x, int z, Player.DIRECTION dir)
    {
        //プレイヤーオブジェクトを取得する
        GameObject plobj = GameObject.FindGameObjectWithTag("PlayerCharacter");

        if (plobj == null)
        {
            //プレイヤーが存在しないときは、プレイヤープレハブのインスタンスを作成する
            Instantiate(playerPrefab, new Vector3((float)x, 0, (float)(map_z_max - z)), Quaternion.Euler(0, (int)dir * 90, 0));
        }
    }

    //ダンジョンのX座標の最大値を渡す関数
    //戻り値:X座標の最大値
    public int GetMapXMax()
    {
        return map_x_max;
    }

    //ダンジョンのZ座標の最大値を渡す関数
    //戻り値:Z座標の最大値
    public int GetMapZMax()
    {
        return map_z_max;
    }

    //指定フロア、指定座標のマップ情報を渡す関数
    //引数
    //f:フロア
    //x:X座標
    //z:Z座標
    public int MapInfoGet(int f, int x, int z)
    {
        return int.Parse(mapDatas[f - 1][z][x]);
    }

    //ダンジョンの初期化を行う関数
    public void ClearMap()
    {
        //ダンジョンの全オブジェクトを取得する
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        GameObject[] dWalls = GameObject.FindGameObjectsWithTag("DWall");
        GameObject[] doorWalls = GameObject.FindGameObjectsWithTag("DoorWall");
        GameObject ceiling = GameObject.FindGameObjectWithTag("Ceiling");
        GameObject floor = GameObject.FindGameObjectWithTag("Floor");

        //天井を消去する
        Destroy(ceiling);

        //内壁を消去する
        foreach (GameObject wall in walls)
        {
            Destroy(wall);
        }

        //外壁を消去する
        foreach (GameObject dWall in dWalls)
        {
            Destroy(dWall);
        }

        //扉を消去する
        foreach (GameObject doorWall in doorWalls)
        {
            Destroy(doorWall);
        }

        //床を消去する
        Destroy(floor);
    }

    //ダンジョンを再度作る関数
    //引数
    //f:指定フロア
    public void ReCreateMap(int f)
    {
        //ダンジョンを初期化する
        ClearMap();
        //ダンジョンを作成する
        CreateMap(f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
