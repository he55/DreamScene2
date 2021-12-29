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
            lblTipMsg.Text = "";
        }

        public string URL => txtUrl.Text;

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUrl.Text))
            {
                try
                {
                    _ = new Uri(txtUrl.Text);
                }
                catch
                {
                    lblTipMsg.Text = "输入的 URL 无效";
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblTipMsg.Text = "输入不能为空";
            }
        }
    }
}
