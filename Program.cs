using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace JSON_Deserialize
{
    class Program
    {
        static void Main(string[] args)
        {
            int count = 0;
            int ticket = 0;
            string txtFromFile = "";
            try
            {
                StreamReader config = new StreamReader("json_config.txt");
                txtFromFile = config.ReadLine();
            }
            catch (Exception)
            {
                Console.WriteLine("File doesnt exist. Need to be with exe file.");
                Environment.Exit(1);
            }
            string[] values = txtFromFile.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            int i = Convert.ToInt32(values[0]);
            int condition = Convert.ToInt32(values[1]);
            StreamWriter file = new StreamWriter("json_result.txt", true);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (count < condition)
            {
                System.Uri uri = new Uri(string.Format("https://hola.org/challenges/word_classifier/testcase/{0}", i++));
                HttpClient client = new HttpClient();
                client.BaseAddress = uri;
                string response;
                response = client.GetStringAsync(uri).Result;
                if (!string.IsNullOrEmpty(response))
                {
                    while(response == "Rate limit exceeded")
                    {
                        ++ticket;
                        System.Threading.Thread.Sleep(1000);
                        response = client.GetStringAsync(uri).Result;
                    }
                    Dictionary<string, bool> words = JsonConvert.DeserializeObject<Dictionary<string, bool>>(response);
                    count += words.Count;
                    foreach (var item in words)
                    {
                        if (!string.IsNullOrEmpty(response))
                            file.WriteLine(string.Format(("{0} {1}"), item.Key, Convert.ToInt32(item.Value)));
                    }
                    if(count == 25420000)
                    {
                        ;
                    }
                }
                else
                {
                    ++ticket;
                }
                client.Dispose();
            }
            Console.WriteLine(ticket);
            stopWatch.Stop();
            Console.WriteLine(((stopWatch.ElapsedMilliseconds) / (1000)).ToString());
            file.Dispose();
        }
    }
}
