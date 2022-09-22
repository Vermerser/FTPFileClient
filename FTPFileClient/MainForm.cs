using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FTPFileClient
{
    public partial class MainForm : Form
    {
        private const string congifFile = "config.dat";
        private FileProcessing _fProc = new FileProcessing();
        // строка хранения имени каталога на компьютере
        private string localDir = "";
        // строка хранения имени каталога на сервере
        private string serverDir = "/";
        // флаг режима передачи данных (true - Пассивный, false - Активный)
        private bool fTransferMode = true;
        // массив структур файлов на FTP сервере
        private FileStruct[] FileList;

        BindingList<string> dataSource = new BindingList<string>();

        public MainForm()
        {
            InitializeComponent();

            // для обработки события при добавлении нового элемента в окно информации
            listBoxInfo.DataSource = dataSource;
            dataSource.ListChanged += listBoxInfoChanged;

            toolStripStatusLabel1.ForeColor = Color.DarkRed;
            // заполнение полей установки соединения при наличии файла конфигурации
            getData();
            printFiles(localDir, listViewLocal, true);   // заполнение списка локальных файлов
            listViewLocal.ContextMenuStrip = contextMenuStripLeft;  // ассоциация контекстного меню с локальными данными
            listViewDistant.ContextMenuStrip = contextMenuStripRight;  // ассоциация контекстного меню с данными сервера
        }

        FtpClient ftpClient = new FtpClient();

        #region menuItems
        /*ОСНОВНОЕ МЕНЮ ПРОГРАММЫ------------------*/
        // Настройки
        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm sForm = new SettingsForm();
            if (sForm.ShowDialog(this) == DialogResult.OK)
            {
                getData();
                printFiles(localDir, listViewLocal, true);
            }
        }

        // Обновить
        private void UpdateMenuItem_Click(object sender, EventArgs e)
        {
            printFiles(localDir, listViewLocal, true);
            if (label9.Visible == false)
            {
                FileList = ftpClient.ListDirectory(serverDir, fTransferMode);
                printFiles(serverDir, listViewDistant, false);
            }
        }

        // Открыть помощь
        private void HelpMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "help.chm");
        }

        // О программе...
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm abForm = new AboutForm();
            abForm.ShowDialog();
        }

        // Выход
        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /*-----------------------------------------*/
        #endregion

        // обработчик изменения флажка "Анонимный вход"
        private void checkBoxAnonym_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAnonym.Checked)
            {
                textBoxUserName.Enabled = false;
                textBoxUserName.Clear();
                textBoxPassword.Enabled = false;
                textBoxPassword.Clear();
            }
            else
            {
                textBoxUserName.Enabled = true;
                textBoxPassword.Enabled = true;
            }
        }

        #region buttons
        // соединение с FTP-сервером
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (!fieldsChecking())
                return;
            else
            {
                ftpClient.Host = textBoxHost.Text;
                if (checkBoxAnonym.Checked)
                    ftpClient.UserName = "anonymous";
                else
                {
                    ftpClient.UserName = textBoxUserName.Text;
                    ftpClient.Password = textBoxPassword.Text;
                }
                ftpClient.Port = textBoxPort.Text;

                dataSource.Add(String.Format("Статус:  Подключение к FTP-серверу {0}:{1}...", ftpClient.Host, ftpClient.Port));
                // метод протокола FTP LIST, который выдает подробный список файлов на FTP-сервере
                FileList = ftpClient.ListDirectory(serverDir, fTransferMode);
                if (FileList == null)
                {
                    dataSource.Add("Статус:  Соединение с сервером не установлено");
                    return;
                }
                
                try
                {
                    printFiles(serverDir, listViewDistant, false);

                    label6.Enabled = true;
                    textBoxServerDir.Enabled = true;
                    label9.Visible = false;
                    DisconnectButton.Enabled = true;
                    contextMenuStripRight.Enabled = true;
                    contextMenuStripRight.Items[2].Enabled = true;
                    contextMenuStripRight.Items[3].Enabled = true;
                    toolStripStatusLabel1.Text = String.Format("Соединение с сервером {0} установлено", ftpClient.Host);
                    toolStripStatusLabel1.ForeColor = Color.Blue;
                    dataSource.Add("Статус:  Соединение с сервером установлено");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // разорвать соединение с FTP-сервером
        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            ftpClient.DisconnectServer(serverDir, fTransferMode);
            DisconnectButton.Enabled = false;
            contextMenuStripLeft.Items[0].Enabled = false;
            contextMenuStripRight.Enabled = false;
            label6.Enabled = false;
            textBoxServerDir.Text = "";
            textBoxServerDir.Enabled = false;
            listViewDistant.Items.Clear();
            label9.Visible = true;
            label8.Text = "Нет соединения!";
            dataSource.Add("Статус:  Соединение разорвано...");
            toolStripStatusLabel1.Text = "Соединение с сервером отсутствует!";
            toolStripStatusLabel1.ForeColor = Color.DarkRed;
        }

        // обработчик кнопки "Загрузить файл" для загрузки файла на сервер
        private void DownloadButton_Click(object sender, EventArgs e)
        {
            int numFiles = listViewLocal.SelectedItems.Count;
            bool[] dirs = new bool[numFiles]; // номера выделенных папок
            if (numFiles == 0)
                return;
            string[] mFiles = new string[numFiles];   // массив с названиями файлов для сохранения
            // заполнение массива
            int i = 0;
            foreach (ListViewItem lv in listViewLocal.SelectedItems)
            {
                mFiles[i] = localDir + "\\" + lv.Text;
                if (lv.SubItems[1].Text == "") // если выделена папка для загрузки
                    dirs[i] = true;
                i++;
            }
            // непосредственная загрузка файлов
            try
            {
                i = 0;
                foreach (string file in mFiles)
                {
                    // если выбрана папка на загрузку
                    if (dirs[i])
                        uploadDirectory(serverDir, file);
                    else    // загрузить только файл
                    {
                        dataSource.Add(String.Format("Статус:  Загрузка файла {0}", file));
                        ftpClient.UploadFile(serverDir, fTransferMode, file);
                        dataSource.Add(String.Format("Статус:  Файл загружен на сервер успешно. Загружено {0} байт.",
                            listViewLocal.SelectedItems[i].SubItems[1].Text));
                    }
                    i++;
                }
                // обновление окна с данными на сервере
                FileList = ftpClient.ListDirectory(serverDir, fTransferMode);
                printFiles(serverDir, listViewDistant, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // обработчик кнопки "Сохранить файл" для сохранения файла с сервера на локальном диске
        private void SaveButton_Click(object sender, EventArgs e)
        {
            int numFiles = listViewDistant.SelectedItems.Count;
            bool[] dirs = new bool[numFiles]; // номера выделенных папок
            if (numFiles == 0)
                return;
            string[] mFiles = new string[numFiles];   // массив с названиями файлов для загрузки
            // заполнение массива
            int i = 0;
            foreach (ListViewItem lv in listViewDistant.SelectedItems)
            {
                mFiles[i] = lv.Text;
                if (lv.SubItems[1].Text == "") // если выделена папка для сохранения
                    dirs[i] = true;
                i++;
            }
            // непосредственное сохранение файлов
            try
            {
                i = 0;
                foreach (string file in mFiles)
                {
                    // если выбрана папка на сохранение
                    if (dirs[i])
                        downloadDirectory(serverDir, localDir, file);
                    else    // сохранить только файл
                    {
                        dataSource.Add(String.Format("Статус:  Сохранение файла {0}", file));
                        ftpClient.DownloadFile(serverDir, fTransferMode, localDir, file);
                        dataSource.Add(String.Format("Статус:  Файл сохранен успешно. Загружено {0} байт.",
                            listViewDistant.SelectedItems[i].SubItems[1].Text));
                    }
                    i++;
                }
                // обновление окна с данными на локальном диске
                printFiles(localDir, listViewLocal, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // обработчик кнопки "Удалить файл" для удаления файла или каталога с сервера
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            string sMessage = "Удалить ";
            int numFiles = listViewDistant.SelectedItems.Count;
            int numDirs = 0;
            bool[] dirs = new bool[numFiles]; // номера выделенных папок
            if (numFiles == 0)
                return;
            string[] mFiles = new string[numFiles];   // массив с названиями файлов для удаления
            // заполнение массива
            int i = 0;
            foreach (ListViewItem lv in listViewDistant.SelectedItems)
            {
                if (serverDir == "/")
                    mFiles[i] = "/" + lv.Text;
                else
                    mFiles[i] = serverDir + "/" + lv.Text;
                if (lv.SubItems[1].Text == "") // если выделена папка на удаление
                {
                    dirs[i] = true;
                    numDirs++;
                }
                i++;
            }
            numFiles -= numDirs;
            if (numFiles == 1)
                sMessage += "файл ";
            else if (numFiles > 1)
                sMessage += "файлы ";
            if (numFiles > 0 && numDirs > 0)
                sMessage += "и ";
            if (numDirs == 1)
                sMessage += "папку ";
            else if (numDirs > 1)
                sMessage += "папки ";
            sMessage += "с сервера?";
            // запрос на подтверждение удаления файлов
            DialogResult confirmationResult = MessageBox.Show(
                    sMessage,
                    "Подтверждение на удаление",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

            if (confirmationResult == DialogResult.Yes)
            {
                // непосредственно удаление
                try
                {
                    i = 0;
                    foreach (string file in mFiles)
                    {
                        dataSource.Add(String.Format("Статус:  Удаление \"{0}\"", file));
                        if (dirs[i]) // если необходимо удалить папку с содержимым
                            removeDirectory(file);
                        else // удалить только файл
                        {
                            ftpClient.DeleteFile(file, fTransferMode);
                            dataSource.Add(String.Format("Статус:  Файл \"{0}\" успешно удален", file));
                        }
                        i++;
                    }
                    // обновление окна с данными на сервере
                    FileList = ftpClient.ListDirectory(serverDir, fTransferMode);
                    printFiles(serverDir, listViewDistant, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        #endregion

        #region contextMenu
        /* КОНТЕКСТНОЕ МЕНЮ ОКНА С ЛОКАЛЬНЫМИ ФАЙЛАМИ */

        // Загрузить на сервер
        private void dounloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadButton_Click(sender, e);
        }

        // Создать каталог
        private void createToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateForm cForm = new CreateForm();
            if (cForm.ShowDialog(this) == DialogResult.OK)
            {
                string directory;
                if (localDir.Length > 3)
                    directory = localDir + "\\" + cForm.folder;
                else
                    directory = localDir + cForm.folder;
                // непосредственно создание каталога
                Directory.CreateDirectory(directory);
                printFiles(localDir, listViewLocal, true);
            }
        }

        // Обновить
        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printFiles(localDir, listViewLocal, true);
        }
        /* КОНТЕКСТНОЕ МЕНЮ ОКНА С ФАЙЛАМИ НА СЕРВЕРЕ */

        // Скачать
        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveButton_Click(sender, e);
        }

        // Создать каталог
        private void createToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CreateForm cForm = new CreateForm();
            if (cForm.ShowDialog(this) == DialogResult.OK)
            {
                string directory;
                if (serverDir.Length == 1)
                    directory = "/";
                else
                    directory = serverDir + "/";
                // непосредственно создание каталога
                ftpClient.CreateDirectory(directory, fTransferMode, cForm.folder);
                FileList = ftpClient.ListDirectory(serverDir, fTransferMode);
                printFiles(serverDir, listViewDistant, false);
            }
        }

        // Обновить
        private void updateToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FileList = ftpClient.ListDirectory(serverDir, fTransferMode);
            printFiles(serverDir, listViewDistant, false);
        }

        // Удалить
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteButton_Click(sender, e);
        }
        #endregion

        #region listviews
        // обработчик получения файлов в виделенной локальной папке
        private void listViewLocal_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string tmpDir = textBoxLocalDir.Text;
            if (listViewLocal.SelectedItems.Count == 0)
                return;
            ListViewItem item = listViewLocal.SelectedItems[0];
            // если выбрана корневая папка
            if (item.Text == "..")
            {
                int index = tmpDir.LastIndexOf("\\");
                if ((tmpDir[index - 1] == ':') && (tmpDir.Length > 3))   // если папка лежит в корне логического диска
                    tmpDir = tmpDir.Substring(0, index + 1);
                else if (tmpDir.Length == 3) // если корневая папка является корнем диска
                    tmpDir = "";
                else
                    tmpDir = tmpDir.Substring(0, index);
                localDir = tmpDir;
                printFiles(localDir, listViewLocal, true);
            }
            // если выбрана произвольная папка
            else
            {
                if (item.SubItems[1].Text == "")
                {
                    // выбран логический диск
                    if (item.SubItems[0].Text.Length == 3)
                        tmpDir = item.SubItems[0].Text;
                    else
                        tmpDir += ("\\" + item.SubItems[0].Text);
                    localDir = tmpDir;
                    printFiles(localDir, listViewLocal, true);
                }
                else    // если выбран файл, то он загружается на сервер
                {
                    if (label9.Visible == false)
                    {
                        string file = localDir + "\\" + item.SubItems[0].Text;
                        try
                        {
                            dataSource.Add(String.Format("Статус:  Загрузка файла {0}", item.SubItems[0].Text));
                            ftpClient.UploadFile(serverDir, fTransferMode, file);
                            dataSource.Add(String.Format("Статус:  Файл загружен на сервер успешно. Загружено {0} байт.",
                                item.SubItems[1].Text));
                            // обновление окна с данными на сервере
                            FileList = ftpClient.ListDirectory(serverDir, fTransferMode);
                            printFiles(serverDir, listViewDistant, false);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
        }

        // обработчик получения файлов в виделенной папке на сервере
        private void listViewDistant_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            bool fCopy = false;
            string directory = textBoxServerDir.Text;
            if (listViewDistant.SelectedItems.Count == 0)
                return;
            ListViewItem item = listViewDistant.SelectedItems[0];

            try
            {
                // если выбрана корневая папка
                if (item.Text == "..")
                {
                    // если папка является корнем серверного диска
                    if (directory.Length == 1)
                        return;
                    else
                    {
                        int index = directory.LastIndexOf("/");
                        directory = directory.Substring(0, index);
                        if (directory.Length == 0) // если убрался символ корневой папки
                            directory = "/";
                    }
                }
                // если выбрана произвольная папка
                else
                {
                    if (item.SubItems[1].Text == "")
                    {
                        if (directory.Length == 1) // если переход осуществляется из корневой папки
                            directory += listViewDistant.SelectedItems[0].SubItems[0].Text.Trim();
                        else
                            directory += ("/" + listViewDistant.SelectedItems[0].SubItems[0].Text.Trim());
                    }
                    else // если выбран файл, то он копируется в локальную папку   
                    {
                        string file = item.SubItems[0].Text;
                        try
                        {
                            dataSource.Add(String.Format("Статус:  Сохранение файла {0}", file));
                            ftpClient.DownloadFile(serverDir, fTransferMode, localDir, file);
                            dataSource.Add(String.Format("Статус:  Файл сохранен успешно. Загружено {0} байт.",
                                item.SubItems[1].Text));
                            // обновление окна с данными на локальном диске
                            printFiles(localDir, listViewLocal, true);
                            fCopy = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                if (!fCopy)
                {
                    dataSource.Add(String.Format("Статус:  Получение списка файлов из \"{0}\"", directory));
                    FileList = ftpClient.ListDirectory(directory, fTransferMode);
                    printFiles(directory, listViewDistant, false);
                    dataSource.Add(String.Format("Статус:  Список файлов \"{0}\" извлечен", directory));
                    serverDir = directory;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // обработчик процедуры выбора элементов в списке Локальных файлов
        private void listViewLocal_SelectedIndexChanged(object sender, EventArgs e)
        {
            int numDir = 0, numFiles = 0;   // счетчики количества каталогов и файлов
            long size = 0;  // счетчик общего размера выделенных файлов
            int num = listViewLocal.SelectedItems.Count;
            if ((num > 0) && (label9.Visible == false))
            {
                ListViewItem item = listViewLocal.SelectedItems[0];
                if (item.Text == ".." || item.Text.Length == 3)
                {
                    // при выборе корневой папки или логического диска
                    SaveButton.Enabled = false;
                    DownloadButton.Enabled = false;
                    contextMenuStripLeft.Items[0].Enabled = false;
                    DeleteButton.Enabled = false;
                }
                else
                {
                    // активация кнопки "Загрузить файл"
                    DownloadButton.Enabled = true;
                    contextMenuStripLeft.Items[0].Enabled = true;
                    SaveButton.Enabled = false;
                    DeleteButton.Enabled = false;
                }

                foreach (ListViewItem lv in listViewLocal.SelectedItems)
                {
                    if (lv.SubItems[0].Text != ".." && lv.SubItems[1].Text == "") // папка с файлами, но не корневая
                        numDir++;
                    else if (lv.SubItems[0].Text.Length == 3)   // логический диск тоже каталог
                        numDir++;
                    else if (lv.SubItems[0].Text != "..")
                    {
                        // все остальные файлы
                        size += Convert.ToInt64(unNumberFormat(lv.SubItems[1].Text));
                        numFiles++;
                    }    
                }

                // вывод информации о количестве файлов и папок в текущей дериктории
                displayInfo(label7, numDir, numFiles, size, true);
            }
        }

        // обработчик процедуры выбора элементов в списке Файлов на сервере
        private void listViewDistant_SelectedIndexChanged(object sender, EventArgs e)
        {
            int numDir = 0, numFiles = 0;   // счетчики количества каталогов и файлов
            long size = 0;  // счетчик общего размера выделенных файлов
            int num = listViewDistant.SelectedItems.Count;
            if (num > 0)
            {
                ListViewItem item = listViewDistant.SelectedItems[0];
                if (item.Text == "..")
                {
                    // при выборе корневой папки
                    SaveButton.Enabled = false;
                    DownloadButton.Enabled = false;
                    DeleteButton.Enabled = false;
                    contextMenuStripRight.Items[0].Enabled = false;
                    contextMenuStripRight.Items[5].Enabled = false;
                }
                else
                {
                    // активация кнопок "Сохранить файл" и "Удалить файл"
                    SaveButton.Enabled = true;
                    DeleteButton.Enabled = true;
                    contextMenuStripRight.Items[0].Enabled = true;
                    contextMenuStripRight.Items[5].Enabled = true;
                    DownloadButton.Enabled = false;
                }

                foreach (ListViewItem lv in listViewDistant.SelectedItems)
                {
                    if (lv.SubItems[0].Text != ".." && lv.SubItems[1].Text == "") // папка с файлами, но не корневая
                        numDir++;
                    else if (lv.SubItems[0].Text != "..")
                    {
                        // все остальные файлы
                        size += Convert.ToInt64(unNumberFormat(lv.SubItems[1].Text));
                        numFiles++;
                    }
                }

                // вывод информации о количестве файлов и папок в текущей дериктории
                displayInfo(label8, numDir, numFiles, size, true);
            }
        }
        #endregion

        #region dopMetods
        // заполнение полей по умолчанию в случае наличия файла config.dat
        private void getData()
        {
            string textFromFile, tmpStr;
            int count = 0;
            // чтение данных из файла
            StreamReader stream = null;
            try
            {
                stream = new StreamReader(congifFile);
                stream.Close();
                textFromFile = _fProc.readConfig();

                // обработка полученной строки
                textBoxHost.Text = _fProc.findeData(textFromFile, 0);
                count += (textBoxHost.Text.Length + 1);
                textBoxPort.Text = _fProc.findeData(textFromFile, count);
                count += (textBoxPort.Text.Length + 1);
                tmpStr = _fProc.findeData(textFromFile, count);
                count += (tmpStr.Length + 1);
                textBoxUserName.Text = _fProc.findeData(textFromFile, count);
                count += (textBoxUserName.Text.Length + 1);
                if (tmpStr == "Анонимный")
                {
                    checkBoxAnonym.Checked = true;
                    textBoxUserName.Text = "";
                }
                else
                    checkBoxAnonym.Checked = false;
                textBoxPassword.Text = _fProc.findeData(textFromFile, count);
                count += (textBoxPassword.Text.Length + 1);
                tmpStr = _fProc.findeData(textFromFile, count);
                count += (tmpStr.Length + 1);
                if (tmpStr == "1")
                    textBoxPassword.PasswordChar = '\0';
                else
                    textBoxPassword.PasswordChar = '*';
                localDir = _fProc.findeData(textFromFile, count);
                count += (localDir.Length + 1);
                serverDir = _fProc.findeData(textFromFile, count);
                count += (serverDir.Length + 1);
                if (serverDir == "")
                    serverDir = "/";
                tmpStr = _fProc.findeData(textFromFile, count);
                if (tmpStr == "2")
                    fTransferMode = false;
                else
                    fTransferMode = true;
            }
            catch (Exception)
            {
                if (stream != null)
                    stream.Close();
            }
        }

        // процедура вывода списка файлов в listView
        private void printFiles(string path, ListView listV, bool localFlag)
        {
            listV.Items.Clear();    // очистка списка файлов
            int numFiles = 0, numDir = 0;
            long size = 0;
            ListViewItem lvi;
            // получение дескриптора списка образов системы 
            NativeMethods.SHFILEINFO shfi = new NativeMethods.SHFILEINFO();
            IntPtr hSysImgList = NativeMethods.SHGetFileInfo("",
                        0,
                        ref shfi,
                        (uint)Marshal.SizeOf(shfi),
                        NativeMethods.SHGFI_SYSICONINDEX
                        | NativeMethods.SHGFI_SMALLICON);
            Debug.Assert(hSysImgList != IntPtr.Zero);

            // установка элемента управления ListView для использования этого списка изображений 
            IntPtr hOldImgList = NativeMethods.SendMessage(listV.Handle,
                        NativeMethods.LVM_SETIMAGELIST,
                        NativeMethods.LVSIL_SMALL,
                        hSysImgList);

            // если элемент управления ListView уже имел список изображений, удалить старый. 
            if (hOldImgList != IntPtr.Zero)
                NativeMethods.ImageList_Destroy(hOldImgList);
            listV.View = View.Details;
            // получение элементов из файловой системы и добавление каждого из них в ListView, 
            // в комплекте с соответствующими индексами имен и значков.
            if (localFlag)
            {
                textBoxLocalDir.Text = path;
                string[] str;
                if (path == "")
                    str = Directory.GetLogicalDrives();
                else
                {
                    str = Directory.GetFileSystemEntries(path);
                    // первая папка будет ссылкой на корневую папку
                    listV.Items.Add("..", 3);
                }

                foreach (string file in str)
                {
                    IntPtr himl = NativeMethods.SHGetFileInfo(file,
                        0,
                        ref shfi,
                        (uint)Marshal.SizeOf(shfi),
                        NativeMethods.SHGFI_DISPLAYNAME
                        | NativeMethods.SHGFI_SYSICONINDEX
                        | NativeMethods.SHGFI_SMALLICON);

                    var fileInfo = new FileInfo(file);

                    if (!fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                        fileInfo.Attributes.HasFlag(FileAttributes.System))    // если файл не скрытый или системный
                    {
                        if (fileInfo.Attributes.HasFlag(FileAttributes.System) && fileInfo.Name == "")
                        {
                            // если file является логическим диском
                            lvi = new ListViewItem(fileInfo.FullName, shfi.iIcon);
                        }
                        else
                            lvi = new ListViewItem(fileInfo.Name, shfi.iIcon);
                        if (fileInfo.Attributes.HasFlag(FileAttributes.Directory)) // если file является Папкой
                        {
                            lvi.SubItems.Add("");
                            lvi.SubItems.Add("Папка с файлами");
                            numDir++;   // количество папок
                        }
                        else
                        {
                            lvi.SubItems.Add(numberFormat(fileInfo.Length));
                            size += fileInfo.Length;// общий размер файлов в папке
                            string tmp = fileInfo.Extension;
                            tmp = tmp.TrimStart('.');
                            tmp = tmp.ToUpper();
                            lvi.SubItems.Add("Файл \"" + tmp + "\"");
                            numFiles++; // количество файлов
                        }
                        lvi.SubItems.Add(Directory.GetLastWriteTime(file).ToString());
                        listV.Items.Add(lvi);
                    }
                }
                // вывод информации о количестве файлов и папок в текущей дериктории
                displayInfo(label7, numDir, numFiles, size, false);
            }
            else
            {
                textBoxServerDir.Text = path;
                // первая папка будет ссылкой на корневую папку
                listV.Items.Add("..", 3);
                foreach (FileStruct s in FileList)
                {
                    IntPtr himl = NativeMethods.SHGetFileInfo(s.Name,
                        0,
                        ref shfi,
                        (uint)Marshal.SizeOf(shfi),
                        NativeMethods.SHGFI_USEFILEATTRIBUTES
                        | NativeMethods.SHGFI_SYSICONINDEX
                        | NativeMethods.SHGFI_SMALLICON);

                    if (s.IsDirectory) // если file является Папкой
                    {
                        lvi = new ListViewItem(s.Name, 3);
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("Папка с файлами");
                        numDir++;   // количество папок
                    }
                    else
                    {
                        lvi = new ListViewItem(s.Name, shfi.iIcon);
                        lvi.SubItems.Add(numberFormat(s.FileSize));
                        size += s.FileSize;// общий размер файлов в папке
                        string tmp = s.FileType;
                        tmp = tmp.ToUpper();
                        lvi.SubItems.Add("Файл \"" + tmp + "\"");
                        numFiles++; // количество файлов
                    }
                    lvi.SubItems.Add(s.CreateTime);
                    listV.Items.Add(lvi);
                }
                // вывод информации о количестве файлов и папок в текущей дериктории
                displayInfo(label8, numDir, numFiles, size, false);
            }
            
            // сокрытие кнопок Сохранить, Загрузить и Удалить файл
            SaveButton.Enabled = false;
            DownloadButton.Enabled = false;
            DeleteButton.Enabled = false;
        }

        // функция разбития чисел для отображения в виде 100 000
        private string numberFormat(long _size)
        {
            string tmpStr = "";
            int length = Convert.ToString(_size).Length/3 + 1;   // размер массива
            int[] array = new int[length];
            long tmpNum, resNum;
            tmpNum = _size;

            for ( int i = length - 1; i >= 0; i--)
            {
                resNum = tmpNum % 1000;
                tmpNum = tmpNum / 1000;
                
                array[i] = (int)resNum;
            }
            
            foreach (int num in array)
            {
                if (num != 0)
                    tmpStr += (String.Format("{0:d3} ", num));
            }
            tmpStr = tmpStr.TrimEnd();  // убрать последний пробел
            tmpStr = tmpStr.TrimStart('0'); // убрать вначале '0'

            return tmpStr;
        }

        // функция представления из вида 100 000 в 100000
        private string unNumberFormat(string _size)
        {
            char[] tmp = new char[_size.Length];
            int i = 0;  // некоторый счетчик

            if (_size.Length > 3)
            {
                foreach (var ch in _size)
                {
                    if (ch != ' ')
                    {
                        tmp[i] = ch;
                        i++;
                    }
                }
                string resStr = new string(tmp);
                resStr = resStr.TrimEnd('\0');  // убрать последние лишние символы
                return resStr;
            }
            else
                return _size;
        }

        // процедура вывода информации о выбранных файлах и их размере
        private void displayInfo(Label infoLabel, int num1, int num2, long num3, bool flag)
        {
            if (flag) // вывод информации о выбранном(ых) файле(ах)
            {
                if (num1 > 0 && num2 == 0) // только каталоги
                    infoLabel.Text = String.Format("Выбрано: каталогов - {0}.", num1);
                else if (num1 == 0 && num2 > 0) // только файлы
                    infoLabel.Text = String.Format("Выбрано: файлов - {0}. Общий размер: {1} байт", 
                        num2, numberFormat(num3));
                else // и каталоги и файлы
                    infoLabel.Text = String.Format("Выбрано: каталогов - {0}, файлов - {1}. Общий размер: {2} байт", 
                        num1, num2, numberFormat(num3));
            }
            else
                infoLabel.Text = String.Format("Каталогов: {0}. Файлов: {1}. Общий размер: {2} байт",
                Convert.ToString(num1), Convert.ToString(num2), numberFormat(num3));
        }

        // проверка ввода имени хоста
        private void textBoxHost_Validating(object sender, CancelEventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxHost.Text))
                errorProvider1.SetError(textBoxHost, "Не указано имя сервера!");
            else
                errorProvider1.Clear();
        }

        // проверка заполненности нужных полей
        private bool fieldsChecking()
        {
            if (textBoxHost.Text == "")
            {
                MessageBox.Show("Имя сервера не может быть пустым!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            else if (checkBoxAnonym.Checked == false)
            {
                if (textBoxUserName.Text == "")
                {
                    MessageBox.Show("Не указано имя пользователя!", "", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
                else if (textBoxPassword.Text == "")
                {
                    MessageBox.Show("Введите пароль!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                else
                    return true;
            }
            else if (textBoxPort.Text == "")
            {
                textBoxPort.Text = "21";    // порт по умолчанию
                return true;
            }
            else
                return true;
        }

        // рекурсивная процедура загрузки папки с содержимым на сервер
        private void uploadDirectory(string server, string _file)
        {
            dataSource.Add(String.Format("Статус:  Загрузка каталога {0}", _file));
            string shortName = _file.Remove(0, _file.LastIndexOf("\\") + 1);
            if (server.Length == 1)
                server = "";
            string directory = server + "/" + shortName;
            // проверка существования загружаемого каталога
            FileList = ftpClient.ListDirectory(directory, fTransferMode);
            if (FileList == null)   // если каталог на сервере отсутствует, то создать его
                ftpClient.CreateDirectory(server + "/", fTransferMode, shortName);
            
            // получение файлов в загружаемом каталоге на локальном диске
            string[] str = Directory.GetFileSystemEntries(_file);
            foreach (string file in str)
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.Attributes.HasFlag(FileAttributes.Directory)) // если file является Папкой
                    uploadDirectory(directory, fileInfo.FullName);  // рекурсия
                else
                {
                    // загрузка файла в созданный или существующий каталог
                    dataSource.Add(String.Format("Статус:  Загрузка файла {0}", fileInfo.FullName));
                    ftpClient.UploadFile(directory, fTransferMode, fileInfo.FullName);
                    dataSource.Add(String.Format("Статус:  Файл загружен на сервер успешно. Загружено {0} байт.",
                        fileInfo.Length));
                }
            }
        }

        // рекурсивная процедура сохранения папки с содержимым на локальном компьютере
        private void downloadDirectory(string server, string local, string _file)
        {
            string localDirectory;
            if (local.Length > 3)   // если локальная папка не является логическим диском
                localDirectory = local + "\\" + _file;
            else
                localDirectory = local + _file;
            // проверка существования сохраняемой папки на локальном диске
            DirectoryInfo dirInfo = new DirectoryInfo(localDirectory);
            if (!dirInfo.Exists)    // если такой папки не существует на локальном диске
                dirInfo.Create();
            // получение списка сохраняемого каталога
            if (server.Length == 1)
                server = "";
            string directory = server + "/" + _file;
            dataSource.Add(String.Format("Статус:  Получение списка каталога \"{0}\"", directory));
            FileStruct[] FList = ftpClient.ListDirectory(directory, fTransferMode);
            dataSource.Add(String.Format("Статус:  Список каталога \"{0}\" извлечен", directory));
            foreach (FileStruct file in FList)
            {
                // если сохраняется папка, то рекурсия
                if (file.IsDirectory)
                    downloadDirectory(directory, localDirectory, file.Name);
                else    // сохранение файлов в локальный каталог
                {
                    dataSource.Add(String.Format("Статус:  Сохранение файла {0}", directory + "/" + file.Name));
                    ftpClient.DownloadFile(directory, fTransferMode, localDirectory, file.Name);
                    dataSource.Add(String.Format("Статус:  Файл сохранен успешно. Загружено {0} байт.",
                        file.FileSize));
                }
            }
        }
        
        // рекурсивная процедура удаления каталога с содержимым с сервера
        private void removeDirectory(string _file)
        {
            int numFiles = 0;   // количество файлов для удаления
            // получение списка файлов в папке
            dataSource.Add(String.Format("Статус:  Получение списка файлов из \"{0}\"", _file));
            FileStruct[] FList = ftpClient.ListDirectory(_file, fTransferMode);
            dataSource.Add(String.Format("Статус:  Список файлов \"{0}\" извлечен", _file));
            // подсчет количества файлов и папок в извлеченном каталоге
            foreach (FileStruct n in FList)
            {
                if (n.IsDirectory == false)
                    numFiles++;
            }
            if (numFiles > 0)   // если есть файлы для удаления
                dataSource.Add(String.Format("Статус:  Удаление {0} файла(ов) из \"{1}\"",
                    numFiles, _file));
            // удаление всех файлов из папки
            foreach (FileStruct f in FList)
            {
                if (f.IsDirectory == false)
                {
                    ftpClient.DeleteFile(_file + "/" + f.Name, fTransferMode);
                    dataSource.Add(String.Format("Статус:  Файл \"{0}\" успешно удален",
                        _file + "/" + f.Name));
                }
                else
                    removeDirectory(_file + "/" + f.Name);  // рекурсия на удаление папки с содержимым
            }
            // удаление пустой папки
            ftpClient.RemoveDirectory(_file, fTransferMode);
            dataSource.Add(String.Format("Статус:  Каталог \"{0}\" успешно удален", _file));
        }

        // автоматическое перемещение курсора в listBoxInfo вниз
        private void listBoxInfoChanged(object sender, ListChangedEventArgs e)
        {
            int visibleItems = listBoxInfo.ClientSize.Height / listBoxInfo.ItemHeight;
            listBoxInfo.TopIndex = Math.Max(listBoxInfo.Items.Count - visibleItems + 1, 0);
        }
        #endregion
    }
}
