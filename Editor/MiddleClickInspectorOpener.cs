using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;

namespace MiddleClickInspector
{
    [InitializeOnLoad]
    public static class MiddleClickInspectorOpener
    {
        static MiddleClickInspectorOpener()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            HandleMiddleClick(instanceID, selectionRect);
        }

        private static void OnProjectGUI(string guid, Rect selectionRect)
        {
            // GUIDからインスタンスIDを取得
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null)
                {
                    int instanceID = obj.GetInstanceID();
                    HandleMiddleClick(instanceID, selectionRect);
                }
            }
        }

        private static void HandleMiddleClick(int instanceID, Rect selectionRect)
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && e.button == 2 && selectionRect.Contains(e.mousePosition))
            {
                UnityEngine.Object clickedObject = EditorUtility.InstanceIDToObject(instanceID);
                
                if (clickedObject != null)
                {
                    OpenNewInspector(clickedObject);
                    e.Use();
                }
            }
        }

        public static void OpenNewInspector(UnityEngine.Object target)
        {
            if (target == null) return;

            UnityEngine.Object[] previousSelection = Selection.objects;
            Type inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            
            if (inspectorType != null)
            {
                EditorWindow inspectorWindow = EditorWindow.CreateInstance(inspectorType) as EditorWindow;
                
                if (inspectorWindow != null)
                {
                    inspectorWindow.Show();
                    Selection.activeObject = target;
                    EditorApplication.delayCall += () =>
                    {
                        inspectorWindow.Repaint();
                        SetInspectorLocked(inspectorWindow, inspectorType, true);
                        EditorApplication.delayCall += () =>
                        {
                            Selection.objects = previousSelection;
                            // うごかない
                            // inspectorWindow.titleContent = new GUIContent($"Inspector ({target.name}) 🔒");
                        };
                    };
                }
            }
        }

        private static void SetInspectorLocked(EditorWindow inspectorWindow, Type inspectorType, bool locked)
        {
            if (inspectorWindow == null || inspectorType == null) return;

            try
            {
                PropertyInfo isLockedProp = inspectorType.GetProperty("isLocked", 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (isLockedProp != null && isLockedProp.CanWrite)
                {
                    isLockedProp.SetValue(inspectorWindow, locked, null);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set inspector lock state: {ex.Message}");
            }
        }
    }
}
