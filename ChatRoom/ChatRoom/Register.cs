using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace ChatRoom
{
    public partial class Register : Form
    {
        private static string connString = "server=localhost;user id=root;persistsecurityinfo=True;database=chatroom;port=3306;password=123";
        private MySqlConnection conn = new MySqlConnection(connString);

        public Register()
        {
            InitializeComponent();
            radioButton1.Select();
        }

        //注册账号
        private void button1_Click(object sender, EventArgs e)
        {
            string newId = textBox1.Text;
            string newName = textBox2.Text;
            string newPwd = textBox3.Text;
            string tmpPwd = textBox4.Text;
            if(newPwd != tmpPwd)
            {
                MessageBox.Show("请重新确认密码！");
            }
            else
            {
                string sql = String.Format("select count(*) from client where id = " + newId);
                try
                {
                    conn.Open();
                    MySqlCommand comm = new MySqlCommand(sql, conn);
                    int n = (int)(long)comm.ExecuteScalar();
                    if (n == 0)
                    {
                        string sex = "0";
                        if (radioButton2.Checked)
                        {
                            sex = "1";
                        }
                        sql = String.Format("insert into client(id, password, name, sex) values ("+ newId + ", " + newPwd + ",'" + newName + "'," + sex + ")");
                        comm = new MySqlCommand(sql, conn);
                        int m = (int)(long)comm.ExecuteNonQuery();
                        if(m > 0)
                        {
                            MessageBox.Show("注册成功！");
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("注册失败！");
                        }
                    }
                    else
                    {
                        MessageBox.Show("该账号已存在！");
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

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)13 && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }
        }
    }
}
