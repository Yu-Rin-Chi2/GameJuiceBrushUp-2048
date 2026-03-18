# クラスリファレンス

> 自動生成日: 2026-03-19
> 対象コードベース: `Assets/0MyAssets/Script/`

---

## アーキテクチャ概要

### レイヤー図

```
┌─────────────────────────────────────────────────────┐
│                    Input 層                          │
│              InputEventController                    │
│         OnSwipe(Direction) / OnReset イベント         │
└────────────────────┬────────────────────────────────┘
                     │ イベント購読
┌────────────────────▼────────────────────────────────┐
│                 Controller 層                        │
│                 TurnController                       │
│  ターン処理オーケストレーション（非同期・ロック制御）     │
└──┬──────────┬──────────┬──────────┬─────────────────┘
   │          │          │          │
   ▼          ▼          ▼          ▼
┌──────┐ ┌────────┐ ┌────────┐ ┌────────────────┐
│Card  │ │Card    │ │Card    │ │  Manager 層    │
│Mover │ │Spawner │ │Registry│ │GameStateManager│
│      │ │        │ │        │ │ScoreManager    │
│移動・│ │カード  │ │グリッド│ │MoveJudger      │
│マージ│ │生成    │ │台帳    │ │GameDatabase    │
└──┬───┘ └────┬───┘ └────────┘ └────────┬───────┘
   │          │                          │
   ▼          ▼                          ▼
┌─────────────────┐               ┌──────────────┐
│    View 層      │               │   UI 層      │
│    CardView     │               │GameUIController│
│  (表示・色・位置)│               │(スコア・パネル)│
└─────────────────┘               └──────────────┘

┌──────────────────────────────────────────────────────┐
│                  Board 層（設定 SO）                  │
│   BoardSettings  /  CardColorTable  /  Direction     │
└──────────────────────────────────────────────────────┘
```

**データフロー:**
キー入力 → `InputEventController.OnSwipe` → `TurnController.HandleSwipe`
→ `CardMover.ExecuteTurnAsync` → `MoveResult` → スポーン / スコア / 判定
→ `GameStateManager` イベント → `GameUIController` パネル表示

---

### 依存関係ルール

| 層 | 依存可能な層 | 注記 |
|----|-------------|------|
| Controller（TurnController） | Card, Manager, Input, UI | ゲームの統合点 |
| Card（CardMover, CardSpawner, CardRegistry） | Board（GameDatabase経由） | SO 直参照禁止 |
| View（CardView） | Board（GameDatabase経由） | SO 直参照禁止 |
| Manager（GameStateManager, ScoreManager） | なし | 純粋状態管理 |
| Manager（MoveJudger） | なし | 純粋ロジック |
| Manager（GameDatabase） | Board（BoardSettings, CardColorTable） | Facade 経由で公開 |
| UI（GameUIController） | Manager | イベント購読のみ |
| Input（InputEventController） | Board（Direction） | enum のみ |

---

## クラス一覧

---

### Controller 層

#### TurnController

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Controller/TurnController.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | ゲーム全体のエントリーポイント。入力イベントを受け付け、ターン処理（移動→スポーン→判定）を非同期で実行し、二重処理を防ぐ。 |

**SerializeField フィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `_inputController` | `InputEventController` | 入力管理 |
| `_cardRegistry` | `CardRegistry` | カード台帳 |
| `_cardMover` | `CardMover` | カード移動・マージ処理 |
| `_cardSpawner` | `CardSpawner` | 新規カード生成 |
| `_gameStateManager` | `GameStateManager` | ゲーム状態管理 |
| `_gameUIController` | `GameUIController` | UI管理 |
| `_scoreManager` | `ScoreManager` | スコア管理 |

**プライベートフィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `_isProcessing` | `bool` | ターン処理中の二重入力を防ぐフラグ。`finally` で必ず解除。 |

**パブリックメソッド**

なし（すべてプライベート）

**処理フロー（ProcessTurnAsync）**

```
1. _cardMover.ExecuteTurnAsync(direction, ct) で移動・マージ計算・アニメーション
2. MoveResult.HasMoved == false なら即return（ノームーブ最適化）
3. result.Merges を走査し absorbed.gameObject を Destroy、survivor の値を更新
4. result.MergedValues を走査し ScoreManager.AddScore / 2048達成なら SetCleared
5. CardSpawner.SpawnCard() で新カード生成
6. MoveJudger.CanMove() でゲームオーバー判定
7. finally で _isProcessing = false、SetInputEnabled(true)
```

