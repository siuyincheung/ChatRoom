using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ChatRoom
{
    //声明委托
    public delegate void Online(string id, string name);
    public delegate void OfflineOnServer(string id, string name);

    public partial class ClientLogin : Form
    {
        //声明委托实例
        public event Online Client_Online;
        public event OfflineOnServer Offline_Server;
        private static string connString = "server=localhost;user id=root;persistsecurityinfo=True;database=chatroom;port=3306;password=123";
        private MySqlConnection conn = new MySqlConnection(connString);
        
        string id = null;
        string password = null;
        //public Boolean isLogined = false;

        public ClientLogin()
        {
            InitializeComponent();
            //防止新线程调用主线程卡死
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }
        
        public ClientLogin(string tempid)
        {
            InitializeComponent();
            //防止新线程调用主线程卡死
            TextBox.CheckForIllegalCrossThreadCalls = false;
            this.textBox1.Text = tempid;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            id = this.textBox1.Text;
            password = this.textBox2.Text;
            string name = null;
            string sex = null;
            string sql = String.Format("select count(*) from client where id = " + id + " and password = '" + password + "'");
            try
            {
                conn.Open();
                MySqlCommand comm = new MySqlCommand(sql, conn);
                int n = (int)(long)comm.ExecuteScalar();
                if (n != 1)
                {
                    MessageBox.Show("账号或密码输入错误，请重新输入！");
                    //this.Tag = false;
                }
                else if (ServerLogin.ClientList_ID.Contains(id))
                {
                    MessageBox.Show("该账号已登录！");
                    //this.Tag = false;
                }
                else
                {
                    sql = String.Format("select name, sex from client where id = " + id); 
                    comm = new MySqlCommand(sql, conn);
                    MySqlDataReader reader = comm.ExecuteReader();
                    if (reader.Read())
                    {
                        name = reader.GetString(0);
                        sex = reader.GetString(1);
                    }
                    ServerLogin.ClientList_ID.Add(id);
                    ServerLogin.ClientList_Name.Add(name);
                    this.DialogResult = DialogResult.OK;
                    Client_Online(id, name);
                    MainList form = new MainList(id, name, sex);
                    form.Client_Offline += new Offline(OfflineServer);
                    form.Show();
                    this.Close();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "操作数据库出错！", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //this.Tag = false;
            }
            finally
            {
                conn.Close();
            }
        }

        //只能输入数字和回车键
        private void TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)13 && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }
            if(e.KeyChar == (char)13)
            {
                Button1_Click(sender, e);
            }
        }

        //注册账号
        private void Label3_Click(object sender, EventArgs e)
        {
            new Register().Show();
        }

        void OfflineServer(string id, string name)
        {
            Offline_Server(id, name);
        }

        private void TextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                Button1_Click(sender, e);
            }
        }
    }
}
