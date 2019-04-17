using System;
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
            IPEndPoint ipe = new IPEndPoint(IPAddress.Any, port);//new IPEndPoint(IPAddress.Any, port);//IPAddress.Parse("180.175.63.122")
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketWatch.Bind(ipe);
            socketWatch.Listen(backlog);
            //this.txt_Log.AppendText("监听成功" + " \r \n");
            //btn_Start.Enabled = false;
            ////实例化回调
            //setCallBack = new SetTextValueCallBack(SetTextValue);
            //receiveCallBack = new ReceiveMsgCallBack(ReceiveMsg);
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

                hsSocket.Add(socketSend);

                string strIp = socketSend.RemoteEndPoint.ToString();
                string strMsg = "远程主机：" + socketSend.RemoteEndPoint + "连接成功";
                //使用回调
                //定义接收客户端消息的线程
                threadReceive = new Thread(new ParameterizedThreadStart(Receive));
                threadReceive.IsBackground = true;
                threadReceive.Start(socketSend);
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 服务器端不停的接收客户端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void Receive(object obj)
        {
            Socket socketSend = obj as Socket;
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[socketSend.ReceiveBufferSize];
                    //实际接收到的有效字节数
                    int count = socketSend.Receive(buffer);
                    string strIp = socketSend.RemoteEndPoint.ToString();
                    if (count == 0)//count 表示客户端关闭，要退出循环
                    {
                        break;
                    }
                    else
                    {
                        DisposeSignal(buffer, socketSend);
                    }
                }
                catch
                {

                }
                Thread.Sleep(1);
            }
        }

        private void Send(Socket socketSend,byte[] bytes)
        {
            ((Socket)htMoudlePC[socketSend]).Send(bytes);
        }

        static Hashtable htMoudlePC = new Hashtable();
        /// <summary>
        /// 连接时判断是模块还是PC,并进行配对
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="socketSend"></param>
        private void DisposeSignal(byte[] buffer, Socket socketSend)
        {
            var s = "";
            foreach (var c in buffer)
                s += c.ToString("X2");
            var message = s.Split(':');
            if (message[0] == "shgarden")
            {
                hsPCSocket.Add(socketSend);
                htPCSocket.Add(message[1], socketSend);
                if (htPCSocket[message[1]] != socketSend)
                {
                    htPCSocket.Remove(message[1]);
                    htPCSocket.Add(message[1], socketSend);

                    htMoudlePC.Remove(socketSend);
                }
                if(htModuleSocket.ContainsKey(message[1]))
                {
                    htMoudlePC.Add(htModuleSocket[message[1]], socketSend);
                    htMoudlePC.Add(socketSend,htModuleSocket[message[1]]);
                }

            }
            else if (message[0] == "nedraghs")
            {
                hsModuleSocket.Add(socketSend);
                htModuleSocket.Add(message[1], socketSend);
                if(htModuleSocket[message[1]] != socketSend)
                {
                    htModuleSocket.Remove(message[1]);
                    htModuleSocket.Add(message[1], socketSend);

                    htMoudlePC.Remove(socketSend);
                }
                if (htPCSocket.ContainsKey(message[1]))
                {
                    htMoudlePC.Add(htPCSocket[message[1]], socketSend);
                    htMoudlePC.Add(socketSend, htPCSocket[message[1]]);
                }
            }
        }

    }
}
