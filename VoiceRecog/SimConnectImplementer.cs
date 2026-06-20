using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using System.Diagnostics;
using VoiceRecog.Services;

namespace VoiceRecog
{
    /// <summary>
    /// This class is responsible for  the connection to SimConnect, and contains functions to transmit events, and read and sent Simdata
    /// 
    /// Add Microsoft.FlightSimulator.SimConnect.dll to the folder and to the dependencies
    /// 
    /// Instantiate object through SimConnectImplementer mysimconnect = new SimConnectImplementer();
    /// 
    /// Functions to use:
    /// Connect()
    /// Disconnect()
    /// SendEvent(eventnaam, datavalue)
    /// SetHandle() - for output to form
    /// HandleWndProc - for output to form
    /// 
    /// </summary>
    public class SimConnectImplementer
    {
        SimConnect simconnect = null;
        public Boolean bSimConnected = false; //if connected is true
        int counterIDevents = 0;
        int counterIDreqs = 0;
        int counterIDevents_max = 0;
        public List<SimVarList> simvarlist;
        public List<SimVarList> simeventlist;
        public IntPtr wih;


        // User-defined win32 event
        public const int WM_USER_SIMCONNECT = 0x0402;
        public SimConnectImplementer()
        {
            // Empty for stuff to do

        }

        public struct StructSimVar
        {
            public double var;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct StructSimVarString
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String var_string;
        }

        /// <summary>
        /// This class is used to create a list with requested variables
        /// </summary>
        public class SimVarList
        {
            public SimVarList(string key, int id, string format)
            {
                Key = key;
                ID = id;
                Format = format;
            }
            public string Key;
            public int ID;
            public string Format;
        }

        /// <summary>
        ///Empty enums for typecasting
        /// </summary>
        enum EVENTS 
        {
            KEY_C_DN = 0,
            KEY_C_UP = 1,

        };

        enum GROUPID
        {
            GROUP0 = 0,
        }

        enum INPUTID
        {
            INPUT0 = 0,
        }



        enum SIMVARDEF { };
        enum SIMVARREQ { };


        /// <summary>
        /// Standard definition 'which seems to work' - undocumented for Simconnect
        /// </summary>
        enum GROUP
        {
            ID_PRIORITY_STANDARD = 1900000000,
            ID_PRIORITY_HIGHEST = 1,
        };



        public IntPtr ProcessSimConnectWin32Events(IntPtr Wih, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            if (msg == WM_USER_SIMCONNECT)
            {
                ReceiveSimConnectMessage();
                handled = true;

            }
            return (IntPtr)0;
        }

        /// <summary>
        /// Defines what to do when message is received
        /// </summary>
        private void ReceiveSimConnectMessage()
        {
            try
            {
                simconnect?.ReceiveMessage();
            }
            catch (Exception ex)
            {
                Logger.Log($"ReceiveSimConnectMessage Error: {ex.Message}");
                Disconnect();
            }
        }


        /// <summary>
        /// Call function to start connection to simconnect
        /// </summary>
        public async void Connect()
        {
            if (simconnect != null)
                return;

            Window simconnectWindow = new Window();
            var helper = new WindowInteropHelper(simconnectWindow);
            helper.EnsureHandle();
            wih = new WindowInteropHelper(simconnectWindow).Handle;
            HwndSource hs = HwndSource.FromHwnd(wih);
            hs.AddHook(ProcessSimConnectWin32Events);

            if (bSimConnected)
            {
                Logger.Log("Already connected");
                return;
            }

            try
            {
                simconnect = await Task.Run(() => new SimConnect("SimConnectorVoiceRecog", wih, WM_USER_SIMCONNECT, null, 0));

                // Listen for connect and quit msgs
                simconnect.OnRecvOpen += SimConnect_OnRecvOpen;
                simconnect.OnRecvQuit += SimConnect_OnRecvQuit;

                // Listen for Exceptions
                simconnect.OnRecvException += SimConnect_OnRecvException;

                // Listen for SimVar Data
                simconnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;

                // create object to register requested simvars
                simvarlist = new List<SimVarList>();
                simeventlist = new List<SimVarList>();



                Logger.Log("Connected");
            }
            catch (COMException ex)
            {
                simconnect = null;
                bSimConnected = false;
                Logger.Log($"Connection error is your simulator running?");
                return;
            }
        }

