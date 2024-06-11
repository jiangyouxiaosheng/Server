using System;

class MsgHandler
{
    //Enter协议，表示有客户端进入游戏
    public static void MsgEnter(ClientState c, string msgArgs)
    {
        //解析参数
        string[] split = msgArgs.Split(',');
        string desc = split[0];
        float x = float.Parse(split[1]);
        float y = float.Parse(split[2]);
        float z = float.Parse(split[3]);
        float eulY = float.Parse(split[4]);
        //赋值
        c.hp = 100;
        c.x = x;
        c.y = y;
        c.z = z;
        c.eulY = eulY;
        //广播，告诉所有其他玩家客户端，有新人进入游戏了，然后其他玩家客户端就会加载这个玩家
        string sendStr = "Enter|" + msgArgs;
        foreach (ClientState cs in MainClass.clients.Values)
        {
            MainClass.Send(cs, sendStr);
        }
    }

    //List协议，用来获得所有当前正在游玩的玩家的 位置和朝向 信息。玩家刚进入游戏时，要通过这个协议来得到其他玩家信息
    public static void MsgList(ClientState c, string msgArgs)
    {
        string sendStr = "List|";
        foreach (ClientState cs in MainClass.clients.Values)
        {
            sendStr += cs.socket.RemoteEndPoint.ToString() + ",";
            sendStr += cs.x.ToString() + ",";
            sendStr += cs.y.ToString() + ",";
            sendStr += cs.z.ToString() + ",";
            sendStr += cs.eulY.ToString() + ",";
            sendStr += cs.hp.ToString() + ",";
        }
        Console.WriteLine(sendStr);
        MainClass.Send(c, sendStr);
    }

    //Move协议，每个玩家的每次移动都要向服务器端发送move协议，然后服务器再转发给所有其他客户端，这样能显示其他玩家的移动
    public static void MsgMove(ClientState c, string msgArgs)
    {
        //解析参数
        string[] split = msgArgs.Split(',');
        string desc = split[0];
        float x = float.Parse(split[1]);
        float y = float.Parse(split[2]);
        float z = float.Parse(split[3]);
        //赋值
        c.x = x;
        c.y = y;
        c.z = z;
        //广播
        string sendStr = "Move|" + msgArgs;
        foreach (ClientState cs in MainClass.clients.Values)
        {
            MainClass.Send(cs, sendStr);
        }
    }

    //Attack协议，每个玩家的每次攻击都要向服务器端发送Attack协议，然后服务器再转发给所有其他客户端。
    public static void MsgAttack(ClientState c, string msgArgs)
    {
        //广播
        string sendStr = "Attack|" + msgArgs;
        foreach (ClientState cs in MainClass.clients.Values)
        {
            MainClass.Send(cs, sendStr);
        }
    }

    //Hit协议，玩家如果攻击到了其他玩家，就要向服务器发送Hit协议，告诉服务器哪个玩家被攻击了
    public static void MsgHit(ClientState c, string msgArgs)
    {
        //解析参数
        string[] split = msgArgs.Split(',');
        string attDesc = split[0];
        string hitDesc = split[1];
        //找出被攻击的角色
        ClientState hitCS = null;
        foreach (ClientState cs in MainClass.clients.Values)
        {
            if (cs.socket.RemoteEndPoint.ToString() == hitDesc)
                hitCS = cs;
        }
        if (hitCS == null)
            return;
        //扣血
        hitCS.hp -= 25;
        //死亡
        if (hitCS.hp <= 0)
        {
            // Die协议，某个玩家hp小于0了，要告诉所有其他玩家某个玩家死亡了。
            string sendStr = "Die|" + hitCS.socket.RemoteEndPoint.ToString();
            foreach (ClientState cs in MainClass.clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }
    }
}