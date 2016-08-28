using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace TsabWebApi.Models
{
    [DataContract]
    public class TelegramResult<T>
    {
        [DataMember(Name="ok")]
        public bool Ok { get; set; }

        [DataMember(Name = "result")]
        public T Result { get; set; }
    }
}