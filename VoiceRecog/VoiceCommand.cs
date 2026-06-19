namespace VoiceRecog
{
	public class VoiceCommand
    {
        public string Phrase { get; set; } = "";
        public string Event { get; set; } = "";

        //optional
        public string Action { get; set; } = "";
    }
}

//public class VoiceCommand
//{
//    public string Phrase { get; set; } = "";

//    public string Event { get; set; } = "";

//    public int Value { get; set; } = 1;

//    public bool Enabled { get; set; } = true;

//    public string Description { get; set; } = "";
//}