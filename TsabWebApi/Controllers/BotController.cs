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
            }
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