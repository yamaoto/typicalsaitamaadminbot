using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class GetFileModel
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; }

    }
}