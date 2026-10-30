using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "Linda/Build Lab": frames under Assets/Linda/Art/<clip>/ -> sprites (pivot at the feet), one AnimationClip
// per folder, an AnimatorController, and the Lab scene: floor, wall, light, camera, Linda and the bag. Re-run after
// Tools/make_linda_frames.py; it overwrites everything it made. Also run headless:
//   Unity -batchmode -quit -projectPath . -executeMethod LindaLab.Build
public static class LindaLab
{
    const string Art = "Assets/Linda/Art", Anim = "Assets/Linda/Anim", Scene = "Assets/Linda/Lab.unity";
    const float Fps = 24;
    static readonly string[] Loops = { "idle", "walk" };

    [MenuItem("Linda/Build Lab")]
    public static void Build()
    {
        var pivots = File.ReadAllLines(Art + "/pivots.txt").Select(l => l.Split(' '))
            .ToDictionary(p => p[0], p => float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(Anim);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Anim + "/Linda.controller");
        var sm = controller.layers[0].stateMachine;
        controller.AddParameter("walk", AnimatorControllerParameterType.Bool);
        controller.AddParameter("whip", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("hit", AnimatorControllerParameterType.Trigger);
        AnimatorState idle = null, walk = null;
        foreach (var dir in Directory.GetDirectories(Art).OrderBy(d => d))
        {
            var name = Path.GetFileName(dir);
            var sprites = ImportFrames(dir, pivots[name]);
            var clip = MakeClip(name, sprites);
            var state = sm.AddState(name);
            state.motion = clip;
            if (name == "idle") idle = state;
            if (name == "walk") walk = state;
        }
        sm.defaultState = idle;
        Bool(idle.AddTransition(walk), "walk", true);
        Bool(walk.AddTransition(idle), "walk", false);
        foreach (var (state, trigger) in new[] { ("hit_whip", "whip"), ("hit_back", "hit") })
        {
            var s = sm.states.First(x => x.state.name == state).state;
            var t = sm.AddAnyStateTransition(s);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.duration = 0; t.canTransitionToSelf = false;
            var back = s.AddTransition(idle);
            back.hasExitTime = true; back.exitTime = 1; back.duration = 0;
        }
        BuildScene(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Linda lab built: " + Scene);
    }

    static void Bool(AnimatorStateTransition t, string param, bool value)
    {
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        t.hasExitTime = false; t.duration = 0;
    }

    static Sprite[] ImportFrames(string dir, float pivotY)
    {
        var paths = Directory.GetFiles(dir, "*.png").OrderBy(p => p).ToArray();
        foreach (var p in paths)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.textureType = TextureImporterType.Sprite;
            s.spriteMode = (int)SpriteImportMode.Single;
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = new Vector2(0.5f, pivotY);
            s.spritePixelsPerUnit = 100;
            s.mipmapEnabled = false;
            s.alphaIsTransparency = true;
            imp.SetTextureSettings(s);
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();
    }

    static AnimationClip MakeClip(string name, Sprite[] sprites)
    {
        var clip = new AnimationClip { frameRate = Fps };
        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / Fps, value = s }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = Loops.Contains(name);
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, Anim + "/" + name + ".anim");
        return clip;
    }

    static void BuildScene(AnimatorController controller)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 1.4f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        cam.transform.position = new Vector3(0, 0.9f, -10);
        cam.tag = "MainCamera";
        var light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, -30, 0);
        Quad("Floor", new Vector3(0, -0.5f, 0.5f), new Vector3(20, 1, 1), new Color(0.18f, 0.18f, 0.2f));
        Quad("Wall", new Vector3(0, 3.5f, 1), new Vector3(20, 7, 1), new Color(0.12f, 0.12f, 0.14f));
        var linda = new GameObject("Linda");
        linda.AddComponent<SpriteRenderer>();
        linda.AddComponent<Animator>().runtimeAnimatorController = controller;
        linda.AddComponent<LindaController>();
        linda.transform.position = new Vector3(-1, 0, 0);
        var bag = new GameObject("Bag");
        Quad("Chain", new Vector3(1.5f, 1.9f, 0.31f), new Vector3(0.04f, 2.2f, 1), new Color(0.35f, 0.35f, 0.38f)).transform.parent = bag.transform;
        Quad("Sack", new Vector3(1.5f, 0.8f, 0.3f), new Vector3(0.5f, 1.1f, 1), new Color(0.45f, 0.12f, 0.1f)).transform.parent = bag.transform;
        bag.AddComponent<Bag>();
        EditorSceneManager.SaveScene(scene, Scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
    }

    static GameObject Quad(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name; q.transform.position = pos; q.transform.localScale = scale;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Unlit/Color")) { color = color };
        AssetDatabase.CreateAsset(mat, Anim + "/" + name + ".mat");
        q.GetComponent<Renderer>().sharedMaterial = mat;
        return q;
    }
}

// Menu "Linda/Screenshot": the lab with one frame of a clip sampled on Linda, rendered to Logs/lab_<clip>.png.
//   Unity -batchmode -quit -projectPath . -executeMethod LindaLabShot.All
public static class LindaLabShot
{
    [MenuItem("Linda/Screenshot")]
    public static void All()
    {
        EditorSceneManager.OpenScene("Assets/Linda/Lab.unity");
        var linda = GameObject.Find("Linda");
        var cam = Camera.main;
        var rt = new RenderTexture(960, 540, 24);
        cam.targetTexture = rt;
        Directory.CreateDirectory("Logs");
        foreach (var clip in new[] { "idle", "walk", "hit_whip", "hit_back" })
        {
            var anim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Linda/Anim/" + clip + ".anim");
            anim.SampleAnimation(linda, anim.length * 0.4f);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            File.WriteAllBytes("Logs/lab_" + clip + ".png", tex.EncodeToPNG());
        }
        RenderTexture.active = null;
        cam.targetTexture = null;
    }
}

// WebGL build for GitHub Pages -> Build/WebGL (gzip + decompression fallback: works without server headers).
//   Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod LindaLabBuild.WebGL
public static class LindaLabBuild
{
    public static void WebGL()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.productName = "Linda Lab";
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Linda/Lab.unity" }, "Build/WebGL", BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
