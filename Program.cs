using System.Net;
using System.Net.Sockets;
using System.Reflection;

//客户状态类，用来存储客户数据
public class ClientState
{
    public Socket socket;
    public byte[] readBuff = new byte[1024];
    public int hp = -100;
    public float x = 0;
    public float y = 0;
    public float z = 0;
    public float eulY = 0;
}

public class MainClass
{
    //监听Socket
    static Socket listenfd;
    //用字典来存储客户端Socket及状态信息
    public static Dictionary<Socket, ClientState> clients =
        new Dictionary<Socket, ClientState>();

    //程序入口函数
    public static void Main(string[] args)
    {
        //创建Socket
        listenfd = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
        //连接Bind，给listenfd套接字绑定IP和端口
        IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
        IPEndPoint ipEp = new IPEndPoint(ipAdr, 8888);
        listenfd.Bind(ipEp);
        //开启监听，等待客户端连接。参数可以指定队列中最多可容纳等待接受的连接数，0表示不限制
        listenfd.Listen(0);
        Console.WriteLine("[服务器]启动成功");
        //checkRead，后面用来筛选可读的socket
        List<Socket> checkRead = new List<Socket>();
        //主循环
        while (true)
        {
            //填充checkRead列表
            checkRead.Clear();
            checkRead.Add(listenfd);
            foreach (ClientState s in clients.Values)
            {
                checkRead.Add(s.socket);
            }
            //Select可以确定一个或多个Socket对象的状态
            //四个参数分别为：检查是否有可读的Socket列表，检查是否有可写的Socket列表，检查是否有出错的Socke列表，等待回应的时间
            Socket.Select(checkRead, null, null, 1000);//（时间单位为微妙，-1表示一直等待，0表示非阻塞）
            //筛选之后checkRead列表只会剩下可读的socket，不可读的就会被舍弃
            //检查遍历可读对象
            foreach (Socket s in checkRead)
            {
                if (s == listenfd)
                {
                    //如果服务器端的Socket可读，说明可能有客户端来连接了
                    ReadListenfd(s);
                }
                else
                {
                    //客户端的socket可读，说明客户端发消息了，来接收处理客户端消息
                    ReadClientfd(s);
                }
            }
        }
    }

    //用来给客户端发送消息的函数
    public static void Send(ClientState cs, string sendStr)
    {
        if (cs == null || cs.socket == null) return;
        if (!cs.socket.Connected) return;
        //GetString方法可以将byte型数组转换成字符串。System.Text.Encoding.Default.GetBytes可以将字符串转换成byte型数组。
        byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
        cs.socket.Send(sendBytes);
    }

    //读取Listenfd
    public static void ReadListenfd(Socket listenfd)
    {
        Console.WriteLine("Accept");
        //Accept返回一个新客户端的Socket对象
        Socket clientfd = listenfd.Accept();
        ClientState state = new ClientState();
        state.socket = clientfd;
        //添加到clients字典中
        clients.Add(clientfd, state);
    }

    //读取Clientfd，读取客户端的消息
    public static bool ReadClientfd(Socket clientfd)
    {
        //通过字典获得当前客户端的状态
        ClientState state = clients[clientfd];
        //接收
        int count = 0;
        try
        {
            //接收消息存储在state.readBuff里面，返回字节数
            count = clientfd.Receive(state.readBuff);
        }
        catch (SocketException ex)
        {
            //关闭客户端

            //MethodInfo就是通过反射指定类获取到的 属性并提供对方法函数数据的访问。
            MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
            object[] ob = { state };
            //通过mei调用所获得的函数，
            //第一个参数null代表this指针，由于消息处理方法都是静态方法，
            //第二个参数ob代表的是参数列表
            mei.Invoke(null, ob);

            clientfd.Close();
            clients.Remove(clientfd);
            Console.WriteLine("Receive SocketException" + ex.ToString());
            return false;
        }
        //客户端关闭
        if (count <= 0)
        {
            MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
            object[] ob = { state };
            mei.Invoke(null, ob);

            clientfd.Close();
            clients.Remove(clientfd);
            Console.WriteLine("Socket Close");
            return false;
        }

        //消息处理
        string recvStr =
            System.Text.Encoding.Default.GetString(state.readBuff, 0, count);
        //有时候客户端连续发送两条消息给服务器端的时候，可能会导致两条消息合并发送过来，后面了解到这叫TCP粘包
        //这是由于底层的网络传输机制，多个小数据包可能会被合并成一个大数据包，这就导致了两个消息在一起发送的现象。
        //所以我在每条消息前面加了一个*号，然后根据*来先进行一次分割，即使粘包了，也可以根据*号来进行拆分。
        string[] allRecv = recvStr.Split('*');
        for (int i = 0; i < allRecv.Length; i++)
        {
            if (allRecv[i] == "") continue;
            Console.WriteLine("Recv:" + allRecv[i]);//打印接收到的消息
            string[] split = allRecv[i].Split('|');//使用‘|’进行二次分割
            string msgName = split[0];//得到的第一个字符串就是协议的名字，比如Enter表示进入游戏，Move表示玩家移动等
            string msgArgs = split[1];//第二个字符串就是执行协议所需要的相关的参数
            string funName = "Msg" + msgName;
            //所有的消息处理函数都在 MsgHandler 里面，且都是静态的，通过反射获得函数信息
            MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
            //这个例子所有的消息处理函数都需要这样两个参数ClientState c, string msgArgs
            object[] o = { state, msgArgs };
            mi.Invoke(null, o);
        }
        return true;
    }
}





























































