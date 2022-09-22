using System.IO;

namespace FTPFileClient
{
    class FileProcessing
    {
        private const string congifFile = "config.dat";

        // процедура записи данных в файл
        public void writeConfig(string strValue, bool fFirst)
        {
            string tmpText = strValue + '\0';

            // преобразование строки в байты
            byte[] array = System.Text.Encoding.Default.GetBytes(tmpText);

            if (fFirst)
            {
                using (FileStream fstream = new FileStream(congifFile, FileMode.Create))
                {
                    // запись массива байтов в файл
                    fstream.Write(array, 0, array.Length);
                }
            }
            else
            {
                using (FileStream fstream = new FileStream(congifFile, FileMode.Append))
                {
                    // запись массива байтов в файл
                    fstream.Write(array, 0, array.Length);
                }
            }
        }

        // функция чтения данных из файла
        public string readConfig()
        {
            string textFromFile;
            
            using (FileStream fstream = File.OpenRead(congifFile))
            {
                // преобразуем строку в байты
                byte[] array = new byte[fstream.Length];
                // считываем данные
                fstream.Read(array, 0, array.Length);
                // декодируем байты в строку
                textFromFile = System.Text.Encoding.Default.GetString(array);
            }

            return textFromFile;
        }

        // функция поиска данных в строке
        public string findeData(string AStr, int num)
        {
            string resStr = "";

            for (int i = num; i < AStr.Length; i++)
            {
                char tmp;
                if (AStr[i] != '\0')
                    tmp = AStr[i];
                else
                    break;
                resStr = resStr + tmp;
            }

            return resStr;
        }
    }
}
