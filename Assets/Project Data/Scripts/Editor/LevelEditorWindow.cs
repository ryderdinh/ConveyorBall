using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class LevelEditorWindow : EditorWindow
{
    private LevelData levelData;
    private SerializedObject serializedLevel;

    [MenuItem("Tools/Level Editor (Multi Conveyor)")]
    public static void Open()
    {
        GetWindow<LevelEditorWindow>("Level Editor").Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Multi Conveyor Level Editor", EditorStyles.boldLabel);

        levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);

        if (GUILayout.Button("Create New LevelData"))
        {
            string path = EditorUtility.SaveFilePanelInProject("Create LevelData", "NewLevelData", "asset", "Select save location for LevelData asset");
            if (!string.IsNullOrEmpty(path))
            {
                LevelData newData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(newData, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newData;
                levelData = newData;
            }
        }

        if (levelData != null)
        {
            if (GUILayout.Button("Open in Scene Tool"))
            {
                Selection.activeObject = levelData;
                EditorGUIUtility.PingObject(levelData);
            }

            if (GUILayout.Button("Open in MultiConveyorCreator"))
            {
                var creator = FindObjectOfType<MultiConveyorCreator>();
                if (creator == null)
                {
                    var go = new GameObject("MultiConveyorCreator");
                    creator = go.AddComponent<MultiConveyorCreator>();
                }
                creator.targetLevelData = levelData;
                Selection.activeGameObject = creator.gameObject;
            }

            if (GUILayout.Button("Print Conveyor Info"))
            {
                Debug.Log($"Conveyors: {levelData.conveyors.Count}, Teleports: {levelData.teleportPortals.Count}");

                for (int i = 0; i < levelData.conveyors.Count; i++)
                {
                    var c = levelData.conveyors[i];
                    Debug.Log($"Conveyor[{i}] Start: {c.startPoint}, Waypoints: {c.pathPoints.Count}, Max Balls: {c.maxBallCount}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a LevelData asset to begin editing, or create a new one.", MessageType.Info);
        }
    }
}