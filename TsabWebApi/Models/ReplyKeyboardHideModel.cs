using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ReplyKeyboardHideModel
    {
        [DataMember(Name="hide_keyboard")]
        public bool HideKeyboard { get; set; }
        [DataMember(Name = "selective")]
        public bool Selective { get; set; }
    }
}