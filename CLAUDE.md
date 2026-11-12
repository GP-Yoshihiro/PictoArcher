# PictoArcher：Claude へのルール

3人の Claude が同じルールで動くための文書。Unity 6000.5.10f1（URP 2D）。
開発ルールの正本は `docs/development-rules.md`。仕様の一覧・工程・工数は `docs/PictoArcher_仕様書.xlsx`。

## 作業を始める前に（毎回・必ず）

3人とも Claude を使うので、同じ作業を2つの Claude が始めないように、コードやファイルを触る前に次を行う。

1. 次を実行して結果を見る。
   ```bash
   git fetch --prune
   gh issue list --state open
   gh pr list --state open
   git branch -r
   ```
2. 頼まれた作業の Issue を探す（タイトルは `[作業工程の ID] タスク名`）。
   - Issue の担当（assignee）がほかの人なら、作業を始めずに人に伝える。
   - 同じファイルを触る PR（下書きを含む）が開いているなら、作業を始めずに人に伝える。
   - Issue がないなら、作ってよいか人に聞く。
3. 自分の Issue の担当になってから、`main` から `<担当>/t<Issue番号>-<短い英語>` のブランチを切り、下書きの PR（本文に `Closes #<番号>` と触るファイル）を開く。push と PR の作成は人の OK をもらってから。
4. 仕様書「作業工程」シートの「先に要るもの」がマージ済みか確かめる。まだなら人に伝える。
5. 作業中は Issue に書いてあること以外をしない。担当外のファイル（ほかの人のプレハブ、`Run.unity` など）を直したくなったら、直さずに人に伝える。

1セッション＝1タスク。別のタスクに移るときは `/clear` してから。計画が要る作業は Plan Mode で計画を作り、人が読んでから進める。

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
| ボタン | ▶ 開始／⏸ 一時停止／↻ やり直し／🏠 ホーム／⚙ 設定／🔒 ロック中／🔊 音量 |
| 素材 | 取り込む前に文字が入っていないか確認する（UIパックの「PLAY」、看板、ロゴ、背景の張り紙や画面の表示など） |
| ウィンドウ・起動画面 | ウィンドウタイトルとアプリ名は OS が表示するもので画面内ではないため対象外。Unity のスプラッシュ画面は、出せない場合の扱いをチームで判断する |

- 開発ビルドやデバッグ表示でも文字を出さない。ログはコンソールだけ。
- 描画の基準解像度は 480×270。1ユニット＝16px（Assets PPU 16）。画像は Filter Mode を Point、圧縮なしで取り込む。
  - 注：設計書 第3版で画面は縦長（基準 270×480）に決まった。切り替えは計画2のタスク0（作業工程 `P2-00`、C さん）で行い、そのときこの行も直す。それが `main` に入るまでは今の 480×270 のまま作業し、縦長を前提にしたプレハブを作らない。
- コードの名前は世界観に依存させず、役割で付ける（弾は `Projectile`、追う敵は `Chaser`）。

## フォルダ構成

```
CLAUDE.md                  全員の Claude が読むルール
.claude/                   共有の Claude 設定（settings.json、skills/、agents/）
.github/                   PR と Issue のテンプレート
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
tools/spec/build_spec_xlsx.py  仕様書 .xlsx を作るスクリプト
docs/assets-list.md
docs/development-rules.md  開発ルールの正本
docs/PictoArcher_仕様書.xlsx  仕様書（スクリプトで作る。直接書き換えない）
docs/superpowers/          設計書（specs/）と計画（plans/）
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
- ブランチ名は `<担当>/t<Issue番号>-<短い名前>`（例 `a/t12-weapon-select`）。1タスク＝1 Issue＝1ブランチ。
- `main` へは直接入れない。ほかの1人が確認してからマージする。
- Claude が書いたコードも、マージ前に人が読む。
- PR は `.github/pull_request_template.md` の項目を埋める。コミットの種類、レビュー、マージの方法は `docs/development-rules.md` §6〜§8。
- 仕様を変えるときは、設計書（`.md`）と `tools/spec/build_spec_xlsx.py` を同じ PR で直し、`.xlsx` を作り直す（§10）。

## Claude の動かし方（ハイブリッド運用）

- メインのセッションは1タスクの計画・判断・統合をする。サブエージェントは同時に2〜3体まで。
  - `log-triage`（haiku）：長いログやテスト結果から失敗と原因だけを抜き出す。
  - `transcriber`（haiku）：計画にコードが全部書いてあるタスクを写して、テストして、コミットする。
  - `task-reviewer`（sonnet）：1タスクが終わるごとに、仕様に合っているかとコードの質を見る。
- 同じファイルを触るサブエージェントを同時に動かさない。
- スキル `minimal-read-edit`（必要な所だけ読んで直す）と `lean-token-workflow`（長いログやサブエージェントの使い方）を使う。プラグインは `superpowers`。
- フックは共有しない。
- UnityYAMLMerge は各自で設定する（`README.md` を見る）。
