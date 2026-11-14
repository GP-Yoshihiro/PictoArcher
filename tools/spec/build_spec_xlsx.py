#!/usr/bin/env python3
"""PictoArcher の仕様書（docs/PictoArcher_仕様書.xlsx）を作る。

使い方:
    python3 tools/spec/build_spec_xlsx.py            # 作り直す（黄色のセルの書き込みは引き継ぐ）
    python3 tools/spec/build_spec_xlsx.py --fresh    # 書き込みを引き継がずに作り直す

- 内容の正本は docs/superpowers/specs/ の設計書（.md）。仕様を変えるときは、設計書とこのスクリプトを
  同じ PR で直し、このスクリプトで .xlsx を作り直す（.xlsx はバイナリでマージできないため）。
- 黄色のセル（状態、Issue、実績、テストの記録など）は Excel で書き込んでよい。作り直すときに、
  前の .xlsx から同じ行（ID などの鍵が同じ行）の値を引き継ぐ。
- 必要なもの: openpyxl（pip3 install --user openpyxl）
"""
import argparse
import datetime
import math
import os
import sys
import unicodedata

from openpyxl import Workbook, load_workbook
from openpyxl.formatting.rule import CellIsRule, FormulaRule
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "docs", "PictoArcher_仕様書.xlsx")

VERSION = "1.1"
BUILT = "2026-11-14"
PROJECT_START = datetime.date(2026, 10, 26)  # 1週目の月曜（スケジュールの入力セル）

FONT = "Yu Gothic"
C_TITLE = "1F3864"
C_HEAD = "2F5597"
C_SECTION = "D9E2F3"
C_INPUT = "FFF2A8"   # 黄色＝書き込むセル
C_EXAMPLE = "EDEDED"  # 記入例の行
C_DONE = "C6EFCE"
C_DOING = "FFEB9C"
C_BAR = "8EA9DB"
C_GRAY = "595959"

THIN = Side(style="thin", color="BFBFBF")
BORDER = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
WRAP = Alignment(wrap_text=True, vertical="top")
CENTER = Alignment(horizontal="center", vertical="top", wrap_text=True)

STATUS = ["未着手", "作業中", "PR確認中", "済み", "保留"]
OWNERS = ["A", "B", "C", "全員", "キャラ担当", "スキル系UI担当", "デザイナー（補助）"]

SPEC = "docs/superpowers/specs/2026-10-31-pictoarcher-design-v3.md"
WORLD = "docs/superpowers/specs/2026-10-31-worldview-sf-final.md"
ROADMAP = "docs/superpowers/plans/2026-10-31-roadmap.md"
RULES = "docs/development-rules.md"


def font(bold=False, size=10, color="000000", italic=False):
    return Font(name=FONT, bold=bold, size=size, color=color, italic=italic)


def fill(color):
    return PatternFill("solid", start_color=color, end_color=color)


def text_width(s):
    """全角を2、半角を1として数えた表示幅。"""
    return sum(2 if unicodedata.east_asian_width(ch) in "WFA" else 1 for ch in str(s))


def lines_needed(value, width_chars):
    if value is None or width_chars <= 0:
        return 1
    total = 0
    for part in str(value).split("\n"):
        total += max(1, math.ceil(text_width(part) / max(width_chars - 1, 1)))
    return total


class InputTable:
    """黄色のセルの表。作り直すときに、鍵の列が同じ行の値を前の .xlsx から引き継ぐ。"""

    def __init__(self, sheet, header_row, first, last, key_col, input_cols, append=False):
        self.sheet = sheet
        self.header_row = header_row
        self.first = first
        self.last = last
        self.key_col = key_col
        self.input_cols = input_cols
        self.append = append  # True なら鍵を見ずに行の順で引き継ぐ（テストの記録など）


class Book:
    def __init__(self):
        self.wb = Workbook()
        self.wb.remove(self.wb.active)
        self.sheets = []
        self.inputs = []

    def sheet(self, name, title, source, widths, has_inputs=False):
        s = Sheet(self, name, title, source, widths, has_inputs)
        self.sheets.append(s)
        return s


class Sheet:
    def __init__(self, book, name, title, source, widths, has_inputs):
        self.book = book
        self.name = name
        self.ws = book.wb.create_sheet(name)
        self.widths = widths
        self.ncols = len(widths)
        for i, w in enumerate(widths, start=1):
            self.ws.column_dimensions[get_column_letter(i)].width = w
        self.ws.sheet_view.showGridLines = False
        c = self.ws.cell(row=1, column=1, value=title)
        c.font = font(True, 14, C_TITLE)
        self.ws.row_dimensions[1].height = 24
        if source:
            c = self.ws.cell(row=2, column=1, value="もと：" + source)
            c.font = font(size=9, color=C_GRAY)
        self.row = 3
        if has_inputs:
            c = self.ws.cell(row=3, column=1, value="黄色のセルだけ書き込む。ほかのセルは数式か仕様の文。")
            c.font = font(size=9, color=C_GRAY)
            c.fill = fill(C_INPUT)
            self.row = 4
        self.row += 1

    def cell(self, row, col, value, **kw):
        c = self.ws.cell(row=row, column=col, value=value)
        c.font = kw.get("font", font())
        c.alignment = kw.get("align", WRAP)
        if "fill" in kw:
            c.fill = kw["fill"]
        if kw.get("border"):
            c.border = BORDER
        if "fmt" in kw:
            c.number_format = kw["fmt"]
        return c

    def span_width(self, col_from, col_to):
        return sum(self.widths[col_from - 1:col_to])

    def heading(self, text):
        if self.row > 5:
            self.row += 1
        r = self.row
        for col in range(1, self.ncols + 1):
            self.ws.cell(row=r, column=col).fill = fill(C_SECTION)
        self.cell(r, 1, text, font=font(True, 11, C_TITLE))
        self.ws.row_dimensions[r].height = 20
        self.row += 1
        return r

    def para(self, text, bold=False, color="000000"):
        r = self.row
        self.ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=self.ncols)
        self.cell(r, 1, text, font=font(bold, 10, color))
        self.ws.row_dimensions[r].height = 15 * lines_needed(text, self.span_width(1, self.ncols))
        self.row += 1
        return r

    def bullets(self, items):
        for item in items:
            self.para("・" + item)

    def table(self, headers, rows, input_cols=(), center_cols=(), formats=None, row_fills=None,
              key_col=None, append=False, freeze=False):
        """表を書く。input_cols は1始まりの列番号。戻り値は (見出しの行, 最初の行, 最後の行)。"""
        formats = formats or {}
        hr = self.row
        for i, h in enumerate(headers, start=1):
            self.cell(hr, i, h, font=font(True, 10, "FFFFFF"), fill=fill(C_HEAD), border=True, align=CENTER)
        self.ws.row_dimensions[hr].height = 15 * max(lines_needed(h, self.widths[i - 1]) for i, h in enumerate(headers, 1))
        r = hr
        for idx, values in enumerate(rows):
            r += 1
            n_lines = 1
            for i in range(1, len(headers) + 1):
                v = values[i - 1] if i - 1 < len(values) else None
                c = self.cell(r, i, v, border=True, align=CENTER if i in center_cols else WRAP)
                if i in formats:
                    c.number_format = formats[i]
                if i in input_cols:
                    c.fill = fill(C_INPUT)
                elif row_fills and row_fills.get(idx):
                    c.fill = fill(row_fills[idx])
                if not (isinstance(v, str) and v.startswith("=")):
                    n_lines = max(n_lines, lines_needed(v, self.widths[i - 1]))
            self.ws.row_dimensions[r].height = 15 * n_lines + 2
        first, last = hr + 1, r
        if freeze:
            self.ws.freeze_panes = self.ws.cell(row=hr + 1, column=1)
        if input_cols and (key_col or append):
            self.book.inputs.append(InputTable(self.name, hr, first, last, key_col, list(input_cols), append))
        self.row = r + 1
        return hr, first, last


def link(sheet, row, col, target):
    c = sheet.ws.cell(row=row, column=col)
    c.hyperlink = "#'{}'!A1".format(target)
    c.font = font(color="0563C1")
    c.font = Font(name=FONT, size=10, color="0563C1", underline="single")


def add_list_validation(ws, values, ref):
    dv = DataValidation(type="list", formula1='"' + ",".join(values) + '"', allow_blank=True)
    dv.error = "一覧から選んでください"
    dv.prompt = "一覧から選ぶ"
    ws.add_data_validation(dv)
    dv.add(ref)


# ---------------------------------------------------------------- 各シート


def build_cover(b):
    s = b.sheet("表紙", "PictoArcher 仕様書（完全版）", None, [22, 40, 60])
    s.row = 3
    rows = [
        ("版", VERSION, "設計書 第3版・世界観 確定版・ロードマップ 第3版 にもとづく"),
        ("作成日", BUILT, ""),
        ("編集する人", "Cさん", "ほかの人は Issue か PR のコメントで直してほしい所を伝える"),
        ("作り方", "python3 tools/spec/build_spec_xlsx.py", "Excel で直接直さず、スクリプトを直して作り直す（黄色のセルは除く）"),
        ("内容の正本", SPEC, "食い違ったら設計書（.md）が正しい。仕様を変える PR では両方を直す"),
        ("世界観", WORLD, ""),
        ("ロードマップ", ROADMAP, ""),
        ("開発の規則", RULES, "命名、構成、Issue、ブランチ、PR、マージ、Claude の使い方"),
        ("進み具合の正本", "GitHub の Issue と PR", "作業工程シートの「状態」は C さんが毎週 Issue から写す"),
    ]
    s.table(["項目", "内容", "メモ"], rows)

    s.heading("表記の凡例")
    s.table(["表記", "意味", ""], [
        ("【決定】", "チームで決めたこと。Claude の判断では変えない", ""),
        ("【提案】", "Claude が提案し、チームが受け入れたこと。プレイテストで変えてよい", ""),
        ("【仮】", "まだ決まっていないこと（未決定シート）", ""),
        ("黄色のセル", "書き込んでよいセル（状態、Issue 番号、実績、テストの記録、チェック）", ""),
        ("灰色の行", "記入例。消さずに残す", ""),
    ], row_fills={3: C_INPUT, 4: C_EXAMPLE})

    s.heading("シートの一覧（名前をクリックすると開く）")
    hr, first, _ = s.table(["シート", "中身", "設計書"], SHEET_INDEX)
    for i, (name, _, _) in enumerate(SHEET_INDEX):
        link(s, first + i, 1, name)


SHEET_INDEX = [
    ("概要", "目的、前提、コンセプト、ゴール、やらないこと、失敗とみなすこと、制約", "§1〜§3"),
    ("操作と武器", "操作、自動射撃、武器3種", "§4"),
    ("文字ゼロ", "文字ゼロのルール、色の意味、ボタンの記号", "§5"),
    ("進行フロー", "起動からクリア・死亡まで、1ウェーブの流れ、障害物、レベルアップ、保存", "§6"),
    ("ウェーブ構成", "50ウェーブの種類、照明、出てくる敵", "§6・§8、世界観 §2"),
    ("チュートリアル", "ウェーブ0の5段階", "§7"),
    ("敵とボス", "敵10種、中ボス、ボス2種、予兆、姿、コードの名前", "§8、世界観 §3"),
    ("強化", "強化15種・カード26枚、SkillId、上限", "§9、ロードマップ"),
    ("画面表示", "HUD と画面の一覧", "§10・§6"),
    ("画面構成", "画面ごとの配置の図（箱と円の簡単な図）", "§6・§10"),
    ("絵の方針", "ドット絵はキャラだけ、UI の絵、カード、取り込み、フリー素材", "§11、世界観 §6"),
    ("世界観", "フィールド、照明、プレイヤーと武器の見た目", "世界観 §1〜§3"),
    ("技術方針", "Unity の構成、レイヤー、コードの名前", "§12、ロードマップ"),
    ("分担", "6人の担当", "§13、世界観 §4・§5・§7"),
    ("工数_プログラマー", "A・B・C の見積もりと実績、計画1のタスク", "§13、計画1"),
    ("工数_絵", "キャラ担当・スキル系UI担当の工数と対策、フリー素材", "世界観 §4〜§7"),
    ("スケジュール", "8週間の予定と期限（開始日から日付を出す）", "§13"),
    ("作業工程", "全タスクの担当・週・状態・Issue とガントチャート", "ロードマップ、計画1"),
    ("削る順と足す候補", "遅れたときに削る順、手が空いたら足す候補、テストの準備", "§13"),
    ("合格ライン", "毎週のプレイテストの基準と記録", "§15"),
    ("未決定と任せること", "まだ決まっていないこと、作る人に任せること", "§16・§18、世界観 §8・§9"),
    ("開発ルール", "命名、構成、Issue、ブランチ、PR、マージの要約", "development-rules.md"),
    ("Claude利用チェックリスト", "重複作業を防ぐための確認項目", "development-rules.md、CLAUDE.md"),
    ("変更履歴", "この仕様書の版", ""),
]


def build_overview(b):
    s = b.sheet("概要", "概要（目的・ゴール・制約）", SPEC + " §1〜§3", [26, 70, 22])
    s.heading("§1 目的と理由")
    s.table(["項目", "内容", "出所"], [
        ("作るもの", "アーチャー伝説（Archero）型のローグライトアクションを、文字を一切使わずに作る", "【決定】"),
        ("前提1", "文字ゼロ（数字・記号・ピクトグラムは使ってよい）", "【決定】"),
        ("前提2", "初見で分かる（説明なしで遊び始められる）", "【決定】"),
        ("前提3", "動かして面白い（触った瞬間に気持ちいい）", "【決定】"),
        ("見た目と仕組み", "見た目はヴァンサバのような2Dドット絵、仕組みはアーチャー伝説。敵の数は少なめにし、1体ずつの動きと予兆を見て避ける遊びにする", "【決定】"),
        ("絵", "ドット絵にするのはキャラクター（プレイヤー、敵、中ボス、ボス）だけ。カード、アイコン、ボタンなどの UI は普通の2Dの絵で作る", "【決定】"),
        ("世界観", "SF（ロボット系）。主人公はパワースーツを着た兵士", "【決定】"),
        ("コンセプト", "撃ちながら避け続ける。強くなるほど弾が目に見えて増える。強化の結果が必ず画面上の見た目の変化として現れる", "【提案】"),
    ])
    s.heading("§2 ゴール")
    s.bullets([
        "1つのステージ（50ウェーブ）を、説明なしで最初から最後まで遊び切れる【決定】",
        "初見の人が合格ライン（合格ラインシート）を満たす",
    ])
    s.heading("§2 やらないこと（8週間の範囲外）")
    s.bullets([
        "2つ目のステージ【決定】",
        "スマホ・タブレットへの対応（のちに iPad・iPhone へ対応させるので、画面は 9:16 で作っておく）",
        "オンライン要素、課金や広告、装備、図鑑、デイリー要素",
        "敵の大群（画面を埋める数十体〜百体の敵）【決定】",
        "天使（ボスのあとに出る味方）【決定】",
        "プレイ中の武器の切り替え（武器は最初に1つ選ぶ）【決定】",
        "プレイ途中の保存（1回のプレイは通しで遊ぶ）【提案】",
        "🏹強化（ホームでの永続強化）と ⚙ 設定画面。手が空いたら足す候補にする【提案】",
    ])
    s.heading("§2 失敗とみなすこと（動いていても失敗）")
    s.bullets([
        "画面のどこかに文字が1つでも出る（ロゴ、素材、エラー表示。ウィンドウタイトルの扱いは文字ゼロシート）",
        "初見の人が「何をすればいいの？」と口にする",
        "強化を選んでも何が変わったか分からない",
        "50ウェーブの途中で「同じことの繰り返し」に感じて飽きる【提案】",
    ])
    s.heading("§3 制約")
    s.table(["項目", "内容", "出所"], [
        ("人数", "プログラマー3人、スキル系UI担当1人、キャラ担当1人、デザイナー（補助）1人", "【決定】"),
        ("期間", "8週間", "【決定】"),
        ("作業日", "全員、週3日くらい（授業などがあるため）。1人が8週間に使える作業日は 24人日（週3日×8週）", "【決定】／【提案】24人日"),
        ("新しい絵の期限", "新しい絵を入れられるのは7週目の終わりまで。絵の担当が新しい絵に使えるのは 21人日 まで。8週目の3日は直しと統一だけ", "【提案】"),
        ("Claude の利用", "プログラマーは全員 Claude を使って作る", "【決定】"),
        ("エンジン", "Unity 6（6000.5.10f1、全員このバージョンに固定）", "【決定】Unity／【提案】版"),
        ("対象", "PC。画面は 9:16 の縦長（1080×1920 基準）。ウィンドウは 540×960 が初期値。全画面では整数倍で拡大し、左右は黒い帯。のちに iPad・iPhone へ対応させるため 9:16 で作る（UI は画面の端を基準に置き、端に余白をとる）", "【決定】PC・9:16・のちに iPad と iPhone／【提案】解像度"),
        ("アート", "キャラ担当はプレイヤーと敵だけを描く。背景とエフェクトはフリー素材", "【決定】"),
        ("管理", "git＋GitHub（非公開リポジトリ）", "【決定】"),
    ])


