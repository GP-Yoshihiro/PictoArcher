# PictoArcher

文字を使わないアクションゲーム。Unity 6（URP 2D）。

## Unity のバージョン

6000.5.10f1。全員これに固定する。

## テストの実行

Unity のエディタを閉じて、次を実行する。

```
tools/run-editmode-tests.sh
```

結果は `Logs/editmode-results.xml`。失敗があると終了コードが 0 以外になる。

## UnityYAMLMerge の設定

シーンやプレハブの衝突を減らすため、各自の `~/.gitconfig`（Windows は `%USERPROFILE%\.gitconfig`）に次を書く。`.gitattributes` は設定済み。

Mac：

```
[merge "unityyamlmerge"]
    name = Unity SmartMerge
    driver = '/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/Tools/UnityYAMLMerge' merge -p %O %B %A %A
```

Windows：

```
[merge "unityyamlmerge"]
    name = Unity SmartMerge
    driver = 'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Data\Tools\UnityYAMLMerge.exe' merge -p %O %B %A %A
```

Unity のインストール先が違う場合は、パスを直す。

## クレジット（CC-BY など）

（まだなし）
