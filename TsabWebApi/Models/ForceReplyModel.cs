using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ForceReplyModel
    {
        [DataMember(Name="force_reply")]
        public bool ForceReply { get; set; }
        [DataMember(Name = "selective")]
        public bool Selective { get; set; }
    }
}