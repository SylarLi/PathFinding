using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class OverrideInternalEditorWindow : EditorWindow
{
    private Type _baseType;

    private EditorWindow _baseWindow;

    public Type baseType
    {
        get
        {
            if (_baseType == null)
            {
                OverrideInternalEditorWindowTypeMarkAttribute attr = Attribute.GetCustomAttribute(GetType(), typeof(OverrideInternalEditorWindowTypeMarkAttribute)) as OverrideInternalEditorWindowTypeMarkAttribute;
                if (attr != null)
                {
                    _baseType = attr.type;
                }
            }
            return _baseType;
        }
    }

    public EditorWindow baseWindow
    {
        get
        {
            if (_baseWindow == null)
            {
                DestroyBaseTypeWindow();
                _baseWindow = EditorWindow.CreateInstance(baseType) as EditorWindow;
            }
            return _baseWindow;
        }
    }

    private void DestroyBaseTypeWindow()
    {
        UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(baseType);
        if (all != null)
        {
            foreach (UnityEngine.Object each in all)
            {
                UnityEngine.Object.DestroyImmediate(each);
            }
        }
    }

    protected virtual void OnBecameVisible()
    {
        InvokeInternalMethod("OnBecameVisible");
    }

    protected virtual void OnBecameInvisible()
    {
        InvokeInternalMethod("OnBecameInvisible");
    }

    protected virtual void OnEnable()
    {
        InvokeInternalMethod("OnEnable");
        hideFlags = HideFlags.HideAndDontSave;
    }

    protected virtual void OnDisable()
    {
        InvokeInternalMethod("OnDisable");
    }

    protected virtual void OnGUI()
    {
        InvokeInternalMethod("OnGUI");
    }

    protected virtual void OnSceneViewGUI(SceneView sceneView)
    {
        InvokeInternalMethod("OnSceneViewGUI", sceneView);
    }

    protected virtual void OnSelectionChange()
    {
        InvokeInternalMethod("OnSelectionChange");
    }

    protected virtual void OnDestroy()
    {

    }

    protected void InvokeInternalMethod(string methodName, params object[] parameters)
    {
        MethodInfo method = baseWindow.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        method.Invoke(baseWindow, parameters);
    }

    protected Type GetUnityEditorInternalType(string typeName)
    {
        return typeof(EditorWindow).Assembly.GetType(typeName);
    }
}

public class OverrideInternalEditorWindowTypeMarkAttribute : Attribute
{
    private Type _type;

    public OverrideInternalEditorWindowTypeMarkAttribute(string typeName)
    {
        Type[] types = Assembly.GetAssembly(typeof(EditorWindow)).GetTypes();
        foreach (Type type in types)
        {
            if (type.Name == typeName)
            {
                _type = type;
                break;
            }
        }
    }

    public Type type
    {
        get
        {
            return _type;
        }
    }
}
