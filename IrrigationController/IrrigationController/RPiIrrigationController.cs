using System;
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
using IrrigationController.Data;
using UnitsNet;

namespace IrrigationController
{
    public class RPiIrrigationController
    {
        #region Declarations

        ILog log;
        private bool bShutdown;
        private DateTime dtLastCommandQuery;
        private DateTime dtLastStatusUpdate;
        private DateTime dtFaultStartDate;
        private int MinUpdateIntervalMinutes;
        private int WebCommandQueryIntervalSeconds;
        private bool bResetRelayState;
        private bool bLowPressureFaultState;
        private bool bHighPressureFaultState;
        private bool bLowWellFaultState;
        private bool bOverloadFaultState;
        private bool bPushButtonPressedState;
        private bool bUpdateStatus;

        //Input schedule
        const ConnectorPin LowPressureFaultInputPin = ConnectorPin.P1Pin29;
        const ConnectorPin HighPressureFaultInputPin = ConnectorPin.P1Pin16;
        const ConnectorPin LowWellFaultInputPin = ConnectorPin.P1Pin12;
        const ConnectorPin OverloadFaultInputPin = ConnectorPin.P1Pin18;
        const ConnectorPin PushButtonInputPin = ConnectorPin.P1Pin10;

        //Analog input
        int Pressure;
        const double TransducerCharacteristic = 0.217;   //conversion factor from digital count to kPa

        //Outputs
        const ConnectorPin Station1OutputPin = ConnectorPin.P1Pin15;
        const ConnectorPin Station2OutputPin = ConnectorPin.P1Pin37;
        const ConnectorPin Station3OutputPin = ConnectorPin.P1Pin13;
        const ConnectorPin Station4OutputPin = ConnectorPin.P1Pin35;
        const ConnectorPin Station5OutputPin = ConnectorPin.P1Pin11;
        const ConnectorPin Station6OutputPin = ConnectorPin.P1Pin33;
        const ConnectorPin Station7OutputPin = ConnectorPin.P1Pin7;
        const ConnectorPin Station8OutputPin = ConnectorPin.P1Pin31;
        const ConnectorPin PumpOperationPin = ConnectorPin.P1Pin32;
        const ConnectorPin TankRelayOutputPin = ConnectorPin.P1Pin40;
        const ConnectorPin SpareOutputPin = ConnectorPin.P1Pin38;
        const ConnectorPin ResetRelayOutputPin = ConnectorPin.P1Pin36;

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
        bool Station8OutputState = false;   //shelter
        bool PumpOperationOutputState = false;
        bool TankRelayOutputState = false;  //used to supply water from the large H2O tank
        bool SpareOutputState = false;
        bool ResetRelayOutputState = false;

        GpioConnection connection;
        PinConfiguration[] outputs;
        List<Station> stations;

        public enum State { Monitor = 0, WaitForTimeout, ConfirmReset, WaitForReset }
        public enum Mode { Auto = 0, Manual, Off }
        public enum EventType { Unknown = 0, ApplicationEvent = 1, FaultEvent = 2, IOEvent = 3, IrrigationStart=4, IrrigationStop=5 }
        public State state;
        public Mode mode;
        public bool Irrigating;
        private int TimeoutDelaySeconds;   //timeout delay before reset in seconds

        public ElectricPotential volts = ElectricPotential.FromVolts(0);
        public ElectricPotential referenceVoltage = ElectricPotential.FromVolts(3.3);
        public IInputAnalogPin spiInput;

        protected List<IrrigationControllerCommand> Commands;
        protected List<PendingCommand> WebCommands;

        #endregion

        public bool LowPressureFault
        {
            get { return bLowPressureFaultState; }
        }

        public bool HighPressureFault
        {
            get { return bHighPressureFaultState; }
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
        

        public RPiIrrigationController()
        {
            Init();
            bShutdown = false;
        }

        public void Init()
        {
            //log4net.Config.XmlConfigurator.Configure();
            log = LogManager.GetLogger("Controller");
            Log("IrrigationController start");            
            CreateEvent(EventType.ApplicationEvent, "RPi-IrrigationController started");

            state = State.Monitor;
            mode = Mode.Manual;
            Irrigating = false;

            TimeoutDelaySeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutDelaySeconds"]);
            MinUpdateIntervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["heartbeat"]);
            WebCommandQueryIntervalSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["CommandQueryInterval"]);

            //initialize interface with raspberry pi GPIO
            InitGpio();            

