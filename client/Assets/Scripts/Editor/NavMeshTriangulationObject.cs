using UnityEngine;

[System.Serializable]
public class NavMeshTriangulationObject : ScriptableObject
{
    public int[] indices;
    public int[] layers;
    public Vector3[] vertices;

    private void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;
    }

    public static NavMeshTriangulationObject Empty
    {
        get
        {
            return new NavMeshTriangulationObject();
        }
    }
}
