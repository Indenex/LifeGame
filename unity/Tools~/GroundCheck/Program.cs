// unity-1: checks Runtime/GroundBuilder.cs (built from shared/) against the ground mesh the
// Godot client exported. Run: dotnet run -- ../../Assets/StreamingAssets/spike/noon
using System; using System.IO; using System.Reflection; using System.Collections.Generic; using System.Text.Json;
// Compares GroundBuilder's heights and colours with the ground mesh Godot exported.
var t = typeof(LifeGame.Spike.GroundBuilder);
var tileColor = t.GetMethod("TileColor", BindingFlags.NonPublic | BindingFlags.Static);
var under = t.GetMethod("UnderGrass", BindingFlags.NonPublic | BindingFlags.Static);
string dir = args[0];
var scene = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "scene.json")));
var mats = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "materials.json")));
var ground = new HashSet<int>();
foreach (var m in mats.RootElement.EnumerateArray())
  if (m.TryGetProperty("params", out var p) && p.TryGetProperty("puddles_ok", out var po) && po.ValueKind == JsonValueKind.Number && po.GetDouble() > 0.5) ground.Add(m.GetProperty("id").GetInt32());
int checkedV = 0, meshes = 0; double maxH = 0, maxC = 0;
foreach (var d in scene.RootElement.GetProperty("draws").EnumerateArray()) {
  bool isGround = d.TryGetProperty("material", out var mo) && ground.Contains(mo.GetInt32());
  foreach (var s in d.GetProperty("surface_materials").EnumerateArray()) if (ground.Contains(s.GetInt32())) isGround = true;
  if (!isGround || meshes >= 12) continue;
  meshes++;
  using var r = new BinaryReader(File.OpenRead(Path.Combine(dir, "meshes", $"mesh_{d.GetProperty("mesh").GetInt32():D4}.bin")));
  r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); int nv = r.ReadInt32(); r.ReadInt32(); int flags = r.ReadInt32();
  var pos = new float[nv * 3]; for (int i = 0; i < pos.Length; i++) pos[i] = r.ReadSingle();
  if ((flags & 1) != 0) r.BaseStream.Seek(nv * 12L, SeekOrigin.Current);
  var col = new float[nv * 4]; for (int i = 0; i < col.Length; i++) col[i] = r.ReadSingle();
  for (int v = 0; v < nv; v += 7) {
    int x = (int)MathF.Round(pos[v * 3]), z = (int)MathF.Round(pos[v * 3 + 2]);
    float h = LifeGame.Shared.World.BaseHeight(x, z);
    maxH = Math.Max(maxH, Math.Abs(h - pos[v * 3 + 1]));
    var c = (UnityEngine.Color)under.Invoke(null, new object[] { x, z, h, tileColor.Invoke(null, new object[] { x, z, h }) });
    maxC = Math.Max(maxC, Math.Max(Math.Abs(c.r - col[v * 4]), Math.Max(Math.Abs(c.g - col[v * 4 + 1]), Math.Abs(c.b - col[v * 4 + 2]))));
    checkedV++;
  }
}
Console.WriteLine($"ground meshes {meshes}, vertices checked {checkedV}, max height diff {maxH:G3} m, max colour diff {maxC:G3}");
