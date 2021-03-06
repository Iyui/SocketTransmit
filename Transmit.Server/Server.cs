﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.IO.Ports;
namespace Transmit.Server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }

        public int port
        {
            get; set;
        }

        public int backlog
        {
            get; set;
        }

        #region 变量
        //定义回调:解决跨线程访问问题
        private delegate void SetTextValueCallBack(string strValue);
        //定义接收客户端发送消息的回调
        private delegate void ReceiveMsgCallBack(string strReceive);
        private delegate void RemoveMsgCallBack(string strRemove);
        private delegate void IPCallBack(string strReceive);

        //声明回调
        private SetTextValueCallBack setCallBack;
        //声明
        private ReceiveMsgCallBack receiveCallBack;

        private ReceiveMsgCallBack receiveIPCallBack;
        private RemoveMsgCallBack removeCallBack;


        private IPCallBack ipCallBack;
        //定义回调：给ComboBox控件添加元素
        private delegate void SetCmbCallBack(string strItem);
        //声明
        private SetCmbCallBack setCmbCallBack;
        //定义发送文件的回调
        private delegate void SendFileCallBack(byte[] bf);
        //定义发送文件的回调
        private delegate void SendMessageCallBack();

        //声明
        private SendFileCallBack sendCallBack;

        //声明
        private SendMessageCallBack sendMessageCallBack;


        //用于通信的Socket
        Socket socketSend;
        //用于监听的SOCKET
        Socket socketWatch;

        //创建监听连接的线程
        Thread AcceptSocketThread;
        //接收客户端发送消息的线程
        Thread threadReceive;
        static HashSet<Socket> hsSocket = new HashSet<Socket>();

        static HashSet<Socket> hsPCSocket = new HashSet<Socket>();
        static HashSet<Socket> hsModuleSocket = new HashSet<Socket>();

        static Hashtable htPCSocket = new Hashtable();
        static Hashtable htModuleSocket = new Hashtable();

        #endregion
        public void Listern()
        {
            try
            {
                IPAddress ip = IPAddress.Parse("192.168.1.128");
                IPEndPoint ipe = new IPEndPoint(ip, 10000);
                //IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 10000);
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketWatch.Bind(ipe);
                socketWatch.Listen(backlog);
                MessageBox.Show("监听成功");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }
            //this.txt_Log.AppendText("监听成功" + " \r \n");
            //btn_Start.Enabled = false;
            ////实例化回调
            //setCallBack = new SetTextValueCallBack(SetTextValue);
            receiveCallBack = new ReceiveMsgCallBack(ReceiveMsg);
            receiveIPCallBack = new ReceiveMsgCallBack(ReceiveMsgIP);
            removeCallBack = new RemoveMsgCallBack(RemoveMsg);
           //ipCallBack = new IPCallBack(IpChangeValue);
           //setCmbCallBack = new SetCmbCallBack(AddCmbItem);
           //sendCallBack = new SendFileCallBack(SendFile);
           //Thread send = new Thread(Send);
           //send.Start();
           ////创建线程
           AcceptSocketThread = new Thread(new ParameterizedThreadStart(StartListen))
            {
                IsBackground = true
            };
            AcceptSocketThread.Start(socketWatch);
        }
        /// <summary>
        /// 等待客户端的连接，并且创建与之通信用的Socket
        /// </summary>
        /// <param name="obj"></param>
        private void StartListen(object obj)
        {
            Socket socketWatch = obj as Socket;
            while (true)
            {
                //等待客户端的连接，并且创建一个用于通信的Socket
                socketSend = socketWatch.Accept();
                //获取远程主机的ip地址和端口号
                try
                {
                    socketSend.ReceiveTimeout = 10000;
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[socketSend.ReceiveBufferSize];
                    //实际接收到的有效字节数
                    int count = socketSend.Receive(buffer, buffer.Length, 0);
                    if(DisposeSignal(buffer,socketSend, out string MoudleCode))
                    {
                        hsSocket.Add(socketSend);
                        string strIp = socketSend.RemoteEndPoint.ToString();
                        listBox2.Invoke(receiveIPCallBack, strIp);
                        string strMsg = "远程主机：" + socketSend.RemoteEndPoint + "连接成功";
                        var signal = Encoding.Default.GetBytes($"{MoudleCode}{MoudleCode}{MoudleCode}{MoudleCode}{MoudleCode}");
                        socketSend.Send(signal);
                        //使用回调
                        //定义接收客户端消息的线程
                        threadReceive = new Thread(new ParameterizedThreadStart(Receive));
                        threadReceive.IsBackground = true;
                        threadReceive.Start(socketSend);
                    }
                    else
                    {
                        var signal = Encoding.Default.GetBytes("AT+Z");
                        var bytes1 = new byte[3] { 0x2b, 0x2b, 0x2b };
                        var bytes2 = new byte[1] { 0x61 };
                        var bytes3 = new byte[11] {0x61,0x64,0x6D,0x69,0x6E,0x41,0x54,0x2b,0X5a,0x0d,0x0a };
                        for (int i = 0; i < 2; i++)
                        {
                            Thread.Sleep(2000);
                            //socketSend.Send(bytes1);
                            Thread.Sleep(2000);

                            socketSend.Send(bytes2);
                            Thread.Sleep(5000);

                            socketSend.Send(bytes3);
                        }
                    }
                }
                catch
                {
                    listBox3.Invoke(receiveCallBack,"断开或无效的连接:" + socketSend.RemoteEndPoint);
                }
                Thread.Sleep(1);
            }
        }
        
        

        private void ClearSocketUnconnected()
        {

        }

        /// <summary>
        /// 服务器端不停的接收客户端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void Receive(object obj)
        {
            Socket socketSend = obj as Socket;
            socketSend.ReceiveTimeout=0;
            while (true)
            {
                try
                {
                    
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[40];
                    //实际接收到的有效字节数

                    int count = socketSend.Receive(buffer, buffer.Length,0);
                    List<byte> bufferlist = new List<byte>();
                    for (int i = 0; i < count; i++)
                        bufferlist.Add(buffer[i]);
                    string strIp = socketSend.RemoteEndPoint.ToString();
                    if (count == 0)//count 表示客户端关闭，要退出循环
                    {
                        var strSocket = socketSend.RemoteEndPoint.ToString();
                        listBox2.Invoke(removeCallBack, strSocket);
                        var keys = (string)(htModuleSocket.OfType<DictionaryEntry>().FirstOrDefault(q => q.Value == socketSend).Key);
                        ClearhtSocket(keys, socketSend);
                        break;
                    }
                    else
                    {
                        DisposeSignal(buffer, socketSend, out string moudlecode);
                        Send(bufferlist.ToArray(), socketSend);
                    }
                }
                catch
                {

                }
                Thread.Sleep(1);
            }
        }

        private void ClearhtSocket(string keys,Socket sk)
        {
            if (htModuleSocket.ContainsKey(keys))
                htModuleSocket.Remove(keys);
            if (htMoudlePC.ContainsKey(sk))
                htMoudlePC.Remove(sk);
        }
        private void Send(byte[] bytes, Socket socketSend)
        {
            if(htMoudlePC.ContainsKey(socketSend))
                ((Socket)htMoudlePC[socketSend]).Send(bytes);
            else
            {
                var signal = Encoding.Default.GetBytes("unconnunconnunconnunconnunconn");
                socketSend.Send(signal);
            }
        }

        static Hashtable htMoudlePC = new Hashtable();
        static int timeInterval = Environment.TickCount;
        //private bool isJudgeMoudleConnected()
        //{
        //    if (htMoudlePC)
        //}

        /// <summary>
        /// 连接时判断是模块还是PC,并进行配对
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="socketSend"></param>
        private bool DisposeSignal(byte[] buffer, Socket socketSend,out string MoudleCode)
        {
            MoudleCode = "";
            var s = "";
            foreach (var c in buffer)
                s += c.ToString("X2");
            listBox3.Invoke(receiveCallBack,socketSend.RemoteEndPoint +":"+s);
            string str = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            //listBox3.Invoke(receiveCallBack, s);
            var message = str.Split(':');
            if (message[0] == "nedraghs")
            {
                hsPCSocket.Add(socketSend);
                if (!htPCSocket.ContainsKey(message[1]))
                    htPCSocket.Add(message[1], socketSend);
                if (htPCSocket[message[1]] != socketSend)
                {
                    try { listBox2.Invoke(removeCallBack, ((Socket)htPCSocket[message[1]]).RemoteEndPoint.ToString()); }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    htPCSocket.Remove(message[1]);
                    htPCSocket.Add(message[1], socketSend);
                    htMoudlePC.Remove(socketSend);
                }
                if(htModuleSocket.ContainsKey(message[1]))
                {
                    if (htMoudlePC.ContainsKey(htModuleSocket[message[1]]))
                        htMoudlePC.Remove(htModuleSocket[message[1]]);
                    if (htMoudlePC.ContainsKey(socketSend))
                        htMoudlePC.Remove(socketSend);
                    htMoudlePC.Add(htModuleSocket[message[1]], socketSend);
                    htMoudlePC.Add(socketSend,htModuleSocket[message[1]]);
                }
                if (!htMoudlePC.ContainsKey(socketSend))
                {
                    var signal = Encoding.Default.GetBytes("unconnunconnunconnunconnunconn");
                    socketSend.Send(signal);
                }
                MoudleCode = message[1];
                return true;
            }
            else if (message[0] == "shgarden")
            {
                hsModuleSocket.Add(socketSend);
                if (!htModuleSocket.ContainsKey(message[1]))
                {
                    htModuleSocket.Add(message[1], socketSend);
                }
                if(htModuleSocket[message[1]] != socketSend)
                {
                    try { listBox2.Invoke(removeCallBack, ((Socket)htModuleSocket[message[1]]).RemoteEndPoint.ToString()); }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    htModuleSocket.Remove(message[1]);
                    htModuleSocket.Add(message[1], socketSend);

                    htMoudlePC.Remove(socketSend);
                }
                if (htPCSocket.ContainsKey(message[1]))
                {
                    if(htMoudlePC.ContainsKey(htPCSocket[message[1]]))
                        htMoudlePC.Remove(htPCSocket[message[1]]);
                    if(htMoudlePC.ContainsKey(socketSend))
                        htMoudlePC.Remove(socketSend);

                    htMoudlePC.Add(htPCSocket[message[1]], socketSend);
                    htMoudlePC.Add(socketSend, htPCSocket[message[1]]);
                }
                MoudleCode = message[1];
                return true;

            }
            if (!htMoudlePC.ContainsKey(socketSend))
            {
                var signal = Encoding.Default.GetBytes("unconnunconnunconnunconnunconn");
                socketSend.Send(signal);
            }
            return false;
        }
        private bool DisposeSignal(byte[] buffer)
        {
            var s = "";
            foreach (var c in buffer)
                s += c.ToString("X2");
            listBox3.Invoke(receiveCallBack,socketSend.RemoteEndPoint + ":" + s);
            string str = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            //listBox3.Invoke(receiveCallBack, s);
            var message = str.Split(':');
            if (message[0] == "nedraghs" || message[0] == "shgarden")
            {
                return true;
            }
            return false;
        }
        private void Server_Load(object sender, EventArgs e)
        {
            Listern();
        }

        private void btRefresh_Click(object sender, EventArgs e)
        {
            lbMoudleCode.Items.Clear();
            foreach (string code in htModuleSocket.Keys)
            {
                lbMoudleCode.Items.Add(code);
            }
        }
        private void ReceiveMsgIP(string strMsg)
        {
            this.listBox2.Items.Add(strMsg);
        }

        private void ReceiveMsg(string strMsg)
        {
            if(checkBox1.Checked)
            this.listBox3.Items.Add(DateTime.Now.ToString() + ":" + strMsg);
        }

        private void RemoveMsg(string strMsg)
        {
            this.listBox2.Items.Remove(strMsg);
        }

        private void RemoveMoudleCode(string strMsg)
        {
      
        }

        private void lbMoudleCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            try
            {
                listBox1.Items.Add("模块:"+((Socket)htModuleSocket[lbMoudleCode.SelectedItem]).RemoteEndPoint);
            }
            catch { };
            try
            {
                listBox1.Items.Add("PC:" + ((Socket)htPCSocket[lbMoudleCode.SelectedItem]).RemoteEndPoint);
            }
            catch { };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox3.Items.Clear();
        }
    }
}
