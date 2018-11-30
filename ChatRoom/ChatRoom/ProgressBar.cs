using System;
using System.Windows.Forms;

namespace ChatRoom
{

    public partial class ProgressBar : Form
    {
        public int max = 100;

        public ProgressBar(int max)
        {
            InitializeComponent();
            this.max = max;
            this.progressBar1.Maximum = max;
        }

        public bool Increase(int value)
        {
            if (value > 0)
            {
                if (progressBar1.Value + value < progressBar1.Maximum)
                {
                    progressBar1.Value += value;
                    this.textLabel.Text = value + "/" + max;
                    Application.DoEvents();
                    progressBar1.Update();
                    progressBar1.Refresh();
                    this.textLabel.Update();
                    this.textLabel.Refresh();
                    return true;
                }
                else
                {
                    progressBar1.Value = progressBar1.Maximum;
                    this.textLabel.Text = value + "/" + max;
                    this.Close();//执行完之后，自动关闭子窗体
                    return false;
                }
            }
            return false;
        }     
    }
}

