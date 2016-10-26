using System;

namespace TsabConsole.Actions
{
    internal class LoadPhotosAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "load-photos";
        public string Descriptioin { get; } = "�������� ���������� �� �����";
        public string Syntax { get; } = "load-photos <wallId>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length == 2 && int.TryParse(args[1], out wallId))
            {
                Program.Context.CompareService.LoadPhotos(wallId);
                Console.WriteLine("���������� ����� ���������.");
            }
            else
            {
                Console.WriteLine("�������� ������ �������.");
                Console.WriteLine("���������: {0}", Syntax);
            }
        }
    }
}