using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

public class WiFiSocket : MonoBehaviour
{
    /// 該結構用於紀錄TCP Server設定(WiFi設定)
    /// ip : Server ip位址，預設0.0.0.0，支援所有ip連入
    /// port : Server port號，預設43208，請依需求自行更改
    public struct WiFiConfig
    {
        public string ip;
        public int port;
        public WiFiConfig(string _ip = "0.0.0.0", int _port = 43208)
        {
            ip = _ip;
            port = _port;
        }
    }

    /// 連線註冊結構
    /// clientName : 連線後的Client名稱
    /// connectedAct : 連線成功時欲執行的委派函數
    private struct ConnectTask
    {
        public string clientName;
        public Action<WiFiClient> connectedAct;
        public ConnectTask(string _clientName, Action<WiFiClient> _connectedAct = null)
        {
            clientName = _clientName;
            connectedAct = _connectedAct;
        }
    }


    public string serverName { private set; get; }      //Server識別名稱
    public WiFiConfig wiFiInfo { private set; get; }    //Server使用的ip與port資訊
    public Socket socket { private set; get; }          //Server使用的socket
    private bool isInitiated = false;                   //初始化成功旗標
    private Thread listeningThread;                     //監聽執行緒
    public bool isListening {private set; get;} = false;//監聽旗標
    private int backlog = 1;                            //可同時連入的Client數量，預設1
    private List<ConnectTask> connectTasks;             //多Client的連線工作註冊表
    public Dictionary<string, WiFiClient> clients { private set; get; }     //多Client的連線表

    public string wifiLog { private set; get; }         //除錯用訊息

    

    /// 靜態函數建構
    /// 依存於Unity MonoBehaviour的生命週期
    /// 請使用此函數建構WiFiSocket
    /// _wifiName : 自定義的WiFiSocket識別名稱(不可與其他WiFiSocket相同)
    /// _wifiInfo : Server使用ip與port設定
    /// 不存在則建立一個新的WiFiSocket，已存在則回傳現有的WiFiSocket
    public static WiFiSocket getNewWiFiSocket(string _wifiName, WiFiConfig _wifiInfo)
    {
        GameObject wifi = GameObject.Find(_wifiName);
        if (wifi != null)
        {
            return wifi.GetComponent<WiFiSocket>();
        }
        wifi = new GameObject(_wifiName);
        WiFiSocket wifiSocket = wifi.AddComponent<WiFiSocket>();
        wifiSocket.serverName = _wifiName;
        wifiSocket.wiFiInfo = _wifiInfo;
        Instantiate(wifi);
        DontDestroyOnLoad(wifi);
        return wifiSocket;
    }


    /// 依識別名稱取得WiFiSocket
    /// _wifiName : 識別名稱
    /// 若不存在則回傳null
    public static WiFiSocket getWiFiSocket(string _wifiName)
    {
        GameObject wifi = GameObject.Find(_wifiName);
        if (wifi != null)
        {
            return wifi.GetComponent<WiFiSocket>();
        }
        else
            return null;
    }


    /// 依識別名稱取得WiFiClient
    /// _wifiName : 識別名稱
    /// 若不存在則回傳null
    public WiFiClient getWiFiClient(string _clientName)
    {
        if(clients.ContainsKey(_clientName))
        {
            return clients[_clientName];
        }
        else
        {
            wifiLog = _clientName + " not exists!\n";
            return null;
        }
    }
    ~WiFiSocket()
    {
        closeServer();
    }

    private void initiate(string _wifiName, WiFiConfig _wifiInfo, int _backlog)
    {
        if(isInitiated)
        {
            wifiLog += "Server is created!\n";
            return;
        }
        isInitiated = true;
        isListening = false;
        serverName = _wifiName;
        wiFiInfo = _wifiInfo;
        backlog = _backlog;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        wifiLog = "";
    }