def build_controls(b):
    s = b.sheet("操作と武器", "操作と武器", SPEC + " §4", [18, 50, 26, 22])
    s.heading("操作")
    s.table(["操作", "内容", "出所", ""], [
        ("移動", "WASD／矢印キー、またはマウス左ボタンを押したままドラッグ（押した位置が仮想スティックの中心）", "【提案】", ""),
        ("射撃", "常に自動射撃。移動中も撃てる。一番近い敵を自動で狙う", "【決定】移動中も可／【提案】自動照準", ""),
        ("武器を選ぶ", "プレイを始める前に、3種から1つ選ぶ。プレイ中は変えない", "【決定】", ""),
        ("カード選択", "クリック、または 1 2 3 キー", "【提案】", ""),
        ("一時停止", "Esc で ⏸", "【提案】", ""),
    ])
    s.bullets([
        "移動中の連射速度は調整値（初期値は止まっているときと同じ 100%）【提案】",
        "敵がいないときは撃たない【提案】",
        "被弾したら短い無敵時間（点滅）【提案】",
    ])
    s.heading("武器（3種）【決定】種類・選び方／【提案】撃ち方")
    s.table(["武器", "撃ち方", "得意なこと", "連射（目安）"], [
        ("銃（WeaponId.Shot）", "光弾を1発ずつまっすぐ撃つ", "どんな場面でも使いやすい", "速い（基準）"),
        ("レーザー（WeaponId.Beam）", "一瞬だけ直線の光線を出す。当たった敵をすべて貫く", "一列に並んだ敵", "遅め"),
        ("ランチャー（WeaponId.Blast）", "小型の弾を撃ち、当たると爆発して周りの敵にも当たる（範囲攻撃）", "固まった敵", "遅い"),
    ])
    s.bullets([
        "武器を選ぶ画面は、ホームで ▶ を押したあとに出す。3つの武器の絵を並べ、選ぶとプレイが始まる。前回選んだ武器に印を付ける【提案】",
        "初回はチュートリアルを銃で遊ぶ。武器を選ぶ画面は2回目のプレイから出す【提案】",
        "強化は3種すべての武器に効く。武器ごとの細かい見え方（例：レーザーの ⟲ は壁で光線が折れる）は A さんが決める",
        "レーザーを選んだときは「ピアッシング」をカードの候補から外す【提案】",
        "数値（攻撃力、連射、射程、爆発の広さ）は ScriptableObject の調整値にする",
    ])


def build_no_text(b):
    s = b.sheet("文字ゼロ", "文字ゼロのルール（全員必読）", SPEC + " §5、CLAUDE.md", [22, 74, 14])
    s.para("前の版から変えない。【決定】 Claude にも同じルールを守らせる（CLAUDE.md に同じ表がある）。", bold=True)
    s.heading("ルール")
    s.table(["項目", "ルール", "出所"], [
        ("禁止", "アルファベット・ひらがな・カタカナ・漢字すべて。LV HP BOSS x2 s（秒）などの略語・単位も禁止", "【決定】"),
        ("代わりの表記", "レベル ★5／倍率 ×2／時間 ⏱ 30／割合 +20%／進行 23/50", "【決定】"),
        ("色の意味", "赤＝危険・攻撃／緑＝回復／黄＝コイン・経験値／青＝防御・氷／紫＝レア。この5色は UI・拾うもの・エフェクトの意味だけに使い、敵の体の主な色やフィールドの照明には使わない（敵は灰・白・黒・茶・桃などから選ぶ。プレイヤーの弾は白）", "【提案】"),
        ("予兆", "敵の攻撃は必ず先に赤い線・赤い円で予告する", "【決定】"),
        ("操作の案内", "手・矢印キー・マウスの絵のアニメで見せる。キーの絵に英字（W A S D、Esc など）を描かない", "【決定】"),
        ("絵文字", "フォントで出さない。すべてスプライト画像（UI の絵）で用意する", "【決定】"),
        ("フォント", "TextMeshPro のフォントアセットには数字と記号だけを登録する。UI の絵に合う、読みやすいフォントを使う（ピクセルフォントでなくてよい）", "【提案】"),
        ("ボタン", "▶ 開始／⏸ 一時停止／↻ やり直し／🏠 ホーム／⚙ 設定／🔒 ロック中／🔊 音量", "【決定】"),
        ("素材", "取り込む前に文字が入っていないか確認する（UIパックの「PLAY」、看板、ロゴ、背景の張り紙や画面の表示など）", "【決定】"),
        ("ウィンドウ・起動画面", "ウィンドウタイトルとアプリ名は OS が表示するもので画面内ではないため対象外。Unity のスプラッシュ画面は、出せない場合の扱いをチームで判断する", "【提案】"),
        ("開発ビルド", "開発ビルドやデバッグ表示でも画面に文字を出さない。ログはコンソールだけ", "【提案】"),
    ])
    s.heading("色の意味（見本）")
    _, first, _ = s.table(["色", "意味", "見本"], [
        ("赤", "危険・攻撃（予兆の線と円、ダメージ）", ""),
        ("緑", "回復", ""),
        ("黄", "コイン・経験値", ""),
        ("青", "防御・氷", ""),
        ("紫", "レア", ""),
    ])
    for i, color in enumerate(["E53935", "43A047", "FDD835", "1E88E5", "8E24AA"]):
        s.ws.cell(row=first + i, column=3).fill = fill(color)
    s.heading("属性の色【提案】（毒は【仮】）")
    s.bullets([
        "炎は赤、氷は青（5色の意味と合う）",
        "雷は黄色だと「コイン・経験値」と見分けにくいので、白い稲妻にする",
        "毒の色はまだ決めない（未決定シート）。紫はレア、緑は回復と重なるので注意する",
    ])
    s.heading("画面で使う記号")
    s.table(["記号", "意味", "使う所"], [
        ("▶", "開始・再開", "ホーム、一時停止"),
        ("⏸", "一時停止", "HUD の右上"),
        ("↻", "やり直し（同じ武器で）", "クリア、死亡、一時停止"),
        ("🏠", "ホーム", "クリア、死亡、一時停止"),
        ("🔊", "音量", "一時停止"),
        ("⚙", "設定", "足す候補（今は作らない）"),
        ("🔒", "ロック中", "必要になったら"),
        ("🏆", "クリア", "クリア画面"),
        ("💀", "死亡／ボス", "死亡画面、ボスの HP バー"),
        ("⚠", "中ボスが来る", "中ボスのウェーブの前"),
        ("⏱", "時間", "ホームの最短タイム、クリアタイム、時間制限型の残り秒数"),
        ("★", "レベル・重ね取りの段階", "経験値バー、カード"),
        ("×", "倍率", "必要なとき"),
        ("♥", "HP", "強化のアイコン、全部上限のときのカード"),
        ("! （赤い円の中）", "敵が出る場所", "ウェーブの始め、雑魚を呼ぶ攻撃"),
    ], center_cols=(1,))


def build_flow(b):
    s = b.sheet("進行フロー", "ゲーム進行フロー", SPEC + " §6", [22, 70, 18])
    s.heading("画面の流れ")
    s.table(["画面", "出すもの", "次へ"], [
        ("起動", "初回はタイトルを出さずにチュートリアル（ウェーブ0、銃）へ。2回目以降はホーム", "チュートリアル／ホーム"),
        ("ホーム", "▶ と、これまでの最高記録 37/50（クリア済みなら ⏱ 最短タイム）", "▶ → 武器を選ぶ"),
        ("武器を選ぶ", "銃／レーザー／ランチャーの絵。前回の武器に印", "選ぶ → プレイ"),
        ("プレイ", "1つのフィールドで 50ウェーブ。障害物の並びはウェーブごとに変わる", "クリア／死亡"),
        ("クリア", "🏆 ＋ ⏱ クリアタイム ＋ 獲得コイン", "↻／🏠"),
        ("死亡", "💀 → 到達ウェーブ 23/50 と、選んだ武器と取得した強化のアイコン", "↻／🏠"),
    ])
    s.bullets([
        "仕様メモの「総ステージ数 50」は、この 50ウェーブのこと【決定】",
        "フィールドは1画面に収まる広さで、カメラは動かない【決定】",
        "今のウェーブの敵を全部倒すと、同じフィールドに次のウェーブの敵が出てくる【決定】",
        "↻ やり直しは、同じ武器でもう一度始める。武器を変えたいときは 🏠 から【提案】",
        "死亡するとそのプレイは終わり、強化はリセット【提案】",
        "1回のプレイ（50ウェーブ）は 20〜30分が目安（1ウェーブ平均 30秒前後）【提案】",
    ])
    s.heading("保存【提案】")
    s.table(["保存するもの", "内容", "形式"], [
        ("最高到達ウェーブ", "ホームに 37/50 の形で出す", "JSON ファイル"),
        ("最短クリアタイム", "クリア済みならホームに ⏱ で出す", "JSON ファイル"),
        ("コイン", "獲得したコインの合計", "JSON ファイル"),
        ("前回選んだ武器", "武器を選ぶ画面で印を付ける", "JSON ファイル"),
    ])
    s.heading("1ウェーブの流れ【提案】")
    s.table(["順", "内容", ""], [
        ("1", "敵が出る位置に赤い ! 円 → 敵が出現", ""),
        ("2", "戦闘", ""),
        ("3", "全滅 → 経験値とコインが自分に吸い寄せられる", ""),
        ("4", "約1.5秒の休み。この間に障害物の並びを次のパターンに変え、画面上部の 23/50 が 24/50 に変わる → 1 に戻る", ""),
    ], center_cols=(1,))
    s.heading("障害物のパターン【決定】数パターン・ウェーブごとに切り替え／【提案】中身")
    s.bullets([
        "障害物の並びを 6パターン 用意する（数は調整してよい）。どのウェーブでどのパターンを使うかは WaveDefinition（ScriptableObject）に書く",
        "切り替えは休みの間に行う。今の障害物が消え、新しい障害物の位置に薄い影を約0.5秒出してから現れる",
        "新しい障害物がプレイヤーと重なる場合は、プレイヤーが離れるまでその障害物だけ出さない（閉じ込めない）",
        "ボスと中ボスのウェーブは、障害物の少ないパターンを使う",
        "障害物の絵はフリー素材（コンテナ、柱、機械の箱）。どのパターンも、通れない袋小路を作らない",
    ])
    s.heading("レベルアップ【決定】自己強化系／【提案】流れ")
    s.bullets([
        "経験値バーが満タン → 時間停止 → カード3枚 → 1枚選ぶと再開。ウェーブの途中でも発生する",
        "50ウェーブで 20回前後のレベルアップが目安（調整値）",
        "全部の強化が上限に達したら、カードは ♥回復 と コイン に置き換える",
    ])
    s.heading("一時停止メニュー【提案】")
    s.para("▶ 再開／↻ やり直し／🏠 ホーム／🔊 音量")


WAVE_NEW_ENEMIES = {
    1: ("単発攻撃型、接触攻撃型", 2),
    6: ("三発扇型、自爆型", 2),
    11: ("単発範囲型、ハンマー回転型", 2),
    16: ("五発連射型、地面燃焼型", 2),
    21: ("落雷型", 1),
    26: ("時間制限型", 1),
}
LIGHTING = [
    (10, "通常の白い照明"),
    (20, "薄暗い照明"),
    (30, "暗い紺色の照明"),
    (40, "照明がゆっくり明滅する"),
    (50, "外の惑星の淡い光"),
]


def wave_kind(n):
    if n == 50:
        return "最後のボス"
    if n % 10 == 0:
        return "ボスA"
    if n % 10 == 5:
        return "中ボス"
    return "通常"


def wave_note(n, kind):
    if kind == "中ボス":
        k = (n - 5) // 10 + 1
        base = "出る前に ⚠ が点滅。HPが減ると分裂する"
        return base if k == 1 else "{}（{}回目：HP・速さ・分裂の数を上げる）".format(base, k)
    if kind == "ボスA":
        return {
            10: "💀付きHPバー。突進（赤い線）／雑魚を呼ぶ（赤い ! 円）",
            20: "10 の攻撃に、範囲攻撃（赤い円）を足す",
            30: "HP と速さを上げ、攻撃の間隔を短くする",
            40: "HP と速さを上げ、攻撃の間隔を短くする",
        }[n]
    if kind == "最後のボス":
        return "突進／弾幕（光ってから）／雑魚を呼ぶ。HPが半分を切ると色と光り方が変わり、間隔が短くなる"
    if n >= 31:
        return "組み合わせと強化版（少し大きく、色を濃く）で変化を付ける"
    return ""


def build_waves(b):
    s = b.sheet("ウェーブ構成", "50ウェーブの構成", SPEC + " §6・§8、" + WORLD + " §2", [9, 12, 20, 24, 10, 10, 50, 18])
    s.para("【決定】ボス2種・中ボス1種・天使なし／【提案】出る場所。ウェーブ0はチュートリアル（チュートリアルシート）。敵の数と組み合わせは調整値（作る人に任せる）。")
    s.heading("種類の数（数式）")
    sum_row = s.row
    s.table(["種類", "数", "", "", "", "", "", ""], [
        ("通常", None), ("中ボス", None), ("ボスA", None), ("最後のボス", None), ("合計", None),
    ])
    s.heading("ウェーブごと")
    rows = []
    for n in range(1, 51):
        kind = wave_kind(n)
        light = next(t for top, t in LIGHTING if n <= top)
        new, count = WAVE_NEW_ENEMIES.get(n, ("", 0))
        obstacle = "少ないパターン" if kind != "通常" else "6パターンから選ぶ"
        rows.append([n, kind, light, new, count, None, wave_note(n, kind), obstacle])
    hr, first, last = s.table(
        ["ウェーブ", "種類", "照明", "この波から出る敵", "新しい種類", "そろった種類", "中身", "障害物"],
        rows, center_cols=(1, 2, 5, 6), freeze=True)
    for r in range(first, last + 1):
        s.ws.cell(row=r, column=6, value="=SUM($E${}:E{})".format(first, r))
        s.ws.cell(row=r, column=6).font = font()
    s.ws.auto_filter.ref = "A{}:H{}".format(hr, last)
    # 種類の数
    for i, kind in enumerate(["通常", "中ボス", "ボスA", "最後のボス"]):
        s.ws.cell(row=sum_row + 1 + i, column=2, value='=COUNTIF($B${}:$B${},A{})'.format(first, last, sum_row + 1 + i))
    s.ws.cell(row=sum_row + 5, column=2, value="=SUM(B{}:B{})".format(sum_row + 1, sum_row + 4))
    boss_fill = {"中ボス": "E2EFDA", "ボスA": "FCE4D6", "最後のボス": "F8CBAD"}
    for r in range(first, last + 1):
        kind = s.ws.cell(row=r, column=2).value
        if kind in boss_fill:
            for col in range(1, 9):
                s.ws.cell(row=r, column=col).fill = fill(boss_fill[kind])


def build_tutorial(b):
    s = b.sheet("チュートリアル", "チュートリアル（ウェーブ0）【提案】", SPEC + " §7", [8, 60, 40])
    s.para("武器は銃で遊ぶ。案内の絵はプレイヤーが一度その操作をしたら消える。2回目以降の起動では出さない。")
    s.table(["段階", "起きること", "覚えること"], [
        ("1", "敵1体だけ。矢印キーとマウスの絵がアニメで動きを見せる → プレイヤーが動く", "動かし方"),
        ("2", "近づくと自動で弾が飛ぶ", "撃つのは自動"),
        ("3", "1体目を倒すと必ずレベルアップ", "カード選択を一度体験する"),
        ("4", "2体目は弾を撃つ敵。赤い予兆線を見せる", "赤い線を見て避ける"),
        ("5", "倒すと 1/50 が出て、そのままウェーブ1が始まる", "ウェーブの表示"),
    ], center_cols=(1,))
    s.heading("関係する決まり")
    s.bullets([
        "操作案内の絵は、3週目のテストに間に合うよう最小版を先に入れる（スキル系UI担当、2週目）",
        "キーの絵に英字を描かない（文字ゼロ）",
        "チュートリアルとカードの実演アニメと予兆は、遅れても削らない（削る順シート）",
    ])


