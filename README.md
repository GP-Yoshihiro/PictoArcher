# PictoArcher

文字を使わないアクションゲーム。Unity 6（URP 2D）。

## はじめに読むもの

| もの | 場所 |
|---|---|
| 仕様書（概要・ウェーブ・敵・強化・スケジュール・作業工程・工数・チェックリスト） | `docs/PictoArcher_仕様書.xlsx` |
| 開発ルール（命名、構成、Issue、ブランチ、PR、マージ） | `docs/development-rules.md` |
| Claude のルール | `CLAUDE.md`、`.claude/` |
| 設計書・計画 | `docs/superpowers/specs/`、`docs/superpowers/plans/` |

作業を始めるときは、開発ルール §3 の確認（開いている Issue・PR・ブランチを見る）をしてから、Issue の担当になる。

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

## 仕様書の作り直し

仕様書 `.xlsx` はスクリプトで作る。Excel で直接書き換えない（黄色の入力セルは除く）。

```
pip3 install openpyxl
python3 tools/spec/build_spec_xlsx.py
```

黄色の入力セル（実績、状態、Issue など）は、前の `.xlsx` から引き継がれる。引き継がずに作るときは `--fresh`。

## Claude Code の準備

`.claude/settings.json` で `superpowers` プラグインを有効にしている。初めてのときは Claude Code で `/plugin` を開き、`claude-plugins-official` から `superpowers` を入れる。スキル（`.claude/skills/`）とサブエージェント（`.claude/agents/`）はリポジトリに入っているので、そのまま使える。フックは共有しない。

## クレジット（CC-BY など）

（まだなし）
