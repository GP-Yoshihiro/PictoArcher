---
name: pictoarcher-coding-rules
description: Use before writing, fixing or reviewing PictoArcher C# in Assets/_Project/Scripts — and whenever tempted to write Update/Start/Awake, Find, Camera.main, coroutines, Invoke, singletons, static state, inspector onClick, or class inheritance, or when adding a new enemy, System, View or Definition.
---

# PictoArcher のコーディング規則

規則の正本は設計書 `docs/superpowers/specs/2026-11-14-coding-rules-design.md`。ここはその要点とお手本。
規則の多くは `ArchitectureGuardTests`（EditMode テスト）が自動で確かめる。Unity の落とし穴は `docs/unity-pitfalls.md` を読む。

## 8項目の要点

1. **層と依存の向き**：`Core`（純粋な C#）← `Game`（Unity の部品）← `Editor`。Core で使ってよい UnityEngine のものは数学の型（`Vector2`、`Vector2Int`、`Vector3`、`Mathf`、`Rect`）だけ。名前空間は `PictoArcher.Core` / `PictoArcher.Game` / `PictoArcher.Editor` の3つだけで、フォルダを分けても名前空間は分けない。
2. **実行順**：Unity の標準ライフサイクル（`Awake`、`Start`、`Update`、`OnEnable`、`OnDestroy` など）を書いてよいのは `Main` だけ。`Main` → シーン（`SceneBase`）→ System の順に呼び、呼ぶ順番をソースに1行ずつ書く。`Init` は上から、`Dispose` は逆順。`Time.deltaTime` を読むのは `Main` だけで、`deltaTime` は引数で渡す。待ち時間はコルーチンではなく Tick で進めるタイマー。
3. **Unity が直接呼ぶもの**：当たり判定は `CollisionRelay2D`、ボタンは `UiButtonRelay` が記録するだけ。処理は Tick の中で取り出して行う。入力は Tick の中で `ReadValue` / `WasPressedThisFrame` で読む。
4. **組み合わせで作る**：継承してよいのは `SceneBase`、`DefinitionAsset<T>`、`DefinitionAsset<T>` を直接継承した役割ごとの抽象クラスだけ。ほかは `sealed`。敵は「定義（ScriptableObject）」と「動く部品（普通の C#）」の組み合わせで作る。interface は1役割に1つ。拡張メソッドは状態を持たない関数だけで `XxxExtensions` にまとめる。
5. **データと状態を分ける**：ScriptableObject は読み取り専用。`[SerializeField] _camelCase` ＋ 読み取り専用のプロパティ。実行中に変わる値（今の HP など）は、データから作った普通の C# のオブジェクトが持つ。
6. **やり取り**：参照は上から渡す（普通の C# はコンストラクター、MonoBehaviour は `Init(...)`、シーンの中はインスペクター）。`Find` 系・`Camera.main`・`SendMessage`・シングルトン・static の書き換えられる値は禁止。下から上へは C# の `event` か戻り値。イベントは `Init` で登録し `Dispose` で外す。
7. **名前とフォルダ**：役割で名前を付ける（`Projectile`、`Chaser`）。接尾辞 `Scene` / `System` / `View` / `Definition` / `Relay` / `Extensions` / `Id`。1ファイルに1つの型、ファイル名＝型名。
8. **今あるコード**：古い計画（計画1 のタスク8〜10 など）に載っているコードの `Update`、`OnTriggerEnter2D`、static の `EnemyRegistry`、public のフィールドは写さない。この規則に読み替える。

### 迷ったときの判断

- 「それは誰が呼ぶのか」を先に決める。答えが「Unity」なら、Relay を挟むか、シーンの Tick から呼ぶ形に直す。
- 「その値は誰のものか」を決める。調整値なら ScriptableObject、実行中に変わるなら System か動く部品、全体で1つなら `GameContext`。
- 似たクラスを作りたくなったら、違う所だけを部品（interface と定義）に切り出す。継承やコピーはしない。
- 規則とぶつかって書けないときは、規則を曲げずに人に伝える。ガードテストに例外を足すのは設計の変更。

## お手本

実物のお手本：`Game/Main/Main.cs`、`Game/Scenes/RunScene.cs`、`Game/Scenes/HomeScene.cs`、`Core/Combat/FireTimer.cs`、`Game/Common/CollisionRelay2D.cs`。
下の `EnemySystem`、`EnemyView`、`MovementDefinition` などは**説明のための仮の名前**。本物の名前と形は、それぞれのタスクで決める。

### シーン（`RunScene` の形）

