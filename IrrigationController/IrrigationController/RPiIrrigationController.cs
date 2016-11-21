﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using Raspberry.IO;
using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.Components.Converters.Mcp3008;
using log4net;
using MySql.Data.MySqlClient;
using IrrigationController.Models;
using UnitsNet;

namespace IrrigationController
{
    public class RPiIrrigationController
    {
        ILog log;
        private bool bShutdown;
        private DateTime dtLastStatusUpdate;
        private DateTime dtFaultStartDate;
        private int MinUpdateIntervalMinutes;
        private bool bResetRelayState;
        private bool bLowPressureFaultState;
        private bool bHighPressureFaultState;
        private bool bLowWellFaultState;
        private bool bOverloadFaultState;
        private bool bPushButtonPressedState;

        //Input schedule
        const ConnectorPin LowPressureFaultInputPin = ConnectorPin.P1Pin29;
        const ConnectorPin HighPressureFaultInputPin = ConnectorPin.P1Pin16;
        const ConnectorPin LowWellFaultInputPin = ConnectorPin.P1Pin12;
        const ConnectorPin OverloadFaultInputPin = ConnectorPin.P1Pin18;
        const ConnectorPin PushButtonInputPin = ConnectorPin.P1Pin10;

        //Analog input
        int Pressure;

        //Outputs
        const ConnectorPin Station1OutputPin = ConnectorPin.P1Pin15;
        const ConnectorPin Station2OutputPin = ConnectorPin.P1Pin37;
        const ConnectorPin Station3OutputPin = ConnectorPin.P1Pin13;
        const ConnectorPin Station4OutputPin = ConnectorPin.P1Pin35;
        const ConnectorPin Station5OutputPin = ConnectorPin.P1Pin11;
        const ConnectorPin Station6OutputPin = ConnectorPin.P1Pin33;
        const ConnectorPin Station7OutputPin = ConnectorPin.P1Pin7;
        //const ConnectorPin Station8OutputPin = ConnectorPin.P1Pin31;
        const ConnectorPin Station9OutputPin = ConnectorPin.P1Pin32;
        const ConnectorPin Station10OutputPin = ConnectorPin.P1Pin40;
        const ConnectorPin Station11OutputPin = ConnectorPin.P1Pin38;
        const ConnectorPin Station12OutputPin = ConnectorPin.P1Pin36;
        const ConnectorPin ResetRelayOutput = ConnectorPin.P1Pin31;

        //SPI
        const ConnectorPin adcClock = ConnectorPin.P1Pin23;
        const ConnectorPin adcMiso = ConnectorPin.P1Pin21;
        const ConnectorPin adcMosi = ConnectorPin.P1Pin19;
        const ConnectorPin adcCs = ConnectorPin.P1Pin24;

        //Local output state storage
        bool Station1OutputState = false;
        bool Station2OutputState = false;
        bool Station3OutputState = false;
        bool Station4OutputState = false;
        bool Station5OutputState = false;
        bool Station6OutputState = false;
        bool Station7OutputState = false;
        bool Station8OutputState = false;
        bool Station9OutputState = false;
        bool Station10OutputState = false;
        bool Station11OutputState = false;
        bool Station12OutputState = false;
        
        public bool LowPressureFault
        {
            get { return bLowPressureFaultState; }// ReadPinState(LowPressureFaultInput); }
        }

        public bool HighPressureFault
        {
            get { return bHighPressureFaultState; }// ReadPinState(HighPressureFaultInput); }
        }

        public bool LowWellFault
        {
            get { return bLowWellFaultState; }// ReadPinState(LowWellFaultInput); }
        }
        public bool OverloadFault
        {
            get { return bOverloadFaultState; }// ReadPinState(OverloadFaultInput); }
        }
        public bool PushButtonPressed
        {
            get { return bPushButtonPressedState; }//!ReadPinState(PushButtonInput); }  //the push button is wired active low
        }

        public bool ResetRelay
        {
            get
            {
                return bResetRelayState;
            }
            set
            {
                if (bResetRelayState != value)
                {
                    Log(string.Format("SetResetButton {0}", value == true ? "On" : "Off"));
                    CreateEvent(EventType.IOEvent, string.Format("Output 6 {0}", value == true ? "On" : "Off"));
                    
                    //LibGpio.Gpio.OutputValue(ResetRelayOutput, value);
                    bResetRelayState = value;
                }
            }
        }
        public enum State { Monitor = 0, WaitForTimeout, ConfirmReset, WaitForReset }
        public enum EventType { Unknown = 0, ApplicationEvent = 1, FaultEvent = 2, IOEvent = 3 }
        public State state;
        private int TimeoutDelaySeconds;   //timeout delay before reset in seconds

