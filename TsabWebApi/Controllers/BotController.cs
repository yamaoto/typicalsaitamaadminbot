using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TsabSharedLib;
using TsabWebApi.Models;

namespace TsabWebApi.Controllers
{
    public class BotController:ApiController
    {
        private static BotService _botService;
        private readonly DbService _dbService;

        public BotController()
        {
            if(_botService==null)
                _botService = new BotService();
            _dbService = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);

        }

        protected HttpResponseMessage View([CallerMemberName] string name = "")
        {
            var result = Request.CreateResponse(HttpStatusCode.OK);
            result.Content = new StringContent(File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath("~/" + name + ".html")), Encoding.UTF8);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return result;
        }

        protected HttpResponseMessage View<T>([CallerMemberName] string name = "",T model=null) where T:class
        {
            var result = Request.CreateResponse(HttpStatusCode.OK);
            var content = File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath("~/" + name + ".html"));
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var modelScript = "<script type=\"text/json\" id=\"model\">" + json + "</script>";
            content = content.Replace("<model />", modelScript);
            result.Content = new StringContent(content, Encoding.UTF8);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return result;
        }

        public static Dictionary<string, SearchResultModel> SearchResult = new Dictionary<string, SearchResultModel>();
        public static Dictionary<int,string> UserSearches = new Dictionary<int, string>();
        [Route("search")]
        [HttpGet]
        public HttpResponseMessage Search(string tag)
        {
            if (SearchResult.ContainsKey(tag))
            {
                return View(model: SearchResult[tag]);
            }
            else
            {
                return View("404");
            }
        }

        [Route("thumb")]
        [HttpGet]
        public HttpResponseMessage Thumb(string src)
        {
            var client = new WebClient();
            var data = client.DownloadData(src);
            Bitmap thumb;
            using (var stream = new MemoryStream(data))
            {
                var img = Image.FromStream(stream);
                var ratio = Convert.ToDecimal(img.Height) / Convert.ToDecimal(img.Width);

                const decimal width = 350;
                var height = width * ratio;

                thumb = new Bitmap(img,new Size(Convert.ToInt32(width),Convert.ToInt32(height)));
            }
            var outStream = new MemoryStream();
            thumb.Save(outStream,ImageFormat.Jpeg);
            outStream.Seek(0, 0);
            var result = Request.CreateResponse(HttpStatusCode.OK);
            result.Content = new StreamContent(outStream);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return result;
        }

        [Route("vk")]
        [HttpGet]
        public HttpResponseMessage Vk(string code,Guid state)
        {
            _dbService.SetVkUser(state,code:code);
            var client = new WebClient();
            var url =
                $"https://oauth.vk.com/access_token?client_id={ConfigStorage.VkAppId}&client_secret={ConfigStorage.VkSecret}&redirect_uri={ConfigStorage.VkOauthRedirect}&code={code}";
            var tokenData = client.DownloadString(url);
            dynamic json = JObject.Parse(tokenData);
            var telegramUserId = _dbService.GetTelegramUserId(state);
            if (json.access_token != null)
            {
                var token = (string)json.access_token;
                var expiresSec = (int)json.expires_in;
                var expires = DateTime.Now.AddSeconds(expiresSec);
                var userId = (long) json.user_id;
                _dbService.SetVkUser(state, token: token,expires:expires,userId:userId);
                _botService.BotApi.BotMethod("sendMessage", new SendMessageModel(telegramUserId, "Все нормально, доступ получили!")).Wait();
            }
            else
            {
                _botService.BotApi.BotMethod("sendMessage", new SendMessageModel(telegramUserId, json.error_description)).Wait();

            }
            return View();
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
                _dbService.SetMessageError(model.Message.Chat.Id,model.Message.MessageId,e.Message+" .Трассировка: "+e.StackTrace+"\r\n");
#if (DEBUG)
                _send(new SendMessageModel(model.Message.Chat.Id, "Что-то пошло не так...")).Wait();
#endif
            }
        }
        public void _webHookCallback(UpdateModel model)
        {
            string photo = null;
            string json = null;
            if (model.Message?.Photo != null)
                photo = model.Message.Photo.OrderBy(o => o.Weight).First().FileId;
#if (DEBUG)
            json = JsonConvert.SerializeObject(model);
#endif
            if (!_dbService.CheckMessage(model.Message.Chat.Id, model.Message.MessageId, model.Message.Text, photo, json))
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
            } else if (item is SendPhotoModel)
            {
                await _sendPhoto(item as SendPhotoModel);
            }
        }

        private async Task _sendPhoto(SendPhotoModel photo)
        {
            var url = _botService.BotApi.Method("sendPhoto");
            var client = new HttpClient();
            var requestContent = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(photo.Photo);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            requestContent.Add(imageContent, "photo", "image.jpg");
            requestContent.Add(new StringContent(photo.ChatId.ToString()), "chat_id");
            if(!string.IsNullOrEmpty(photo.Caption))
                requestContent.Add(new StringContent(photo.Caption), "caption");
            var result = await client.PostAsync(url, requestContent);
        }


        private async Task _sendMessage(SendMessageModel message)
        {
            var result = await _botService.BotApi.BotMethod< SendMessageModel,MessageModel>("sendMessage", message);
        }

        private async Task _sendSticker(SendStickerModel sticker)
        {
            var result = await _botService.BotApi.BotMethod<SendStickerModel, MessageModel>("sendSticker", sticker);
        }        

        
    }

}