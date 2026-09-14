"""把前两个仓库里与游戏无关的基础设施移植到 BelfryA11y。

只做一件事：换命名空间。文本编码显式按 UTF-8 处理 —— 这些源文件是
无 BOM 的 UTF-8，用 PowerShell 5.1 的文本 cmdlet 读会按 GBK 解码而变乱码。
"""
import os

SRC_TH = r"D:\DSHWorkBase\transparenther_a11y\mod\src\TransparentHerA11y"
SRC_NE = r"D:\DSHWorkBase\noexistence_a11y\mod\src\NoExistenceA11y"
DST = r"D:\DSHWorkBase\belfry_a11y\mod\src\BelfryA11y"

# (源目录, 文件名)
FILES = [
    (SRC_TH, "Speech.cs"),
    (SRC_TH, "Nvda.cs"),
    (SRC_TH, "Sapi.cs"),
    (SRC_TH, "UiNav.cs"),
    (SRC_NE, "TextProc.cs"),
]


def main():
    os.makedirs(DST, exist_ok=True)
    for src, name in FILES:
        p = os.path.join(src, name)
        with open(p, encoding="utf-8") as fh:
            text = fh.read()
        text = text.replace("namespace TransparentHerA11y", "namespace BelfryA11y")
        text = text.replace("namespace NoExistenceA11y", "namespace BelfryA11y")
        out = os.path.join(DST, name)
        with open(out, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
        print(f"{name:<12} {len(text):>7} 字符  -> {out}")


if __name__ == "__main__":
    main()