            //initialize stations
            stations = new List<Station>();
            for (int i = 1; i < 9; i++)
            {
                Station station = new Station() { Id = i, Name = String.Format("Station{0}", i), OutputState = false };
                stations.Add(station);
            }
            log.Debug("Stations created");

            AllOutputsOff();
            WebCommands = new List<PendingCommand>();
            bUpdateStatus = true;
        }

        public void InitGpio()
        {
            outputs = new PinConfiguration[]
                        {
                            Station1OutputPin.Output().Name("Station1"),
                            Station2OutputPin.Output().Name("Station2"),
                            Station3OutputPin.Output().Name("Station3"),
                            Station4OutputPin.Output().Name("Station4"),
                            Station5OutputPin.Output().Name("Station5"),
                            Station6OutputPin.Output().Name("Station6"),
                            Station7OutputPin.Output().Name("Station7"),
                            Station8OutputPin.Output().Name("Station8"),
                            PumpOperationPin.Output().Name("PumpOperation"),
                            TankRelayOutputPin.Output().Name("TankRelay"),
                            SpareOutputPin.Output().Name("Spare"),                              
                            ResetRelayOutputPin.Output().Name("ResetRelay")
                        };
            connection = new GpioConnection(outputs);

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
                CreateEvent(EventType.FaultEvent, string.Format("High pressure fault {0}", b ? "detected" : "cleared"));
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
        
        public async void Monitor()
        {
           
            log.Debug("Starting Monitor()");

            while (!bShutdown)
            {
                try
                {
                    //get SPI reading
                    ReadSPI();

                    switch (state)
                    {
                        case State.Monitor:

                            if (mode == Mode.Manual)
                            {
                                //check to see if we are in an irrigation window
                                if (ManualProgram.Start != null)
                                {
                                    DateTime dtStart = (DateTime)ManualProgram.Start;
                                    if (DateTime.Now > ManualProgram.Start && DateTime.Now < dtStart.AddMinutes(ManualProgram.Duration))
                                    {
                                        //check to see that the correct outputs are on
                                        SwitchStation(ManualProgram.StationId);
                                        Irrigating = true;
                                    }
                                    else
                                    {
                                        if (Irrigating)
                                        {
                                            //record end of manual program
                                            CreateEvent(EventType.IrrigationStop, string.Format("Station:{0} elapsed:{1} minutes", ManualProgram.StationId, (DateTime.Now - dtStart).TotalMinutes.ToString("N2")));
                                            bUpdateStatus = true;
                                        }
                                        SwitchStation(0);  //Station 0 - all stations off
                                        Irrigating = false;
                                        ManualProgram.StationId = 0;
                                        ManualProgram.Start = null;
                                        ManualProgram.Duration = 0;
                                    }
                                }
                            }
                            if (mode == Mode.Auto)
                            {
                                //check to see if we are in a scheduled irrigation window

                            }
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

                    switch (mode)
                    {
                        case Mode.Auto:

                            break;
                        case Mode.Manual:

                            break;
                        case Mode.Off:

                            break;
                        default:
                            break;
                    }

                    // retrieve web commands every WebCommandQueryIntervalSeconds seconds
                    if (DateTime.Now > (dtLastCommandQuery.AddSeconds(WebCommandQueryIntervalSeconds)))
                    {
                        //get pending commands from web            
                        await GetCommands();
                        //log.DebugFormat("{0} commands queued", WebCommands.Count());

                        //process pending commands from CommandHistory
                        await ProcessCommands();

                        dtLastCommandQuery = DateTime.Now;
                    }

                    if (DateTime.Now > (dtLastStatusUpdate.AddMinutes(MinUpdateIntervalMinutes)))
                    {
                        bUpdateStatus = true;
                    }

                    if (bUpdateStatus)
                    {
                        //heartbeat
                        RecordStatus();
                        bUpdateStatus = false;
                        dtLastStatusUpdate = DateTime.Now;
                    }

                    Thread.Sleep(100);

                }
                catch (Exception ex)
                {
                    log.ErrorFormat(ex.Message);
                }                
            }
            //end program            
            connection.Close();
            log.Info("Shutting down");
            Console.WriteLine("Shutting down");
        }

        public void ReadSPI()
        {
            var v = referenceVoltage * (double)spiInput.Read().Relative;
            Pressure = Convert.ToInt32(v.Millivolts * TransducerCharacteristic);
            double diff = Math.Abs(v.Millivolts - volts.Millivolts);
            if (diff > 250)
            {
                volts = ElectricPotential.FromMillivolts(v.Millivolts);
                Console.WriteLine("Pressure: {0}", Pressure);
                CreateEvent(EventType.IOEvent, string.Format("Pressure change {0}", Pressure));
                bUpdateStatus = true;
            }
            if ((diff > 150) && !bUpdateStatus)
            {
                volts = ElectricPotential.FromMillivolts(v.Millivolts);
                Console.WriteLine("Pressure: {0}", Pressure);
                CreateEvent(EventType.IOEvent, string.Format("Pressure change {0}", Pressure));
                bUpdateStatus = true;
            }

        }
        public bool InFaultState()
        {
            return false;// LowPressureFault || HighPressureFault || LowWellFault || OverloadFault;
        }
        public string getFaultDetail()
        {
            if (LowPressureFault) { return "Fault - Low pressure"; }
            if (HighPressureFault) { return "Fault - High pressure"; }
            if (LowWellFault) { return "Fault - Low well"; }
            if (OverloadFault) { return "Fault - Overload"; }
            return "";
        }
        public void ChangeState(State newState)
        {
            Log(string.Format("ChangeState {0}", newState.ToString()));
            CreateEvent(EventType.ApplicationEvent, string.Format("Change state to {0}", newState.ToString()));
            state = newState;
            bUpdateStatus = true; 
        }
        public void SetMode(Mode newMode)
        {
            if (mode != newMode)
            {
                Log(string.Format("SetMode: {0}", newMode.ToString()));                
                CreateEvent(EventType.ApplicationEvent, string.Format("Set mode to {0}", newMode.ToString()));
                mode = newMode;
            }
        }
        public void Log(string msg)
        {
            //Console.WriteLine(msg);
            log.Info(msg);
        }
        public async void CreateEvent(EventType eventType, string desc)
        {
            //string sql = string.Format("INSERT INTO EventHistory (TimeStamp, EventType, Description) values (CURRENT_TIMESTAMP(), {0}, '{1}')", (int)eventType, desc);
            //log.Debug(sql);
            //using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["IrrigationController"].ToString()))
            //{
            //    MySqlCommand cmd = new MySqlCommand(sql, conn);
            //    conn.Open();

            //    cmd.ExecuteNonQuery();
            //    conn.Close();
            //}
            EventHistory eh = new EventHistory { EventType = (int)eventType, TimeStamp = DateTime.Now, Value = desc };
            Uri x = await DataAccess.PostEvent(eh);
        }

        public async Task GetCommands()
        {
            try {
                List<PendingCommand> commands = await DataAccess.GetCommands();
                if (commands.Count() > 0)
                {
                    log.DebugFormat("RPiIrrigationController.GetCommands() {0} commands retrieved", commands.Count());
                }
                foreach (PendingCommand command in commands)
                {
                    log.DebugFormat("CommandId:{0} Params:{1} Issued:{2} ", command.CommandId, command.Params, command.Issued);
                    WebCommands.Add(command);
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("GetCommands(): {0}",ex.Message);
            }
        }

        protected async Task ProcessCommands()
        {
            //log.InfoFormat("RPiIrrigationController.ProcessCommands()");
            PendingCommand cmd;
            try
            {
                if (WebCommands.Count() > 0)
                {
                    cmd = WebCommands.Last();
                    switch (cmd.CommandId)
                    {
                        case 1:         //SHUTDOWN
                            CreateEvent(EventType.ApplicationEvent, "Shutdown");
                            AllOutputsOff();
                            bShutdown = true;
                            break;
                        case 2:         //Auto
                            //SetMode(Mode.Auto);
                            break;
                        case 3:         //Manual
                                        //Extract the stationid and duration params of the manual command
                                        //eg. 4,60 - station 4 for 60 minutes
                            string[] parts = cmd.Params.Split(',');
                            
                            if (parts.Length == 2)
                            {
                                int newStationId = 0;                                
                                Int32.TryParse(parts[0], out newStationId);
                                int newDuration = 0;
                                Int32.TryParse(parts[1], out newDuration);                                

                                if (mode == Mode.Manual && Irrigating)
                                {
                                    //Need record the end of existing program first
                                    CreateEvent(EventType.IrrigationStop, string.Format("Station:{0} elapsed:{1} minutes", ManualProgram.StationId, getElapsed().ToString("N2")));
                                }

                                //start new program
                                ManualProgram.StationId = newStationId;
                                ManualProgram.Duration = newDuration;
                                ManualProgram.Start = DateTime.Now;
                                SetMode(Mode.Manual);

                                //Record start of new manual program
                                CreateEvent(EventType.IrrigationStart, string.Format("Station:{0} duration:{1} minutes", ManualProgram.StationId, ManualProgram.Duration));
                            }
                            break;
                        case 4:         //Off
                            CreateEvent(EventType.IrrigationStop, string.Format("Station:{0} elapsed:{1} minutes", ManualProgram.StationId, getElapsed().ToString("N2")));
                            AllOutputsOff();
                            SetMode(Mode.Off);
                            break;
                        case 5:         //GetSchedules
                            break;
                        default:
                            break;
                    }

                    CommandHistory ch = new CommandHistory()
                    {
                        Id = cmd.Id,
                        CommandId = cmd.CommandId,
                        Params = cmd.Params,
                        Issued = cmd.Issued,
                        Actioned = DateTime.Now
                    };
                    log.DebugFormat("Marking command as actioned: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(ch));

                    var x = await DataAccess.PutCommand(ch);

                    WebCommands.Remove(cmd);
                    bUpdateStatus = true;
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("ProcessCommands(): {0}", ex.Message);
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
                try
                {
                    if (reader.Read())
                    {
                        icc.Id = (int)reader["Id"];
                        icc.CommandId = (int)reader["CommandId"];
                        icc.Title = reader["Title"].ToString();
                        icc.Description = reader["Description"].ToString();
                        icc.Params = reader["Params"].ToString();
                        icc.Issued = (DateTime)reader["Issued"];
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
            if (icc.Id > 0)
            {
                return icc;
            }
            return null;

        }
        public async void RecordStatus()
        {
            //string sql = string.Format("UPDATE ControllerStatus set State = '{0}', Mode = '{1}', TimeStamp = now(), LowPressureFault = {2}, HighPressureFault = {3}, LowWellFault = {4}, OverloadFault = {5}, ResetRelay = {6}, Station1Relay = {7}, Station2Relay = {8}, Station3Relay = {9}, Station4Relay = {10}, Station5Relay = {11}, Station6Relay = {12}, Station7Relay = {13}, Station8Relay = {14}, Station9Relay = {15}, Station10Relay = {16}, Station11Relay = {17}, Station12Relay = {18}, Pressure = {19}",
            //    state.ToString(),
            //    mode.ToString(),
            //    LowPressureFault,
            //    HighPressureFault,
            //    LowWellFault,
            //    OverloadFault,
            //    ResetRelay,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    false,
            //    Pressure);

            //log.Debug(sql);
            //using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["IrrigationController"].ToString()))
            //{
            //    MySqlCommand cmd = new MySqlCommand(sql, conn);
            //    conn.Open();

            //    cmd.ExecuteNonQuery();
            //    conn.Close();
            //}            

            //ControllerStatus cs = new ControllerStatus
            //{
            //    Id = 0,
            //    State = state.ToString(),
            //    Mode = mode.ToString(),
            //    TimeStamp = DateTime.Now,
            //    LowPressureFault = Convert.ToInt32(LowPressureFault),
            //    HighPressureFault = Convert.ToInt32(HighPressureFault),
            //    LowWellFault = Convert.ToInt32(LowWellFault),
            //    OverloadFault = Convert.ToInt32(OverloadFault),
            //    PumpRelay = Convert.ToInt32(PumpOperationOutputState),
            //    ResetRelay = Convert.ToInt32(ResetRelayOutputState),
            //    TankRelay = Convert.ToInt32(TankRelayOutputState),
            //    Station1Relay = Convert.ToInt32(stations[0].OutputState),
            //    Station2Relay = Convert.ToInt32(stations[1].OutputState),
            //    Station3Relay = Convert.ToInt32(stations[2].OutputState),
            //    Station4Relay = Convert.ToInt32(stations[3].OutputState),
            //    Station5Relay = Convert.ToInt32(stations[4].OutputState),
            //    Station6Relay = Convert.ToInt32(stations[5].OutputState),
            //    Station7Relay = Convert.ToInt32(stations[6].OutputState),
            //    Station8Relay = Convert.ToInt32(stations[7].OutputState),
            //    Station9Relay = 0,
            //    Station10Relay = 0,
            //    Pressure = Pressure
            //};
            //log.DebugFormat("ControllerStatus: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(cs));
            //try
            //{
            //    var x = await DataAccess.PutControllerStatus(cs);
            //}
            //catch(Exception ex)
            //{
            //    log.ErrorFormat("RecordStatus():{0}", ex.Message);
            //}

            string sState = "Off";
            if (InFaultState()) { sState = getFaultDetail(); } else if (Irrigating) { sState = string.Format("Irrigating Station {0}",ManualProgram.StationId); }
            Status status = new Status()
            {
                Id = 0,
                State = sState,
                Mode = mode.ToString(),
                TimeStamp = DateTime.Now,
                Pressure = Pressure,
                Station = ManualProgram.StationId,
                Start = ManualProgram.Start,
                Duration = ManualProgram.Duration,
                Inputs = string.Format("{0}{1}{2}{3}",
                        Convert.ToInt32(LowPressureFault),
                        Convert.ToInt32(HighPressureFault),
                        Convert.ToInt32(LowWellFault),
                        Convert.ToInt32(OverloadFault)),
                Outputs = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}",
                        Convert.ToInt32(PumpOperationOutputState),                        
                        Convert.ToInt32(TankRelayOutputState),
                        Convert.ToInt32(stations[0].OutputState),
                        Convert.ToInt32(stations[1].OutputState),
                        Convert.ToInt32(stations[2].OutputState),
                        Convert.ToInt32(stations[3].OutputState),
                        Convert.ToInt32(stations[4].OutputState),
                        Convert.ToInt32(stations[5].OutputState),
                        Convert.ToInt32(stations[6].OutputState),
                        Convert.ToInt32(stations[7].OutputState),
                        Convert.ToInt32(ResetRelayOutputState))
            };
            log.DebugFormat("Status: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(status));
            try
            {
                var x = await DataAccess.PutStatus(status);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("RecordStatus():{0}", ex.Message);
            }

        }

        public void SwitchStation(int stationId)
        {
            if (stationId==0)
            {
                //switch all stations off
                AllOutputsOff();
                return;
            }
            foreach(Station station in stations)
            {
                if (stationId == station.Id)
                {
                    if (!station.OutputState)
                    {
                        Log(string.Format("Turning on Station{0} OutputState:{1}",station.Name,station.OutputState));
                        connection.Toggle(station.Name);
                        station.OutputState = true;
                        bUpdateStatus = true;
                    }
                    //for all stations except station 8 (shelter), we activate the pump
                    //for station 8, we activate the tank relay
                    if (station.Id == 8)
                    {
                        if (!TankRelayOutputState)
                        {
                            Log("Turning on tank relay");
                            connection.Toggle("TankRelay");
                            TankRelayOutputState = true;
                            bUpdateStatus = true;
                        }
                        if (PumpOperationOutputState)
                        {
                            Log("Turning off pump");
                            connection.Toggle("PumpOperation");
                            PumpOperationOutputState = false;
                            bUpdateStatus = true;
                        }
                    }
                    else
                    {
                        if (TankRelayOutputState)
                        {
                            Log("Turning off tank relay");
                            connection.Toggle("TankRelay");
                            TankRelayOutputState = false;
                            bUpdateStatus = true;
                        }
                        if (!PumpOperationOutputState)
                        {
                            Log("Turning on pump");
                            connection.Toggle("PumpOperation");
                            PumpOperationOutputState = true;
                            bUpdateStatus = true;
                        }
                    }
                    
                }
                else
                {
                    if (station.OutputState != false)
                    {
                        Log(string.Format("Turning off Station{0} OutputState:{1}", station.Name,station.OutputState));
                        connection.Toggle(station.Name);
                        station.OutputState = false;
                        bUpdateStatus = true;
                    }
                }
                Irrigating = true;
            }

           
        }
        public void AllOutputsOff()
        {
            //Log("AllOutputsOff()");
            foreach (Station station in stations)
            {
                if (station.OutputState)
                {
                    connection.Toggle(station.Name);
                    station.OutputState = false;
                }
            }
            if (PumpOperationOutputState) { connection.Toggle("PumpOperation"); PumpOperationOutputState = false; }
            if (TankRelayOutputState) { connection.Toggle("TankRelay"); TankRelayOutputState = false; }
            if (SpareOutputState) { connection.Toggle("Spare"); SpareOutputState = false; }
            if (ResetRelayOutputState) { connection.Toggle("ResetRelay"); ResetRelayOutputState = false; }
            Irrigating = false;
            ManualProgram.Duration = 0;
            ManualProgram.StationId = 0;
        }

        public double getElapsed()
        {
            if (ManualProgram.Start == null)
            {
                return -1;
            }
            DateTime dtStart = (DateTime)ManualProgram.Start;
            return (DateTime.Now - dtStart).TotalMinutes;
        }
    }
}
