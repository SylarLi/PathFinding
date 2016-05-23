using UnityEditor;
using UnityEngine;

[System.Serializable]
[ExecuteInEditMode]
public class NavMeshTriangle : MonoBehaviour
{
    private static readonly Color ColorNormal = new Color(0, 1, 0, 0.6f);
    private static readonly Color ColorSelected = new Color(1, 0, 0, 0.6f);

    public int[] indices;
    public Vector3[] points;

    [SerializeField]
    private NavMeshTriangleState mState;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    public void Setup(Vector3[] points)
    {
        this.points = points;

        Mesh mesh = new Mesh();
        mesh.vertices = points;
        mesh.uv = System.Array.ConvertAll<Vector3, Vector2>(mesh.vertices, (Vector3 each) => new Vector2(each.x, each.z));
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Transparent/Diffuse"));
        material.color = ColorNormal;
        meshRenderer.sharedMaterial = material;
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
    }

    private void OnDrawGizmos()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        Color tcolor = System.Array.IndexOf(Selection.gameObjects, gameObject) != -1 ? ColorSelected : ColorNormal;
        float fader = mState == NavMeshTriangleState.Removed ? 0.4f : 1f;
        meshRenderer.sharedMaterial.color = tcolor * fader;
        Gizmos.color = Color.black * fader;
        Gizmos.DrawLine(points[0], points[1]);
        Gizmos.DrawLine(points[1], points[2]);
        Gizmos.DrawLine(points[2], points[0]);
    }

    public NavMeshTriangleState state
    {
        get
        {
            return mState;
        }
        set
        {
            if (mState != value)
            {
                mState = value;
            }
        }
    }
}
