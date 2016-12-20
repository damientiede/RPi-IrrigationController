using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrrigationController.Data
{
    public class PendingCommand
    {
        public int Id;
        public int CommandId;
        public DateTime Issued;
        public string Params;
        public string Description;
    }
}
