using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class MessageEntryModel
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "offset")]
        public int Offset { get; set; }
        [DataMember(Name = "length")]
        public int Length { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "user")]
        public UserModel User { get; set; }
    }
}