using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;

public class Main : MonoBehaviour 
{
    private SimpleSocket socket;
    private Vector3 dest = new Vector3(40, 0, 40);

    private Queue<byte[]> messages = new Queue<byte[]>();

	void Start () 
    {
        socket = new SimpleSocket();
        socket.onReceived = onReceived;
        socket.Connect("127.0.0.1", 12345);
	}
	
	void Update () 
    {
	    if (messages.Count > 0)
        {
            byte[] bytes = messages.Dequeue();
            HandleMessage(bytes);
        }
	}

    void OnGUI()
    {
        GUI.color = socket.connected ? Color.green : Color.red;
        GUILayout.Label(socket.connected ? "已连接" : "未连接");
        GUI.color = Color.white;
        if (socket.connected)
        {
            GUILayout.BeginHorizontal();
            string x = GUILayout.TextField(dest.x.ToString());
            string y = GUILayout.TextField(dest.y.ToString());
            string z = GUILayout.TextField(dest.z.ToString());
            GUILayout.EndHorizontal();
            try
            {
                dest.x = float.Parse(x);
                dest.y = float.Parse(y);
                dest.z = float.Parse(z);
            }
            catch (Exception e){ }
            if (GUILayout.Button("Go"))
            {
                socket.Send(new byte[] { Convert.ToByte(true) });
            }
        }
    }

    private void onReceived(byte[] bytes)
    {
        messages.Enqueue(bytes);
    }

    private void HandleMessage(byte[] bytes)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(bytes));
        if (reader.ReadBoolean())
        {
            Debug.Log("寻路成功");
            int length = reader.ReadInt32();
            List<Vector3> corners = new List<Vector3>();
            for (int i = 0; i < length; i++)
            {
                Vector3 v = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );
                corners.Add(v);
            }
            GameObject go = new GameObject("path");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.SetWidth(0.1f, 0.1f);
            lr.SetVertexCount(corners.Count);
            for (int i = 0; i < corners.Count; i++)
            {
                lr.SetPosition(i, corners[i]);
            }
            lr.useWorldSpace = true;
        }
        else
        {
            Debug.Log("无法抵达该点");
        }
    }
}
