using System.Threading.Tasks;

namespace TsabSharedLib
{
    public interface IBotApi
    {
        Task<string> BotMethod<TSend>(string method, TSend data);
        Task<TResult> BotMethod<TSend, TResult>(string method, TSend data) where TResult : class;
        Task<byte[]> GetFile(string filePath);
    }
}