def build_enemies(b):
    s = b.sheet("敵とボス", "敵とボス", SPEC + " §8、" + WORLD + " §3、" + ROADMAP,
                [7, 13, 30, 24, 8, 24, 24, 15, 12])
    s.para("敵は「役割」で決め、見た目は世界観で決める。数値（HP・攻撃力・速度・出現数・時間制限の秒数）はすべて ScriptableObject の調整値。担当は B さん。")
    s.heading("通常の敵（7つの見た目で10種）【決定】種類／【提案】予兆・登場")
    s.table(["モデル", "種類", "動き", "予兆", "登場", "姿（世界観）", "同じモデルの見分け方", "コードの名前", "作る時期"], [
        ("1", "単発攻撃型", "止まって狙い、1発まっすぐ撃つ", "赤い線", "1〜", "一つ目の警備ロボ", "細い銃身", "Shooter", "計画2（3週）"),
        ("1", "単発範囲型", "1発撃ち、着いた所で爆発する", "着く場所に赤い円", "11〜", "一つ目の警備ロボ", "太い砲身", "Lobber", "計画4（5週）"),
        ("2", "三発扇型", "扇形に3発を同時に撃つ", "赤い線3本", "6〜", "腕に砲が並んだ多連装ロボ", "砲が扇に開く", "SpreadShooter", "計画2（3週）"),
        ("2", "五発連射型", "同じ向きに5発を続けて撃つ", "太い赤い線", "16〜", "腕に砲が並んだ多連装ロボ", "砲が縦に並ぶ", "BurstShooter", "計画4（5週）"),
        ("3", "接触攻撃型", "プレイヤーを追ってくる、体当たり", "なし（遅い）", "1〜", "クモ型の小さな機械", "—", "Chaser", "計画1（作成中）"),
        ("3", "自爆型", "突進して自爆する", "赤い円が広がる", "6〜", "クモ型の小さな機械", "背中のコアが点滅（プログラム）", "Bomber", "計画2（3週）"),
        ("4", "地面燃焼型", "床に炎を残す。炎の上にいるとダメージ。炎は一定時間で消える", "燃える場所に薄い赤い円", "16〜", "火炎放射ロボ", "—", "Burner", "計画4（5週）"),
        ("5", "落雷型", "プレイヤーのいる場所に雷を落とす", "赤い円が縮んでから落ちる", "21〜", "浮かんでいる送電ロボ", "—", "Striker", "計画4（6週）"),
        ("6", "ハンマー回転型", "ハンマーを振り回しながら近づく", "回る範囲に赤い円", "11〜", "ハンマーアームの作業ロボ", "—", "Spinner", "計画4（5週）"),
        ("7", "時間制限型", "出てから一定の時間がたつと、プレイヤーを即死させる攻撃を出す【仮】", "頭の上に ⏱ と残り秒数。最後の3秒は画面の縁が赤く点滅", "26〜", "時限装置を抱えたロボ", "—", "Countdown", "計画4（6週）"),
    ], center_cols=(1, 5))
    s.bullets([
        "同じモデルの2種は、見た目のちょっとした違い（武器の部分など）で見分ける。描く量を減らす場合は、大きさと予兆の違いだけで見分ける（工数_絵の対策1）",
        "時間制限型は 26〜 にし、残り秒数を必ず見せる。即死の代わりに大きなダメージにするかはプレイテストで決める（調整値）",
        "強化版の敵は、見た目を少し大きくし、色を濃くする（5色の意味の色にはしない）。色はプログラマーが出す",
    ])
    s.heading("中ボスとボス")
    s.table(["役割", "出るウェーブ", "攻撃と変化", "予兆", "", "姿（世界観）", "見た目の変化", "コードの名前", "作る時期"], [
        ("中ボス（1種）", "5・15・25・35・45", "HPが減ると分裂する。出るたびに HP と速さを上げ、分裂の数を増やす", "出る前に ⚠ が点滅", "", "金属の粒が集まった塊", "大きな塊が小さな塊に割れる（小さい塊は縮小して使う）", "Splitter", "計画3（4週）"),
        ("ボスA", "10・20・30・40", "10：突進／雑魚を呼ぶ。20 から範囲攻撃を足す。30・40 は HP と速さを上げ、攻撃の間隔を短くする", "突進は赤い線、呼ぶは赤い ! 円、範囲は赤い円", "", "大型の二足メカ", "胸のハッチが開いてクモ型を出す。範囲攻撃は「ため」の動きを使い回す（新しいコマを描かない）", "BossA", "計画3（4週）、強化版は計画4（5週）"),
        ("最後のボス", "50", "突進／弾幕（光ってから）／雑魚を呼ぶ。HPが半分を切ると色と光り方が変わり、間隔が短くなる", "光ってから撃つ、赤い ! 円", "", "ステーションの中枢機械（巨大な一つ目）", "変化はプログラマーが色と光り方で出す（変化のコマは描かない）", "FinalBoss", "計画4（6週、仮の図形）、絵は7週、調整は8週"),
    ])


SKILLS = [
    # (#, 強化, SkillId, アイコン, 見え方, 上限, 削る順, 先に作る目安)
    ("1", "HP回復", "Heal", "♥＋", "緑に光り、HPバーが戻る", "何度でも（満タンのときは候補から外す）", "", "○"),
    ("2", "最大HPアップ＋回復", "MaxHpUp", "♥↑", "HPバーが伸びる", "5", "", "○"),
    ("3", "攻撃力 +%", "AttackUp", "⚔↑", "弾が大きく光る", "5", "", "○"),
    ("4", "移動速度 +%", "MoveSpeedUp", "足＋↑", "走りが速くなり、残像が出る", "3", "", ""),
    ("5", "攻撃速度 +%", "AttackSpeedUp", "弾＋»", "撃つ間隔が短くなる", "5", "", "○"),
    ("6", "弾が増える：フロント", "FrontShot", "自機＋前の矢印", "前に並んで飛ぶ", "3", "", "○"),
    ("6", "弾が増える：ダイアゴナル", "DiagonalShot", "自機＋斜めの矢印", "斜め前にも飛ぶ", "2", "", "○"),
    ("6", "弾が増える：サイド", "SideShot", "自機＋左右の矢印", "左右にも飛ぶ", "1", "", "○"),
    ("6", "弾が増える：リア", "RearShot", "自機＋後ろの矢印", "後ろにも飛ぶ", "1", "", "○"),
    ("6", "弾が増える：マルチショット", "MultiShot", "自機＋続く矢印", "同じ向きに続けて撃つ", "2", "4", "○"),
    ("7", "弾の性質：ウォールバウンド", "WallBounce", "⟲", "壁で跳ね返る", "2", "", "○"),
    ("7", "弾の性質：エネミーバウンド", "EnemyBounce", "弾＋敵＋折れ線", "敵に当たると近くの別の敵へ跳ねる", "2", "4", "○"),
    ("7", "弾の性質：ピアッシング", "Pierce", "⤳", "敵をすり抜ける（レーザーでは候補から外す）", "1", "", "○"),
    ("8", "属性：炎", "Fire", "🔥", "燃えて継続ダメージの数字", "3", "", ""),
    ("8", "属性：氷", "Ice", "❄", "青くなり遅くなる", "3", "", ""),
    ("8", "属性：雷", "Lightning", "⚡", "白い稲妻が近くの敵へ連鎖する", "3", "", ""),
    ("8", "属性：毒【仮】", "Poison", "毒【仮】", "継続ダメージが重なる（色は未決定）", "3", "", ""),
    ("9", "攻撃範囲 +%", "AreaUp", "◯↑", "銃は弾が大きく、レーザーは光線が太く、ランチャーは爆発が広くなる", "3", "", ""),
    ("10", "自機縮小", "Shrink", "自機＋↓", "自機が小さくなり、当たり判定も小さくなる", "2", "", ""),
    ("11", "死亡時の爆発：縦", "DeathBlastVertical", "💥＋│", "倒した敵の位置から縦に爆発が広がる", "2", "4", ""),
    ("11", "死亡時の爆発：横", "DeathBlastHorizontal", "💥＋─", "倒した敵の位置から横に爆発が広がる", "2", "4", ""),
    ("11", "死亡時の爆発：円", "DeathBlastRing", "💥＋◯", "倒した敵の位置から円の形に爆発が広がる", "2", "", ""),
    ("12", "死亡時の吸い寄せ", "DeathPull", "渦", "倒した敵の位置に、周りの敵が吸い寄せられる", "2", "", ""),
    ("13", "経験値の取得率 +%", "ExpGainUp", "黄色の粒＋↑", "拾う経験値が増え、経験値バーが速く伸びる", "3", "", ""),
    ("14", "ファンネル", "Orbiter", "小型機", "自機の周りを小型機が回り、敵を撃つ", "3", "", ""),
    ("15", "ネタ系【仮】", "Wildcard", "【仮】", "【仮】（未決定シート）", "【仮】", "1", ""),
]


def build_skills(b):
    s = b.sheet("強化", "強化（15種、カード26枚）", SPEC + " §9、" + ROADMAP + "（SkillId）",
                [6, 26, 20, 16, 40, 18, 8, 10])
    s.para("【決定】種類／【提案】アイコン・見え方・重ね取り。レベルアップで選ぶのは自分を強くする強化だけ。カードには大きいアイコン＋数字＋約1秒の実演アニメを入れる。")
    s.heading("数（数式）")
    count_row = s.row
    s.table(["", "項目", "数", "", "", "", "", ""], [
        ("", "カード（SkillId）の数", None),
        ("", "先に作る目安（計画2）の数", None),
        ("", "削る順に入っているカード", None),
    ])
    s.heading("カード一覧")
    hr, first, last = s.table(
        ["#", "強化", "SkillId", "アイコンの絵", "画面での見え方", "上限（重ね取り）", "削る順", "先に作る目安"],
        [list(x) for x in SKILLS], center_cols=(1, 4, 6, 7, 8), freeze=True)
    s.ws.cell(row=count_row + 1, column=3, value="=COUNTA(C{}:C{})".format(first, last))
    s.ws.cell(row=count_row + 2, column=3, value='=COUNTIF(H{}:H{},"○")'.format(first, last))
    s.ws.cell(row=count_row + 3, column=3, value="=COUNTA(G{}:G{})".format(first, last))
    s.heading("決まり")
    s.bullets([
        "重ね取りの段階はカードに ★★ のように表示する。上限に達した強化はカード候補から外す",
        "カード3枚は重複なしでランダム。重み付けは調整値",
        "カードは縦長の UI の絵（ドット絵ではない）。上に強化のアイコン、真ん中に ★ の段階、下に実演アニメを置く【決定】縦長／【提案】中の並べ方",
        "6 と 11 のアイコンは、同じ本体の絵に「向きの印」を重ねて作り分ける",
        "実演アニメは、ゲームの実際のスプライトを使ってプログラマー（C さん）が作る。UI担当はカードの枠と並べ方を作る",
        "「先に作る目安」：見た目が分かりやすく、ほかの強化の土台になるもの。計画2と計画4のどちらに入れるかは A さんが計画2を書くときに決める（ロードマップ）",
        "「削る順」の数字は削る順シートの番号",
    ])


def build_screens(b):
    s = b.sheet("画面表示", "画面表示（HUD と画面の一覧）", SPEC + " §10・§6", [26, 60, 22])
    s.heading("HUD【提案】（縦長の画面に合わせて、表示は上の帯にまとめる）")
    s.table(["場所", "表示", "担当"], [
        ("プレイヤーの頭上", "HPバー＋数字", "C"),
        ("画面上部の1段目", "ウェーブ 23/50、⏸（右端）。ボス戦ではボスのHPバー（💀付き）", "C"),
        ("画面上部の2段目", "経験値バー＋★n、コイン数", "C"),
        ("画面上部の3段目（フィールドに重ねる）", "選んだ武器のアイコンと、取得した強化のアイコン一覧（UI の絵、半透明の帯の上）。1段に収まらないときは2段に折り返す", "C"),
        ("戦闘中", "ダメージ数字のポップアップ（C）、ヒットストップ・画面の揺れ（A）", "C・A"),
    ])
    s.heading("画面の一覧")
    s.table(["画面", "並べるもの", "担当"], [
        ("ホーム", "▶、最高記録 37/50、⏱ 最短タイム", "C"),
        ("武器を選ぶ", "武器3種の絵（2倍表示）、前回の武器の印", "C"),
        ("カード選択", "カード3枚（アイコン2倍、★の段階、実演アニメ）。クリックか 1 2 3", "C"),
        ("一時停止", "▶ ↻ 🏠 🔊", "C"),
        ("クリア", "🏆（後ろに一枚絵。届くまでは 🏆 だけ）、⏱、コイン、↻ 🏠", "C"),
        ("死亡", "💀、23/50、武器と強化のアイコン、↻ 🏠", "C"),
    ])
    s.para("大まかな配置は「画面構成」シートの図を見る。仕上げのレイアウト（縦長）の絵はスキル系UI担当が4週目までに作る。開発ビルドやデバッグ表示でも文字を出さない。")


