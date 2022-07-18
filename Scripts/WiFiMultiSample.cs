using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// wifisocket，一個server同時連線多client的範例
/// 請參考<WiFiSample.cs>檔案中的說明
public class WiFiMultiSample : MonoBehaviour
{
    WiFiSocket wifiSocket;
    WiFiSocket.WiFiClient client1, client2;
    string wifiName = "pocketcard_WiFi";
    public Text debugText;
    public Text c1Log, c2Log;
    public Text c1Data, c2Data;

    void Update()
    {
        if(Input.GetKey(KeyCode.Escape))
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0); //返回
        }
        if(wifiSocket)
            debugText.text = wifiSocket.wifiLog;
        if (client1 != null)
        {            
            c1Log.text = client1.wifiLog;
            client1.recvFunc((data) =>
            {
                c1Data.text = "c1 recv: " + data;
            });
        }

        if(client2 != null)
        {
            c2Log.text = client2.wifiLog;
            client2.recvFunc((data) =>
            {
                c2Data.text = "c2 recv: " + data;
            });
        }
    }

    public void createServer(Text portText)
    {
        wifiSocket = WiFiSocket.getNewWiFiSocket(wifiName, new WiFiSocket.WiFiConfig("0.0.0.0", int.Parse(portText.text)));
        wifiSocket.createServer(2);
    }

    public void acceptClient1()
    {
        wifiSocket.acceptClient("client1",
        (_client) => 
        {
            client1 = _client;
            
        });
    }
    public void acceptClient2()
    {
        wifiSocket.acceptClient("client2",
        (_client) => 
        {
            client2 = _client;
        });
    }

    public void disconnectClient1()
    {
        client1.disconnect();
    }

    public void disconnectClient2()
    {
        client2.disconnect();
    }

    public void sendDataClient1(Text sendText)
    {
        if (client1.sendFlag)
        {
            client1.sendFunc(sendText.text);
            c1Log.text = "c1 send: " + sendText.text;
        }
    }

    public void sendDataClient2(Text sendText)
    {
        if (client2.sendFlag)
        {
            client2.sendFunc(sendText.text);
            c2Log.text = "c2 send: " + sendText.text;
        }
    }

    public void closeServer()
    {
        wifiSocket.closeServer(false);
    }
}
