"""统计《钟塔 Belfry》五张剧本表的结构。

产物：extracted/script_stats.txt
"""
import json
import os
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EX = os.path.join(ROOT, "extracted")
OUT = os.path.join(EX, "script_stats.txt")

TABLES = [f"TheBelfrySchedule{i}" for i in range(1, 6)]


def load(name):
    p = os.path.join(EX, f"textasset_{name}.txt")
    with open(p, encoding="utf-8") as fh:
        return json.load(fh)


def main():
    lines = []
    w = lines.append
    grand = collections.Counter()
    total = 0
    voiced = 0
    for t in TABLES:
        data = load(t)
        types = collections.Counter(d.get("TypeName", "") for d in data)
        chars = collections.Counter(d.get("CharacterName", "") for d in data)
        v = sum(1 for d in data if (d.get("VoiceFilename") or "").strip())
        dlg = sum(1 for d in data if (d.get("Dialogue") or "").strip())
        cho = sum(1 for d in data if (d.get("ChoiceText") or "").strip())
        w(f"=== {t} ===")
        w(f"  总行数 {len(data)}  有 Dialogue {dlg}  有 ChoiceText {cho}  有配音 {v}")
        w(f"  TypeName: {dict(types)}")
        top = ", ".join(f"{k or '(空)'}={n}" for k, n in chars.most_common(20))
        w(f"  角色名 Top20: {top}")
        w("")
        grand.update(types)
        total += len(data)
        voiced += v
    w(f"总计：{total} 行，有配音 {voiced} 行 ({voiced / total * 100:.1f}%)")
    w(f"TypeName 汇总：{dict(grand)}")
    with open(OUT, "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines))
    print(f"written {OUT}")


if __name__ == "__main__":
    main()
