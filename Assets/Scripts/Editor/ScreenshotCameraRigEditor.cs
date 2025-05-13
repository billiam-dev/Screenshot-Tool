using UnityEngine;
using UnityEditor;

namespace Billiam.UEdit.ScreenshotTool
{
    [CustomEditor(typeof(ScreenshotCameraRig))]
    public class ScreenshotCameraRigEditor : Editor
    {
        private ScreenshotCameraRig cameraRig;

        private SerializedProperty cameraProperty;
        private SerializedProperty frameSettingsProperty;
        private SerializedProperty sceneSettingsProperty;

        void OnEnable()
        {
            cameraRig = (ScreenshotCameraRig)target;

            cameraProperty = serializedObject.FindProperty("cam");
            frameSettingsProperty = serializedObject.FindProperty("frameSettings");
            sceneSettingsProperty = serializedObject.FindProperty("sceneSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(cameraProperty, new GUIContent("Camera"));
            EditorGUILayout.PropertyField(frameSettingsProperty, new GUIContent("Frame"));
            EditorGUILayout.PropertyField(sceneSettingsProperty, new GUIContent("Scene"));

            GUILayout.Space(20);
            if (GUILayout.Button("Take Screenshot", GUILayout.Height(40)))
            {
                cameraRig.TakeScreenshot();
            }

            if (GUILayout.Button("Reset Scene"))
            {
                cameraRig.sceneSettings.ResetToDefault(cameraRig.cam);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            if (!cameraRig.cam)
            {
                return;
            }

            Handles.color = Color.red;

            Vector3 center = cameraRig.transform.position;
            center.y = cameraRig.cam.transform.position.y;
            Handles.DrawWireDisc(center, Vector3.up, Vector3.Distance(cameraRig.cam.transform.position, center));
        }
    }
}
