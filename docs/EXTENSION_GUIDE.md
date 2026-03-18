# 拡張ガイド — アニメーション・エフェクト・SE の追加方法

## はじめに

本プロジェクトは基本的なカード移動アニメーション（DOTween による `DOAnchorPos`）のみが実装された、動作完成状態の 2048 ゲームです。
このガイドは以下を追加したい開発者向けに、**どのファイルのどこに何を書けばよいか**を具体的に示します。

**追加できる主な拡張**
- マージ時のスケールポップ、パーティクルバースト、フロートテキスト
- スポーン時のフェードイン・ポップインアニメーション
- スライド SE、マージ SE（音程バリエーション付き）
- ゲームオーバー・クリア時のパネルフェードイン・BGM 切替
- カード色変更時のカラートゥイーン・グロー効果

**前提知識**
- DOTween の基本操作（`DOAnchorPos`, `DOScale`, `DOColor`, `Sequence`）
- UniTask の `async/await`（`await` で DOTween を待機する `.ToUniTask()` パターン）
- Unity の `AudioSource` と `AudioClip` の基本操作

---

## 拡張ポイント一覧

| # | 拡張内容 | 対象ファイル | 対象メソッド | 難易度 |
|---|----------|-------------|-------------|--------|
| 1 | マージ演出（スケールポップ・パーティクル） | `TurnController.cs` | `ProcessTurnAsync()` | ★★☆ |
| 2 | カードスポーン演出（フェード・ポップイン） | `CardSpawner.cs` | `SpawnCard()` | ★☆☆ |
| 3 | カード移動アニメーションの強化 | `CardMover.cs` | `AnimateMovesAsync()` | ★☆☆ |
| 4 | スライド SE | `TurnController.cs` | `ProcessTurnAsync()` | ★☆☆ |
| 5 | マージ SE（音程バリエーション） | `TurnController.cs` | `ProcessTurnAsync()` | ★☆☆ |
| 6 | ゲームオーバー・クリア演出 | `GameUIController.cs` | `ShowGameOver()` / `ShowCleared()` | ★★☆ |
| 7 | カード色変更アニメーション | `CardView.cs` | `SetValue()` | ★★☆ |

---

## 1. マージ演出の追加 ★★☆

**対象ファイル:** `Assets/0MyAssets/Script/Controller/TurnController.cs`
**対象メソッド:** `ProcessTurnAsync()`

### 現在のコード（TurnController.cs 76〜83行目）

```csharp
// マージ後処理（Model層操作はController側で）
if (result.Merges != null)
{
    foreach (var (survivor, absorbed, newValue) in result.Merges)
    {
        Object.Destroy(absorbed.gameObject);
        _cardRegistry.UpdateCardValue(survivor, newValue);
    }
}
```

### 問題点

`UpdateCardValue()` は即座に数値と色を変更するだけで、視覚的なフィードバックがありません。
マージが発生したことをプレイヤーが認知しにくい状態です。

### 追加方法：スケールポップ

`Object.Destroy(absorbed.gameObject)` の直後、`_cardRegistry.UpdateCardValue()` の後に
スケールアニメーションを追加します。`ProcessTurnAsync` を `async UniTask` にする必要があります（すでにそうなっています）。

```csharp
if (result.Merges != null)
{
    foreach (var (survivor, absorbed, newValue) in result.Merges)
    {
        Object.Destroy(absorbed.gameObject);
        _cardRegistry.UpdateCardValue(survivor, newValue);

        // ---- ここから追加 ----
        // マージ後にスケールポップ演出（1 → 1.2 → 1.0）
        await survivor.RectTransform
            .DOScale(1.2f, 0.08f)
            .SetEase(Ease.OutQuad)
            .ToUniTask(cancellationToken: ct);
        await survivor.RectTransform
            .DOScale(1.0f, 0.08f)
            .SetEase(Ease.InQuad)
            .ToUniTask(cancellationToken: ct);
        // ---- ここまで追加 ----
    }
}
```

