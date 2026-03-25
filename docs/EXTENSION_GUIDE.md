# 拡張ガイド

## 全体構造

```
Input(InputEventController) → Controller(TurnController) → Card/Manager → View/UI
```

演出を追加するなら **TurnController** か **CardView** を触る。
設定データは全て **GameDatabase.Instance** 経由で取得する。

---

## 処理フロー

```
1. InputEventController: 方向キー → OnSwipe(Direction) 発火
2. TurnController: HandleSwipe → ProcessTurnAsync 開始（二重入力ロック）
3. CardMover: ExecuteTurnAsync → 移動先・マージ先を計算 → DOTween で並列アニメーション → MoveResult 返却
4. TurnController: マージ後処理（absorbed を Destroy、survivor の値更新、スコア加算、2048チェック）
5. CardSpawner: SpawnCard → 空きマスにランダム生成（2:90%, 4:10%）
6. MoveJudger: CanMove → false ならゲームオーバー
7. TurnController: 二重入力ロック解除
```

---

## 拡張ポイント

| やりたいこと                         | 触るファイル        | 触るメソッド                                    |
| ------------------------------------ | ------------------- | ----------------------------------------------- |
| マージ演出（スケール・パーティクル） | TurnController.cs   | `ProcessTurnAsync()` のマージループ内           |
| スポーン演出（ポップイン・フェード） | CardSpawner.cs      | `SpawnCard()` の `Initialize()` 直後            |
| 移動アニメーション変更               | CardMover.cs        | `AnimateMovesAsync()`                           |
| カード色変更アニメーション           | CardView.cs         | `SetValue()`                                    |
| スライド SE                          | TurnController.cs   | `ProcessTurnAsync()` の `ExecuteTurnAsync` 直後 |
| マージ SE                            | TurnController.cs   | `ProcessTurnAsync()` のマージループ内           |
| ゲームオーバー・クリア演出           | GameUIController.cs | `ShowGameOver()` / `ShowCleared()`              |

---

## ログ出力

Console で動作確認する際の参考用。

| タグ           | メッセージ例                            | タイミング                  |
| -------------- | --------------------------------------- | --------------------------- |
| `[Spawn]`      | `[Spawn] (2, 3) = 4`                    | カード生成時（毎ターン）    |
| `[Move Start]` | `[Move Start] Direction=Left, Cards=3`  | 移動開始時                  |
| `[Move End]`   | `[Move End] Merges=1, MergedValues=[8]` | 移動完了時                  |
| `[GameOver]`   | `[GameOver] No moves available.`        | ゲームオーバー時（1回のみ） |
| `[Clear]`      | `[Clear] 2048 achieved!`                | 2048達成時（1回のみ）       |

---
