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
using System.Diagnostics;
using SocketTransmit.Config;
namespace Transmit.Client
{
    public partial class Client : Form
    {
        public Client(Socket socketWatch,string MoudleCode,string host,int port)
        {
            InitializeComponent();
            this.socketWatch = socketWatch;
            this.MoudleCode = MoudleCode;
            sp.DataReceived += Sp_DataReceived;
            this.host = host;
            this.port = port;
#if (!DEBUG)
            button2.Visible = false;
#endif  
        }
        //用于通信的Socket
        Socket socketSend;
        static bool isConnect = false;
        INIFile config = new INIFile(Application.StartupPath + @"\Config.ini");
        //定义回调:解决跨线程访问问题
        private delegate void SetTextValueCallBack(string strValue);
        //定义接收客户端发送消息的回调
        private delegate void ReceiveMsgCallBack(string strReceive);

        private delegate void IPCallBack(string strReceive);

        private delegate void ReStartApp();

        private delegate bool SendFromSerialportCallBack(byte[] bytes);

        private SendFromSerialportCallBack SendByteCallBack;

        private ReStartApp restartapp;

        //声明回调
        private SetTextValueCallBack setCallBack;
        //声明
        private ReceiveMsgCallBack receiveCallBack;

        private IPCallBack ipCallBack;
        //定义回调：给ComboBox控件添加元素
        private delegate void SetCmbCallBack(string strItem);
        //声明
        private SetCmbCallBack setCmbCallBack;
        //定义发送文件的回调
        private delegate void SendFileCallBack(byte[] bf);
        //声明
        private SendFileCallBack sendCallBack;

        Thread sendThread = null;

        //接收客户端发送消息的线程
        Thread threadReceive;

        //用于监听的SOCKET
        public Socket socketWatch
        {
            get;set;
        }

        public string host
        {
            get; set;
        }

        public int port
        {
            get; set;
        }

        public string MoudleCode
        {
            get; set;
        }
        private void Client_Load(object sender, EventArgs e)
        {

            textBox1.Text = MoudleCode;
            Connect();
            var spName = config.IniReadValue("远程通讯", "串口号");
            cbSerialPort_DropDown(sender, e);
            for (int i = 0; i < cbSerialPort.Items.Count; i++)
            {
                if (cbSerialPort.Items[i].ToString() == spName)
                {
                    //上次用过的串口依然可用
                    cbSerialPort.Text = spName;
                }
                if(cbSerialPort.SelectedIndex < 0 && cbSerialPort.Items.Count > 0)
                {
                    cbSerialPort.SelectedIndex = 0;
                }
            }
        }

        private void Connect()
        {
            var signal = Encoding.Default.GetBytes($"nedraghs:{MoudleCode}:");
            socketWatch.Send(signal);
            
            //实例化回调
            setCallBack = new SetTextValueCallBack(SetTextValue);
            receiveCallBack = new ReceiveMsgCallBack(ReceiveMsg);
            SendByteCallBack = new SendFromSerialportCallBack(SendFromSerialportToSocket);
            restartapp = new ReStartApp(ReStart);
            //ipCallBack = new IPCallBack(IpChangeValue);
            //setCmbCallBack = new SetCmbCallBack(AddCmbItem);
            //sendCallBack = new SendFileCallBack(SendFile);
            //sendThread = new Thread(Send);
            //sendThread.Start();
            threadReceive = new Thread(new ParameterizedThreadStart(Receive));
            threadReceive.IsBackground = true;
            threadReceive.Start(socketWatch);
            isConnect = true;
            //txt_Log.Invoke(receiveCallBack, "成功连接至服务器");
        }

