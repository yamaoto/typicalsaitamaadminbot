using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class InlineKeyboardButtonModel
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name="callback_data")]
        public string CallbackData { get; set; }
        [DataMember(Name="switch_inline_query")]
        public string SwitchInlineQuery { get; set; }
    }
}