def build_layouts(b):
    """画面ごとの配置の目安。縦長 270×480 を 1マス＝10px の箱と円で描く。"""
    W, H, GAP = 27, 48, 2
    s = b.sheet("画面構成", "画面構成（配置の目安・縦長 270×480）", SPEC + " §6・§10", [1.0] * (4 * W + 3 * GAP))
    ws = s.ws
    BG, HUD, DIM, PANEL = "3A3F47", "1E2126", "2A2D33", "2B2F36"
    WALL, OBST, BTN, ICON, CARD, PICTURE = "6B6F76", "8A8F98", "D6D6D6", "BDBDBD", "EEEEEE", "4A5059"
    WHITE, GREEN, YELLOW, RED, RED_AREA = "FFFFFF", "43A047", "FDD835", "E53935", "E57373"
    ENEMY_GRAY, ENEMY_BROWN, ENEMY_PINK, XP_EMPTY = "9E9E9E", "8D6E63", "F48FB1", "555A62"
    MIDDLE = Alignment(horizontal="center", vertical="center", wrap_text=True)
    LINE = Side(style="thin", color=HUD)
    FRAME = Side(style="medium", color="000000")

    s.heading("この図の見方")
    s.bullets([
        "縦長の画面（270×480。P2-00 で切り替えたあと）の配置の目安。1マス＝10px。キャラの絵（32×32）はおよそ3×3マス。カード、アイコン、ボタンはドット絵ではない普通の2Dの絵。",
        "箱と円は場所と大きさの目安で、絵ではない。仕上げのレイアウトの絵はスキル系UI担当が4週目までに作る。",
        "画面の中に出すのは記号と数字だけ。①②③や図の下の説明は仕様書のためのもので、ゲームには出さない。",
        "色は「色の目安」を見る。青と紫は意味のある物だけに使うので、この図では使っていない。",
    ])

    class Mock:
        def __init__(self, slot, top, name):
            self.r0, self.c0 = top, 1 + slot * (W + GAP)
            ws.merge_cells(start_row=top - 1, start_column=self.c0, end_row=top - 1, end_column=self.c0 + W - 1)
            c = ws.cell(row=top - 1, column=self.c0, value=name)
            c.font = font(True, 10, C_TITLE)

        def at(self, x, y):
            return ws.cell(row=self.r0 + y, column=self.c0 + x)

        def screen(self):
            for y in range(H):
                for x in range(W):
                    c = self.at(x, y)
                    c.fill = fill(BG)
                    c.border = Border(left=FRAME if x == 0 else None, right=FRAME if x == W - 1 else None,
                                      top=FRAME if y == 0 else None, bottom=FRAME if y == H - 1 else None)

        def paint(self, x, y, w, h, color):
            for yy in range(y, y + h):
                for xx in range(x, x + w):
                    self.at(xx, yy).fill = fill(color)

        def circle(self, cx, cy, r, color, label=None, label_color="000000"):
            for y in range(int(cy - r) - 1, int(cy + r) + 2):
                for x in range(int(cx - r) - 1, int(cx + r) + 2):
                    if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                        self.at(x, y).fill = fill(color)
            if label:
                self.text(cx, cy, label, label_color)

        def text(self, x, y, label, color, size=8):
            c = self.at(x, y)
            c.value = label
            c.font = font(True, size, color)
            c.alignment = MIDDLE

        def box(self, x, y, w, h, color, label="", size=9, label_color="000000", border=True):
            c = self.at(x, y)
            c.value = label or None
            c.fill = fill(color)
            c.font = font(True, size, label_color)
            c.alignment = MIDDLE
            if border:
                c.border = Border(left=LINE, right=LINE, top=LINE, bottom=LINE)
            if w > 1 or h > 1:
                ws.merge_cells(start_row=self.r0 + y, start_column=self.c0 + x,
                               end_row=self.r0 + y + h - 1, end_column=self.c0 + x + w - 1)

    def home(m):
        m.box(4, 6, 19, 13, PICTURE, "①", 14, WHITE)
        m.box(6, 22, 15, 3, PANEL, "37/50", 11, WHITE, False)
        m.box(6, 26, 15, 3, PANEL, "⏱ 12:34", 11, WHITE, False)
        m.box(8, 33, 11, 6, BTN, "▶", 20)

    def weapon(m):
        m.box(10, 13, 8, 2, BG, "▼", 11, WHITE, False)
        for x, label in ((1, "①"), (10, "②"), (19, "③")):
            m.paint(x, 16, 8, 12, CARD)
            m.box(x + 1, 17, 6, 6, ICON, label, 14)

    def play(m):
        m.paint(0, 0, W, 4, HUD)
        m.box(1, 0, 6, 2, HUD, "23/50", 9, WHITE, False)
        m.box(23, 0, 3, 2, BTN, "⏸", 10)
        m.box(1, 2, 3, 2, HUD, "★5", 8, WHITE, False)
        m.paint(4, 2, 9, 2, YELLOW)
        m.paint(13, 2, 5, 2, XP_EMPTY)
        m.box(20, 2, 6, 2, HUD, "● 120", 8, YELLOW, False)
        m.paint(0, 4, W, 1, WALL)
        m.paint(0, 4, 1, H - 4, WALL)
        m.paint(W - 1, 4, 1, H - 4, WALL)
        m.paint(0, H - 1, W, 1, WALL)
        m.paint(1, 5, W - 2, 3, PANEL)
        m.box(1, 5, 3, 3, ICON, "◆", 8)
        for x in (5, 9, 13, 17):
            m.box(x, 5, 3, 3, ICON, "◇", 8)
        m.paint(5, 30, 3, 2, OBST)
        m.paint(18, 18, 2, 3, OBST)
        m.circle(6, 13, 1.6, ENEMY_GRAY)
        m.circle(9, 21, 1.6, ENEMY_PINK)
        m.circle(20, 25, 1.6, ENEMY_BROWN)
        m.text(8, 11, "12", WHITE)
        m.paint(10, 30, 1, 1, WHITE)
        m.paint(9, 26, 1, 1, WHITE)
        m.paint(12, 33, 3, 1, GREEN)
        m.text(16, 33, "80", WHITE, 7)
        m.circle(13, 36, 1.6, WHITE)
        m.circle(19, 40, 3, RED_AREA, "!", "000000")
        m.paint(3, 44, 10, 1, RED)

    def cards(m):
        m.paint(0, 0, W, H, DIM)
        for i, x in enumerate((1, 10, 19)):
            m.paint(x, 14, 8, 13, CARD)
            m.box(x + 2, 15, 4, 4, ICON, "①", 11)
            m.box(x + 1, 19, 6, 1, CARD, "★★", 8, "000000", False)
            m.box(x + 1, 21, 6, 5, BG, "③", 10, WHITE)
            m.box(x + 2, 29, 4, 3, BTN, str(i + 1), 11)

    def pause(m):
        m.paint(0, 0, W, H, DIM)
        m.box(8, 12, 11, 4, BTN, "▶", 16)
        m.box(8, 18, 11, 4, BTN, "↻", 16)
        m.box(8, 24, 11, 4, BTN, "🏠", 14)
        m.box(4, 31, 3, 3, DIM, "🔊", 12, WHITE, False)
        m.paint(8, 32, 14, 1, BTN)
        m.paint(17, 31, 1, 3, WHITE)

    def clear(m):
        m.box(2, 4, 23, 15, PICTURE, "🏆", 28, WHITE)
        m.box(6, 22, 15, 3, PANEL, "⏱ 12:34", 11, WHITE, False)
        m.box(6, 26, 15, 3, PANEL, "● 120", 11, YELLOW, False)
        m.box(5, 33, 7, 5, BTN, "↻", 16)
        m.box(15, 33, 7, 5, BTN, "🏠", 14)

    def death(m):
        m.box(9, 4, 9, 8, PANEL, "💀", 26, WHITE, False)
        m.box(6, 14, 15, 3, PANEL, "23/50", 11, WHITE, False)
        m.box(3, 19, 3, 3, ICON, "◆", 8)
        for x, y in ((7, 19), (11, 19), (15, 19), (19, 19), (3, 23), (7, 23)):
            m.box(x, y, 3, 3, ICON, "◇", 8)
        m.box(5, 33, 7, 5, BTN, "↻", 16)
        m.box(15, 33, 7, 5, BTN, "🏠", 14)

    bands = [
        [("ホーム", home, "①一枚絵かプレイヤーの絵を置く場所（中身は未定）\n37/50＝最高記録　⏱＝最短タイム\n▶＝開始（武器を選ぶ画面へ）"),
         ("武器を選ぶ", weapon, "①②③＝武器3種の縦長のカード。中に武器の絵（UI の絵）\n▼＝前回選んだ武器"),
         ("プレイ（HUD）", play, "1段目：23/50＝ウェーブ、⏸。ボス戦では赤いボスのHPバー＋💀\n2段目：★5＋経験値バー（黄）、●＝コイン\n3段目：◆武器、◇強化（UI の絵。収まらなければ2段に折り返す）\n白＝プレイヤー（頭上に緑のHPバーと数字）、白い点＝弾、灰・桃・茶＝敵、12＝ダメージ数字\n赤い円と赤い線＝攻撃の予兆"),
         ("カード選択", cards, "画面を暗くして縦長のカード3枚を並べる\n①強化のアイコン　★★＝段階\n③実演アニメ（ゲームのスプライト）\n1 2 3＝キーでも選べる（クリックでも可）")],
        [("一時停止", pause, "▶続ける　↻やり直し　🏠ホーム\n🔊＝音量（つまみを左右に動かす）"),
         ("クリア", clear, "🏆の後ろに一枚絵（届くまでは🏆だけ）\n⏱＝クリアまでの時間　●＝得たコイン\n↻やり直し　🏠ホーム"),
         ("死亡", death, "💀と、たどり着いたウェーブ 23/50\n◆武器と◇取った強化のアイコン\n↻やり直し　🏠ホーム")],
    ]
    legend = [(HUD, "HUD の帯"), (WALL, "壁"), (OBST, "障害物"), (BTN, "ボタン（中立の色）"),
              (ICON, "アイコンの場所（中身は絵）"), (CARD, "カード（縦長）"), (PICTURE, "一枚絵の場所"), (WHITE, "プレイヤー・弾"),
              (ENEMY_GRAY, "敵（灰）"), (ENEMY_PINK, "敵（桃）"), (ENEMY_BROWN, "敵（茶）"),
              (RED, "攻撃の予兆（赤い線）"), (RED_AREA, "攻撃の予兆（赤い円）"), (GREEN, "HP"),
              (YELLOW, "経験値・コイン")]

    top = s.row + 2
    for band_no, band in enumerate(bands):
        for y in range(H):
            ws.row_dimensions[top + y].height = 9
        cap_row = top + H + 1
        cap_lines = 1
        for slot, (name, draw, caption) in enumerate(band):
            m = Mock(slot, top, name)
            m.screen()
            draw(m)
            ws.merge_cells(start_row=cap_row, start_column=m.c0, end_row=cap_row, end_column=m.c0 + W - 1)
            c = ws.cell(row=cap_row, column=m.c0, value=caption)
            c.font = font(size=8)
            c.alignment = WRAP
            cap_lines = max(cap_lines, lines_needed(caption, 34))
        if band_no == len(bands) - 1:
            m = Mock(len(band), top, "色の目安")
            for i, (color, label) in enumerate(legend):
                y = 1 + i * 3
                m.box(1, y, 3, 2, color)
                ws.merge_cells(start_row=top + y, start_column=m.c0 + 5, end_row=top + y + 1, end_column=m.c0 + W - 1)
                c = m.at(5, y)
                c.value = label
                c.font = font(size=9)
                c.alignment = Alignment(vertical="center")
        ws.row_dimensions[cap_row].height = 12 * cap_lines
        top = cap_row + 3
    s.row = top


def build_art(b):
    s = b.sheet("絵の方針", "絵の方針（ドット絵はキャラだけ）【提案】", SPEC + " §11、" + WORLD + " §6", [26, 66, 18])
    s.para("ドット絵で描くのはキャラクターだけ。カード、アイコン、ボタン、ピクトグラム、操作案内の絵は、普通の2Dの UI の絵で描く【決定】。")
    s.para("細かい見た目（線の有無、色の数、陰影、頭身）はデザイナーが決める。工数は下の大きさを前提に見積もっているので、大きさを変えるときは工数も見直す。")
    s.table(["項目", "前提", ""], [
        ("描画の基準解像度", "270×480（1080×1920 の 4倍拡大）。URP 2D の Pixel Perfect Camera を使う（フィールドとキャラ）。UI は別に、Canvas Scaler（基準 1080×1920）でなめらかに描く", ""),
        ("フィールド", "1画面＝270×480 の中に収める（壁を含む）。HUD はフィールドの上に重ねる。戦える広さはおよそ 240×400", ""),
        ("タイル", "16px のマス目（1ユニット＝16px、PPU 16）。フリー素材もこの大きさのものを選ぶ", ""),
        ("ドット絵にするもの", "キャラクターだけ（プレイヤー、敵、中ボス、ボス）。すべて 32×32 で描く。32×32 は 2×2 ユニット", "【決定】"),
        ("中ボス・ボス", "32×32 で描き、ゲーム内で 2倍に拡大して表示する（倍率は調整値）", ""),
        ("フリー素材", "タイル、障害物、エフェクトはドット絵のフリー素材を使う（大きさは 32×32 に縛らない）", ""),
        ("UI の絵", "カード、カードの枠、強化と武器のアイコン、ボタン、HUD のピクトグラム、操作案内の絵はドット絵にしない。普通の2Dの絵（なめらかな線）で描き、大きさも 32×32 に縛らない", "【決定】"),
        ("カード", "縦長（縦横の比は 3:2 くらい）。枠は伸ばしても崩れないよう 9スライスで描く", "【決定】縦長／【提案】比と描き方"),
        ("強化・武器のアイコン", "UI の絵。256×256 くらいで描き、カードや HUD では縮めて使う", ""),
        ("HUD のピクトグラム・ボタン", "UI の絵。128×128 くらいで描き、縮めて使う", ""),
        ("向き", "左右だけ（右向きを描き、左は反転）。上下の向きは描かない", ""),
        ("被弾の白い光り", "プログラマーがシェーダーで出す（コマを描かない）", ""),
        ("強化版の色", "プログラマーが色を変えて出す（別の絵を描かない）", ""),
        ("取り込み", "キャラとフリー素材：Filter Mode は Point、圧縮なし（PixelArtImportPostprocessor が自動で設定する）。UI の絵：Filter Mode は Bilinear（Point にしない）", ""),
    ])
    s.heading("受け渡しと素材")
    s.bullets([
        "UI の絵は1枚ずつの PNG（背景は透明）で渡す",
        "キャラの絵は PNG のスプライトシート（コマを横一列、1コマの大きさをそろえる）で渡す。Unity への取り込みとアニメの設定は、そのキャラや敵を担当するプログラマーが行う",
        "絵が届くまでは、プログラマーは仮の図形で作り、届いたら差し替える",
        "全員の素材について、間に合わない場合の代わり（仮の図形、既存の絵の流用）を先に決めておく",
        "背景とエフェクトはフリー素材。キャラの絵柄と合うかを1週目に確かめる。合わない場合は素材側の色をプログラマーが合わせる",
        "フリー素材は CC0 を優先する。CC-BY はクレジットをゲームの外（README と配布ページ）に書けば使ってよい。素材を買う予算は無い",
        "SF の素材には文字が入りやすい（画面の表示、警告の看板、機体の番号など）。取り込む前に確認して消す。docs/assets-list.md に記録する",
    ])
    s.heading("フリー素材（担当：C さん、約3人日。仮アイコンの 0.5 を含む）")
    s.table(["素材", "探すもの", ""], [
        ("床と壁", "金属の床と壁のタイル（ドット絵、16px）", ""),
        ("障害物", "コンテナ、柱、機械の箱（6パターンを組めるだけの種類）", ""),
        ("エフェクト", "命中、爆発、炎、冷却ガス、稲妻、回復、レベルアップの光", ""),
        ("弾", "光弾、光線、敵の弾（見つからなければプログラマーが図形で作る）", ""),
        ("仮アイコン", "強化と武器の仮アイコン（UI担当の絵が届くまで使う）", ""),
        ("音", "効果音、BGM（10ウェーブごとに変えられるなら変える）", ""),
        ("フォント", "数字と記号が入った、読みやすいフォント（ピクセルフォントでなくてよい）", ""),
    ])
    s.para("候補：Kenney、OpenGameArt（CC0 で絞り込む）。中身とライセンスは取り込む前に確認する。")


def build_world(b):
    s = b.sheet("世界観", "世界観（SF・確定版）", WORLD + " §1〜§3", [22, 56, 40])
    s.para("乗っ取られた宇宙ステーションの格納庫で、パワースーツの兵士が暴走したロボットの波を50回しのぎ切る。", bold=True)
    s.heading("フィールド")
    s.bullets([
        "宇宙ステーションの格納庫。1画面の縦長 270×480 の中に壁まで収める【決定】縦長／【提案】格納庫",
        "障害物（コンテナ、柱、機械の箱）の並びを6パターン用意し、ウェーブごとに切り替える。パターンは C さんが組む",
        "色合いの変化はプログラマーが照明と色調で出す。背景の絵は描き直さない。部分的な明かりは出さない",
    ])
    s.heading("照明（10ウェーブごと）")
    s.table(["ウェーブ", "照明", "注意"], [
        ("1〜10", "通常の白い照明", "色合いは彩度を抑え、色の意味の5色に見えないようにする"),
        ("11〜20", "薄暗い照明", ""),
        ("21〜30", "暗い紺色の照明", ""),
        ("31〜40", "照明がゆっくり明滅する", ""),
        ("41〜50", "外の惑星の淡い光", "警報灯のような赤い照明は使わない"),
    ])
    s.heading("プレイヤーと武器")
    s.table(["役割", "姿", "ひと目で分かる理由"], [
        ("プレイヤー", "パワースーツを着た兵士【決定】。選んだ武器を両手で構える", "武器を構える姿で「撃つ人」と分かる。武器の形で今どの武器か分かる"),
        ("銃", "小型の光線銃", "白い光弾"),
        ("レーザー", "長い銃身", "一瞬だけ伸びる白い光線"),
        ("ランチャー", "太い筒", "小さな弾が飛び、当たると白っぽい爆発"),
    ])
    s.para("敵とボスの姿は「敵とボス」シートにまとめた。")


