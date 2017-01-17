using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raspberry.IO;
using Raspberry.IO.GeneralPurpose;

namespace IrrigationController.Data
{
    public class Station
    {
        public int Id;  //Station number eg. Station 4
        public bool OutputState;
        public string Name;
    }            
}
