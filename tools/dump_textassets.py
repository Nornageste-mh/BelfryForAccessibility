"""导出《钟塔 Belfry》resources.assets 中的全部 TextAsset。

产物写到 extracted/ 目录（游戏版权内容，不入库）。
"""
import os
import sys

import UnityPy

GAME = r"D:\Steam\steamapps\common\钟塔 Belfry\The Belfry_Data"
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "extracted")

TARGETS = [
    os.path.join(GAME, "resources.assets"),
    os.path.join(GAME, "sharedassets0.assets"),
    os.path.join(GAME, "sharedassets1.assets"),
    os.path.join(GAME, "globalgamemanagers.assets"),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    for path in TARGETS:
        if not os.path.exists(path):
            print(f"跳过（不存在）: {path}")
            continue
        env = UnityPy.load(path)
        n = 0
        for obj in env.objects:
            if obj.type.name != "TextAsset":
                continue
            d = obj.read()
            name = getattr(d, "m_Name", None) or f"unnamed_{obj.path_id}"
            script = getattr(d, "m_Script", None)
            if script is None:
                raw = getattr(d, "m_RawData", None)
                if raw:
                    script = bytes(raw)
            if isinstance(script, str):
                # UnityPy 用 surrogateescape 解码，二进制资源（如 spine 的 .skel）
                # 必须按同样方式编回去，否则会抛 UnicodeEncodeError
                data = script.encode("utf-8", "surrogateescape")
            elif isinstance(script, (bytes, bytearray)):
                data = bytes(script)
            else:
                print(f"  !! {name}: 未知内容类型 {type(script)}")
                continue
            safe = name.replace("/", "_").replace("\\", "_")
            with open(os.path.join(OUT, f"textasset_{safe}.txt"), "wb") as fh:
                fh.write(data)
            print(f"  {name:<40} {len(data):>10} 字节")
            n += 1
        print(f"{os.path.basename(path)}: 导出 {n} 个 TextAsset")


if __name__ == "__main__":
    main()
