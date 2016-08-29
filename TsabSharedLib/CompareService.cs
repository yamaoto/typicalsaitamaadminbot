using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using kasthack.vksharp;
using kasthack.vksharp.DataTypes.Enums;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TsabSharedLib
{
    public class CompareService
    {
        private readonly  DbService _dbService;
        private readonly Api _vk;
        private readonly ImgComparer _comparer;
        private readonly ImgComparer _comparerThumb;
        public const int ThumbSize = 5;
        private readonly CloudBlobContainer _imagesContainer;
        private readonly CloudBlobContainer _draftsContainer;
        public CompareService(DbService dbService, CloudStorageAccount storage)
        {
            _dbService = dbService;
            _vk = new Api();
            var walls = _dbService.GetWalls().Select(s => s.Id);
            _comparer = new ImgComparer(ConfigStorage.CompareSize, ConfigStorage.CompareValue, walls);
            _comparerThumb = new ImgComparer(ThumbSize, ThumbSize, walls);
            var blobClient = storage.CreateCloudBlobClient();
            _imagesContainer = blobClient.GetContainerReference("images");
            _draftsContainer = blobClient.GetContainerReference("drafts");
            var blobs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blob");
            if (!Directory.Exists(blobs))
                Directory.CreateDirectory(blobs);
        }

        public void LoadPhots(int wallId)
        {
            var photos = _dbService.GetLoadedPhotos(wallId);
            foreach (var photo in photos.Where(w => !_comparer.CheckLoad(wallId, w.Blob)))
            {
                using (var stream = new MemoryStream())
                {
                    var localBlob = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blob", photo.Blob);
                    if (File.Exists(localBlob))
                    {
                        using (var file = File.OpenRead(localBlob))
                            file.CopyTo(stream);
                    }
                    else
                    {
                        using (var blobStream = _imagesContainer.GetBlobReference(photo.Blob).OpenRead())
                            blobStream.CopyTo(stream);
                        stream.Seek(0, 0);
                        using (var file = File.OpenWrite(localBlob))
                            stream.CopyTo(file);
                    }
                    stream.Seek(0, 0);
                    _comparer.Load(wallId, photo.Blob, stream);
                    stream.Seek(0, 0);
                    _comparerThumb.Load(wallId, photo.Blob, stream);
                }
            }
        }

        public void UpdateWall(int wallId)
        {
            var wall = _dbService.GetWall(wallId);
            if (wall == null)
                throw new Exception();
            int offset = 0;
            int count = 0;
            bool retry;
            int retryCount = 0;
            while (true)
            {

                try
                {
                    count = _updateWall(wall.Id, offset);
                    retry = false;
                    if (count == -1)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    retry = true;
                }
                if (retry)
                {
                    if (retryCount > 3 && retryCount <= 4)
                    {
                        retryCount++;
                        Thread.Sleep(60000);
                        continue;
                    }
                    else if (retryCount >= 5)
                    {
                        throw new Exception();
                    }
                    retryCount++;
                    Thread.Sleep(300);
                    continue;
                }

                if (offset >= count)
                    break;
                offset += 100;
                Thread.Sleep(300);
            }
            _dbService.SetWallUpdate(wallId);
        }

        private int _updateWall(int ownerId, int offset)
        {
            var get = _vk.Wall.Get(ownerId, offset: offset, count: 100);
            get.Wait();
            var result = get.Result;
            foreach (var item in result)
            {
                if (_dbService.CheckWallItem(ownerId, item.Id))
                {
                    return -1;
                }
                try
                {
                    _dbService.InsertWallItem(new WallItemModel(ownerId, item.Id, $"http://vk.com/wall{ownerId}_{item.Id}"));
                }
                catch (Exception)
                {
                    Console.WriteLine("wall item {0}", item.Id);
                    throw;
                }
                if (item.Attachments == null)
                    continue;
                foreach (var photo in item.Attachments.Where(w => w.Type == ContentType.Photo).Select(s => s.Photo))
                {
                    try
                    {
                        string src = null;
                        if (!string.IsNullOrEmpty(photo.Photo2560))
                            src = photo.Photo2560;
                        else if (!string.IsNullOrEmpty(photo.Photo1280))
                            src = photo.Photo1280;
                        if (!string.IsNullOrEmpty(photo.Photo807))
                            src = photo.Photo807;
                        if (!string.IsNullOrEmpty(photo.Photo604))
                            src = photo.Photo604;
                        if (src == null)
                            continue;
                        _dbService.InsertPhoto(new PhotoModel(ownerId, item.Id, src));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("photo {0}", item.Id);
                    }
                }
            }
            return result.Count;
        }

        public CheckPhotoResultModel CheckPhoto(CheckPhotoModel model)
        {
                if (!_comparer.CheckLoad(model.WallId))
                {
                    LoadPhots(model.WallId);
                }
                CompareStrictResult compare = null;
                using (var inpuTStream = _draftsContainer.GetBlobReference(model.Blob).OpenRead())
                {
                    using (var stream = new MemoryStream())
                    {
                        inpuTStream.CopyTo(stream);
                        stream.Seek(0, 0);
                        var inputThumb = new ImgMapper(Image.FromStream(stream), ThumbSize);
                        stream.Seek(0, 0);
                        var input = new ImgMapper(Image.FromStream(stream), ConfigStorage.CompareSize);
                        var order = _comparerThumb.Order(model.WallId, inputThumb, model.Blob, model.Number, model.Total);
                        compare = _comparer.Compare(model.WallId, input, model.Blob, order);
                    }
                }
                return new CheckPhotoResultModel(compare.FoundBlob, compare.Value);
            
        }

        public void LoadWall(int wallId)
        {
            var wall = _dbService.GetWall(wallId);
            if (wall == null)
                throw new Exception();
            var items = _dbService.GetNotLoadedItems(wallId);
            var tasks = new List<Task<KeyValuePair<string, string>>>();
            foreach (var item in items)
            {
                foreach (var photo in item)
                {
                    tasks.Add(_loadWall(photo.Url));
                }
            }
            var taskAr = tasks.ToArray();
            Task.WaitAll(taskAr);
            var list = taskAr.Where(w => w.Status != TaskStatus.Faulted).Select(task => task.Result).ToArray();
            if (list.Length > 0)
                _dbService.SetLoadedPhoto(list);
            _dbService.SetLoadedWall(wallId);
        }

        private Task<KeyValuePair<string, string>> _loadWall(string photoUrl)
        {
            return Task.Run(() => _loadWallSync(photoUrl));
        }
        private KeyValuePair<string, string> _loadWallSync(string photoUrl)
        {
            var data = _loadFileSync(photoUrl);
            var extr = _prepareImage(data);
            var name = Guid.NewGuid().ToString("N") + ".bmp";
            _uploadBlob(name, extr);

            var localBlob = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blob", name);
            if (!File.Exists(localBlob))
            {
                using (var file = File.OpenWrite(localBlob))
                    file.Write(data, 0, data.Length);
            }
            return new KeyValuePair<string, string>(photoUrl, name);
        }

        private void _uploadBlob(string name, byte[] data)
        {

            var blockBlob = _imagesContainer.GetBlockBlobReference(name);
            if (blockBlob.Exists())
                return;
            blockBlob.Properties.ContentType = "image/x-ms-bmp";
            blockBlob.UploadFromByteArray(data, 0, data.Length);
        }
        private byte[] _prepareImage(byte[] data)
        {
            Image img;
            using (var stream = new MemoryStream(data))
            {
                img = Image.FromStream(stream);
            }
            var prepared = new Bitmap(img, ConfigStorage.CompareSize, ConfigStorage.CompareSize);
            byte[] result = null;
            using (var stream = new MemoryStream())
            {
                prepared.Save(stream, ImageFormat.Bmp);
                result = stream.GetBuffer();
            }
            return result;
        }


        private byte[] _loadFileSync(string url)
        {
            var cl = new WebClient();
            return cl.DownloadData(url);
        }
    }
}
