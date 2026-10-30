using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Original, deterministic 3D scenery. No downloaded media and no gameplay colliders.
public static class BuildSpaceEnvironment
{
    const string Root = "Assets/MapRevision/SpaceEnvironment";
    static readonly Vector3 SunPosition = new Vector3(-800, 300, 500);

    // The caller owns removal of old scenery. Use a camera far plane of 1800 or more.
    public static void Build(Transform parent)
    {
        Directory.CreateDirectory(Root + "/Meshes");
        Directory.CreateDirectory(Root + "/Materials");
        AssetDatabase.Refresh();
        var root = Group(parent, "SpaceEnvironment_OriginalProcedural").transform;
        var sphere = MakeSphere(128, 64);
        var earth = Planet(root, "Pelagos_OceanWorld", sphere, new Vector3(-250, 90, -340), 110,
            new Color(.016f, .075f, .15f), new Color(.18f, .23f, .135f), new Color(.47f, .39f, .23f), false);
        earth.transform.localRotation = Quaternion.Euler(18, 42, -12);
        var gas = Planet(root, "Vesper_RingedWorld", sphere, new Vector3(350, 100, 200), 55,
            new Color(.23f, .27f, .33f), new Color(.50f, .43f, .33f), new Color(.77f, .67f, .48f), true);
        gas.transform.localRotation = Quaternion.Euler(22, 10, -24);
        Rings(gas.transform);
        Stars(root);
        Sun(root);
        AssetDatabase.SaveAssets();
    }

    static GameObject Planet(Transform parent, string name, Mesh sphere, Vector3 position, float radius,
        Color ocean, Color land, Color highland, bool gas)
    {
        var material = new Material(RequiredShader("SpaceJanitors/MapRevision/Planet")) { name = name };
        material.SetColor("_OceanColor", ocean);
        material.SetColor("_LandColor", land);
        material.SetColor("_HighlandsColor", highland);
        material.SetColor("_CloudColor", new Color(.89f, .92f, .94f));
        material.SetFloat("_GasWorld", gas ? 1 : 0);
        material.SetVector("_SunDirection", (SunPosition - position).normalized);
        material = Save(material, "Materials/" + name + ".mat");
        var world = MeshObject(parent, name, sphere, material);
        world.transform.localPosition = position;
        world.transform.localScale = Vector3.one * radius;
        var atmosphere = new Material(RequiredShader("SpaceJanitors/MapRevision/Atmosphere")) { name = name + "_Atmosphere" };
        atmosphere.SetColor("_Color", gas ? new Color(.66f, .59f, .43f, .27f) : new Color(.20f, .53f, 1f, .54f));
        atmosphere.SetVector("_SunDirection", (SunPosition - position).normalized);
        var rim = MeshObject(world.transform, "AtmosphereRim", sphere, Save(atmosphere, "Materials/" + atmosphere.name + ".mat"));
        rim.transform.localScale = Vector3.one * (gas ? 1.014f : 1.021f);
        return world;
    }

