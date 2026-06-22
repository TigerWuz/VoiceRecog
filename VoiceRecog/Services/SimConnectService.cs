using Microsoft.FlightSimulator.SimConnect;
using System.CodeDom;
using System.Runtime.InteropServices;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace VoiceRecog.Services
{
    public class SimConnectService
    {
        public const int      WM_USER_SIMCONNECT = 0x0404;
        private const string    _MSFS_PROCESS_NAME = "FlightSimulator";
        private const string    _PLUGIN_NAME = "Voice Recognition";

        private IntPtr _wndHandle = new IntPtr(0);

        private SimConnect _simConnect;
        private SimVars _simVars;

        public bool IsConnected { get; private set; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct SimVars
        {
            public bool taxi_Light;
            public bool light_landing;     
        }

        private enum RequestType
        {
            PerFrameData,
        }

        public void ReceiveMessage() 
        {
            try
            {
                _simConnect.ReceiveMessage();
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception in {nameof(ReceiveMessage)}: {ex.Message}");
            }
        }

        public SimConnectService(IntPtr _hWnd) 
        {
            _wndHandle = _hWnd;
        }

        //connect to sim
        public void Connect() 
        {
            if (IsConnected)
                return;

            try
            {
                _simConnect = new SimConnect("Voice Recognition",
                                       _wndHandle,
                                       WM_USER_SIMCONNECT,
                                       null,
                                       0);
                _simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(OnConnect);
                _simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(OnDisconnect);
                _simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(OnReceiveException);
                _simConnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(OnReceiveSimData);

                IsConnected = true;
            }
            catch 
            {
                IsConnected = false;
            }
        }

        //register simvars to monitor
        private void OnConnect(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {

            _simConnect.AddToDataDefinition(RequestType.PerFrameData, "LIGHT TAXI", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
            _simConnect.AddToDataDefinition(RequestType.PerFrameData, "LIGHT LANDING", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);

            _simConnect.RegisterDataDefineStruct<SimVars>(RequestType.PerFrameData);
            _simConnect.RequestDataOnSimObject(RequestType.PerFrameData,
                                    RequestType.PerFrameData,
                                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                                    SIMCONNECT_PERIOD.SIM_FRAME,
                                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                                    0, 0, 0);

            Logger.Log($"Connected to Flight Sim.");
        }



        //disconnect to sim
        private void OnDisconnect(SimConnect sender, SIMCONNECT_RECV data)
        {
            IsConnected = false;
        }

        //do something when data is received
        private void OnReceiveSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            switch((RequestType)data.dwRequestID)
            {
                case RequestType.PerFrameData:
                    _simVars = (SimVars)data.dwData[0];
                    Logger.Log($"Received Data from Sim: {_simVars.taxi_Light}, {_simVars.light_landing}");
                    break;
                default:
                    Logger.Log($"Unsupported Request Type: {data.dwRequestID}");
                    break;
            }
        }

        private void OnReceiveException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION ex)
        {
            Logger.Log($"Sim Exception Received: {ex}");
        }
        
    }
}
