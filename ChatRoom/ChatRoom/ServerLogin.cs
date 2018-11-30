using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatRoom
{

    public partial class ServerLogin : Form
    {
        //是否初次开启服务器
        Boolean first = true;
        //是否已经开启服务器
        Boolean open = false;
        //服务器是否意外断开
        public static Boolean isAccident = false;
        //监听线程
        Thread thread1 = null;
        //用于监听的Socket
        Socket serverSocket = null;
        //用于连接的Socket
        Socket acceptSocket = null;
        private static string connString = "server=localhost;user id=root;persistsecurityinfo=True;database=chatroom;port=3306;password=123";
        private MySqlConnection conn = new MySqlConnection(connString);
        public static Hashtable hashtable { get; set; }
        public static ArrayList ClientList_ID { get; set; }
        public static ArrayList ClientList_Name { get; set; }
        public static Dictionary<string, int> PortList { get; set; }
        public static Dictionary<string, bool> SendFileList { get; set; }
        public static string[] ClientList_ID_temp { get; set; }
        public static ArrayList ChatFormList { get; set; }
        public static List<ChatForm> ChatForms { get; set; }
        public static List<MainList> MainLists { get; set; }
        public static List<Socket> SocketList { get; set; }
        //"春田花花幼稚园"群聊组的组员登录列表
        public static List<string> GroupChatList1 { get; set; }
        //"学霸交流群"群聊组的组员登录列表
        public static List<string> GroupChatList2 { get; set; }

        public ServerLogin()
        {
            InitializeComponent();
            //防止新线程调用主线程卡死
            CheckForIllegalCrossThreadCalls = false;
            hashtable = new Hashtable();
            ClientList_ID = new ArrayList();
            ClientList_Name = new ArrayList();
            PortList = new Dictionary<string, int>();
            SendFileList = new Dictionary<string, bool>();
            ClientList_ID_temp = null;
            ChatFormList = new ArrayList();
            ChatForms = new List<ChatForm>();
            MainLists = new List<MainList>();
            SocketList = new List<Socket>();
            GroupChatList1 = new List<string>();
            GroupChatList2 = new List<string>();
        }

        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取本机IP出错:" + ex.Message);
                return "";
            }
        }

        //点击开启服务器后
        private void StartServer(object sender, EventArgs e)
        {
            if (!open)
            {
                if (first)
                {
                    // 连接服务器后返回信息到信息表中
                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    textBox1.Text = GetLocalIP();
                    IPAddress ip = IPAddress.Parse(GetLocalIP());
                    IPEndPoint endPoint = new IPEndPoint(ip, int.Parse(textBox2.Text));
                    //绑定端口号
                    serverSocket.Bind(endPoint);
                    //设置监听
                    serverSocket.Listen(100);
                    ShowMsg("开启监听！");
                    first = false;
                }
                else
                {
                    ShowMsg("服务器重新连接！");
                    for(int i = 0; i < ClientList_ID_temp.Length; i++)
                    {
                        //创建监听线程
                        thread1 = new Thread(ListenSocket)
                        {
                            IsBackground = true
                        };
                        thread1.Start(serverSocket);
                        ClientLogin form1 = new ClientLogin(ClientList_ID_temp[i]);
                        form1.Client_Online += new Online(ShowClientList1);
                        form1.Offline_Server += new OfflineOnServer(ShowClientList2);
                        form1.Show();
                    }
                }
                open = true;
                isAccident = false;
            }
        }

        private void Login(object sender, EventArgs e)
        {
            if (open)
            {
                //创建监听线程
                thread1 = new Thread(ListenSocket)
                {
                    IsBackground = true
                };
                thread1.Start(serverSocket);
                ClientLogin form1 = new ClientLogin();
                form1.Client_Online += new Online(ShowClientList1);
                form1.Offline_Server += new OfflineOnServer(ShowClientList2);
                form1.Show();
            }
        }

        private void CloseServer(object sender, EventArgs e)
        {
            if (open)
            {
                ShowMsg("服务器断开！");
                isAccident = true;
                ClientList_ID_temp = new string[ClientList_ID.Count];
                for (int i = 0; i < ClientList_ID.Count;i++){
                    ClientList_ID_temp.SetValue(ClientList_ID[i], i);
                }
                foreach (ChatForm form1 in ChatForms)
                {
                    form1.CloseChatForm();
                }
                foreach (MainList form2 in MainLists)
                {
                    form2.CloseMainList();
                }
                ChatForms.Clear();
                MainLists.Clear();
                open = false;
                if(acceptSocket != null)
                {
                    acceptSocket.Disconnect(false);
                    ChatFormList.Clear();
                }
            }
        }

        //Socket服务监听
        void ListenSocket(object o)
        {
            Socket serverSocket = o as Socket;
            while (open)
            {
                acceptSocket = serverSocket.Accept();
                SocketList.Add(acceptSocket);
                ShowMsg("连接成功！");
                //创建新线程，执行接收消息的方法
                Thread thread2 = new Thread(Received)
                {
                    IsBackground = true
                };
                thread2.Start(acceptSocket);
            }
        }

        void Received(object o)
        {
            try
            {
                Socket acceptSocket = o as Socket;
                byte[] buffer = new byte[1024];
                byte[] buffer1 = new byte[1024];
                string tmp = null;
                string[] result = null;
                while (true)
                {
                    int r = acceptSocket.Receive(buffer);
                    if (r <= 0)
                    {
                        break;
                    }
                    tmp = Encoding.UTF8.GetString(buffer, 0, r);
                    result = tmp.Split('|');
                    string content = result[0] + "向";
                    for (int i = 1; i < result.Length - 2; i++)
                    {
                        content += (result[i] + "、");
                    }
                    content += (result[result.Length - 2] + "发送了: " + result[result.Length - 1]);
                    ShowMsg(content);
                    buffer1 = Encoding.UTF8.GetBytes(tmp);
                    foreach (Socket soc in SocketList)
                    {
                        soc.Send(buffer1);
                    }
                    try
                    {
                        conn.Open();
                        string sql = String.Format("select name from client where id = " + result[0]);
                        MySqlCommand comm = new MySqlCommand(sql, conn);
                        MySqlDataReader reader = comm.ExecuteReader();
                        if (reader.Read())
                        {
                            //没有打开对话框
                            if (!ServerLogin.ChatFormList.Contains(result[result.Length - 2] + "|" + result[0]))
                            {
                                foreach (MainList form in MainLists)
                                {
                                    if(form.id == result[result.Length - 2])
                                    {
                                        form.AddNotice(result[result.Length - 2], reader.GetString(0));
                                    }   
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "操作数据库出错！", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message);
            }
        }

        //listBox1信息显示内容
        void ShowMsg(string str)
        {
            string tempStr = "";
            if(textBox3.Text != "")
            {
                tempStr = textBox3.Text + "\r\n";
            }
            textBox3.Text = tempStr + str;
        }

        //listBox2客户列表:上线
        void ShowClientList1(string id, string name)
        {
            string tempStr = "";
            if (textBox4.Text != "")
            {
                tempStr = textBox4.Text + "\r\n";
            }
            textBox4.Text = tempStr + name + " (" + id + ") 上线";
        }

        //listBox2客户列表：离线
        void ShowClientList2(string id, string name)
        {
            string tempStr = "";
            if (textBox4.Text != "")
            {
                tempStr = textBox4.Text + "\r\n";
            }
            textBox4.Text = tempStr + name + " (" + id + ") 离线";
        }
    }
}