---

### Card 層

#### CardView

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Card/CardView.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | 1枚のカードの表示（位置・数値・色）を担う View クラス。`GameDatabase` 経由でグリッド座標を UI 座標に変換し、`CardColorTable` から色を取得して描画する。 |

**SerializeField フィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `_backgroundImage` | `Image` | カードの背景画像 |
| `_valueText` | `TextMeshProUGUI` | カードの数値を表示する TextMeshPro |

**パブリックプロパティ**

| プロパティ名 | 型 | 説明 |
|-------------|-----|------|
| `Value` | `int` | カードの現在の数値 |
| `Row` | `int` | グリッド上の行番号 |
| `Col` | `int` | グリッド上の列番号 |
| `RectTransform` | `RectTransform` | DOTween アニメーション用の RectTransform |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `void Initialize(int row, int col, int value)` | カードを指定位置・数値で初期化。`CardSpawner` から最初に呼ばれる。 |
| `void SetGridPosition(int row, int col)` | グリッド位置を更新し、UI 座標を `GameDatabase.GetAnchoredPosition` で再計算して反映する。 |
| `void SetGridPositionLogical(int row, int col)` | グリッド論理座標のみ更新（UI 座標は変えない）。アニメーション前の論理位置更新用。 |
| `void SetValue(int value)` | 数値を更新し、テキストと背景色・テキスト色を `GameDatabase.TryGetCardColors` で変更する。 |

---

#### CardMover

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Card/CardMover.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | カード移動・マージを処理し、DOTween アニメーションを実行する。グリッドを方向に沿って走査して移動目標・マージ先を計算し、全カードを並列アニメーションで移動させる。 |

**SerializeField フィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `_cardRegistry` | `CardRegistry` | カード台帳 |
| `_moveDuration` | `float` | カード移動時間（デフォルト 0.1 秒） |
| `_moveEase` | `Ease` | カード移動のイージング（デフォルト `Ease.OutQuart`） |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `async UniTask<MoveResult> ExecuteTurnAsync(Direction direction, CancellationToken ct)` | ターン開始から終了まで実行。移動・マージ計算 → 並列アニメーション → `MoveResult` を返す。 |

**プライベートメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `(bool, List<(CardView, Vector2)>, List<(CardView, CardView, int)>) CalculateMoves(Direction direction)` | 方向を受けて全カードの移動目標とマージ先を計算する。二重マージを `mergedPositions` で防ぐ。 |
| `async UniTask AnimateMovesAsync(List<(CardView, Vector2)> moves, CancellationToken ct)` | DOTween を使用して全カードを並列移動アニメーションする。 |
| `(int dr, int dc) GetDelta(Direction d)` | 方向から行・列のデルタ値を返す。 |

---

#### CardRegistry

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Card/CardRegistry.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | ゲーム中に存在する全カードを管理するカード台帳。`List<CardView>` とグリッド配列 `int[,]` を双方保持し、常に同期する。他クラスはこのクラス経由でカードを操作・削除・更新する。 |

**パブリックプロパティ**

| プロパティ名 | 型 | 説明 |
|-------------|-----|------|
| `CardCount` | `int` | 現在のカード枚数 |
| `EmptyCount` | `int` | 空きマス数（`GridSize^2 - CardCount`） |
| `TotalCells` | `int` | グリッド全体のマス数（`GridSize^2`） |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `List<CardView> GetAllCards()` | 現在ゲーム中の全カードリストを返す。 |
| `CardView GetCard(int row, int col)` | 指定グリッド位置のカードを返す（存在しない場合は `null`）。 |
| `void AddCard(CardView card)` | 新しいカードを追加し、グリッド配列を更新する。 |
| `void RemoveCard(CardView card)` | カードをリストから削除し、グリッド配列をリセット（0）する。 |
| `void UpdateCardPosition(CardView card, int newRow, int newCol)` | カードの論理位置を更新し、グリッド配列を同期する。 |
| `void UpdateCardValue(CardView card, int newValue)` | カードの数値を更新し、グリッド配列を同期する。マージ時に生存カードへ適用。 |
| `int[,] GetGridState()` | グリッド状態配列の参照を返す。移動計算・ゲームオーバー判定用。 |
| `void Reset()` | 全カードの `GameObject` を破棄し、グリッド配列を初期化する。 |

