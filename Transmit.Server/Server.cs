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
        //定义回调:解决跨线程访问问题
        private delegate void SetTextValueCallBack(string strValue);
        //定义接收客户端发送消息的回调
        private delegate void ReceiveMsgCallBack(string strReceive);

        private delegate void IPCallBack(string strReceive);

        //声明回调
        private SetTextValueCallBack setCallBack;
        //声明
        private ReceiveMsgCallBack receiveCallBack;

        private ReceiveMsgCallBack receiveIPCallBack;

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
            IPAddress ip = IPAddress.Parse("192.168.1.128");
            IPEndPoint ipe = new IPEndPoint(/*IPAddress.Any*/ip, 10001);//new IPEndPoint(IPAddress.Any, port);//IPAddress.Parse("180.175.63.122")
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketWatch.Bind(ipe);
            socketWatch.Listen(backlog);
            MessageBox.Show("监听成功");
            //this.txt_Log.AppendText("监听成功" + " \r \n");
            //btn_Start.Enabled = false;
            ////实例化回调
            //setCallBack = new SetTextValueCallBack(SetTextValue);
            receiveCallBack = new ReceiveMsgCallBack(ReceiveMsg);
            receiveIPCallBack = new ReceiveMsgCallBack(ReceiveMsgIP);

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
                listBox2.Invoke(receiveIPCallBack, strIp);
                string strMsg = "远程主机：" + socketSend.RemoteEndPoint + "连接成功";
                //使用回调
                //定义接收客户端消息的线程
                threadReceive = new Thread(new ParameterizedThreadStart(Receive));
                threadReceive.IsBackground = true;
                threadReceive.Start(socketSend);
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
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[socketSend.ReceiveBufferSize];
                    //实际接收到的有效字节数

                    int count = socketSend.Receive(buffer, buffer.Length,0);
                    List<byte> bufferlist = new List<byte>();
                    for (int i = 0; i < count; i++)
                        bufferlist.Add(buffer[i]);
                    string strIp = socketSend.RemoteEndPoint.ToString();
                    if (count == 0)//count 表示客户端关闭，要退出循环
                    {
                        break;
                    }
                    else
                    {
                        DisposeSignal(buffer, socketSend);
                        Send(bufferlist.ToArray(), socketSend);
                    }
                }
                catch
                {

                }
                Thread.Sleep(1);
            }
        }

        private void Send(byte[] bytes, Socket socketSend)
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
            listBox3.Invoke(receiveCallBack,socketSend.RemoteEndPoint +":"+s);
            string str = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            listBox3.Invoke(receiveCallBack, str);
            var message = str.Split(':');
            if (message[0] == "shgarden")
            {
     
                hsPCSocket.Add(socketSend);
                if (!htPCSocket.ContainsKey(message[1]))
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
                if(!htModuleSocket.ContainsKey(message[1]))
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

        private void Server_Load(object sender, EventArgs e)
        {
            Listern();
        }

        private void btRefresh_Click(object sender, EventArgs e)
        {
            lbMoudleCode.Items.Clear();
            foreach (string code in htPCSocket.Keys)
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
            this.listBox3.Items.Add(strMsg);
        }

        private void lbMoudleCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            try
            {
                listBox1.Items.Add(((Socket)htModuleSocket[lbMoudleCode.SelectedItem]).RemoteEndPoint);
            }
            catch { };
            try
            {
                listBox1.Items.Add(((Socket)htPCSocket[lbMoudleCode.SelectedItem]).RemoteEndPoint);
            }
            catch { };
        }
    }
}
