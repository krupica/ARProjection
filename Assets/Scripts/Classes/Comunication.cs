using Assets.Scripts.Classes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
//using NativeWebSocket;

public class Comunication : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("START");
        //Listen();
    }

    // Update is called once per frame
    void Update()
    {

    }

    static async void Listen()
    {
        var websocket = new ClientWebSocket();
        websocket.ConnectAsync(new Uri("ws://localhost:6789/"), CancellationToken.None).Wait();

        var buffer = new ArraySegment<byte>(new byte[1024]);
        while (websocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await websocket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var jsonData = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    //Console.WriteLine(jsonData);
                    DataManager.Data = JsonConvert.DeserializeObject<RecievedData>(jsonData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        websocket.Dispose();
    }
}