        public ElectricPotential volts = ElectricPotential.FromVolts(0);
        public ElectricPotential referenceVoltage = ElectricPotential.FromVolts(3.3);
        public IInputAnalogPin spiInput;

        protected List<IrrigationControllerCommand> Commands;

        public RPiIrrigationController()
        {
            Init();
            bShutdown = false;
        }

        public void Init()
        {
            log4net.Config.XmlConfigurator.Configure();
            log = LogManager.GetLogger("Controller");
            Log("IrrigationController start");
            CreateEvent(EventType.ApplicationEvent, "RPiIrrigationController started");

            state = State.Monitor;
            TimeoutDelaySeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutDelaySeconds"]);            
            MinUpdateIntervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["heartbeat"]);
            InitGpio();

        }

        public void InitGpio()
        {
            var outputpins = new PinConfiguration[]
                          {
                              Station1OutputPin.Output().Name("Station1"),
                              Station2OutputPin.Output().Name("Station2"),
                              Station3OutputPin.Output().Name("Station3"),
                              Station4OutputPin.Output().Name("Station4"),
                              Station5OutputPin.Output().Name("Station5"),
                              Station6OutputPin.Output().Name("Station6"),
                              Station7OutputPin.Output().Name("Station7"),
                              //Station8OutputPin.Output().Name("Station8"),
                              Station9OutputPin.Output().Name("Station9"),
                              Station10OutputPin.Output().Name("Station10"),
                              Station11OutputPin.Output().Name("Station11"),
                              Station12OutputPin.Output().Name("Station12"),
                              ResetRelayOutput.Output().Name("ResetRelay")
                          };
            GpioConnection connection = new GpioConnection(outputpins);

            connection.Add(LowPressureFaultInputPin.Input().OnStatusChanged(b =>
            {
                Console.WriteLine("LowPressureFaultInput {0}", b ? "on" : "off");
                bLowPressureFaultState = b;
                CreateEvent(EventType.IOEvent, string.Format("Input {0} on", LowPressureFaultInputPin.ToString()));
                CreateEvent(EventType.FaultEvent, string.Format("Low pressure fault  {0}", b ? "detected" : "cleared"));
            }));

            connection.Add(HighPressureFaultInputPin.Input().OnStatusChanged(b =>
            {
                Console.WriteLine("HighPressureFaultInput {0}", b ? "on" : "off");
                bHighPressureFaultState = b;
                CreateEvent(EventType.IOEvent, string.Format("Input {0} {1}", HighPressureFaultInputPin.ToString(), b ? "on" : "off"));
                CreateEvent(EventType.FaultEvent, string.Format("High pressure fault {0}",b ? "detected":"cleared"));
            }));

            connection.Add(LowWellFaultInputPin.Input().OnStatusChanged(b =>
            {
                Console.WriteLine("LowWellFaultInput {0}", b ? "on" : "off");
                bLowWellFaultState = b;
                CreateEvent(EventType.IOEvent, string.Format("Input {0} {1}", LowWellFaultInputPin.ToString(), b ? "on" : "off"));
                CreateEvent(EventType.FaultEvent, string.Format("Low well fault {0}", b ? "detected" : "cleared"));
                if (b)
                {
                    dtFaultStartDate = DateTime.Now;
                    Log(string.Format("Initializing timeout at {0}", dtFaultStartDate.ToString()));
                    ChangeState(State.WaitForTimeout);
                }
                else
                {
                    ChangeState(State.Monitor);
                }
            }));

            connection.Add(OverloadFaultInputPin.Input().OnStatusChanged(b =>
            {
                Console.WriteLine("OverloadFaultInput {0}", b ? "on" : "off");
                bOverloadFaultState = b;
            }));

            //ElectricPotential referenceVoltage = ElectricPotential.FromVolts(3.3);

            var driver = new MemoryGpioConnectionDriver(); //GpioConnectionSettings.DefaultDriver;

            Mcp3008SpiConnection spi = new Mcp3008SpiConnection(
                driver.Out(adcClock),
                driver.Out(adcCs),
                driver.In(adcMiso),
                driver.Out(adcMosi));
             
            spiInput = spi.In(Mcp3008Channel.Channel0);

            connection.Open();
        }
        public void Monitor()
        {
            while (!bShutdown)
            {
                //process pending command from CommandHistory
                ProcessCommand();

                //get SPI reading
                ReadSPI();

                switch (state)
                {
                    case State.Monitor:
                        //CheckFaultInputs();
                        break;

                    case State.WaitForTimeout:
                        if (!bLowWellFaultState)
                        {
                            ResetRelay = false;
                            ChangeState(State.Monitor);
                        }
                        if (DateTime.Now > dtFaultStartDate.AddSeconds(TimeoutDelaySeconds))
                        {
                            ResetRelay = true;
                            ChangeState(State.ConfirmReset);
                        }
                        break;

                    case State.ConfirmReset:
                        if (!bLowWellFaultState)
                        {
                            ResetRelay = false;
                            ChangeState(State.Monitor);
                        }
                        break;

                    case State.WaitForReset:
                        if (!InFaultState())
                        {
                            ChangeState(State.Monitor);
                        }
                        break;

                    default:
                        break;
                }

                if (DateTime.Now > (dtLastStatusUpdate.AddMinutes(MinUpdateIntervalMinutes)))
                {
                    //heartbeat
                    RecordStatus();
                }
                Thread.Sleep(100);
            }
        }        
        
