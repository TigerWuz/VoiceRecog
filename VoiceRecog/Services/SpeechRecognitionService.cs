using System.Speech.Recognition;

namespace VoiceRecog.Services;

public class SpeechRecognitionService
{
    private readonly SpeechRecognitionEngine _recognizer;

    public event EventHandler<SpeechRecognizedEventArgs>? CommandRecognized;

    public SpeechRecognitionService(IEnumerable<VoiceCommand> commands)
    {
        var recognizers = SpeechRecognitionEngine.InstalledRecognizers();

        if (!recognizers.Any())
            throw new InvalidOperationException("No speech recognizer installed.");

        var recognizerInfo = recognizers.First();

        Choices choices = new();
        choices.Add(commands.Select(c => c.Phrase).ToArray());

        GrammarBuilder builder = new();
        builder.Culture = recognizerInfo.Culture;
        builder.Append(choices);

        Grammar grammar = new(builder);

        _recognizer = new SpeechRecognitionEngine(recognizerInfo);

        _recognizer.SetInputToDefaultAudioDevice();
        _recognizer.LoadGrammar(grammar);

        _recognizer.SpeechRecognized += (s, e) =>
        {
            CommandRecognized?.Invoke(this, e);
        };
    }

    public void Start()
    {
        Logger.Log("Speech recognition started.");
        _recognizer.RecognizeAsync(RecognizeMode.Multiple);
    }

    public void Stop()
    {
        Logger.Log("Speech recognition stopped.");
        _recognizer.RecognizeAsyncStop();
    }
}
