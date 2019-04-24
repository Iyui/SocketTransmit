using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.IO.Ports;

namespace SocketTransmit
{
    public class SocketTransmit
    {
        public int port
        {
            get;set;
        }

        public int backlog
        {
            get; set;
        }


        public SocketTransmit(int port,int backlog)
        {
            this.port = port;
            this.backlog = backlog;
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
            //AcceptSocketThread = new Thread(new ParameterizedThreadStart(StartListen));
            //AcceptSocketThread.IsBackground = true;
            //AcceptSocketThread.Start(socketWatch);
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
                }
                catch
                {

                }
                Thread.Sleep(1);
            }
        }

    }
}