    static void Rings(Transform parent)
    {
        var verts = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        const int segments = 256;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            verts.Add(direction * 1.32f); verts.Add(direction * 2.16f);
            uv.Add(new Vector2(0, i / (float)segments)); uv.Add(new Vector2(1, i / (float)segments));
            if (i < segments) { int n = i * 2; tri.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 }); }
        }
        var mesh = new Mesh { name = "Vesper_ParticleRing" };
        mesh.SetVertices(verts); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var material = new Material(RequiredShader("SpaceJanitors/MapRevision/PlanetRings")) { name = "Vesper_IceRings" };
        material.SetColor("_Color", new Color(.62f, .56f, .42f, .54f));
        MeshObject(parent, "FineIceRings", Save(mesh, "Meshes/VesperRings.asset"), Save(material, "Materials/VesperRings.mat"));
    }

    static void Stars(Transform parent)
    {
        var random = new System.Random(47219);
        var verts = new List<Vector3>(); var colors = new List<Color>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int i = 0; i < 1850; i++)
        {
            float a = (float)random.NextDouble() * Mathf.PI * 2;
            float y = (float)random.NextDouble() * 2 - 1;
            float r = Mathf.Sqrt(1 - y * y);
            Vector3 dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            // A restrained denser band gives the sky depth without opaque nebula cards.
            if (i > 1450) dir = (Quaternion.Euler(30, 20, 35) * new Vector3(Mathf.Cos(a), y * .12f, Mathf.Sin(a))).normalized;
            var center = dir * (1160 + (float)random.NextDouble() * 130);
            float halfSize = Mathf.Lerp(.65f, 1.65f, Mathf.Pow((float)random.NextDouble(), 2.5f));
            if (i % 71 == 0) halfSize = 2f;
            var tint = Color.Lerp(new Color(.64f, .76f, 1f), new Color(1f, .88f, .68f), (float)random.NextDouble());
            float light = Mathf.Lerp(.4f, 1f, (float)random.NextDouble());
            tint *= light; tint.a = 1;
            AddCard(center, halfSize, tint, verts, colors, uv, tri);
        }
        var mesh = new Mesh { name = "DistantFineStars" };
        mesh.SetVertices(verts); mesh.SetColors(colors); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
        var mat = new Material(RequiredShader("SpaceJanitors/MapRevision/SpaceGlow")) { name = "FineStarlight" };
        mat.SetFloat("_Sun", 0); mat.SetColor("_Color", Color.white);
        MeshObject(parent, "DistantFineStars", Save(mesh, "Meshes/Starfield.asset"), Save(mat, "Materials/Starlight.mat"));
    }

    static void Sun(Transform parent)
    {
        var verts = new List<Vector3>(); var colors = new List<Color>(); var uv = new List<Vector2>(); var tri = new List<int>();
        AddCard(SunPosition, 52, Color.white, verts, colors, uv, tri);
        var mesh = new Mesh { name = "SolarDiscAndCorona" };
        mesh.SetVertices(verts); mesh.SetColors(colors); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
        var mat = new Material(RequiredShader("SpaceJanitors/MapRevision/SpaceGlow")) { name = "WarmSolarDisc" };
        mat.SetFloat("_Sun", 1); mat.SetColor("_Color", new Color(1f, .73f, .37f, 1));
        MeshObject(parent, "Helios_Sun", Save(mesh, "Meshes/Sun.asset"), Save(mat, "Materials/Sun.mat"));
    }

    static void AddCard(Vector3 center, float halfSize, Color tint, List<Vector3> vertices,
        List<Color> colors, List<Vector2> uv, List<int> triangles)
    {
        var normal = -center.normalized;
        var right = Vector3.Cross(Mathf.Abs(normal.y) > .98f ? Vector3.forward : Vector3.up, normal).normalized * halfSize;
        var up = Vector3.Cross(normal, right).normalized * halfSize;
        int n = vertices.Count;
        vertices.AddRange(new[] { center - right - up, center + right - up, center + right + up, center - right + up });
        uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
        colors.AddRange(new[] { tint, tint, tint, tint });
        triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
    }

    static Mesh MakeSphere(int longitude, int latitude)
    {
        var verts = new List<Vector3>(); var normal = new List<Vector3>(); var tri = new List<int>();
        for (int j = 0; j <= latitude; j++)
        {
            float phi = j * Mathf.PI / latitude;
            for (int i = 0; i <= longitude; i++)
            {
                float theta = i * Mathf.PI * 2 / longitude;
                var v = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                verts.Add(v); normal.Add(v);
                if (j < latitude && i < longitude)
                {
                    int a = j * (longitude + 1) + i;
                    tri.AddRange(new[] { a, a + 1, a + longitude + 1, a + 1, a + longitude + 2, a + longitude + 1 });
                }
            }
        }
        var mesh = new Mesh { name = "SmoothPlanetSphere" };
        mesh.SetVertices(verts); mesh.SetNormals(normal); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
        return Save(mesh, "Meshes/PlanetSphere.asset");
    }

    static Shader RequiredShader(string name)
    {
        var shader = Shader.Find(name);
        if (!shader) throw new InvalidOperationException("Missing map scenery shader: " + name);
        return shader;
    }

    static GameObject Group(Transform parent, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
    }

    static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material)
    {
        var go = Group(parent, name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return go;
    }

    static T Save<T>(T asset, string path) where T : UnityEngine.Object
    {
        path = Root + "/" + path;
        var previous = AssetDatabase.LoadAssetAtPath<T>(path);
        if (previous) { EditorUtility.CopySerialized(asset, previous); UnityEngine.Object.DestroyImmediate(asset); return previous; }
        AssetDatabase.CreateAsset(asset, path); return asset;
    }
}
