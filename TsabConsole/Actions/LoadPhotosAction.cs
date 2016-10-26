using System;

namespace TsabConsole.Actions
{
    internal class LoadPhotosAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "load-photos";
        public string Descriptioin { get; } = "Загрузка фотографий со стены";
        public string Syntax { get; } = "load-photos <wallId>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length == 2 && int.TryParse(args[1], out wallId))
            {
                Program.Context.CompareService.LoadPhotos(wallId);
                Console.WriteLine("Фотографии стены загружены.");
            }
            else
            {
                Console.WriteLine("Неверный формат команды.");
                Console.WriteLine("Синтаксис: {0}", Syntax);
            }
        }
    }
}