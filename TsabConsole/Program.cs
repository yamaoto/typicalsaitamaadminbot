using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TsabConsole.Actions;
using TsabSharedLib;

namespace TsabConsole
{
    class Program
    {
        public static Context Context;

        public static IDictionary<string, ITsabConsoleAction> Actions;

        static void Main(string[] args)
        {
            string config = null;
            while (true)
            {
                Console.Write("config: ");
                config = Console.ReadLine();
                if (config == "default" || config == "local") 
                    break;
                Console.WriteLine("config может принимать значение 'default' или 'local'");

            }
            Context = new Context(config);
            var list = new List<ITsabConsoleAction>()
            {
                new CleanAction(),
                new UpdateWallAction(),
                new LoadWallAction(),
                new LoadPhotosAction(),
                new CheckPhotoAction(),
                new ListWallAction(),
                new ListAdminAction(),
                new HelpAction(),
                new VkAction(),
                new SearchAction()
            };
            Actions = list.ToDictionary(s => s.ActionName, s => s);
            var command = "";
            while (command.ToLower()!="exit")
            {
                Console.Write(">");
                command = Console.ReadLine();
                Command(command.Split(new[] {' '}));
            }
        }

        public static void Command(string[] args)
        {
            var actionName = args[0];
            if (Actions.ContainsKey(actionName))
            {
                try
                {
                    var action = Actions[actionName];
                    var start = DateTime.Now;
                    action.Exec(args);
                    var end = DateTime.Now;
                    var span = end - start;
                    Console.WriteLine("Выполнено: {0}сек", span.TotalSeconds.ToString("F"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: {0}", e.Message);
                    Console.WriteLine("StackTrace: {0}", e.StackTrace);
                }
            }
            else
            {
                Console.WriteLine("Неизвестная команда");
            }
        }
    }

    public class Context
    {
        public Context(string config)
        {
            CloudStorageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            DbService = new DbService(ConfigurationManager.ConnectionStrings[config].ConnectionString);
            CompareService = new CompareService(DbService, CloudStorageAccount);
            SearchService = new SearchService();

            var blobClient = CloudStorageAccount.CreateCloudBlobClient();
            ImagesContainer = blobClient.GetContainerReference("images");
            DraftsContainer = blobClient.GetContainerReference("drafts");
        }

        public readonly DbService DbService;
        public readonly CompareService CompareService;
        public readonly CloudStorageAccount CloudStorageAccount;

        public readonly CloudBlobContainer ImagesContainer;
        public readonly CloudBlobContainer DraftsContainer;

        public readonly SearchService SearchService;
    }

}