        public void ReadSPI()
        {            
            var v = referenceVoltage * (double)spiInput.Read().Relative;
            Pressure = Convert.ToInt32(v.Millivolts);
            if ((Math.Abs(v.Millivolts - volts.Millivolts) > 100))
            {
                volts = ElectricPotential.FromMillivolts(v.Millivolts);
                Console.WriteLine("Pressure: {0}", Pressure);
                CreateEvent(EventType.IOEvent, string.Format("Pressure change {0}", Pressure));
            }
        }
        public bool InFaultState()
        {
            return LowPressureFault || HighPressureFault || LowWellFault || OverloadFault;
        }

        public void ChangeState(State newState)
        {
            Log(string.Format("ChangeState {0}", newState.ToString()));
            CreateEvent(EventType.ApplicationEvent, string.Format("Change state to {0}", newState.ToString()));
            state = newState;
            RecordStatus();
        }
        public void Log(string msg)
        {
            //Console.WriteLine(msg);
            log.Info(msg);
        }
        public void CreateEvent(EventType eventType, string desc)
        {
            string sql = string.Format("INSERT INTO EventHistory (TStamp, EventType, Description) values (CURRENT_TIMESTAMP(), {0}, '{1}')", (int)eventType, desc);
            log.Debug(sql);
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["IrrigationController"].ToString()))
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                conn.Open();

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void ProcessCommand()
        {
            IrrigationControllerCommand cmd = GetPendingCommand();
            if (cmd == null)
            {
                return;
            }

            Log(string.Format("ProcessCommand(): processing command: {0}", cmd.Title));
            cmd.SetActioned();

            switch (cmd.CommandId)
            {
                case 1:         //SHUTDOWN
                    CreateEvent(EventType.ApplicationEvent, "Shutdown");
                    bShutdown = true;
                    break;
                case 2:         //MANUAL
                    break;
                case 3:         //AUTO
                    break;
                case 4:         //STOP PUMP
                    break;
                case 5:         //STANDBY
                    break;
                default:
                    break;
            }

        }
        protected IrrigationControllerCommand GetPendingCommand()
        {
            IrrigationControllerCommand icc = new IrrigationControllerCommand();
            string sql = "select Id, CommandId, Params, Title, Description, Issued from vwPendingCommands order by Issued Limit 1";
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["IrrigationController"].ToString()))
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                conn.Open();

                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    icc.Id = (int)reader["Id"];
                    icc.CommandId = (int)reader["CommandId"];
                    icc.Title = reader["Title"].ToString();
                    icc.Description = reader["Description"].ToString();
                    icc.Params = reader["Params"].ToString();
                    icc.Issued = (DateTime)reader["Issued"];
                }
                conn.Close();
            }
            if (icc.Id > 0)
            {
                return icc;
            }
            return null;

        }
        public void RecordStatus()
        {
            string sql = string.Format("UPDATE ControllerStatus set State = '{0}', Mode = '{1}', TStamp = now(), LowPressureFault = {2}, HighPressureFault = {3}, LowWellFault = {4}, OverloadFault = {5}, ResetRelay = {6}, Station1Relay = {7}, Station2Relay = {8}, Station3Relay = {9}, Station4Relay = {10}, Station5Relay = {11}, Station6Relay = {12}, Station7Relay = {13}, Station8Relay = {14}, Station9Relay = {15}, Station10Relay = {16}, Station11Relay = {17}, Station12Relay = {18}, Pressure = {19}",
                state.ToString(),
                "Monitoring",
                LowPressureFault,
                HighPressureFault,
                LowWellFault,
                OverloadFault,
                ResetRelay,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                Pressure);

            log.Debug(sql);
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["IrrigationController"].ToString()))
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                conn.Open();

                cmd.ExecuteNonQuery();
                conn.Close();
            }
            dtLastStatusUpdate = DateTime.Now;
        }
    }
}
