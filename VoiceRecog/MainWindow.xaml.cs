using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Speech.Recognition;
using System.Windows.Media;
using VoiceRecog.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoiceRecog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechRecognitionService _speechRecognition;
        private SimConnectImplementer _simConnect;

        private readonly List<VoiceCommand> _voiceCommands = new();
        private bool _copilotActive = true;

        private bool _subscribedToSimData = false;


        public MainWindow()
        {
            InitializeComponent();

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

                Log($"Loaded {_voiceCommands.Count} voice commands.");
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
            Thread connectSimConnect = new Thread(ConnectSimConnect);
            connectSimConnect.IsBackground = true;
            connectSimConnect.Start();

            Debug.WriteLine("Window Loaded");
        }

        public void ConnectSimConnect()
        {
            _simConnect = new SimConnectImplementer();
            _simConnect.LogResult += OnAddResult;

            //Debug.WriteLine($"Simconnect started");
            bool localbSimConnected = false;

            while (true)
            {
                //Debug.Write($"Start loop");
                Thread.Sleep(1000);
                localbSimConnected = _simConnect.bSimConnected;
                if (!localbSimConnected)
                {
                    _subscribedToSimData = false;
                    //Debug.WriteLine($"Start inner if ");
                    Thread.Sleep(100);
                    this.Dispatcher.Invoke(() =>
                    {
                        //Debug.WriteLine($"Just before simconnect call");
                        _simConnect.Connect();
                        Thread.Sleep(1000);
                        localbSimConnected = _simConnect.bSimConnected;
                        Debug.WriteLine($"Simconnect status loop: {localbSimConnected}");
                        if (localbSimConnected)
                        {
                            SimconnectStatus.Fill = Brushes.Green;
                        }
                        else
                        {
                            SimconnectStatus.Fill = Brushes.Red;
                            TextBox1.Text += "Looking for simulator...\r\n";
                            Thread.Sleep(200);
                        }
                    });
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        SimconnectStatus.Fill = Brushes.Green;
                    });
                    if (!_subscribedToSimData)
                    {
                        _subscribedToSimData = true;
                        Debug.WriteLine($"SimConnect Data registred.");

                    }

                }
            }
        }


        private void OnAddResult(object sender, string sResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!sResult.Contains("|"))
                {
                    Log(sResult);
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
                        Log("Your co-pilot is active!");
                        return;
                    case "DisableRecognition":
                        _copilotActive = false;
                        RecogStatus.Fill = Brushes.Red;
                        Log("Your co-pilot has a break!");
                        return;
                }
            }

            if (_copilotActive && !string.IsNullOrEmpty(command.Event))
            {
                _simConnect.SendEvent(command.Event, 1);
                Log(command.Phrase + " sent: " + command.Event);
            }
        }
       

        private void Quit_Click(object sender, RoutedEventArgs e)
        {

            _simConnect.Disconnect();
            Environment.Exit(0);
            System.Windows.Application.Current.Shutdown();
        }

        private void Log(string message)
        {
            TextBox1.AppendText(message + Environment.NewLine);
            TextBox1.ScrollToEnd();
            Debug.WriteLine(message);
        }
    }
}