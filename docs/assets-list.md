# 素材一覧

取り込んだ素材をここに書く。取り込む前に、文字が入っていないか確認する。
フリー素材は、表記（クレジット）の要らないもの（CC0、OFL のフォントなど）だけを使う。クレジットは作らないので、CC-BY など表記が要る素材は使わない。ライセンス文が要るもの（OFL など）は、素材の隣に置く。

| ファイル | 入手先URL | ライセンス | 使用箇所 | 文字の確認 |
|---|---|---|---|---|
| `Assets/_Project/Fonts/Fredoka-Variable.ttf`（ライセンス文 `Fredoka-OFL.txt`） | https://github.com/google/fonts/tree/main/ofl/fredoka | SIL Open Font License 1.1 | `Assets/_Project/Fonts/Digits.asset`（TMP の既定フォント。`0123456789+-×/%` だけを登録） | フォントアセットに数字と記号だけが入っていることを `NoTextGuardTests` が確かめる。元のフォントの `★` `⏱` はない |

## 仮の絵（自作）

本番の絵が届くまで見た目の表に入れておく絵。外から取り込んでいないので入手先とライセンスはない（この PJ で描いた）。本番の絵に差し替えたら行を消す。

| ファイル | 入手先URL | ライセンス | 使用箇所 | 文字の確認 |
|---|---|---|---|---|
| `Assets/_Project/Art/Placeholder/UI/` の `Play` `Pause` `Retry` `Home` `Settings` `Locked` `Volume`（ボタン） | なし（自作） | なし（自作） | `Data/Visuals/UiIconVisual.asset` のボタンの枠 | 図形だけ。文字なしを目で確認した |
| `Assets/_Project/Art/Placeholder/UI/` の `Health` `Coin` `Experience` `Level` `Timer` `Boss` `Warning` `BarFill`（HUD） | なし（自作） | なし（自作） | `UiIconVisual.asset` の HUD の枠、HUD のバー | 図形だけ。文字なしを目で確認した |
| `Assets/_Project/Art/Placeholder/UI/` の `WeaponGun` `WeaponLaser` `WeaponLauncher` `CardFrame` `Trophy`（武器・カード・結果） | なし（自作） | なし（自作） | `UiIconVisual.asset` の武器・カード・結果の枠 | 図形だけ。文字なしを目で確認した |
| `Assets/_Project/Art/Placeholder/UI/` の `GuideArrowKey` `GuideMouse` `GuideHand`（操作案内） | なし（自作） | なし（自作） | `UiIconVisual.asset` の操作案内の枠、`Prefabs/UI/ControlGuide.prefab` | 矢印キーは矢印だけで英字を描いていない。マウスと手は形だけ。文字なしを目で確認した |
