using System.Diagnostics;
using System.IO;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoiceRecog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechRecognitionEngine recognizer;
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
            ReadMapFile("voice_commands.yml");
        }

        private void InitializeSpeechRecognition()
        {
            // Create a Choices object containing a list of choices.
            var recognizers = SpeechRecognitionEngine.InstalledRecognizers();

            if (!recognizers.Any())
            {
                MessageBox.Show("No Speech recognizer installed.");
                return;
            }

            RecognizerInfo recognizerInfo = recognizers.First();

            Debug.WriteLine($"Using Recognizer: {recognizerInfo.Name}");
            Debug.WriteLine($"Language: {recognizerInfo.Culture}");

            Choices commands = new Choices();
            commands.Add(_voiceCommands.Select(c => c.Phrase).ToArray());
            //commands.Add(controls);

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(commands);
            gb.Culture = recognizerInfo.Culture;

            Grammar grammar = new Grammar(gb);

            recognizer = new SpeechRecognitionEngine(recognizerInfo);

            recognizer.SetInputToDefaultAudioDevice();

            //Grammar grammar = new DictationGrammar(); //this is the default grammar

            recognizer.LoadGrammar(grammar);
            recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(VoiceRecognizer);
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
            _copilotActive = true;
            RecogStatus.Fill = Brushes.Green;
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
                    TextBox1.AppendText(sResult + "\r\n");
                    TextBox1.ScrollToEnd();
                }
                //Debug.WriteLine(sResult);

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
                        TextBox1.AppendText("Your co-pilot is active!\r\n");
                        TextBox1.ScrollToEnd();
                        return;
                    case "DisableRecognition":
                        _copilotActive = false;
                        RecogStatus.Fill = Brushes.Red;
                        TextBox1.AppendText("Your co-pilot has a break!\r\n");
                        TextBox1.ScrollToEnd();
                        return;
                }
            }

            if (_copilotActive && !string.IsNullOrEmpty(command.Event))
            {
                TextBox1.AppendText(command.Phrase + "\r\n");
                _simConnect.SendEvent(command.Event, 1);

                TextBox1.AppendText(command.Event + " sent to sim!\r\n");
                TextBox1.ScrollToEnd();
            }
        }
       

        private void Quit_Click(object sender, RoutedEventArgs e)
        {

            _simConnect.Disconnect();
            Environment.Exit(0);
            System.Windows.Application.Current.Shutdown();
        }


        public void ReadMapFile(string filepath)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                using var reader = new StreamReader(filepath);

                var config = deserializer.Deserialize<VoiceCommandConfig>(reader);
                _voiceCommands.Clear();

                if (config?.Commands != null)
                {
                    _voiceCommands.AddRange(config.Commands);
                }

                TextBox1.AppendText($"Loaded {_voiceCommands.Count} voice commands.\r\n");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
                MessageBox.Show($"Could not load voice commands from {filepath}\n\n{e.Message}");
            }
            
        }
    }
}