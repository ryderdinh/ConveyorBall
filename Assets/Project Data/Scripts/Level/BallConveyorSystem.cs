using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class BallConveyorSystem : MonoBehaviour
{
    public LevelData levelData;
    public GameObject ballPrefab;

    private List<GameObject> activeBalls = new();
    private bool hasQueuedBallAtStartPoint = false;
    private float safeDistance = 0.6f;

    private void Start()
    {
        RunConveyor();
    }

    private void RunConveyor()
    {
        if (levelData == null || levelData.conveyors.Count == 0) return;
        SpawnGhostBalls(0);
    }

    private void SpawnGhostBalls(int conveyorIndex)
    {
        var conveyor = levelData.conveyors[conveyorIndex];
        var path = conveyor.pathPoints;
        int max = conveyor.maxBallCount;

        float totalLength = 0f;
        List<float> segmentLengths = new();

        for (int i = 0; i < path.Count - 1; i++)
        {
            float len = Vector3.Distance(path[i], path[i + 1]);
            segmentLengths.Add(len);
            totalLength += len;
        }

        for (int i = 0; i < max; i++)
        {
            float t = (i + 0.5f) / max * totalLength;
            float acc = 0;

            for (int seg = 0; seg < segmentLengths.Count; seg++)
            {
                if (acc + segmentLengths[seg] >= t)
                {
                    float localT = (t - acc) / segmentLengths[seg];
                    Vector3 pos = Vector3.Lerp(path[seg], path[seg + 1], localT);

                    GameObject ball = Instantiate(ballPrefab, pos, Quaternion.identity);
                    var controller = ball.GetComponent<BallController>();
                    controller.Initialize(conveyorIndex, seg + 1, true);
                    MoveBall(ball, conveyorIndex, seg + 1).Forget();

                    activeBalls.Add(ball);
                    break;
                }
                acc += segmentLengths[seg];
            }
        }
    }

    [Button("Spawn Real Ball (Editor Test)")]
    public void SpawnRealBallFromEditor()
    {
        if (levelData == null || levelData.conveyors.Count == 0) return;

        int conveyorIndex = 0;
        var conveyor = levelData.conveyors[conveyorIndex];

        Vector3 spawnPos = conveyor.startPoint + Vector3.up * 2f;
        GameObject ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

        MoveSpawnBallToConveyor(ball, conveyorIndex).Forget();
    }

    private async UniTaskVoid MoveSpawnBallToConveyor(GameObject spawnBall, int conveyorIndex)
    {
        var conveyor = levelData.conveyors[conveyorIndex];
        Vector3 target = conveyor.startPoint;

        hasQueuedBallAtStartPoint = true;

        await spawnBall.transform.DOMove(target, 0.3f)
            .SetEase(Ease.OutQuad)
            .AsyncWaitForCompletion();

        BallController closestGhost = null;
        float minDist = float.MaxValue;

        foreach (var ball in activeBalls)
        {
            if (ball == null) continue;

            var ctrl = ball.GetComponent<BallController>();
            if (ctrl != null && ctrl.isGhost && ctrl.conveyorIndex == conveyorIndex)
            {
                float dist = Vector3.Distance(ball.transform.position, target);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestGhost = ctrl;
                }
            }
        }

        if (closestGhost != null)
        {
            closestGhost.ConvertToReal();
            Destroy(spawnBall);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy ghost ball phù hợp.");
        }

        hasQueuedBallAtStartPoint = false;
    }

    public void SpawnRealBallFromOutside(int conveyorIndex)
    {
        BallController closestGhost = null;
        float minDist = float.MaxValue;
        Vector3 start = levelData.conveyors[conveyorIndex].startPoint;

        foreach (var ball in activeBalls)
        {
            if (ball == null) continue;

            var ctrl = ball.GetComponent<BallController>();
            if (ctrl != null && ctrl.isGhost && ctrl.conveyorIndex == conveyorIndex)
            {
                float dist = Vector3.Distance(ball.transform.position, start);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestGhost = ctrl;
                }
            }
        }

        if (closestGhost != null)
        {
            closestGhost.ConvertToReal();
        }
    }

    private async UniTaskVoid MoveBall(GameObject ball, int conveyorIndex, int pathIndex)
    {
        while (true)
        {
            var conveyor = levelData.conveyors[conveyorIndex];
            var path = conveyor.pathPoints;

            if (pathIndex >= path.Count)
            {
                pathIndex = 0;
            }

            var target = path[pathIndex];
            var ctrl = ball.GetComponent<BallController>();

            // Dừng real ball nếu đang gần startPoint và có hàng đợi
            if (!ctrl.isGhost)
            {
                var startPoint = conveyor.startPoint;
                float distToStart = Vector3.Distance(ball.transform.position, startPoint);

                while (hasQueuedBallAtStartPoint && distToStart < safeDistance)
                {
                    await UniTask.Yield();
                    distToStart = Vector3.Distance(ball.transform.position, startPoint);
                }
            }

            await ball.transform
                .DOMove(target, 5f)
                .SetEase(Ease.Linear)
                .SetSpeedBased()
                .AsyncWaitForCompletion();

            ball.transform.position = target;
            pathIndex++;

            if (ctrl != null)
            {
                ctrl.currentPathIndex = pathIndex;
            }
        }
    }
}
