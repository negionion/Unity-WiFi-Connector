# Unity WiFi Connector
依存於Unity MonoBehaviour的生命週期</br>
<img src="https://imgur.com/ZRLFWhv.png" height="500px" width="250px" />
<img src="https://imgur.com/c6RRnLb.png" height="500px" width="250px" />

## 使用流程

![](https://i.imgur.com/3SnebZu.png)

---

## WiFiSocket

---

### struct

#### public WiFiConfig
該結構用於紀錄TCP Server設定(WiFi設定)
- ip : Server ip位址，預設0.0.0.0，支援所有ip連入
- port : Server port號，預設43208，請依需求自行更改

---

### static method

#### public static WiFiSocket getNewWiFiSocket(string _wifiName, WiFiConfig _wifiInfo)
取得新的WiFiSocket物件，若已存在則直接傳回該物件
- _wifiName：用於識別WiFi Server
- _wifiInfo：Server連線資料，請參考WiFiConfig設定

#### public static WiFiSocket getWiFiSocket(string _wifiName)
依識別名稱取得WiFiSocket
- _wifiName：用於識別WiFi Server

---

### public method

#### public WiFiClient getWiFiClient(string _clientName)
依識別名稱取得WiFiClient
- _clientName：用於識別WiFi Client

#### public void createServer(int _backlog = 1)
建立Server
- _backlog : 可容納的Client數量上限

#### public void acceptClient(string clientName, Action\<WiFiClient> acceptedClient = null)
註冊連線工作，依函數呼叫順序執行連線工作
- clientName : 連線成功後的Client識別名稱
- acceptedClient : 連線成功後自動執行的委派函數

#### public void closeServer(bool flag = true)
關閉WiFiSocket，並中斷連線
- flag : 
    - true  = 保留WiFiSocket物件，並保留socket
    - false = 不保留全部回收，並銷毀該物件

#### public bool clientRename(WiFiClient _client, string _name)
指定的client重新命名，可更改client識別名稱，方便識別不同client
- _client : 要改名的client
- _name : 欲更改的名稱
- return : 
    - true  : 更名成功
    - false : clients中不存在該client，更名失敗

---

## WiFiSocket.WiFiClient

#### public void recvFunc(Action\<string> recvAction)
接收資料，非同步接收，使用單一記憶體空間(新資料直接覆蓋舊資料)
(可多載改為使用其他資料型別)
- recvAction : 有新資料將會執行該委派函數，委派引數為接收到的字串資料，無新資料則不執行

#### public void sendFunc(string _data)
送出資料，非同步傳送，使用單一記憶體空間(新資料直接覆蓋舊資料)
(可多載改為使用其他資料型別)
- _data : 傳送的字串資料

#### public void disconnect()
中斷Client的連線
