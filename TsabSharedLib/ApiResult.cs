using System.Runtime.Serialization;

namespace TsabSharedLib
{
    [DataContract]
    public class ApiResult
    {
        public ApiResult()
        {
            Result = true;
        }
        public ApiResult(string error)
        {
            Error = error;
            Result = false;
        }
        [DataMember(Name = "error")]
        public string Error { get; set; }
        [DataMember(Name = "result")]
        public bool Result { get; set; }
    }

    [DataContract]
    public class ApiResult<T> : ApiResult
    {
        [DataMember(Name = "data")]
        public T Data { get; set; }
        public ApiResult() : base()
        {
        }
        public ApiResult(string error) : base(error)
        {
        }

        public ApiResult(T data) : base()
        {
            Data = data;
        }
    }
}