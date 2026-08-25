using System.IO;
using UnityEditor;
using UnityEngine;

namespace RobotHunt.Editor.WorldTools
{
    public static class UnitySceneCameraCapturer
    {
        [MenuItem("Robot Hunt/Capture World Screenshots", false, 4)]
        public static void CaptureAllZoneScreenshots()
        {
            string outputFolder = Path.Combine(Application.dataPath, "RobotHunt/Screenshots");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            GameObject camObj = new GameObject("TempCaptureCamera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.farClipPlane = 1000f;

            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;

            // Camera View Positions
            var views = new (string name, Vector3 pos, Vector3 rot)[]
            {
                ("World_Overview", new Vector3(0f, 250f, -50f), new Vector3(75f, 0f, 0f)),
                ("City_Zone", new Vector3(-150f, 30f, -60f), new Vector3(30f, 35f, 0f)),
                ("Suburban_Zone", new Vector3(-70f, 25f, -50f), new Vector3(25f, 30f, 0f)),
                ("Farm_Zone", new Vector3(10f, 25f, -50f), new Vector3(25f, 30f, 0f)),
                ("Nature_Zone", new Vector3(90f, 30f, -50f), new Vector3(25f, 30f, 0f)),
                ("Survival_Zone", new Vector3(170f, 25f, -40f), new Vector3(25f, 30f, 0f))
            };

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            foreach (var view in views)
            {
                cam.transform.position = view.pos;
                cam.transform.rotation = Quaternion.Euler(view.rot);
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                string filePath = Path.Combine(outputFolder, $"{view.name}.png");
                File.WriteAllBytes(filePath, bytes);
                Debug.Log($"[CameraCapturer] Saved visual screenshot: {filePath}");
            }

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(tex);

            AssetDatabase.Refresh();
            Debug.Log($"[CameraCapturer] All 6 zone screenshots captured in Assets/RobotHunt/Screenshots/");
        }
    }
}
