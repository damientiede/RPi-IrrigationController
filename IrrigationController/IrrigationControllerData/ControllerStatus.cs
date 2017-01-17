using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrrigationController.Data
{
    public class ControllerStatus
    {
        public int Id;
        public string State;
        public string Mode;
        public DateTime TimeStamp;
        public int LowPressureFault;
        public int HighPressureFault;
        public int LowWellFault;
        public int OverloadFault;
        public int ResetRelay;
        public int PumpRelay;
        public int TankRelay;
        public int Station1Relay;
        public int Station2Relay;
        public int Station3Relay;
        public int Station4Relay;
        public int Station5Relay;
        public int Station6Relay;
        public int Station7Relay;
        public int Station8Relay;
        public int Station9Relay;        
        public int Station10Relay;
        //public int Station12Relay;
        public int Pressure;
    }
}