def build_tech(b):
    s = b.sheet("技術方針", "技術方針【提案】", SPEC + " §12、CLAUDE.md、" + ROADMAP, [24, 70, 18])
    s.table(["項目", "方針", ""], [
        ("エンジン", "Unity 6000.5.10f1。URP 2D Renderer のテンプレート", ""),
        ("ドット絵", "Pixel Perfect Camera（基準 270×480。計画2のタスク0で 480×270 から直す）。キャラとフリー素材の画像は Filter Mode を Point、圧縮なし", ""),
        ("UI", "uGUI の Canvas（Screen Space - Overlay）。Canvas Scaler は Scale With Screen Size、基準 1080×1920。UI の画像は Filter Mode を Bilinear。のちの iPad・iPhone に備えて、画面の端を基準に置き、端に余白をとる", ""),
        ("画面", "Player Settings で縦長のウィンドウ（540×960）。全画面では整数倍で拡大し、左右は黒い帯", ""),
        ("入力", "Input System。キーボードとマウスのドラッグを同じ「移動ベクトル」に変換する", ""),
        ("データ", "武器・強化・敵・ウェーブ・障害物のパターンは ScriptableObject。データを足すだけで増やせるようにする。数値はコードに直接書かない", ""),
        ("文字", "TextMeshPro、数字と記号だけのフォントのアセット（UI の絵に合う読みやすいフォント）", ""),
        ("シーン", "Boot／Home／Run の3つ。武器を選ぶ画面は Home の中。Run はフィールド1つで、ウェーブはデータを順に読んで敵と障害物を出す", ""),
        ("カメラ", "MainCamera.prefab の位置は (0, 0, -10) から動かさない。カメラは動かない", ""),
        ("共同作業", "シーンを同時に編集しない。Run.unity を編集するのは C さんだけ。ほかの人はプレハブと Scenes/Sandbox/Sandbox_<担当>.unity。UnityYAMLMerge を各自設定する", ""),
        ("ビルド", "Windows と Mac のスタンドアロン", ""),
        ("テスト", "純粋なロジック（ダメージ計算、強化の重ね取り、カードの抽選、ウェーブの進行、障害物のパターンの選び方）は EditMode テスト", ""),
    ])
    s.heading("アセンブリとフォルダ")
    s.table(["アセンブリ", "置き場所と中身", "名前空間"], [
        ("PictoArcher.Core", "Assets/_Project/Scripts/Core。純粋な C#（MonoBehaviour と UnityEngine の部品を使わない）。テストの対象", "PictoArcher.Core"),
        ("PictoArcher.Game", "Assets/_Project/Scripts/Game。MonoBehaviour と ScriptableObject", "PictoArcher.Game"),
        ("PictoArcher.Editor", "Assets/_Project/Scripts/Editor。エディタ専用", "PictoArcher.Editor"),
        ("PictoArcher.Tests.EditMode", "Assets/_Project/Tests/EditMode/<クラス名>Tests.cs。NUnit", "PictoArcher.Tests.EditMode"),
    ])
    s.heading("レイヤー")
    s.table(["番号", "名前", "使う物"], [
        ("6", "Player", "プレイヤー"),
        ("7", "Enemy", "敵"),
        ("8", "PlayerShot", "プレイヤーの弾（Projectile）"),
        ("9", "Wall", "壁と障害物"),
    ], center_cols=(1,))
    s.heading("コードの名前（役割で付ける）")
    s.table(["役割", "コードの名前", ""], [
        ("プレイヤーの弾", "Projectile（レイヤー PlayerShot）", ""),
        ("武器（銃／レーザー／ランチャー）", "WeaponId.Shot／WeaponId.Beam／WeaponId.Blast", ""),
        ("接触攻撃型／単発攻撃型／単発範囲型", "Chaser／Shooter／Lobber", ""),
        ("三発扇型／五発連射型／自爆型", "SpreadShooter／BurstShooter／Bomber", ""),
        ("地面燃焼型／落雷型／ハンマー回転型／時間制限型", "Burner／Striker／Spinner／Countdown", ""),
        ("中ボス／ボスA／最後のボス", "Splitter／BossA／FinalBoss", ""),
        ("障害物のパターン", "ObstacleLayout（ScriptableObject。WaveDefinition から指定する）", ""),
        ("強化", "SkillId（強化シートの26枚）", ""),
    ])


def build_roles(b):
    s = b.sheet("分担", "分担", SPEC + " §13、" + WORLD + " §4・§5・§7", [20, 74, 22])
    s.table(["担当", "作るもの", "工数"], [
        ("Aさん（プログラマー）", "プレイヤー：操作、射撃、武器3種、弾と当たり判定、障害物との当たり、強化の仕組みと26枚の効果、経験値とコイン、手触り（ヒットストップ、揺れ）、プレイヤーのアニメの組み込み", "22.5人日（工数_プログラマー）"),
        ("Bさん（プログラマー）", "敵：敵AI 10種、中ボス1種、ボス2種、予兆、出現、被弾の光りと強化版の色、敵のアニメの組み込み、プログラムで出す敵の動き", "23人日"),
        ("Cさん（プログラマー）", "進行とUI：ウェーブの進行と50ウェーブの構成、障害物のパターン、ホームと武器を選ぶ画面、カード選択、カードの実演アニメ、HUD とダメージ数字、結果画面、チュートリアル、音、保存、照明の変化、フリー素材と素材一覧、仮アイコン、文字ゼロのルール管理、CLAUDE.md、この仕様書", "22.5人日"),
        ("キャラ担当", "パワースーツの兵士（武器3種）、通常の敵（7つの見た目で10種）、中ボス1種、ボス2種", "対策なし28人日／対策をして22人日（工数_絵）"),
        ("スキル系UI担当", "強化のアイコン15種（26枚分）、武器のアイコン3種、カードの枠、HUD とボタンのピクトグラム、操作案内の絵、画面のレイアウト、フォントの記号の描き足し", "対策なし26人日／対策をして約23.5人日"),
        ("デザイナー（補助）", "背景の飾り小物（フリー素材の不足分を補う）、宣伝用のキービジュアル（ゲームの外で使う。文字なし）、クリア画面の一枚絵", "届いた時点で差し込む"),
    ])
    s.bullets([
        "前の版で A さんが担当していた「カードの実演アニメ」「ダメージ数字」「照明の変化」は C さんに移した",
        "プログラマーは、絵が届くまで仮の図形で作り、届いたら差し替える",
        "デザイナー（補助）の作業は、ほかの担当の作業を待たずに始められ、ほかの担当もこの作業を待たない",
        "絵の順番は、プログラムで使い始める順に合わせてデザイナーが入れ替えてよい",
    ])


PROG_WORK = {
    "A": [
        ("移動と射撃", 2, "計画1・2"),
        ("武器3種（銃、レーザー、ランチャー）", 2.5, "計画2"),
        ("弾の管理・当たり判定・障害物との当たり", 1, "計画1・2"),
        ("強化の仕組み（カード、重ね取り、抽選）", 2, "計画2"),
        ("強化26枚の効果", 12, "計画2・4"),
        ("手触り（ヒットストップ、揺れ）", 2, "計画5"),
        ("経験値とコインの落下と吸い寄せ", 0.5, "計画2"),
        ("プレイヤーのアニメの組み込み（武器3種）", 0.5, "計画2〜5"),
    ],
    "B": [
        ("敵の土台と予兆と出現", 2, "計画1・2"),
        ("通常の敵10種", 8, "計画1・2・4"),
        ("中ボス", 1.5, "計画3"),
        ("ボス2種（ボスAの強化版を含む）", 6, "計画3・4"),
        ("被弾の白い光りと強化版の色", 1, "計画4"),
        ("敵のアニメの組み込み", 2.5, "計画2〜5"),
        ("プログラムで出す動き（揺れ、回転、点滅）", 1, "計画4"),
        ("ボスの動きの調整", 1, "計画4・8週"),
    ],
    "C": [
        ("土台・文字ゼロの仕組み・CLAUDE.md（計画1で済み）", 1.5, "計画1"),
        ("縦長への切り替え（計画1の直し）", 0.5, "計画2"),
        ("ウェーブの進行", 1, "計画1"),
        ("50ウェーブの構成", 2, "計画3"),
        ("障害物のパターンと切り替え", 1.5, "計画3"),
        ("HUD", 1.5, "計画1〜3"),
        ("カード選択の画面", 1, "計画2"),
        ("武器を選ぶ画面", 0.5, "計画3"),
        ("ホーム・結果・死亡の画面", 1, "計画3"),
        ("保存", 0.5, "計画3"),
        ("一時停止", 0.5, "計画3"),
        ("チュートリアル", 2, "計画4"),
        ("音", 1, "計画5"),
        ("フリー素材の取り込み", 2.5, "1〜7週"),
        ("仮アイコン（フリー素材で用意）", 0.5, "計画2"),
        ("カードの実演アニメ", 3, "計画4"),
        ("ダメージ数字", 0.5, "計画2"),
        ("照明の変化", 0.5, "計画4"),
        ("ビルド（縦長のウィンドウ）", 1, "8週"),
    ],
}

PLAN1_TASKS = [
    ("1 土台と CLAUDE.md", "C", "1（初日）", "なし", 0.75),
    ("2 データ形式と共通部品", "A", "1", "1", 0.5),
    ("3 文字ゼロとドット絵の取り込み", "C", "1", "1", 0.75),
    ("4 移動ベクトル", "A", "1", "1", 0.5),
    ("5 照準と連射", "A", "1", "1", 0.5),
    ("6 HPと無敵時間", "B", "1", "1", 0.5),
    ("7 ウェーブの進行", "C", "1", "1", 1),
    ("8 プレイヤーと弾", "A", "1–2", "2, 3, 4, 5, 6", 1.5),
    ("9 追う敵と出現予兆", "B", "1–2", "2, 3, 6", 2.25),
    ("10 フィールド・ウェーブの流れ・HUD", "C", "2", "3, 7, 8, 9", 1.5),
]


def build_prog_effort(b):
    s = b.sheet("工数_プログラマー", "プログラマーの工数（1人 24人日）", SPEC + " §13、docs/superpowers/plans/2026-10-26-plan1-core-loop.md",
                [10, 46, 13, 13, 11, 14, 30], has_inputs=True)
    s.para("見積もりは「Claude を使うとコードを書く作業が約半分になり、全体で3〜4割少なくなる」という推測にもとづく。2週目の終わりに実績と比べて見直す。速くならなかった場合は3人とも 32〜35人日になり、約10日はみ出す。そのときは削る順シートを当てはめる。")
    s.heading("使える日数（調整値）")
    cap_row = s.row
    s.table(["", "項目", "人日", "", "", "", "メモ"], [
        ("", "1人が使える作業日", 24, "", "", "", "週3日×8週（設計書 §3）"),
    ], input_cols=(3,))
    cap = "$C${}".format(cap_row + 1)

    s.heading("担当ごとの見積もりと実績")
    summary = []
    for who, items in PROG_WORK.items():
        rows = [[who, name, est, None, None, plan, ""] for name, est, plan in items]
        hr, first, last = s.table(["担当", "作業", "見積もり（人日）", "実績（人日）", "差", "計画", "メモ"],
                                  rows, input_cols=(4,), center_cols=(1, 3, 4, 5, 6), key_col=2,
                                  formats={3: "0.0#", 4: "0.0#", 5: "+0.0#;-0.0#;0"})
        for r in range(first, last + 1):
            s.ws.cell(row=r, column=5, value='=IF(D{0}="","",D{0}-C{0})'.format(r))
        r = s.row
        s.cell(r, 2, "小計（{}）".format(who), font=font(True), border=True)
        s.cell(r, 3, "=SUM(C{}:C{})".format(first, last), font=font(True), border=True, align=CENTER, fmt="0.0#")
        s.cell(r, 4, "=SUM(D{}:D{})".format(first, last), font=font(True), border=True, align=CENTER, fmt="0.0#")
        s.row += 1
        summary.append((who, r))

    s.heading("まとめ（数式）")
    rows = []
    for who, r in summary:
        rows.append([who, "見積もりの合計", "=C{}".format(r), "=D{}".format(r), None, "={}-C{}".format(cap, r), ""])
    hr, first, last = s.table(["担当", "", "見積もり", "実績の合計", "", "余裕（使える日−見積もり）", "メモ"], rows,
                              center_cols=(1, 3, 4, 6), formats={3: "0.0#", 4: "0.0#", 6: "0.0#"})
    s.ws.cell(row=first, column=7, value="設計書では余裕 1.5日")
    s.ws.cell(row=first + 1, column=7, value="設計書では余裕 1日")
    s.ws.cell(row=first + 2, column=7, value="設計書では余裕 1.5日")
    s.para("余裕の日は、数値の調整、毎週のテスト、バグ修正（8週目を含む）、Claude が書いたコードの確認にあてる。")

    s.heading("計画1のタスク（見積もりと実績を比べる）")
    rows = [[t, who, wk, dep, est, None, None] for t, who, wk, dep, est in PLAN1_TASKS]
    hr, first, last = s.table(["タスク", "担当", "週", "先に要るもの", "見積もり", "実績", "差"], [
        [r[1], r[0], r[4], r[5], r[6], r[2], r[3]] for r in rows
    ], input_cols=(4,), center_cols=(1, 3, 4, 5, 6), key_col=2, formats={3: "0.0#", 4: "0.0#", 5: "+0.0#;-0.0#;0"})
    # 列の並び：担当, タスク, 見積もり, 実績, 差, 週, 先に要るもの
    for col, h in enumerate(["担当", "タスク", "見積もり", "実績（記入）", "差", "週", "先に要るもの"], start=1):
        s.ws.cell(row=hr, column=col, value=h)
    for r in range(first, last + 1):
        s.ws.cell(row=r, column=5, value='=IF(D{0}="","",D{0}-C{0})'.format(r))
    r = s.row
    s.cell(r, 2, "合計", font=font(True), border=True)
    s.cell(r, 3, "=SUM(C{}:C{})".format(first, last), font=font(True), border=True, align=CENTER, fmt="0.0#")
    s.cell(r, 4, "=SUM(D{}:D{})".format(first, last), font=font(True), border=True, align=CENTER, fmt="0.0#")
    s.row += 1
    s.para("実績は、そのタスクに実際にかかった作業日（0.25 刻みでよい）。例：半日なら 0.5。")


CHAR_ART = [
    ("試作（絵柄・色の数・頭身を決め、パワースーツとクモ型のラフ）", "—", "—", 1.5, 1),
    ("パワースーツ（プレイヤー）", "32×32", "待機4／走り6／被弾1／倒れる4／撃つ2×武器3種", 5, 2),
    ("クモ型（モデル3）", "32×32", "歩く4／壊れる3＋自爆型の差分（0.5）", 2, 3),
    ("警備ロボ（モデル1）", "32×32", "待機2／構え2／撃つ2／壊れる3＋範囲型の差分（0.5）", 2, 3),
    ("多連装ロボ（モデル2）", "32×32", "待機2／構え2／撃つ2／壊れる3＋五発連射型の差分（0.5）", 2, 4),
    ("金属の塊（中ボス）", "32×32（2倍表示）", "うごめく4／割れる2／消える3", 2, 4),
    ("二足メカ（ボスA）", "32×32（2倍表示）", "歩く4／ため2／突進3／ハッチが開く3／壊れる4", 3, 5),
    ("作業ロボ（モデル6）", "32×32", "歩く4／振る2／壊れる3", 1.5, 5),
    ("火炎放射ロボ（モデル4）", "32×32", "歩く4／放射2／壊れる3", 1.5, 6),
    ("送電ロボ（モデル5）", "32×32", "浮く2／ため2／壊れる3", 1.5, 6),
    ("時限ロボ（モデル7）", "32×32", "歩く4／壊れる3", 1.5, 6),
    ("中枢機械（最後のボス）", "32×32（2倍表示）", "待機4／突進3／撃つ3／呼ぶ3／壊れる4", 3.5, 7),
    ("直しと統一", "—", "—", 1, 8),
]
CHAR_MEASURES = [
    ("1 同じモデルの2種の差分を描かず、大きさと予兆の違いだけで見分ける", 1.5, ""),
    ("2 「壊れる」のコマを描かず、共通の爆発エフェクトと部品が飛ぶ動き（プログラム）で出す", 2, "B さんが約0.5人日増える（見積もりに入れてある）"),
    ("3 中枢機械の「呼ぶ」を描かず、目が光るだけにする", 1, ""),
    ("4 送電ロボの「浮く」と作業ロボの「振る」をプログラムの揺れと回転で出す", 1, "B さんが増える（2 と合わせて約0.5人日）"),
    ("5 パワースーツの武器を別の絵にして手元に重ねる。撃つコマは1組だけ", 0.5, "A さんのアニメの組み込みが少し増える"),
]
UI_ART = [
    ("数字と記号のフォントの確認（★ ⏱ × などを描き足す）", "—", "フリー素材のフォントに足りない記号（UI の絵で描く）", 0.5, 2),
    ("操作案内の絵（最小版は2週目）", "—", "矢印キー、マウス、手の動きのアニメ（キーに英字を描かない）", 2, 7),
    ("カードの枠", "縦長・9スライス", "通常／選んでいる／★の段階表示（UI の絵）", 2.5, 3),
    ("武器のアイコン 3種", "256×256 くらい", "銃・レーザー・ランチャー", 1, 3),
    ("強化のアイコン（15種、カード26枚分。半分は4週目）", "256×256 くらい", "本体の絵 約20枚＋向きの印 約8個", 10, 6),
    ("画面のレイアウト", "—", "ホーム、武器選択、カード選択、クリア、死亡、一時停止（縦長）", 1.5, 4),
    ("HUD", "—", "HPバー、経験値バー、ボスのHPバー、コイン、23/50 の枠、武器と強化の一覧の枠", 3.5, 5),
    ("ボタンとピクトグラム", "128×128 くらい", "▶ ⏸ ↻ 🏠 🔊 🏆 💀 ⚠ ⏱ ♥ ★ × など約12個", 2, 7),
    ("直しと統一", "—", "実際の画面で見て、色・大きさ・並びを直す（新しい絵は足さない）", 3, 8),
]
UI_MEASURES = [
    ("1 操作案内の絵は最小版のまま使い、完成版を作らない", 1, ""),
    ("2 6 と 11 の向きの印は、ボタン用の矢印のピクトグラムを流用する", 1, ""),
    ("3 ネタ系の強化のアイコンを作らない（中身が決まるまで）", 0.5, ""),
]


