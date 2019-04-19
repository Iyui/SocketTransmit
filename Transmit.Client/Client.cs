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

namespace Transmit.Client
{
    public partial class Client : Form
    {
        public Client(Socket socketWatch,string MoudleCode)
        {
            InitializeComponent();
            this.socketWatch = socketWatch;
            this.MoudleCode = MoudleCode;
            sp.DataReceived += Sp_DataReceived;


        }
        //用于通信的Socket
        Socket socketSend;


        //定义回调:解决跨线程访问问题
        private delegate void SetTextValueCallBack(string strValue);
        //定义接收客户端发送消息的回调
        private delegate void ReceiveMsgCallBack(string strReceive);

        private delegate void IPCallBack(string strReceive);

        private delegate void SendFromSerialportCallBack(byte[] bytes);

        private SendFromSerialportCallBack SendByteCallBack;

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

        //创建监听连接的线程
        Thread AcceptSocketThread;
        //接收客户端发送消息的线程
        Thread threadReceive;

        //用于监听的SOCKET
        public Socket socketWatch
        {
            get;set;
        }

        public string MoudleCode
        {
            get; set;
        }
        private void Client_Load(object sender, EventArgs e)
        {
            Connect();
        }

        private void Connect()
        {
            var signal = Encoding.Default.GetBytes($"nedraghs:{MoudleCode}:");
            socketWatch.Send(signal);
            //实例化回调
            setCallBack = new SetTextValueCallBack(SetTextValue);
            receiveCallBack = new ReceiveMsgCallBack(ReceiveMsg);
            SendByteCallBack = new SendFromSerialportCallBack(SendFromSerialportToSocket);
            //ipCallBack = new IPCallBack(IpChangeValue);
            //setCmbCallBack = new SetCmbCallBack(AddCmbItem);
            //sendCallBack = new SendFileCallBack(SendFile);
            //sendThread = new Thread(Send);
            //sendThread.Start();
            threadReceive = new Thread(new ParameterizedThreadStart(Receive));
            threadReceive.IsBackground = true;
            threadReceive.Start(socketWatch);
        }

        /// <summary>
        /// 不停的接收服务器端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void Receive(object obj)
        {
            socketSend = obj as Socket;
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[36];
                    //实际接收到的有效字节数
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
                        string str = Encoding.ASCII.GetString(buffer, 0, count);
                        string strReceiveMsg = $"接收：{s}发送的消息：{ str}";
                        txt_Log.Invoke(receiveCallBack, strReceiveMsg);
                    }
                }
                catch
                {

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
            SendFromSerialportToSocket(data);

            string temp = "";
            for (int i = 0; i < data.Length; i++)
            {
                temp += data[i].ToString("X2") + " ";
            }
            txt_Log.Invoke(receiveCallBack, $"接收数据{temp}\n");
            
        }

        private void SendFromSerialportToSocket(byte[] data)
        {
            socketSend.Send(data);
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

    }
}
