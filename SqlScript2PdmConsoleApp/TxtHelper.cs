using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SqlScript2PdmConsoleApp
{
    public class TxtHelper
    {


        public static string filePath = string.Format(@".\{0}.txt", DateTime.Now.ToString("yyyy-MM-dd"));



        public static void WriteTxt(string txt)
        {
            WriteTxt(txt, filePath);
        }
        public static void WriteTxt(string txt, string file)
        {
            WriteTxt(txt, file, FileMode.OpenOrCreate);
        }
        public static void WriteTxt(string txt, string file, FileMode fileModel)
        {
            //指定路径不存在，则创建路径
            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }
            //
            FileStream fileStream = new FileStream(file, fileModel);
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                writer.WriteLine(txt);
            }
            fileStream.Close();
        }

        public static void WriteTxt2(string txt)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Create);
            byte[] data = System.Text.Encoding.Default.GetBytes(txt);
            fileStream.Write(data, 0, data.Length);
            fileStream.Flush();
            fileStream.Close();
        }
        /// <summary>
        /// 追加文本
        /// </summary>
        /// <param name="txt"></param>
        public static void WriteAppendTxt(string txt)
        {
            WriteAppendTxt("\r\n" + txt, filePath);
        }
        /// <summary>
        /// 追加文本
        /// </summary>
        /// <param name="txt">txt文件完整路径</param>
        /// <param name="file">追加文本内容</param>
        public static void WriteAppendTxt(string txt, string file)
        {
            //指定路径不存在，则创建路径
            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }
            //
            File.AppendAllText(file, "\r\n" + txt);
        }

        public static string ReadTxt()
        {
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(filePath, Encoding.Default))
            {
                sb.Append(sr.ReadToEnd());
            }
            return sb.ToString();
        }

        public static string ReadTxt2()
        {
            byte[] data = new byte[1000];
            char[] dd = new char[1000];
            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Read(data, 0, 100);
            Decoder d = Encoding.Default.GetDecoder();
            d.GetChars(data, 0, data.Length, dd, 0);
            fileStream.Close();
            return dd.ToString();
        }

        /// <summary>
        /// 按行读取
        /// </summary>
        /// <returns></returns>
        public static IList<string> ReadTextByLine()
        {
            IList<string> list = new List<string>();

            FileStream fileStream = null;
            StreamReader streamReader = null;
            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream, Encoding.Default);
                fileStream.Seek(0, SeekOrigin.Begin);
                string text = streamReader.ReadLine();
                while (text != null)
                {
                    list.Add(text);
                    text = streamReader.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return list;
        }

    }
}
