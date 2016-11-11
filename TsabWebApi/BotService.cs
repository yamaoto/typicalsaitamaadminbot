using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using TsabSharedLib;
using TsabWebApi.BotCommands;
using TsabWebApi.Controllers;
using TsabWebApi.Models;

namespace TsabWebApi
{
    internal class BotService
    {
        public readonly BotApi BotApi;
        private readonly DbService _dbService;

        private readonly CloudBlobContainer _imagesContainer;
        private readonly CloudBlobContainer _draftsContainer;
        private readonly CompareService _compareService;
        private readonly SearchService _searchService;

        private readonly IBotAction[] _actions;
        

        internal BotService()
        {
           
            _dbService = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var blobClient = storage.CreateCloudBlobClient();
            _imagesContainer = blobClient.GetContainerReference("images");
            _draftsContainer = blobClient.GetContainerReference("drafts");
            _compareService = new CompareService(new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString), storage);            
            _searchService = new SearchService();
            _actions = GetActions();
            BotApi = new BotApi();
            var context = new BotActionContext(BotApi, _dbService,_imagesContainer,_draftsContainer,_compareService, _searchService);
            foreach (var action in _actions)
            {
                action.Start(context);
            }
        }
        public static IBotAction[] GetActions()
        {
            var assembly = Assembly.GetAssembly(typeof (BotService));
            var actionTypes =  assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(BotActionAttribute), true).Length > 0);
            var actions =  actionTypes.Select(type => (IBotAction) Activator.CreateInstance(type)).ToList();
            return actions.ToArray();
        }

        public static DbService GetDbService()
        {
            return new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
        }

        internal ISendItem Photo(MessageModel message, out MessageFlow flow)
        {
            flow = null;
            var auths = _dbService.GetAuths(message.From.Id).Where(a => a.Auth);
            if (!auths.Any())
            {
                return new SendMessageModel(message.Chat.Id, "Похоже что у тебя еще нет привязанных сообществ. Введи /public чтобы продолжить работу");
            }
#pragma warning disable 4014
            _photoAsyncTask(message);
#pragma warning restore 4014
            
            return new SendMessageModel(message.Chat.Id, "мм, посмотрим что у нас есть...");
        }

        private bool _photoCompareSync(CheckPhotoModel param)
        {
            var result = _compareService.CheckPhoto(param);
            var db = GetDbService();
            db.CloseCompare(param.Id, result.FoundBlob, compareValue: result.Value);
            if (!result.Value.HasValue)
                return false;
            var wall = db.GetWall(param.WallId);
            var compare = db.GetCompare(param.Id);
            var wallItem = db.GetWallItemByBlob(param.WallId, result.FoundBlob);
            decimal dif = compare.CompareValue.Value / (decimal)ConfigStorage.CompareSize * 100.0m / (decimal)ConfigStorage.CompareDif;
            decimal sec = compare.Timespan.Value / 1000m;
            BotApi.BotMethod("sendMessage",
                new SendMessageModel(compare.AuthorChatId,
                    $"Такое уже есть в {wall.Name}, оценка погрешости ({dif.ToString("F")}%), найдено за {sec.ToString("F1")} сек.")
                {
                    ReplyToMessageId = param.MessageId
                }).Wait();
            BotApi.BotMethod("sendMessage", new SendMessageModel(compare.AuthorChatId, wallItem.Url)).Wait();
            return true;
        }

        private async Task _photoAsyncTask(MessageModel message)
        {
            try
            {
                var photo = message.Photo.OrderBy(o => o.Weight).First();
                var fileInfo =
                    await
                        BotApi.BotMethod<GetFileModel, TelegramResult<FileModel>>("getFile",
                            new GetFileModel() { FileId = photo.FileId });
                var data = await BotApi.GetFile(fileInfo.Result.FilePath);

                var blockBlob = _draftsContainer.GetBlockBlobReference(photo.FileId);
                blockBlob.Properties.ContentType = "image/jpeg";
                blockBlob.UploadFromByteArray(data, 0, data.Length);
                var auths = _dbService.GetAuths(message.From.Id).Where(a => a.Auth);
                var list = new List<CheckPhotoModel>();
                foreach (var auth in auths)
                {
                    var id = _dbService.InsertCompare(photo.FileId, message.From.Id, message.Chat.Id,
                        message.From.LastName, message.From.FirstName, ConfigStorage.MtNonClusteredV1, 1,
                        auth.WallId);
                    var param = new CheckPhotoModel(id, message.MessageId, photo.FileId, auth.WallId);
                    list.Add(param);
                    
                }
                var resultList = new ConcurrentBag<bool>();
                Parallel.ForEach(list, new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount},
                    (param) => resultList.Add(_photoCompareSync(param)));
                if (!resultList.Any(a => a))
                {
                    await BotApi.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id,"Ничего похожего я не смог найти"));
                }
            }
            catch (Exception e)
            {
#if (DEBUG)
                await BotApi.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, e.Message));
