#!/usr/bin/env python3
"""Checks the spike's URP shaders compile, without Unity (unity-1).

Pulls the HLSLINCLUDE and HLSLPROGRAM blocks out of each .shader, and compiles every
pass's vertex and fragment entry points with Microsoft's DXC against URP 17.3's
shader library, for a few keyword sets. Unity compiles the same HLSL with its own
front end (also DXC/FXC based), so this catches typos, missing functions and type
errors before Justin opens the project.

  python3 -I unity/Tools~/hlsl_check.py [--dxc /root/dxc] [--packages /root/hlslroot]

--packages must hold Packages/com.unity.render-pipelines.{core,universal,universal-config}
(symlinks into the Unity editor's BuiltInPackages are fine).
"""
import argparse
import itertools
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
SHADERS = os.path.join(HERE, "..", "Assets", "LifeGame", "Shaders")

# Roughly what Unity defines when it compiles a URP shader for D3D11 / Vulkan.
BASE_DEFINES = [
    "SHADER_API_D3D11", "SHADER_TARGET=45", "UNITY_VERSION=60030", "UNITY_COLORSPACE_GAMMA=0",
    "UNITY_REVERSED_Z=1", "UNITY_UV_STARTS_AT_TOP=1", "UNITY_NEAR_CLIP_VALUE=1",
    "SHADER_API_DESKTOP",
    # UnityInstancing.hlsl pastes a macro name (arr##Array) that Unity's own preprocessor
    # resolves; plain DXC needs the pasted name spelled out.
    "UNITY_BUILTINS_WITH_WORLDTOOBJECTARRAYArray=unity_Builtins0Array",
]

KEYWORD_SETS = [
    [],
    ["_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT", "_SCREEN_SPACE_OCCLUSION", "_ADDITIONAL_LIGHTS", "FOG_EXP", "INSTANCING_ON"],
    ["_MAIN_LIGHT_SHADOWS", "_PAINTED_FOLIAGE", "_UNITY_PBR", "FOG_EXP", "_CASTING_PUNCTUAL_LIGHT_SHADOW"],
    ["_MAIN_LIGHT_SHADOWS_SCREEN", "_PAINTED_FOLIAGE", "INSTANCING_ON", "_SHADOWS_SOFT_HIGH"],
]


def blocks(text, start, end="ENDHLSL"):
    out, i = [], 0
    while True:
        a = text.find(start, i)
        if a < 0:
            return out
        b = text.find(end, a)
        out.append(text[a + len(start):b])
        i = b + len(end)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dxc", default="/root/dxc")
    ap.add_argument("--packages", default="/root/hlslroot")
    args = ap.parse_args()
    dxc = os.path.join(args.dxc, "bin", "dxc")
    env = dict(os.environ, LD_LIBRARY_PATH=os.path.join(args.dxc, "lib"))
    failures = 0
    runs = 0
    for name in sorted(os.listdir(SHADERS)):
        if not name.endswith(".shader"):
            continue
        text = open(os.path.join(SHADERS, name)).read()
        include = "\n".join(blocks(text, "HLSLINCLUDE"))
        for p, prog in enumerate(blocks(text, "HLSLPROGRAM")):
            vs = re.search(r"#pragma\s+vertex\s+(\w+)", prog).group(1)
            ps = re.search(r"#pragma\s+fragment\s+(\w+)", prog).group(1)
            src = include + "\n" + prog
            with tempfile.NamedTemporaryFile("w", suffix=".hlsl", dir=SHADERS, delete=False) as f:
                f.write(src)
                path = f.name
            try:
                for keys in KEYWORD_SETS:
                    for stage, entry, profile in (("VERTEX", vs, "vs_6_0"), ("FRAGMENT", ps, "ps_6_0")):
                        cmd = [dxc, "-T", profile, "-E", entry, "-HV", "2018", "-Wno-ignored-attributes",
                               "-I", args.packages, "-I", SHADERS, path, "-Fo", os.devnull]
                        for d in BASE_DEFINES + keys + [f"SHADER_STAGE_{stage}"]:
                            cmd += ["-D", d]
                        r = subprocess.run(cmd, capture_output=True, text=True, env=env)
                        runs += 1
                        if r.returncode != 0:
                            failures += 1
                            errs = [l for l in r.stderr.splitlines() if "error" in l][:6]
                            print(f"FAIL {name} pass {p} {entry} {profile} {' '.join(keys)}")
                            for l in errs:
                                print("   ", l.replace(path, name + ".hlsl"))
            finally:
                os.unlink(path)
    print(f"{runs - failures}/{runs} compiles passed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
