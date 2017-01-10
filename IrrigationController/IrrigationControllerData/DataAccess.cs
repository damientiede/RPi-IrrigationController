using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using log4net;

namespace IrrigationController.Data
{
    public static class DataAccess
    {
        static ILog log;
        static HttpClient client = new HttpClient();
        
        public static async Task<Uri> PutStatus(ControllerStatus cs)
        {
            InitClient();
            HttpResponseMessage response = await client.PostAsJsonAsync("ControllerStatusUpdate", cs);
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            return response.Headers.Location;
        }
        
        public static void InitClient()
        {
            client.BaseAddress = new Uri("http://www.creepytree.co.nz/IrrigationController/api.php/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public static async Task<Uri> PostEvent(EventHistory eh)
        {
            InitClient();
            HttpResponseMessage response = await client.PostAsJsonAsync("EventHistory", eh);
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            return response.Headers.Location;
        }

        public static async Task<Uri> PutCommand(CommandHistory ch)
        {
            InitClient();
            HttpResponseMessage response = await client.PostAsJsonAsync("CommandHistoryUpdate", ch);
            Console.WriteLine("PutCommand response: {0}", response.StatusCode.ToString());
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            return response.Headers.Location;
        }

        public static async Task<List<Schedule>> GetSchedules(string path)
        {
            InitClient();
            List<Schedule> schedule = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                schedule = await response.Content.ReadAsAsync<List<Schedule>>();
            }
            return schedule;
        }

        public static async Task<List<EventHistory>> GetEvents(string path)
        {
            InitClient();
            List<EventHistory> events = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                events = await response.Content.ReadAsAsync<List<EventHistory>>();
            }
            return events;
        }

        public static async Task<List<PendingCommand>> GetCommands()
        {
            //log.Debug("DataAccess.GetCommands()");
            InitClient();
            List<PendingCommand> commands = null;
            //Console.WriteLine("Getting commands");
            HttpResponseMessage response = await client.GetAsync("vwPendingCommands");
            if (response.IsSuccessStatusCode)
            {
                commands = await response.Content.ReadAsAsync<List<PendingCommand>>();
            }
            return commands;
        }

        

    }
}
