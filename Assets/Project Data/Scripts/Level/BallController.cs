using UnityEngine;

public class BallController : MonoBehaviour
{
    public bool isGhost = true;

    [SerializeField] private GameObject ghostVisual;
    [SerializeField] private GameObject realVisual;

    public int conveyorIndex;
    public int currentPathIndex;

    public void Initialize(int conveyorIndex, int pathIndex, bool ghost)
    {
        this.conveyorIndex = conveyorIndex;
        this.currentPathIndex = pathIndex;
        isGhost = ghost;
        UpdateVisual();
    }

    public void ConvertToReal()
    {
        isGhost = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (ghostVisual != null) ghostVisual.SetActive(isGhost);
        if (realVisual != null) realVisual.SetActive(!isGhost);
    }
}