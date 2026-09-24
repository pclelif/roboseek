#if UNITY_EDITOR
using System;
using System.Reflection;
using Robot.Player.CameraControl;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run in a disposable editor: -batchmode -executeMethod CameraStabilityChecks.RunBatch
public static class CameraStabilityChecks
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float baselineDrift = CheckRig(true, 0, 35f, 15f);
            if (baselineDrift < 0.01f)
                throw new Exception("The regression stimulus did not expose the old follow lag.");
            for (int cadence = 0; cadence < 4; ++cadence)
                foreach (float yaw in new[] { -120f, 0f, 75f })
                    foreach (float pitch in new[] { -25f, 15f, 60f })
                        CheckRig(false, cadence, yaw, pitch);
            Debug.Log($"CAMERA CHECK: 36 movement/cadence/orbit cases passed; old relative drift={baselineDrift:F6}m");
            CheckAnimatedHull();

            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Maps/RoboSeek_City.unity");
            CheckCity();
            Debug.Log("CAMERA CHECK: PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static ThirdPersonCameraController CreateRig(Transform target)
    {
        var go = new GameObject("Camera stability probe");
        go.AddComponent<CinemachineCamera>();
        go.AddComponent<CinemachineOrbitalFollow>();
        go.AddComponent<CinemachineHardLookAt>(); // Existing scene component must be disabled.
        var rig = go.AddComponent<ThirdPersonCameraController>();
        Invoke(rig, "Awake");
        rig.SetTarget(target);
        return rig;
    }

    private static float CheckRig(bool oldPipeline, int cadence, float yaw, float pitch)
    {
        var target = new GameObject("Camera probe target");
        var rig = CreateRig(target.transform);
        Set(rig, "yaw", yaw);
        Set(rig, "pitch", pitch);
        var camera = rig.GetComponent<CinemachineCamera>();
        var orbit = rig.GetComponent<CinemachineOrbitalFollow>();
        Quaternion expectedRotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 expectedOffset = Vector3.up * 1.4f + expectedRotation * new Vector3(0, 0, -4.5f);
        float maxDrift = 0, maxAngle = 0;
        float elapsed = 0;
        for (int i = 0; i < 360; ++i)
        {
            float dt = cadence == 0 ? 1f / 60 : cadence == 1 ? 1f / 30 :
                cadence == 2 ? 1f / 120 : (i % 2 == 0 ? 1f / 30 : 1f / 120);
            elapsed += dt;
            // Run, stop, reverse; add millimetric terrain changes and body turns.
            float speed = i < 120 ? 6f : i < 180 ? 0f : -6f;
            target.transform.position += new Vector3(speed * dt, 0, speed * dt * 0.4f);
            var p = target.transform.position;
            p.y = i % 2 == 0 ? 0.002f : 0;
            target.transform.position = p;
            target.transform.rotation = Quaternion.Euler(0, i * 7f, 0);
            CinemachineCore.CurrentTimeOverride = elapsed;
            Invoke(rig, "ApplyOrbit");
            if (oldPipeline)
            {
                rig.GetComponent<CinemachineHardLookAt>().enabled = true;
                rig.GetComponent<CinemachineHardLookAt>().LookAtOffset = Vector3.up * 1.4f;
                orbit.TrackerSettings.PositionDamping = Vector3.one * 0.1f;
            }
            camera.InternalUpdateCameraState(Vector3.up, i == 0 ? -1 : dt);
            float drift = (camera.State.GetFinalPosition() - target.transform.position - expectedOffset).magnitude;
            float angle = Quaternion.Angle(camera.State.GetFinalOrientation(), expectedRotation);
            maxDrift = Mathf.Max(maxDrift, drift);
            maxAngle = Mathf.Max(maxAngle, angle);
        }
        UnityEngine.Object.DestroyImmediate(rig.gameObject);
        UnityEngine.Object.DestroyImmediate(target);
        CinemachineCore.CurrentTimeOverride = -1;
        if (!oldPipeline && (maxDrift > 0.0001f || maxAngle > 0.08f))
            throw new Exception($"Follow instability cadence={cadence}, yaw={yaw}, pitch={pitch}: {maxDrift}m / {maxAngle}deg");
        if (oldPipeline)
            Debug.Log($"CAMERA CHECK: old pipeline max offset error={maxDrift:F6}m, angle error={maxAngle:F4}deg");
        return maxDrift;
    }

    private static void CheckAnimatedHull()
    {
        var player = new GameObject("Player root");
        player.tag = "Player";
        player.AddComponent<CharacterController>();
        var body = player.AddComponent<Robot.Player.Movement.SolidRobotBody>();
        Invoke(body, "Awake");
        if (body.Hull.CompareTag("Player"))
            throw new Exception("Collision hull must not compete with the player root in tag lookup.");
        var rig = CreateRig(body.Hull.transform);
        var camera = rig.GetComponent<CinemachineCamera>();
        if (camera.Follow != player.transform)
            throw new Exception("SetTarget did not resolve the animated hull to the controller root.");
        rig.SnapToRoundStart(body.Hull.transform);
        Vector3 initial = camera.State.GetFinalPosition();
        for (int i = 0; i < 120; ++i)
        {
            body.Hull.transform.localPosition = new Vector3(Mathf.Sin(i) * .2f, i % 2 * .1f, Mathf.Cos(i) * .2f);
            body.Hull.transform.localRotation = Quaternion.Euler(i, i * 3, i * 2);
            // Also exercise a legacy/serialized child target.
            Set(rig, "target", body.Hull.transform);
            Invoke(rig, "LateUpdate");
            camera.InternalUpdateCameraState(Vector3.up, 1f / 60);
            if (camera.Follow != player.transform || Vector3.Distance(initial, camera.State.GetFinalPosition()) > .0001f)
                throw new Exception("Animated hull moved the camera while the player root stayed still.");
        }
        UnityEngine.Object.DestroyImmediate(rig.gameObject);
        UnityEngine.Object.DestroyImmediate(player);
        Debug.Log("CAMERA CHECK: untagged hull and 120 animated-child target frames passed (SetTarget, SnapToRoundStart, serialized target)");
    }

    private static void CheckCity()
    {
        var player = new GameObject("City physics probe");
        var body = player.AddComponent<CharacterController>();
        body.height = 1.8f;
        body.radius = 0.35f;
        body.center = Vector3.up * 0.9f;
        body.stepOffset = 0.3f;
        body.skinWidth = 0.035f;
        body.minMoveDistance = 0;
        player.transform.position = new Vector3(0, 0.1f, -2.5f);
        Physics.SyncTransforms();
        var rig = CreateRig(player.transform);
        var camera = rig.GetComponent<CinemachineCamera>();
        Vector3 expectedOffset = Vector3.up * 1.4f + Quaternion.Euler(15, 0, 0) * new Vector3(0, 0, -4.5f);
        float maxError = 0;
        int grounded = 0;
        for (int i = 0; i < 240; ++i)
        {
            float dt = i % 2 == 0 ? 1f / 30 : 1f / 120;
            body.Move(new Vector3(0, -2, 6) * dt);
            if (body.isGrounded) grounded++;
            Physics.SyncTransforms();
            Invoke(rig, "ApplyOrbit");
            camera.InternalUpdateCameraState(Vector3.up, i == 0 ? -1 : dt);
            maxError = Mathf.Max(maxError,
                (camera.State.GetFinalPosition() - player.transform.position - expectedOffset).magnitude);
        }
        if (maxError > 0.0001f || grounded == 0)
            throw new Exception($"City check failed: relative error={maxError}, grounded frames={grounded}");
        Debug.Log($"CAMERA CHECK: City 240 CharacterController steps; relative error={maxError:F7}m; grounded frames={grounded}; end={player.transform.position}");
    }

    private static void Invoke(object instance, string method) =>
        instance.GetType().GetMethod(method, PrivateInstance).Invoke(instance, null);
    private static void Set(object instance, string field, object value) =>
        instance.GetType().GetField(field, PrivateInstance).SetValue(instance, value);
}
#endif
