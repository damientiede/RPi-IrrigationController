using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace IrrigationController
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            ILog log = LogManager.GetLogger("Controller");
            try
            {
                RPiIrrigationController ctrl = new RPiIrrigationController();
                ctrl.Monitor();
            }
            catch (Exception ex)
            {
                log.ErrorFormat(ex.Message);
            }        
        }
    }
}
