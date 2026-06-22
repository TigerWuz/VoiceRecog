using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;

namespace VoiceRecog.Services
{
    public class SimConnectService
    {
        public const int        WM_USER_SIMCONNECT = 0x0404;
        private const string    _PLUGIN_NAME = "Voice Recognition";

        private IntPtr _wndHandle = new IntPtr(0);

        private SimConnect _simConnect;
        private readonly List<string> _simEvents;
        private readonly Dictionary<string, ClientEventId> _registeredEvents = new();

        public bool IsConnected { get; private set; }

        public enum ClientEventId
        {
            None = 0
        }

        private enum GROUP
        {
            ID_PRIORITY_HIGHEST = 1
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

        public SimConnectService(IntPtr _hWnd, IEnumerable<string> simEvents ) 
        {
            _wndHandle = _hWnd;
            _simEvents = simEvents.ToList();
        }

        //connect to sim
        public void Connect() 
        {
            if (IsConnected)
                return;

            try
            {
                _simConnect = new SimConnect(_PLUGIN_NAME,
                                       _wndHandle,
                                       WM_USER_SIMCONNECT,
                                       null,
                                       0);
                _simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(OnConnect);
                _simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(OnDisconnect);
                _simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(OnReceiveException);

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
            RegisterEvents();
            Logger.Log($"Connected to Flight Sim.");
        }

        private void RegisterEvents()
        {
            uint eventId = 0;
            foreach(string simEvent in _simEvents) 
            { 
                try
                {
                    _simConnect.MapClientEventToSimEvent((ClientEventId)eventId, simEvent);
                    _registeredEvents.Add(simEvent, (ClientEventId)eventId);
                    Logger.Log($"Registered Event {eventId}: {simEvent}");
                    eventId++;
                }
                catch(Exception ex)
                {
                    Logger.Log($"Failed to register '{simEvent}': {ex.Message}");
                }
            }
        }

        public void SendEvent(string simEvent)
        {
            if (!_registeredEvents.TryGetValue(simEvent, out var eventId))
                return;

            _simConnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER,
                                eventId,
                                0,
                                GROUP.ID_PRIORITY_HIGHEST,
                                SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        //disconnect to sim
        private void OnDisconnect(SimConnect sender, SIMCONNECT_RECV data)
        {
            IsConnected = false;
        }
        private void OnReceiveException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION ex)
        {
            Logger.Log($"Sim Exception Received: {ex}");
        }
        
    }
}
