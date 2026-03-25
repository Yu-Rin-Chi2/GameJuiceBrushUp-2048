# 2048 パズルゲーム — Unity サンプル

**ゲームジャム向けの学習・参考用プロジェクト** です。

Unity 6 と uGUI で 2048 パズルゲームを実装しています。基本的なゲームロジックと移動アニメーションが実装済みで、**マージ演出・スポーンエフェクト・SE** などの拡張を想定した設計になっています。

![Unity](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity)

---

## 遊び方

| 操作                            | 動作                         |
| ------------------------------- | ---------------------------- |
| **矢印キー** / **WASD**         | カードを指定の方向にスライド |
| **スワイプ**（タッチ / マウス） | カードを指定の方向にスライド |
| **R**                           | ゲームをリセット             |

1. **スライド** — 矢印キーを押すと、すべてのカードがその方向に可能な限り移動します。
2. **マージ** — 同じ数字のカードが隣接すると自動合体（1 ターン 1 回まで）。
3. **スコア** — マージ時の合成値がスコアに加算（例：2+2=4 → +4 点）。
4. **スポーン** — 移動後、空きマスに新カード出現（2 が 90%、4 が 10%）。
5. **クリア** — **2048** のカードが出現すればクリア。
6. **ゲームオーバー** — 空きマスがなく、隣接する同じ数字もない状態。

---

## セットアップ

### 必要環境

- **Unity 6**（`6000.0` 以上を推奨）
- **DOTween**（無料版）— 事前に [Unity Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) から My Assets に追加しておいてください

### インストール手順

```bash
git clone https://github.com/Yu-Rin-Chi2/GameJuiceBrushUp-2048.git
```

1. **Unity Hub** → Add → クローンしたフォルダを選択（Unity 6 以上）
2. Unity エディタで **Window → Package Manager** → **My Assets** → DOTween を検索 → **Import**
3. `Assets/0MyAssets/Scenes/PlayScene.unity` を開く
4. **Play**（`Ctrl + P`）

> **Note:** クローンした状態ではDOTweenが入っていないので、コンパイルエラーが発生します。DOTween をインポートすると解消されます。

---

## 使用ライブラリ

| パッケージ   | バージョン   |
| ------------ | ------------ |
| Unity        | 6000.0 以上  |
| Input System | 1.14.2       |
| UniTask      | latest (git) |
| DOTween      | （ローカル） |

---

## プロジェクト構成

```
Assets/0MyAssets/
├── Script/
│   ├── Board/          # グリッド設定・方向 enum
│   ├── Card/           # カード表示・移動・生成・状態管理
│   ├── Controller/     # ターンのフロー制御
│   ├── Input/          # キーボード入力
│   ├── Manager/        # シングルトン・スコア・ゲーム状態・判定
│   └── UI/             # UI パネル・スコア表示
├── Prefab/             # Card_Base.prefab
├── Settings/           # BoardSettings.asset, CardColorTable.asset
└── Scenes/             # PlayScene.unity
```

> 拡張ポイントや処理フローの概要は [docs/EXTENSION_GUIDE.md](docs/EXTENSION_GUIDE.md) を参照してください。

---

## カスタマイズ

| 項目               | 変更箇所                                     | 備考                       |
| ------------------ | -------------------------------------------- | -------------------------- |
| グリッドサイズ     | `BoardSettings.asset` → `GridSize`           | コード変更不要             |
| カード色           | `CardColorTable.asset` → `Entries`           | 値ごとに背景色・文字色設定 |
| アニメーション速度 | `CardMover` コンポーネント → `_moveDuration` | デフォルト 0.1 秒          |

> アニメーション・SE・エフェクトの追加方法は [docs/EXTENSION_GUIDE.md](docs/EXTENSION_GUIDE.md) を参照してください。

---

## ドキュメント

| ドキュメント                                       | 内容                                   |
| -------------------------------------------------- | -------------------------------------- |
| [docs/EXTENSION_GUIDE.md](docs/EXTENSION_GUIDE.md) | 拡張ポイント・ログ出力・Inspector 設定 |

---

## 注意事項

- このプロジェクトは **ゲームジャム向けの学習・参考用** です。本プロジェクトをそのまま、または改変して商用サービスやアプリとして公開・配布することはご遠慮ください。
- DOTween など、再配布が制限されているサードパーティアセットを含むため、MIT 等のオープンソースライセンスでは公開していません。

### サードパーティライセンス

| パッケージ                                    | ライセンス                                           |
| --------------------------------------------- | ---------------------------------------------------- |
| [DOTween](http://dotween.demigiant.com/)      | DOTween 独自ライセンス（Asset Store 経由で各自取得） |
| [UniTask](https://github.com/Cysharp/UniTask) | MIT ライセンス                                       |

---

## 謝辞

Gabriele Cirulli による [2048](https://github.com/gabrielecirulli/2048) の GitHub リポジトリを参考に、Unity で再実装しました。
