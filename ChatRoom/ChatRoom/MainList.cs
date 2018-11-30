using System;
using System.Windows.Forms;

namespace ChatRoom
{
    //声明委托
    public delegate void Offline(string id, string name);

    public partial class MainList : Form
    {
        //声明委托实例
        public event Offline Client_Offline;
        public string id = null;
        string name = null;
        string sex = null;

        public MainList(string id, string name, string sex)
        {
            InitializeComponent();
            ServerLogin.MainLists.Add(this);
            this.id = id;
            this.name = name;
            this.sex = sex;
            this.Text = "Chat - " + name;
            //用于测试多对多聊天
            if(Convert.ToInt32(id) % 2 == 0 || Convert.ToInt32(id) % 3 == 0)
            {
                listBox2.Items.Add("春田花花幼稚园");
            }
            else
            {
                listBox2.Items.Add("学霸交流群");
            }
        }

        //关闭窗口时
        private void MainList_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ServerLogin.isAccident)
            {
                if (DialogResult.OK == MessageBox.Show("确定退出？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
                {
                    this.FormClosing -= new FormClosingEventHandler(this.MainList_FormClosing);
                    ServerLogin.ClientList_ID.Remove(id);
                    ServerLogin.ClientList_Name.Remove(name);
                    Client_Offline(id, name);
                    ServerLogin.MainLists.Remove(this);
                    this.Close();
                }
                else
                {
                    e.Cancel = true;  //取消关闭事件
                }
            }
        }

        private void MainList_Load(object sender, EventArgs e)
        {
            if(sex == "0")
            {
                sex = "男";
            }
            else
            {
                sex = "女";
            }
            textBox1.Text = "名称: " + name + "\r\n账号: " + id + "\r\n性别: " + sex;
            timer1.Enabled = true;
            timer1.Interval = 1000;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox1.Items.Add("我 （" + id + "）");
            for (int i = 0; i < ServerLogin.ClientList_ID.Count; i++)
            {
                if ((string)ServerLogin.ClientList_ID[i] != id)
                {
                    listBox1.Items.Add((string)ServerLogin.ClientList_Name[i] + " (" + (string)ServerLogin.ClientList_ID[i] + ")");
                }
            }
        }

        //在线好友
        private void ListBox1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                int index = listBox1.IndexFromPoint(e.X, e.Y);
                string[] friendStr = null;
                if (index != -1 && index != 0)
                {
                    friendStr = listBox1.SelectedItem.ToString().Split(new char[3] { ' ', '(', ')' });
                    if (!(ServerLogin.ChatFormList.Contains(id + "|" + friendStr[2])))
                    {
                        new ChatForm(id, name, friendStr[2], friendStr[0]).Show();
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        //聊天群组
        private void ListBox2_MouseClick(object sender, MouseEventArgs e)
        {
            int index = listBox2.IndexFromPoint(e.X, e.Y);
            if (index != -1)
            {
                if (!(ServerLogin.ChatFormList.Contains(listBox2.SelectedItem.ToString() + "|" + id + "|" + name)))
                {
                    new ChatForm(listBox2.SelectedItem.ToString(), id, name).Show();
                }
            }
        }

        public void AddNotice(string fromID, string fromName)
        {
            MessageBox.Show(fromName + " 发来了消息。", "消息通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void CloseMainList()
        {
            this.FormClosing -= new FormClosingEventHandler(this.MainList_FormClosing);
            ServerLogin.ClientList_ID.Remove(id);
            ServerLogin.ClientList_Name.Remove(name);
            Client_Offline(id, name);
            this.Close();
        }
    }
}
