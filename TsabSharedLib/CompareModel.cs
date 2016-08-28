using System;
using System.Runtime.Serialization;

namespace TsabSharedLib
{
    [DataContract]
    public class CompareModel
    {
        public Guid Id { get; set; }
        public string InputBlob { get; set; }
        public string FoundBlob { get; set; }
        public int AuthorId { get; set; }
        public int AuthorChatId { get; set; }
        public string AuthorLastName { get; set; }
        public string AuthorFirstName { get; set; }
        public int? Timespan { get; set; }
        public DateTime ExecDate { get; set; }
        public string Error { get; set; }
        public string Algorithm { get; set; }
        public int Workers { get; set; }
        public int WallId { get; set; }
        public int? CompareValue { get; set; }
    }
}