```csharp
public sealed class RunScene : SceneBase
{
    [SerializeField] EnemyView _enemyPrefab;   // シーンの中のものはインスペクターでつなぐ

    EnemyRegistry _enemies;
    EnemySystem _enemySystem;

    public override void Init(GameContext context)
    {
        _enemies = new EnemyRegistry();
        _enemySystem = new EnemySystem(_enemies, _enemyPrefab);   // 参照はコンストラクターで渡す
    }

    public override void Tick(float deltaTime)
    {
        _enemySystem.Tick(deltaTime);   // 1つの System に1行。この順番が実行順
    }

    public override void Dispose()
    {
        _enemySystem.Dispose();         // 作った順の逆に片付ける
        _enemies.Clear();
    }
}
```

### Sandbox のシーン（各自の試し用）

`Scenes/Sandbox/Sandbox_<担当>.unity` にプレハブを置いて再生するだけでは、何も動かない。View は自分では動かず、`Main` は「SceneBase が 0 個」とエラーを1回出すだけだから。試し用のシーンにも、自分の `SceneBase` を1つ置く。

- クラスは `Scripts/Game/Scenes/Sandbox/Sandbox<担当>Scene.cs`（例 `SandboxAScene`）。規則は本番のシーンと同じ。
- シーンの一番上の階層に空の GameObject を1つ作り、このクラスを付ける。試したいプレハブはインスペクターでつなぐ。
- `SceneId` には足さない（エディタで直接再生するだけ）。フォルダがなければ、最初に使う人が作る。

```csharp
public sealed class SandboxAScene : SceneBase
{
    [SerializeField] EnemyView _enemyPrefab;   // 試したいものをつなぐ

    EnemySystem _enemySystem;

    public override void Init(GameContext context) => _enemySystem = new EnemySystem(new EnemyRegistry(), _enemyPrefab);

    public override void Tick(float deltaTime) => _enemySystem.Tick(deltaTime);

    public override void Dispose() => _enemySystem.Dispose();
}
```

### System（普通の C#）

```csharp
public sealed class EnemySystem : ITickable, IDisposable
{
    readonly EnemyRegistry _enemies;
    readonly EnemyView _prefab;
    readonly List<EnemyView> _views = new List<EnemyView>();

    public event Action<EnemyView> Died;   // イベント名は過去形、On を付けない

    public EnemySystem(EnemyRegistry enemies, EnemyView prefab)
    {
        _enemies = enemies;
        _prefab = prefab;
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < _views.Count; i++) _views[i].MoveBy(Vector2.left * deltaTime);
    }

    public void Dispose()
    {
        for (int i = 0; i < _views.Count; i++) UnityEngine.Object.Destroy(_views[i].gameObject);
        _views.Clear();
    }
}
```

### View（プレハブに付く MonoBehaviour。自分では動かない）

```csharp
public sealed class EnemyView : MonoBehaviour
{
    [SerializeField] SpriteRenderer _sprite;   // 部品はインスペクターでつなぐ
    Transform _transform;

    public void Init()
    {
        _transform = transform;               // GetComponent や transform は作ったときに1回だけ
    }

    public Vector2 Position => _transform.position;
    public void MoveBy(Vector2 delta) => _transform.position += (Vector3)delta;
}
```

### Definition と動く部品（敵の組み合わせ）

```csharp
// 役割の interface（動く部品）。自分の状態を持ち、必要なものは Tick の引数で受け取る
public interface IMovement
{
    Vector2 Tick(float deltaTime, Vector2 self, Vector2 target);
}

// 役割の抽象定義（DefinitionAsset<T> を直接継承した抽象クラスは継承してよい）
public abstract class MovementDefinition : DefinitionAsset<IMovement> { }

// 具体的な定義。新しい動きは「定義」と「動く部品」を1組足すだけで、ほかのファイルを直さない
[CreateAssetMenu(menuName = "PictoArcher/Movement/Chase")]
public sealed class ChaseMovementDefinition : MovementDefinition
{
    [SerializeField] float _speed = 2f;
    public float Speed => _speed;
    public override IMovement Create() => new ChaseMovement(_speed);
}

public sealed class ChaseMovement : IMovement
{
    readonly float _speed;
    public ChaseMovement(float speed) => _speed = speed;
    public Vector2 Tick(float deltaTime, Vector2 self, Vector2 target)
        => Vector2.MoveTowards(self, target, _speed * deltaTime) - self;
}
```

`EnemyDefinition` が `[SerializeField] MovementDefinition _movement;` のように役割の定義を持ち、敵を作るときに `_movement.Create()` で動く部品を作る。

### Relay の使い方

