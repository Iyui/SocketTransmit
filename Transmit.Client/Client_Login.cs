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
using SocketTransmit.Config;
namespace Transmit.Client
{
    public partial class Client_Login : Form
    {
        public Client_Login()
        {
            InitializeComponent();
            textBox1.Text = "0000";
        }
        Socket socketWatch;
        private void btn_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                string host = "192.168.1.128";
                 //string host = "118.31.47.54 ";//服务端IP地址

                int port = 10000;
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //设置端口可复用
                socketWatch.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //连接服务端
                socketWatch.Connect(host, port);
                Client client = new Client(socketWatch, textBox1.Text, host,port);
                client.Show();
                SetConfigFile();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接服务器失败:" + ex.ToString());
            }
        }
        INIFile config = new INIFile(Application.StartupPath+@"\Config.ini");
        private void SetConfigFile()
        {
            config.IniWriteValue("远程通讯", "模块号", textBox1.Text);
        }

        private void GetConfigFile()
        {
            if(config.ExistINIFile())
                textBox1.Text=config.IniReadValue("远程通讯", "模块号");
        }

        private void Client_Login_Load(object sender, EventArgs e)
        {
            GetConfigFile();
        }
    }
}