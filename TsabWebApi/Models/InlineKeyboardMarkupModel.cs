using System.Runtime.Serialization;

namespace TsabWebApi.Models
{

    [DataContract]
    public class InlineKeyboardMarkupModel
    {
        [DataMember(Name="inline_keyboard")]
        public InlineKeyboardButtonModel[] InlineKeyboard { get; set; }
    }
}