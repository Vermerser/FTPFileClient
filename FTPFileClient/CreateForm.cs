using System;
using System.Windows.Forms;

namespace FTPFileClient
{
    public partial class CreateForm : Form
    {
        public string folder = "";
        public CreateForm()
        {
            InitializeComponent();
        }

        // ОК
        private void button1_Click(object sender, EventArgs e)
        {
            folder = textBox1.Text;
        }

        // Отмена
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
