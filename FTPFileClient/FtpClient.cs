using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace FTPFileClient
{
    class FtpClient
    {
        // поле для хранения имени FTP-сервера
        private string host;
        // поле для хранения порта
        private string port;
        // поле для хранения логина
        private string userName;
        // поле для хранения пароля
        private string password;

        // объект для запроса данных
        FtpWebRequest ftpRequest;
        // объект для получения данных
        FtpWebResponse ftpResponse;

        // флаг использования SSL
        private bool useSSL = false;

        // FTP-сервер
        public string Host
        {
            get
            {
                return host;
            }
            set
            {
                host = value;
            }
        }

        // порт
        public string Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }
        
        // логин
        public string UserName
        {
            get
            {
                return userName;
            }
            set
            {
                userName = value;
            }
        }
        
        // пароль
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }
        
        // для установки SSL-чтобы данные нельзя было перехватить
        public bool UseSSL
        {
            get
            {
                return useSSL;
            }
            set
            {
                useSSL = value;
            }
        }

        // реализация команды LIST для получения подробного списока файлов на FTP-сервере
        public FileStruct[] ListDirectory(string path, bool pass)
        {
            if (path == null || path == "")
            {
                path = "/";
            }
            // создание объекта запроса
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + ":" + port + path);
            // отправка логина и пароля
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            // непосредственно команда на FTP - LIST
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            ftpRequest.UsePassive = pass;
            ftpRequest.EnableSsl = useSSL;
            try
            {
                // получение входящего потока
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            }
            catch (Exception ex)
            {
                return null;
            }
            
            // переменная для хранения всей полученной информации
            string sContent;

            StreamReader sReader = new StreamReader(ftpResponse.GetResponseStream(), System.Text.Encoding.UTF8);
            sContent = sReader.ReadToEnd();
            sReader.Close();

            ftpResponse.Close();

            DirectoryListParser parser = new DirectoryListParser(sContent);
            return parser.FullListing;
        }

        // реализация метода разрыва соединения с сервером
        public void DisconnectServer(string path, bool pass)
        {
            if (path == null || path == "")
            {
                path = "/";
            }
            // создание объекта запроса
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + ":" + port + path);
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            ftpRequest.UsePassive = pass;
            ftpRequest.EnableSsl = useSSL;
            ftpRequest.KeepAlive = false;   // разрыв соединения с сервером
            // получение входящего потока
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }

        // реализация метода протокола FTP RETR для загрузки файла с FTP-сервера
        public void DownloadFile(string path, bool pass, string localPath, string fileName)
        {

            localPath += ("\\" + fileName);
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + ":" + port + path + "/" + fileName);

            ftpRequest.Credentials = new NetworkCredential(userName, password);
            // непосредственно команда на FTP - RETR
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            ftpRequest.UsePassive = pass;
            ftpRequest.EnableSsl = useSSL;
            // копирование файлов в кталог программы
            FileStream downloadFile = new FileStream(localPath, FileMode.Create, FileAccess.ReadWrite);

            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            // получение входящего потока
            Stream responseStream = ftpResponse.GetResponseStream();

            // буфер для считываемых данных
            byte[] buffer = new byte[1024];
            int size = 0;

            while ((size = responseStream.Read(buffer, 0, 1024)) > 0)
            {
                downloadFile.Write(buffer, 0, size);

            }
            ftpResponse.Close();
            downloadFile.Close();
            responseStream.Close();
        }
        
        // реализация метода протокола FTP STOR для загрузки файла на FTP-сервер
        public void UploadFile(string path, bool pass, string fileName)
        {
            // имя файла
            string shortName = "/" + fileName.Remove(0, fileName.LastIndexOf("\\" ) + 1);

            FileStream uploadFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + ":" + port + path + shortName);
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            ftpRequest.UsePassive = pass;
            ftpRequest.EnableSsl = useSSL;
            // непосредственно команда на FTP - STOR
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

            // буфер для загружаемых данных
            byte[] file_to_bytes = new byte[uploadFile.Length];
            // считывание данных в буфер
            uploadFile.Read(file_to_bytes, 0, file_to_bytes.Length);
            uploadFile.Close();

            // поток для загрузки файла 
            Stream writer = ftpRequest.GetRequestStream();

            writer.Write(file_to_bytes, 0, file_to_bytes.Length);
            writer.Close();
        }

        // реализация метода протокола FTP DELE для удаления файла с FTP-сервера 
        public void DeleteFile(string path, bool pass)
        {
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + ":" + port + path);
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            ftpRequest.UsePassive = pass;
            ftpRequest.EnableSsl = useSSL;
            // непосредственно команда на FTP - DELE
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }

        // реализация метода протокола FTP MKD для создания каталога на FTP-сервере 
        public void CreateDirectory(string path, bool pass, string folderName)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + ":" + port + path + folderName);

            ftpRequest.Credentials = new NetworkCredential(userName, password);
            ftpRequest.UsePassive = pass;
            ftpRequest.EnableSsl = useSSL;
            // непосредственно команда на FTP - MKD
            ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }

        // реализация метода протокола FTP RMD для удаления каталога с FTP-сервера 
        public void RemoveDirectory(string path, bool pass)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + host + ":" + port + path);

            ftpRequest.Credentials = new NetworkCredential(userName, password);
            ftpRequest.UsePassive = pass;
            ftpRequest.EnableSsl = useSSL;
            // непосредственно команда на FTP - RWD
            ftpRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }
    }
    
    // парсинг полученного детального списка каталогов FTP-сервера
    // структура для хранения детальной информации о файле или каталоге
    public struct FileStruct
    {
        public string Flags;
        public string Owner;
        public bool IsDirectory;
        public string FileType;
        public long FileSize;
        public string CreateTime;
        public string Name;
    }

    public enum FileListStyle
    {
        UnixStyle,
        WindowsStyle,
        Unknown
    }
    
    // класс для парсинга
    public class DirectoryListParser
    {
        private List<FileStruct> _myListArray;

        public FileStruct[] FullListing
        {
            get
            {
                return _myListArray.ToArray();
            }
        }

        public FileStruct[] FileList
        {
            get
            {
                List<FileStruct> _fileList = new List<FileStruct>();
                foreach (FileStruct thisstruct in _myListArray)
                {
                    if (!thisstruct.IsDirectory)
                    {
                        _fileList.Add(thisstruct);
                    }
                }
                return _fileList.ToArray();
            }
        }

        public FileStruct[] DirectoryList
        {
            get
            {
                List<FileStruct> _dirList = new List<FileStruct>();
                foreach (FileStruct thisstruct in _myListArray)
                {
                    if (thisstruct.IsDirectory)
                    {
                        _dirList.Add(thisstruct);
                    }
                }
                return _dirList.ToArray();
            }
        }

        public DirectoryListParser(string responseString)
        {
            _myListArray = GetList(responseString);
        }

        private List<FileStruct> GetList(string datastring)
        {
            List<FileStruct> myListArray = new List<FileStruct>();
            string[] dataRecords = datastring.Split('\n');
            
            // получение стиля записей на сервере
            FileListStyle _directoryListStyle = GuessFileListStyle(dataRecords);
            foreach (string s in dataRecords)
            {
                if (_directoryListStyle != FileListStyle.Unknown && s != "")
                {
                    FileStruct f = new FileStruct();
                    f.Name = "..";
                    switch (_directoryListStyle)
                    {
                        case FileListStyle.UnixStyle:
                            f = ParseFileStructFromUnixStyleRecord(s);
                            break;
                        case FileListStyle.WindowsStyle:
                            f = ParseFileStructFromWindowsStyleRecord(s);
                            break;
                    }
                    if (f.Name != "" && f.Name != "." && f.Name != "..")
                    {
                        myListArray.Add(f);
                    }
                }
            }
            return myListArray;
        }
        
        // парсинг, если FTP-сервер работает на Windows
        private FileStruct ParseFileStructFromWindowsStyleRecord(string Record)
        {
            // предположим стиль записи 02-03-04  07:46PM  <DIR>  Append
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            // получение даты
            string dateStr = processstr.Substring(0, 8);
            processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();
            // получение времени
            string timeStr = processstr.Substring(0, 7);
            processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
            f.CreateTime = dateStr + " " + timeStr;
            // проверка на папку
            if (processstr.Substring(0, 5) == "<DIR>")  // если Папка
            {
                f.IsDirectory = true;
                processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
            }
            else
            {
                string[] strs = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                processstr = strs[1];
                f.IsDirectory = false;
            }
            
            // остальное содержимое строки представляет имя каталога/файла
            f.Name = processstr;
            return f;
        }
        
        // получение информации, на какой ОС работает FTP-сервер - от этого будет зависеть дальнейший парсинг
        public FileListStyle GuessFileListStyle(string[] recordList)
        {
            foreach (string s in recordList)
            {
                // если используется стиль Unix
                if (s.Length > 10
                    && Regex.IsMatch(s.Substring(0, 10), "(-|d)((-|r)(-|w)(-|x)){3}"))
                {
                    return FileListStyle.UnixStyle;
                }
                // иначе стиль Windows
                else if (s.Length > 8
                    && Regex.IsMatch(s.Substring(0, 8), "[0-9]{2}-[0-9]{2}-[0-9]{2}"))
                {
                    return FileListStyle.WindowsStyle;
                }
            }
            return FileListStyle.Unknown;
        }
        
        // если сервер работает на nix-ах
        private FileStruct ParseFileStructFromUnixStyleRecord(string record)
        {
            // предположим стиль записи dr-xr-xr-x   1 owner    group    0 Nov 25  2002 bussys
            FileStruct f = new FileStruct();
            if (record[0] == '-' || record[0] == 'd')
            {// правильная запись файла
                string processstr = record.Trim();
                f.Flags = processstr.Substring(0, 9);
                // проверка на папку
                f.IsDirectory = (f.Flags[0] == 'd');
                processstr = (processstr.Substring(11)).Trim();
                // отсечение части строки
                _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.Owner = _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                // получение размера файла
                _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.FileSize = Convert.ToInt64(_cutSubstringFromStringWithTrim(ref processstr, ' ', 0));
                // получение даты и времени
                f.CreateTime = getCreateTimeString(record);
                // индекс начала имени файла
                int fileNameIndex = record.IndexOf(f.CreateTime) + f.CreateTime.Length;
                f.CreateTime = datetimeFormat(f.CreateTime);
                // непосредственно имя файла
                f.Name = record.Substring(fileNameIndex).Trim();
                // тип файла
                if (f.IsDirectory)
                    f.FileType = "dir";
                else
                {
                    // отсечение части строки
                    int lastPos = processstr.LastIndexOf('.', processstr.Length - 1);
                    processstr = processstr.Substring(lastPos);
                    f.FileType = processstr.TrimStart('.');
                }
            }
            else
            {
                f.Name = "";
            }
            return f;
        }

        private string getCreateTimeString(string record)
        {
            // получение временных данных
            string month = "(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)";
            string space = @"(\040)+";
            string day = "([0-3][0-9])";
            string year = "[1-2][0-9]{3}";
            string time = "[0-9]{1,2}:[0-9]{2}";
            Regex dateTimeRegex = new Regex(month + space + day + space + "(" + year + "|" + time + ")", RegexOptions.IgnoreCase);
            Match match = dateTimeRegex.Match(record);
            return match.Value;
        }

        private string _cutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
        {
            int pos1 = s.IndexOf(c, startIndex);
            string retString = s.Substring(0, pos1);
            s = (s.Substring(pos1)).Trim();
            return retString;
        }

        private string datetimeFormat(string datetime)
        {
            string day, month, year, time;
            string resultStr = "";
            string[] _month = new[] {"jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"};
            // поиск месяца
            int indexMonth = datetime.IndexOf(' ');
            month = datetime.Substring(0, 3);
            month = month.ToLower();
            for (int i = 0; i < 12; i++)
            {
                if (month == _month[i])
                    month = String.Format("{0:d2}", (i + 1));
            }
            // поиск дня
            int indexDay = datetime.IndexOf(' ', indexMonth + 1);
            day = datetime.Substring(indexMonth + 1, 2);
            // поиск года
            Regex yearRegex = new Regex("[1-2][0-9]{3}");
            Match _year = yearRegex.Match(datetime);
            year = _year.Value;
            // поиск времени
            Regex timeRegex = new Regex("[0-9]{1,2}:[0-9]{2}");
            Match _time = timeRegex.Match(datetime);
            time = _time.Value;
            // формирование результата
            resultStr = String.Format("{0}.{1}", day, month);
            if (year.Length > 0)
                resultStr = String.Format("{0}.{1}", resultStr, year);
            if (time.Length > 0)
                resultStr = String.Format("{0} {1}", resultStr, time);

            return resultStr;
        }
    }
}
