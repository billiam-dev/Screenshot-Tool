using UnityEngine;
using UnityEditor;

namespace JARcraft.UnityEditor.ScreenshotTool
{
    [CustomEditor(typeof(ScreenshotCameraRig))]
    public class ScreenshotCameraRigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ScreenshotCameraRig cameraRig = (ScreenshotCameraRig)target;

            GUILayout.Space(20);
            if (GUILayout.Button("Take Screenshot", GUILayout.Height(40)))
            {
                cameraRig.TakeScreenshot();
            }

            if (GUILayout.Button("Reset Scene"))
            {
                cameraRig.scene.ResetToDefault(cameraRig.screenshotCamera);
            }
        }
    }
}
