using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;

namespace JSON_Deserialize
{
    class Program
    {
        static void Main(string[] args)
        {
            int count = 0;
            int i = 1;
            StreamWriter file = new StreamWriter("file.txt", true);
            while (count < 1000000000)
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
                client.Dispose();
            }
            file.Dispose();
        }
    }
}
