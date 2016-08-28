using System;
using System.Runtime.Serialization;

namespace TsabSharedLib
{
    [DataContract]
    public class CheckPhotoResultModel
    {
        public CheckPhotoResultModel()
        {
            
        }

        public CheckPhotoResultModel(string foundBlob,int? value = null)
        {
            FoundBlob = foundBlob;
            Value = value;
        }
        [DataMember(Name = "foundBlob")]
        public string FoundBlob { get; set; }     
        
        [DataMember(Name = "result")]
        public int? Value { get; set; }   
    }
}