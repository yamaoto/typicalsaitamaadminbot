using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsabSharedLib.ImageSearch;

namespace TsabConsole.Actions
{
    public class VkPublish: ITsabConsoleAction
    {
        public string Syntax { get; } = "vk-publish <user> <group> <album> <imageUrl>";
        public string Descriptioin { get; } = "no";
        public string ActionName { get; } = "vk-publish";
        public void Exec(string[] args)
        {
            var user = int.Parse(args[1]);
            var group = int.Parse(args[2]);
            var album = int.Parse(args[3]);
            var image = args[4];
            try
            {
                var item = new TumblrSearchResultItem()
                {
                    ImageUrl = image
                };
                Program.Context.CompareService.Publish(item, user, group, album).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
