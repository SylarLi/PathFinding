using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class NavMeshTriangulationEditor : ScriptableObject
{
    [SerializeField]
    private NavMeshTriangulationObject mTriangulation;

    [SerializeField]
    private bool mShowNavMesh;

    [SerializeField]
    private List<NavMeshTriangle> mTriangles;

    private void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;
        if (mTriangles == null)
        {
            mTriangles = new List<NavMeshTriangle>();
        }
    }

    public NavMeshTriangulationObject triangulation
    {
        get
        {
            return mTriangulation;
        }
        set
        {
            mTriangulation = value;
            UpdateTriangulation();
            UpdateShowNavMesh();
        }
    }

    public bool showNavMesh
    {
        get
        {
            return mShowNavMesh;
        }
        set
        {
            if (mShowNavMesh != value)
            {
                mShowNavMesh = value;
                UpdateShowNavMesh();
            }
        }
    }

    private void UpdateTriangulation()
    {
        for (int i = mTriangles.Count - 1; i >= 0; i--)
        {
            if (mTriangles[i] != null)
            {
                GameObject.DestroyImmediate(mTriangles[i].gameObject);
            }
        }
        mTriangles.Clear();
        GameObject[] gos = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in gos)
        {
            if (go.name.StartsWith("NavMeshTriangl"))
            {
                GameObject.DestroyImmediate(go);
            }
        }
        if (mTriangulation.indices != null)
        {
            for (int i = 0; i < mTriangulation.indices.Length; i += 3)
            {
                int i1 = mTriangulation.indices[i];
                int i2 = mTriangulation.indices[i + 1];
                int i3 = mTriangulation.indices[i + 2];
                GameObject go = new GameObject(string.Format("NavMeshTriangle_{0}_{1}_{2}", i1, i2, i3));
                GameObject.DontDestroyOnLoad(go);
                NavMeshTriangle tri = go.AddComponent<NavMeshTriangle>();
                tri.indices = new int[] { i1, i2, i3 };
                tri.Setup(new Vector3[] { mTriangulation.vertices[i1], mTriangulation.vertices[i2], mTriangulation.vertices[i3] });
                mTriangles.Add(tri);
            }
        }
    }

    private void UpdateShowNavMesh()
    {
        mTriangles.ForEach((NavMeshTriangle each) =>
        {
            each.gameObject.hideFlags = showNavMesh ? HideFlags.NotEditable : HideFlags.NotEditable | HideFlags.HideInHierarchy;
            each.gameObject.SetActive(showNavMesh);
        });
    }
}
