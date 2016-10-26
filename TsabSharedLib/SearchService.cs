using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TsabSharedLib
{
    public class SearchService
    {
        public ISearchEngine[] Engines { get; set; }
        public SearchService()
        {
            Engines = GetEngines();
        }

        public IEnumerable<ISearchResultItem> Search(string tag, int totalForEach,DateTime? after=null)
        {
            var result = new List<ISearchResultItem>();
            foreach (var engine in Engines)
            {
                IEnumerable<ISearchResultItem> items;
                try
                {
                    items = engine.Search(tag, totalForEach, after);
                }
                catch
                {
                    continue;                    
                }
                result.AddRange(items);
            }
            return result;
        }

        public static ISearchEngine[] GetEngines()
        {
            var assembly = Assembly.GetAssembly(typeof(SearchService));
            var actionTypes = assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(SearchEngineAttribute), true).Length > 0);
            var actions = actionTypes.Select(type => (ISearchEngine)Activator.CreateInstance(type)).ToList();
            return actions.ToArray();
        }
    }
}
