using System;
using System.IO;
using System.Collections;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace JARcraft.UnityEditor.ScreenshotTool
{
    [ExecuteInEditMode]
    public class ScreenshotCameraRig : MonoBehaviour
    {
        [Serializable]
        public class FrameSettings
        {
            [Range(-180, 180)]
            public float pitch = 0;

            [Range(-180, 180)]
            public float yaw = 0;

            [Range(-180, 180)]
            public float roll = 0;

            [Min(1)]
            public float zDistance = 20;

            [Range(1, 179)]
            public float fov = 45;

            public void Apply(Camera camera, Transform origin)
            {
                float x = Mathf.Cos(pitch * Mathf.Deg2Rad);
                float y = yaw * Mathf.Deg2Rad;
                float z = Mathf.Sin(pitch * Mathf.Deg2Rad);

                Vector3 direction = new Vector3(x, y, z).normalized;
                camera.transform.forward = direction;
                camera.transform.Rotate(new Vector3(0, 0, roll));
                camera.transform.localPosition = origin.position + -(direction * zDistance);

                camera.fieldOfView = fov;
            }

            public FrameSettings CopyFrom(FrameSettings target)
            {
                pitch = target.pitch;
                yaw = target.yaw;
                pitch = target.roll;
                zDistance = target.zDistance;

                return this;
            }

            public void LerpTo(FrameSettings target, float timeStep)
            {
                pitch = Mathf.Lerp(pitch, target.pitch, timeStep);
                yaw = Mathf.Lerp(yaw, target.yaw, timeStep);
                roll = Mathf.Lerp(roll, target.roll, timeStep);
                zDistance = Mathf.Lerp(zDistance, target.zDistance, timeStep);
                fov = Mathf.Lerp(fov, target.fov, timeStep);
            }
        }

        [Serializable]
        public class SceneSettings
        {
            public Material skybox;
            [Range(0, 8)]
            public float environmentLightIntensity;

            public bool fogEnabled;
            public Color fogColor;

            [ColorUsage(false, false)]
            public Color cameraBackground;

            public void Apply(Camera camera)
            {
                RenderSettings.skybox = skybox;
                RenderSettings.ambientIntensity = environmentLightIntensity;

                RenderSettings.fog = fogEnabled;
                RenderSettings.fogColor = fogColor;

                camera.backgroundColor = cameraBackground;
            }

            public void ResetToDefault(Camera camera)
            {
                skybox = null;
                environmentLightIntensity = 1;

                fogEnabled = false;
                fogColor = new Color(0.5f, 0.5f, 0.5f);

                cameraBackground = Color.green;

                Apply(camera);
            }
        }

#if UNITY_EDITOR
        public Camera screenshotCamera;

        public FrameSettings frame;
        public SceneSettings scene;

        EditorCoroutine updateRigRoutine;

        void OnEnable()
        {
            if (updateRigRoutine == null)
            {
                updateRigRoutine = EditorCoroutineUtility.StartCoroutine(UpdateRig(), this);
            }
        }

        void OnDisable()
        {
            if (updateRigRoutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(updateRigRoutine);
                updateRigRoutine = null;
            }
        }

        IEnumerator UpdateRig()
        {
            FrameSettings interpolatedFrame = new FrameSettings().CopyFrom(frame);
            float timestamp = Time.realtimeSinceStartup;

            while (true)
            {
                float deltaTime = Mathf.Max(Time.realtimeSinceStartup - timestamp, 0.001f);

                if (screenshotCamera)
                {
                    interpolatedFrame.LerpTo(frame, deltaTime * 20f);
                    interpolatedFrame.Apply(screenshotCamera, transform);
                }

                timestamp = Time.realtimeSinceStartup;
                yield return null;
            }
        }

        public void TakeScreenshot()
        {
            StartCoroutine(TakeScreenshotAsync());
        }

        IEnumerator TakeScreenshotAsync()
        {
            // We should only read the screen buffer after rendering is complete
            yield return new WaitForEndOfFrame();

            string fileName = "New Screenshot";

            // Create screenshots folder
            string parentFolder = "Screenshots";
            if (!AssetDatabase.IsValidFolder(string.Format("Assets/{0}", parentFolder)))
            {
                AssetDatabase.CreateFolder("Assets", parentFolder);
            }

            // Get unique path
            string path = AssetDatabase.GenerateUniqueAssetPath(string.Format("Assets/{0}/{1}.png", parentFolder, fileName));

            int width = Screen.width;
            int height = Screen.height;

            // Read screen to texture
            Texture2D screenCapture = new Texture2D(width, height);
            screenCapture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenCapture.Apply();

            // Encode texture to bytes
            byte[] bytes = screenCapture.EncodeToPNG();
            DestroyImmediate(screenCapture);

            // Write bytes to disc
            File.WriteAllBytes(path, bytes);

            // Save assets
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            // Highlight screenshot in project window
            Selection.activeObject = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            EditorUtility.FocusProjectWindow();

            Debug.Log("Saved screenshot as " + path);
        }

        void OnValidate()
        {
            if (!screenshotCamera)
            {
                Debug.LogWarning("No camera assigned!");
                return;
            }

            scene.Apply(screenshotCamera);
        }
#endif
    }
}
