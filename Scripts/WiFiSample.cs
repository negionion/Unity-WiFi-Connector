using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WiFiSample : MonoBehaviour
{
    WiFiSocket wifiSocket;
    WiFiSocket.WiFiClient client1;
    string wifiName = "pocketcard_WiFi";
    public Text debugText;
    public Text c1Log;
    public Text c1Data;
    void Update()
    {
        if(Input.GetKey(KeyCode.Escape))
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0); //返回
        }
        if(wifiSocket)
            debugText.text = wifiSocket.wifiLog;    //印出除錯資訊
        if (client1 != null)
        {            
            c1Log.text = client1.wifiLog;
            client1.recvFunc((data) =>              //偵測指定的client是否有新資料
            {
                //有新資料將執行下列程式碼
                //data : 收到的新資料，預設為string型別
                c1Data.text = "c1 recv: " + data;
            });
        }
    }

    public void createServer(Text portText)
    {
        wifiSocket = WiFiSocket.getNewWiFiSocket(wifiName, new WiFiSocket.WiFiConfig("0.0.0.0", int.Parse(portText.text))); //建立新的wifisocket
        wifiSocket.createServer(1);     //建立server，並傳入該server允許的最大連線數量
    }

    public void acceptClient1()         //呼叫該函數，將於server中註冊連線工作，並依據呼叫次序對應連入的client順序
    {
        wifiSocket.acceptClient("client1",
        (_client) => 
        {
            client1 = _client;
            
        });
    }

    public void disconnectClient1()     //中斷指定的client連線
    {
        client1.disconnect();
    }

    public void sendDataClient1(Text sendText)  //傳送資料到client中
    {
        if (client1.sendFlag)
        {
            client1.sendFunc(sendText.text);
            c1Log.text = "c1 send: " + sendText.text;
        }
    }

    public void closeServer()           //中斷所有連線並關閉server
    {
        wifiSocket.closeServer(false);
    }
}