### 追加方法：スコアフロートテキスト

マージ値を画面上に浮かせて消すフロートテキストはこの位置に追加します。

```csharp
// _cardRegistry.UpdateCardValue() の直後に挿入
// FloatTextSpawner は別途作成するコンポーネント（参考設計は後述）
_floatTextSpawner.Spawn($"+{newValue}", survivor.RectTransform.anchoredPosition);
```

### 追加方法：パーティクルバースト

```csharp
// survivor の位置にパーティクルを生成（ParticleSystem.Play() でも可）
var particle = Instantiate(_mergeParticlePrefab, _cardParent);
particle.GetComponent<RectTransform>().anchoredPosition =
    survivor.RectTransform.anchoredPosition;
particle.GetComponent<ParticleSystem>().Play();
Destroy(particle, 2f); // 2秒後に自動削除
```

---

## 2. カードスポーン演出 ★☆☆

**対象ファイル:** `Assets/0MyAssets/Script/Card/CardSpawner.cs`
**対象メソッド:** `SpawnCard()`

### 現在のコード（CardSpawner.cs 38〜43行目）

```csharp
// Prefabを生成し、初期化し、CardRegistryに登録
var card = Instantiate(_cardPrefab, _cardParent);
card.Initialize(row, col, value);
_cardRegistry.AddCard(card);

Debug.Log($"[Spawn] ({row}, {col}) = {value}");
return (row, col, value);
```

### 問題点

`Instantiate()` の直後に `Initialize()` を呼んでいるため、カードが突然出現します。
スポーンアニメーションを追加するには、`SpawnCard()` を非同期化するか、
または `Initialize()` 後にスケール 0 から始める方法が簡単です。

### 追加方法：ポップイン（スケール 0→1）

`SpawnCard()` は同期メソッドなので、`Initialize()` の後にアニメーションを起動して
`Forget()` するだけで問題なく動作します。

```csharp
var card = Instantiate(_cardPrefab, _cardParent);
card.Initialize(row, col, value);
_cardRegistry.AddCard(card);

// ---- ここから追加 ----
// スポーン時にスケールポップイン（0 → 1.1 → 1.0）
card.RectTransform.localScale = Vector3.zero;
card.RectTransform
    .DOScale(1.1f, 0.12f)
    .SetEase(Ease.OutBack)
    .OnComplete(() =>
        card.RectTransform.DOScale(1.0f, 0.06f).SetEase(Ease.InQuad))
    .Play();
// ---- ここまで追加 ----

Debug.Log($"[Spawn] ({row}, {col}) = {value}");
return (row, col, value);
```

### 追加方法：フェードイン

カードの `CanvasGroup` コンポーネントを使ってフェードインする場合：

```csharp
// CardView に CanvasGroup コンポーネントを追加しておく
var cg = card.GetComponent<CanvasGroup>();
if (cg != null)
{
    cg.alpha = 0f;
    cg.DOFade(1f, 0.15f).SetEase(Ease.OutQuad).Play();
}
```

---

## 3. カード移動アニメーションの強化 ★☆☆

**対象ファイル:** `Assets/0MyAssets/Script/Card/CardMover.cs`
**対象メソッド:** `AnimateMovesAsync()`

### 現在のコード（CardMover.cs 147〜161行目）

```csharp
private async UniTask AnimateMovesAsync(List<(CardView card, Vector2 pos)> moves, CancellationToken ct)
{
    // 全カードのDOTweenタスクを作成
    var tasks = new List<UniTask>();
    foreach (var (card, pos) in moves)
    {
        // 各カードを目標位置へ移動
        tasks.Add(card.RectTransform
            .DOAnchorPos(pos, _moveDuration)
            .SetEase(_moveEase)
            .ToUniTask());
    }
    // 全タスクを待機
    await UniTask.WhenAll(tasks);
}
```

### 現在の設定

- `_moveDuration`：0.1秒（Inspector で変更可能）
- `_moveEase`：`Ease.OutQuart`（Inspector で変更可能）

