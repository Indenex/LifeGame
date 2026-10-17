#!/usr/bin/env bash
# Compiles unity/Assets/LifeGame/**/*.cs against the real Unity 6.3 + URP 17.3 APIs
# without a Unity licence (unity-1): Roslyn builds our scripts together with URP,
# Core, Mathematics, Collections and Burst sources from the Linux editor install and
# reports only the errors in our files. A probe error confirms method bodies were bound.
#   unity/Tools~/compile_check.sh [unity editor dir, default ~/unity-editor]
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
ed=${1:-$HOME/unity-editor}
B=$ed/Editor/Data/Resources/PackageManager/BuiltInPackages
P=$ed/Editor/Data/Resources/PackageManager/Editor
M=$ed/Editor/Data/Managed/UnityEngine
w=${TMPDIR:-/tmp}/unity-spike-cc
export PATH="$HOME/.dotnet:$PATH"
mkdir -p "$w/pkgs"
for p in com.unity.mathematics-1.3.3 com.unity.burst-1.8.30 com.unity.collections-2.6.8; do
  [ -d "$w/pkgs/$p" ] || { mkdir -p "$w/pkgs/$p" && tar xzf "$P/$p.tgz" -C "$w/pkgs/$p"; }
done
# URP declares three overrides 'protected' where the engine base is 'protected internal'
# (fine inside Unity, an error for a plain compile): patch a scratch copy.
if [ ! -d "$w/urp" ]; then
  mkdir -p "$w/urp" && cp -r "$B/com.unity.render-pipelines.universal/Runtime" "$w/urp/Runtime"
  sed -i 's/protected override bool requiresCompatibleRenderPipelineGlobalSettings/protected internal override bool requiresCompatibleRenderPipelineGlobalSettings/' "$w/urp/Runtime/Data/UniversalRenderPipelineAsset.cs"
  sed -i 's/protected override bool IsRenderRequestSupported/protected internal override bool IsRenderRequestSupported/; s/protected override bool IsPreviewSupported/protected internal override bool IsPreviewSupported/' "$w/urp/Runtime/UniversalRenderPipeline.cs"
fi
removes=""
for f in $(grep -rlE "using UnityEngine.UI;|DebugUIHandler|UnityEngine.EventSystems" "$w/urp/Runtime" "$B/com.unity.render-pipelines.core/Runtime"); do
  removes="$removes<Compile Remove=\"$f\" />"
done
cat > "$w/Stubs.cs" <<'CS'
namespace UnityEngine.Rendering { public interface IDebugData { System.Action GetReset(); } }
CS
echo 'class Probe { void F() { UnityEngine.Rendering.Universal.Bloom b = null; b.probeShouldFail(); } }' > "$w/Probe.cs"
defs="UNITY_EDITOR;UNITY_EDITOR_LINUX;UNITY_STANDALONE;UNITY_64;ENABLE_PROFILER;ENABLE_LEGACY_INPUT_MANAGER;ENABLE_UNITY_COLLECTIONS_CHECKS"
for v in 6000_0 6000_1 6000_2 6000_3 2023_3 2023_2 2023_1 2022_3 2022_2 2022_1 2021_3 2021_2 2021_1 2020_3 2020_2 2020_1 2019_4 2019_3; do defs="$defs;UNITY_${v}_OR_NEWER"; done
cat > "$w/Check.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <!-- UnityEngine.CoreModule lets this assembly name see the internals URP's GPU-driven code uses. -->
    <AssemblyName>Unity.RenderPipelines.GPUDriven.Runtime</AssemblyName>
    <NoWarn>\$(NoWarn);CS0618;CS0612;CS0414;CS0649;CS0169;CS0067;CS1998;CS0162;CS8632;CS0219;CS0168;CS0108;CS0114;CS0109</NoWarn>
    <DefineConstants>$defs</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$here/../Assets/LifeGame/**/*.cs" />
    <Compile Include="Probe.cs;Stubs.cs" />
    <Compile Include="urp/Runtime/**/*.cs" />
    <Compile Include="$B/com.unity.render-pipelines.core/Runtime/**/*.cs" />
    <Compile Include="$B/com.unity.render-pipelines.universal/ShaderLibrary/**/*.cs" />
    <Compile Include="$B/com.unity.render-pipelines.core/ShaderLibrary/**/*.cs" />
    <Compile Include="$B/com.unity.render-pipelines.universal-config/Runtime/**/*.cs" />
    <Compile Include="pkgs/com.unity.mathematics-1.3.3/package/Unity.Mathematics/**/*.cs" />
    <Compile Include="pkgs/com.unity.burst-1.8.30/package/Runtime/**/*.cs" />
    <Compile Include="pkgs/com.unity.collections-2.6.8/package/Unity.Collections/**/*.cs" />
    <Compile Remove="**/Tests/**;pkgs/com.unity.burst-1.8.30/package/Runtime/Editor/**" />
    $removes
    <Reference Include="$M/*.dll" />
    <Reference Include="$here/../Assets/LifeGame/Plugins/LifeGame.Shared.dll" />
  </ItemGroup>
</Project>
XML
out=$(cd "$w" && dotnet build 2>&1 || true)
grep -q "probeShouldFail" <<<"$out" || { echo "compile check did not reach method bodies; see: cd $w && dotnet build"; exit 2; }
mine=$(grep -E "error|warning" <<<"$out" | grep "Assets/LifeGame" | sed 's|\[/.*||' | sort -u || true)
if [ -n "$mine" ]; then echo "$mine"; exit 1; fi
echo "compile check: no errors or warnings in unity/Assets/LifeGame"
