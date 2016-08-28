using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ChosenInlineResultModel
    {
        [DataMember(Name="result_id")]
        public string ResultId { get; set; }
        [DataMember(Name = "from")]
        public UserModel From { get; set; }
        [DataMember(Name = "location")]
        public LocationModel Location { get; set; }
        [DataMember(Name="inline_message_id")]
        public string InlineMessageId { get; set; }
        [DataMember(Name="query")]
        public string Query { get; set; }
    }
}