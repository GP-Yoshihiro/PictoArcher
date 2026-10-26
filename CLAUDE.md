# PictoArcher：Claude へのルール

3人の Claude が同じルールで動くための文書。Unity 6000.5.10f1（URP 2D）。

## 文字ゼロのルール

画面に文字を出さない。設計書 §5 のとおり。

| 項目 | ルール |
|---|---|
| 禁止 | アルファベット・ひらがな・カタカナ・漢字すべて。`LV` `HP` `BOSS` `x2` `s`（秒）などの略語・単位も禁止 |
| 代わりの表記 | レベル `★5`／倍率 `×2`／時間 `⏱ 30`／割合 `+20%`／進行 `23/50` |
| 色の意味 | 赤＝危険・攻撃／緑＝回復／黄＝コイン・経験値／青＝防御・氷／紫＝レア。この5色は UI・拾うもの・エフェクトの意味だけに使い、敵の体の主な色やフィールドの照明には使わない（敵は灰・白・黒・茶・桃などから選ぶ。プレイヤーの弾は白） |
| 予兆 | 敵の攻撃は必ず先に赤い線・赤い円で予告する |
| 操作の案内 | 手・矢印キー・マウスの絵のアニメで見せる。キーの絵に英字（W A S D、Esc など）を描かない |
| 絵文字 | フォントで出さない。すべてスプライト画像（ドット絵）で用意する |
| フォント | TextMeshPro のフォントアセットには数字と記号だけを登録する。ドット絵に合うピクセルフォントを使う |
| ボタン | ▶ 開始／⏸ 一時停止／↻ やり直し／🏠 ホーム／⚙ 設定／🔒 ロック中／🔊 音量（すべてスプライト画像で出す） |
| 素材 | 取り込む前に文字が入っていないか確認する（UIパックの「PLAY」、看板、ロゴ、背景の張り紙や画面の表示など） |
| ウィンドウ・起動画面 | ウィンドウタイトルとアプリ名は OS が表示するもので画面内ではないため対象外。Unity のスプラッシュ画面は、出せない場合の扱いをチームで判断する |

- 開発ビルドやデバッグ表示でも文字を出さない。ログはコンソールだけ。
- 描画の基準解像度は 480×270。1ユニット＝16px（Assets PPU 16）。画像は Filter Mode を Point、圧縮なしで取り込む。
- コードの名前は世界観に依存させず、役割で付ける（弾は `Projectile`、追う敵は `Chaser`）。

## フォルダ構成

```
CLAUDE.md                  全員の Claude が読むルール
Assets/_Project/
  Scripts/Core/            PictoArcher.Core（純粋な C#、テスト対象）
    PictoArcher.Core.asmdef
    MoveVectorResolver.cs  移動ベクトルの計算
    TargetSelector.cs      一番近い敵を選ぶ
    FireTimer.cs           連射の間隔
    Health.cs              HP
    HitInvulnerability.cs  無敵時間
    WaveLayout.cs          50ウェーブの種類（WaveKind を含む）
    WaveProgress.cs        1ウェーブの敵の数と全滅
    WaveSequence.cs        今のウェーブ番号、休み、次のウェーブへ（WavePhase を含む）
  Scripts/Game/            PictoArcher.Game（MonoBehaviour と ScriptableObject）
    PictoArcher.Game.asmdef
    Data/                  PlayerStats, EnemyDefinition, WaveDefinition, SkillId, SkillDefinition
    IHittable.cs           弾や体当たりが当たる相手
    EnemyRegistry.cs       今いる敵の一覧
    Player/                PlayerController, PlayerShooter, PlayerHealth, Projectile
    Enemies/               EnemyActor, ChaseMovement, SpawnTelegraph
    Run/                   WaveSpawner, RunController
    UI/                    HudView, HeadHpView
  Scripts/Editor/          PictoArcher.Editor（エディタ専用）
    PictoArcher.Editor.asmdef
    PixelArtImportPostprocessor.cs  ドット絵の取り込み設定
  Tests/EditMode/          PictoArcher.Tests.EditMode
  Data/                    ScriptableObject のアセット（Enemies/, Waves/）
  Prefabs/                 MainCamera, Player, Projectile, Chaser, SpawnTelegraph, Field_Test
  Scenes/Run.unity         Cさんだけが編集する
  Scenes/Sandbox/          Sandbox_A.unity, Sandbox_B.unity（各自の試し用、他の人は触らない）
  Fonts/                   数字と記号だけのピクセルフォント
  Art/                     絵（フリー素材と、絵の担当から届いたスプライトシート）
tools/run-editmode-tests.sh
docs/assets-list.md
```

ルール：
- `Scripts/Core` は純粋な C#。`MonoBehaviour` や `UnityEngine` の部品を使わない。名前空間は `PictoArcher.Core`。
- Unity の部品（`MonoBehaviour`、`ScriptableObject`）は `Scripts/Game` に置く。名前空間は `PictoArcher.Game`。
- エディタ専用のコードは `Scripts/Editor` に置く。名前空間は `PictoArcher.Editor`。
- レイヤー：`Player`=6、`Enemy`=7、`PlayerShot`=8、`Wall`=9（障害物も `Wall`）。
- `MainCamera.prefab` の位置は (0, 0, -10) から動かさない。カメラは動かない。
- 数値（HP、攻撃力、速度、出現数）は ScriptableObject の調整値にする。コードに直接書かない。

## テストの書き方

- `Core` のルールは、先に EditMode テストを書いて失敗を確認してから実装する。
- NUnit を使う。置き場所は `Assets/_Project/Tests/EditMode/<クラス名>Tests.cs`。
- 実行方法：Unity のエディタを閉じて、`tools/run-editmode-tests.sh`。失敗があると終了コードが 0 以外になる。結果は `Logs/editmode-results.xml`。
- `NoTextGuardTests` を含む全テストが通ってからプルリクエストを出す。

## シーンとブランチ

- `Run.unity` を編集するのは Cさんだけ。ほかの人はプレハブと `Scenes/Sandbox/Sandbox_<担当>.unity` で作業する。
- ブランチ名は `<担当>/t<番号>-<短い名前>`（例 `c/t1-foundation`）。1タスク＝1ブランチ。
- `main` へは直接入れない。ほかの1人が確認してからマージする。
- Claude が書いたコードも、マージ前に人が読む。
- UnityYAMLMerge は各自で設定する（`README.md` を見る）。
