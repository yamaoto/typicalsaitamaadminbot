using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class CallbackQueryModel
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "from")]
        public UserModel From { get; set; }
        [DataMember(Name = "message")]
        public MessageModel Message { get; set; }
        [DataMember(Name="inline_message_id")]
        public string InlineMessageId { get; set; }
        [DataMember(Name = "data")]
        public string Data { get; set; }
    }
}