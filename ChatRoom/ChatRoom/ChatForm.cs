using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatRoom
{
    delegate void ProcessingCallback(int value, int max);

    public partial class ChatForm : Form
    {
        private static string connString = "server=localhost;user id=root;persistsecurityinfo=True;database=chatroom;port=3306;password=123";
        private MySqlConnection conn = new MySqlConnection(connString);
        //用于接收信息
        Socket TCPsocket = null;
        Socket UDPsocket = null;
        Thread thread1 = null;
        //用于接收文件
        Socket fileSocket = null;
        Thread thread2 = null;
        MiniForm miniForm;
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        string sendPath = "";
        string savePath = "";
        //获取文件名，不带路径，带扩展名
        string sendFileNameExt = "";
        string saveFileNameExt = "";
        string ip = ServerLogin.GetLocalIP();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ServerLogin.GetLocalIP()), Convert.ToInt32("51111"));
        //用于一对一聊天
        private string id = null;
        private string name = null;
        private string friendName = null;
        private string friendID = null;
        //自己的端口
        private int port;
        //用于群组聊天
        private string gid = null;
        private string gname = null;
        private string groupName = null;

        public ChatForm(string id, string name, string friendID, string friendName)
        {
            InitializeComponent();
            GetRandomPort();
            ServerLogin.ChatForms.Add(this);
            this.id = id;
            this.name = name;
            this.friendName = friendName;
            this.friendID = friendID;
            this.Text = name + "(" + id + ") chat with " + friendName;
            TCPsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            TCPsocket.Connect(endPoint);
            UDPsocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            UDPsocket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            fileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            fileSocket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            fileSocket.Listen(100);
            ServerLogin.ChatFormList.Add(id + "|" + friendID);
            ServerLogin.PortList[id + "|" + friendID] = port;
            thread1 = new Thread(ReceiveUDP);
            thread1.Start();
            ServerLogin.SendFileList[id + "|" + friendID] = false;
            ServerLogin.SendFileList[friendID + "|" + id] = false;
            thread2 = new Thread(ReceiveFile) {
                IsBackground = true
            };
            thread2.Start(fileSocket);
        }

        public ChatForm(string groupName, string gid, string gname)
        {
            InitializeComponent();
            GetRandomPort();
            ServerLogin.ChatForms.Add(this);
            this.gid = gid;
            this.gname = gname;
            this.groupName = groupName;
            this.Text = gname + "-" + groupName;
            TCPsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            TCPsocket.Connect(endPoint);
            UDPsocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            UDPsocket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            ServerLogin.ChatFormList.Add(groupName + "|" + gid + "|" + gname);
            if (groupName == "春田花花幼稚园")
            {
                ServerLogin.GroupChatList1.Add(gid);
            }
            else if (groupName == "学霸交流群")
            {
                ServerLogin.GroupChatList2.Add(gid);
            }
            ServerLogin.PortList[gid] = port;
            thread1 = new Thread(ReceiveUDP);
            thread1.Start();
            button2.Enabled = false;
        }

        //获取随机端口号
        void GetRandomPort()
        {
            while (true)
            {
                Random random = new Random();
                port = random.Next(1, 10000);
                if (!ServerLogin.hashtable.ContainsValue(port))
                {
                    ServerLogin.hashtable.Add(port, port);
                    break;
                }
            }
        }

        void ReceiveUDP()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    int r = UDPsocket.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    string str = Encoding.UTF8.GetString(buffer, 0, r);
                    string[] tmpMsg = str.Split('|');
                    /**一对一聊天的信息：gid == null 且 id == tmpMsg[1] 且 friendID == tmpMsg[0]
                     * 或群组聊天的信息： gid != tmpMsg[0] 且
                     * [(groupName == "春田花花幼稚园" 且 ServerLogin.GroupChatList1.Contains(gid)) 
                     * 或 (groupName == "学霸交流群" 且 ServerLogin.GroupChatList2.Contains(gid))]
                     */
                    if ((gid == null && id == tmpMsg[1] && friendID == tmpMsg[0]) || (gid != tmpMsg[0] && ((groupName == "春田花花幼稚园" && ServerLogin.GroupChatList1.Contains(gid)) || (groupName == "学霸交流群" && ServerLogin.GroupChatList2.Contains(gid)))))
                    {
                        FindFromWhere(tmpMsg);
                    }
                }
            }
            catch (Exception ex1)
            {
                if(ex1.HResult == -2147467259)
                {
                    return;
                }
                else
                {
                    MessageBox.Show(ex1.Message);
                }
            }
        }

        //查找信息是谁发的
        void FindFromWhere(string[] tmpMsg)
        {
            string sql = String.Format("select name from client where id = " + tmpMsg[0]);
            string fromName = null;
            try
            {
                conn.Open();
                MySqlCommand comm = new MySqlCommand(sql, conn);
                MySqlDataReader reader = comm.ExecuteReader();
                if (reader.Read())
                {
                    fromName = reader.GetString(0);
                }
                string newstr = "[" + DateTime.Now.ToString() + "]" + "\r\n" + fromName + ": " + tmpMsg[tmpMsg.Length - 1] + "\r\n";
                ShowChatMsg(newstr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "操作数据库出错！", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //this.Tag = false;
            }
            finally
            {
                conn.Close();
            }
        }

        //点击发送
        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (chatBox.Text == "")
                {
                    MessageBox.Show("请输入要发送的内容！", "提示");
                }
                else
                {
                    string newstr = "[" + DateTime.Now.ToString() + "]" + "\r\n" + "我: " + chatBox.Text.Trim() + "\r\n";
                    ShowChatMsg(newstr);
                    string str = null;
                    //一对一聊天
                    if (gid == null)
                    {
                        str = id + "|" + friendID + "|" + chatBox.Text.Trim();
                    }
                    //群组聊天
                    else
                    {
                        str = gid + "|";
                        if (groupName == "春田花花幼稚园")
                        {
                            foreach (string tmpID in ServerLogin.GroupChatList1)
                            {
                                if (tmpID != gid)
                                {
                                    str += tmpID + "|";
                                }
                            }
                        }
                        else if (groupName == "学霸交流群")
                        {
                            foreach (string tmpID in ServerLogin.GroupChatList2)
                            {
                                if (tmpID != gid)
                                {
                                    str += tmpID + "|";
                                }
                            }
                        }
                        str += chatBox.Text.Trim();
                    }
                    chatBox.Text = "";
                    byte[] buffer = Encoding.UTF8.GetBytes(str);
                    TCPsocket.Send(buffer);
                    if (ServerLogin.ChatFormList.Contains(friendID + "|" + id))
                    {
                        EndPoint point = new IPEndPoint(IPAddress.Parse(ip), ServerLogin.PortList[friendID + "|" + id]);
                        UDPsocket.SendTo(Encoding.UTF8.GetBytes(str), point);
                    }
                    else if(gid != null)
                    {
                        List<string> tmpGroupChatList = (groupName == "春田花花幼稚园") ? ServerLogin.GroupChatList1 : ServerLogin.GroupChatList2;
                        foreach (string tmpID in tmpGroupChatList)
                        {
                            EndPoint point = new IPEndPoint(IPAddress.Parse(ip), ServerLogin.PortList[tmpID]);
                            UDPsocket.SendTo(Encoding.UTF8.GetBytes(str), point);
                        }
                    }
                }
            }
            catch { }
        }

        //显示聊天信息
        void ShowChatMsg(string str)
        {
            try
            {
                textBox1.AppendText(str + "\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //关闭会话窗口后，从列表中删除该会话
        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(gid == null)
            {
                int i = ServerLogin.ChatFormList.IndexOf(id + "|" + friendID);
                ServerLogin.SocketList.Remove(ServerLogin.SocketList[i]);
                ServerLogin.ChatFormList.Remove(id + "|" + friendID);
                ServerLogin.PortList.Remove(id + "|" + friendID + "|" + port);
            }
            else
            {
                int i = ServerLogin.ChatFormList.IndexOf(groupName + "|" + gid + "|" + gname);
                ServerLogin.SocketList.Remove(ServerLogin.SocketList[i]);
                ServerLogin.ChatFormList.Remove(groupName + "|" + gid + "|" + gname);
                if (groupName == "春田花花幼稚园")
                {
                    ServerLogin.GroupChatList1.Remove(gid);
                }
                else if (groupName == "学霸交流群")
                {
                    ServerLogin.GroupChatList2.Remove(gid);
                }
                ServerLogin.PortList.Remove(gid);
            }
            //thread.Abort();
            UDPsocket.Dispose();
            ServerLogin.hashtable.Remove(port);
        }

        //关闭会话窗口
        public void CloseChatForm()
        {
            this.Close();
        }

        private void OpenFileDialog(object sender, EventArgs e)
        {                      
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                sendPath = openFileDialog1.FileName;
                sendFileNameExt = sendPath.Substring(sendPath.LastIndexOf("\\") + 1);
                string str = "是否确认传送 【" + sendPath + "】 给 ";
                str += (gid == null ? friendName : groupName);
                if(DialogResult.Yes == MessageBox.Show(str + " ?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    ServerLogin.SendFileList[id + "|" + friendID] = true;
                    timer1.Enabled = true;
                    miniForm = new MiniForm();                   
                    MessageBox.Show(miniForm, "正在等待对方接收...", "提示", MessageBoxButtons.OK, MessageBoxIcon.None);                   
                }
                else
                {
                    return;
                }
            }
        }

        public void SendFile()
        {
            FileInfo fileInfo = new FileInfo(sendPath);
            FileStream fileStream = fileInfo.OpenRead();
            //包的大小
            int packetSize = 5000;
            //包的数量
            int packetCount = (int)(fileInfo.Length / ((long)packetSize));
            //最后一个包的大小
            int lastPacketData = (int)(fileInfo.Length - ((long)packetSize * packetCount));
            byte[] data = new byte[packetSize];
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(IPAddress.Parse(ip), ServerLogin.PortList[friendID + "|" + id]));
                //发送本机昵称
                SendData(socket, Encoding.Unicode.GetBytes(name));
                //发送包的大小
                SendData(socket, Encoding.Unicode.GetBytes(packetSize.ToString()));
                //发送包的总数量
                SendData(socket, Encoding.Unicode.GetBytes(packetCount.ToString()));
                for (int i = 0; i < packetCount; i++)
                {
                    fileStream.Read(data, 0, data.Length);
                    SendData(socket, data);
                }
                if (lastPacketData != 0)
                {
                    data = new byte[lastPacketData];
                    fileStream.Read(data, 0, data.Length);
                    SendData(socket, data);
                }
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public static int SendData(Socket s, byte[] data)
        {
            int total = 0;
            int size = data.Length;
            int dataLeft = size;
            int sent;
            byte[] datasize = new byte[4];
            datasize = BitConverter.GetBytes(size);
            sent = s.Send(datasize);
            while (total < size)
            {
                sent = s.Send(data, total, dataLeft, SocketFlags.None);
                total += sent;
                dataLeft -= sent;
            }
            return total;
        }

        private void ReceiveFile(object o)
       {
            Socket socket = o as Socket;
            while (true)
            {
                if (ServerLogin.SendFileList[friendID + "|" + id])
                {
                    ServerLogin.SendFileList[friendID + "|" + id] = false;
                    if (MessageBox.Show(friendName + "正在向你发送文件，是否接收?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        foreach(ChatForm chatForm in ServerLogin.ChatForms)
                        {
                            if(chatForm.id == friendID && chatForm.friendID == id)
                            {
                                chatForm.SendFile();
                                saveFileNameExt = chatForm.sendFileNameExt;
                            }
                        }
                        Socket s = socket.Accept();
                        Thread receiveThread = new Thread(ReceiveFileData)
                        {
                            IsBackground = true
                        };
                        receiveThread.SetApartmentState(ApartmentState.STA);
                        receiveThread.Start(s);
                    }
                    else {
                        foreach (ChatForm chatForm in ServerLogin.ChatForms)
                        {
                            if (chatForm.id == friendID && chatForm.friendID == id)
                            {
                                chatForm.ShowChatMsg(name + " 已拒绝接收文件。\r\n");
                            }
                        }
                    }
                }
            }
        }

        private void ReceiveFileData(object o)
        {
            Socket mySocket = o as Socket;
            //文件大小
            string totalSize;
            //总的包数量
            int totalCount = 0;
            //统计已收的包的数量
            int receiveCount = 0;
            string fromName;
            //发送端的用户名字，用于确定对话框
            fromName = Encoding.Unicode.GetString(ReceiveData(mySocket));
            //文件大小
            totalSize = Encoding.Unicode.GetString(ReceiveData(mySocket));
            //总的包数量
            totalCount = int.Parse(Encoding.Unicode.GetString(ReceiveData(mySocket)));
            
            saveFileDialog1.FileName = saveFileNameExt;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                savePath = saveFileDialog1.FileName;
            }
            FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
            while (true)
            {
                byte[] data = ReceiveData(mySocket);
                //接收来自socket的数据
                if (data.Length == 0)
                {
                    fileStream.Write(data, 0, data.Length);
                    ShowProgressBar(totalCount + 1, totalCount);
                    ShowChatMsg("成功接收文件。");
                    foreach (ChatForm chatForm in ServerLogin.ChatForms)
                    {
                        if (chatForm.id == friendID && chatForm.friendID == id)
                        {
                            chatForm.ShowChatMsg(name + " 已接收文件。\r\n");
                        }
                    }
                    break;
                }
                else
                {
                    fileStream.Write(data, 0, data.Length);
                    receiveCount++;
                    ShowProgressBar(receiveCount, totalCount);
                    Thread.Sleep(1000);
                }
            }
            fileStream.Close();
            mySocket.Close();
        }

        public void ShowProgressBar(int value, int max)
        {
            if (progressBar1.InvokeRequired)
            {
                ProcessingCallback d = new ProcessingCallback(ShowProgressBar);
                this.Invoke(d, new object[] { value , max });
            }
            else
            {
                progressBar1.Visible = true;
                progressBar1.Maximum = max;
                if (value > max)
                {
                    progressBar1.Visible = false;
                }
                else
                {
                    progressBar1.Value = value;
                }
            }
        }

        private static byte[] ReceiveData(Socket s)
        {
            int total = 0;
            int recv;
            byte[] datasize = new byte[4];
            recv = s.Receive(datasize, 0, 4, SocketFlags.None);
            int size = BitConverter.ToInt32(datasize, 0);
            int dataLeft = size;
            byte[] data = new byte[size];
            while (total < size)
            {
                recv = s.Receive(data, total, dataLeft, SocketFlags.None);
                if (recv == 0)
                {
                    data = null;
                    break;
                }
                total += recv;
                dataLeft -= recv;
            }
            return data;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            miniForm.Close();
        }
    }
}