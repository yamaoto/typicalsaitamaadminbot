using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ReplyKeyboardMarkupModel
    {
        [DataMember(Name = "keyboard")]
        public KeyboardButtonModel[][] Keyboard { get; set; }
        [DataMember(Name="resize_keyboard")]
        public bool ResizeKeyboard { get; set; }
        [DataMember(Name="one_time_keyboard")]
        public bool OneTimeKeyboard { get; set; }
        [DataMember(Name = "selective")]
        public bool Selective { get; set; }
    }
}