using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsabConsole.Actions
{
    internal class SearchAction: ITsabConsoleAction
    {
        public string Syntax { get; } = "search <tag> [count] [after]";
        public string Descriptioin { get; } = "Поиск изображений";
        public string ActionName { get; } = "search";
        public void Exec(string[] args)
        {
            string tag = null;
            int count = 0;
            DateTime? after = null;
            switch (args.Length)
            {
                case 2:
                    tag = args[1];
                    break;
                case 3:
                    tag = args[1];
                    count = int.Parse(args[2]);
                    break;
                case 4:
                    tag = args[1];
                    count = int.Parse(args[2]);
                    after = DateTime.Parse(args[3]);
                    break;
                default:
                    Console.WriteLine("Неверный формат команды.");
                    Console.WriteLine("Синтаксис: {0}", Syntax);
                    return;
            }
            var result =
                Program.Context.SearchService.Search(tag, count,after:after)
                .OrderByDescending(s => s.Score)
                .ToArray();
            for (var i = 0; i < result.Length; i++)
            {
                var item = result[i];
                Console.WriteLine($"{i}: {item.ImageUrl}");
            }
        }
    }
}
