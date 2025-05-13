using System;
using System.IO;
using System.Collections;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace Billiam.UEdit.ScreenshotTool
{
    [ExecuteInEditMode]
    public class ScreenshotCameraRig : MonoBehaviour
    {
        [Serializable]
        public class FrameSettings
        {
            [Header("Camera")]
            [Range(1, 179)]
            public float fov = 45;

            [Header("Rotation")]
            [Range(-180, 180)]
            public float pitch = 0;

            [Range(-180, 180)]
            public float yaw = 0;

            [Range(-180, 180)]
            public float roll = 0;

            [Header("Fine Control")]
            [Range(-1, 1)]
            public float xOffset = 0;

            [Range(-1, 1)]
            public float yOffset = 0;

            [Min(0)]
            public float zOffset = 20;

            public void Apply(Camera camera, Transform origin)
            {
                float x = Mathf.Cos(pitch * Mathf.Deg2Rad);
                float y = yaw * Mathf.Deg2Rad;
                float z = Mathf.Sin(pitch * Mathf.Deg2Rad);

                Vector3 direction = new Vector3(x, y, z).normalized;
                camera.transform.forward = direction;
                camera.transform.Rotate(new Vector3(0, 0, roll));
                camera.transform.localPosition = origin.position + -(direction * zOffset);

                camera.transform.localPosition += (camera.transform.right * xOffset + new Vector3(0, yOffset)) * zOffset;
                
                camera.fieldOfView = fov;
            }

            public FrameSettings CopyFrom(FrameSettings target)
            {
                fov = target.fov;
                pitch = target.pitch;
                yaw = target.yaw;
                pitch = target.roll;
                xOffset = target.xOffset;
                yOffset = target.yOffset;
                zOffset = target.zOffset;

                return this;
            }

            public void LerpTo(FrameSettings target, float timeStep)
            {
                fov = Mathf.Lerp(fov, target.fov, timeStep);
                pitch = Mathf.Lerp(pitch, target.pitch, timeStep);
                yaw = Mathf.Lerp(yaw, target.yaw, timeStep);
                roll = Mathf.Lerp(roll, target.roll, timeStep);
                zOffset = Mathf.Lerp(zOffset, target.zOffset, timeStep);
                xOffset = Mathf.Lerp(xOffset, target.xOffset, timeStep);
                yOffset = Mathf.Lerp(yOffset, target.yOffset, timeStep);
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
        public Camera cam;
        public FrameSettings frameSettings;
        public SceneSettings sceneSettings;

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
            FrameSettings interpolatedFrame = new FrameSettings().CopyFrom(frameSettings);
            float timestamp = Time.realtimeSinceStartup;

            while (true)
            {
                float deltaTime = Mathf.Max(Time.realtimeSinceStartup - timestamp, 0.001f);

                if (cam)
                {
                    interpolatedFrame.LerpTo(frameSettings, deltaTime * 20f);
                    interpolatedFrame.Apply(cam, transform);
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

            // Create screenshots folder
            string parentFolder = "Screenshots";
            if (!AssetDatabase.IsValidFolder(string.Format("Assets/{0}", parentFolder)))
            {
                AssetDatabase.CreateFolder("Assets", parentFolder);
            }

            // Get unique path
            string fileName = "New Screenshot";
            string path = AssetDatabase.GenerateUniqueAssetPath(string.Format("Assets/{0}/{1}.png", parentFolder, fileName));

            int width = Screen.width;
            int height = Screen.height;

            // Read screen to texture
            Texture2D screenTex = new Texture2D(width, height);
            screenTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenTex.Apply();

            // Encode texture to bytes
            byte[] bytes = screenTex.EncodeToPNG();
            DestroyImmediate(screenTex);

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
            if (!cam)
            {
                Debug.LogWarning("No camera assigned!");
                return;
            }

            sceneSettings.Apply(cam);
        }
#endif
    }
}