### 追加方法：バウンス

`_moveEase` を `Ease.OutBounce` に変更するだけで実現できますが、
バウンス後の静止位置がずれて見える場合は以下のように `Sequence` を使います。

```csharp
tasks.Add(card.RectTransform
    .DOAnchorPos(pos, _moveDuration)
    .SetEase(Ease.OutBounce)
    .ToUniTask(cancellationToken: ct));
```

### 追加方法：移動中に軽く回転させる

```csharp
var seq = DOTween.Sequence();
seq.Append(card.RectTransform.DOAnchorPos(pos, _moveDuration).SetEase(_moveEase));
seq.Join(card.RectTransform.DORotate(new Vector3(0, 0, -3f), _moveDuration * 0.5f)
    .SetEase(Ease.OutQuad)
    .OnComplete(() => card.RectTransform.DORotate(Vector3.zero, _moveDuration * 0.5f)));
tasks.Add(seq.ToUniTask(cancellationToken: ct));
```

### 注意点

`AnimateMovesAsync` の `ToUniTask()` に `cancellationToken: ct` を渡すと
シーン破棄時にアニメーションが安全にキャンセルされます。現在のコードでは `ct` が未指定なので
併せて修正することを推奨します。

---

## 4. スライド SE ★☆☆

**対象ファイル:** `Assets/0MyAssets/Script/Controller/TurnController.cs`
**対象メソッド:** `ProcessTurnAsync()`

### 挿入位置（TurnController.cs 72〜73行目付近）

```csharp
var result = await _cardMover.ExecuteTurnAsync(direction, ct);
if (!result.HasMoved) return;

// ---- ここに追加 ----
// カードが実際に移動したときだけ SE を再生
AudioManager.Instance.PlaySE(_slideSEClip);
// ---- ここまで追加 ----
```

`_slideSEClip` は `[SerializeField] private AudioClip _slideSEClip;` として
Inspector からアサインします。

`AudioManager` の設計は末尾の「AudioManager の設計提案」を参照してください。

### InputEventController での SE 再生（代替案）

キー押下の瞬間に SE を鳴らしたい場合は `InputEventController.cs` の
`FireSwipe()` メソッド（65〜68行目）に追加します。

```csharp
private void FireSwipe(Direction dir)
{
    if (!_inputEnabled) return;
    // ---- ここに追加 ----
    AudioManager.Instance.PlaySE(_slideSEClip);
    // ---- ここまで追加 ----
    OnSwipe?.Invoke(dir);
}
```

ただし、この位置では「移動できなかった場合でも SE が鳴る」ため、
実際に移動があった場合のみ鳴らすには `TurnController` 側への挿入を推奨します。

---

## 5. マージ SE ★☆☆

**対象ファイル:** `Assets/0MyAssets/Script/Controller/TurnController.cs`
**対象メソッド:** `ProcessTurnAsync()` のマージループ内

### 挿入位置（TurnController.cs 78〜83行目付近）

```csharp
foreach (var (survivor, absorbed, newValue) in result.Merges)
{
    Object.Destroy(absorbed.gameObject);
    _cardRegistry.UpdateCardValue(survivor, newValue);

    // ---- ここに追加 ----
    // マージ値に応じてピッチを変える（高い値 = 高いピッチ）
    float pitch = Mathf.Log(newValue, 2) / 11f; // 4〜2048 → 0.18〜1.0 程度
    AudioManager.Instance.PlaySE(_mergeSEClip, pitch);
    // ---- ここまで追加 ----
}
```

`Mathf.Log(newValue, 2)` は 2の対数なので `4=2, 8=3, ... 2048=11` となり、
`11f` で割ることで 0.18〜1.0 の範囲に正規化できます。
ピッチの感触はプロジェクトの SE に合わせて調整してください。

### AudioManager に対応したオーバーロードの追加

