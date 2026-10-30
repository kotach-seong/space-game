using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit standalone opt-in: --sj-map-test <absolute output directory>.
// Normal play never creates this object or changes the save profile location.
public sealed class MapRevisionPlaytest : MonoBehaviour
{
    const float StepTime = 1f / 60f;
    const int SolidLayers = ~(1 << 2);
    readonly List<string> results = new List<string>();
    string output;
    int failures;
    int runtimeErrors;
    bool finished;
    float began;
    SJPlayer player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        string[] args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "--sj-map-test");
        if (flag < 0 || flag + 1 >= args.Length) return;

        string destination = Path.GetFullPath(args[flag + 1]);
        Directory.CreateDirectory(destination);
        // Isolate even a menu's initialization from the user's real save files.
        SJProfile.TestDirectory = Path.Combine(destination, "IsolatedProfile");
        SJProfile.ActiveSlot = -1;
        SJProfile.Run = null;
        SJProfile.ReturnToShop = false;
        SJProfile.Reload();
        SJProfile.Data.settings.fullscreen = false;
        SJProfile.Data.settings.width = 1280;
        SJProfile.Data.settings.height = 720;
        var host = new GameObject("MapRevision_AutomatedPlaytest");
        DontDestroyOnLoad(host);
        var test = host.AddComponent<MapRevisionPlaytest>();
        test.output = destination;
        test.began = Time.realtimeSinceStartup;
        Application.runInBackground = true;
        Application.logMessageReceived += test.OnError;
    }

    void OnError(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        runtimeErrors++;
        results.Add("RUNTIME ERROR: " + message + "\n" + stack);
    }

    IEnumerator Start()
    {
        var routine = Run();
        while (!finished)
        {
            bool next;
            object current = null;
            try
            {
                next = routine.MoveNext();
                if (next) current = routine.Current;
            }
            catch (Exception e)
            {
                failures++;
                results.Add("FAIL: Unhandled test failure: " + e);
                break;
            }
            if (!next) break;
            yield return current;
        }
        Finish();
    }

    void Update()
    {
        if (!finished && Time.realtimeSinceStartup - began > 180)
        {
            failures++;
            results.Add("FAIL: Test exceeded 180 second timeout.");
            Finish();
        }
    }

    void Finish()
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnError;
        bool passed = failures == 0 && runtimeErrors == 0;
        results.Add("Runtime errors: " + runtimeErrors);
        results.Add("Failed checks: " + failures);
        results.Add(passed ? "RESULT: PASS" : "RESULT: FAIL");
        File.WriteAllLines(Path.Combine(output, "map-playtest.txt"), results);
        Debug.Log(passed ? "SJ_MAP_PLAYTEST_PASSED" : "SJ_MAP_PLAYTEST_FAILED");
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Application.Quit(passed ? 0 : 1);
    }

    void Check(bool value, string description)
    {
        results.Add((value ? "PASS: " : "FAIL: ") + description);
        if (!value) failures++;
        Debug.Log("SJ_MAP_TEST: " + (value ? "PASS " : "FAIL ") + description);
    }

    void Move(Vector2 axes, float lift, int steps, bool fast = false)
    {
        for (int i = 0; i < steps; i++) player.MoveStep(axes, lift, fast, false, StepTime);
        Physics.SyncTransforms();
    }

    bool ShellHit(Vector3 origin, Vector3 direction, float distance, Predicate<Vector3> correctSurface)
    {
        foreach (var hit in Physics.RaycastAll(origin, direction, distance, SolidLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider && !hit.collider.attachedRigidbody && correctSurface(hit.point)) return true;
        return false;
    }

    void RoomShell(int side, float z)
    {
        Vector3 point = new Vector3(side * 8, 1.5f, z);
        string label = (side < 0 ? "Port" : "Starboard") + " room z=" + z;
        Check(ShellHit(point, Vector3.right * side, 6,
            hit => Mathf.Abs(hit.x) >= 11.75f && Mathf.Abs(hit.x) <= 12.6f), label + " has a solid outer wall");
        Check(ShellHit(new Vector3(point.x, .2f, z), Vector3.down, 1,
            hit => Mathf.Abs(hit.y) < .18f), label + " has a floor");
        Check(ShellHit(new Vector3(point.x, 2.8f, z), Vector3.up, 2.4f,
            hit => hit.y > 4.1f && hit.y < 4.85f), label + " has a sealed roof");

        player.zeroGravity = false;
        player.Teleport(new Vector3(point.x, .08f, z), Quaternion.Euler(0, side * 90, 0));
        Move(Vector2.up, 0, 160);
        Check(Mathf.Abs(player.transform.position.x) < 12 && !player.InSpace,
            label + " blocks outward capsule travel: " + player.transform.position);

        player.zeroGravity = true;
        player.Teleport(new Vector3(point.x, .55f, z), Quaternion.identity);
        Move(Vector2.zero, 1, 190);
        Check(player.transform.position.y < 3.15f && player.transform.position.y > -.2f && !player.InSpace,
            label + " blocks swimming through ceiling: " + player.transform.position);
    }

    void Capture(string filename, Vector3 position, Vector3 target)
    {
        player.tethered = false;
        player.Teleport(position, Quaternion.identity);
        player.view.transform.rotation = Quaternion.LookRotation(target - player.view.transform.position);
        Capture(filename);
    }

    void Capture(string filename)
    {
        Camera camera = player.view;
        var texture = new RenderTexture(1440, 900, 24) { antiAliasing = 2 };
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        Texture2D bitmap = null;
        try
        {
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            bitmap = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            bitmap.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            bitmap.Apply();
            File.WriteAllBytes(Path.Combine(output, filename + ".png"), bitmap.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            texture.Release();
            Destroy(texture);
            if (bitmap) Destroy(bitmap);
        }
    }

    IEnumerator Run()
    {
        yield return null;
        if (!FindFirstObjectByType<SJGame>())
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("Kestrel_FirstPerson");
        }
        yield return null;
        yield return null;
        var game = FindFirstObjectByType<SJGame>();
        if (!game || !game.player) throw new Exception("Gameplay scene or first person player missing.");
        player = game.player;
        player.automated = true;
        player.tethered = false;
        player.zeroGravity = false;
        game.started = true;
        game.SetPaused(false);
        yield return new WaitForFixedUpdate();
        Check(!GameObject.Find("CrewScaleReference"), "Crew reference mannequin removed");
        Check(player.view && !player.view.orthographic, "First person camera preserved");
        Check(game.canvasHUD, "Existing Canvas HUD retained");
        Check(SJProfile.ActiveSlot == -1, "QA does not occupy or write a player save slot");
        Check(player.useInteriorVolumes, "Expanded ship uses explicit interior volumes");

        foreach (int side in new[] { -1, 1 })
            foreach (float z in new[] { -30f, 0f, 30f })
            {
                RoomShell(side, z);
                yield return null;
            }

        // A visible, collidable bulkhead seals the former one-way side exit.
        Check(ShellHit(new Vector3(13, 1.5f, 0), Vector3.right, 3,
            point => point.x >= 14.4f && point.x <= 15.2f), "EVA vestibule outer opening is sealed");

        foreach (int end in new[] { -1, 1 })
            foreach (int side in new[] { -1, 1 })
                Check(ShellHit(new Vector3(side * 2.96f, 2.2f, end * 44f), Vector3.forward * end, 2,
                    point => Mathf.Abs(Mathf.Abs(point.z) - 45f) < .3f),
                    "Narrow corridor/stair transition joint sealed: end=" + end + ", side=" + side);

        Check(ShellHit(new Vector3(7, 14, 51), Vector3.down, 9,
            point => point.y > 6 && point.y < 9), "Raised exterior shoulder has collision");
        Check(ShellHit(new Vector3(7, 9, -52), Vector3.down, 9,
            point => point.y > 1 && point.y < 4), "Aft exterior shoulder has collision");
        Check(ShellHit(new Vector3(0, 5.3f, 78), Vector3.back, 3,
            point => point.z > 75.7f && point.z < 76.3f), "Bow tip is capped and solid");

        // Traverse the enlarged concourse and the actual continuous stair geometry.
        player.zeroGravity = false;
        player.Teleport(new Vector3(0, .1f, 10), Quaternion.Euler(0, 180, 0));
        Move(Vector2.up, 0, 900);
        Check(player.transform.position.z < -38 && player.transform.position.z > -43 && player.transform.position.y > -.15f,
            "Walk 49 m down expanded central corridor: " + player.transform.position);
        Capture("01_Concourse_Aft");
        Move(Vector2.up, 0, 380);
        Check(player.transform.position.z < -57 && player.transform.position.y < -4.7f && player.transform.position.y > -5.25f && !player.InSpace,
            "Walk from main corridor down enclosed aft stairs: " + player.transform.position);
        Capture("02_Lower_Landing");
        yield return null;

        // Step beside the stair run, then walk forward into the larger lower hold.
        player.transform.rotation = Quaternion.identity;
        Move(Vector2.right, 0, 66);
        Move(Vector2.up, 0, 1280);
        Check(player.transform.position.z > 8 && player.transform.position.y < -4.7f && player.transform.position.y > -5.25f && !player.InSpace,
            "Walk from lower landing to cargo hold: " + player.transform.position);
        Capture("03_Lower_Cargo");
        yield return null;

        player.Teleport(new Vector3(0, .1f, 43), Quaternion.identity);
        Move(Vector2.up, 0, 350);
        Check(player.transform.position.z > 58 && player.transform.position.y > 4.75f && player.transform.position.y < 5.35f && !player.InSpace,
            "Walk up enclosed forward stairs into bridge: " + player.transform.position);
        Capture("04_Bridge");
        Check(player.transform.position.magnitude < 80, "Farthest tested upper deck stays within existing 85 m safety radius");
        yield return null;

        foreach (float z in new[] { 47f, 51f, 55f })
        {
            float floor = (z - 45) * 5 / 12;
            Vector3 at = new Vector3(0, floor + 1.6f, z);
            Check(ShellHit(at, Vector3.up, 4, point => point.y > floor + 3), "Upper stair roof sealed at z=" + z);
            foreach (int side in new[] { -1, 1 })
                Check(ShellHit(at, Vector3.right * side, 4, point => Mathf.Abs(point.x) > 2), "Upper stair side sealed at z=" + z + ", side=" + side);
        }
        foreach (float z in new[] { -47f, -51f, -55f })
        {
            float floor = (z + 45) * 5 / 12;
            Vector3 at = new Vector3(0, floor + 1.6f, z);
            Check(ShellHit(at, Vector3.up, 6, point => point.y > floor + 2.6f), "Aft stair roof sealed at z=" + z);
            Check(ShellHit(new Vector3(7, -3.4f, z), Vector3.right, 5,
                point => point.x > 9.5f && point.x < 12.5f), "Aft cargo outer wall sealed at z=" + z);
        }

        // Test gameplay components without touching profile checkpoints or menus.
        var allItems = FindObjectsByType<SJInteractable>(FindObjectsSortMode.None);
        var wastes = new List<SJInteractable>();
        var repairs = new List<SJInteractable>();
        foreach (var item in allItems)
        {
            if (item.kind == SJInteractable.Kind.Waste) wastes.Add(item);
            if (item.kind == SJInteractable.Kind.Repair) repairs.Add(item);
        }
        Check(wastes.Count == 5, "All five existing waste objects preserved");
        Check(repairs.Count == 2, "Both existing repair panels preserved");
        var bin = FindFirstObjectByType<SJBin>();
        if (wastes.Count == 0 || !bin) throw new Exception("Required waste or recycling bin missing.");

        var waste = wastes[0];
        player.zeroGravity = false;
        player.Teleport(new Vector3(0, .1f, 10), Quaternion.identity);
        waste.Body.linearVelocity = Vector3.zero;
        waste.Body.angularVelocity = Vector3.zero;
        waste.Body.position = player.view.transform.position + player.view.transform.forward * 2;
        Physics.SyncTransforms();
        Check(player.TryGrab(waste), "Existing grab action acquires waste after map replacement");
        yield return new WaitForSeconds(.35f);
        Check(player.held == waste && !waste.Body.useGravity, "Held waste follows player and remains weightless");
        Capture("05_Grab_Interaction");
        player.Release();
        Check(!player.held && waste.Body.useGravity, "Release restores interior gravity");
        Check(player.Push(waste), "Existing push interaction retained");
        yield return new WaitForFixedUpdate();
        Check(waste.Body.linearVelocity.sqrMagnitude > 1, "Push applies motion to waste");
        waste.Body.linearVelocity = Vector3.zero;
        waste.Body.position = bin.transform.position;
        Physics.SyncTransforms();
        yield return new WaitForSeconds(.3f);
        Check(waste.completed && game.collected == 1, "Recycling trigger collects deposited waste exactly once");
        Check(!bin.Collect(waste), "Recycling does not count the same item twice");
        foreach (var item in wastes) bin.Collect(item);
        foreach (var item in repairs)
        {
            item.RepairFor(1);
            Check(!item.completed, "Panel requires sustained repair: " + item.displayName);
            item.RepairFor(1.1f);
            item.RepairFor(1);
        }
        Check(game.collected == 5 && game.repairs == 2 && game.Finished, "Existing mission completion survives map change");
        if (game.canvasHUD)
        {
            game.canvasHUD.Refresh();
            Check(game.canvasHUD.missionCounts.text.Contains("5/5") && game.canvasHUD.missionCounts.text.Contains("2/2"),
                "Existing Canvas mission display updates correctly");
        }

        player.zeroGravity = true;
        player.tethered = false;
        player.Teleport(new Vector3(15.5f, 1, 0), Quaternion.identity);
        Check(player.InSpace, "Existing exterior shortcut still lands outside ship");
        float initialY = player.transform.position.y;
        Move(Vector2.zero, 1, 90);
        Check(player.transform.position.y > initialY + .75f, "Swimming movement still works outside");
        player.tethered = true;
        player.Teleport(new Vector3(30, 2, 2.5f), Quaternion.Euler(0, 90, 0));
        Move(Vector2.up, 0, 160, true);
        Check(Vector3.Distance(player.transform.position, player.tetherOrigin) <= SJPlayer.TetherLength + .15f,
            "Existing exterior safety tether still limits travel");
        player.Respawn();
        Check(!player.tethered && !player.InSpace && Vector3.Distance(player.transform.position, new Vector3(0, .1f, 10)) < .1f,
            "Emergency return reaches safe main corridor");

        Capture("06_Concourse_Forward", new Vector3(0, .1f, -10), new Vector3(0, 2, 45));
        Capture("07_Sealed_Room", new Vector3(4, .15f, 28), new Vector3(11, 2, 31));
        Capture("08_Exterior", new Vector3(47, 24, 59), new Vector3(0, 2, 8));
        Capture("09_Upper_Stairs", new Vector3(0, 2.2f, 50), new Vector3(0, 7, 60));
        player.Respawn();
        Check(!File.Exists(SJProfile.PathName), "Map QA has not written any profile file");
        yield return null;
    }
}
