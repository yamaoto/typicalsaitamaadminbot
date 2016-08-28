using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using TsabSharedLib;
using TsabWebApi.Controllers;
using TsabWebApi.Models;

namespace TsabWebApi
{
    internal class BotService
    {
        private readonly IBotMethod _botMethod;
        private readonly DbService _dbService;

        private readonly CloudBlobContainer _imagesContainer;
        private readonly CloudBlobContainer _draftsContainer;
        internal BotService(IBotMethod botMethod)
        {
            _botMethod = botMethod;
            _dbService = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var blobClient = storage.CreateCloudBlobClient();
            _imagesContainer = blobClient.GetContainerReference("images");
            _draftsContainer = blobClient.GetContainerReference("drafts");
        }

        public static DbService GetDbService()
        {
            return new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
        }

        internal ISendItem Photo(MessageModel message, out MessageFlow flow)
        {
            var auths = _dbService.GetAuths(message.From.Id).Where(a => a.Auth);
            _photo(message);
            flow = null;
            return new SendMessageModel(message.Chat.Id, "мм, посмотрим что у нас есть...");
        }

        private async Task _photo(MessageModel message)
        {
            try
            {
                var photo = message.Photo.OrderBy(o => o.Weight).First();
                var fileInfo =
                    await
                        _botMethod.BotMethod<GetFileModel, TelegramResult<FileModel>>("getFile",
                            new GetFileModel() {FileId = photo.FileId});
                var data = await _botMethod.GetFile(fileInfo.Result.FilePath);

                var blockBlob = _draftsContainer.GetBlockBlobReference(photo.FileId);
                blockBlob.Properties.ContentType = "image/jpeg";
                blockBlob.UploadFromByteArray(data, 0, data.Length);
                var workers = _dbService.GetWorkers();
                var auths = _dbService.GetAuths(message.From.Id).Where(a => a.Auth);
                var tasks = new List<Task<ApiResult<CheckPhotoResultModel>>>();
                var number = 0;
                var compareIds = new List<Guid>();
                foreach (var auth in auths)
                {
                    var id = _dbService.InsertCompare(photo.FileId, message.From.Id, message.Chat.Id,
                        message.From.LastName, message.From.FirstName, ConfigStorage.ClusteredV1, workers.Count(),
                        auth.WallId);
                    compareIds.Add(id);
                    foreach (var worker in workers)
                    {
                        var param = new CheckPhotoModel(id, message.MessageId, photo.FileId, number, workers.Count(),
                            auth.WallId);
                        var task = Task.Run(() =>
                        {
                            var tsk=  _botMethod.ApiMethod<CheckPhotoModel, ApiResult<CheckPhotoResultModel>>(worker,
                                ConfigStorage.CheckPhotoMethod, param);
                            var taskResult =
                                tsk.ContinueWith((t) => _photoResultHandler(t, id, auth.WallId, message.MessageId));
                            return tsk;
                        });
                        tasks.Add(task);
                        number++;
                    }
                }
                Task[] ar = tasks.ToArray();
                Task.WaitAll(ar);
                var db = GetDbService();
                var found = false;
                foreach (var compareId in compareIds)
                {
                    var compare = db.GetCompare(compareId);
                    if (compare.FoundBlob != null)
                    {
                        found = true;
                    }
                    if (!compare.Timespan.HasValue)
                    {
                        _dbService.CloseCompare(compare.Id, error: "Сходств не обнаружено");
                    }
                }
                if (!found)
                {
                    await
                        _botMethod.BotMethod("sendMessage",
                            new SendMessageModel(message.Chat.Id, "Такой картинки я нигде не нашел!"));
                }
            }
            catch (Exception e)
            {
#if (DEBUG)
                await _botMethod.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, e.InnerException.Message+e.InnerException.StackTrace));
#else
                await _botMethod.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, "Что-то пошло не так..."));
