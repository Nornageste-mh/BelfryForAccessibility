"""扫描《钟塔 Belfry》的 Unity 资源文件，统计对象类型与数量。

用法: python tools/scan_assets.py [路径...]
不带参数时扫描默认的游戏数据目录。
"""
import os
import sys
import collections

import UnityPy

GAME = r"D:\Steam\steamapps\common\钟塔 Belfry\The Belfry_Data"

DEFAULT_TARGETS = [
    os.path.join(GAME, "globalgamemanagers"),
    os.path.join(GAME, "globalgamemanagers.assets"),
    os.path.join(GAME, "level0"),
    os.path.join(GAME, "level1"),
    os.path.join(GAME, "resources.assets"),
    os.path.join(GAME, "sharedassets0.assets"),
    os.path.join(GAME, "sharedassets1.assets"),
]


def scan(path):
    print(f"\n{'=' * 70}\n{path}")
    if not os.path.exists(path):
        print("  (不存在)")
        return
    try:
        env = UnityPy.load(path)
    except Exception as exc:  # noqa: BLE001
        print(f"  加载失败: {exc!r}")
        return

    counts = collections.Counter()
    names = collections.defaultdict(list)
    for obj in env.objects:
        t = obj.type.name
        counts[t] += 1
        if len(names[t]) < 40:
            try:
                d = obj.read()
                nm = getattr(d, "m_Name", None)
                if nm:
                    names[t].append(nm)
            except Exception:  # noqa: BLE001
                pass

    for t, c in counts.most_common():
        print(f"  {t:<28} {c}")
    for t in ("TextAsset", "MonoBehaviour", "GameObject", "ScriptableObject"):
        if names[t]:
            print(f"  -- {t} 名称样例: {names[t][:40]}")


if __name__ == "__main__":
    targets = sys.argv[1:] or DEFAULT_TARGETS
    for t in targets:
        scan(t)