```csharp
// AudioManager 内に追加
public void PlaySE(AudioClip clip, float pitch = 1f)
{
    if (clip == null) return;
    _seSource.pitch = pitch;
    _seSource.PlayOneShot(clip);
}
```

---

## 6. ゲームオーバー・クリア演出 ★★☆

**対象ファイル:** `Assets/0MyAssets/Script/UI/GameUIController.cs`
**対象メソッド:** `ShowGameOver()` / `ShowCleared()`

### 現在のコード（GameUIController.cs 60〜71行目）

```csharp
/// <summary>ゲームオーバーパネルを表示。GameStateManagerのイベント購読先</summary>
private void ShowGameOver()
{
    HideAllPanels();
    _gameOverPanel.SetActive(true);
}

/// <summary>ゲームクリアパネルを表示。GameStateManagerのイベント購読先</summary>
private void ShowCleared()
{
    HideAllPanels();
    _clearedPanel.SetActive(true);
}
```

### 問題点

`SetActive(true)` で即座に表示するため、画面遷移が唐突に感じられます。

### 追加方法：CanvasGroup フェードイン

各パネルの GameObject に `CanvasGroup` コンポーネントを追加し、
`ShowGameOver()` / `ShowCleared()` を非同期化します。

```csharp
// using Cysharp.Threading.Tasks; を追加
// using DG.Tweening; を追加

private void ShowGameOver()
{
    ShowPanelAsync(_gameOverPanel, destroyCancellationToken).Forget();
}

private void ShowCleared()
{
    ShowPanelAsync(_clearedPanel, destroyCancellationToken).Forget();
    // クリア時に BGM 変更したい場合はここで AudioManager を呼ぶ
    // AudioManager.Instance.PlayBGM(_clearBGMClip);
}

private async UniTask ShowPanelAsync(GameObject panel, CancellationToken ct)
{
    HideAllPanels();
    panel.SetActive(true);

    var cg = panel.GetComponent<CanvasGroup>();
    if (cg == null)
    {
        // CanvasGroup がなければ即表示
        return;
    }

    cg.alpha = 0f;
    await cg.DOFade(1f, 0.4f)
        .SetEase(Ease.OutQuad)
        .ToUniTask(cancellationToken: ct);
}
```

### 追加方法：パネルのスライドイン（上から降下）

```csharp
private async UniTask ShowPanelAsync(GameObject panel, CancellationToken ct)
{
    HideAllPanels();
    panel.SetActive(true);

    var rt = panel.GetComponent<RectTransform>();
    var originalY = rt.anchoredPosition.y;
    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, originalY + 100f);

    await rt.DOAnchorPosY(originalY, 0.3f)
        .SetEase(Ease.OutBack)
        .ToUniTask(cancellationToken: ct);
}
```

### GameStateManager のイベントを活用した BGM 変更

`GameUIController` と独立した `BGMController` を作成し、
`GameStateManager.OnGameOver` / `OnGameCleared` を購読させる設計が疎結合で推奨です。

```csharp
// BGMController.cs（新規作成）
public class BGMController : MonoBehaviour
{
    [SerializeField] private GameStateManager _gameStateManager;
    [SerializeField] private AudioClip _normalBGM;
    [SerializeField] private AudioClip _gameOverBGM;
    [SerializeField] private AudioClip _clearBGM;

    private void Start()
    {
        _gameStateManager.OnGameOver += () => AudioManager.Instance.PlayBGM(_gameOverBGM);
        _gameStateManager.OnGameCleared += () => AudioManager.Instance.PlayBGM(_clearBGM);
        AudioManager.Instance.PlayBGM(_normalBGM);
    }
}
```

---

## 7. カード色変更アニメーション ★★☆

**対象ファイル:** `Assets/0MyAssets/Script/Card/CardView.cs`
**対象メソッド:** `SetValue()`

### 現在のコード（CardView.cs 68〜78行目）

