---
name: minimal-read-edit
description: Use before reading source files to understand code, before editing or adding code, and when resuming work in a new session — whenever the urge is to open whole files, explore the project "to get context", rewrite a file, or re-read earlier conversation.
---

# 必要な所だけ読む、必要な所だけ直す

## なぜ
読むことが一番むだになりやすい。2,000行の C# は約2万トークンで、一度会話に入れたものは、そのあとの毎ターン読み直される。先に正確な行を見つけ、そこだけ読み、タスクに要る所だけ直す。

## 読み方の段（答えが出た段で止める）
| 知りたいこと | やること |
|---|---|
| X はどこで定義・使用されている？ | `grep -rn "X" Assets/_Project --include=*.cs` |
| このファイル・型に何がある？ | `python3 .claude/skills/minimal-read-edit/scripts/cs-outline.py <file.cs>` |
| コード全体の地図 | `cs-outline.py Assets/_Project`（1セッションに1回。`--grep Word` で絞る） |
| プレハブ・シーン・アセットの中の値 | `grep -n "fieldName" <file>.prefab`。YAML を丸ごと読まない |
| 1つのメソッドの中身 | `Read` を `offset`＝アウトラインの行−2、`limit`＝メソッドの長さで |
| ファイル全体 | 150行未満か、大半を書き換えるときだけ |

- まずプロジェクトの文書：`CLAUDE.md` のフォルダ構成と `docs/development-rules.md` で「X はどこ？」に答えが出ることが多い。
- 直したばかりのファイルを読み直さない。
- ログや XML を `cat` しない。`grep -n` で失敗の行だけ見るか、`log-triage` サブエージェントに任せる。

## 直し方
- 一番小さい一意の部分を目印に直す。1つの変更に1回の Edit、またはそれぞれの目印が1回だけ出ることを確かめるスクリプトで、まとめて置き換える。
- タスクに要る行だけ変える。触らない所の整形・名前の変更・「ついでの片付け」をしない。頼まれていない部品や抽象化を足さない。
- 新しいファイルを作る前に、似たものがないか grep する。あるならそれを広げる。
- 差分が見込みの2倍を超えそうなら、広げ続けずに止めて人に伝える。
- テストは振る舞いを示す少数だけ足す。既存の準備用のコードを使い回す。

## 新しいセッションで続きをするとき
- 会話をさかのぼらない。Issue、PR、`git log --oneline -15`、計画の見出しから始める。
- 長い出力（報告、レビュー、差分）はファイルに書き、結論の行だけ読む。
- サブエージェントに頼むときは、アウトラインの行番号付きでファイルの場所を渡し、探索させない。

## よくある失敗
- どのメソッドが大事か分かる前に、大きいファイルを3つ「文脈のために」開く → 先にアウトライン。
- 5行を変えるためにファイル全体を Write で書き直す → Edit。
- サブエージェントごとに同じ探索をさせる → メインのセッションが場所を渡す。
