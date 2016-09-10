using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using TsabSharedLib;

namespace TsabConsole
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

    internal class UpdateWallAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "update-wall";
        public string Descriptioin { get; } = "Обновление стены";
        public string Syntax { get; } = "update-wall <wallId>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length>=2&&int.TryParse(args[1], out wallId))
            {
                Program.Context.CompareService.UpdateWall(wallId);
                Console.WriteLine("Обновление стены завршено.");
            }
            else
            {
                Console.WriteLine("Неверный формат команды.");
                Console.WriteLine("Синтаксис: {0}", Syntax);
            }
        }
    }

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
    internal class CheckPhotoAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "check-photo";
        public string Descriptioin { get; } = "Проверка фотографии";
        public string Syntax { get; } = "check-photo <wallId> <file>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length == 3 && int.TryParse(args[1], out wallId))
            {
                var file = args[2];
                if (!System.IO.File.Exists(file))
                {
                    Console.WriteLine("Файл не обнаружен.");
                }
                var name = Guid.NewGuid().ToString("N");
                var blockBlob = Program.Context.DraftsContainer.GetBlockBlobReference(name);
                blockBlob.Properties.ContentType = "image/jpg";
                var img = Image.FromFile(file);
                var tmpFile = name + ".jpg";
                img.Save(tmpFile,ImageFormat.Jpeg);
                var data = System.IO.File.ReadAllBytes(tmpFile);
                blockBlob.UploadFromByteArray(data, 0, data.Length);
                System.IO.File.Delete(tmpFile);
                
                var model = new CheckPhotoModel() {Blob = name, WallId = wallId};
                var result = Program.Context.CompareService.CheckPhoto(model);
                Console.WriteLine("Результат: '{0}'\r\nОбнаруженный объект: '{1}'", result.Value, result.FoundBlob);
                if (!string.IsNullOrEmpty(result.FoundBlob))
                {
                    var found = Program.Context.DbService.GetWallItemByBlob(wallId, result.FoundBlob);
                    Console.WriteLine(found.Url);
                    
                }
                Console.WriteLine("Фотографии стены загружены.");
            }
            else
            {
                Console.WriteLine("Неверный формат команды.");
                Console.WriteLine("Синтаксис: {0}", Syntax);
            }
        }
    }
    internal class ListWallAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "list-wall";
        public string Descriptioin { get; } = "Вывести список стен";
        public string Syntax { get; } = "list-wall";
        public void Exec(string[] args)
        {
            var list = Program.Context.DbService.GetWalls();
            foreach (var wall in list)
            {
                Console.WriteLine("id: '{0}'; name: '{1}'", wall.Id, wall.Name);
            }
        }
    }
    internal class ListAdminAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "list-admin";
        public string Descriptioin { get; } = "Вывод администраторов стены";
        public string Syntax { get; } = "list-admin <wallId>";

        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length == 2 && int.TryParse(args[1], out wallId))
            {

                var list = Program.Context.DbService.GetWallAdmins(wallId);
                foreach (var wall in list)
                {
                    Console.WriteLine("id: '{0}'; name: '{1}'", wall.UserId, wall.UserFirstName + " " + wall.UserLastName);
                }
            }
            else
            {
                Console.WriteLine("Неверный формат команды.");
                Console.WriteLine("Синтаксис: {0}", Syntax);
            }
        }
    }
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
