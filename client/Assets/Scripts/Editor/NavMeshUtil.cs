using System.Collections.Generic;
using UnityEngine;

public sealed class NavMeshUtil
{
    public static void FilterDuplicate(NavMeshTriangulation nt, out List<Vector3> vertices, out List<int> indices, out List<int> layers)
    {
        vertices = new List<Vector3>(nt.vertices);
        indices = new List<int>(nt.indices);
        layers = new List<int>(nt.layers);
        int index = 0;
        while (index < vertices.Count)
        {
            for (int i = vertices.Count - 1; i > index; i--)
            {
                if (Vector3.Distance(vertices[i], vertices[index]) < 0.001f)
                {
                    vertices.RemoveAt(i);
                    for (int s = indices.Count - 1; s >= 0; s--)
                    {
                        if (indices[s] == i)
                        {
                            indices[s] = index;
                        }
                        else if (indices[s] > i)
                        {
                            indices[s] -= 1;
                        }
                    }
                }
            }
            index += 1;
        }
    }
}
