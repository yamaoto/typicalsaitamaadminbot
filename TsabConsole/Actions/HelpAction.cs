using System;

namespace TsabConsole.Actions
{
    internal class HelpAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "help";
        public string Descriptioin { get; } = "Вывод справочной информации";
        public string Syntax { get; } = "help <action>";
        public void Exec(string[] args)
        {
            if (args.Length == 2)
            {
                var actionName = args[1];
                if (!Program.Actions.ContainsKey(actionName))
                {
                    Console.WriteLine("Неизвестная команда");
                }
                var action = Program.Actions[actionName];
                Console.WriteLine("{0}\t{1}", action.ActionName, action.Descriptioin);
                Console.WriteLine("Синтаксис: {0}", action.Syntax);
            }
            else
            {
                foreach (var action in Program.Actions.Values)
                {
                    Console.WriteLine("{0}\t{1}",action.ActionName, action.Descriptioin);
                }
            }
        }
    }
}