```csharp
public void SetValue(int value)
{
    Value = value;
    _valueText.text = value.ToString();
    // CardColorTableから数値に対応した色を取得して適用
    if (GameDatabase.Instance.TryGetCardColors(value, out var bgColor, out var textColor))
    {
        _backgroundImage.color = bgColor;
        _valueText.color = textColor;
    }
}
```

### 問題点

`_backgroundImage.color` の代入が即時適用のため、マージ後の色変化が瞬間的です。

### 追加方法：DOTween カラートゥイーン

`SetValue()` を非同期化せずに DOTween を `.Forget()` する方式が最もシンプルです。

```csharp
// using DG.Tweening; を追加

public void SetValue(int value)
{
    Value = value;
    _valueText.text = value.ToString();

    if (GameDatabase.Instance.TryGetCardColors(value, out var bgColor, out var textColor))
    {
        // ---- 変更箇所 ----
        // 即時代入から DOTween カラーアニメーションに変更
        _backgroundImage.DOColor(bgColor, 0.15f).SetEase(Ease.OutQuad).Play();
        _valueText.DOColor(textColor, 0.15f).SetEase(Ease.OutQuad).Play();
        // ---- 変更ここまで ----
    }
}
```

### 注意点

`Initialize()` → `SetValue()` の初回呼び出し時にもアニメーションが再生されます。
スポーン時は即時適用したい場合は、初期化フラグを持つか
以下のように `Initialize()` 内で直接代入するよう分岐します。

```csharp
public void Initialize(int row, int col, int value)
{
    SetGridPosition(row, col);
    SetValueImmediate(value); // アニメーションなし版を追加
}

/// <summary>アニメーションなしで値を即時設定（初期化専用）</summary>
private void SetValueImmediate(int value)
{
    Value = value;
    _valueText.text = value.ToString();
    if (GameDatabase.Instance.TryGetCardColors(value, out var bgColor, out var textColor))
    {
        _backgroundImage.color = bgColor;
        _valueText.color = textColor;
    }
}
```

### 追加方法：グロー（発光）エフェクト

UI の発光には `Outline` や `Shadow` コンポーネントを動的に変化させる方法と、
`_backgroundImage` に Material を使う方法があります。
DOTween の `DOFloat` でシェーダーパラメータをアニメーションさせる例：

```csharp
// Material に "_GlowIntensity" プロパティがある前提
_backgroundImage.material.DOFloat(1.5f, "_GlowIntensity", 0.1f)
    .OnComplete(() =>
        _backgroundImage.material.DOFloat(0f, "_GlowIntensity", 0.2f));
```

---

## AudioManager の設計提案（参考）

### クラス設計

以下のシンプルなシングルトン `AudioManager` を `Assets/0MyAssets/Script/Manager/` に作成することを推奨します。

```csharp
using UnityEngine;

/// <summary>
/// SE・BGM を管理するシングルトン。
/// 各拡張ポイントからこのクラス経由で音を再生する。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioSource _bgmSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>SE を1回再生。</summary>
    /// <param name="clip">再生するクリップ</param>
    /// <param name="pitch">ピッチ（デフォルト 1.0）</param>
    public void PlaySE(AudioClip clip, float pitch = 1f)
    {
        if (clip == null) return;
        _seSource.pitch = pitch;
        _seSource.PlayOneShot(clip);
    }

    /// <summary>BGM を再生（同じクリップなら再起動しない）。</summary>
    /// <param name="clip">再生するクリップ</param>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || _bgmSource.clip == clip) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    /// <summary>BGM を停止。</summary>
    public void StopBGM()
    {
        _bgmSource.Stop();
    }
}
```

### 各拡張ポイントからの呼び出し

| 拡張ポイント | 呼び出し例 |
|-------------|-----------|
| スライド SE | `AudioManager.Instance.PlaySE(_slideSEClip)` |
| マージ SE | `AudioManager.Instance.PlaySE(_mergeSEClip, pitch)` |
| スポーン SE | `AudioManager.Instance.PlaySE(_spawnSEClip)` |
| ゲームオーバー BGM | `AudioManager.Instance.PlayBGM(_gameOverBGMClip)` |
| クリア BGM | `AudioManager.Instance.PlayBGM(_clearBGMClip)` |
| BGM 停止 | `AudioManager.Instance.StopBGM()` |