def build_art_effort(b):
    s = b.sheet("工数_絵", "絵の担当の工数", WORLD + " §4〜§7", [46, 12, 46, 10, 10, 12], has_inputs=True)
    s.para("どの対策を採るかは、デザイナーとチームで決める（2週目の終わりの見直しで）。「採用」に ○ を書くと、採用後の合計が変わる。")
    s.heading("使える日数（調整値）")
    cap_row = s.row
    s.table(["項目", "人日", "メモ", "", "", ""], [
        ("1人が使える作業日", 24, "週3日×8週"),
        ("新しい絵に使える日（7週目の終わりまで）", 21, "8週目の3日は直しと統一だけ"),
    ], input_cols=(2,))
    cap_all = "$B${}".format(cap_row + 1)
    cap_new = "$B${}".format(cap_row + 2)

    def art_block(title, items, measures, who):
        s.heading(title)
        hr, first, last = s.table(["素材", "大きさ", "動き・内容", "人日", "期限（週）", ""],
                                  [list(x) + [""] for x in items], center_cols=(2, 4, 5), formats={4: "0.0#"})
        s.ws.cell(row=hr, column=5, value="期限（週）")
        r = s.row
        s.cell(r, 1, "合計（対策なし）", font=font(True), border=True)
        s.cell(r, 4, "=SUM(D{}:D{})".format(first, last), font=font(True), border=True, align=CENTER, fmt="0.0#")
        s.cell(r + 1, 1, "7週目までの新しい絵", font=font(True), border=True)
        s.cell(r + 1, 4, '=SUMIF(E{}:E{},"<=7",D{}:D{})'.format(first, last, first, last), font=font(True), border=True, align=CENTER, fmt="0.0#")
        total, new = "D{}".format(r), "D{}".format(r + 1)
        s.row = r + 2
        s.heading(who + "の対策")
        mh, mf, ml = s.table(["対策", "減る人日", "影響", "採用（○）", "", ""],
                             [[m, d, note, None] for m, d, note in measures], input_cols=(4,),
                             center_cols=(2, 4), key_col=1, formats={2: "0.0#"})
        r = s.row
        rows = [
            ("全部の対策をした場合の合計", "={}-SUM(B{}:B{})".format(total, mf, ml)),
            ("採用した対策だけの合計", '={}-SUMIF(D{}:D{},"○",B{}:B{})'.format(total, mf, ml, mf, ml)),
            ("採用した対策だけの、7週目までの新しい絵", '={}-SUMIF(D{}:D{},"○",B{}:B{})'.format(new, mf, ml, mf, ml)),
            ("はみ出す日数（合計−使える日。マイナスなら収まる）", "=B{}-{}".format(r + 1, cap_all)),
            ("はみ出す日数（新しい絵−使える日）", "=B{}-{}".format(r + 2, cap_new)),
        ]
        for i, (label, f) in enumerate(rows):
            s.cell(r + i, 1, label, font=font(True), border=True)
            s.cell(r + i, 2, f, font=font(True), border=True, align=CENTER, fmt="+0.0#;-0.0#;0" if i >= 3 else "0.0#")
        s.row = r + len(rows)

    art_block("キャラ担当の素材", CHAR_ART, CHAR_MEASURES, "キャラ担当")
    art_block("スキル系UI担当の素材", UI_ART, UI_MEASURES, "スキル系UI担当")
    s.heading("フリー素材（C さん、約3人日）")
    s.para("C さんの見積もり（フリー素材の取り込み 2.5＋仮アイコン 0.5）に入っている。中身は絵の方針シート。仮アイコンは、前の版では UI担当が作る予定だった（UI担当の −3.5人日）。")
    s.heading("デザイナー（補助）の作業")
    s.table(["作業", "", "内容", "", "", ""], [
        ("背景の飾り小物", "", "フィールドに置く飾り（配管、ケーブル、ランプ、床の傷など）。文字や番号を描かない。フリー素材で足りない分を補う"),
        ("キービジュアル", "", "宣伝用の一枚絵（文字なし）。ゲームの外で使う"),
        ("クリア画面の一枚絵", "", "🏆 の後ろに出す絵。届くまでは 🏆 だけで出す"),
    ])
    s.para("どれもほかの担当の作業を待たずに始められ、ほかの担当もこの作業を待たない。届いた時点で差し込む。")


SCHEDULE = [
    (1, "移動・自動射撃・敵1体が動く。データ形式、CLAUDE.md、ブランチの運用を決める（計画1）",
     "ドット絵の大きさと色の決定、試作（パワースーツとクモ型のラフ）。フリー素材の候補を集める", "", "計画1"),
    (2, "縦長の画面に切り替える（タスク0）。1つのフィールドで数ウェーブ続けて遊べる。実際の速さと見積もりを比べる",
     "パワースーツの完成。アイコンの試作、操作案内の最小版（UI担当）。仮アイコンは C さんがフリー素材で用意", "見積もりと実績を比べる", "計画1・2"),
    (3, "レベルアップとカード3枚、強化の半分、武器3種、敵4種（接触・単発・三発扇・自爆）",
     "クモ型、警備ロボ、カードの枠、武器のアイコン", "毎週のプレイテストを始める（ウェーブ5まで）", "計画2"),
    (4, "全体の流れ：50ウェーブ（仮の構成）、障害物のパターン、中ボス、ボスA、ホームと武器を選ぶ画面、結果画面、保存",
     "多連装ロボ、中ボス、強化のアイコンの半分、画面のレイアウト", "テストはウェーブ10まで。武器の違いも測る", "計画3"),
    (5, "中身を揃える：強化の残り、敵（単発範囲・ハンマー・五発連射・地面燃焼）、ボスAの強化版（20〜40）、チュートリアル",
     "ボスA、ハンマーの作業ロボ、HUD", "", "計画4"),
    (6, "敵（落雷・時間制限）、最後のボス（絵が届くまでは仮の図形）、難易度調整",
     "火炎ロボ、送電ロボ、時限ロボ、強化のアイコンの残り", "アイコンの期限", "計画4"),
    (7, "手触りの仕上げ：ヒットストップ、揺れ、効果音、アイコンの統一。届いた絵を差し替える",
     "最後のボス、ボタンのピクトグラム、操作案内の完成版", "新しい絵の期限", "計画5"),
    (8, "新機能禁止。バグ修正と最終テスト。最後のボスの絵を入れて調整。Windows と Mac のビルド",
     "新しい絵は入れない。直しと統一だけ", "提出", "—"),
]


def build_schedule(b):
    s = b.sheet("スケジュール", "スケジュール（8週間）", SPEC + " §13、" + ROADMAP, [8, 12, 12, 9, 44, 40, 24, 10], has_inputs=True)
    r = s.row
    s.cell(r, 1, "開始日", font=font(True))
    s.ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=2)
    c = s.cell(r, 3, PROJECT_START, fill=fill(C_INPUT), border=True, fmt="yyyy/m/d", align=CENTER)
    s.cell(r, 5, "1週目の月曜日。ここを変えると、このシートと作業工程シートの日付がすべて変わる。", font=font(size=9, color=C_GRAY))
    start_ref = "'スケジュール'!$C${}".format(r)
    b.start_ref = start_ref
    s.cell(r + 1, 1, "今日", font=font(True))
    s.ws.merge_cells(start_row=r + 1, start_column=1, end_row=r + 1, end_column=2)
    s.cell(r + 1, 3, "=TODAY()", border=True, fmt="yyyy/m/d", align=CENTER)
    s.cell(r + 2, 1, "今は何週目", font=font(True))
    s.ws.merge_cells(start_row=r + 2, start_column=1, end_row=r + 2, end_column=2)
    s.cell(r + 2, 3, '=IF(C{0}<C{1},"開始前",IF(C{0}>C{1}+55,"終了",INT((C{0}-C{1})/7)+1))'.format(r + 1, r),
           border=True, align=CENTER)
    s.row = r + 4
    b.input_cells = [("スケジュール", "C{}".format(r))]

    s.heading("週ごとの予定")
    rows = [[w, None, None, None, prog, art, mark, plan] for w, prog, art, mark, plan in SCHEDULE]
    hr, first, last = s.table(["週", "開始日", "終了日", "今週", "プログラム", "絵", "期限・節目", "計画"],
                              rows, center_cols=(1, 2, 3, 4, 8), formats={2: "m/d", 3: "m/d"}, freeze=False)
    for rr in range(first, last + 1):
        s.ws.cell(row=rr, column=2, value="=$C${}+(A{}-1)*7".format(r, rr)).number_format = "m/d"
        s.ws.cell(row=rr, column=3, value="=B{}+6".format(rr)).number_format = "m/d"
        s.ws.cell(row=rr, column=4, value='=IF(AND(TODAY()>=B{0},TODAY()<=C{0}),"◀ 今週","")'.format(rr))
    s.ws.conditional_formatting.add("A{}:H{}".format(first, last),
                                    FormulaRule(formula=['$D{}<>""'.format(first)], fill=fill(C_DOING)))
    s.bullets([
        "絵が届くまでは、プログラマーは仮の図形で作る。絵の順番は、プログラムで使い始める順に合わせて、デザイナーが入れ替えてよい",
        "最後のボスの絵は7週目に届くので、プログラム側は6週目に仮の図形で動きを仕上げておき、届いたら差し替えるだけにする",
        "計画は1つずつ書く。前の計画の「データ形式とルール」のタスクがマージされたら、次の計画を書いてよい（ロードマップ）",
        "見積もりより遅い人がいたら、計画2の残りを進める前に、削る順をどこまで当てはめるかをチームで決める（2週目の終わり）",
    ])


TASKS = [
    # (ID, 計画, タスク, 担当, 先に要るもの, 開始週, 終了週, 見積もり, 状態, Issue, メモ)
    ("P1-01", "計画1", "土台と CLAUDE.md", "C", "", 1, 1, 0.75, "済み", "", "PR #1"),
    ("P1-02", "計画1", "データ形式と共通部品", "A", "P1-01", 1, 1, 0.5, "済み", "", "PR #1"),
    ("P1-03", "計画1", "文字ゼロとドット絵の取り込み（NoTextGuardTests、PixelArtImportPostprocessor）", "C", "P1-01", 1, 2, 0.75, "未着手", "", ""),
    ("P1-04", "計画1", "移動ベクトル", "A", "P1-01", 1, 1, 0.5, "済み", "", "PR #1"),
    ("P1-05", "計画1", "照準と連射", "A", "P1-01", 1, 1, 0.5, "済み", "", "PR #1"),
    ("P1-06", "計画1", "HPと無敵時間", "B", "P1-01", 1, 1, 0.5, "済み", "", "PR #1"),
    ("P1-07", "計画1", "ウェーブの進行", "C", "P1-01", 1, 1, 1, "済み", "", "PR #1"),
    ("P1-08", "計画1", "プレイヤーと弾", "A", "P1-02〜06", 1, 2, 1.5, "未着手", "", ""),
    ("P1-09", "計画1", "追う敵（Chaser）と出現予兆", "B", "P1-02, 03, 06", 1, 2, 2.25, "未着手", "", ""),
    ("P1-10", "計画1", "フィールド・ウェーブの流れ・HUD（HP と 1/50）", "C", "P1-03, 07, 08, 09", 2, 2, 1.5, "未着手", "", ""),
    ("P2-00", "計画2", "タスク0：第3版に合わせて直す（カメラ 270×480、Field_Test、Player Settings、WaveKind.Angel をなくす、SkillId を26枚に、CLAUDE.md）", "C", "計画1のマージ", 2, 2, 0.5, "未着手", "", "これが main に入るまで、縦長を前提にしたプレハブを作らない"),
    ("P2-01", "計画2", "経験値とコインの落下と吸い寄せ", "A", "P2-00", 3, 3, 0.5, "未着手", "", ""),
    ("P2-02", "計画2", "強化の仕組み（カード、重ね取り、抽選、上限）", "A", "P2-00", 3, 3, 2, "未着手", "", "Core に置き、EditMode テストを書く"),
    ("P2-03", "計画2", "レベルアップとカード選択の画面", "C", "P2-02", 3, 3, 1, "未着手", "", ""),
    ("P2-04", "計画2", "強化の効果（前半 約13枚。強化シートの「先に作る目安」）", "A", "P2-02", 3, 4, 6, "未着手", "", "どれを入れるかは A さんが計画2で決める"),
    ("P2-05", "計画2", "武器3種（銃・レーザー・ランチャー）", "A", "P1-08", 3, 3, 2.5, "未着手", "", ""),
    ("P2-06", "計画2", "敵3種の追加（単発攻撃型・三発扇型・自爆型）と予兆線", "B", "P1-09", 2, 3, 2.4, "未着手", "", ""),
    ("P2-07", "計画2", "ダメージ数字", "C", "P1-10", 3, 3, 0.5, "未着手", "", ""),
    ("P2-08", "計画2", "仮アイコン（フリー素材）と操作案内の最小版の組み込み", "C", "P2-00", 2, 3, 0.5, "未着手", "", ""),
    ("P2-09", "計画2", "HUD の残り（経験値バー、★n、コイン、武器と強化のアイコン一覧）", "C", "P2-03", 3, 3, 1.5, "未着手", "", ""),
    ("P3-01", "計画3", "50ウェーブの仮の構成", "C", "P1-07", 4, 4, 2, "未着手", "", ""),
    ("P3-02", "計画3", "障害物のパターン6つとウェーブごとの切り替え（ObstacleLayout）", "C", "P2-00", 4, 4, 1.5, "未着手", "", ""),
    ("P3-03", "計画3", "中ボス（分裂、Splitter）と ⚠", "B", "P2-06", 4, 4, 1.5, "未着手", "", ""),
    ("P3-04", "計画3", "ボスA（ウェーブ10の形）とボスの HP バー", "B", "P2-06", 4, 4, 2.5, "未着手", "", ""),
    ("P3-05", "計画3", "Boot と Home、クリア画面と死亡画面", "C", "P2-00", 4, 4, 1, "未着手", "", ""),
    ("P3-06", "計画3", "武器を選ぶ画面", "C", "P2-05", 4, 4, 0.5, "未着手", "", ""),
    ("P3-07", "計画3", "保存（JSON。前回の武器を含む）", "C", "P3-05", 4, 4, 0.5, "未着手", "", ""),
    ("P3-08", "計画3", "一時停止メニュー", "C", "P3-05", 4, 4, 0.5, "未着手", "", ""),
    ("P3-09", "計画3", "デバッグ操作（保存を消す、好きなウェーブから、好きな強化を取る）", "C", "P3-07", 4, 4, None, "未着手", "", "好きな強化を取る操作は A さんと相談。余裕の日を使う"),
    ("P4-01", "計画4", "強化の効果（残り 約13枚）", "A", "P2-04", 5, 6, 6, "未着手", "", ""),
    ("P4-02", "計画4", "敵4種（単発範囲型・ハンマー回転型・五発連射型・地面燃焼型）", "B", "P2-06", 5, 5, 3.2, "未着手", "", ""),
    ("P4-03", "計画4", "敵2種（落雷型・時間制限型）", "B", "P2-06", 6, 6, 1.6, "未着手", "", ""),
    ("P4-04", "計画4", "ボスAの強化版（20 から範囲攻撃、30・40 は強化）", "B", "P3-04", 5, 5, 1, "未着手", "", ""),
    ("P4-05", "計画4", "最後のボス（絵が届くまでは仮の図形）", "B", "P3-04", 6, 6, 2.5, "未着手", "", ""),
    ("P4-06", "計画4", "被弾の白い光りのシェーダーと強化版の色", "B", "P2-06", 5, 5, 1, "未着手", "", ""),
    ("P4-07", "計画4", "プログラムで出す敵の動き（揺れ、回転、点滅）", "B", "P2-06", 5, 6, 1, "未着手", "", ""),
    ("P4-08", "計画4", "チュートリアル（ウェーブ0）", "C", "P2-03", 5, 5, 2, "未着手", "", ""),
    ("P4-09", "計画4", "カードの実演アニメ", "C", "P2-03", 5, 6, 3, "未着手", "", ""),
    ("P4-10", "計画4", "10ウェーブごとの照明の変化", "C", "P3-01", 6, 6, 0.5, "未着手", "", ""),
    ("P4-11", "計画4", "難易度の調整", "全員", "P3-01", 6, 6, None, "未着手", "", "余裕の日を使う"),
    ("P5-01", "計画5", "手触り（ヒットストップ、画面の揺れ）", "A", "P2-05", 7, 7, 2, "未着手", "", ""),
    ("P5-02", "計画5", "効果音と BGM", "C", "P3-05", 7, 7, 1, "未着手", "", ""),
    ("P5-03", "計画5", "アイコンの統一、届いた絵の差し替え、素材一覧の最終確認", "全員", "", 7, 7, None, "未着手", "", ""),
    ("P5-04", "計画5", "プレイヤーのアニメの組み込み（武器3種）", "A", "", 2, 7, 0.5, "未着手", "", "絵が届くたびに"),
    ("P5-05", "計画5", "敵のアニメの組み込み", "B", "", 3, 7, 2.5, "未着手", "", "絵が届くたびに"),
    ("P5-06", "計画5", "フリー素材の取り込みと docs/assets-list.md", "C", "", 1, 7, 2.5, "作業中", "", ""),
    ("W8-01", "8週目", "最後のボスの絵を入れて調整、ボスの動きの調整", "B", "P4-05", 8, 8, 1, "未着手", "", ""),
    ("W8-02", "8週目", "バグ修正と最終テスト", "全員", "", 8, 8, None, "未着手", "", "新しい機能は入れない"),
    ("W8-03", "8週目", "Windows と Mac のビルド（縦長のウィンドウ）", "C", "", 8, 8, 1, "未着手", "", ""),
    ("T-01", "毎週", "初見の人でプレイテスト（合格ラインシートに記録）", "全員", "P2-04", 3, 8, None, "未着手", "", "その週に手の空いている人"),
    ("ART-01", "絵", "試作（絵柄、パワースーツとクモ型のラフ）", "キャラ担当", "", 1, 1, 1.5, "未着手", "", ""),
    ("ART-02", "絵", "パワースーツ（プレイヤー）", "キャラ担当", "ART-01", 1, 2, 5, "未着手", "", ""),
    ("ART-03", "絵", "クモ型・警備ロボ", "キャラ担当", "ART-01", 3, 3, 4, "未着手", "", ""),
    ("ART-04", "絵", "多連装ロボ・金属の塊（中ボス）", "キャラ担当", "ART-01", 4, 4, 4, "未着手", "", ""),
    ("ART-05", "絵", "二足メカ（ボスA）・作業ロボ", "キャラ担当", "ART-01", 5, 5, 4.5, "未着手", "", ""),
    ("ART-06", "絵", "火炎放射ロボ・送電ロボ・時限ロボ", "キャラ担当", "ART-01", 6, 6, 4.5, "未着手", "", ""),
    ("ART-07", "絵", "中枢機械（最後のボス）", "キャラ担当", "ART-01", 7, 7, 3.5, "未着手", "", "新しい絵の期限"),
    ("ART-08", "絵", "キャラの直しと統一", "キャラ担当", "", 8, 8, 1, "未着手", "", ""),
    ("UI-01", "絵", "フォントの記号の描き足し", "スキル系UI担当", "", 2, 2, 0.5, "未着手", "", ""),
    ("UI-02", "絵", "操作案内の絵（最小版 2週目、完成版 7週目）", "スキル系UI担当", "", 2, 7, 2, "未着手", "", ""),
    ("UI-03", "絵", "カードの枠・武器のアイコン", "スキル系UI担当", "", 3, 3, 3.5, "未着手", "", ""),
    ("UI-04", "絵", "強化のアイコン（半分は4週目、残りは6週目）", "スキル系UI担当", "", 4, 6, 10, "未着手", "", "アイコンの期限は6週目"),
    ("UI-05", "絵", "画面のレイアウト", "スキル系UI担当", "", 4, 4, 1.5, "未着手", "", ""),
    ("UI-06", "絵", "HUD", "スキル系UI担当", "", 5, 5, 3.5, "未着手", "", ""),
    ("UI-07", "絵", "ボタンとピクトグラム", "スキル系UI担当", "", 7, 7, 2, "未着手", "", ""),
    ("UI-08", "絵", "UI の直しと統一", "スキル系UI担当", "", 8, 8, 3, "未着手", "", ""),
    ("EX-01", "絵", "背景の飾り小物", "デザイナー（補助）", "", None, None, None, "未着手", "", "届いた時点で差し込む"),
    ("EX-02", "絵", "キービジュアル", "デザイナー（補助）", "", None, None, None, "未着手", "", "ゲームの外で使う"),
    ("EX-03", "絵", "クリア画面の一枚絵", "デザイナー（補助）", "", None, None, None, "未着手", "", "届くまでは 🏆 だけで出す"),
]


