# 自作ゲームフォルダ
## 作者について
以前にゲーム業界でゲームプログラマをしていました。<BR>
制作したジャンル（業務以外の物も含む）はシューティングゲーム、パズルゲーム、<BR>
アクションゲーム、ノベル形式のアドベンチャーゲームです。

### 制作した作品
#### １．DungeonOfDarkness（ダンジョンオブダークネス）

魔物達の徘徊する地下迷宮に単身で乗り込み、<BR>
さらわれた姫を助け、魔物達を支配する魔王を倒すのが目的の<BR>
ロールプレイングゲームです。<BR>

タイトル画面<BR>
![](素材/ゲーム画像/サムネイル/サムネイル1.png)<BR>
街画面<BR>
![](素材/ゲーム画像/サムネイル/サムネイル2.png)<BR>
戦闘画面<BR>
![](素材/ゲーム画像/サムネイル/サムネイル3.png)<BR>

- ジャンル：3DダンジョンRPG

- 制作理由
  <BR>　ゲーム開発の仕事に復帰したくなったため、自分のスキルをアピールする作品を作ろうと思いました。<BR>
  これまで2Dのゲームがメインだったことから、3Dゲームの制作スキルが乏しいと感じたので、スキル習得のために挑戦しました。<BR>
  RPGを選んだのは一番好きなジャンルだったからです。<BR>これまではグラフィック制作スキルやサウンド制作スキルがなかったことから、
  制作を躊躇していたのですが、<BR>フリー素材の充実や生成AIの発達によって素材の調達が以前より容易になったこともあり、挑戦してみました。<BR>

- こだわった点・苦労した点
  - 複数カメラの制御
    <BR>複数のカメラを操作することで、プレイヤーの主観視点と小マップの俯瞰視点を同時に実装可能にしました。<BR>
    それにより、初心者でも迷うことなくゲームを楽しめるようにしました。
  - エフェクト表現
    <BR>エフェクトアニメーションを入れることで臨場感あふれる戦闘シーンを実現しました。
  - ゲームバランスの調整
    <BR>敵の強さ、経験値、金銭等、快適かつやりごたえがあるようなゲームにするためのバランス調整に苦労しました。
  - 特に苦労した点
    <BR>３Dに不慣れだったため、モデルの作成、カメラの移動および回転等、3D関連のあらゆる面で苦労しました。<BR>
    Webページや参考書を見ながら制作しました。
  
- 開発ツール：Unity2020.3.49f1

- 開発言語：C#

- 対象：PC

- 使用素材（画像および音声はフリー素材のサイト様の物を使用しています）
  - 画像
    - 万屋＿絵師：https://scenery.booth.pm/
    - こがぶし：https://cogabushi.booth.pm/
    - 空想曲線：https://kopacurve.blog.fc2.com/
    - Unity Asset Store：https://assetstore.unity.com/ja-JP
    - ぴぽや倉庫：https://pipoya.net/sozai/
  - 音声
    - 魔王魂：https://maou.audio/
    - ユーフルカ：https://youfulca.com/
    - 甘茶の音楽工房：https://amachamusic.chagasi.com/
    - DOVA-SYNDROME：https://dova-s.jp/
    - Pixabay：https://pixabay.com/sound-effects/
    - ハシマミ：https://hashimamiweb.com/
    - 効果音ラボ：https://soundeffect-lab.info/
    - PocketSound：https://pocket-se.info/
    - OtoLogic：https://otologic.jp/
    - スプリギン：https://www.springin.org/sound-stock/
    - BGMと効果音・みんなの創作支援サイトＴスタ：https://tnosite.com/
  - 使用AI
    - Claude：シナリオ作成に使用
    - ChatGPT：シナリオおよび2D画像の作成に使用

- 実行ファイルのダウンロード
  - URL：https://xgf.nu/GZzKj
  - Password：1234

- フォルダ階層
  - [dungeon-of-darkness](./dungeon-of-darkness)：ゲームプロジェクト、ゲーム素材、ドキュメントが入っています
    - [ドキュメント](./dungeon-of-darkness/ドキュメント)：ゲーム制作に使用した文書が入っています
      - [イベント詳細](./dungeon-of-darkness/ドキュメント/イベント詳細)：各イベントの仕様書が入っています
      - [データ](./dungeon-of-darkness/ドキュメント/データ)：ゲームのデータ（Excelファイル）が入っています
        - [CSV](./dungeon-of-darkness/ドキュメント/データ/CSV)：ゲームのデータ（CSVファイル）が入っています
    - [プロジェクト](./dungeon-of-darkness/プロジェクト)：ゲームのプロジェクトが入っています
    - [素材](./dungeon-of-darkness/素材)：ゲームで使用した素材が入っています
      - [エフェクト](./dungeon-of-darkness/素材/エフェクト)：エフェクトアニメーションに使用する画像が入っています
      - [ゲームBGM](./dungeon-of-darkness/素材/ゲームBGM)：BGMが入っています
      - [ゲームSE](./dungeon-of-darkness/素材/ゲームSE)：効果音が入っています
      - [ゲーム画像](./dungeon-of-darkness/素材/ゲーム画像)：2D画像が入っています
        - [アイテム](./dungeon-of-darkness/素材/ゲーム画像/アイテム)：アイテムの画像が入っています
        - [イベント](./dungeon-of-darkness/素材/ゲーム画像/イベント)：迷宮内のイベント画像が入っています
        - [キャラクター](./dungeon-of-darkness/素材/ゲーム画像/キャラクター)：城や街に登場するキャラクターの画像が入っています
        - [サムネイル](./dungeon-of-darkness/素材/ゲーム画像/サムネイル)：READMEのサムネイル画像が入っています
        - [タイトルロゴ](./dungeon-of-darkness/素材/ゲーム画像/タイトルロゴ)：タイトルロゴが入っています
        - [マニュアル](./dungeon-of-darkness/素材/ゲーム画像/マニュアル)：説明書に使用した画像が入っています
        - [ラベル](./dungeon-of-darkness/素材/ゲーム画像/ラベル)：街画面の左上に表示する場所を示すラベルが入っています
        - [敵orNPC](./dungeon-of-darkness/素材/ゲーム画像/敵orNPC)：敵とノンプレイヤーキャラクターの画像が入っています
        - [背景](./dungeon-of-darkness/素材/ゲーム画像/背景)：城や街、エンディングの画像が入っています
                    
          
