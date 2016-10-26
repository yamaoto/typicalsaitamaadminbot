using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsabSharedLib
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SearchEngineAttribute : Attribute
    {
    }

    public interface ISearchEngine
    {
        string EngineName { get; }
        IEnumerable<ISearchResultItem> Search(string tag,int count,DateTime? after);
    }

    public interface ISearchResultItem
    {
        string ItemUrl { get; set; }
        string ImageUrl { get; set; }
        string Description { get; set; }
        string[] Tags { get; set; }
        string Engine { get; set; }
        string Group { get; set; }
        int Score { get; set; }
    }

}
