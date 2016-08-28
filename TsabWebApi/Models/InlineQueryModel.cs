using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class InlineQueryModel
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "from")]
        public UserModel From { get; set; }
        [DataMember(Name = "location")]
        public LocationModel Location { get; set; }
        [DataMember(Name = "query")]
        public string Query { get; set; }
        [DataMember(Name = "offset")]
        public int Offset { get; set; }
    }
}