---

#### CardSpawner

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Card/CardSpawner.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | 空きセルを探してランダムに新カードを生成し、`CardRegistry` に登録する。生成数値の確率は `GameDatabase` 経由で `BoardSettings` から取得する。 |

**SerializeField フィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `_cardRegistry` | `CardRegistry` | カード台帳 |
| `_cardPrefab` | `CardView` | 生成するカードの Prefab |
| `_cardParent` | `Transform` | 生成したカード GameObject の親 |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `(int row, int col, int value)? SpawnCard()` | 空きセルを探し、ランダムに新カードを生成・登録する。空きセルなしの場合は `null` を返す。数値は `SpawnValue2Weight`（デフォルト 9）と `SpawnValue4Weight`（デフォルト 1）の重み比で決定する。 |

---

#### MoveResult

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Card/MoveResult.cs` |
| 基底クラス | `readonly struct` |
| 責務 | カード移動の結果を表すデータ構造体。移動有無・マージされた数値一覧・マージ詳細を保持し、`CardMover` から `TurnController` へ渡される。 |

**フィールド**

| 名前 | 型 | 説明 |
|------|-----|------|
| `HasMoved` | `bool` | カードが移動したかどうか |
| `MergedValues` | `List<int>` | マージされた新数値の一覧（スコア加算・クリア判定用） |
| `Merges` | `List<(CardView survivor, CardView absorbed, int newValue)>` | マージ詳細。`TurnController` でのマージ後処理に使用。 |
| `None` | `static readonly MoveResult` | 移動なしの結果を表す定数値 |

**コンストラクタ**

| シグネチャ | 説明 |
|-----------|------|
| `MoveResult(List<int> mergedValues, List<(CardView survivor, CardView absorbed, int newValue)> merges)` | 移動ありの結果を生成（`HasMoved = true`）。 |

---

### Input 層

#### InputEventController

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Input/InputEventController.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | Unity Input System を使用してキーボード入力を受け付け、`Direction` イベントを発火する。`TurnController` からターン処理中の二重入力防止・ゲーム終了時の入力無効化を制御される。 |

**イベント**

| イベント名 | 型 | 発火タイミング |
|-----------|-----|--------------|
| `OnSwipe` | `event Action<Direction>` | 方向キー（上下左右）押下時 |
| `OnReset` | `event Action` | リセットキー（R）押下時 |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `void SetInputEnabled(bool enabled)` | 入力の有効・無効を切り替える。ターン処理中は `false`、処理完了後は `true` に設定される。 |

---

### Manager 層

#### GameDatabase

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Manager/GameDatabase.cs` |
| 基底クラス | `MonoBehaviour`（シングルトン） |
| 責務 | `BoardSettings` と `CardColorTable` の ScriptableObject 依存を一元管理する Singleton Facade。各クラスは SO に直接依存せず、`GameDatabase.Instance` 経由でのみ設定データにアクセスする。 |

**SerializeField フィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `_boardSettings` | `BoardSettings` | グリッド設定 SO |
| `_cardColorTable` | `CardColorTable` | カード色テーブル SO |

**パブリックプロパティ（Facade）**

| プロパティ名 | 型 | 委譲先 |
|-------------|-----|--------|
| `Instance` | `static GameDatabase` | シングルトンインスタンス |
| `GridSize` | `int` | `BoardSettings.GridSize` |
| `SpawnValue2Weight` | `int` | `BoardSettings.SpawnValue2Weight` |
| `SpawnValue4Weight` | `int` | `BoardSettings.SpawnValue4Weight` |

**パブリックメソッド（Facade）**

| シグネチャ | 委譲先 |
|-----------|--------|
| `Vector2 GetAnchoredPosition(int row, int col)` | `BoardSettings.GetAnchoredPosition(row, col)` |
| `bool TryGetCardColors(int value, out Color bgColor, out Color textColor)` | `CardColorTable.TryGetColors(value, out bgColor, out textColor)` |

---