        /// <summary>
        /// 不停的接收服务器端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void Receive(object obj)
        {
            socketSend = obj as Socket;
            int ReconnectCount = 1;
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[36];
                    //实际接收到的有效字节数
                    ReconnectCount = 1;
                    int count = socketSend.Receive(buffer);
                    if (count == 0)//count 表示客户端关闭，要退出循环
                    {
                        break;
                    }
                    else
                    {
                        if (sp.IsOpen)
                            sp.Write(buffer, 0, buffer.Length);
                        var s = "";
                        foreach (var c in buffer)
                            s += c.ToString("X2");
                        string str = Encoding.Default.GetString(buffer, 0, count);
                        if (str.IndexOf(MoudleCode) >= 0)
                        {
                            txt_Log.Invoke(receiveCallBack, "服务器连接成功,模块号:"+MoudleCode);
                        }
                        if (str.IndexOf("unconn")>=0)
                        {
                            MessageBox.Show("对应模块未连接");
                        }
                        if (str.IndexOf("AT+Z") >= 0)
                        {
                            if (MessageBox.Show("服务器获取客户端信息失败,是否重新连接服务器", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                this.Invoke(restartapp);
                            else
                                Disconnect();
                        }
                        string strReceiveMsg = $"接收{socketSend.RemoteEndPoint}：{s}";
                        txt_Log.Invoke(receiveCallBack, strReceiveMsg );
                    }
                }
                catch(Exception ex)
                {
                    isConnect = false;
                    MessageBox.Show($"与服务器连接断开,第{ReconnectCount}次尝试重连...");
                    socketWatch.Close();
                    socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //设置端口可复用
                    socketWatch.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    //连接服务端
                    try
                    {
                        socketWatch.Connect(host, port);
                        socketSend = socketWatch;
                        Connect();
                    }
                    catch
                    {
                        if (ReconnectCount < 5)
                            MessageBox.Show($"第{ReconnectCount}次尝试重连到服务器失败");
                        else
                        {
                            MessageBox.Show("无法重连到服务器");
                            Disconnect(false);                       
                        }
                        ReconnectCount++;
                    }

                }

            }
        }
        SerialPort sp = new SerialPort();
        private void cbSerialPort_DropDown(object sender, EventArgs e)
        {
            cbSerialPort.Items.Clear();
            cbSerialPort.Items.AddRange(SerialPort.GetPortNames());
        }

        private void cbSerialPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sp.IsOpen)
                sp.Close();
            sp.PortName = cbSerialPort.Text;
        }

        private void OpenSeraialport()
        {
            if (!sp.IsOpen)
            {
                try
                {
                    sp.PortName = cbSerialPort.Text;
                    sp.Open();
                    txt_Log.AppendText($"打开串口{sp.PortName}成功");
                }
                catch { }
            }
        }
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int len = sp.BytesToRead;
            byte[] data = new byte[len];
            sp.Read(data, 0, data.Length);
            if(!SendFromSerialportToSocket(data))
            {
                //Disconnect();
            }

            string temp = "";
            for (int i = 0; i < data.Length; i++)
            {
                temp += data[i].ToString("X2") + " ";
            }
            txt_Log.Invoke(receiveCallBack, $"{sp.PortName}接收数据{temp}\n");           
        }

        private bool SendFromSerialportToSocket(byte[] data)
        {
            if (isConnect == true)
            {
                socketSend.Send(data);
                return true;
            }
            else
                MessageBox.Show("与服务器连接断开");
            return false;
        }

        /// <summary>
        /// 回调委托需要执行的方法
        /// </summary>
        /// <param name="strValue"></param>
        private void SetTextValue(string strValue)
        {
            this.txt_Log.AppendText(strValue + " \r \n");
        }


        private void ReceiveMsg(string strMsg)
        {
            this.txt_Log.AppendText(strMsg + " \r \n");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenSeraialport();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void Client_FormClosed(object sender, FormClosedEventArgs e)
        {
            config.IniWriteValue("远程通讯", "串口号", cbSerialPort.Text);
            if (!bRestart)
            {
                bRestart = false;
                Disconnect();
            }
            
        }

        private void Disconnect(bool bDisCon = true)
        {
            if (bDisCon)
            {
                socketWatch.Disconnect(false);
                socketSend.Disconnect(false);
            }
            Environment.Exit(0);
        }
        static bool bRestart = false;
        private void ReStart()
        {
            socketWatch.Disconnect(false);
            socketSend.Disconnect(false);
            bRestart = true;
            Application.ExitThread();
            Application.Exit();
            Application.Restart();
            Process.GetCurrentProcess().Kill();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ReStart();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Thread td = new Thread(sendtest);
            td.Start();
        }

        private void sendtest()
        {
            var bytes = Encoding.Default.GetBytes("12 34 56 78 04 B0 00 FA 00 64 00 64 2B 03 F4 01 01 00 01 00 00 00 D1 00 00 00 00 00 00 00 64 00 00 00 00 00");
            while (true)
            {
                socketSend.Send(bytes);
                Thread.Sleep(1);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (sp.IsOpen)
            {
                try
                {
                    //sp.PortName = cbSerialPort.Text;
                    sp.Close();
                    txt_Log.AppendText($"关闭串口{sp.PortName}成功");
                }
                catch(Exception ex)
                {
                    txt_Log.AppendText($"关闭串口{sp.PortName}失败");
                }
            }
        }
    }
}
