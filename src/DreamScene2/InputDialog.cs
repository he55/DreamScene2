using System;
using System.Windows.Forms;

namespace DreamScene2
{
    public partial class InputDialog : Form
    {
        public InputDialog()
        {
            InitializeComponent();
            this.Icon = DreamScene2.Properties.Resources.icon;
            label2.Text = "";
        }

        public string URL => textBox1.Text;

        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                try
                {
                    _ = new Uri(textBox1.Text);
                }
                catch
                {
                    label2.Text = "输入的 URL 无效";
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                label2.Text = "输入不能为空";
            }
        }
    }
}
