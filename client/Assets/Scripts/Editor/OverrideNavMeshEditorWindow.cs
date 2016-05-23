using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[OverrideInternalEditorWindowTypeMark("NavMeshEditorWindow")]
public class OverrideNavMeshEditorWindow : OverrideInternalEditorWindow
{
    private static readonly Vector2 BoundsWindowSize = new Vector2(200, 100);

    [SerializeField]
    private Rect bounds;

    [SerializeField]
    private Vector2 close;

    [SerializeField]
    private NavMeshTriangulationEditor navMeshTri;

    [SerializeField]
    private bool editMode;

    [MenuItem("Window/Navigation E")]
    public static void Open()
    {
        OverrideNavMeshEditorWindow window = OverrideNavMeshEditorWindow.GetWindow<OverrideNavMeshEditorWindow>();
        window.title = "Navigation E";
        window.Show();
    }

    protected override void OnBecameVisible()
    {
        PropertyInfo info = GetNavMeshVisualizationSetting("showNavigation");
        if (!(bool)info.GetValue(null, null) && !editMode)
        {
            info.SetValue(null, true, null);
            InvokeInternalMethod("RepaintSceneAndGameViews");
        }
        EditorApplication.playmodeStateChanged += OnPlayModeChange;
        navMeshTri.showNavMesh = editMode && !Application.isPlaying;
    }

    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        EditorApplication.playmodeStateChanged -= OnPlayModeChange;
        navMeshTri.showNavMesh = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (SceneView.onSceneGUIDelegate != null)
        {
            Delegate[] delegates = SceneView.onSceneGUIDelegate.GetInvocationList();
            foreach (Delegate d in delegates)
            {
                if (d.Target == baseWindow)
                {
                    SceneView.onSceneGUIDelegate -= (SceneView.OnSceneFunc)d;
                }
            }
        }
        SceneView.onSceneGUIDelegate += OnSceneViewGUI;
        if (EditorApplication.searchChanged != null)
        {
            Delegate[] delegates = EditorApplication.searchChanged.GetInvocationList();
            foreach (Delegate d in delegates)
            {
                if (d.Target == baseWindow)
                {
                    EditorApplication.searchChanged -= (EditorApplication.CallbackFunction)d;
                }
            }
        }
        EditorApplication.searchChanged += Repaint;
        if (navMeshTri == null)
        {
            navMeshTri = NavMeshTriangulationEditor.CreateInstance<NavMeshTriangulationEditor>();
        }
    }

    protected virtual void OnDisable()
    {
        base.OnDisable();
        SceneView.onSceneGUIDelegate -= OnSceneViewGUI;
        EditorApplication.searchChanged -= Repaint;
    }

    protected override void OnSelectionChange()
    {
        base.OnSelectionChange();
        Repaint();
    }

    protected override void OnGUI()
    {
        base.OnGUI();
        GUI.enabled = !Application.isPlaying;
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        editMode = GUILayout.Toggle(editMode, "Edit", EditorStyles.toolbarButton);
        if (EditorGUI.EndChangeCheck())
        {
            OnEditModeUpdate();
        }
        if (GUILayout.Button("Revert", EditorStyles.toolbarButton))
        {
            navMeshTri.triangulation = NavMeshTriangulationObject.Empty;
            editMode = false;
            OnEditModeUpdate();
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
        GUILayout.BeginHorizontal();
        GUI.color = Color.yellow;
        if (GUILayout.Button("Export", EditorStyles.toolbarButton))
        {
            string path = EditorUtility.SaveFilePanel("选择保存路径", "", "NetMesh.data", "data");
            try
            {
                BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create), System.Text.Encoding.UTF8);
                NavMeshTriangulation t = NavMesh.CalculateTriangulation();
                List<Vector3> vertices = null;
                List<int> indices = null;
                List<int> layers = null;
                NavMeshUtil.FilterDuplicate(t, out vertices, out indices, out layers);
                Debug.Log("顶点数量: " + vertices.Count);
                writer.Write(vertices.Count);
                vertices.ToList<Vector3>().ForEach((Vector3 each) =>
                {
                    writer.Write(each.x);
                    writer.Write(each.y);
                    writer.Write(each.z);
                });
                writer.Write(layers.Count);
                Debug.Log("三角形数量: " + layers.Count);
                indices.ToList<int>().ForEach((int each) =>
                {
                    writer.Write(each);
                });
                layers.ToList<int>().ForEach((int each) =>
                {
                    writer.Write(each);
                });
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
            }
        }
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }

    protected override void OnSceneViewGUI(SceneView sceneView)
    {
        if (!editMode)
        {
            base.OnSceneViewGUI(sceneView);
        }
        else if (navMeshTri.showNavMesh)
        {
            Handles.BeginGUI();
            OnBoundsWindowGUI(sceneView);
            Handles.EndGUI();
            if (!Application.isPlaying)
            {
                Event e = Event.current;
                if (e.type == EventType.keyUp && e.keyCode == KeyCode.Space)
                {
                    GameObject[] selections = Selection.gameObjects;
                    if (selections != null)
                    {
                        selections = Array.FindAll<GameObject>(selections, (GameObject each) => each.GetComponent<NavMeshTriangle>() != null);
                        foreach (GameObject selection in selections)
                        {
                            if (selection != null)
                            {
                                NavMeshTriangle tri = selection.GetComponent<NavMeshTriangle>();
                                tri.state = (NavMeshTriangleState)(1 - (int)tri.state);
                            }
                        }
                    }
                    e.Use();
                    EditorUtility.SetDirty(navMeshTri);
                }
            }
        }
    }

    private void OnEditModeUpdate()
    {
        PropertyInfo info = GetNavMeshVisualizationSetting("showNavigation");
        info.SetValue(null, !editMode, null);
        if (navMeshTri != null)
        {
            if (editMode)
            {
                if (navMeshTri.triangulation.vertices == null || navMeshTri.triangulation.vertices.Length == 0)
                {
                    NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                    NavMeshTriangulationObject trio = NavMeshTriangulationObject.CreateInstance<NavMeshTriangulationObject>();
                    trio.vertices = tri.vertices;
                    trio.indices = tri.indices;
                    trio.layers = tri.layers;
                    navMeshTri.triangulation = trio;
                }
            }
            navMeshTri.showNavMesh = editMode;
        }
        EditorUtility.SetDirty(navMeshTri);
    }

    private void OnBoundsWindowGUI(SceneView sceneView)
    {
        Rect screenRect = new Rect(Screen.width - BoundsWindowSize.x - 25, Screen.height - BoundsWindowSize.y - 20, BoundsWindowSize.x, BoundsWindowSize.y);
        GUILayout.Window(0, screenRect,
        (int id) =>
        {
            int LabelWidth = 50;

            GUI.enabled = !Application.isPlaying;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("xMin", GUILayout.Width(LabelWidth));
            bounds.xMin = EditorGUILayout.FloatField(bounds.xMin);
            EditorGUILayout.LabelField("zMin", GUILayout.Width(LabelWidth));
            bounds.yMin = EditorGUILayout.FloatField(bounds.yMin);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("xMax", GUILayout.Width(LabelWidth));
            bounds.xMax = EditorGUILayout.FloatField(bounds.xMax);
            EditorGUILayout.LabelField("zMax", GUILayout.Width(LabelWidth));
            bounds.yMax = EditorGUILayout.FloatField(bounds.yMax);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("xClose", GUILayout.Width(LabelWidth));
            close.x = EditorGUILayout.FloatField(close.x);
            EditorGUILayout.LabelField("zClose", GUILayout.Width(LabelWidth));
            close.y = EditorGUILayout.FloatField(close.y);
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CloseScale"))
            {
                Vector3[] vertices = navMeshTri.triangulation.vertices;
                if (vertices != null)
                {
                    for (int i = vertices.Length - 1; i >= 0; i--)
                    {
                        Vector3 vertice = vertices[i];
                        if (Mathf.Abs(vertice.x - bounds.xMin) < close.x)
                        {
                            vertice.x = bounds.xMin;
                        }
                        if (Mathf.Abs(vertice.x - bounds.xMax) < close.x)
                        {
                            vertice.x = bounds.xMax;
                        }
                        if (Mathf.Abs(vertice.z - bounds.yMin) < close.y)
                        {
                            vertice.z = bounds.yMin;
                        }
                        if (Mathf.Abs(vertice.z - bounds.yMax) < close.y)
                        {
                            vertice.z = bounds.yMax;
                        }
                        vertices[i] = vertice;
                    }
                }
                navMeshTri.triangulation = navMeshTri.triangulation;
                EditorUtility.SetDirty(navMeshTri);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        },
        "Bounds");
    }

    private PropertyInfo GetNavMeshVisualizationSetting(string name)
    {
        return GetUnityEditorInternalType("UnityEditor.NavMeshVisualizationSettings").GetProperty("showNavigation", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    }

    private void OnPlayModeChange()
    {
        navMeshTri.showNavMesh = editMode && !Application.isPlaying;
        Repaint();
    }
}