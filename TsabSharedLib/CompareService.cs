using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using kasthack.vksharp;
using kasthack.vksharp.DataTypes;
using kasthack.vksharp.DataTypes.Enums;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace TsabSharedLib
{
    public class CompareService
    {
        private readonly DbService _dbService;
        private readonly ImgComparer _comparer;
        private readonly ImgComparer _comparerThumb;
        private readonly CloudBlobContainer _imagesContainer;
        private readonly CloudBlobContainer _draftsContainer;

        protected IBotApi BotApi { get; set; }

        public CompareService(DbService dbService, CloudStorageAccount storage)
        {
            _dbService = dbService;
            var walls = _dbService.GetWalls().Select(s => s.Id);
            _comparer = new ImgComparer(ConfigStorage.CompareSize, ConfigStorage.CompareValue, ConfigStorage.CompareValue * 15, walls);
            _comparerThumb = new ImgComparer(ConfigStorage.OrderSize, ConfigStorage.OrderValue, ConfigStorage.OrderValue * 15, walls);
            var blobClient = storage.CreateCloudBlobClient();
            _imagesContainer = blobClient.GetContainerReference("images");
            _draftsContainer = blobClient.GetContainerReference("drafts");
            _checkAndClean();
            _checkFolders();
        }


        private string _baseBlobFolder;
        public string BaseBlobFolder => _baseBlobFolder ?? (_baseBlobFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blob"));

        private string _resizeBlobFolder;
        public string ResizeBlobFolder => _resizeBlobFolder ?? (_resizeBlobFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blob", ConfigStorage.CompareSize.ToString()));

        private string _rawBlobFolder;
        public string RawBlobFolder => _rawBlobFolder ?? (_rawBlobFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blob", "raw"));

        private void _checkAndClean()
        {
            if (!Directory.Exists(BaseBlobFolder))
            {
                Clean();
            }
        }
        private void _checkFolders()
        {
            var folders = new[]
            {
                BaseBlobFolder,
                ResizeBlobFolder,
                RawBlobFolder
            };
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }
        }

        public void LoadPhotos(int wallId)
        {
            var photos = _dbService.GetLoadedPhotos(wallId);
            const int maxTryCount = 5;
            foreach (var photo in photos.Where(w => !_comparer.CheckLoad(wallId, w.Blob)))
            {
                var tryCount = 0;
                while (tryCount < maxTryCount)
                {
                    try
                    {
                        using (var stream = new MemoryStream())
                        {
                            var localBlob = Path.Combine(ResizeBlobFolder, photo.Blob);
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
                        break;
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Blob: {0}", photo.Blob);
                        System.Diagnostics.Debug.WriteLine("Ul: {0}", photo.Url);
                        System.Diagnostics.Debug.WriteLine("Exception: {0}", e.Message);
                        System.Diagnostics.Debug.WriteLine("StackTrace: {0}", e.StackTrace);
                        tryCount++;
                    }
                }
            }
        }

        public string GetVkAuth(string state)
        {
            var url = $"https://oauth.vk.com/authorize?client_id={ConfigStorage.VkAppId}&display=page&redirect_uri={ConfigStorage.VkOauthRedirect}&scope=groups&state={state}&response_type=code&v=5.53";
            return url;
        }
        public string GetVkGroupAuth(string state, int group)
        {
            var url = $"https://oauth.vk.com/authorize?client_id={ConfigStorage.VkAppId}&display=page&redirect_uri={ConfigStorage.VkGroupOauthRedirect}&group_ids={group}&scope=photos&state={state}&response_type=code&v=5.53";
            return url;
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
                    count = _updateWall(wall.Id, offset, wall.LastItemId);

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

        private int _updateWall(int ownerId, int offset, long? lastItemId)
        {
            var vkClient = new Api();
            var get = vkClient.Wall.Get(ownerId, offset: offset, count: 100);
            get.Wait();
            var result = get.Result;
            if (result.Items.Length == 0)
            {
                return -1;
            }
            var listItems = new List<WallItemModel>();
            var listPhotos = new List<PhotoModel>();
            var isLast = false;
            long? lasUpdateItem = null;
            if (offset == 0 && result.Count > 0)
                lasUpdateItem = result.First().Id;
            foreach (var item in result)
            {
                try
                {
                    if (lastItemId == item.Id)
                    {
                        isLast = true;
                        break;
                    }
                    listItems.Add(new WallItemModel(ownerId, item.Id, $"http://vk.com/wall{ownerId}_{item.Id}"));
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
                        listPhotos.Add(new PhotoModel(ownerId, item.Id, src));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("photo {0}", item.Id);
                    }
                }
            }
            _dbService.InsertWallItems(listItems, lasUpdateItem, ownerId);
            _dbService.InsertPhotos(listPhotos);
            if (isLast)
                return -1;
            return result.Count;
        }

        public CheckPhotoResultModel CheckPhoto(CheckPhotoModel model)
        {
            if (!_comparer.CheckLoad(model.WallId))
            {

                throw new Exception("Ошибочка вышла, я еще не обновил стену сообщества. Сперва получи обновления сообщества и затем попробуй еще раз...");
                //LoadPhots(model.WallId);
            }
            CompareStrictResult compare = null;
            using (var inpuTStream = _draftsContainer.GetBlobReference(model.Blob).OpenRead())
            {
                using (var stream = new MemoryStream())
                {
                    inpuTStream.CopyTo(stream);
                    stream.Seek(0, 0);
                    var inputThumb = new ImgMapper(Image.FromStream(stream), ConfigStorage.OrderSize);
                    stream.Seek(0, 0);
                    var input = new ImgMapper(Image.FromStream(stream), ConfigStorage.CompareSize);
                    var order = _comparerThumb.Order(model.WallId, inputThumb, model.Blob);
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
            var urls = new List<string>();
            foreach (var item in items)
            {
                foreach (var photo in item)
                {
                    //tasks.Add(_loadWall(photo.Url));
                    urls.Add(photo.Url);
                }
            }
            var resultCollection = new ConcurrentBag<KeyValuePair<string, string>>();
            Parallel.ForEach(
                urls,
                new ParallelOptions { MaxDegreeOfParallelism = 10 }, url => resultCollection.Add(_loadWallSync(url)));

            var list = resultCollection.ToArray();
            if (list.Length > 0)
                _dbService.SetLoadedPhoto(list);
            _dbService.SetLoadedWall(wallId);
        }

        private KeyValuePair<string, string> _loadWallSync(string photoUrl)
        {
            var tryCount = 0;
            const int maxTry = 4;
            byte[] data;
            while (true)
            {
                try
                {
                    data = _loadFileSync(photoUrl);
                    break;
                }
                catch (Exception)
                {
                    tryCount++;
                    if (tryCount >= maxTry)
                        throw;
                }
            }
            var extr = _prepareImage(data);
            var id = Guid.NewGuid().ToString("N");
            var name = id + ".bmp";
            var nameRaw = id + ".jpg";
            _uploadBlob(name, extr);

            var localBlobRaw = Path.Combine(RawBlobFolder, nameRaw);
            if (!File.Exists(localBlobRaw))
            {
                using (var file = File.OpenWrite(localBlobRaw))
                    file.Write(data, 0, data.Length);
            }
            var localBlob = Path.Combine(ResizeBlobFolder, name);
            if (!File.Exists(localBlob))
            {
                using (var file = File.OpenWrite(localBlob))
                    file.Write(extr, 0, extr.Length);
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

        public void Clean()
        {
            Parallel.ForEach(_imagesContainer.ListBlobs().OfType<CloudBlob>(), x => (x).Delete());
            Parallel.ForEach(_draftsContainer.ListBlobs().OfType<CloudBlob>(), x => (x).Delete());
            _dbService.ClearAll();
        }

        public async Task Publish(int chatId,int messageId, ISearchResultItem item, int telegramUserId, int wallId, long albumId)
        {
            try
            {
                var vkClient = new Api();
                var tokens = _dbService.GetTokens(telegramUserId);
                foreach (var token in tokens)
                {
                    vkClient.AddToken(new Token(token));
                }
                var imageData = new WebClient().DownloadData(item.ImageUrl);
                byte[] jpegImageData = null;
                using (var stream = new MemoryStream(imageData))
                {
                    var image = Image.FromStream(stream);
                    using (var outStream = new MemoryStream())
                    {
                        image.Save(outStream, ImageFormat.Jpeg);
                        jpegImageData = outStream.GetBuffer();
                    }
                }
                var urlResult = await vkClient.Photos.GetUploadServer(albumId, wallId);
                var client = new HttpClient();
                var requestContent = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(jpegImageData);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                requestContent.Add(imageContent, "photo");
                var result = await client.PostAsync(urlResult.UploadUrl, requestContent);
                PhotoUploadResult uploadResult = null;
                using (var resultContent = await result.Content.ReadAsStreamAsync())
                {
                    using (var reader = new StreamReader(resultContent))
                    {
                        var jsonString = reader.ReadToEnd();
                        uploadResult = JsonConvert.DeserializeObject<PhotoUploadResult>(jsonString);
                    }
                }
                var photoSaveResult = await vkClient.Photos.Save(albumId, uploadResult.Server, uploadResult.PhotosList, uploadResult.Hash, groupId: wallId);
                var text = "";
                var photoAtt = new ObjectContentId(ContentType.Photo, photoSaveResult.First().Id, wallId);
                var postResult =
                    await
                        vkClient.Wall.Post(text, new ContentId[] { photoAtt }, ownerId: wallId, fromGroup: true, signed: false,
                            publishDate: new DateTimeOffset(DateTime.Now, TimeSpan.FromHours(1)));
            }
            catch (Exception e)
            {
                _dbService.SetMessageError(chatId, messageId, e.Message);
                throw;
            }
        }
    }
}
