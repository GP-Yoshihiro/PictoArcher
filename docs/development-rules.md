# PictoArcher 開発ルール

人も Claude も、このルールで動く。`CLAUDE.md` は Claude 向けの要約で、ここが正本。仕様書 `docs/PictoArcher_仕様書.xlsx` の「開発ルール」「Claude利用チェックリスト」シートはここの要約。食い違ったらこの文書が正しい。

## §1 何がどこにあるか

| もの | 場所 | 直す人 |
|---|---|---|
| ゲームの仕様（正本） | `docs/superpowers/specs/2026-10-31-pictoarcher-design-v3.md`、`2026-10-31-worldview-sf-final.md` | C さん（チームで決めたあと） |
| 仕様書（一覧・工程・工数） | `docs/PictoArcher_仕様書.xlsx`（`tools/spec/build_spec_xlsx.py` で作る） | C さん |
| 計画 | `docs/superpowers/plans/` | その計画を書く人 |
| タスクの進み具合（今の状態） | GitHub の Issue と PR | 各担当 |
| Claude のルール | `CLAUDE.md`、`.claude/` | 全員（PR で） |
| コーディング規則（正本） | `docs/superpowers/specs/2026-11-14-coding-rules-design.md` | C さん（チームで決めたあと） |
| コーディング規則のお手本 | `.claude/skills/pictoarcher-coding-rules/SKILL.md` | 全員（PR で） |
| Unity の落とし穴 | `docs/unity-pitfalls.md` | 全員（PR で） |
| 素材の出どころ | `docs/assets-list.md` | 素材を入れた人 |

今の状態は Issue と PR で見る。仕様書の「作業工程」シートは予定表で、状態の列は C さんが週に1回 Issue から写す。

## §2 命名

- 名前は世界観に依存させず、役割で付ける（弾は `Projectile`、追う敵は `Chaser`、ボスの体当たりは `Dash`）。
- 名前空間：`PictoArcher.Core`（純粋な C#）、`PictoArcher.Game`（MonoBehaviour・ScriptableObject）、`PictoArcher.Editor`（エディタ専用）、`PictoArcher.Tests.EditMode`・`PictoArcher.Tests.PlayMode`（テスト）。
- 名前空間はこれだけ。フォルダを分けても名前空間は分けない。
- 型・メソッド・プロパティ・イベント：PascalCase（`FireTimer`、`Tick`、`Interval`）。interface は `I` ＋ PascalCase（`ITickable`）。
- フィールド：private は `_camelCase`（`_elapsed`）。`private` は書かない（既定のまま）。
- `const` と `static readonly`：PascalCase（`TotalWaves`）。
- 引数・ローカル変数：camelCase（`deltaTime`）。
- ScriptableObject の調整値：`[SerializeField]` の `_camelCase` と、読み取り専用のプロパティ（`_moveSpeed`／`MoveSpeed`）。public のフィールドと public の setter は作らない。名前を変えるときは `[FormerlySerializedAs]` を付ける。
- イベント：過去形で `On` を付けない（`Died`、`WaveCleared`）。受けるメソッドは `Handle` ＋ イベント名（`HandleDied`）。
- 継承してよいのは骨組み（`SceneBase`、`DefinitionAsset<T>`、`DefinitionAsset<T>` を直接継承した抽象クラス）だけ。ほかのクラスはすべて `sealed`。
- 1ファイルに1つの型。ファイル名と型の名前を同じにする。
- 接尾辞：`XxxScene`（`SceneBase` の子）、`XxxSystem`（シーンが持つ処理、普通の C#）、`XxxView`（プレハブに付く MonoBehaviour）、`XxxDefinition`（ScriptableObject のデータ）、`XxxRelay`（Unity から来るものを受け取るだけ）、`XxxExtensions`（拡張メソッド）、`XxxId`（種類の enum）。
- テスト：`Assets/_Project/Tests/EditMode/<クラス名>Tests.cs`。メソッドは PascalCase で、条件と結果を `_` で区切ってよい（`NoTarget_DoesNotFire`）。
- アセット：プレハブは役割の名前（`Player`、`Projectile`、`Chaser`）。データは種類ごとのフォルダ（`Data/Player`、`Data/Enemies`、`Data/Waves`、`Data/Skills`、`Data/Obstacles`）。プレハブも種類ごと（`Prefabs/System`、`Prefabs/Player`、`Prefabs/Enemies` など）。絵は `Art/Characters`・`Art/Tiles`・`Art/Effects`・`Art/UI`、音は `Audio/BGM`・`Audio/SE`。直下に直接置かない。シーンの試し用は `Scenes/Sandbox/Sandbox_<担当>.unity`。
- 構成は Unity の標準に合わせる。自分たちのものはすべて `Assets/_Project` の下。ルールは Core（テストする）、つなぎは Game、数値は ScriptableObject。