#### GameStateManager

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Manager/GameStateManager.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | ゲームの終了状態（ゲームオーバー・クリア）を管理し、状態変化時にイベントを発火する。重複発火を防ぐガード付き。 |

**パブリックプロパティ**

| プロパティ名 | 型 | 説明 |
|-------------|-----|------|
| `IsGameOver` | `bool` | ゲームオーバー状態か |
| `IsCleared` | `bool` | ゲームクリア（2048 達成）したか |

**イベント**

| イベント名 | 型 | 発火タイミング |
|-----------|-----|--------------|
| `OnGameOver` | `event Action` | ゲームオーバー確定時（重複発火なし） |
| `OnGameCleared` | `event Action` | ゲームクリア確定時（重複発火なし） |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `void SetGameOver()` | ゲームオーバーを設定し `OnGameOver` を発火する。`IsGameOver` が `false` の場合のみ実行。 |
| `void SetCleared()` | ゲームクリアを設定し `OnGameCleared` を発火する。`IsCleared` が `false` の場合のみ実行。 |
| `void Reset()` | `IsGameOver`・`IsCleared` を `false` にリセットする。 |

---

#### ScoreManager

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Manager/ScoreManager.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | ゲームスコアを管理し、更新時にイベントを発火する。スコア加算はマージ後に `TurnController` から呼ばれる。 |

**パブリックプロパティ**

| プロパティ名 | 型 | 説明 |
|-------------|-----|------|
| `Score` | `int` | 現在のスコア |

**イベント**

| イベント名 | 型 | 発火タイミング |
|-----------|-----|--------------|
| `OnScoreChanged` | `event Action<int>` | スコア加算時・リセット時（現在スコア値を引数で渡す） |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `void AddScore(int points)` | スコアを加算し `OnScoreChanged` を発火する。 |
| `void Reset()` | スコアを 0 にリセットし `OnScoreChanged` を発火する。 |

---

#### MoveJudger

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Manager/MoveJudger.cs` |
| 基底クラス | `static class` |
| 責務 | ゲーム続行可否を判定する純粋ロジッククラス。`MonoBehaviour` を持たず、グリッド配列のみを受け取って判定する。 |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `static bool CanMove(int[,] grid, int size)` | 空きセル（`grid[r,c] == 0`）または隣接同値があれば `true` を返す。両条件が満たされない場合は `false`（ゲームオーバー）。 |

---

### UI 層

#### GameUIController

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/UI/GameUIController.cs` |
| 基底クラス | `MonoBehaviour` |
| 責務 | スコア・ゲームステータス変更イベントを購読し、スコアテキスト・ゲームオーバーパネル・クリアパネルの表示を制御する。 |

**SerializeField フィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `_scoreText` | `TextMeshProUGUI` | スコアを表示する TextMeshPro |
| `_mainPanel` | `GameObject` | 通常時に表示するメイン UI パネル |
| `_gameOverPanel` | `GameObject` | ゲームオーバー時に表示するパネル |
| `_clearedPanel` | `GameObject` | ゲームクリア時に表示するパネル |
| `_scoreManager` | `ScoreManager` | スコア管理（イベント購読用） |
| `_gameStateManager` | `GameStateManager` | ゲーム状態管理（イベント購読用） |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `void Reset()` | 全パネルを非表示にし、`_mainPanel` をアクティブ化する。`TurnController.ResetGame` から呼ばれる。 |

**イベント購読（Start で登録、OnDestroy で解除）**

| イベント発行元 | イベント名 | ハンドラ |
|--------------|-----------|---------|
| `ScoreManager` | `OnScoreChanged` | `UpdateScore(int score)` — スコアテキスト更新 |
| `GameStateManager` | `OnGameOver` | `ShowGameOver()` — ゲームオーバーパネル表示 |
| `GameStateManager` | `OnGameCleared` | `ShowCleared()` — クリアパネル表示 |

---

### Board 層（設定）

