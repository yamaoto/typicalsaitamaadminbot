using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ChatModel
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "username")]
        public string Username { get; set; }
        [DataMember(Name="first_name")]
        public string FirstName { get; set; }
        [DataMember(Name="last_name")]
        public string LastName { get; set; }
    }
}