## §2.1 設計の規則（8項目）

正本は設計書 `docs/superpowers/specs/2026-11-14-coding-rules-design.md`、お手本はスキル `pictoarcher-coding-rules`。多くは `ArchitectureGuardTests` が自動で確かめる。

1. 層と依存の向き：`Core`（純粋な C#）← `Game` ← `Editor`。Core で使ってよい UnityEngine のものは数学の型だけ。
2. 実行順：標準ライフサイクル（`Update` など）を書くのは `Main` だけ。`Main` → シーン（`SceneBase`）→ System の順に、ソースに書いた順で呼ぶ。`Time.` を読むのは `Main` だけ。待ち時間はタイマーで作る。
3. Unity が直接呼ぶもの：当たり判定は `CollisionRelay2D`、ボタンは `UiButtonRelay` が記録するだけで、処理は Tick の中。入力は Tick の中で読む。
4. 組み合わせで作る：継承は骨組みだけ、ほかは `sealed`。敵は定義（ScriptableObject）と動く部品（普通の C#）の組み合わせで作る。
5. データと状態を分ける：ScriptableObject は読み取り専用。実行中に変わる値は、データから作った普通の C# のオブジェクトが持つ。
6. やり取り：参照は上から渡す（コンストラクター、`Init`、インスペクター）。`Find`、`Camera.main`、シングルトン、static の書き換えられる値は禁止。下から上へはイベントか戻り値。
7. 名前とフォルダ：§2 のとおり。フォルダ構成は設計書 §10。
8. 古い計画のコード（`Update`、`OnTriggerEnter2D`、static の `EnemyRegistry`、public のフィールド）は写さず、この規則に読み替える。

ガードテストの例外を足すのは設計の変更なので、PR に理由を書く。

## §3 作業を始める前の確認（重複の防止）

3人とも Claude を使うので、同じタスクを2人（2つの Claude）が始める、同じファイルを別々に直す、ということが起きやすい。タスクを始めるたびに、次を上から確かめる。Claude にも同じ確認をさせる（`CLAUDE.md`「作業を始める前に」）。

```bash
git switch develop && git pull
gh issue list --state open
gh pr list --state open
git fetch --prune && git branch -r
```

1. 自分のタスクの Issue がある。なければ作る（§4）。
2. その Issue の担当（assignee）がだれもいない、または自分。ほかの人なら始めない。
3. 開いている PR（下書きを含む）に、同じファイルを触るものがない。あるなら、その人と Issue で順番を決める。
4. 仕様書「作業工程」シートの「先に要るもの」がマージ済み。
5. 自分を担当にしてから、ブランチを作り、すぐ下書きの PR を開く（§5、§7）。

同じ作業が重なったとき：

- 先に Issue の担当になった人が続ける。あとから気づいた人は手を止め、Issue にコメントして別のタスクに移る。
- 途中までのコードが役に立つなら、その差分を Issue に貼るか、相手のブランチに PR を出す（develop には入れない）。
- プレハブとシーンは2人で同時に直さない。

## §4 Issue

- 1タスク＝1 Issue。仕様書「作業工程」シートの1行が1 Issue になる。
- タイトル：`[作業工程の ID] タスク名`（例 `[P2-02] 強化の仕組み`）。
- 本文は Issue テンプレート（`.github/ISSUE_TEMPLATE/task.md`）を使う。触る予定のファイルを書く。
- 始める前に自分を担当にする：`gh issue edit <番号> --add-assignee @me`。
- 担当になったら、終わるか手放すまで持つ。手放すときは担当を外してコメントを残す。
- 仕様にないことを決めたら、その Issue に書く。仕様書への反映は C さんに頼む（§10）。
- 終わったら、かかった日数を Issue にコメントする（C さんが工数の実績に写す）。

## §5 ブランチ

- 日々の作業をまとめるのは `develop`。作業のブランチは `develop` から切り、PR も `develop` 向けに出す。
- `main` はリリースした版だけを置く。`develop` から `main` へ入れるのはリリースのときだけで、C さんが `release/` のブランチで行う。
- 1タスク＝1ブランチ。`develop` にも `main` にも直接コミットしない。
- 名前：`<プレフィックス>/t<Issue番号>-<短い英語>`（例 `feature/t12-weapon-select`、`docs/t20-assets-list`）。名前でどんな作業かわかるようにする。

