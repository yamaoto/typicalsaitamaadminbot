using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ContactModel
    {
        [DataMember(Name="user_id")]
        public int UserId { get; set; }
        [DataMember(Name="first_name")]
        public string FirstName { get; set; }
        [DataMember(Name="last_name")]
        public string LastName { get; set; }
        [DataMember(Name="phone_number")]
        public string PhoneNumber { get; set; }
    }
}