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
        private bool isRecognitionRunning = true; // Track whether recognition is currently running
        private SimConnectImplementer _simconnect;

        private readonly List<VoiceCommand> _voiceCommands = new();
        private bool _copilotactive = true;

        private bool _subscribedToSimData = false;

        public MainWindow()
        {
            InitializeComponent();

            

            //reading voice commands
            string configfile = "voice_commands.yml";
            ReadMapFile(configfile);



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
            commands.Add(voiceCommands.Select(c => c.Phrase).ToArray());
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
            isRecognitionRunning = !isRecognitionRunning;
            copilotactive = true;
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
            _simconnect = new SimConnectImplementer();
            _simconnect.LogResult += OnAddResult;

            //Debug.WriteLine($"Simconnect started");
            bool localbSimConnected = false;

            while (true)
            {
                //Debug.Write($"Start loop");
                Thread.Sleep(1000);
                localbSimConnected = _simconnect.bSimConnected;
                if (!localbSimConnected)
                {
                    subscribedToSimData = false;
                    //Debug.WriteLine($"Start inner if ");
                    Thread.Sleep(100);
                    this.Dispatcher.Invoke(() =>
                    {
                        //Debug.WriteLine($"Just before simconnect call");
                        _simconnect.Connect();
                        Thread.Sleep(1000);
                        localbSimConnected = _simconnect.bSimConnected;
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
                        _copilotactive = true;
                        RecogStatus.Fill = Brushes.Green;
                        TextBox1.AppendText("Your co-pilot is active!\r\n");
                        TextBox1.ScrollToEnd();
                        return;
                    case "DisableRecognition":
                        _copilotactive = false;
                        RecogStatus.Fill = Brushes.Red;
                        TextBox1.AppendText("Your co-pilot has a break!\r\n");
                        TextBox1.ScrollToEnd();
                        return;
                }
            }

            if (_copilotactive && !string.IsNullOrEmpty(command.Event))
            {
                TextBox1.AppendText(command.Phrase + "\r\n");
                _simconnect.SendEvent(command.Event, 1);

                TextBox1.AppendText(command.Event + " sent to sim!\r\n");
                TextBox1.ScrollToEnd();
            }
        }
       

        private void Quit_Click(object sender, RoutedEventArgs e)
        {

            _simconnect.Disconnect();
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