using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Speech.Recognition;
using System.Windows.Media;
using VoiceRecog.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Windows.Interop;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace VoiceRecog
{
    public partial class MainWindow : Window
    {
        private SpeechRecognitionService _speechRecognition;
        private SimConnectService? _simConnectService;
        private DispatcherTimer? _connectTimer;
        private IntPtr _wndHandle;

        private readonly List<VoiceCommand> _voiceCommands = new();

        private bool _copilotActive = true;
       

        public MainWindow()
        {
            InitializeComponent();
            Logger.MessageLogged += Log;

            LoadVoiceCommands();
            InitializeSpeechRecognition();
        }

        private void LoadVoiceCommands()
        {
            try
            {
                _voiceCommands.Clear();
                _voiceCommands.AddRange(
                    VoiceCommandLoader.Load("voice_commands.yml"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load voice commands.\n\n{ex.Message}");
            }
        }

        private void InitializeSpeechRecognition()
        {
            try
            {
                _speechRecognition = new SpeechRecognitionService(_voiceCommands);

                _speechRecognition.CommandRecognized += VoiceRecognizer;

                _speechRecognition.Start();

                _copilotActive = true;
                RecogStatus.Fill = Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _wndHandle = new WindowInteropHelper(this).Handle;

            HwndSource source = HwndSource.FromHwnd(_wndHandle);
            source.AddHook(WndProc);

            var simEvents = _voiceCommands
                .Where(c => !string.IsNullOrWhiteSpace(c.Event))
                .Select(c => c.Event)
                .Distinct()
                .ToList();
            _simConnectService = new SimConnectService(_wndHandle, simEvents);
            _simConnectService.ConnectionStateChanged += ConnectionStateChanged;
            StartConnectTimer();
            Logger.Log("Window Loaded");
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.MessageLogged -= Log;
        }

        //This function tries to let the SimConnectService connect to the Sim
        //then it is stopped
        private void StartConnectTimer()
        {
            _connectTimer = new DispatcherTimer();
            _connectTimer.Interval = TimeSpan.FromSeconds(2);
            _connectTimer.Tick += ConnectTimer_Tick;
            _connectTimer.Start();
        }

        private void ConnectTimer_Tick(object? sender, EventArgs e)
        {
            if (_simConnectService == null)
                return;

            if (_simConnectService.IsConnected)
            {
                _connectTimer?.Stop();
                return;
            }
                
            Logger.Log($"Trying to connect...");
            _simConnectService.Connect();
        }

        private void ConnectionStateChanged(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                SimconnectStatus.Fill = connected
                    ? Brushes.Green
                    : Brushes.Red;

                if (!connected)
                    _connectTimer?.Start();
            });
        }

        private IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == SimConnectService.WM_USER_SIMCONNECT)
            {
                _simConnectService?.ReceiveMessage();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void Settings(object sender, RoutedEventArgs e)
        {
            //curerntly empty
        }

        private void VoiceRecognizer(object sender, SpeechRecognizedEventArgs e)
        {
            string recognizedText = e.Result.Text;
            Debug.WriteLine(e);
            
            var command = _voiceCommands.FirstOrDefault(c => c.Phrase == recognizedText);

            if (command == null)
                return;

            //internal reaction for the tool not propagated to simulation
            if (!string.IsNullOrEmpty(command.Action))
            { 
                switch (command.Action)
                {
                    case "EnableRecognition":
                        _copilotActive = true;
                        RecogStatus.Fill = Brushes.Green;
                        Logger.Log($"Your co-pilot is active! | Confidence: {e.Result.Confidence:P1}");
                        return;
                    case "DisableRecognition":
                        _copilotActive = false;
                        RecogStatus.Fill = Brushes.Red;
                        Logger.Log($"Your co-pilot has a break! | Confidence: {e.Result.Confidence:P1}");
                        return;
                }
            }

            if (_copilotActive && !string.IsNullOrEmpty(command.Event))
            {
                _simConnectService?.SendEvent(command.Event);
                Logger.Log($"{command.Phrase} sent: {command.Event} | Confidence: {e.Result.Confidence:P1}");
            }
        }
       

        private void Quit_Click(object sender, RoutedEventArgs e)
        {

            //_simConnectService.Disconnect();
            Environment.Exit(0);
            System.Windows.Application.Current.Shutdown();
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TextBox1.AppendText(message + Environment.NewLine);
                TextBox1.ScrollToEnd();
            });
        }
    }
}