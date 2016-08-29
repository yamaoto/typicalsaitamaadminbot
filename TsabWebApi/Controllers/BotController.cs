using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using TsabSharedLib;
using TsabWebApi.Models;

namespace TsabWebApi.Controllers
{
    internal interface IBotMethod
    {
        Task<string> BotMethod<TSend>(string method, TSend data);
        Task<TResult> BotMethod<TSend,TResult>(string method, TSend data) where TResult : class;
        Task<byte[]> GetFile(string filePath);
        //Task<TResult> ApiMethod<TSend, TResult>(string worker, string method, TSend data) where TResult : class;
    }

    public class BotController:ApiController, IBotMethod
    {
        private readonly string _token;
        private readonly WebClient _client;
        private readonly BotService _botService;
        private readonly DbService _dbService;

        public BotController()
        {
            _token = ConfigurationManager.AppSettings["token"];
            var registered = ConfigurationManager.AppSettings["reistered"]=="true";
            _client = new WebClient();
            _botService = new BotService(this);
            _dbService = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);

        }

        [Route("Register")]
        [HttpGet]
        public async Task<bool> Register(UpdateModel model)
        {
            var registered = ConfigurationManager.AppSettings["reistered"]=="true";
            if (!registered)
            {
                await _registerWebhook();
                return true;
            }
            return false;
        }
        [Route("244732989_BotWebhook")]
        [HttpPost]
        public void WebHookCallback(UpdateModel model)
        {
            try
            {
                _webHookCallback(model);
            }
            catch (Exception e)
            {
#if (DEBUG)
                _send(new SendMessageModel(model.Message.Chat.Id, e.Message+ e.StackTrace)).Wait();
#endif
            }
        }
        public void _webHookCallback(UpdateModel model)
        {
            if (!_dbService.CheckMessage(model.Message.Chat.Id,model.Message.MessageId,model.Message.Text))
            {
                return;
            }
            MessageFlow flow = null;
            ISendItem response = null;
            if (!string.IsNullOrEmpty(model.Message?.Text))
            {

                if (model.Message.Text.StartsWith("/"))
                    response = _botService.Command(model.Message.Text, model.Message, out flow);
                else
                    response = _botService.Message(model.Message.Text, model.Message, out flow);
            }
            else
            {
                if (model.Message?.Photo != null)
                {
                    response = _botService.Photo(model.Message, out flow);

                }
                else if (model.Message != null)
                {
                    response = _botService.Message("", model.Message, out flow);
                }
            }
#pragma warning disable 4014
            if (response != null)
            {
                _send(response);
            }
            if (flow != null)
            {
                _sendItems(flow);
            }

#pragma warning restore 4014
        }

        private Task _sendItems(MessageFlow flow)
        {
            return Task.Run(() => _sendItemsSync(flow));
        }
        private void _sendItemsSync(MessageFlow flow)
        {
            foreach (var item in flow)
            {
                if(item.Span.HasValue)
                    Thread.Sleep(item.Span.Value);
                _send(item.Message).Wait();
            }
        }

        private async Task _send(ISendItem item)
        {
            if (item is SendMessageModel)
            {
                await _sendMessage(item as SendMessageModel);
            } else if (item is SendStickerModel)
            {
                await _sendSticker(item as SendStickerModel);
            }
        }

        public async Task<string> BotMethod<TSend>(string method, TSend data)
        {
            var result = await _makreRequest<TSend>(_method(method), data);
            return result;
        }

        public async Task<TResult> BotMethod<TSend,TResult>(string method, TSend data) where TResult : class
        {
            var result = await _makreRequest<TSend, TResult>(_method(method), data);
            return result;
        }
        
        public async Task<TResult> ApiMethod<TSend, TResult>(string worker,string method, TSend data) where TResult : class
        {
            var result = await _makreRequest<TSend, TResult>(worker+method, data);
            return result;
        }

        public async Task<byte[]> GetFile(string filePath)
        {
            var result = await _client.DownloadDataTaskAsync($"https://api.telegram.org/file/bot{_token}/{filePath}");
            return result;
        }

        private async Task _sendMessage(SendMessageModel message)
        {
            var result = await BotMethod< SendMessageModel,MessageModel>("sendMessage", message);
        }

        private async Task _sendSticker(SendStickerModel sticker)
        {
            var result = await BotMethod<SendStickerModel, MessageModel>("sendMessage", sticker);
        }

        private string _method(string method,Dictionary<string,string> param=null)
        {
            var url= $"https://api.telegram.org/bot{_token}/{method}";
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

        private string _api(string method, int number, Dictionary<string, string> param = null)
        {
            var url = $"http://typical-saitama-admin-bot-w{number}.azurewebsites.net/{method}";
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

        private async Task _registerWebhook()
        {
            var webhook = ConfigurationManager.AppSettings["webhook"];
            var url = _method("setWebhook",new Dictionary<string, string>() { {"url", webhook } });
            var result = await _makreRequest(url);
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
                var json = JsonConvert.SerializeObject(data,new JsonSerializerSettings() {NullValueHandling = NullValueHandling.Ignore});
                var buffer = Encoding.UTF8.GetBytes(json);
                var req = (HttpWebRequest )WebRequest.Create(url);
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

        private async Task<TResponse> _makreRequest<TSend,TResponse>(string url, TSend data) where TResponse : class
        {
            var json = await _makreRequest<TSend>(url,data);
            TResponse result = null;
            try
            {
                result = JsonConvert.DeserializeObject<TResponse>(json);
            }
            catch (Exception e)
            {
#if (DEBUG)
                throw new Exception("json error: "+json,e);
#endif
                throw;
            }
            return result;
        }
    }

}