def build_tasks(b):
    widths = [8, 8, 46, 12, 14, 6, 6, 8, 10, 8, 18, 7, 7] + [4] * 8 + [30]
    s = b.sheet("作業工程", "作業工程（全タスク）", ROADMAP + "、計画1、" + WORLD, widths, has_inputs=True)
    s.para("1タスク＝1 Issue＝1ブランチ。Issue のタイトルは「[ID] タスク名」にする（例：[P2-02] 強化の仕組み）。"
           "状態の正本は GitHub の Issue と PR。C さんが毎週この表に写す。見積もりは設計書 §13 から割り振った目安。")
    s.heading("担当ごとの進み具合（数式）")
    sum_hr = s.row
    owners_rows = [[o, None, None, None, None, None] for o in OWNERS]
    s_hr, s_first, s_last = s.table(["担当", "件数", "済み", "作業中・PR確認中", "見積もりの合計", "進み具合"], [
        [o, None, None, None, None, None] for o in OWNERS
    ], center_cols=(1, 2, 3, 4, 5, 6))
    # 表の見出し（列が違うので列幅は作業工程の列に合わせて見やすい範囲だけ使う）
    total_row = s.row
    s.cell(total_row, 1, "合計", font=font(True), border=True)
    s.row += 1

    s.heading("タスク一覧")
    week_row = s.row
    s.row += 1
    headers = ["ID", "計画", "タスク", "担当", "先に要るもの", "開始週", "終了週", "見積もり", "状態", "Issue",
               "ブランチ", "開始日", "終了日"] + [w for w in range(1, 9)] + ["メモ"]
    rows = []
    for t in TASKS:
        tid, plan, name, who, dep, ws, we, est, st, issue, memo = t
        rows.append([tid, plan, name, who, dep, ws, we, est, st, issue, "", None, None] + [None] * 8 + [memo])
    hr, first, last = s.table(headers, rows, input_cols=(9, 10, 11), center_cols=tuple(range(4, 22)) ,
                              key_col=1, formats={8: "0.0#", 12: "m/d", 13: "m/d"}, freeze=False)
    s.ws.freeze_panes = s.ws.cell(row=hr + 1, column=4)
    for i in range(8):
        col = 14 + i
        c = s.ws.cell(row=hr, column=col)
        c.number_format = '"W"0'
    start = b.start_ref
    for r in range(first, last + 1):
        s.ws.cell(row=r, column=12, value='=IF(F{0}="","",{1}+(F{0}-1)*7)'.format(r, start)).number_format = "m/d"
        s.ws.cell(row=r, column=13, value='=IF(G{0}="","",{1}+(G{0}-1)*7+6)'.format(r, start)).number_format = "m/d"
        for i in range(8):
            col = 14 + i
            L = get_column_letter(col)
            s.ws.cell(row=r, column=col,
                      value='=IF(OR($F{0}="",$G{0}=""),"",IF(AND({1}${2}>=$F{0},{1}${2}<=$G{0}),"■",""))'.format(r, L, hr))
    # 今週の印
    for i in range(8):
        col = 14 + i
        L = get_column_letter(col)
        c = s.ws.cell(row=week_row, column=col,
                      value='=IF(AND(TODAY()>={0}+({1}${2}-1)*7,TODAY()<={0}+({1}${2}-1)*7+6),"▼","")'.format(start, L, hr))
        c.font = font(True, 10, "C00000")
        c.alignment = CENTER
    s.cell(week_row, 13, "今週 →", font=font(size=9, color=C_GRAY), align=Alignment(horizontal="right"))
    s.ws.auto_filter.ref = "A{}:{}{}".format(hr, get_column_letter(len(headers)), last)
    add_list_validation(s.ws, STATUS, "I{}:I{}".format(first, last + 100))
    add_list_validation(s.ws, OWNERS, "D{}:D{}".format(first, last + 100))
    bars = "N{}:U{}".format(first, last)
    s.ws.conditional_formatting.add(bars, CellIsRule(operator="equal", formula=['"■"'], fill=fill(C_BAR), font=Font(color=C_BAR)))
    s.ws.conditional_formatting.add("I{}:I{}".format(first, last), CellIsRule(operator="equal", formula=['"済み"'], fill=fill(C_DONE)))
    s.ws.conditional_formatting.add("I{}:I{}".format(first, last), CellIsRule(operator="equal", formula=['"作業中"'], fill=fill(C_DOING)))
    s.ws.conditional_formatting.add("I{}:I{}".format(first, last), CellIsRule(operator="equal", formula=['"PR確認中"'], fill=fill(C_DOING)))
    # 担当ごとの進み具合
    D = "$D${}:$D${}".format(first, last + 100)
    I = "$I${}:$I${}".format(first, last + 100)
    H = "$H${}:$H${}".format(first, last + 100)
    for r in range(s_first, s_last + 1):
        s.ws.cell(row=r, column=2, value="=COUNTIF({},A{})".format(D, r))
        s.ws.cell(row=r, column=3, value='=COUNTIFS({},A{},{},"済み")'.format(D, r, I))
        s.ws.cell(row=r, column=4, value='=COUNTIFS({0},A{1},{2},"作業中")+COUNTIFS({0},A{1},{2},"PR確認中")'.format(D, r, I))
        s.ws.cell(row=r, column=5, value="=SUMIF({},A{},{})".format(D, r, H)).number_format = "0.0#"
        s.ws.cell(row=r, column=6, value='=IF(B{0}=0,"",C{0}/B{0})'.format(r)).number_format = "0%"
    for col, f in [(2, "=SUM(B{}:B{})"), (3, "=SUM(C{}:C{})"), (4, "=SUM(D{}:D{})"), (5, "=SUM(E{}:E{})")]:
        s.cell(total_row, col, f.format(s_first, s_last), font=font(True), border=True, align=CENTER,
               fmt="0.0#" if col == 5 else "General")
    s.cell(total_row, 6, '=IF(B{0}=0,"",C{0}/B{0})'.format(total_row), font=font(True), border=True, align=CENTER, fmt="0%")


def build_cuts(b):
    s = b.sheet("削る順と足す候補", "遅れたときに削る順・手が空いたら足す候補", SPEC + " §13", [8, 64, 30])
    s.heading("遅れたときに削る順")
    s.table(["順", "削るもの", "メモ"], [
        ("1", "ネタ系の強化", "中身が決まっていないため"),
        ("2", "最後のボスの「雑魚を呼ぶ」", ""),
        ("3", "ボスAの範囲攻撃", "20〜40 は HP と速さだけ上げる"),
        ("4", "強化の一部の種類（マルチショット、エネミーバウンド、死亡時の爆発の縦と横）", ""),
        ("5", "敵の種類を減らすかどうかをチームで決める", "敵10種は仕様メモで決まったので、Claude の判断では削らない"),
    ], center_cols=(1,))
    s.para("予兆表示、チュートリアル、カードの実演アニメは「初見で分かる」の柱なので最後まで削らない。", bold=True)
    s.para("絵が間に合わないときは、仮の図形か、ほかの絵の色違いで出す。どの絵をそうするかは、デザイナーとそのプログラマーで決める。")
    s.heading("手が空いたら足す候補（上から）")
    s.table(["順", "足すもの", "必要なもの"], [
        ("1", "⚙ 設定画面（音量、画面の揺れの強さ）", "UI担当の ⚙ アイコン"),
        ("2", "手触りの仕上げ（エフェクトや音の追加）", ""),
        ("3", "🏹強化（ホームでの永続強化）", "UI担当のアイコン"),
    ], center_cols=(1,))
    s.para("絵が要るものは、絵の担当に余裕があるときだけ。")
    s.heading("テストのための準備")
    s.bullets([
        "保存データを消して初回状態に戻すデバッグ用の操作（キーの組み合わせ）",
        "好きなウェーブから始めるデバッグ用の操作（50ウェーブを毎回通しで遊ばないで済むように）",
        "好きな強化を取れるデバッグ用の操作（26枚の組み合わせと、武器3種との組み合わせを確かめるため）",
        "操作案内の絵は、3週目のテストに間に合うよう最小版を先に入れる",
        "デバッグ用の操作でも画面に文字を出さない",
    ])


def build_playtest(b):
    s = b.sheet("合格ライン", "合格ライン（毎週のプレイテスト）【提案】", SPEC + " §15",
                [11, 6, 12, 10, 10, 10, 10, 10, 30, 30, 12], has_inputs=True)
    s.heading("基準（説明を一切せずに初見の人に渡す）")
    s.table(["#", "基準", "", "", "", "", "", "", "いつから", "", ""], [
        ("1", "30秒以内に自分から動いて敵を倒せるか", "", "", "", "", "", "", "3週目"),
        ("2", "ウェーブ10のボスまで誰の助けもなく進めるか", "", "", "", "", "", "", "4週目（3週目はウェーブ5まで）"),
        ("3", "強化のカードを選んだあと、その効果を自分で言えるか", "", "", "", "", "", "", "3週目"),
        ("4", "武器を選ぶ画面で、3つの武器の違いを絵だけで想像できるか", "", "", "", "", "", "", "4週目"),
    ], center_cols=(1,))
    for r in range(s.row - 4, s.row):
        s.ws.merge_cells(start_row=r, start_column=2, end_row=r, end_column=8)
    s.bullets([
        "テスト中は観察する人も口を出さない。止まった場所と、その人が口にした言葉を記録する",
        "毎週まだ遊んだことのない人を探す。見つからない週は2回目の人で測り、「2回目」と書く",
        "観察と記録は、その週に手の空いている人が行う",
    ])
    s.heading("まとめ（数式）")
    sum_r = s.row
    s.table(["", "", "", "", "", "", "", "", "項目", "数", ""], [
        ("", "", "", "", "", "", "", "", "記録した人数（記入例を除く）", None),
        ("", "", "", "", "", "", "", "", "1 を満たした人", None),
        ("", "", "", "", "", "", "", "", "2 を満たした人", None),
        ("", "", "", "", "", "", "", "", "3 を満たした人", None),
        ("", "", "", "", "", "", "", "", "4 を満たした人", None),
    ])
    s.heading("記録（1人1行。○／× を書く。灰色の行は記入例）")
    example = ["2026/11/18", 3, "Dさん", "初回", 5, "○", "×", "○", "", "ウェーブ4の三発扇型で止まった", "「赤い線は何？」"]
    blank = [[None] * 11 for _ in range(30)]
    hr, first, last = s.table(["日付", "週", "遊んだ人", "初回／2回目", "到達ウェーブ", "基準1", "基準2", "基準3", "基準4", "止まった場所", "言葉"],
                              [example] + blank, input_cols=tuple(range(1, 12)), center_cols=(1, 2, 4, 5, 6, 7, 8, 9),
                              append=True)
    for col in range(1, 12):
        c = s.ws.cell(row=first, column=col)
        c.fill = fill(C_EXAMPLE)
        c.font = font(italic=True, color=C_GRAY)
    # 記入例を入力表から外す（引き継ぎは2行目から）
    b.inputs[-1].first = first + 1
    data = "{}:{}".format(first + 1, last)
    s.ws.cell(row=sum_r + 1, column=10, value="=COUNTA(C{}:C{})".format(first + 1, last))
    for i, col in enumerate("FGHI"):
        s.ws.cell(row=sum_r + 2 + i, column=10, value='=COUNTIF({0}{1}:{0}{2},"○")'.format(col, first + 1, last))
    add_list_validation(s.ws, ["○", "×", "—"], "F{}:I{}".format(first + 1, last))
    add_list_validation(s.ws, ["初回", "2回目"], "D{}:D{}".format(first + 1, last))


