using System.Collections.Generic;
using System.IO;
using LifeGame.Shared;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LifeGame.Spike
{
    // Builds the Godot spawn scene in Unity and lights it the way the Godot export
    // says (unity-1). Press Play in Assets/LifeGame/Spike.unity.
    //
    //   1, 2, ...  switch preset (noon, dusk: the folders in StreamingAssets/spike)
    //   G          Godot grade (Godot's tonemap + adjustments in a LUT) / Unity's own ACES grade
    //   L          the hand-written Godot light model / URP's stock PBR lighting
    //   P or F12   save a screenshot to renders/unity-spike-<preset>-<mode>.png
    //   H          hide this help
    //   right-drag + WASD/QE: look and fly; R: back to the Godot camera
    public sealed class SpikeWorld : MonoBehaviour
    {
        public string preset = "noon";
        [Tooltip("Ground chunks built in Unity from shared/World.cs (true), or the meshes Godot exported (false).")]
        public bool sharedGround = true;
        public bool godotGrade = true;
        public bool unityLighting = false;
        [Tooltip("Adds part of Godot's volumetric fog density to Unity's exponential fog (URP has no volumetric fog).")]
        public bool approximateVolumetricFog = true;

        string[] presets = new string[0];
        SpikeData data;
        Camera cam;
        Volume volume;
        VolumeProfile profile;
        readonly List<Light> suns = new();
        readonly List<GameObject> spawned = new();
        readonly List<Batch> batches = new();
        readonly HashSet<Material> materials = new();
        Vector3 godotCamPos;
        Quaternion godotCamRot;
        bool help = true;
        SphericalHarmonicsL2 ambientProbe;
        string status = "";
        float yaw, pitch;

        sealed class Batch
        {
            public Mesh Mesh;
            public int Submesh;
            public Material Material;
            public ShadowCastingMode Shadows;
            public Matrix4x4[] Matrices;
            public MaterialPropertyBlock Props;
        }

        static string Root => Path.Combine(Application.streamingAssetsPath, "spike");

        void Start()
        {
            QualitySettings.vSyncCount = 1;
            if (Directory.Exists(Root))
            {
                var dirs = new List<string>();
                foreach (var d in Directory.GetDirectories(Root))
                    if (File.Exists(Path.Combine(d, "scene.json"))) dirs.Add(Path.GetFileName(d));
                dirs.Sort((a, b) => a == "noon" ? -1 : b == "noon" ? 1 : string.CompareOrdinal(a, b));
                presets = dirs.ToArray();
            }
            if (presets.Length == 0) { status = $"No exports in {Root}"; return; }
            if (System.Array.IndexOf(presets, preset) < 0) preset = presets[0];
            Load(preset);
        }

        // ---- loading -------------------------------------------------------------

        void Clear()
        {
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            batches.Clear();
            materials.Clear();
            foreach (var l in suns) Destroy(l.gameObject);
            suns.Clear();
        }

        void Load(string name)
        {
            Clear();
            preset = name;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            data = new SpikeData(Path.Combine(Root, name));
            // A preset may hold only lighting (scene.json); its geometry then comes from noon.
            var geometry = Directory.Exists(Path.Combine(data.Dir, "meshes")) ? data : new SpikeData(Path.Combine(Root, "noon"));
            SetGlobals(data.Scene);
            SetupCamera(data.Scene);
            SetupLights(data.Scene);
            SetupEnvironment(data.Scene);
            BuildDraws(geometry);
            if (sharedGround) BuildSharedGround(data.Scene);
            ApplyLightingMode();
            status = $"{name}: {batches.Count} instanced batches, {spawned.Count} objects, loaded in {sw.ElapsedMilliseconds} ms";
            Debug.Log("[spike] " + status);
        }

        void SetGlobals(object scene)
        {
            var g = scene.O("global_shader_params");
            float Gf(string k, float d) => g != null && g.Get(k) is double v ? (float)v : d;
            float[] Ga(string k, float[] d) => g?.Floats(k) ?? d;
            Shader.SetGlobalFloat("lg_wetness", Gf("wetness", 0f));
            Shader.SetGlobalFloat("lg_wind_strength", Gf("wind_strength", 0.3f));
            Shader.SetGlobalFloat("lg_wind_gust", Gf("wind_gust", 0.3f));
            var wd = Ga("wind_dir", new[] { 0.6f, 0.8f });
            Shader.SetGlobalVector("lg_wind_dir", new Vector4(wd[0], wd[1], 0, 0));
            var sky = data.MaterialDef(scene.I("sky_material", -1))?.Get("params");
            Shader.SetGlobalFloat("lg_cloud_cover", Gf("cloud_cover", sky.F("cloud_cover", 0.2f)));
            var co = Ga("cloud_offset", sky.Floats("cloud_offset") ?? new[] { 0f, 0f });
            Shader.SetGlobalVector("lg_cloud_offset", new Vector4(co[0], co[1], 0, 0));
            var player = SpikeData.V(scene.O("player")?.Floats("origin"));
            Shader.SetGlobalVector("lg_player_position", player);
            foreach (var def in data.MaterialDefs)
                if (def.Get("params") is Dictionary<string, object> ps && ps.TryGetValue("clouds", out var c) && c is string file)
                {
                    Shader.SetGlobalTexture("lg_clouds", data.Texture(file));
                    break;
                }
        }

        void SetupCamera(object scene)
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Camera", typeof(Camera));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            var c = scene.O("camera");
            if (c != null)
            {
                godotCamPos = SpikeData.V(c.Floats("origin"));
                var fwd = SpikeData.V(c.Floats("forward"));
                var up = SpikeData.V(c.Floats("up"));
                godotCamRot = Quaternion.LookRotation(fwd, up);
                cam.fieldOfView = c.F("fov", 45f);   // Godot keeps the height: vertical fov, as Unity
                cam.nearClipPlane = Mathf.Max(0.05f, c.F("near", 0.05f));
                cam.farClipPlane = c.F("far", 4000f);
            }
            ResetView();
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var extra = cam.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = true;
            extra.renderShadows = true;
            extra.antialiasing = AntialiasingMode.FastApproximateAntialiasing;   // Godot: MSAA 4x + FXAA
            extra.requiresDepthTexture = true;
        }

        void ResetView()
        {
            cam.transform.SetPositionAndRotation(godotCamPos, godotCamRot);
            var e = godotCamRot.eulerAngles;
            yaw = e.y; pitch = e.x;
        }

        void SetupLights(object scene)
        {
            var list = scene.A("suns") ?? new List<object>();
            Light main = null;
            foreach (var s in list)
            {
                if (s.Get("visible") is false) continue;
                float energy = s.F("light_energy", 1f);
                if (energy <= 0f) continue;
                var go = new GameObject("Sun (Godot DirectionalLight3D)");
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;
                // Godot divides by PI in light() and multiplies the energy by PI, so its
                // energy is Unity's intensity; the colour is sRGB in both.
                light.color = SpikeData.C(s.Floats("light_color"));
                light.intensity = energy;
                bool shadow = s.Get("shadow_enabled") is true;
                light.shadows = shadow ? LightShadows.Soft : LightShadows.None;
                light.shadowStrength = s.F("shadow_opacity", 1f);
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                go.transform.rotation = Quaternion.LookRotation(SpikeData.V(s.Floats("forward")));
                suns.Add(light);
                if (main == null && shadow) main = light;
            }
            if (main == null && suns.Count > 0) main = suns[0];
            RenderSettings.sun = main;
            foreach (var l in suns) l.renderMode = l == main ? LightRenderMode.ForcePixel : LightRenderMode.Auto;

            foreach (var o in scene.A("omnis") ?? new List<object>())
            {
                var go = new GameObject("Omni (Godot OmniLight3D)");
                go.transform.position = SpikeData.V(o.Floats("origin"));
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = SpikeData.C(o.Floats("color"));
                light.intensity = o.F("energy", 1f);
                light.range = o.F("range", 5f);
                light.shadows = o.Get("shadow") is true ? LightShadows.Soft : LightShadows.None;
                suns.Add(light);
            }
        }

        static float SrgbToLinear(float c) => c < 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        static Color Linear(Color c) => new(SrgbToLinear(c.r), SrgbToLinear(c.g), SrgbToLinear(c.b), c.a);

        void SetupEnvironment(object scene)
        {
            var env = scene.O("environment");

            // Sky: the ported Atmosphere sky shader, fogged as Godot fogs its sky.
            var skyMat = data.Material(scene.I("sky_material", -1));
            var fogColor = SpikeData.C(env.Floats("fog_light_color")) * env.F("fog_light_energy", 1f);
            if (skyMat != null)
            {
                skyMat.SetColor("_FogColor", fogColor);
                bool fogOn = env.Get("fog_enabled") is true;
                skyMat.SetFloat("_FogSkyAffect", fogOn ? env.F("fog_sky_affect", 1f) : 0f);
                skyMat.SetFloat("_AerialPerspective", env.F("fog_aerial_perspective", 0f));
                RenderSettings.skybox = skyMat;
            }

            // Ambient: Godot mixes its ambient colour with the sky's light
            // (ambient_light_sky_contribution), times ambient_light_energy. Unity's
            // ambient probe is filled the same way from the same sky colours.
            float energy = env.F("ambient_light_energy", 1f);
            float skyMix = env.I("ambient_light_source", 0) == 2 ? 0f : env.F("ambient_light_sky_contribution", 1f);
            var ambColor = Linear(SpikeData.C(env.Floats("ambient_light_color")));
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(ambColor * energy * (1f - skyMix));
            if (skyMat != null && skyMix > 0f)
            {
                const int samples = 512;
                // GetColor gives back the sRGB value that was set; the sky shader sees linear.
                var top = Linear(skyMat.GetColor("_TopColor")); var horizon = Linear(skyMat.GetColor("_HorizonColor"));
                var ground = Linear(skyMat.GetColor("_GroundColor")); var sunCol = Linear(skyMat.GetColor("_SunColor"));
                Vector3 sunDirG = skyMat.GetVector("_SunDir");
                var sunDir = new Vector3(sunDirG.x, sunDirG.y, -sunDirG.z).normalized;
                for (int i = 0; i < samples; i++)
                {
                    // Fibonacci sphere.
                    float y = 1f - 2f * (i + 0.5f) / samples;
                    float r = Mathf.Sqrt(1f - y * y);
                    float phi = i * 2.39996323f;
                    var d = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
                    var col = Color.Lerp(horizon, top, Mathf.Pow(Mathf.Clamp01(d.y), 0.45f));
                    col = Color.Lerp(col, ground, 1f - Smooth(-0.25f, 0f, d.y));
                    float s = Mathf.Max(Vector3.Dot(d, sunDir), 0f);
                    col += sunCol * (Mathf.Pow(s, 5f) * 0.3f + Mathf.Pow(s, 60f) * 0.5f) * Smooth(-0.15f, 0.05f, sunDir.y);
                    sh.AddDirectionalLight(d, col, energy * skyMix * 4f / samples);
                }
            }
            // Unity may regenerate the probe from the skybox on its own; Update puts ours back.
            RenderSettings.ambientMode = AmbientMode.Skybox;
            ambientProbe = sh;
            RenderSettings.ambientProbe = sh;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.5f;

            // Fog: Godot's exponential depth fog. URP has no volumetric fog, so a part of
            // Godot's volumetric density can be folded in to approximate its haze.
            RenderSettings.fog = env.Get("fog_enabled") is true;
            RenderSettings.fogMode = FogMode.Exponential;
            float density = env.F("fog_density", 0.001f);
            if (approximateVolumetricFog && env.Get("volumetric_fog_enabled") is true)
                density += 0.5f * env.F("volumetric_fog_density", 0f);
            RenderSettings.fogDensity = density;
            RenderSettings.fogColor = fogColor;

            BuildVolume(env, scene.O("camera_attributes"));
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        void BuildVolume(object env, object camAttr)
        {
            if (volume == null)
            {
                var go = new GameObject("Post (Godot Environment)");
                volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10;
            }
            if (profile != null) Destroy(profile);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            // Glow -> Bloom. Godot's glow (soft light, after tonemapping) and URP's bloom
            // (additive, before) differ; these numbers aim for a similar amount.
            if (env.Get("glow_enabled") is true)
            {
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(env.F("glow_hdr_threshold", 1f));
                bloom.intensity.Override(env.F("glow_intensity", 0.8f) * 0.5f);
                bloom.scatter.Override(0.7f);
            }

            var tone = profile.Add<Tonemapping>(true);
            var adjust = profile.Add<ColorAdjustments>(true);
            var lookup = profile.Add<ColorLookup>(true);
            if (godotGrade)
            {
                tone.mode.Override(TonemappingMode.None);
                adjust.postExposure.Override(GodotGrade.HeadroomEv);
                var asset = UniversalRenderPipeline.asset;
                int size = asset != null ? asset.colorGradingLutSize : 32;
                var grad = env.Get("adjustment_color_correction") is string f ? data.Texture(f) : null;
                lookup.texture.Override(GodotGrade.BuildLut(size, env, grad));
                lookup.contribution.Override(1f);
            }
            else
            {
                tone.mode.Override(TonemappingMode.ACES);
                adjust.postExposure.Override(Mathf.Log(Mathf.Max(env.F("tonemap_exposure", 1f), 0.01f), 2f));
                if (env.Get("adjustment_enabled") is true)
                {
                    adjust.contrast.Override((env.F("adjustment_contrast", 1f) - 1f) * 100f);
                    adjust.saturation.Override((env.F("adjustment_saturation", 1f) - 1f) * 100f);
                }
                lookup.contribution.Override(0f);
            }

            // CameraRig's far blur (CameraAttributesPractical) -> Gaussian depth of field.
            if (camAttr != null && camAttr.Get("dof_blur_far_enabled") is true)
            {
                var dof = profile.Add<DepthOfField>(true);
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                float start = camAttr.F("dof_blur_far_distance", 32f);
                dof.gaussianStart.Override(start);
                dof.gaussianEnd.Override(start + camAttr.F("dof_blur_far_transition", 10f));
                dof.gaussianMaxRadius.Override(Mathf.Clamp(camAttr.F("dof_blur_amount", 0.1f) * 10f, 0.5f, 1.5f));
            }
        }

        // ---- geometry ------------------------------------------------------------

        static bool IsGround(SpikeData d, object def) =>
            def != null && def.Get("params") is Dictionary<string, object> p && p.TryGetValue("puddles_ok", out var v) && v is double x && x > 0.5;

        void BuildDraws(SpikeData geo)
        {
            var parent = new GameObject("Godot export");
            spawned.Add(parent);
            var merged = new Dictionary<(Mesh, int, Material, ShadowCastingMode), (List<Matrix4x4>, List<Vector4>)>();
            foreach (var draw in geo.Scene.A("draws") ?? new List<object>())
            {
                int meshId = draw.I("mesh", -1);
                int count = draw.I("count", 1);
                if (meshId < 0 || count <= 0) continue;
                var mesh = geo.Mesh(meshId);
                int overrideId = draw.I("material", -1);
                var surf = draw.A("surface_materials") ?? new List<object>();
                var mats = new Material[mesh.subMeshCount];
                bool any = false;
                for (int s = 0; s < mats.Length; s++)
                {
                    int id = overrideId >= 0 ? overrideId : s < surf.Count && surf[s] is double dd ? (int)dd : -1;
                    var def = geo.MaterialDef(id);
                    if (sharedGround && IsGround(geo, def)) continue;
                    mats[s] = geo.Material(id);
                    if (mats[s] != null) { any = true; materials.Add(mats[s]); }
                }
                if (!any) continue;
                var shadows = draw.I("shadow", 1) switch
                {
                    0 => ShadowCastingMode.Off,
                    2 => ShadowCastingMode.TwoSided,
                    3 => ShadowCastingMode.ShadowsOnly,
                    _ => ShadowCastingMode.On,
                };
                int offset = draw.I("offset", 0);
                if (draw.Get("multimesh") is true || count > 1)
                {
                    // Godot keeps one multimesh per chunk; Unity draws fastest with every
                    // instance of a mesh and material together, so they are merged here.
                    for (int s = 0; s < mats.Length; s++)
                    {
                        if (mats[s] == null) continue;
                        var key = (mesh, s, mats[s], shadows);
                        if (!merged.TryGetValue(key, out var list)) merged[key] = list = (new List<Matrix4x4>(), new List<Vector4>());
                        for (int i = 0; i < count; i++)
                        {
                            list.Item1.Add(geo.InstanceMatrix(offset, i));
                            list.Item2.Add(geo.InstanceColor(offset, i));
                        }
                    }
                }
                else
                {
                    var go = new GameObject(mesh.name);
                    go.transform.SetParent(parent.transform, false);
                    var m = geo.InstanceMatrix(offset, 0);
                    go.transform.SetPositionAndRotation(m.GetPosition(), m.rotation);
                    go.transform.localScale = m.lossyScale;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    var fallback = mats[System.Array.FindIndex(mats, x => x != null)];
                    for (int s = 0; s < mats.Length; s++) mats[s] ??= fallback;
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = shadows;
                    var c = geo.InstanceColor(offset, 0);
                    if (c != Vector4.one)
                    {
                        var block = new MaterialPropertyBlock();
                        block.SetVector("_InstColor", c);
                        r.SetPropertyBlock(block);
                    }
                }
            }
            // 500 per call: what fits Unity's instancing constant buffer on D3D11, so the
            // per-instance colour array never has to be split by the engine.
            const int perCall = 500;
            foreach (var ((mesh, sub, mat, shadows), (mats4, cols)) in merged)
                for (int start = 0; start < mats4.Count; start += perCall)
                {
                    int n = Mathf.Min(perCall, mats4.Count - start);
                    var b = new Batch
                    {
                        Mesh = mesh, Submesh = sub, Material = mat, Shadows = shadows,
                        Matrices = mats4.GetRange(start, n).ToArray(), Props = new MaterialPropertyBlock(),
                    };
                    b.Props.SetVectorArray("_InstColor", cols.GetRange(start, n));
                    batches.Add(b);
                }
        }

        // The ground around the player from shared/World.cs, over the same chunks Godot drew.
        void BuildSharedGround(object scene)
        {
            var groundDef = -1;
            for (int i = 0; i < data.MaterialDefs.Count; i++)
                if (IsGround(data, data.MaterialDefs[i])) { groundDef = i; break; }
            var mat = data.Material(groundDef);
            if (mat == null) { Debug.LogWarning("[spike] no ground material in the export"); return; }
            materials.Add(mat);
            var parent = new GameObject("Ground (shared/World.cs)");
            spawned.Add(parent);
            var p = scene.O("player")?.Floats("origin") ?? new[] { 0f, 0f, 0f };
            int pcx = World.ChunkOfCell(Mathf.FloorToInt(p[0])), pcz = World.ChunkOfCell(Mathf.FloorToInt(p[2]));
            const int radius = 8;   // ViewDistance's default radius in the Godot export
            for (int cx = pcx - radius; cx <= pcx + radius; cx++)
                for (int cz = pcz - radius; cz <= pcz + radius; cz++)
                {
                    var go = new GameObject($"Chunk {cx},{cz}");
                    go.transform.SetParent(parent.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = GroundBuilder.BuildChunk(cx, cz);
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = ShadowCastingMode.On;
                }
        }

        void ApplyLightingMode()
        {
            foreach (var m in materials)
                if (unityLighting) m.EnableKeyword("_UNITY_PBR"); else m.DisableKeyword("_UNITY_PBR");
        }

        // ---- per frame -------------------------------------------------------------

        void Update()
        {
            Shader.SetGlobalFloat("lg_world_seconds", Time.time);
            if (data != null) RenderSettings.ambientProbe = ambientProbe;
            foreach (var b in batches)
            {
                var rp = new RenderParams(b.Material)
                {
                    shadowCastingMode = b.Shadows,
                    receiveShadows = true,
                    matProps = b.Props,
                    worldBounds = new Bounds(Vector3.zero, Vector3.one * 2000f),
                };
                Graphics.RenderMeshInstanced(rp, b.Mesh, b.Submesh, b.Matrices);
            }
            Keys();
            FlyCamera();
        }

        void Keys()
        {
            for (int i = 0; i < presets.Length && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Load(presets[i]);
            if (Input.GetKeyDown(KeyCode.G)) { godotGrade = !godotGrade; BuildVolume(data.Scene.O("environment"), data.Scene.O("camera_attributes")); }
            if (Input.GetKeyDown(KeyCode.L)) { unityLighting = !unityLighting; ApplyLightingMode(); }
            if (Input.GetKeyDown(KeyCode.H)) help = !help;
            if (Input.GetKeyDown(KeyCode.R)) ResetView();
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.F12)) Screenshot();
        }

        void FlyCamera()
        {
            if (cam == null || !Input.GetMouseButton(1)) return;
            yaw += Input.GetAxis("Mouse X") * 3f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, -89f, 89f);
            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            var move = new Vector3((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            float speed = Input.GetKey(KeyCode.LeftShift) ? 20f : 5f;
            cam.transform.position += cam.transform.TransformDirection(move) * speed * Time.deltaTime;
        }

        void Screenshot()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "renders"));
            Directory.CreateDirectory(dir);
            string mode = (unityLighting ? "unitylit" : "godotlit") + (godotGrade ? "" : "-unitygrade");
            string path = Path.Combine(dir, $"unity-spike-{preset}-{mode}.png");
            bool wasHelp = help;
            help = false;
            ScreenCapture.CaptureScreenshot(path);
            status = $"saved {path}";
            Debug.Log("[spike] " + status);
            StartCoroutine(RestoreHelp(wasHelp));
        }

        System.Collections.IEnumerator RestoreHelp(bool on)
        {
            yield return null;
            yield return null;
            help = on;
        }

        void OnGUI()
        {
            if (!help) return;
            var lines = $"LifeGame Unity spike - preset: {preset}   ({string.Join(", ", presets)})\n" +
                "1-9 preset   G grade: " + (godotGrade ? "Godot (same curve as the game)" : "Unity ACES") +
                "   L lighting: " + (unityLighting ? "URP stock PBR" : "Godot painted (ported)") +
                "\nP/F12 screenshot to renders/   right-drag + WASD/QE fly, Shift fast   R reset camera   H hide\n" + status;
            GUI.color = Color.white;
            GUI.Box(new Rect(8, 8, 720, 74), GUIContent.none);
            GUI.Label(new Rect(16, 12, 710, 70), lines);
        }
    }
}