#### BoardSettings

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Board/BoardSettings.cs` |
| 基底クラス | `ScriptableObject` |
| メニュー | `Create > 2048 > BoardSettings` |
| 責務 | グリッドサイズ・UI 座標の原点・セルサイズ・スポーン確率の重みを保持する設定 SO。座標計算メソッドも持つ。直接参照は `GameDatabase` Facade のみ許可。 |

**パブリックフィールド**

| フィールド名 | 型 | デフォルト値 | 説明 |
|-------------|-----|------------|------|
| `GridSize` | `int` | `4` | グリッドの行列サイズ |
| `OriginX` | `float` | `90f` | グリッド左上の X 座標（pixel） |
| `OriginY` | `float` | `-90f` | グリッド左上の Y 座標（pixel） |
| `CellSize` | `float` | `140f` | セル間隔を含む 1 セルの大きさ（pixel） |
| `SpawnValue2Weight` | `int` | `9` | カード「2」スポーン確率の重み |
| `SpawnValue4Weight` | `int` | `1` | カード「4」スポーン確率の重み |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `Vector2 GetAnchoredPosition(int row, int col)` | グリッド座標から UI Canvas 上のアンカー位置を計算する。`new Vector2(OriginX + col * CellSize, OriginY - row * CellSize)` |

**座標計算式**

```
x = OriginX + col * CellSize  →  x = 90 + col * 140
y = OriginY - row * CellSize  →  y = -90 - row * 140
```

---

#### CardColorTable

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Card/CardColorTable.cs` |
| 基底クラス | `ScriptableObject` |
| メニュー | `Create > 2048 > CardColorTable` |
| 責務 | カードの数値と背景色・テキスト色の対応テーブルを保持する設定 SO。直接参照は `GameDatabase` Facade のみ許可。 |

**内部型**

```csharp
[Serializable]
public struct CardColorEntry
{
    public int Value;          // カードの数値（2, 4, 8, 16 ...）
    public Color BackgroundColor;  // カード背景色
    public Color TextColor;    // カードテキスト色
}
```

**パブリックフィールド**

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| `Entries` | `CardColorEntry[]` | 数値と色の対応表配列 |

**パブリックメソッド**

| シグネチャ | 説明 |
|-----------|------|
| `bool TryGetColors(int value, out Color bgColor, out Color textColor)` | 指定数値に対応する色を返す。完全一致がなければ `Entries` の末尾エントリの色を使用。`Entries` が空の場合のみ `false`（デフォルト: グレー背景・白テキスト）を返す。 |

> **注意:** `GameDatabase` は `TryGetColors` を `TryGetCardColors` という名前で公開している。呼び出し側は必ず `GameDatabase.Instance.TryGetCardColors(...)` を使うこと。

---

#### Direction

| 項目 | 内容 |
|------|------|
| ファイルパス | `Assets/0MyAssets/Script/Board/Direction.cs` |
| 基底クラス | `enum` |
| 責務 | カード移動方向を表す列挙型。`InputEventController` が生成し、`TurnController` → `CardMover` へと渡される。 |

**値**

| 値 | 説明 |
|----|------|
| `Up` | 上方向への移動 |
| `Down` | 下方向への移動 |
| `Left` | 左方向への移動 |
| `Right` | 右方向への移動 |

---

## イベント・通信パターン

| イベント名 | 発行元 | 購読元 | 発火タイミング |
|-----------|--------|--------|--------------|
| `OnSwipe` | `InputEventController` | `TurnController` | 方向キー押下時（`_inputEnabled == true` の場合のみ） |
| `OnReset` | `InputEventController` | `TurnController` | R キー押下時 |
| `OnScoreChanged` | `ScoreManager` | `GameUIController` | `AddScore` / `Reset` 呼び出し時 |
| `OnGameOver` | `GameStateManager` | `GameUIController` | `SetGameOver` 呼び出し時（初回のみ） |
| `OnGameCleared` | `GameStateManager` | `GameUIController` | `SetCleared` 呼び出し時（初回のみ） |

**通信パターンのまとめ**

- `InputEventController` → `TurnController`: C# `event` （直接デリゲート登録）
- `ScoreManager` → `GameUIController`: C# `event Action<int>`
- `GameStateManager` → `GameUIController`: C# `event Action`
- `CardMover` → `TurnController`: `MoveResult` 構造体（UniTask 戻り値）
- 設定データアクセス（全クラス → `GameDatabase`）: シングルトン直接呼び出し

---

## シーンヒエラルキー

