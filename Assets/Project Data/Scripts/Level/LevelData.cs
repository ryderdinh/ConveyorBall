using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConveyorPathData
{
    public Vector3 startPoint;
    public List<Vector3> pathPoints = new();
    public int maxBallCount = 5;
}

[System.Serializable]
public class TeleportPortalData
{
    public Vector3 entryPoint;
    public Vector3 exitPoint;
    public int targetConveyorIndex;
    public int targetPathIndex;
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    public List<ConveyorPathData> conveyors = new();
    public List<TeleportPortalData> teleportPortals = new();
}