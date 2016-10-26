using System;

namespace TsabConsole.Actions
{
    internal class CleanAction: ITsabConsoleAction
    {
        public string ActionName { get; } = "clean";
        public string Descriptioin { get; } = "Отчистка даных стены из базы и хранилища, включая фотогафии";
        public string Syntax { get; } = "clean";
        public void Exec(string[] args)
        {
            Program.Context.CompareService.Clean();            
            Console.WriteLine("Удаление завршено.");
        }
    }
}