//using System.Net.Sockets;
//using System.Net;
//using System.Reflection;
//using System.Linq;
//using System.Globalization;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace 服务器搭建
//{
//    internal class Program
//    {
//        internal class ClientState
//        {
//            public Socket socket;
//            public byte[] readBuff = new byte[1024];

//            public int hp = -100;
//            public float x = 0;
//            public float y = 0;
//            public float z = 0;
//            public float eulY = 0;
//        }
//        public class MainClass
//        {
//            //监听Socket
//            static Socket listenfd;
//            //客户端Socket及状态信息
//            public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();
//            static void Main(string[] args)
//            {


//                Console.WriteLine("Hello, World!");
//                //Socket 
//                Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                //Bind
//                IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
//                IPEndPoint ipEd = new IPEndPoint(ipAdr, 8888);
//                listenfd.Bind(ipEd);

//                //Listen
//                listenfd.Listen(0);
//                Console.WriteLine("[服务器]启动成功");
//                //CheckRead
//                List<Socket> checkRead = new List<Socket>();
//                //主循环
//                while (true)
//                {
//                    //填充Check列表
//                    checkRead.Clear();
//                    checkRead.Add(listenfd);
//                    foreach (ClientState s in clients.Values)
//                    {
//                        checkRead.Add(s.socket);
//                    }
//                    //select
//                    Socket.Select(checkRead, null, null, 1000);
//                    //检查可读对象
//                    foreach (Socket s in checkRead)
//                    {
//                        if (s == listenfd)
//                        {
//                            ReadListenfd(s);
//                        }
//                        else
//                        {
//                            ReadClientfd(s);
//                        }
//                    }
//                    if (listenfd.Poll(0, SelectMode.SelectRead))
//                    {
//                        ReadListenfd(listenfd);

//                    }
//                    //检查clientfd
//                    foreach (ClientState s in clients.Values)
//                    {
//                        Socket clientfd = s.socket;
//                        if (clientfd.Poll(0, SelectMode.SelectRead))
//                        {
//                            if (!ReadClientfd(clientfd))
//                            {
//                                break;
//                            }

//                        }

//                    }
//                    //防止cpu占用过高
//                    System.Threading.Thread.Sleep(1);
//                }



//                //Accept
//                //listenfd.BeginAccept(AcceptCallback, listenfd);
//                ////等待
//                //Console.ReadLine();
//                //while (true)
//                //{
//                //    //Accept
//                //    Socket connfd = listenfd.Accept();
//                //    Console.WriteLine("[服务器]Accept");
//                //    //Receive
//                //    byte[] readBuff = new byte[1024];
//                //    int count = connfd.Receive(readBuff);
//                //    string readStr = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
//                //    Console.WriteLine("[服务器]" + readStr);
//                //    //Send
//                //    //  byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
//                //    string sendStr = System.DateTime.Now.ToString();
//                //    byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
//                //    byte[] sendBytesTime = System.Text.Encoding.Default.GetBytes(sendStr);
//                //    connfd.Send(sendBytesTime);
//                //}

