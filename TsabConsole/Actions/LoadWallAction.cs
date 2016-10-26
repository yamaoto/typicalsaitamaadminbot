using System;

namespace TsabConsole.Actions
{
    internal class LoadWallAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "load-wall";
        public string Descriptioin { get; } = "Загрузка стены";
        public string Syntax { get; } = "load-wall <wallId>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length == 2 && int.TryParse(args[1], out wallId))
            {
                Program.Context.CompareService.LoadWall(wallId);
                Console.WriteLine("Загрузка стены завршена.");
            }
            else
            {
                Console.WriteLine("Неверный формат команды.");
                Console.WriteLine("Синтаксис: {0}", Syntax);
            }
        }
    }
}