#else
                await BotApi.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, "Что-то пошло не так..., на всякий случай: попробуй обновить стены сообществ..."));
#endif
            }
        }

        internal ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            _dbService.UpdateState(message.From.Id, message.From.LastName, message.From.FirstName, message.From.Username,
                message.Chat.Id);
            var action = _actions.FirstOrDefault(s => s.CommandName == command);
            if (action!=null)
            {
                return action.Command(command, message, out flow);
            }
            return _actions.First(s=>s.CommandName=="/help").Command(command, message, out flow);
        }        
        
        internal ISendItem Message(string text, MessageModel message, out MessageFlow flow)
        {
            _dbService.UpdateState(message.From.Id, message.From.LastName, message.From.FirstName, message.From.Username,
                message.Chat.Id);
            var sate = _dbService.GetState(message.From.Id);
            var state = _dbService.GetState(message.From.Id).State;
            var action = _actions.FirstOrDefault(s => s.States.Any(a=>a== state));
            if (action != null)
            {
                return action.Message(sate.State, text, message, out flow);
            }
            return _actions.First(s => s.States.Any(a=>a== "NoState")).Message(sate.State, text, message, out flow);
        }        

    }

    public class BotApi : IBotApi
    {
        private readonly WebClient _client;
        private readonly string _token;

        public BotApi()
        {
            _token = ConfigurationManager.AppSettings["token"];
            _client = new WebClient();
        }

        public string Method(string method, Dictionary<string, string> param = null)
        {
            var url = $"https://api.telegram.org/bot{_token}/{method}";
            if (param != null && param.Keys.Count > 0)
            {
                url += "?";
                foreach (var item in param)
                {
                    if (url[url.Length - 1] != '?')
                        url += "&";
                    url += HttpUtility.UrlEncode(item.Key) + "=" + HttpUtility.UrlEncode(item.Value);
                }
            }
            return url;
        }


        public async Task<string> BotMethod<TSend>(string method, TSend data)
        {
            var result = await _makreRequest<TSend>(Method(method), data);
            return result;
        }

        public async Task<TResult> BotMethod<TSend, TResult>(string method, TSend data) where TResult : class
        {
            var result = await _makreRequest<TSend, TResult>(Method(method), data);
            return result;
        }

        public async Task<byte[]> GetFile(string filePath)
        {
            var result = await _client.DownloadDataTaskAsync($"https://api.telegram.org/file/bot{_token}/{filePath}");
            return result;
        }
        private async Task<string> _makreRequest(string url)
        {
            var result = await _client.DownloadStringTaskAsync(url);
            return result;
        }
        private async Task<string> _makreRequest<TSend>(string url, TSend data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                var buffer = Encoding.UTF8.GetBytes(json);
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.ContentLength = buffer.Length;
                req.Timeout = Convert.ToInt32(TimeSpan.FromHours(1).TotalMilliseconds);
                var stream = req.GetRequestStream();

                stream.Write(buffer, 0, buffer.Length);
                stream.Close();
                var response = req.GetResponse();
                var responseStream = response.GetResponseStream();
                var result = new StreamReader(responseStream, Encoding.UTF8).ReadToEnd();
                response.Close();
                responseStream.Close();
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        private async Task<TResponse> _makreRequest<TSend, TResponse>(string url, TSend data) where TResponse : class
        {
            var json = await _makreRequest<TSend>(url, data);
            TResponse result = null;
            try
            {
                result = JsonConvert.DeserializeObject<TResponse>(json);
            }
            catch (Exception e)
            {
#if (DEBUG)
                throw new Exception("json error: " + json, e);
#endif
                throw;
            }
            return result;
        }


        //private async Task _registerWebhook()
        //{
        //    var webhook = ConfigurationManager.AppSettings["webhook"];
        //    var url = _method("setWebhook",new Dictionary<string, string>() { {"url", webhook } });
        //    var result = await _makreRequest(url);
        //}
    }

    internal class BotActionContext
    {
        public readonly BotApi BotMethods;
        public readonly DbService DbService;
        public readonly SearchService SearchService;

        public readonly CloudBlobContainer ImagesContainer;
        public readonly CloudBlobContainer DraftsContainer;
        public readonly CompareService CompareService;

        public BotActionContext(BotApi botMethods, DbService dbService, CloudBlobContainer imagesContainer, CloudBlobContainer draftsContainer, CompareService compareService, SearchService searchService)
        {
            BotMethods = botMethods;
            DbService = dbService;
            ImagesContainer = imagesContainer;
            DraftsContainer = draftsContainer;
            CompareService = compareService;
            SearchService = searchService;
        }
    }
}