def build_open(b):
    s = b.sheet("未決定と任せること", "まだ決まっていないこと・作る人に任せること", SPEC + " §16・§18、" + WORLD + " §8・§9", [26, 40, 44])
    s.heading("まだ決まっていないこと【仮】（決まったら設計書と世界観の文書とこの仕様書を直す）")
    s.table(["項目", "今の扱い", "決めるときの注意"], [
        ("毒の属性の色と見た目", "決めない。仮の図形の色はプログラマーが選ぶ。アイコンは色が決まってから描く", "紫はレア、緑は回復と重なる"),
        ("ネタ系の強化の中身", "カードの枠だけ1つ取っておく。アイコンも描かない", "画面の見た目が変わるもの（文字の説明なしで伝わるもの）にする"),
        ("時間制限型の「即死」の読み方", "一定時間で倒さないと、プレイヤーを即死させる攻撃を出す、と読んだ", "この読み方で合っているか。敵が自分で消える意味なら敵とボスを直す"),
        ("Unity のスプラッシュ画面", "出せない場合の扱いをチームで判断する", "文字ゼロ"),
        ("どの工数の対策を採るか", "工数_絵シートの「採用」", "2週目の終わりの見直しで決める"),
    ])
    s.heading("作る人に任せること（プログラマー）")
    s.bullets([
        "数値のバランス（HP、攻撃力、経験値の必要量、ウェーブごとの敵の数と組み合わせ、武器ごとの強さ）",
        "障害物のパターンの中身（進行フローのルールの範囲内で）",
        "強化と武器の組み合わせの細かい見え方（操作と武器・強化の範囲内で）",
        "エフェクトや効果音の細かい選び方",
        "コードの構成（技術方針の範囲内で）",
    ])
    s.heading("デザイナーに任せること")
    s.bullets([
        "線の有無、色の数、陰影、頭身などの絵柄（絵の方針シートの範囲内で）",
        "各キャラと敵の細かいデザイン（役割と「ひと目で分かる理由」を守る範囲で）",
        "コマ数の増減（増やすときは工数を見直す）",
        "描く順番（プログラムで使い始める順に合わせて入れ替えてよい）",
        "カードの枠と HUD の質感",
        "どの動きを絵で描き、どの動きをプログラムで出すかの線引き（B さんと相談）",
        "工数の対策をどれにするか",
    ])


def build_rules(b):
    s = b.sheet("開発ルール", "開発ルール（要約）", RULES + "、CLAUDE.md", [20, 76, 16])
    s.para("正本は docs/development-rules.md と CLAUDE.md。ここは要約。食い違ったら正本が正しい。", bold=True)
    s.heading("命名")
    s.table(["対象", "ルール", "詳しくは"], [
        ("名前の付け方", "世界観に依存させず、役割で付ける（弾は Projectile、追う敵は Chaser）", "§2"),
        ("名前空間", "PictoArcher.Core／PictoArcher.Game／PictoArcher.Editor／PictoArcher.Tests.EditMode", "§2"),
        ("型・メソッド・プロパティ", "PascalCase（FireTimer、Tick、Interval）", "§2"),
        ("private のフィールド", "_camelCase（_elapsed）。private は書かない（既定のまま）。static readonly は camelCase", "§2"),
        ("ScriptableObject の調整値", "public のフィールドで camelCase（maxHp、moveSpeed）", "§2"),
        ("テスト", "<クラス名>Tests.cs。メソッドは PascalCase、条件と結果を _ で区切ってよい（NoTarget_DoesNotFire）", "§2"),
        ("アセット", "プレハブは役割の名前（Player、Projectile、Chaser）。データは種類ごとのフォルダ（Data/Enemies、Data/Waves）", "§2"),
        ("ブランチ", "<担当>/t<Issue番号>-<短い英語>（例 a/t12-weapon-select）。文書だけなら <担当>/docs-<名前>", "§5"),
        ("Issue", "[作業工程の ID] タスク名（例 [P2-02] 強化の仕組み）", "§4"),
        ("コミット", "feat: / fix: / test: / docs: / chore: ＋ 英語の短い説明", "§6"),
    ])
    s.heading("構成（Unity の標準に合わせる）")
    s.bullets([
        "自分たちのものはすべて Assets/_Project の下。外部の素材は Assets/_Project/Art（取り込む前に文字がないか確かめる）",
        "ルールは Core（純粋な C#、テストする）、つなぎは Game（MonoBehaviour）、データは ScriptableObject",
        "数値はコードに書かず ScriptableObject の調整値にする",
        "Run.unity は C さんだけ。ほかの人はプレハブと Scenes/Sandbox/Sandbox_<担当>.unity",
        "Core のルールは先に EditMode テストを書いて失敗を確かめてから作る",
    ])
    s.heading("Issue・ブランチ・PR・マージ")
    s.table(["段階", "やること", "詳しくは"], [
        ("始める前", "gh で開いている Issue・PR・ブランチを見て、同じ作業がないか確かめる（Claude利用チェックリスト）", "§3"),
        ("Issue", "1タスク＝1 Issue。始める前に自分を担当（assignee）にする。ないときは Issue を作ってから始める", "§4"),
        ("ブランチ", "main から切る。1タスク＝1ブランチ。main に直接コミットしない", "§5"),
        ("下書きの PR", "ブランチを作ったらすぐ下書きの PR を開き、本文に Closes #<Issue> と触るファイルを書く", "§7"),
        ("PR を出す", "全テスト（NoTextGuardTests を含む）が通ってから「レビュー待ち」にする。PR テンプレートの項目を埋める", "§7"),
        ("レビュー", "ほかの1人が読む。Claude が書いたコードも人が読む", "§8"),
        ("マージ", "レビューで OK が出たら、書いた本人がマージする。マージのあとブランチを消す", "§8"),
        ("仕様の変更", "設計書（.md）と tools/spec/build_spec_xlsx.py を同じ PR で直し、.xlsx を作り直す。編集は C さん", "§10"),
    ])


CHECKLIST = [
    ("始める前", "main を最新にした", "git switch main && git pull"),
    ("始める前", "開いている Issue を見て、同じ作業がないか確かめた", "gh issue list --state open"),
    ("始める前", "開いている PR（下書きを含む）を見て、同じファイルを触る PR がないか確かめた", "gh pr list --state open"),
    ("始める前", "同じ作業のブランチがないか確かめた", "git fetch --prune && git branch -r"),
    ("始める前", "自分のタスクの Issue があり、自分が担当になっている（なければ作って自分を担当にした）", "gh issue edit <番号> --add-assignee @me"),
    ("始める前", "Issue のタイトルが「[作業工程の ID] タスク名」になっている", "仕様書の作業工程シート"),
    ("始める前", "先に要るタスクがマージ済み", "作業工程シートの「先に要るもの」"),
    ("始める前", "ブランチ名が <担当>/t<Issue番号>-<短い名前>", "git switch -c a/t12-weapon-select"),
    ("始める前", "下書きの PR を開き、Closes #<Issue> と触るファイルを書いた", "gh pr create --draft"),
    ("始める前", "Claude に CLAUDE.md と自分の Issue を読ませ、担当外の作業をしないよう伝えた", "CLAUDE.md「作業を始める前に」"),
    ("作業中", "1セッション＝1タスク。別のタスクに移るときは /clear してから", ""),
    ("作業中", "計画が要る作業は Plan Mode で計画を作り、自分で読んでから進めた", "Shift+Tab で Plan Mode"),
    ("作業中", "サブエージェントは2〜3体まで（log-triage、transcriber、task-reviewer）", ".claude/agents"),
    ("作業中", "担当外のファイル（ほかの人のプレハブ、Run.unity など）を触っていない。直したいときはその Issue にコメントした", ""),
    ("作業中", "仕様にないことを決めたら Issue に書いた（仕様書の更新は C さんに頼む）", ""),
    ("作業中", "Core は先にテストを書いて失敗を確かめてから作った", "tools/run-editmode-tests.sh"),
    ("作業中", "数値をコードに直接書いていない（ScriptableObject の調整値にした）", ""),
    ("PR を出す前", "全テストが通った（NoTextGuardTests を含む）", "tools/run-editmode-tests.sh"),
    ("PR を出す前", "main を取り込んで、衝突がない", "git merge origin/main"),
    ("PR を出す前", "Claude が書いたコードを自分で全部読んだ", ""),
    ("PR を出す前", "画面と素材に文字がない。新しい素材は docs/assets-list.md に書いた", ""),
    ("PR を出す前", "PR テンプレートの項目を埋め、レビューする人を1人指定した", "gh pr ready"),
    ("マージのあと", "Issue が閉じた", "gh issue view <番号>"),
    ("マージのあと", "ブランチを消した", "gh pr merge --delete-branch"),
    ("マージのあと", "かかった日数を Issue にコメントした（C さんが工数の実績に写す）", ""),
]


def build_checklist(b):
    s = b.sheet("Claude利用チェックリスト", "Claude を使うときのチェックリスト（重複作業の防止）", RULES + " §3、CLAUDE.md",
                [6, 14, 60, 40, 8], has_inputs=True)
    s.para("3人とも Claude を使うので、同じタスクを2人（2つの Claude）が始めてしまう、同じファイルを別々に直す、ということが起きやすい。"
           "タスクを始めるたびにこの表を上から確かめる。同じ項目が PR テンプレートにもある。✓ 欄はその都度使い、作り直すと消える。")
    hr, first, last = s.table(["#", "時点", "確認すること", "コマンド・場所", "✓"],
                              [[i + 1, w, t, c, None] for i, (w, t, c) in enumerate(CHECKLIST)],
                              input_cols=(5,), center_cols=(1, 2, 5))
    add_list_validation(s.ws, ["✓"], "E{}:E{}".format(first, last))
    r = s.row
    s.cell(r, 3, "確かめた数", font=font(True), border=True)
    s.cell(r, 5, '=COUNTIF(E{}:E{},"✓")&"/"&COUNTA(A{}:A{})'.format(first, last, first, last), font=font(True), border=True, align=CENTER)
    s.row += 1
    s.heading("同じ作業が重なったとき")
    s.bullets([
        "先に Issue の担当になった人が続ける。あとから気づいた人は手を止め、Issue にコメントして別のタスクに移る",
        "途中までのコードが役に立つなら、その差分を Issue に貼るか、相手のブランチに PR を出す（main には入れない）",
        "同じファイルを2人が直す必要があるときは、先に Issue で順番を決める。プレハブとシーンは同時に直さない",
    ])
    s.heading("Claude の動かし方（ハイブリッド運用）")
    s.table(["", "役", "使い方", "モデル", ""], [
        ("1", "メインのセッション", "1タスクの計画、判断、統合。Plan Mode で計画し、タスクが終わったら /clear", "既定", ""),
        ("2", "log-triage", "長いログやテスト結果（Logs/editmode-results.xml）から、失敗と原因だけを抜き出す", "haiku", ""),
        ("3", "transcriber", "計画にコードが全部書いてあるタスク（Core の小さな部品）を写して、テストして、コミットする", "haiku", ""),
        ("4", "task-reviewer", "1タスクが終わるごとに、仕様に合っているかとコードの質を見る", "sonnet", ""),
    ], center_cols=(1, 4))
    s.para("サブエージェントは同時に2〜3体まで。同じファイルを触るサブエージェントを同時に動かさない。")


def build_history(b):
    s = b.sheet("変更履歴", "変更履歴", None, [10, 14, 70, 16])
    s.table(["版", "日付", "変更", "書いた人"], [
        ("1.0", "2026-11-12", "初版。設計書 第3版、世界観 確定版、ロードマップ 第3版、計画1 を1冊にまとめ、スケジュール・作業工程・工数・チェックリストを足した", "C（Claude で作成）"),
        (VERSION, BUILT, "ドット絵はキャラだけ（32×32）にし、カード・アイコン・ボタンは普通の2Dの UI の絵にした。カードは縦長。のちの iPad・iPhone への対応に備えて 9:16 で作ることと、UI の置き方（Canvas Scaler、画面の端を基準）を足した。ドット絵シートを絵の方針シートにした", "C（Claude で作成）"),
    ])


# ---------------------------------------------------------------- 引き継ぎ


def read_previous(path):
    """前の .xlsx から黄色のセルの値を読む。{(シート, 鍵, 列): 値} と {シート: [行の値]}"""
    if not os.path.exists(path):
        return {}
    try:
        wb = load_workbook(path)
    except Exception as e:  # 壊れていたら引き継がない
        print("前の .xlsx を読めなかったので、書き込みは引き継がない: {}".format(e), file=sys.stderr)
        return {}
    return wb


def carry_over(book, old_wb):
    if not old_wb:
        return 0
    count = 0
    # スケジュールの開始日などの単独の入力セル
    for sheet, ref in getattr(book, "input_cells", []):
        if sheet in old_wb.sheetnames:
            v = old_wb[sheet][ref].value
            if v is not None:
                book.wb[sheet][ref].value = v
                count += 1
    for t in book.inputs:
        if t.sheet not in old_wb.sheetnames:
            continue
        old = old_wb[t.sheet]
        new = book.wb[t.sheet]
        headers = {new.cell(row=t.header_row, column=c).value: c for c in t.input_cols}
        # 前の .xlsx で、同じ見出しの行を探す
        old_hr = None
        key_header = new.cell(row=t.header_row, column=t.key_col).value if t.key_col else None
        probe = list(headers)[0]
        for r in range(1, old.max_row + 1):
            vals = [old.cell(row=r, column=c).value for c in range(1, old.max_column + 1)]
            if probe in vals and (key_header is None or key_header in vals):
                old_hr = r
                old_cols = {v: i + 1 for i, v in enumerate(vals) if v is not None}
                break
        if old_hr is None:
            continue
        if t.append:
            # 記入例の次の行から順に写す
            dst = t.first
            for r in range(old_hr + 2, old.max_row + 1):
                vals = {h: old.cell(row=r, column=old_cols[h]).value for h in headers if h in old_cols}
                if all(v is None for v in vals.values()):
                    continue
                if dst > t.last:
                    break
                for h, c in headers.items():
                    if h in vals and vals[h] is not None:
                        new.cell(row=dst, column=c).value = vals[h]
                        count += 1
                dst += 1
            continue
        old_key_col = old_cols.get(key_header)
        if old_key_col is None:
            continue
        old_rows = {}
        r = old_hr + 1
        while r <= old.max_row:
            k = old.cell(row=r, column=old_key_col).value
            if k is not None:
                old_rows.setdefault(k, r)
            r += 1
        for r in range(t.first, t.last + 1):
            k = new.cell(row=r, column=t.key_col).value
            if k not in old_rows:
                continue
            for h, c in headers.items():
                if h not in old_cols:
                    continue
                v = old.cell(row=old_rows[k], column=old_cols[h]).value
                if v is not None and v != new.cell(row=r, column=c).value:
                    new.cell(row=r, column=c).value = v
                    count += 1
    return count


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--fresh", action="store_true", help="黄色のセルの書き込みを引き継がない")
    parser.add_argument("--out", default=OUT)
    args = parser.parse_args()

    b = Book()
    build_cover(b)
    build_overview(b)
    build_controls(b)
    build_no_text(b)
    build_flow(b)
    build_waves(b)
    build_tutorial(b)
    build_enemies(b)
    build_skills(b)
    build_screens(b)
    build_layouts(b)
    build_art(b)
    build_world(b)
    build_tech(b)
    build_roles(b)
    build_prog_effort(b)
    build_art_effort(b)
    build_schedule(b)
    build_tasks(b)
    build_cuts(b)
    build_playtest(b)
    build_open(b)
    build_rules(b)
    build_checklist(b)
    build_history(b)

    names = [s.name for s in b.sheets]
    missing = [n for n, _, _ in SHEET_INDEX if n not in names]
    if missing:
        raise SystemExit("表紙の一覧にあるのに作っていないシート: {}".format(missing))

    for ws in b.wb.worksheets:
        ws.sheet_properties.pageSetUpPr.fitToPage = True
        ws.page_setup.orientation = "landscape"
        ws.page_setup.fitToWidth = 1
        ws.page_setup.fitToHeight = 0
    b.wb.calculation.fullCalcOnLoad = True

    carried = 0
    if not args.fresh:
        carried = carry_over(b, read_previous(args.out))
    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    b.wb.save(args.out)
    print("書き出した: {}（シート {} 枚、引き継いだ値 {} 個）".format(os.path.relpath(args.out, ROOT), len(names), carried))


if __name__ == "__main__":
    main()
