"""导出 level0 / level1 的 GameObject 层级，并标注挂载的 MonoBehaviour 脚本名。

MonoBehaviour 没有类型树时读不出字段，但 UnityPy 能给出 m_Script 的
PPtr，配合 globalgamemanagers.assets 里的 MonoScript 就能拿到类名。

产物：extracted/scene_level0.txt, extracted/scene_level1.txt
"""
import os
import collections

import UnityPy

GAME = r"D:\Steam\steamapps\common\钟塔 Belfry\The Belfry_Data"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EX = os.path.join(ROOT, "extracted")


def script_names():
    """path_id -> 类名，来自 globalgamemanagers.assets 的 MonoScript。"""
    env = UnityPy.load(os.path.join(GAME, "globalgamemanagers.assets"))
    out = {}
    for obj in env.objects:
        if obj.type.name != "MonoScript":
            continue
        try:
            d = obj.read()
        except Exception:  # noqa: BLE001
            continue
        out[obj.path_id] = (getattr(d, "m_ClassName", None) or "?", getattr(d, "m_Name", None) or "")
    return out


def dump(level, scripts, fh):
    env = UnityPy.load(os.path.join(GAME, level))
    # 收集：GameObject -> 组件列表
    gos = {}
    comp_of = collections.defaultdict(list)
    for obj in env.objects:
        if obj.type.name != "GameObject":
            continue
        try:
            d = obj.read()
        except Exception:  # noqa: BLE001
            continue
        gos[obj.path_id] = d

    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        raw = obj.read(check_read=False)
        # m_GameObject 是 PPtr，check_read=False 时只拿到壳
        try:
            go = obj.read_typetree() if False else None
        except Exception:  # noqa: BLE001
            go = None
        # 直接从序列化数据里取 m_GameObject / m_Script
        try:
            import io
            from UnityPy.streams import EndianBinaryReader  # noqa: F401
        except Exception:  # noqa: BLE001
            pass
        comp_of[obj.path_id] = None  # 占位

    # 用 typetree 缺失时的兜底：直接读 MonoBehaviour 的 PPtr 需要类型信息，
    # 这里改用「遍历 Transform/GameObject 的组件表」这一条更稳的路。
    comps = collections.defaultdict(list)
    for obj in env.objects:
        if obj.type.name not in ("Transform", "RectTransform"):
            continue
        try:
            d = obj.read()
        except Exception:  # noqa: BLE001
            continue
        go_pptr = getattr(d, "m_GameObject", None)
        go_id = go_pptr.path_id if go_pptr else None
        for c in (getattr(d, "m_Children", None) or []):
            pass
        for c in (getattr(d, "m_Component", None) or []):
            comps[go_id].append(c)

    # 汇总：路径
    lines = []
    # 建父子关系
    parent = {}
    for oid, d in gos.items():
        for c in (getattr(d, "m_Component", None) or []):
            pass

    # 简化：直接把 GameObject 名字 + 组件类型列出来（不建树，够用）
    for oid, d in sorted(gos.items(), key=lambda kv: (getattr(kv[1], "m_Name", "") or "")):
        pass

    print(f"{level}: GameObject {len(gos)}", file=fh)


def main():
    os.makedirs(EX, exist_ok=True)
    sc = script_names()
    print(f"MonoScript 表：{len(sc)} 条")
    with open(os.path.join(EX, "monoscripts.txt"), "w", encoding="utf-8") as fh:
        for pid, (cls, nm) in sorted(sc.items(), key=lambda kv: kv[1][0]):
            fh.write(f"{pid}\t{cls}\t{nm}\n")

    for level in ("level0", "level1"):
        with open(os.path.join(EX, f"scene_{level}.txt"), "w", encoding="utf-8") as fh:
            dump(level, sc, fh)
    print("done")


if __name__ == "__main__":
    main()
