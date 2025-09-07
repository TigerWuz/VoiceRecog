using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YamlDotNet.RepresentationModel;

namespace VoiceRecog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechRecognitionEngine recognizer;
        private bool isRecognitionRunning = true; // Track whether recognition is currently running
        public SimConnectImplementer mysimconnect;
        public int jmax = 0;

        //string[] voice_commands = { "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null" };
        string[] voice_commands = { "null", "null" };
        string[] events = { "null", "null"};
        string[] controls = { "copilot on", "copilot off" };
        public bool copilotactive;

        public bool subscribedToSimData = false;
        public bool voiceRecogActive = true;

        public MainWindow()
        {
            InitializeComponent();

            

            //reading voice commands
            string configfile = "voice_commands.yml";
            readMapFile(configfile);



            // Create a Choices object containing a list of choices.
            CultureInfo culture = new CultureInfo("en-US");

            Choices commands = new Choices();
            commands.Add(voice_commands);
            commands.Add(controls);


            // Create a GrammarBuilder object and append the Choices object.
            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(commands);
            gb.Culture = culture;

            // Create the Grammar instance and load it into the recognizer.
            Grammar grammar = new Grammar(gb);

            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
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
            mysimconnect = new SimConnectImplementer();
            mysimconnect.LogResult += OnAddResult;

            //Debug.WriteLine($"Simconnect started");
            bool localbSimConnected = false;
            bool title_registred = false;
            while (true)
            {
                //Debug.Write($"Start loop");
                Thread.Sleep(1000);
                localbSimConnected = mysimconnect.bSimConnected;
                if (localbSimConnected == false)
                {
                    subscribedToSimData = false;
                    //Debug.WriteLine($"Start inner if ");
                    Thread.Sleep(1000);
                    this.Dispatcher.Invoke(() =>
                    {
                        //Debug.WriteLine($"Just before simconnect call");
                        mysimconnect.Connect();
                        Thread.Sleep(200);
                        localbSimConnected = mysimconnect.bSimConnected;
                        Debug.WriteLine($"Simconnect status loop: {localbSimConnected}");
                        if (localbSimConnected)
                        {
                            SimconnectStatus.Fill = Brushes.Green;
                            //int i = mysimconnect.RegisterSimVar("TITLE", "unit", "string", "NOT_USED");
                            //title_registred = true;
                            //Debug.WriteLine($"Title registred {i}");

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
                    if (!subscribedToSimData)
                    {
                        subscribeToSimConnectData();
                        subscribedToSimData = true;
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


        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (isRecognitionRunning)
            {
                recognizer.RecognizeAsyncCancel();
                Button1.Content = "Continue";
            }
            else
            {
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
                Button1.Content = "Pause";
            }

            isRecognitionRunning = !isRecognitionRunning; // Toggle the recognition state

        }

        private void VoiceRecognizer(object sender, SpeechRecognizedEventArgs e)
        {
            string text_found = e.Result.Text;
            Debug.WriteLine(e);
            if (text_found != "null");
            {
                
                for (int i = 0; i < jmax; i++)
                {
                    //Debug.WriteLine(voice_commands[i]);
                    if ((text_found == voice_commands[i]) && copilotactive)
                    {
                        TextBox1.Text += e.Result.Text + "\r\n";
                        mysimconnect.SendEvent(events[i], 1);
                        Debug.WriteLine(events[i], "sent to sim!");
                        TextBox1.AppendText(events[i] + " sent to sim!" + "\r\n");
                        TextBox1.ScrollToEnd();
                        break;
                    }
                    if (text_found == "copilot on")
                    {
                        copilotactive = true;
                        TextBox1.AppendText("Your co-pilot is active!\r\n");
                        TextBox1.ScrollToEnd();
                        RecogStatus.Fill = Brushes.Green;
                        break;
                    }
                    if (text_found == "copilot off")
                    {
                        copilotactive = false;
                        TextBox1.AppendText("Your co-pilot has a break!\r\n");
                        TextBox1.ScrollToEnd();
                        RecogStatus.Fill = Brushes.Red;
                        break;
                    }

                }
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {

            mysimconnect.Disconnect();
            Environment.Exit(0);
            System.Windows.Application.Current.Shutdown();
        }



        public void subscribeToSimConnectData()
        {
            //mysimconnect.RegisterSimVar("Title", "unit", "string", "NOT_USED");
            //mysimconnect.RegisterSimVar("TURB ENG AFTERBURNER:4", "Bool", "float", "NOT_USED");
        }

        public void readMapFile(string filepath)
        {
            String line;
            int j = 0;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                var input = new StreamReader(filepath);
                //Read the first line of text
                line = input.ReadLine();

                //Continue to read until you reach end of file
                while (line != null)
                {
                    Debug.WriteLine(line);
                    if (line.Contains("#"))
                    {
                        Debug.WriteLine("Line skipped");
                    }
                    else
                    {
                        Debug.WriteLine("Line NOT skipped");
                        if (j > 1) 
                        {
                            Debug.WriteLine(j);
                            Array.Resize(ref voice_commands, j+1); 
                            Array.Resize(ref events, j+1);
                        }
                        voice_commands[j] = line.Split(": ")[0];
                        events[j] = line.Split(": ")[1];
                        j++;
                        jmax++;
                    }
                    //Read the next line
                    line = input.ReadLine();
                }
                //close the file
                Debug.WriteLine(voice_commands[5]); Debug.WriteLine(events[5]);
                input.Close();
                TextBox1.Text = "Input file correctly read \r\n";
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Debug.WriteLine("Executing finally block.");
            }


        


        }


    }
}