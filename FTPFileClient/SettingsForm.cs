using System;
using System.IO;
using System.Windows.Forms;

namespace FTPFileClient
{
    public partial class SettingsForm : Form
    {
        private const string congifFile = "config.dat";
        /* ПОЛЯ КЛАССА */
        // поле для хранения имени FTP-сервера
        private string host;
        // поле для хранения номера порта
        private string port;
        // поле для хранения типа входа
        private string enterType;
        // поле для хранения логина
        private string userName;
        // поле для хранения пароля
        private string password;
        // флаг отображения пароля
        private bool flagDisplayPass;
        // поле для хранения режима передачи
        private int transferMode;
        // поле для хранения пути к локальному каталогу по умолчанию
        private string localDir;
        // поле для хранения пути к удаленному каталогу по умолчанию
        private string netDir;

        private FileProcessing fProc = new FileProcessing();

        public SettingsForm()
        {
            InitializeComponent();

            // значения по умолчанию
            comboBox1.SelectedIndex = 0;
            enterType = "Анонимный";
            userName = "anonymous";
            flagDisplayPass = false;
            comboBox2.SelectedIndex = 0;
            transferMode = 0;
            // чтение данных из файла config.dat
            setConfig();
        }

        #region getters
        public string Host
        {
            get
            {
                return host;
            }
        }
        public string Port
        {
            get
            {
                return port;
            }
        }
        public string EnterType
        {
            get
            {
                return enterType;
            }
        }
        public string UserName
        {
            get
            {
                return userName;
            }
        }
        public string Password
        {
            get
            {
                return password;
            }
        }
        public bool FlagDisplayPass
        {
            get
            {
                return flagDisplayPass;
            }
        }
        public int TransferMode
        {
            get
            {
                return transferMode;
            }
        }
        public string LocalDir
        {
            get
            {
                return localDir;
            }
        }
        public string NetDir
        {
            get
            {
                return netDir;
            }
        }
        #endregion

        // кнопка "Обзор..."
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBox5.Text = folderBrowserDialog1.SelectedPath;
        }

        // проверка при вводе значений Порта
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (!Char.IsDigit(number) && number != 8)  // если введена не цифра и не <-
            {
                e.Handled = true;
            }
        }

        // обработчик выбора типа входа
        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != 0)
            {
                textBox3.Enabled = true;
                textBox4.Enabled = true;
                checkBox1.Enabled = true;
            }
            else
            {
                // если тип входа Анонимный
                textBox3.Enabled = false;
                textBox4.Enabled = false;
                checkBox1.Checked = false;
                checkBox1.Enabled = false;
            }
            enterType = comboBox1.SelectedItem.ToString();
        }

        // обработчик изменения флажка "Отображать пароль"
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true) // если флажок отмечен
            {
                flagDisplayPass = true;
                textBox4.PasswordChar = '\0';
            }
            else
            {
                flagDisplayPass = false;
                textBox4.PasswordChar = '*';
            }
        }

        // обработчик нажатия кнопки ОК
        private void okButton_Click(object sender, EventArgs e)
        {
            // получение всех введенных данных
            host = textBox1.Text;
            port = textBox2.Text;
            if (enterType != "Анонимный")
            {
                userName = textBox3.Text;
                password = textBox4.Text;
            }
            else
            {
                userName = "anonymous";
                password = "";
            }
            string strFDPass = flagDisplayPass ? "1" : "0";
            localDir = textBox5.Text;
            netDir = textBox6.Text;
            transferMode = comboBox2.SelectedIndex;

            // запись данных в файл config.dat
            fProc.writeConfig(host, true);
            fProc.writeConfig(port, false);
            fProc.writeConfig(enterType, false);
            fProc.writeConfig(userName, false);
            fProc.writeConfig(password, false);
            fProc.writeConfig(strFDPass, false);
            fProc.writeConfig(localDir, false);
            fProc.writeConfig(netDir, false);
            fProc.writeConfig(Convert.ToString(transferMode), false);

            this.Close();
        }

        // процедура установки данных из файла
        private void setConfig()
        {
            string textFromFile;
            int count = 0;
            // чтение данных из файла
            StreamReader stream = null;
            try
            {
                stream = new StreamReader(congifFile);
                stream.Close();
                textFromFile = fProc.readConfig();

                // обработка полученной строки
                host = fProc.findeData(textFromFile, 0);
                count += (host.Length + 1);
                port = fProc.findeData(textFromFile, count);
                count += (port.Length + 1);
                enterType = fProc.findeData(textFromFile, count);
                count += (enterType.Length + 1);
                userName = fProc.findeData(textFromFile, count);
                count += (userName.Length + 1);
                password = fProc.findeData(textFromFile, count);
                count += (password.Length + 1);
                string strFDPass = fProc.findeData(textFromFile, count);
                flagDisplayPass = strFDPass == "1" ? true : false;
                count += (strFDPass.Length + 1);
                localDir = fProc.findeData(textFromFile, count);
                count += (localDir.Length + 1);
                netDir = fProc.findeData(textFromFile, count);
                count += (netDir.Length + 1);
                transferMode = Convert.ToInt32(fProc.findeData(textFromFile, count));

                getData();
            }
            catch (Exception)
            {
                if (stream != null)
                    stream.Close();
            }
        }

        // заполнение полей по умолчанию в случае наличия файла config.dat
        private void getData()
        {
            textBox1.Text = host;
            textBox2.Text = port;
            if (enterType == "Анонимный")
            {
                comboBox1.SelectedIndex = 0;
                textBox3.Text = "";
            }
            else
            {
                comboBox1.SelectedIndex = 1;
                textBox3.Enabled = true;
                textBox3.Text = userName;
                textBox4.Enabled = true;
                checkBox1.Enabled = true;
            }
            textBox4.Text = password;
            if (flagDisplayPass)
            {
                checkBox1.Checked = true;
                textBox4.PasswordChar = '\0';
            }
            comboBox2.SelectedIndex = transferMode;
            textBox5.Text = localDir;
            textBox6.Text = netDir;
        }

        // обработчик нажатия кнопки Отмена
        private void cancellButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
