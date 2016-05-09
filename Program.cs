using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
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
        static int countQuery = 254200; // 10 000 000 запросов это 1млрд строк. 254200 запросов это Сереги для проверки(25,42 млн)
        static ConcurrentDictionary<int, string> data = new ConcurrentDictionary<int, string>();
        static int queryOnThread;
        static int currentQuery = 0;

        static void Main(string[] args)
        {
            string txtFromFile = "";
            try
            {
                using (StreamReader config = new StreamReader("json_config.txt"))
                {
                    txtFromFile = config.ReadLine();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("File doesnt exist. Need to be with exe file.");
                Environment.Exit(1);
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            int maxThreadsAllowed = Convert.ToInt32(txtFromFile);
            Thread[] Threads = new Thread[maxThreadsAllowed];
            queryOnThread = countQuery / maxThreadsAllowed;

            for (int i = 0; i < maxThreadsAllowed; ++i)
            {
                Threads[i] = new Thread(GetDataFromServer);
                Threads[i].Start();
            }
            using (StreamWriter file = new StreamWriter("result.txt", true))
            {
                file.NewLine = "\n";
                Console.WriteLine("Программа запущена...");
                for (int i = 1; i <= countQuery;)
                {
                    string content;

                    if (data.TryRemove(i, out content))
                    {
                        StringBuilder mutableContent = new StringBuilder(content);
                        mutableContent = mutableContent.Replace("\"", "").Replace("{\n ", "").Replace("}", "").Replace(" ", "").Replace(",", "").Replace(":true", " 1").Replace(":false", " 0");
                        file.Write(mutableContent.ToString());
                        file.Flush();
                        if (i % 10 == 0)
                        {
                            Console.WriteLine("Выполнено '{0}' запросов, записано '{1}' строк", i, i * 100);
                        }
                        ++i;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(100); 
                    }
                }
            }
            stopWatch.Stop();
            Console.WriteLine(((stopWatch.ElapsedMilliseconds) / 1000 ).ToString());
        }

        static void GetDataFromServer()
        {
            while (currentQuery < countQuery)
            {
                try
                {
                    int localCurrentQuery = Interlocked.Increment(ref currentQuery); // здесь ошибка
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
                                    Console.WriteLine("Shit happens! TryAdd return false.");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Запрос '{0}' вернул код '{1}'", localCurrentQuery, response.StatusCode);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error when getting data from client");
                    Console.WriteLine(e.ToString());
                    System.Threading.Thread.Sleep(10000);
                }
            }
        }
    }
}