        /// <summary>
        /// Call function to disconnect from Simconnect
        /// </summary>
        public void Disconnect()
        {
            if (!bSimConnected)
            {
                Logger.Log("Already disconnected");
                return;
            }

            // Raise event to notify client we've disconnected and get rid of client
            simconnect?.Dispose(); // May have already been disposed or not even been created, e.g. Disconnect called before Connect
            simconnect = null;
            bSimConnected = false;
            Logger.Log("Disconnected");
        }

        /// <summary>
        /// This functions sends some logdata when Simconnect connection is established
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            bSimConnected = true;
            Logger.Log($"VoiceRecognition succesfully connected to MSFS using SimConnect");



           // simconnect.MapInputEventToClientEvent(
           //        INPUTID.INPUT0,
           //        "a",               // Virtuele keycode
           //        EVENTS.KEY_C_DN,    // Down event
           //        0,
           //        EVENTS.KEY_C_UP,      // Up event
           //        1,
           //        false
           //    );

            //LogResult?.Invoke(this, $"- Application name: {data.szApplicationName}");
            //LogResult?.Invoke(this, $"- Application Version {data.dwApplicationVersionMajor}.{data.dwApplicationVersionMinor} - build {data.dwApplicationBuildMajor}.{data.dwApplicationBuildMinor}");
            //LogResult?.Invoke(this, $"- SimConnect  Version {data.dwSimConnectVersionMajor}.{data.dwSimConnectVersionMinor} - build {data.dwSimConnectBuildMajor}.{data.dwSimConnectBuildMinor}");
        }

