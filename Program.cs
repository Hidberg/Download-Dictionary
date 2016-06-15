using System;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;

namespace JSON_Deserialize
{
    class Program
    {
        static volatile int countQuery; // 10 000 000 запросов это 1млрд строк. 254200 запросов это Сереги для проверки(25,42 млн)
        static int queryOnThread;
        static int currentQuery;
        static ConcurrentDictionary<int, string> data = new ConcurrentDictionary<int, string>();
        static ConcurrentQueue<string> logMessages = new ConcurrentQueue<string>();
        static bool allLogsInQueue = false;
        static Stopwatch timer;
        static AutoResetEvent readyToStop = new AutoResetEvent(false);
        static AutoResetEvent readyToDownload = new AutoResetEvent(false);
        static initialParams parametrs;

        static void Main(string[] args)
        {
            int maxThreadsAllowed = 0;
            bool ok = true;
            try
            {
                using (StreamReader config = new StreamReader("json_config.txt"))
                {
                    parametrs = JsonConvert.DeserializeObject<initialParams>(config.ReadToEnd());
                }
                maxThreadsAllowed = parametrs.maxThreadsAllowed;
                currentQuery = parametrs.currentQuery;
                countQuery = parametrs.countQuery;
            }
            catch (Exception)
            {
                Console.WriteLine("Файл json_config.txt не найден или в нем некорректные значения!");
                ok = false;
            }
            if (ok)
            {
                timer = new Stopwatch();
                timer.Start();

                Thread[] Threads = new Thread[maxThreadsAllowed];
                queryOnThread = countQuery / maxThreadsAllowed;

                Thread dataWriter = new Thread(WriteToFile);
                dataWriter.Start();

                readyToDownload.WaitOne();
                for (int i = 0; i < maxThreadsAllowed; ++i)
                {
                    Threads[i] = new Thread(GetDataFromServer);
                    Threads[i].Start();
                }
                Thread logWriter = new Thread(WriteLog);
                logWriter.Start();

                string comand;
                while (currentQuery < countQuery)
                {
                    comand = Console.ReadLine();
                    if (comand == "stop")
                    {
                        countQuery = currentQuery;
                    }
                }
                readyToStop.WaitOne();
            }
        }

        static void GetDataFromServer()
        {
            int localCurrentQuery = 0;
            while (currentQuery < countQuery)
            {
                try
                {
                    if (localCurrentQuery == 0)
                    {
                        localCurrentQuery = Interlocked.Increment(ref currentQuery);
                    }
                    HttpWebRequest http = WebRequest.CreateHttp("https://hola.org/challenges/word_classifier/testcase/" + localCurrentQuery);
                    using (HttpWebResponse response = (HttpWebResponse)http.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                            {
                                string content = sr.ReadToEnd();
                                while (!data.TryAdd(localCurrentQuery, content))
                                {
                                    logMessages.Enqueue("Shit happens! TryAdd return false.");
                                }
                                localCurrentQuery = 0;
                            }
                        }
                        else
                        {
                            logMessages.Enqueue(string.Format("Запрос '{0}' вернул код '{1}'", localCurrentQuery, response.StatusCode));
                        }
                    }
                }
                catch (Exception e)
                {
                    logMessages.Enqueue("Error when getting data from client" + e.ToString());
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        static void WriteLog()
        {
            using (StreamWriter logFile = new StreamWriter("log.log"))
            {
                string logMessage;
                while (!allLogsInQueue || !logMessages.IsEmpty)
                {
                    if (logMessages.TryDequeue(out logMessage))
                    {
                        logFile.WriteLine(logMessage);
                        logFile.Flush();
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
            readyToStop.Set();
        }

        static void WriteToFile()
        {
            int a = currentQuery + 1;
            readyToDownload.Set();
            using (StreamWriter file = new StreamWriter("result.txt", true))
            {
                file.NewLine = "\n";
                logMessages.Enqueue("Программа запущена...");
                for (; a <= countQuery;)
                {
                    string content;
                    if (data.TryRemove(a, out content))
                    {
                        StringBuilder mutableContent = new StringBuilder(content);
                        mutableContent = mutableContent.Replace("\"", "").Replace("{\n ", "").Replace("}", "").Replace(" ", "").Replace(",", "").Replace(":true", " 1").Replace(":false", " 0");
                        file.Write(mutableContent.ToString());
                        file.Flush();
                        if (a % 10 == 0)
                        {
                            logMessages.Enqueue(string.Format("Выполнено '{0}' запросов, записано '{1}' строк", a, a * 100));
                        }
                        ++a;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                using (StreamWriter config = new StreamWriter("json_config.txt"))
                {
                    parametrs.currentQuery = countQuery;
                    config.WriteLine(JsonConvert.SerializeObject(parametrs));
                    config.Flush();
                }
                timer.Stop();
                Console.WriteLine("Программа завершена, время работы '{0}' мс", timer.ElapsedMilliseconds.ToString());
                logMessages.Enqueue(string.Format("Программа завершена, время работы '{0}' мс", timer.ElapsedMilliseconds.ToString()));
                allLogsInQueue = true;
            }
        }
    }

    class initialParams
    {
        public int maxThreadsAllowed;
        public int currentQuery;
        public int countQuery;
    }
}
