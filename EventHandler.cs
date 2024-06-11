using System;

public class EventHandler
{
    //某个客户端退出时调用，告诉其他客户端，有个客户端退出了
    public static void OnDisconnect(ClientState c)
    {
        string desc = c.socket.RemoteEndPoint.ToString();
        //Leave协议，客户端收到leave协议之后，就会把对应的玩家从场景中卸载
        string sendStr = "Leave|" + desc + ",";
        Console.WriteLine("OnDisconnect:" + sendStr);
        foreach (ClientState cs in MainClass.clients.Values)
        {
            MainClass.Send(cs, sendStr);
        }
    }
}