        /// <summary>
        /// This functions is called when connection to Simconnect is ended from Simconnect side
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Disconnect();
            bSimConnected = false;
        }



        private static void Simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            if ((EVENTS)data.uEventID == EVENTS.KEY_C_UP)
            {
                Logger.Log("C is ingedrukt in de simulator! 🎉");
                // Hier kun je je eigen actie uitvoeren, bv. gear up, logging, externe hardware aansturen etc.
            }
        }

        /// <summary>
        ///  Deal with exceptions received from Simconnect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            foreach (var property in data.GetType().GetFields())
            {
                Logger.Log($"Exception (on exception): {property.Name} {property.GetValue(data)}");
            }

        }

        /// <summary>
        /// Call function to set Simconnect Event
        /// </summary>
        /// <param name="simconnect_event_key"> event to set</param>
        /// <param name="simconnect_event_dataset"> data value to set</param>
        public void SendEvent(string simconnect_event_key, int simconnect_event_dataset)
        {
            if (bSimConnected)
            {
                if (simeventlist.FindIndex(item => item.Key == simconnect_event_key) == -1)
                {
                    string var_type = "float";
                    counterIDevents_max += 1;
                    counterIDevents = counterIDevents_max;
                    simconnect.MapClientEventToSimEvent((EVENTS)counterIDevents, simconnect_event_key);
                    simeventlist.Add(new SimVarList(simconnect_event_key, counterIDevents, var_type));
                }
                else
                {
                    int index = simeventlist.FindIndex(item => item.Key == simconnect_event_key);
                    counterIDevents = simeventlist[index].ID;
                }
                
                try
                {

                    simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (EVENTS)counterIDevents, (uint)simconnect_event_dataset, GROUP.ID_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    Thread.Sleep(10);
                    //LogResult?.Invoke(this, $"Event send, ID: {counterIDevents} and event: {simconnect_event_key}, with set value: {simconnect_event_dataset}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"SetSimVar Error: {ex.Message}");
                }
            }
            
        }


        public int RegisterSimVar(string simvar, string unit, string var_type, string frequency)
        {
            if (bSimConnected)
            {
                // Check if variable is not already requested
                if (simvarlist.FindIndex(item => item.Key == simvar) == -1)
                {
                    // Submit the SimVar request to SimConnect
                    if (var_type == "float")
                    {
                        try
                        {
                            counterIDreqs += 1;
                            // add one or more SimVar names and units to a client defined object definition
                            simconnect.AddToDataDefinition((SIMVARDEF)counterIDreqs, simvar, unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                            // tell SimConnect what type of value we are expecting to be returned
                            simconnect.RegisterDataDefineStruct<StructSimVar>((SIMVARDEF)counterIDreqs); // We'll presume default values being requested are numeric

                            // request when the SimConnect client is to receive data values for a specific object
                            simconnect.RequestDataOnSimObject((SIMVARREQ)counterIDreqs, (SIMVARDEF)counterIDreqs, 0, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                            //LogResult?.Invoke(this, "Registered!");

                            simvarlist.Add(new SimVarList(simvar, counterIDreqs, var_type));
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"RegisterSimVar Error: {ex.Message}");
                            SimConnect_OnRecvException(simconnect, new SIMCONNECT_RECV_EXCEPTION { dwException = (uint)ex.HResult });
                            return -1;
                        }
                    }
                    if (var_type == "string")
                    {
                        try
                        {
                            counterIDreqs += 1;
                            // add one or more SimVar names and units to a client defined object definition
                            simconnect.AddToDataDefinition((SIMVARDEF)counterIDreqs, simvar, null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);

                            // tell SimConnect what type of value we are expecting to be returned
                            simconnect.RegisterDataDefineStruct<StructSimVarString>((SIMVARDEF)counterIDreqs); // We'll presume default values being requested are numeric

                            // request when the SimConnect client is to receive data values for a specific object
                            simconnect.RequestDataOnSimObject((SIMVARREQ)counterIDreqs, (SIMVARDEF)counterIDreqs, 0, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                            Logger.Log("String registered!");

                            simvarlist.Add(new SimVarList(simvar, counterIDreqs, var_type));
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"RegisterSimVar Error: {ex.Message}");
                            SimConnect_OnRecvException(simconnect, new SIMCONNECT_RECV_EXCEPTION { dwException = (uint)ex.HResult });
                            return -1;
                        }
                    }
                    return 0;
                }
                else
                {
                    Logger.Log($"Simvar {simvar} is already requested");
                }
            }
            return -1;
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            var reqid = data.dwRequestID;
            int index = simvarlist.FindIndex(item => item.ID == reqid);
            string type = simvarlist[index].Format;
            string var = simvarlist[index].Key;

            bSimConnected = true;

            if (type == "float")
            {
                StructSimVar result = (StructSimVar)data.dwData[0];
                Logger.Log($"{var} |{result.var:F3}| ReqID: {reqid}");
            }
            if (type == "string")
            {
                StructSimVarString result = (StructSimVarString)data.dwData[0];
                Logger.Log($"{var} |{result.var_string}| ReqID: {reqid}");
            }
        }


        /// <summary>
        /// Sets value for specified SimData
        /// </summary>
        /// <param name="simvar"></param>
        /// <param name="unit"></param>
        /// <param name="var_type"></param>
        /// <param name="frequency"></param>
        /// <param name="value"></param>
        public void SetDataOnObject(string simvar, string unit, string var_type, string frequency, string value)
        {
            if (simvarlist.FindIndex(item => item.Key == simvar) == -1)
            {
                int reg_succes = RegisterSimVar(simvar, unit, var_type, frequency);
            }
            int index = simvarlist.FindIndex(item => item.Key == simvar);
            int req_id = simvarlist[index].ID;

            StructSimVar s1 = new StructSimVar();
            s1.var = float.Parse(value);

            try
            {
                simconnect.SetDataOnSimObject((SIMVARREQ)req_id, 0, SIMCONNECT_DATA_SET_FLAG.DEFAULT, s1);
                Logger.Log($"Set! {req_id} {value}");
            }
            catch (Exception ex)
            {
                Logger.Log($"SetSimVar Error: {ex.Message} {counterIDreqs} {value}");
            }
        }


    }
}


