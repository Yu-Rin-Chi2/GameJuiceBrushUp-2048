# 命名規則

## クラス・型

| 種類 | 規則 | 例 |
|------|------|-----|
| クラス | PascalCase | `TurnController`, `CardMover`, `ScoreManager` |
| インターフェース | `I` プレフィックス + PascalCase | `IMovable` |
| 列挙型 | PascalCase | `Direction`, `GameState` |
| 構造体 | PascalCase | `CardColorEntry` |

---

## メソッド・プロパティ

| 種類 | 規則 | 例 |
|------|------|-----|
| パブリックメソッド | PascalCase | `AddScore()`, `SetGameOver()` |
| async メソッド | PascalCase + Async | `MoveCardAsync()`, `InitializeAsync()` |
| Fire-and-Forget | メソッド名 + Void | `HandleSwipeVoid()` |
| プロパティ（読み取り） | PascalCase | `public int Score { get; }` |
| プロパティ（読み書き） | PascalCase | `public int Value { get; set; }` |

---

## フィールド

| 種類 | 規則 | 例 |
|------|------|-----|
| private フィールド | `_camelCase` | `_score`, `_cards` |
| SerializeField | `_camelCase` | `_cardPrefab`, `_moveDuration` |
| readonly | `_camelCase` | `_grid` |
| 定数 | UPPER_SNAKE_CASE | `CELL_SIZE`, `GRID_SIZE` |

---

## イベント・デリゲート

| 種類 | 規則 | 例 |
|------|------|-----|
| イベント | `On` + PascalCase | `OnSwipe`, `OnGameOver`, `OnScoreChanged` |
| コールバック | `On` + PascalCase | `OnMove`, `OnMerge` |

---

## ゲーム用語の統一

| 用語 | 定義 |
|------|------|
| **Card** | プレイ中のタイル（値を持つ1個のゲームオブジェクト） |
| **Tile** | グリッド上のマス（Cardとの区別） |
| **Grid** | 4×4 の配列構造（状態管理用） |
| **Value** | カードの数値（2, 4, 8, ... 2048） |
| **Move** | ユーザー入力による1ターンの移動 |
| **Merge** | 同じ値のカード同士の合体 |
| **Spawn** | 新しいカードの生成 |
| **Score** | 獲得スコア（マージで加算） |
| **GameOver** | ゲーム終了状態（動かせない状態） |
| **Cleared** | ゲームクリア状態（2048達成） |

**避けるべき用語**：
- ❌ Tile（値と混同するので使わない、Grid/Card で統一）
- ❌ Block（Card で統一）
- ❌ Box（Tile か Card で統一）

---

## メソッド命名のパターン

### 状態取得（Getter）
```csharp
public int GetScore()              // → Score プロパティを使う
public int Score { get; }          // ✓ 推奨

public bool CanMove()              // bool の Can/Is パターン
public bool IsGameOver { get; }    // ✓ bool プロパティ
```

### 状態変更（Setter）
```csharp
public void SetValue(int value)        // Set + 対象
public void AddScore(int points)       // Add + 対象
public void Reset()                    // 全リセット
```

### イベント検査
```csharp
public bool CanMoveInDirection(Direction dir)
public List<(int, int)> FindEmptySpots()
```

### 非同期処理
```csharp
public async UniTask MoveCardAsync(Card card, Vector2 pos, CancellationToken ct)
public async UniTask InitializeAsync()
public async void HandleSwipeVoid(Direction dir)  // Fire-and-Forget
```

---

## 変数命名のパターン

### ローカル変数
```csharp
private void ProcessMove(Direction direction)
{
    var tasks = new List<UniTask>();        // 複数のタスク
    var card = GetCard(row, col);            // 1個のオブジェクト
    var emptySpots = new List<(int, int)>(); // リスト
    var (row, col) = emptySpots[0];          // タプル分解
}
```

### パラメータ
```csharp
public void Initialize(int row, int col, int value)
public async UniTask MoveCardAsync(Card card, Vector2 targetPos, CancellationToken ct)
```

### コレクション
```csharp
private List<Card> _cards;              // 複数形（List）
private int[,] _grid;                   // 配列（Grid）
private Dictionary<int, Color> _colors; // マッピング
```

---

## ファイル名

| 種類 | 規則 | 例 |
|------|------|-----|
| クラスファイル | クラス名と同じ | `TurnController.cs` |
| 複数クラス | 主要クラス名 | `Direction.cs` （Direction enum含む） |
| ScriptableObject | クラス名 + 用途 | `BoardSettings.cs`, `CardColorTable.cs` |

---

## フォルダ名

```
Assets/0MyAssets/
├── Script/
│   ├── Input/               # 入力関連
│   ├── Model/               # ゲーム状態・ロジック
│   ├── Spawner/             # 生成関連
│   ├── View/                # UI・アニメーション
│   └── Manager/             # 統合制御
├── Prefab/                  # プレハブ
├── Settings/                # ScriptableObject
└── Scenes/                  # シーン
```

---

## アセット名

| 種類 | 規則 | 例 |
|------|------|-----|
| ScriptableObject | `[名前].asset` | `BoardSettings.asset`, `CardColorTable.asset` |
| Prefab | `[名前].prefab` | `Card_Base.prefab` |
| Scene | `[シーン名].unity` | `PlayScene.unity` |
| Sprite | `[対象]_[状態].png` | - |
| Font | `[フォント名].ttf` | - |

---

## 定数命名の詳細

```csharp
// グリッド関連
private const int GRID_SIZE = 4;
private const float ORIGIN_X = 90f;
private const float ORIGIN_Y = -90f;
private const float CELL_SIZE = 140f;

// アニメーション
private const float MOVE_DURATION = 0.1f;
private const float MERGE_DURATION = 0.15f;

// ゲームロジック
private const int SPAWN_VALUE_2_WEIGHT = 9;
private const int SPAWN_VALUE_4_WEIGHT = 1;
private const int WIN_VALUE = 2048;

// UI
private const int MAX_CARDS = 16;
```

---

## 禁止事項

| パターン | 理由 | 代替案 |
|----------|------|--------|
| `m_variable` | 古い Microsoft 規約 | `_variable` |
| `pVariable` | ハンガリアン記法 | 型情報は省略 |
| `GetValue()` → `Value` | プロパティで十分 | `public int Value { get; }` |
| `isXxx` / `hasXxx` プロパティ | 統一性 | `public bool IsXxx { get; }` |
| 1文字変数 | 可読性低下 | `card`, `row`, `col` など意味のある名前 |
| 省略語（`sp`, `obj`, `tmp`） | 不明確 | `spawner`, `card`, `tempList` |

---

## 名前の推奨バージョン

| ❌ 非推奨 | ✓ 推奨 | 理由 |
|---------|--------|------|
| `HandleInputEvent()` | `HandleSwipe()` | より具体的 |
| `UpdateCard()` | `SetValue()` / `SetGridPosition()` | 何を更新するか明確 |
| `ProcessLogic()` | `ProcessMove()` / `ProcessMerges()` | 何をするのか明確 |
| `MoveCard()` | `MoveCardAsync()` | 非同期であることを明示 |
| `TryGetValue()` | `TryGetColors()` | 複数の戻り値の場合は具体的に |
| `Check()` | `CanMove()` / `HasEmptySpace` | 何をチェックするか明確 |
