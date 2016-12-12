using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace IrrigationController.Data
{
    public static class DataAccess
    {
        static HttpClient client = new HttpClient();
        
        public static async Task<Uri> PutStatus(ControllerStatus cs)
        {
            InitClient();
            HttpResponseMessage response = await client.PutAsJsonAsync("ControllerStatus", cs);
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
            HttpResponseMessage response = await client.PostAsJsonAsync("CommandHistory", ch);
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

        public static async Task<List<CommandHistory>> GetCommands()
        {
            InitClient();
            List<CommandHistory> commands = null;
            HttpResponseMessage response = await client.GetAsync("vwPendingCommands");
            if (response.IsSuccessStatusCode)
            {
                commands = await response.Content.ReadAsAsync<List<CommandHistory>>();
            }
            return commands;
        }

        

    }
}