```csharp
// 当たり判定：System の Tick で取り出す
readonly List<Collider2D> _hits = new List<Collider2D>();   // 毎フレーム new しない

public void Tick(float deltaTime)
{
    _hits.Clear();
    _relay.DrainEntered(_hits);
    for (int i = 0; i < _hits.Count; i++)
    {
        if (_hits[i] == null) continue;    // Destroy された相手は == null で確かめる（?. は効かない）
        // 当たったときの処理
    }
}

// ボタン：Init で Init、Tick で ConsumeClicked、Dispose で Dispose（HomeScene.cs が実物）
_startButton.Init();
if (_startButton.ConsumeClicked()) _context.RequestScene(SceneId.Run);
_startButton.Dispose();
```

### イベントの登録と解除

```csharp
public override void Init(GameContext context)
{
    _enemySystem = new EnemySystem(_enemies, _enemyPrefab);
    _enemySystem.Died += HandleDied;      // 登録は Init
}

public override void Dispose()
{
    _enemySystem.Died -= HandleDied;      // 解除は Dispose（必ず対にする）
    _enemySystem.Dispose();
}

void HandleDied(EnemyView enemy) { /* 受けるメソッドは Handle + イベント名 */ }
```

### TickList（回している最中に増えたり減ったりする一覧）

```csharp
readonly TickList<IEnemyPart> _parts = new TickList<IEnemyPart>();

public void Tick(float deltaTime)
{
    _parts.ApplyPending();                 // 先に、予約された追加と削除を反映する
    for (int i = 0; i < _parts.Count; i++) _parts[i].Tick(deltaTime);   // 中で Add/Remove しても壊れない
}
```

## こう書きたくなったら、代わりにこう書く

| 書きたくなるもの | 代わりに書くもの |
|---|---|
| `Update` / `Start` / `Awake` / `OnEnable` / `OnDestroy` | シーンの `Tick` / `Init` / `Dispose` から呼ばれるメソッド。MonoBehaviour なら `Init(...)` を作り、持ち主が呼ぶ |
| `FixedUpdate` で物理 | シーンの `FixedTick` から呼ぶ（`Rigidbody2D` を動かすときだけ） |
| `GameObject.Find`、`FindAnyObjectByType`、`Camera.main` | シーンのインスペクターでつなぎ、`Init` やコンストラクターで渡す |
| コルーチン、`Invoke`、`async`/`await`、`WaitForSeconds` | Tick で `deltaTime` を足していくタイマー（Core。`FireTimer` が例） |
| シングルトン（`Instance`）、static の値 | シーンが持って渡す。全体で1つのものは `GameContext` に足す |
| `Time.deltaTime`、`Time.time` | 引数の `deltaTime`。一時停止と倍率は `GameContext.Clock` |
| `OnTriggerEnter2D` / `OnCollisionEnter2D` | `CollisionRelay2D` を付け、System の Tick で `DrainEntered` / `Touching` を見る |
| インスペクターで Button の onClick に関数をつなぐ | `UiButtonRelay` を付け、Tick で `ConsumeClicked` を見る |
| `action.performed += ...` | Tick の中で `ReadValue<Vector2>()` / `WasPressedThisFrame()` |
| 敵のクラスをコピーして少し変える、敵の基底クラスを作って継承 | 違う所を役割の定義と動く部品に切り出し、`EnemyDefinition` で組み合わせる |
| `public float moveSpeed;` | `[SerializeField] float _moveSpeed;` ＋ `public float MoveSpeed => _moveSpeed;` |
| ScriptableObject の値を実行中に書き換える | データから普通の C# のオブジェクトを作り、そちらの値を変える |
| `LayerMask.NameToLayer("Enemy")`、`CompareTag("Enemy")`、数字の `7` | `Layers.Enemy` / `Layers.EnemyMask` |
| `Debug.Log` を Core に書く | Core はログを出さない。結果を戻り値で返し、Game 側で必要なら出す |

## ガードテストが落ちたとき

- `ArchitectureGuardTests` は規則ごとに1つのテストで、違反を全部まとめて出す。メッセージには場所（ファイルと行、または型とメンバー）と日本語の直し方が書いてある。まずそれを読み、書いてあるとおりに直す。
- 実行：Unity のエディタを閉じて `tools/run-editmode-tests.sh`。結果は `Logs/editmode-results.xml`。
- テストの例外の一覧に足して通すのは、規則を変えること（設計の変更）。足したくなったら、直さずに人に伝える。足すときは PR に理由を書き、レビューで認めてもらう。
- コメントで違反を見逃させる仕組みはない。テストを書き換えて通さない。
