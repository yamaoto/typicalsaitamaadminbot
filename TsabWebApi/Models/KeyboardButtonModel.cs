using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class KeyboardButtonModel
    {
        public KeyboardButtonModel()
        {

        }
        public KeyboardButtonModel(string text)
        {
            Text = text;
        }
        [DataMember(Name = "text")]
        public string Text { get; set; }
        [DataMember(Name="request_contact")]
        public bool RequestContact { get; set; }
        [DataMember(Name="request_location")]
        public bool RequestLocation { get; set; }
    }
}