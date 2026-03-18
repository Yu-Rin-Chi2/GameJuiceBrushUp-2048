# GameJam-2048Sample CLAUDE.md

## プロジェクト概要
Unity (uGUI) による 2048 パズルゲーム。
キーボード操作でカードをスライド・マージし、2048の数値を目指す。

## ゲームのルール

### 基本ルール
1. **入力** → キーボード（上下左右）でカードを操作
2. **移動** → インプットした方向に、すべてのカードが最も遠い位置まで移動
3. **マージ** → 移動後、同じ数字のカード同士が隣接していれば自動合体
4. **スコア** → マージするたびにスコア加算
5. **スポーン** → 毎ターンランダムに新しいカード（2 or 4）が生成
6. **ゲームオーバー** → 空きマスがなく、かつ隣接同値がない状態
7. **クリア** → 2048のカードが出現

### タイル生成
- **確率**：2 が 90%、4 が 10%
- **生成位置**：ランダムに空いているマス

### スコア
- マージ時：合成値 × 1 をスコアに加算
- 例：2+2=4 → スコア +4

---

## 技術スタック
- Unity (URP)
- uGUI / Canvas（UI実装）
- Input System (com.unity.inputsystem)
- UniTask（非同期処理）
- TextMesh Pro
- DOTween（アニメーション）

---

## ディレクトリ構成
```
Assets/0MyAssets/
├── Script/
│   ├── Board/          # BoardSettings.cs, Direction.cs
│   ├── Card/           # CardView, CardMover, CardRegistry, CardSpawner, CardColorTable, MoveResult
│   ├── Controller/     # TurnController.cs
│   ├── Input/          # InputEventController.cs
│   ├── Manager/        # GameDatabase, GameStateManager, ScoreManager, MoveJudger
│   └── UI/             # GameUIController.cs
├── Prefab/Card_Base.prefab
├── Settings/（BoardSettings.asset, CardColorTable.asset）
└── Scenes/PlayScene.unity

docs/
├── CLASS_REFERENCE.md     # クラス構造・アーキテクチャ・Inspector設定
├── EXTENSION_GUIDE.md     # アニメーション・SE・エフェクト拡張ガイド
├── FLOW.md                # 処理フロー詳細
└── NAMING.md              # 命名規則
```

---

## グリッド座標
- **サイズ**：4×4
- **左上**：(90, -90)
- **右下**：(510, -510)
- **間隔**：140px
- **公式**：`x = 90 + col * 140`, `y = -90 - row * 140`

---

## 設計ドキュメント

詳細な設計情報は docs フォルダを参照：
- [CLASS_REFERENCE.md](docs/CLASS_REFERENCE.md) - クラス構造・アーキテクチャ・Inspector設定
- [EXTENSION_GUIDE.md](docs/EXTENSION_GUIDE.md) - アニメーション・SE・エフェクト拡張ガイド
- [FLOW.md](docs/FLOW.md) - 処理フロー詳細
- [NAMING.md](docs/NAMING.md) - 命名規則