### 推奨フォルダ構成

```
Assets/0MyAssets/
├── Script/
│   └── Manager/
│       └── AudioManager.cs        # 新規作成
├── Audio/
│   ├── SE/
│   │   ├── slide.wav              # スライド SE
│   │   ├── merge.wav              # マージ SE（基準ピッチ）
│   │   └── spawn.wav              # スポーン SE
│   └── BGM/
│       ├── bgm_normal.ogg         # 通常 BGM
│       ├── bgm_gameover.ogg       # ゲームオーバー BGM
│       └── bgm_clear.ogg          # クリア BGM
```

### Unity エディタでの設定手順

1. PlayScene に `Create Empty` → `AudioManager` と命名
2. `AudioManager.cs` をアタッチ
3. `AudioManager` の子として `AudioSource` を 2つ作成（`SE Source`, `BGM Source`）
4. `BGM Source` の `Loop` にチェックを入れる
5. Inspector で `_seSource`, `_bgmSource` に各 `AudioSource` をアサイン

---

## よくある質問

### Q: DOTween がインストールされていない場合は？

Unity Package Manager または DOTween 公式サイト（http://dotween.demigiant.com/）からインポートしてください。
インポート後に `DOTween Setup` ウィンドウが開くので `Setup DOTween...` ボタンを押して初期設定を完了させてください。
`using DG.Tweening;` が解決できない場合はアセンブリ定義（`.asmdef`）への参照追加が必要です。

### Q: UniTask の async/await がわからない場合は？

`UniTask` は `Task` の Unity 向け軽量版です。
DOTween アニメーションを `await` するには `.ToUniTask()` を末尾に付けます。

```csharp
// 例：0.3秒かけてスケールを 1.2 にしてから処理を続ける
await transform.DOScale(1.2f, 0.3f).ToUniTask(cancellationToken: ct);
Debug.Log("アニメーション完了");
```

### Q: マージアニメーションを追加したら動きがカクつく

`ProcessTurnAsync()` のマージループ内で `await` を使うと、マージが複数ある場合に
1つずつ順番に処理されます。複数マージを並列に行いたい場合は `UniTask.WhenAll` を使います。

```csharp
var mergeTasks = new List<UniTask>();
foreach (var (survivor, absorbed, newValue) in result.Merges)
{
    Object.Destroy(absorbed.gameObject);
    _cardRegistry.UpdateCardValue(survivor, newValue);

    var capturedSurvivor = survivor; // ラムダキャプチャ用にローカルコピー
    mergeTasks.Add(capturedSurvivor.RectTransform
        .DOScale(Vector3.one * 1.2f, 0.08f)
        .SetEase(Ease.OutQuad)
        .ToUniTask(cancellationToken: ct));
}
await UniTask.WhenAll(mergeTasks);
```

### Q: カスタム入力（ゲームパッド、タッチ）を追加したい

`InputEventController.cs` は `InputSystem_Actions`（Unity Input System の自動生成クラス）を使用しています。
`InputSystem_Actions.inputactions` アセットを開き、`Player` アクションマップに新しいバインディングを追加するか、
`FireSwipe(Direction dir)` を `public` にして外部の入力クラスから直接呼び出す方法があります。

タッチ入力（スワイプ）を追加する場合、Input System の `TouchScreen` バインディングを追加するか、
以下のように `InputEventController` に `SwipeDetector` コンポーネントを接続して
`OnSwipe?.Invoke(direction)` を発火させる方式が最も疎結合で推奨です。

```csharp
// InputEventController に追加
/// <summary>外部コンポーネント（タッチ入力など）から方向イベントを発火するためのメソッド</summary>
public void FireSwipeExternal(Direction direction) => FireSwipe(direction);
```
