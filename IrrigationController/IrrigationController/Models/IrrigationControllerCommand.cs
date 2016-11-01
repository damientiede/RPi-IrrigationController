using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using log4net;
using MySql.Data.MySqlClient;

namespace IrrigationController.Models
{
    public class IrrigationControllerCommand
    {
        ILog log;
        public int? Id { get; set; }
        public int? CommandId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Params { get; set; }
        public DateTime? Issued { get; set; }
        public DateTime? Actioned { get; set; }

        public IrrigationControllerCommand()
        {
            log = LogManager.GetLogger("Monitor");
        }

        public IrrigationControllerCommand(int id, int commandid, string title, string description, string param, DateTime issued)
        {
            Id = id;
            CommandId = commandid;
            Title = title;
            Description = description;
            Params = param;
            Issued = issued;
            log = LogManager.GetLogger("Monitor");
        }
        public void SetActioned()
        {
            Actioned = DateTime.Now;
            string sql = string.Format("UPDATE CommandHistory set Actioned = CURRENT_TIMESTAMP() where Id = {0}", this.Id);
            log.Debug(sql);
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["IrrigationController"].ToString()))
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}