//            }
//            //用来给客户端发送消息的函数
//            public static void Send(ClientState cs, string sendStr)
//            {
//                if (cs == null || cs.socket == null) return;
//                if (!cs.socket.Connected) return;
//                //GetString方法可以将byte型数组转换成字符串。System.Text.Encoding.Default.GetBytes可以将字符串转换成byte型数组。
//                byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
//                cs.socket.Send(sendBytes);
//            }
//            private static void ReadListenfd(Socket listenfd)
//            {
//                Console.WriteLine("Accept");
//                Socket clientfd = listenfd.Accept();
//                ClientState state = new ClientState();
//                state.socket = clientfd;
//                clients.Add(clientfd, state);
//            }

//            private static bool ReadClientfd(Socket clientfd)
//            {
//                ClientState state = clients[clientfd];
//                //接收
//                int count = 0;
//                try
//                {
//                    count = clientfd.Receive(state.readBuff);
//                }
//                catch (SocketException ex)
//                {
//                    MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
//                    object[] ob = { state };
//                    mei.Invoke(null, ob);

//                    clientfd.Close();
//                    clients.Remove(clientfd);
//                    Console.WriteLine("Roceive SocketExcetion" + ex.ToString());
//                    return false;
//                }
//                //客户端关闭
//                if (count <= 0)
//                {
//                    MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
//                    object[] ob = { state };
//                    mei.Invoke(null, ob);

//                    clientfd.Close();
//                    clients.Remove(clientfd);
//                    Console.WriteLine("Socket Close");
//                    return false;
//                }
//                //广播
//                string recvStr =
//                System.Text.Encoding.Default.GetString(state.readBuff, 0, count);
//                String[] split = recvStr.Split('|');
//                Console.WriteLine("Receive" + recvStr);
//                string msgName = split[0];
//                string msgArgs = split[1];
//                string funName = "Msg" + msgName;
//                MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
//                object[] o = { state, msgArgs };
//                mi.Invoke(null, o);
//                return true;
//                //string sendStr = recvStr; 
//                //// string sendStr = clientfd.RemoteEndPoint.ToString() + ":" + recvSte;
//                ////byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
//                //byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
//                //foreach (ClientState cs in clients.Values)
//                //{
//                //    cs.socket.Send(sendBytes);
//                //}


//            }


//            //Accept回调

//            //        private static void AcceptCallback(IAsyncResult ar)
//            //        {
//            //            try
//            //            {
//            //                Console.WriteLine("[服务器]Accept");
//            //                Socket listenfd = (Socket)ar.AsyncState;
//            //                Socket clientfd = listenfd.EndAccept(ar);
//            //                //clients列表
//            //                ClientState state = new ClientState();
//            //                state.socket = clientfd;
//            //                clients.Add(clientfd, state);
//            //                //接收数据BeginReceive
//            //                clientfd.BeginReceive(state.readBuff, 0, 1024, 0, ReceiveCallback, state);
//            //                //继续Accept
//            //                listenfd.BeginAccept(AcceptCallback, listenfd);
//            //            }
//            //            catch (SocketException ex)
//            //            {
//            //                Console.WriteLine("Socket Accept fail" + ex.ToString());
//            //            }
//            //        }

//            //        //Receive回调
//            //        private static void ReceiveCallback(IAsyncResult ar)
//            //        {
//            //            try
//            //            {
//            //                ClientState state = (ClientState)ar.AsyncState;
//            //                Socket clientfd = state.socket;
//            //                int count = clientfd.EndReceive(ar);
//            //                //客户端关闭
//            //                if (count == 0)
//            //                {
//            //                    clientfd.Close();
//            //                    clients.Remove(clientfd);
//            //                    Console.WriteLine("Socket Close");
//            //                    return;
//            //                }
//            //                string recvStr = System.Text.Encoding.Default.GetString(state.readBuff, 0, count);
//            //                byte[] sendBytes = System.Text.Encoding.Default.GetBytes("echo" + recvStr);
//            //                //clientfd.Send(sendBytes);//减少代码量，不用异步
//            //                foreach(ClientState s in clients.Values)
//            //                {
//            //                    s.socket.Send(sendBytes);
//            //                }
//            //                clientfd.BeginReceive(state.readBuff, 0, 1024, 0, ReceiveCallback, state);
//            //            }
//            //            catch (SocketException ex)
//            //            {
//            //                Console.WriteLine("Socket Receive fail" + ex.ToString());
//            //            }
//            //        }
//        }


//    }
//}
