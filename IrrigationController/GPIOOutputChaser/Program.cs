using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.GeneralPurpose.Behaviors;

namespace GPIOOutputChaser
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

        static void Main(string[] args)
        {
            // Declare outputs (leds)
            var leds = new PinConfiguration[]
                           {
                               Station1OutputPin.Output().Name("Led1").Enable(),
                               Station2OutputPin.Output().Name("Led2"),
                               Station3OutputPin.Output().Name("Led3").Enable(),
                               Station4OutputPin.Output().Name("Led4"),
                               Station5OutputPin.Output().Name("Led5").Enable(),
                               Station6OutputPin.Output().Name("Led6"),
                               Station7OutputPin.Output().Name("Led7").Enable(),
                               Station8OutputPin.Output().Name("Led8")
                               //Station9OutputPin.Output().Name("Led9").Enable(),
                               //Station10OutputPin.Output().Name("Led10"),
                               //Station11OutputPin.Output().Name("Led11").Enable(),
                               //Station12OutputPin.Output().Name("Led12")
                           };

            Console.WriteLine("Chaser Sample: Sample a LED chaser with a switch to change behavior");
            Console.WriteLine();
            Console.WriteLine("\tLed 1: {0}", Station1OutputPin);
            Console.WriteLine("\tLed 2: {0}", Station2OutputPin);
            Console.WriteLine("\tLed 3: {0}", Station3OutputPin);
            Console.WriteLine("\tLed 4: {0}", Station4OutputPin);
            Console.WriteLine("\tLed 5: {0}", Station5OutputPin);
            Console.WriteLine("\tLed 6: {0}", Station6OutputPin);
            Console.WriteLine("\tSwitch: {0}", PushButtonInputPin);
            Console.WriteLine();

            // Assign a behavior to the leds
            int period = 250;
            var behavior = new ChaserBehavior(leds)
            {
                Loop = true,// args.GetLoop(),
                RoundTrip = true,// args.GetRoundTrip(),
                Width = 8,// args.GetWidth(),
                Interval = TimeSpan.FromMilliseconds(period)//TimeSpan.FromMilliseconds(args.GetSpeed())
            };
            var switchButton = LowPressureFaultInputPin.Input()
               //.Name("Switch")
               //.Revert()
               //.Switch()
               //.Enable()
               .OnStatusChanged(b =>
               {
                   behavior.RoundTrip = !behavior.RoundTrip;
                   Console.WriteLine("Button switched {0}", b ? "on" : "off");
               });

            // Create connection
            var settings = new GpioConnectionSettings();// { Driver = driver };

            using (var connection = new GpioConnection(settings, leds))
            {
                Console.WriteLine("Using {0}, frequency {1:0.##}hz", settings.Driver.GetType().Name, 1000.0 / period);

                Thread.Sleep(1000);

                connection.Add(switchButton);
                connection.Start(behavior); // Starting the behavior automatically registers the pins to the connection, if needed.

                Console.ReadKey(true);

                connection.Stop(behavior);
            }
        }
    }
}
