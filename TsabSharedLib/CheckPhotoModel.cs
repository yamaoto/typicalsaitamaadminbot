using System;
using System.Runtime.Serialization;

namespace TsabSharedLib
{
    [DataContract]
    public class CheckPhotoModel
    {
        public CheckPhotoModel()
        {

        }

        public CheckPhotoModel(Guid id,int messageId,string blob,int number,int total,int wallId)
        {
            Id = id;
            MessageId = messageId;
            Blob = blob;
            Number = number;
            Total = total;
            WallId = wallId;
        }
        [DataMember(Name = "blob")]
        public string Blob { get; set; }
        [DataMember(Name = "number")]
        public int Number { get; set; }
        [DataMember(Name = "total")]
        public int Total { get; set; }
        [DataMember(Name = "wallId")]
        public int WallId { get; set; }
        [DataMember(Name = "messageId")]
        public int MessageId { get; set; }
        [DataMember(Name = "id")]
        public Guid Id { get; set; }
    }
}