#endif
            }
        }

        private void _photoResultHandler(Task<ApiResult<CheckPhotoResultModel>> task,Guid compareId,int wallId,int messageId)
        {
            if (task.Status!=TaskStatus.Faulted)
            {
                if (!task.Result.Result) return;
                if (task.Result.Data.FoundBlob == null) return;
                var result = task.Result.Data;
                var db = GetDbService();
                db.CloseCompare(compareId, result.FoundBlob, task.Result.Error,compareValue: result.Value);
                var wall = db.GetWall(wallId);
                var compare = db.GetCompare(compareId);
                var wallItem = db.GetWallItemByBlob(wallId, result.FoundBlob);
                var dif = compare.CompareValue.Value / (decimal)ConfigStorage.CompareSize * 100.0m / (decimal)ConfigStorage.CompareDif;
                _botMethod.BotMethod("sendMessage",
                    new SendMessageModel(compare.AuthorChatId,
                        $"Такое уже есть в {wall.Name}, оценка погрешости ({dif.ToString("F")}%)")
                    {
                        ReplyToMessageId = messageId
                    });
                _botMethod.BotMethod("sendMessage", new SendMessageModel(compare.AuthorChatId, wallItem.Url));
            }
            
        }
        internal ISendItem Command(string command,MessageModel message, out MessageFlow flow)
        {
            _dbService.UpdateState(message.From.Id, message.From.LastName, message.From.FirstName, message.From.Username,
                message.Chat.Id);
            switch (command)
            {
                case "/start":
                    return _start(message, out flow);
                case "/public":
                    return _public(message, out flow);
                case "/cansel":
                    return _cansel(message, out flow);
                case "/update":
                    return _update(message, out flow);
                case "/help":
                default:
                    return _help(message, out flow);
            }
        }
        private ISendItem _update(MessageModel message, out MessageFlow flow)
        {
            _sendUpdate(message);
            flow = null;
            return new SendMessageModel(message.Chat.Id,"Щас...");
        }

        private Task _sendUpdate(MessageModel message)
        {
            return Task.Run(() => _sendUpdateSync(message));
        }

        private void _sendUpdateSync(MessageModel message)
        {
            var auths = _dbService.GetAuths(message.From.Id);
            var workers = _dbService.GetWorkers();
            var i = 0;
            foreach (var auth in auths)
            {
                if (i >= workers.Length)
                    i = 0;
                var worker = workers[i];
                var wall = _dbService.GetWall(auth.WallId);
                //TODO:тут происходит синхнронный вызов изза ContinueWith
                Task.Run(() =>
                {
                    var task = _botMethod.ApiMethod<WallUpdateModel, ApiResult>(worker, ConfigStorage.UpdateWallMethod, new WallUpdateModel(auth.WallId));
                    task.ContinueWith(taskResult => _updateAfter(taskResult, message.Chat.Id, wall.Name, auth.WallId));
                });
            }
        }
        private void _updateAfter(Task<ApiResult> task,int chatId, string wall,int wallId)
        {
            if (task.Status!=TaskStatus.Faulted)
            {
                if (task.Result.Result)
                {
                    var workers = _dbService.GetWorkers();
                    var i = new Random(DateTime.Now.Millisecond).Next(0, workers.Length - 1);
                    var worker = workers[i];
                    _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Стена {wall} почти обновлена..."));
                    var loadTask = _botMethod.ApiMethod<WallUpdateModel, ApiResult>(worker, ConfigStorage.LoadWallMethod, new WallUpdateModel(wallId));
                    loadTask.ContinueWith(taskResult => _loadAfter(taskResult, chatId,wall,wallId));
                    return;
                }
#if (DEBUG)
                _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось: {task.Result.Error}"));
                return;
#endif
            }
#if (DEBUG)
            _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось: {task.Exception.Message}"));
#else
            _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось"));
#endif
        }

        private void _loadAfter(Task<ApiResult> task, int chatId, string wall,int wallId)
        {
            if (task.Status != TaskStatus.Faulted)
            {
                if (task.Result.Result)
                {
                    _loadPhotos(chatId,wallId,wall);
                    _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Стена {wall} обновлена! Теперь осталось подготовить рабочих...."));
                    return;
                }
#if (DEBUG)
                _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось: {task.Result.Error}"));
                return;
#else
                _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось"));
#endif
            }
#if (DEBUG)
            _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось: {task.Exception.Message} {task.Exception.StackTrace}"));
            if(task.Exception.InnerException!=null)
                _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось: {task.Exception.InnerException.Message} {task.Exception.InnerException.StackTrace}"));
#else
            _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Со стеной {wall} что-то стряслось"));
#endif
        }

        private void _loadPhotosSync(int chatId,int wallId,string wall)
        {
            try
            {
                var workers = _dbService.GetWorkers();
                var data = new WallUpdateModel(wallId);
                var tasks = new List<Task>();
                foreach (var worker in workers)
                {
                    var task = _botMethod.ApiMethod<WallUpdateModel, ApiResult>(worker, ConfigStorage.LoadPhotosMethod, data);
                    tasks.Add(task);
                }
                var ar = tasks.ToArray();
                Task.WaitAll(ar);
                _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, $"Отлично! Наши рабочие готовы к работе со стеной {wall}!"));
            }
            catch (Exception e)
            {
#if (DEBUG)
                _botMethod.BotMethod("sendMessage", new SendMessageModel(chatId, "Наши рабочие при подготовке допустили ошибку: "+e.Message+e.StackTrace));
#else
                Console.WriteLine("Наши рабочие при подготовке допустили какую-то ошибку...");
#endif
            }
        }
        private Task _loadPhotos(int chatId,int wallId,string wall)
        {
            return Task.Run(() => _loadPhotosSync(chatId,wallId, wall));
        }
        private ISendItem _cansel(MessageModel message, out MessageFlow flow)
        {
            _dbService.SetState(message.From.Id,"NoState");
            var msg = $@"Ладно";
            flow = new MessageFlow() {new MessageFlowItem(message.Chat.Id, "BQADBAADMAMAAqKYZgABj5c3MVEUXD4C", true,TimeSpan.FromSeconds(2))};
            return new SendMessageModel(message.Chat.Id, msg);
        }
        private ISendItem _public(MessageModel message, out MessageFlow flow)
        {
            var msg = $@"Ok, ты можешь получить доступ к этим сообществам:";
            var msg2 = new SendMessageModel(message.Chat.Id, "К какому сообществу тебя записать?");
            msg2.ReplyMarkup = new ReplyKeyboardMarkupModel()
            {
                OneTimeKeyboard = true,
            };
            var walls = _dbService.GetWalls();
            var btns = new List<KeyboardButtonModel>();
            var auths = _dbService.GetAuths(message.From.Id);
            foreach (var wall in walls.Where(wall => !auths.Any(a => a.WallId == wall.Id && a.Auth)))
            {
                msg = msg + $"\r\n    *) {wall.Name}";
                btns.Add(new KeyboardButtonModel() {Text= wall.Name });
            }
            if (btns.Count == 0)
            {
                flow = new MessageFlow() {new MessageFlowItem(message.Chat.Id, "BQADAgAD-SYAAktqAwABXyf8g9wohKUC", true, TimeSpan.FromSeconds(1))};
                return new SendMessageModel(message.Chat.Id, "Так, слушай. У нас больше нет сообществ для тебя");
            }
            ((ReplyKeyboardMarkupModel) msg2.ReplyMarkup).Keyboard = btns.Select(s => new[] {s}).ToArray();
           _dbService.SetState(message.From.Id,"choose-public");
            flow = new MessageFlow() { { new MessageFlowItem(msg2, TimeSpan.FromSeconds(2)) } };
            return new SendMessageModel(message.Chat.Id, msg);
        }

        private ISendItem _start(MessageModel message, out MessageFlow flow)
        {            
            var msg = $@"Привет, {message.From.FirstName}!
Я @typical_saitama_adminBot. С моей помощью ты можешь проверять картинки на загрузку в сообщества, для этого просто введи /public или /help для получения всех подказок";
            var sticker = "BQADBAADtwMAAqKYZgABJFsIZLA51N0C";
            flow = new MessageFlow() { { new MessageFlowItem(message.Chat.Id, sticker, true,TimeSpan.FromSeconds(3))}};
            return new SendMessageModel(message.Chat.Id,msg);
        }
        private ISendItem _help(MessageModel message, out MessageFlow flow)
        {
            var msg = $@"Итак, {message.From.FirstName}!
Вот команды на которые меня обучил @yamaoto:
/start - приветствие
/public - получение доступа к сообществу
/update - загрузка обновлений сообществ
/cansel - отмена действия
/help - показать команды";
            flow = null;
            return new SendMessageModel(message.Chat.Id, msg);
        }

        internal ISendItem Message(string text, MessageModel message, out MessageFlow flow)
        {
            _dbService.UpdateState(message.From.Id, message.From.LastName, message.From.FirstName, message.From.Username,
                message.Chat.Id);
            var state = _dbService.GetState(message.From.Id);
            switch (state.State)
            {
                case "choose-public":
                    return _stateChoosePublic(text, message, out flow);
                case "wait-auth":
                    return _stateWaitAuth(text, message, out flow);
                case "grant-auth":
                    return _stateGrantAuth(text, message, out flow);
                default:
                    flow = null;
                    if (message.Sticker != null)
                    {
                        flow = new MessageFlow()
                        {
                            new MessageFlowItem(message.Chat.Id, message.Sticker.FileId,true, TimeSpan.FromMilliseconds(500))
                        };
                        return new SendMessageModel(message.Chat.Id, message.Sticker.FileId);
                    }
                    return new SendMessageModel(message.Chat.Id, ".-.");
            }            
        }

        internal ISendItem _stateGrantAuth(string text, MessageModel message, out MessageFlow flow)
        {
            var state = _dbService.GetState(message.From.Id);
            var authId = Guid.Parse(state.StateParams);
            var auth = _dbService.GetAuth(authId);
            var admins = _dbService.GetWallAdmins(auth.WallId);

            if (auth.Solved)
            {
                flow = new MessageFlow()
                {
                    new MessageFlowItem(message.From.Id,"BQADBAADtwMAAqKYZgABJFsIZLA51N0C",true,TimeSpan.FromSeconds(1)),
                    new MessageFlowItem(message.From.Id,"Эту проблему уже решили",TimeSpan.FromSeconds(3))
                };
                return new SendMessageModel(message.Chat.Id, "Поздняк метаться");
            }

            var yes = new[] {"yes", "да", "конечно"};
            var no = new[] {"нет", "no", "исключено"};
            var messages = new MessageFlow();
            if (yes.Any(a => a.Equals(text, StringComparison.OrdinalIgnoreCase)))
            {
                _dbService.GrantAuth(authId,true);
                _dbService.SetState(auth.UserId,"NoState");
                _dbService.SetState(message.From.Id, "NoState");
                messages.Add(new MessageFlowItem(auth.UserChatId,
                    $"Отличные новости! {message.From.FirstName} {message.From.LastName} подтвердил твой зарос!"));
                messages.Add(new MessageFlowItem(auth.UserChatId, "BQADBAADgwMAAqKYZgABz5uEcP7gGQ0C", true,TimeSpan.FromSeconds(1)));
                messages.Add(new MessageFlowItem(auth.UserChatId, "Теперь смело можешь отправлять мне картинки на проверку",TimeSpan.FromSeconds(2)));            
                foreach (var admin in admins)
                {
                    messages.Add(new MessageFlowItem(admin.UserChatId, $"{message.From.FirstName} {message.From.LastName} принял решение касательно запроса {auth.UserLastName} {auth.UserFirstName} - в доступе разрешить!"));
                    messages.Add(new MessageFlowItem(admin.UserChatId, $"Теперь нас {admins.Count()+1}!",TimeSpan.FromSeconds(2)));
                    messages.Add(new MessageFlowItem(admin.UserChatId, "BQADBAADgwMAAqKYZgABz5uEcP7gGQ0C",true, TimeSpan.FromSeconds(2)));
                }
                flow = messages;
                return new SendStickerModel(message.Chat.Id, "BQADBAAD3gQAAqKYZgAB12DvgaPftTgC");
            } else if (no.Any(a => a.Equals(text, StringComparison.OrdinalIgnoreCase)))
            {
                _dbService.GrantAuth(authId, false);
                _dbService.SetState(auth.UserId, "NoState");
                _dbService.SetState(message.From.Id, "NoState");
                messages.Add(new MessageFlowItem(auth.UserChatId, "Плохие новости! СУК-А направило мне решение"));
                messages.Add(new MessageFlowItem(auth.UserChatId, "В нем указано что тебе запрещено в доступе к сообщению", TimeSpan.FromSeconds(1)));
                messages.Add(new MessageFlowItem(auth.UserChatId, "BQADBAADYgEAAqIkSQPtYd2YcYlnzQI", true,TimeSpan.FromSeconds(1)));
                foreach (var admin in admins)
                {
                    messages.Add(new MessageFlowItem(admin.UserChatId, $"{message.From.FirstName} {message.From.LastName} принял решение отказать {auth.UserLastName} {auth.UserFirstName}"));
                    messages.Add(new MessageFlowItem(admin.UserChatId, "BQADBAADaQEAAqIkSQNakH2j7oO-wgI", true, TimeSpan.FromSeconds(2)));
                }
                flow = messages;
                return new SendStickerModel(message.Chat.Id, "BQADBAADaQEAAqIkSQNakH2j7oO-wgI");
            }
            else
            {
                messages.Add(new MessageFlowItem(message.Chat.Id, "BQADBAADkgMAAqKYZgABPhmkbMd0GJAC", true, TimeSpan.FromMilliseconds(500)));
                messages.Add(new MessageFlowItem(message.Chat.Id, "Очень спешу, давай скорей", true, TimeSpan.FromMilliseconds(500)));
                flow = messages;
                return new SendMessageModel(message.Chat.Id, "Хорош разглагольствовать, пиши да или нет");
            }
            
        }
        internal ISendItem _stateWaitAuth(string text, MessageModel message, out MessageFlow flow)
        {
            flow = null;
            return new SendMessageModel(message.Chat.Id, ".-.");
        }
        internal ISendItem _stateChoosePublic(string text, MessageModel message, out MessageFlow flow)
        {
            var walls = _dbService.GetWalls();
            var auths = _dbService.GetAuths(message.From.Id);
            var wall = walls.FirstOrDefault(f => f.Name == text);
            if (wall == null)
            {
                flow = null;               
                return new SendMessageModel(message.Chat.Id, "Ээ, что-то я не могу найти такое сообщество.\r\nУточни-ка...");
            }
            var check = auths.FirstOrDefault(f => f.WallId == wall.Id);
            if (check != null)
            {
                _dbService.SetState(message.From.Id, "NoState");
                if (check.Auth)
                {
                    flow = new MessageFlow() {new MessageFlowItem(message.Chat.Id, "BQADBAADWwMAAqKYZgAB3mljwfZMlVkC",true,TimeSpan.FromSeconds(1)) };
                    return new SendMessageModel(message.Chat.Id, "Ok, но вообщето я уже раньше тебя записывал в это сообщество, не помнишь?");
                }
                else
                {
                    flow = new MessageFlow(){new MessageFlowItem(message.Chat.Id, "BQADBAADMAMAAqKYZgABj5c3MVEUXD4C", true,TimeSpan.FromSeconds(1))};
                    return new SendMessageModel(message.Chat.Id, "Кстати да, ты уже раньше отправлял запрос на доступ к сообществу "+wall.Name+".\r\nПросто запасись терпением, наши администраторы ну очень занятые..");
                }
                
            }

            _dbService.SetState(message.From.Id, "wait-auth",wall.Id.ToString());
            _dbService.InsertAuthQuery(message.From.Id, message.Chat.Id,message.From.LastName,message.From.FirstName,message.From.Username,wall.Id);
            var auth = _dbService.GetAuth(message.From.Id, wall.Id);
            var admins = _dbService.GetWallAdmins(wall.Id);
            var messages = new List<SendMessageModel>();
            foreach (var admin in admins)
            {
                _dbService.SetState(admin.UserChatId, "grant-auth", auth.Id.ToString());
                messages.Add(new SendMessageModel(admin.UserChatId,$"Привет еще раз {admin.UserFirstName}!\r\n('{message.From.FirstName} {message.From.LastName}' запрашивает доступ к сообществу {wall.Name}.\r\n\r\nРазрешить ему?"));
            }

            flow = new MessageFlow()
            {
                new MessageFlowItem(message.Chat.Id, "Я написал заявление в Структурное Управление Кандидатов-Администраторов для разрешения дотсупа к твоему сообщесву", TimeSpan.FromSeconds(2)),
                new MessageFlowItem(message.Chat.Id,"Будем ждать их реакции...",TimeSpan.FromMilliseconds(600)),
                new MessageFlowItem(message.Chat.Id,"BQADBAADkgMAAqKYZgABPhmkbMd0GJAC",true,TimeSpan.FromMilliseconds(500))
            };
            return new SendMessageModel(message.Chat.Id, "Отлично, теперь осталась небольшая бюрократическая процедура...");
        }


    }
}