| プレフィックス | 使うとき |
|---|---|
| `feature/` | 新機能の開発 |
| `fix/`（または `bugfix/`） | バグの修正 |
| `hotfix/` | 本番（リリースした版）の緊急修正。`main` から切り、`main` と `develop` の両方に入れる |
| `release/` | リリースに向けた最終調整・バージョンアップ |
| `refactor/` | 動きを変えずにコードを整理・改善 |
| `docs/` | 文書の修正・追加 |

- 長く持たない。2日に1回は `develop` を取り込む（`git merge origin/develop`）。

## §6 コミット

- 先頭に種類を付ける：`feat:`（機能）／`fix:`（直し）／`test:`（テストだけ）／`docs:`（文書）／`chore:`（設定など）。そのあとに英語の短い説明。
- 1コミット＝1つの意味のある変更。テストと実装は同じコミットでよい。
- Claude が書いたコミットには `Co-Authored-By:` の行が付く。消さない。

## §7 プルリクエスト

- ブランチを作ったら、すぐ下書きの PR を開く：`gh pr create --draft --base develop`。本文に `Closes #<Issue番号>` と触るファイルを書く。これで、ほかの人と Claude に「この作業は進行中」と伝わる。
- 本文は PR テンプレート（`.github/pull_request_template.md`）の項目を埋める。
- 全テスト（`NoTextGuardTests` を含む）が通ってから「レビュー待ち」にする：`gh pr ready`。
- レビューする人を1人指定する。
- 1つの PR は1タスク。関係ない直しを混ぜない。

## §8 レビューとマージ

- ほかの1人が読んで OK を出してからマージする。Claude が書いたコードも人が読む。
- 見るところ：仕様に合っているか、文字が画面に出ないか、数値がコードに直接書かれていないか、担当外のファイルを触っていないか、テストがあるか。
- マージは書いた本人が行う。方法は「Create a merge commit」。マージしたらブランチを消す：`gh pr merge --merge --delete-branch`。
- GitHub は既定のブランチ（`main`）へのマージでしか `Closes #<番号>` の Issue を閉じない。`develop` へマージしたら、`gh issue close <番号>` で Issue を閉じる。
- `Root.unity`・`Home.unity`・`Battle.unity`・`Result.unity`・`Pause.unity` を触る PR は C さんがレビューする。

## §9 Claude の使い方

近い考え方で動くように、`.claude/` をリポジトリで共有する。

- プラグイン：`superpowers`（`.claude/settings.json` で有効にする。初回は `/plugin` で入れる）。
- スキル：`minimal-read-edit`（必要な所だけ読んで直す）、`lean-token-workflow`（長いログやサブエージェントの使い方）。
- サブエージェント（`.claude/agents/`）：

| 役 | 使い方 | モデル |
|---|---|---|
| メインのセッション | 1タスクの計画、判断、統合 | 既定 |
| `log-triage` | 長いログやテスト結果から、失敗と原因だけを抜き出す | haiku |
| `transcriber` | 計画にコードが全部書いてあるタスクを写して、テストして、コミットする | haiku |
| `task-reviewer` | 1タスクが終わるごとに、仕様に合っているかとコードの質を見る | sonnet |

運用（ハイブリッド）：

- 1セッション＝1タスク。別のタスクに移るときは `/clear` してから。
- 計画が要る作業は Plan Mode で計画を作り、自分で読んでから進める。
- サブエージェントは同時に2〜3体まで。同じファイルを触るサブエージェントを同時に動かさない。
- フック（`hooks`）は共有しない。使うなら各自の `~/.claude/settings.json` に書く。

## §10 仕様の変更

- 仕様はチームで決めてから変える。決まったことは Issue に残す。
- 変えるときは、設計書（`.md`）と `tools/spec/build_spec_xlsx.py` を同じ PR で直し、`.xlsx` を作り直す。編集は C さん。
- 作り直し：`python3 tools/spec/build_spec_xlsx.py`（openpyxl が要る）。黄色の入力セル（実績、状態、Issue など）は前の `.xlsx` から引き継がれる。
- `.xlsx` は Excel で直接書き換えない（黄色の入力セルは除く）。衝突しても混ぜられないので、作り直す。
- 変更したら、スクリプトの `build_history` に1行足し、`VERSION` と `BUILT` を上げる。
