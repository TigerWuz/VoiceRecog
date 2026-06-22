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

namespace VoiceRecog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechRecognitionService _speechRecognition;
        private SimConnectService _simConnectService;
        //private SimConnectImplementer _simConnect;
        
        private readonly List<VoiceCommand> _voiceCommands = new();
        private bool _copilotActive = true;

        private bool _subscribedToSimData = false;

        private IntPtr _wndHandle;

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
                MessageBox.Show(
                    $"Could not load voice commands.\n\n{ex.Message}");
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
            //Thread connectSimConnect = new Thread(ConnectSimConnect);
            //connectSimConnect.IsBackground = true;
            //connectSimConnect.Start();
            ConnectSimConnect();
            Logger.Log("Window Loaded");
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.MessageLogged -= Log;
        }

        public void ConnectSimConnect()
        {
            _simConnectService = new SimConnectService(_wndHandle);

            //Debug.WriteLine($"Simconnect started");
            bool localbSimConnected = false;

            while (true)
            {
                //Debug.Write($"Start loop");
                //Thread.Sleep(1000);
                localbSimConnected = _simConnectService._simConnected;
                if (!localbSimConnected)
                {
                    //Thread.Sleep(100);
                    //this.Dispatcher.Invoke(() =>
                    //{
                        //Debug.WriteLine($"Just before simconnect call");
                        _simConnectService.Connect();
                        //Thread.Sleep(1000);
                        localbSimConnected = _simConnectService._simConnected;
                        Logger.Log( $"Simconnect status loop: {localbSimConnected}");
                        if (localbSimConnected)
                        {
                            SimconnectStatus.Fill = Brushes.Green;
                            break;
                        }
                        else
                        {
                            SimconnectStatus.Fill = Brushes.Red;
                            Logger.Log("Looking for simulator...");
                            //Thread.Sleep(200);
                        }
                    //});
                }
                //else
                //{
                //    this.Dispatcher.Invoke(() =>
                //    {
                //        SimconnectStatus.Fill = Brushes.Green;
                //    });
                //    if (!_subscribedToSimData)
                //    {
                //        Logger.Log("SimConnect Data registred.");
                //    }

                //}
            }
            HwndSource source = HwndSource.FromHwnd(_wndHandle);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            const int WM_USER_SIMCONNECT = 0x0404;
            if (msg == WM_USER_SIMCONNECT)
            {
                _simConnectService.ReceiveMessage();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnAddResult(object sender, string sResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!sResult.Contains("|"))
                {
                    Logger.Log(sResult);
                }

            });
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
                        Logger.Log("Your co-pilot is active!");
                        return;
                    case "DisableRecognition":
                        _copilotActive = false;
                        RecogStatus.Fill = Brushes.Red;
                        Logger.Log("Your co-pilot has a break!");
                        return;
                }
            }

            if (_copilotActive && !string.IsNullOrEmpty(command.Event))
            {
                //_simConnect.SendEvent(command.Event, 1);
                Logger.Log(command.Phrase + " sent: " + command.Event);
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