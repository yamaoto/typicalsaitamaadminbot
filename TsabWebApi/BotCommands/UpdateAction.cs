using System;
using System.Linq;
using System.Threading.Tasks;
using TsabSharedLib;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class UpdateAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new string[0];
        private bool _isUpdating;
        public string CommandName { get; } = "/update";
        public string Description { get; } = "загрузка обновлений сообществ";
        private readonly DbService _dbService = BotService.GetDbService();

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            if (_isUpdating)
            {
                flow = null;
                return new SendMessageModel(message.Chat.Id, "Погоди чуточку, сейчас я как раз занимаюсь обновлением...");
            }
            _isUpdating = true;
            _updateTask(message);
            flow = null;
            return new SendMessageModel(message.Chat.Id, "Одну секунду...");
        }

        public ISendItem Message(string state,string text, MessageModel message, out MessageFlow flow)
        {
            throw new InvalidOperationException();
        }

        private Task _updateTask(MessageModel message)
        {
            return Task.Run(() => _updateSync(message));
        }

        private void _updateSync(MessageModel message)
        {
            var auths = _dbService.GetAuths(message.From.Id).Where(w => w.Auth);
            if (!auths.Any())
            {
                _context.BotMethods.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, $"Похоже что у тебя еще нет привязанных сообществ. Введи /public чтобы продолжить работу")).Wait();
            }
            var list = auths.Select(s => _dbService.GetWall(s.WallId));
            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (s) => _updateWallSync(message, s.Id, s.Name));
            _isUpdating = false;
        }

        private void _updateWallSync(MessageModel message, int wallId, string wallName)
        {
            _context.CompareService.UpdateWall(wallId);
            _context.BotMethods.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, $"Стена {wallName} обновлена, начинаю загрузку фотографий...")).Wait();
            try
            {
                _context.CompareService.LoadWall(wallId);
                _context.BotMethods.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, "Еще чуууть-чуть")).Wait();

            }
            catch (Exception e)
            {
                _context.BotMethods.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, e.InnerException.Message + e.InnerException.StackTrace)).Wait();
                throw;
            }
            try
            {
                _context.CompareService.LoadPhotos(wallId);
            }
            catch (Exception e)
            {
                _context.BotMethods.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, e.InnerException.Message + e.InnerException.StackTrace)).Wait();
                throw;
            }
            _context.BotMethods.BotMethod("sendMessage", new SendMessageModel(message.Chat.Id, $"Фотографии со стены {wallName} загружены. Все готово.")).Wait();
        }
    }
}