```
PlayScene
├── Canvas
│   ├── Board（背景タイル画像）
│   ├── Cards（CardSpawner._cardParent）
│   │   └── （CardView Prefab インスタンスが動的生成される）
│   └── UI
│       ├── MainPanel（通常時表示: スコア等）
│       │   └── ScoreText（TextMeshProUGUI）
│       ├── GameOverPanel（ゲームオーバー時表示）
│       └── ClearedPanel（クリア時表示）
└── Managers
    ├── GameDatabase（MonoBehaviour シングルトン）
    ├── TurnController
    ├── CardMover
    ├── CardRegistry
    ├── CardSpawner
    ├── InputEventController
    ├── GameUIController
    ├── ScoreManager
    └── GameStateManager
```

### Inspector 設定テーブル

| GameObject | スクリプト | 設定が必要なフィールド |
|-----------|-----------|---------------------|
| GameDatabase | `GameDatabase` | `BoardSettings`（BoardSettings.asset）、`CardColorTable`（CardColorTable.asset） |
| TurnController | `TurnController` | `InputEventController`、`CardRegistry`、`CardMover`、`CardSpawner`、`GameStateManager`、`GameUIController`、`ScoreManager` |
| CardMover | `CardMover` | `CardRegistry`、`MoveDuration`（任意）、`MoveEase`（任意） |
| CardSpawner | `CardSpawner` | `CardRegistry`、`CardPrefab`（Card.prefab）、`CardParent`（Cards GameObject） |
| GameUIController | `GameUIController` | `ScoreText`、`MainPanel`、`GameOverPanel`、`ClearedPanel`、`ScoreManager`、`GameStateManager` |
| CardView Prefab | `CardView` | `BackgroundImage`（Image）、`ValueText`（TextMeshProUGUI） |

---

## 設計上の決定

### GameDatabase Singleton Facade パターン

**背景**

`BoardSettings` と `CardColorTable` の ScriptableObject 参照が `CardView`・`CardMover`・`CardSpawner`・`CardRegistry` の 4 クラスに `[SerializeField]` で散在していた。CardView.prefab の Inspector 設定が毎回必要で、SO 変更時の影響範囲が広かった。

**実装方針**

`GameDatabase`（MonoBehaviour シングルトン）がすべての SO を一元保持し、Facade プロパティ・メソッドで公開する。各クラスは `GameDatabase.Instance.GridSize` のように静的アクセスするため、Inspector 設定が GameDatabase の 1 か所で済む。

**Facade API 一覧**

```csharp
GameDatabase.Instance.GridSize                           // グリッドサイズ
GameDatabase.Instance.GetAnchoredPosition(row, col)     // UI 座標計算
GameDatabase.Instance.SpawnValue2Weight                  // 2 スポーン重み
GameDatabase.Instance.SpawnValue4Weight                  // 4 スポーン重み
GameDatabase.Instance.TryGetCardColors(val, out bg, out text) // カード色取得
```

**メソッド名の注意**

`CardColorTable.TryGetColors()` は `GameDatabase` では `TryGetCardColors()` として公開している。呼び出し側は常に `GameDatabase.Instance.TryGetCardColors(...)` を使うこと。

---

### 二重入力ガード（`_isProcessing` + `SetInputEnabled`）

**問題**

1 ターンのアニメーション中に次の入力を受け付けると、グリッド状態が不整合になる。

**実装**

2 段階のガードを採用している。

1. `TurnController._isProcessing`（フラグ）: `HandleSwipe` の先頭でチェック。`ProcessTurnAsync` の `finally` ブロックで確実に `false` に戻す。
2. `InputEventController.SetInputEnabled(false/true)`: ターン開始で `false`、終了で `true`。OS イベントキューレベルで入力を止める。

`_isProcessing` が `finally` 保護されているため、非同期例外発生時もロックが解除される。

---

### No-Move 最適化（`!HasMoved` 時はスポーン・判定をスキップ）

**実装箇所**

`TurnController.ProcessTurnAsync` の `if (!result.HasMoved) return;`

**効果**

移動が発生しない方向キーを押した場合（例: 全カードが壁に寄っている方向にさらに押す）、`CardSpawner.SpawnCard()` と `MoveJudger.CanMove()` の呼び出しをスキップする。これにより、無駄なカード生成とゲームオーバー誤判定を防ぐ。`MoveResult.None`（`HasMoved = false`）が `CardMover.ExecuteTurnAsync` から返された場合に該当する。
