using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class SimpleSocket
{
    public Action<byte[]> onReceived;

    private Socket socket;

    public void Connect(string ip, int port)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.BeginConnect(ip, port, new AsyncCallback(ConnectCallBack), socket);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n" + e.StackTrace);
        }
    }

    public bool connected
    {
        get
        {
            return socket != null && socket.Connected;
        }
    }

    public void Send(byte[] bytes)
    {
        try
        {
            socket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallBack), socket);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n" + e.StackTrace);
        }
    }

    private void SendCallBack(IAsyncResult ar)
    {
        Socket socket = ar.AsyncState as Socket;
        try
        {
            socket.EndSend(ar);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n" + e.StackTrace);
        }
    }

    private void HandleMessage(SocketState state)
    {
        MemoryStream stream = state.stream;
        stream.Position = 0;
        int length = -1;
        if (stream.Length >= 4)
        {
            byte[] lenbytes = new byte[4];
            stream.Read(lenbytes, 0, lenbytes.Length);
            length = BitConverter.ToInt32(lenbytes, 0);
            Debug.Log("Received bytes content length: " + length);
        }
        if (length != -1)
        {
            if ((stream.Length - stream.Position) >= length)
            {
                byte[] cbytes = new byte[length];
                stream.Read(cbytes, 0, cbytes.Length);
                onReceived(cbytes);

                byte[] leftbytes = new byte[stream.Length - stream.Position];
                stream.Read(leftbytes, 0, leftbytes.Length);
                MemoryStream newStream = new MemoryStream(leftbytes);
                state.stream = newStream;
            }
        }
        state.stream.Position = state.stream.Length;
    }

    private void ConnectCallBack(IAsyncResult ar)
    {
        try
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndConnect(ar);
            Receive();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n" + e.StackTrace);
        }
    }

    private void Receive()
    {
        try
        {
            SocketState state = new SocketState();
            state.simpleSocket = this;
            state.socket = socket;
            socket.BeginReceive(state.buffer, 0, SocketState.bufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n" + e.StackTrace);
        }
    }

    private void ReceiveCallBack(IAsyncResult ar)
    {
        SocketState state = ar.AsyncState as SocketState;
        Socket socket = state.socket;
        try
        {
            int count = socket.EndReceive(ar);
            if (count > 0)
            {
                state.stream.Write(state.buffer, 0, count);
                state.simpleSocket.HandleMessage(state);
            }
            socket.BeginReceive(state.buffer, 0, SocketState.bufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "\n" + e.StackTrace);
        }
    }

    private class SocketState
    {
        public const int bufferSize = 256;
        public SimpleSocket simpleSocket;
        public Socket socket;
        public byte[] buffer = new byte[bufferSize];
        public MemoryStream stream = new MemoryStream();
    }
}
