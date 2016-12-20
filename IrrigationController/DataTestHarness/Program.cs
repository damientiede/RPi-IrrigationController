using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DataTestHarness
{
    class Program
    {
        static HttpClient client = new HttpClient();
        static void Main()
        {
            RunAsync().Wait();
        }

        static async Task RunAsync()
        {
            client.BaseAddress = new Uri("http://www.creepytree.co.nz/IrrigationController/api.php/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Console.WriteLine("Press any key to get the data from the webapi");
            Console.ReadKey();
            Command c = await GetProductAsync("Command");
            Console.WriteLine(string.Format("CommandId:{0}, Title:{1}, Description:{2}",c.CommandId,c.Title,c.Description));
            Console.ReadKey();
        }
        static async Task<Command> GetProductAsync(string path)
        {
            Command cmd = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                //use JavaScriptSerializer from System.Web.Script.Serialization
                JavaScriptSerializer JSserializer = new JavaScriptSerializer();
                //deserialize to your class
                cmd = JSserializer.Deserialize<Command>(data);
                //cmd = await response.Content.ReadAsAsync<Command>();
            }
            return cmd;
        }
    }
}
