using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingaTor.Methods
{
    internal static class Utils
    {
        public static FileStream LogFile;
        public static DateTime Date;
        static string _pathLog;

        static object loker = new();

        public static void Set(string pathLog)
        {
            _pathLog = pathLog;
            Set();
        }

        static void Set()
        {
            LogFile = new FileStream($"{_pathLog}{Path.DirectorySeparatorChar}log{DateTime.Now.Date.ToString("yyyy.MM.dd")}.txt", FileMode.OpenOrCreate);
            Date = DateTime.Now.Date;
        }

        public static void Exit()
        {
            Console.WriteLine("Exit programm");
            Console.ReadLine();

            Environment.Exit(0);
        }

        public static void WriteLine(string text)
        {
            Write($"{DateTime.Now.ToLongTimeString()} {text}");
        }

        public static void Write(string text)
        {
            if(DateTime.Now.Date > Date) Set();
            lock (loker)
            {
                text += Environment.NewLine;
                Console.WriteLine(text);

                LogFile.Seek(0, SeekOrigin.End);
                byte[] input = Encoding.Default.GetBytes(text);
                LogFile.Write(input, 0, input.Length);
            }
        }

        public static void RunFile(string fileName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo($"{Path.GetDirectoryName(Environment.ProcessPath)}\\Actions\\{fileName}");
            startInfo.WorkingDirectory = Path.GetDirectoryName(startInfo.FileName);

            Write(Environment.NewLine);
            WriteLine($"Run file {startInfo.FileName}{Environment.NewLine}");
            Process.Start(startInfo);
        }
    }
}