    private static byte[] keepAlive(bool isUse, int keepAliveTime, int keepAliveInterval)
    {
        byte[] buffer = new byte[12];
        BitConverter.GetBytes(isUse ? 1 : 0).CopyTo(buffer, 0);
        BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4);
        BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);
        return buffer;
    }

    /// 建立Server
    /// _backlog : 可容納的Client數量
    public void createServer(int _backlog = 1)
    {
        initiate(serverName, wiFiInfo, _backlog);
        if (socket == null || wiFiInfo.port <= 0)
        {
            wifiLog += "Server create error!\n";
            return;
        }
        socket.Bind(new IPEndPoint(IPAddress.Parse(wiFiInfo.ip), wiFiInfo.port));
        connectTasks = new List<ConnectTask>();
        clients = new Dictionary<string, WiFiClient>();        
        startListen();
    }

    private void startListen()
    {
        if(isListening)
            return;
        isListening = true;     
        listeningThread = new Thread(listeningProcess);
        listeningThread.IsBackground = true;
        listeningThread.Start();
    }

    /// 註冊連線工作
    /// 依函數呼叫順序執行連線工作
    /// clientName : 連線成功後的Client識別名稱
    /// acceptedClient : 連線成功後自動執行的委派函數
    public void acceptClient(string clientName, Action<WiFiClient> acceptedClient = null)
    {
        foreach(var _client in connectTasks)
        {
            if(_client.clientName == clientName)
            {
                return;
            }
        }
        if(clients.Count < backlog)
            connectTasks.Add(new ConnectTask(clientName, acceptedClient));
    }

    /// 異常斷線重連
    /// 連線工作強制插隊在最前
    private void reconnect(string clientName)
    {
        connectTasks.Insert(0, new ConnectTask(clientName));
    }

    /// 監聽執行緒
    /// isListening : 執行緒旗標
    private void listeningProcess()
    {
        try
        {
            while (isListening)
            {
                
                if(connectTasks.Count <= 0)
                {
                    Thread.Sleep(100);
                    continue;
                }
                Thread.Sleep(100);
                socket.Listen(1);
                Debug.Log(serverName + " Listening...\n");
                wifiLog += serverName + " Listening...\n";
                Socket newClient = socket.Accept();
                socket.Listen(0);
                ConnectTask newConnectTask = connectTasks[0];
                if(clients.ContainsKey(newConnectTask.clientName))
                {
                    clients[newConnectTask.clientName].resetWiFiClient(newConnectTask.clientName, serverName, newClient);
                    wifiLog += newConnectTask.clientName + " 重新連線\n";
                }
                else
                {
                    clients.Add(newConnectTask.clientName, new WiFiClient(newConnectTask.clientName, serverName, newClient));
                }
                WiFiClient _client = clients[newConnectTask.clientName];           
                wifiLog += "Accept client. Name : " + newConnectTask.clientName + "\n";
                if (newConnectTask.connectedAct != null)
                {         
                    newConnectTask.connectedAct(_client);                                      
                }
                connectTasks.RemoveAt(0);
                if(clients.Count >= backlog)    
                {            
                    wifiLog += "Client full!  Server stop listen...\n";
                }
                
            }
        }
        catch (SocketException)
        {
            wifiLog = string.Format("{0} accept Stop!\n", serverName);
            Debug.LogFormat("Wifi socket accept error : {0}.\n", serverName);
        }
        catch (ThreadAbortException)
        {
            
        }
        catch (Exception e)
        {
            wifiLog = e.StackTrace;
        }
    }

    /// 指定的client重新命名
    /// _client : 要改名的client
    /// _name : 欲更改的名稱
    /// return : 
    ///     true  : 更名成功
    ///     false : clients中不存在該client，更名失敗
    /// 更改client識別名稱，更方便的識別不同client
    public bool clientRename(WiFiClient _client, string _name)
    {
        if(clients.ContainsKey(_client.name))
        {
            clients.Remove(_client.name);
            clients.Add(_name, _client);
            _client.rename(_name);
            return true;
        }
        else
            return false;
    }

    /// 關閉WiFiSocket，並中斷連線
    /// flag : 
    ///     true  = 保留WiFiSocket物件，並保留socket
    ///     false = 不保留全部回收，並銷毀該物件
    public void closeServer(bool flag = true)
    {
        if(!isInitiated)
        {
            wifiLog += "Server is not connected!\n";
            return;
        }
        try
        {            
            isListening = false;
            listeningThread.Abort();                                 
        }
        catch (ThreadAbortException)
        {
            wifiLog += "Stop Listen...\n";
        }
        catch (Exception e)
        {
            wifiLog += "StopConnect error " + e + "\n";
            Debug.Log("StopConnect error " + e.StackTrace);
        }
        finally
        {
            foreach(var client in clients.ToList())
            {
                client.Value.disconnect();
            }  
            socket.Disconnect(flag); 
            socket.Close();
            if(!socket.Connected)
            {
                isInitiated = false;
                wifiLog += "Disconnect completed!\n";
                Debug.Log("StopConnect complete");
                if(!flag)
                {
                    Destroy(gameObject);
                }
            }
        }
        
    }

    /// 用於控制與Server連線的Client
    /// name : Client自定義名稱(不可與其他Client重複)
    /// MTU : 預設 512 Bytes
    /// wifiLog : Debug訊息
    /// 其他變數，非必要請勿更改內部設置
    public class WiFiClient
    {
        public string name { private set; get; }
        private string serverName;  //主控Server的Name
        public Socket socket { private set; get; }  //Client使用的Socket
        public int MTU = 64;
        private int keepAliveTime = 200;            //keepalive間隔時間，單位ms
        public string wifiLog { private set; get; }
        private Thread recvThread, sendThread;  //recv, send 異步處理
        public bool recvFlag { private set; get; } = true;  //控制recv執行緒
        public bool sendFlag { private set; get; } = true;  //控制send執行緒
        private byte[] recvBuffer, sendBuffer;  //buffer
        private int recvLength = 0, sendLength = 0; //buffer中的有效資料長度

        /// 建構子
        /// name : 該Client的自定義名稱(不可與其他Client重複)
        /// serverName : 該Client的主控Server名稱，請填入建立該Client的ServerName
        /// socket : 連線成功的Client socket
        /// MTU : 預設 64 Bytes
        internal WiFiClient(string _name, string _serverName, Socket _socket, int _MTU = 64)
        {
            name = _name;
            serverName = _serverName;
            socket = _socket;
            MTU = _MTU;
            recvFlag = true;
            sendFlag = true;
            wifiLog = string.Empty;
            socket.IOControl(IOControlCode.KeepAliveValues, keepAlive(true, keepAliveTime, keepAliveTime), null);
            recvThread = new Thread(recvProcess);
            recvThread.IsBackground = true;
            recvThread.Start();
            sendThread = new Thread(sendProcess);
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        /// 重設Client
        /// 用於異常斷線，重設Client參數並重啟recv與send流程
        /// name : 該Client的自定義名稱(不可與其他Client重複)
        /// serverName : 該Client的主控Server名稱，請填入建立該Client的ServerName
        /// socket : 連線成功的Client socket
        /// MTU : 預設 64 Bytes
        internal void resetWiFiClient(string _name, string _serverName, Socket _socket, int _MTU = 64)
        {
            name = _name;
            serverName = _serverName;
            socket = _socket;
            MTU = _MTU;
            recvFlag = true;
            sendFlag = true;
            wifiLog = string.Empty;
            socket.IOControl(IOControlCode.KeepAliveValues, keepAlive(true, keepAliveTime, keepAliveTime), null);
            recvThread = new Thread(recvProcess);
            recvThread.IsBackground = true;
            recvThread.Start();
            sendThread = new Thread(sendProcess);
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        internal void rename(string _name)
        {
            name = _name;
        }

        /// recv執行緒
        /// 異常斷線將會觸發SocketException，並自動重啟連線流程
        private void recvProcess()
        {
            recvBuffer = new byte[MTU];
            recvLength = 0;
            while (recvFlag)
            {
                try
                {
                    if(!socket.Connected)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    recvLength = socket.Receive(recvBuffer);
                }                
                catch (SocketException)
                {
                    wifiLog += name + " Disconnected!\n";   
                    getWiFiSocket(serverName).reconnect(name);                   
                    closeClient(true);                                   
                }
                catch (Exception e)
                {
                    wifiLog += e.Message + "\n";
                }
            }
        }

        /// 接收資料，非同步接收，使用單一記憶體空間(新資料直接覆蓋舊資料)
        /// recvAction : 有新資料將會執行該委派函數，委派引數為接收到的字串資料，無新資料則不執行
        /// 可自行多載改為使用其他資料型別
        public void recvFunc(Action<string> recvAction)
        {
            if (recvLength <= 0)
                return;
            string data = Encoding.UTF8.GetString(recvBuffer, 0, recvLength);
            recvAction(data);
            wifiLog += "recv:" + data + "\n";
            Debug.Log("recv:" + data + "\n");
            Array.Clear(recvBuffer, 0, recvBuffer.Length);
            recvLength = 0;
        }

        /// send執行緒
        private void sendProcess()
        {
            sendBuffer = new byte[MTU];
            sendLength = 0;
            while (sendFlag)
            {
                if (sendLength <= 0)
                {
                    Thread.Sleep(1);
                    continue;
                }
                try
                {
                    socket.Send(sendBuffer, 0, sendLength, SocketFlags.None);
                    wifiLog += "send:" + Encoding.UTF8.GetString(sendBuffer, 0, sendLength) + "\n";
                    Array.Clear(sendBuffer, 0, sendBuffer.Length);
                    sendLength = 0;
                }catch (SocketException)
                {

                }
            }
        }

        /// 送出資料，非同步傳送，使用單一記憶體空間(新資料直接覆蓋舊資料)
        /// _data : 傳送的字串資料
        /// 可自行多載改為使用其他資料型別
        public void sendFunc(string _data)
        {
            sendBuffer = Encoding.UTF8.GetBytes(_data + "\r");
            sendLength = _data.Length;
        }

        /// 中斷連線
        /// 無引數
        public void disconnect()
        {
            WiFiSocket server = getWiFiSocket(serverName);                        
            closeClient();
            server.clients.Remove(name);
        }

        /// 關閉Client的所有功能，並中斷連線
        /// reuseFlag : 
        ///     true  = socket將保留以便下次使用
        ///     false = socket將直接回收
        private void closeClient(bool reuseFlag = false)
        {                                  
            try
            {
                socket.Shutdown(SocketShutdown.Both);                               
            }
            catch(SocketException)
            {
                //wifiLog += "Close Socket error : " + e.StackTrace + "\n";
            }  
            catch(Exception e)
            {
                wifiLog += "Close Socket error : " + e.StackTrace + "\n";
            }
            finally
            {
                socket.Disconnect(reuseFlag); 
                socket.Close();  
                wifiLog += "disconnect!";  
                sendFlag = false;
                recvFlag = false;
                recvThread.Abort();
                sendThread.Abort(); 
            }
        }

        ~WiFiClient()
        {
            disconnect();
        }
    }

}
