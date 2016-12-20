using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Raspberry.IO;
using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.GeneralPurpose.Behaviors;
using Raspberry.IO.Components.Converters.Mcp3008;
using UnitsNet;

namespace GPIOTestHarness
{
    class Program
    {
        //Input schedule
        const ConnectorPin LowPressureFaultInputPin = ConnectorPin.P1Pin29;
        const ConnectorPin HighPressureFaultInputPin = ConnectorPin.P1Pin16;
        const ConnectorPin LowWellFaultInputPin = ConnectorPin.P1Pin12;
        const ConnectorPin OverloadFaultInputPin = ConnectorPin.P1Pin18;
        const ConnectorPin PushButtonInputPin = ConnectorPin.P1Pin10;

        //Outputs
        const ConnectorPin Station1OutputPin = ConnectorPin.P1Pin15;
        const ConnectorPin Station2OutputPin = ConnectorPin.P1Pin37;        
        const ConnectorPin Station3OutputPin = ConnectorPin.P1Pin13;       
        const ConnectorPin Station4OutputPin = ConnectorPin.P1Pin35;        
        const ConnectorPin Station5OutputPin = ConnectorPin.P1Pin11;
        const ConnectorPin Station6OutputPin = ConnectorPin.P1Pin33;
        const ConnectorPin Station7OutputPin = ConnectorPin.P1Pin7;
        const ConnectorPin Station8OutputPin = ConnectorPin.P1Pin31;
        //const ConnectorPin Station9OutputPin = ConnectorPin.P1Pin32;
        //const ConnectorPin Station10OutputPin = ConnectorPin.P1Pin40;
        //const ConnectorPin Station11OutputPin = ConnectorPin.P1Pin38;
        //const ConnectorPin Station12OutputPin = ConnectorPin.P1Pin36;

        //SPI
        const ConnectorPin adcClock = ConnectorPin.P1Pin23;        
        const ConnectorPin adcMiso = ConnectorPin.P1Pin21;        
        const ConnectorPin adcMosi = ConnectorPin.P1Pin19;        
        const ConnectorPin adcCs = ConnectorPin.P1Pin24;

        static void Main(string[] args)
        {
            Console.WriteLine("GPIOTestHarness");
            bool Station1OutputState = false;
            bool Station2OutputState = false;
            bool Station3OutputState = false;
            bool Station4OutputState = false;

            //var Output1 = Station1OutputPin.Output();
            //var Output2 = Station2OutputPin.Output();
            //var Output3 = Station3OutputPin.Output();
            //var Output4 = Station4OutputPin.Output();
            var pins = new PinConfiguration[]
            {
                Station1OutputPin.Output().Name("Output1"),
                Station2OutputPin.Output().Name("Output2"),
                Station3OutputPin.Output().Name("Output3"),
                Station4OutputPin.Output().Name("Output4")                             
            };
            //var settings = new GpioConnectionSettings();
            var connection = new GpioConnection(pins);

            var Input1 = LowPressureFaultInputPin.Input().OnStatusChanged(b =>
                {
                    Console.WriteLine("LowPressureFaultInput {0}", b ? "on" : "off");
                    if (Station1OutputState != b) { connection.Toggle("Output1"); Station1OutputState = b; }                                        
                });
            connection.Add(Input1);
            var Input2 = HighPressureFaultInputPin.Input().OnStatusChanged(b =>
            {
                Console.WriteLine("HighPressureFaultInput {0}", b ? "on" : "off");
                if (Station2OutputState != b) { connection.Toggle("Output2"); Station2OutputState = b; }
            });
            connection.Add(Input2);
            var Input3 = LowWellFaultInputPin.Input().OnStatusChanged(b =>
            {
                Console.WriteLine("LowWellFaultInput {0}", b ? "on" : "off");
                if (Station3OutputState != b) { connection.Toggle("Output3"); Station3OutputState = b; }
            });
            connection.Add(Input3);
            var Input4 = OverloadFaultInputPin.Input().OnStatusChanged(b =>
            {
                Console.WriteLine("OverloadFaultInput {0}", b ? "on" : "off");
                if (Station4OutputState != b) { connection.Toggle("Output4"); Station4OutputState = b; }
            });
            connection.Add(Input4);

            ElectricPotential referenceVoltage = ElectricPotential.FromVolts(3.3);

            var driver = new MemoryGpioConnectionDriver(); //GpioConnectionSettings.DefaultDriver;

            Mcp3008SpiConnection spi = new Mcp3008SpiConnection(
                driver.Out(adcClock),
                driver.Out(adcCs),
                driver.In(adcMiso),
                driver.Out(adcMosi));

            IInputAnalogPin inputPin = spi.In(Mcp3008Channel.Channel0);

            connection.Open();
            ElectricPotential volts = ElectricPotential.FromVolts(0);
                     
            while (!Console.KeyAvailable)
            {
                var v = referenceVoltage * (double)inputPin.Read().Relative;
                if ((Math.Abs(v.Millivolts - volts.Millivolts) > 100))
                {
                    volts = ElectricPotential.FromMillivolts(v.Millivolts);
                    Console.WriteLine("Voltage ch0: {0}", volts.Millivolts.ToString());
                }                               
            }
            connection.Close();        
        }
    }
}
