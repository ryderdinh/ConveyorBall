using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MultiConveyorCreator : MonoBehaviour
{
    [System.Serializable]
    public class ConveyorGroup
    {
        public string name = "Conveyor";
        public Transform startPoint;
        public List<Transform> waypoints = new();
        [Min(1)] public int maxBallCount = 5;
    }

    [System.Serializable]
    public class TeleportLink
    {
        public Transform entry;
        public Transform exit;
        public int targetConveyorIndex;
        public int targetPathIndex;
    }

    public LevelData targetLevelData;
    public List<ConveyorGroup> conveyors = new();
    public List<TeleportLink> teleportLinks = new();

#if UNITY_EDITOR
    [Button("Save LevelData (Conveyors + Teleports)")]
    public void SaveLevelData()
    {
        if (targetLevelData == null)
        {
            Debug.LogWarning("No LevelData assigned.");
            return;
        }

        targetLevelData.conveyors = new List<ConveyorPathData>();

        foreach (var group in conveyors)
        {
            ConveyorPathData data = new()
            {
                startPoint = group.startPoint != null ? group.startPoint.position : Vector3.zero,
                pathPoints = new List<Vector3>(),
                maxBallCount = group.maxBallCount
            };

            foreach (var point in group.waypoints)
            {
                if (point != null)
                    data.pathPoints.Add(point.position);
            }

            targetLevelData.conveyors.Add(data);
        }

        targetLevelData.teleportPortals = new List<TeleportPortalData>();

        foreach (var tp in teleportLinks)
        {
            if (tp.entry == null || tp.exit == null) continue;

            var portal = new TeleportPortalData
            {
                entryPoint = tp.entry.position,
                exitPoint = tp.exit.position,
                targetConveyorIndex = tp.targetConveyorIndex,
                targetPathIndex = tp.targetPathIndex
            };

            targetLevelData.teleportPortals.Add(portal);
        }

        Debug.Log($"Saved {conveyors.Count} conveyors and {teleportLinks.Count} teleports to LevelData.");
        EditorUtility.SetDirty(targetLevelData);
        AssetDatabase.SaveAssets();
    }

    [Button("Load From LevelData")]
    public void LoadFromLevelData()
    {
        if (targetLevelData == null)
        {
            Debug.LogWarning("No LevelData assigned.");
            return;
        }

        // Clear existing scene objects
        foreach (var group in conveyors)
        {
            if (group.startPoint != null)
                DestroyImmediate(group.startPoint.gameObject);
            foreach (var wp in group.waypoints)
            {
                if (wp != null)
                    DestroyImmediate(wp.gameObject);
            }
        }
        conveyors.Clear();

        foreach (var tp in teleportLinks)
        {
            if (tp.entry != null)
                DestroyImmediate(tp.entry.gameObject);
            if (tp.exit != null)
                DestroyImmediate(tp.exit.gameObject);
        }
        teleportLinks.Clear();

        for (int i = 0; i < targetLevelData.conveyors.Count; i++)
        {
            var data = targetLevelData.conveyors[i];
            CreateConveyorFromData(i, data);
        }

        foreach (var tp in targetLevelData.teleportPortals)
        {
            GameObject group = new GameObject("TeleportLink_" + teleportLinks.Count);
            group.transform.SetParent(transform);

            GameObject entry = new GameObject("Entry");
            GameObject exit = new GameObject("Exit");

            entry.transform.SetParent(group.transform);
            exit.transform.SetParent(group.transform);

            entry.transform.position = tp.entryPoint;
            exit.transform.position = tp.exitPoint;

            TeleportLink link = new TeleportLink
            {
                entry = entry.transform,
                exit = exit.transform,
                targetConveyorIndex = tp.targetConveyorIndex,
                targetPathIndex = tp.targetPathIndex
            };

            teleportLinks.Add(link);
        }
    }

    private void CreateConveyorFromData(int index, ConveyorPathData data)
    {
        GameObject parent = new GameObject("Conveyor_Group_" + index);
        parent.transform.SetParent(transform);

        GameObject startObj = new GameObject("StartPoint");
        startObj.transform.position = data.startPoint;
        startObj.transform.SetParent(parent.transform);

        ConveyorGroup group = new ConveyorGroup
        {
            name = parent.name,
            startPoint = startObj.transform,
            waypoints = new List<Transform>(),
            maxBallCount = data.maxBallCount
        };

        for (int i = 0; i < data.pathPoints.Count; i++)
        {
            GameObject wp = new GameObject("Waypoint_" + i);
            wp.transform.position = data.pathPoints[i];
            wp.transform.SetParent(parent.transform);
            group.waypoints.Add(wp.transform);
        }

        conveyors.Add(group);
    }

    [Button("Create Sample Conveyor")]
    public void CreateSampleConveyor()
    {
        CreateConveyor(new Vector3(0, 0, 0), new List<Vector3>
        {
            new Vector3(2, 0, 0),
            new Vector3(4, 0, 0),
            new Vector3(6, 0, 0)
        });
    }

    public void CreateConveyor(Vector3 start, List<Vector3> waypoints)
    {
        GameObject parent = new GameObject("Conveyor_Group_" + conveyors.Count);
        parent.transform.SetParent(transform);

        GameObject startObj = new GameObject("StartPoint");
        startObj.transform.position = start;
        startObj.transform.SetParent(parent.transform);

        ConveyorGroup group = new ConveyorGroup
        {
            name = parent.name,
            startPoint = startObj.transform,
            waypoints = new List<Transform>(),
            maxBallCount = 5
        };

        for (int i = 0; i < waypoints.Count; i++)
        {
            GameObject wp = new GameObject("Waypoint_" + i);
            wp.transform.position = waypoints[i];
            wp.transform.SetParent(parent.transform);
            group.waypoints.Add(wp.transform);
        }

        conveyors.Add(group);
    }

    public void CreatePolygonConveyor(int sides, float radius, Vector3 center = default)
    {
        if (sides < 3)
        {
            Debug.LogWarning("Polygon must have at least 3 sides.");
            return;
        }

        List<Vector3> points = new();
        float angleStep = 360f / sides;

        for (int i = 0; i < sides; i++)
        {
            float angleRad = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Cos(angleRad) * radius;
            float z = Mathf.Sin(angleRad) * radius;
            points.Add(center + new Vector3(x, 0, z));
        }

        points.Add(points[0]);
        CreateConveyor(points[0], points.GetRange(1, points.Count - 1));
    }

    [Button(ButtonSizes.Medium), GUIColor(0.3f, 0.8f, 1f)]
    public void CreateConveyor([MinValue(3)] int side = 6, [MinValue(0.5f)] float radius = 3f)
    {
        CreatePolygonConveyor(side, radius);
    }

    [Button("Create Teleport Link")]
    public void CreateTeleportLink()
    {
        if (conveyors.Count == 0 || conveyors[0].waypoints.Count < 2)
        {
            Debug.LogWarning("Conveyor 0 không đủ waypoint để tạo teleport.");
            return;
        }

        var conveyor = conveyors[0];

        GameObject group = new GameObject("TeleportLink_" + teleportLinks.Count);
        group.transform.SetParent(transform);

        GameObject entry = new GameObject("Entry");
        GameObject exit = new GameObject("Exit");

        entry.transform.SetParent(group.transform);
        exit.transform.SetParent(group.transform);

        entry.transform.position = conveyor.waypoints[^1].position;
        exit.transform.position = conveyor.waypoints[0].position;

        TeleportLink link = new TeleportLink
        {
            entry = entry.transform,
            exit = exit.transform,
            targetConveyorIndex = 0,
            targetPathIndex = 0
        };

        teleportLinks.Add(link);
    }

#endif

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var conveyor in conveyors)
        {
            if (conveyor.startPoint != null)
            {
                Gizmos.DrawSphere(conveyor.startPoint.position, 0.2f);
#if UNITY_EDITOR
                Handles.Label(conveyor.startPoint.position + Vector3.up * 0.2f, "Start");
#endif
            }

            for (int i = 0; i < conveyor.waypoints.Count; i++)
            {
                var wp = conveyor.waypoints[i];
                if (wp != null)
                {
                    Gizmos.DrawSphere(wp.position, 0.15f);
                    if (i < conveyor.waypoints.Count - 1 && conveyor.waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(wp.position, conveyor.waypoints[i + 1].position);
                    }
#if UNITY_EDITOR
                    Handles.Label(wp.position + Vector3.up * 0.2f, $"{i}");
#endif
                }
            }
        }

        Gizmos.color = Color.magenta;
        foreach (var link in teleportLinks)
        {
            if (link.entry != null && link.exit != null)
            {
                Gizmos.DrawSphere(link.entry.position, 0.15f);
                Gizmos.DrawSphere(link.exit.position, 0.15f);
                Gizmos.DrawLine(link.entry.position, link.exit.position);
            }
        }
    }
}
