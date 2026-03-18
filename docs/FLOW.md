# 処理フロー詳細

## フロー全体

```
1. Input
   InputEventController → Direction 取得

2. Move + Merge
   TurnController.ProcessTurnAsync(direction)
   → CardMover.ExecuteTurnAsync(direction)
     → CalculateMoves() で移動先・マージ先を計算
     → AnimateMovesAsync() で全カードを並列アニメーション
     → MoveResult を返却

3. Post-Merge
   TurnController がマージ結果を処理
   → 吸収されたカードを Destroy
   → 生存カードの Value を更新（CardRegistry.UpdateCardValue）
   → スコア加算（ScoreManager.AddScore）
   → 2048 チェック → GameStateManager.SetCleared()

4. Spawn
   CardSpawner.SpawnCard() で新カード生成（2 or 4）

5. Judgment
   MoveJudger.CanMove(grid, size) で判定
   → false なら GameStateManager.SetGameOver()
```

---

## 詳細フロー

### 1. Input フロー

```
[InputEventController.OnMoveUp / OnMoveDown / OnMoveLeft / OnMoveRight]
  ① 各方向キーの performed コールバックで FireSwipe(Direction) を呼び出し
  ② FireSwipe 内で _inputEnabled を確認
  ③ if (_inputEnabled) OnSwipe?.Invoke(direction)

[InputEventController.OnResetPerformed]
  ① OnReset?.Invoke()
```

**出力**：OnSwipe イベント（Direction 型）

---

### 2. Move フロー

```
[TurnController.HandleSwipe(Direction direction)]
  ① if (_isProcessing || IsGameOver || IsCleared) return
  ② ProcessTurnAsync(direction, ct).Forget()

[TurnController.ProcessTurnAsync(Direction direction, CancellationToken ct)]
  ① _isProcessing = true
  ② _inputController.SetInputEnabled(false)
  ③ try:
     a. MoveResult result = await _cardMover.ExecuteTurnAsync(direction, ct)
     b. if (!result.HasMoved) return  ← No-Move 最適化
     c. マージ処理（後述）
     d. スポーン
     e. ゲームオーバー判定
  ④ finally:
     _isProcessing = false
     _inputController.SetInputEnabled(true)
```

---

### 3. CardMover.ExecuteTurnAsync 内部

```
[CardMover.ExecuteTurnAsync(Direction direction, CancellationToken ct)]
  ① CalculateMoves(direction) を呼び出し
     → (hasMoved, moves[], merges[]) を返却

  ② if (!hasMoved) return MoveResult.None

  ③ moves に基づき CardRegistry の論理位置を更新
     CardRegistry.UpdateCardPosition(card, newRow, newCol)
     card.SetGridPositionLogical(newRow, newCol)  ← アニメ前に論理位置だけ更新

  ④ merges の吸収カードも論理位置を更新
     → 生存カードの位置に移動させる

  ⑤ AnimateMovesAsync(allMoveTargets, ct)
     → 全カードを DOAnchorPos で並列移動（Ease.OutQuart, 0.1s）

  ⑥ MoveResult を構築して返却
```

**CalculateMoves のグリッド走査順**：
- **Left**：行ごとに col=0 → col=3（左から右）
- **Right**：行ごとに col=3 → col=0（右から左）
- **Up**：列ごとに row=0 → row=3（上から下）
- **Down**：列ごとに row=3 → row=0（下から上）

**マージ判定**：
- 移動先に同値カードがあり、そのカードが今ターンまだマージされていない場合
- `HashSet<(int,int)> mergedPositions` で二重マージを防止

---

### 4. Merge 処理（TurnController 側）

```
[TurnController.ProcessTurnAsync 内のマージループ]
  foreach (survivor, absorbed, newValue) in result.Merges:
    ① CardRegistry.RemoveCard(absorbed)
    ② Destroy(absorbed.gameObject)
    ③ CardRegistry.UpdateCardValue(survivor, newValue)
    ④ _scoreManager.AddScore(newValue)
    ⑤ if (newValue >= WIN_VALUE)
         _gameStateManager.SetCleared()
```

**重要なルール**：
- 1 ターンで 1 つのカードは最大 1 回のマージ
- 例：`[2,2,2,2]` → `[4,4,0,0]`（`[8,0,0,0]` にはならない）

---

### 5. Spawn フロー

```
[TurnController.ProcessTurnAsync → SpawnCard]
  ① _cardSpawner.SpawnCard() を呼び出し

[CardSpawner.SpawnCard]
  ① grid = _cardRegistry.Grid
  ② FindEmptySpots(grid) → List<(int row, int col)>
  ③ if (emptySpots.Count == 0) return null
  ④ ランダムに空きマスを選択
  ⑤ Value を決定（SpawnValue2Weight : SpawnValue4Weight の比重）
     → GameDatabase.Instance から取得
  ⑥ CardView card = Instantiate(_cardPrefab, _cardParent)
     card.Initialize(row, col, value)
     _cardRegistry.AddCard(card)
  ⑦ return (row, col, value)
```

---

### 6. Judgment フロー

```
[TurnController.ProcessTurnAsync → ゲームオーバー判定]
  ① if (!MoveJudger.CanMove(_cardRegistry.Grid, gridSize))
       _gameStateManager.SetGameOver()

[MoveJudger.CanMove(int[,] grid, int size)]  ← static メソッド
  ① グリッドに空きマスがあるか？
     → True なら return true
  ② 隣接同値があるか？（右・下隣接をチェック）
     → True なら return true
  ③ return false（ゲームオーバー）
```

---

## アニメーション時間

| 処理 | 時間 | ツール |
|------|------|--------|
| カード移動 | 0.1s | DOTween DOAnchorPos |

> マージスケールアニメーション・スポーンアニメーションは未実装（拡張ポイント）。
> 詳細は [EXTENSION_GUIDE.md](EXTENSION_GUIDE.md) を参照。

---

## ゲームオーバー状態の判定

**ゲームオーバーになる条件**：
1. 空きマスがない
2. かつ隣接同値がない

**実装（MoveJudger.CanMove）**：
```csharp
public static bool CanMove(int[,] grid, int size)
{
    for (int r = 0; r < size; r++)
    {
        for (int c = 0; c < size; c++)
        {
            if (grid[r, c] == 0) return true;  // 空きマス
            int val = grid[r, c];
            if (c < size - 1 && grid[r, c + 1] == val) return true;  // 右隣接
            if (r < size - 1 && grid[r + 1, c] == val) return true;  // 下隣接
        }
    }
    return false;  // ゲームオーバー
}
```

---

## クリア判定

- マージ時に `newValue >= 2048` となった時点で `GameStateManager.SetCleared()` を呼び出し
- `WIN_VALUE` 定数は `TurnController.cs` で定義
