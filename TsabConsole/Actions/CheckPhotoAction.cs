using System;
using System.Drawing;
using System.Drawing.Imaging;
using TsabSharedLib;

namespace TsabConsole.Actions
{
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
}