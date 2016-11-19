using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;

namespace TsabSharedLib
{
    public class DbService
    {
        private SqlConnection Connection()
        {
            return new SqlConnection(_connectionName);
        }
        private readonly string _connectionName;
        public DbService(string connectionName)
        {
            _connectionName = connectionName;
        }

        public WallModel[] GetWalls()
        {
            const string sql = "SELECT * FROM [dbo].[Wall]";
            IEnumerable<WallModel> result;
            using (var connection = Connection())
            {
                result = connection.Query<WallModel>(sql);
            }
            return result.ToArray();
        }

        public WallModel GetWall(int id)
        {
            const string sql = "SELECT * FROM [dbo].[Wall] WHERE [Id] = @Id";
            WallModel result;
            using (var connection = Connection())
            {
                result = connection.QueryFirstOrDefault<WallModel>(sql, new { Id = id });
            }
            return result;
        }

        public void InsertWallItem(WallItemModel item)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[InsertWallItem]", item, commandType: CommandType.StoredProcedure);
            }
        }

        public void SetLoadedWallItem(long id)
        {
            const string sql = @"UPDATE [dbo].[WallItem] SET [Loaded] = 1 WHERE [Id]=@Id";
            using (var connection = Connection())
            {
                connection.Execute(sql, new { Id = id });
            }
        }


        public void InsertPhoto(PhotoModel item)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[InsertPhoto]", item, commandType: CommandType.StoredProcedure);
            }
        }

        public void SetLoadedPhoto(string url, string blob)
        {
            const string sql = "UPDATE [dbo].[Photo] SET [Loaded] = 1, [Blob]=@Blob WHERE [Url]=@Url";
            using (var connection = Connection())
            {
                connection.Execute(sql, new { Url = url, Blob = blob });
            }
        }

        public void SetLoadedPhoto(KeyValuePair<string, string>[] values)
        {
            const string sql = "UPDATE [dbo].[Photo] SET [Loaded] = 1, [Blob]='{0}' WHERE [Url]='{1}';\r\n";
            var query = "";
            foreach (var pair in values)
            {
                query += string.Format(sql, pair.Value, pair.Key);
            }
            using (var connection = Connection())
            {
                connection.Execute(query, commandTimeout: 0);
            }
        }

        public void SetWallUpdate(int id)
        {
            const string sql = "UPDATE [dbo].[Wall] SET [LastUpdate] = GETDATE() WHERE [Id]=@Id";
            using (var connection = Connection())
            {
                connection.Execute(sql, new { Id = id });
            }
        }

        public bool CheckWallItem(int ownerId, long id)
        {
            const string sql = "SELECT * FROM [dbo].[WallItem] WHERE [WallId]=@WallId AND [Id] = @Id";
            bool result;
            using (var connection = Connection())
            {
                var item = connection.QueryFirstOrDefault(sql, new { WallId = ownerId, Id = id });
                result = item != null;
            }
            return result;
        }

        public IEnumerable<IGrouping<long, PhotoModel>> GetNotLoadedItems(int ownerId)
        {
            IEnumerable<PhotoModel> items;
            using (var connection = Connection())
            {
                items = connection.Query<PhotoModel>("[dbo].[GetNotLoadedItems]", new { WallId = ownerId }, commandType: CommandType.StoredProcedure);
            }
            var grouped = items.GroupBy(g => g.WallItemId);
            return grouped;
        }

        public void UpdateState(int userId, string lastName, string firstName, string username, int chatId)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[QuickUpdateState]",
                new
                {
                    UserId = userId,
                    UserLastName = lastName,
                    UserFirstName = firstName,
                    Username = username,
                    UserChatId = chatId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public void SetState(int userId,int chatId, string state="NoState", string stateParams = null)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[SetState]",
                new
                {
                    UserId = userId,
                    ChatId= chatId,
                    State = state,
                    StateParams = stateParams
                }, commandType: CommandType.StoredProcedure);
            }

        }

        public StateModel GetState(int id)
        {
            const string sql = "SELECT * FROM[dbo].[State] WHERE [UserId] = @Id";
            StateModel result;
            using (var connection = Connection())
            {
                result = connection.QueryFirstOrDefault<StateModel>(sql, new { Id = id });
            }
            return result;
        }

        public void InsertAuthQuery(int userId, int userChatId, string userLastName, string userFirstName, string username, int wallId)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[InsertAuthQuery]",
                new
                {
                    UserId = userId,
                    UserChatId = userChatId,
                    UserLastName = userLastName,
                    UserFirstName = userFirstName,
                    Username = username,
                    WallId = wallId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public AuthModel[] GetAuths(int userId)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [UserId]=@UserId";
            IEnumerable<AuthModel> result;
            using (var connection = Connection())
            {
                result = connection.Query<AuthModel>(sql, new { UserId = userId });
            }
            return result.ToArray();
        }

        public AuthModel[] GetWallAdmins(int wallId)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [WallId]=@WallId AND [Auth]=1";
            IEnumerable<AuthModel> result;
            using (var connection = Connection())
            {
                result = connection.Query<AuthModel>(sql, new { WallId = wallId });
            }
            return result.ToArray();
        }

        public AuthModel GetAuth(int userId, int wallId)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [UserId]=@UserId AND [WallId]=@WallId AND [Auth]=0";
            AuthModel result;
            using (var connection = Connection())
            {
                result = connection.QueryFirstOrDefault<AuthModel>(sql, new { UserId = userId, WallId = wallId });
            }
            return result;
        }
        public AuthModel GetAuth(Guid id)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [Id]=@Id";
            AuthModel result;
            using (var connection = Connection())
            {
                result = connection.QueryFirstOrDefault<AuthModel>(sql, new { Id = id });
            }
            return result;
        }

        public void GrantAuth(Guid id, bool grant)
        {
            const string sql = "UPDATE [dbo].[Auth] SET [Auth] = @Grant, [Solved]=1 WHERE [Id]=@Id";
            using (var connection = Connection())
            {
                connection.Execute(sql, new { Id = id, Grant = grant });
            }
        }


        public Guid InsertCompare(string inputBlob, int authorId, int authorChatId, string authorLastName, string authorFirstName, string algorithm, int workers, int wallId)
        {
            Guid id;
            using (var connection = Connection())
            {
                id = connection.ExecuteScalar<Guid>("[dbo].[InsertCompare]",
                    new
                    {
                        InputBlob = inputBlob,
                        AuthorId = authorId,
                        AuthorChatId = authorChatId,
                        AuthorLastName = authorLastName,
                        AuthorFirstName = authorFirstName,
                        Algorithm = algorithm,
                        Workers = workers,
                        WallId = wallId
                    }, commandType: CommandType.StoredProcedure);
            }
            return id;
        }

        public CompareModel GetCompare(Guid id)
        {
            const string sql = "SELECT * FROM [dbo].[Compare] WHERE [Id]=@Id";
            CompareModel result;
            using (var connection = Connection())
            {
                result = connection.QueryFirstOrDefault<CompareModel>(sql, new { Id = id });
            }
            return result;
        }

        public void CloseCompare(Guid id, string foundBlob = null, string error = null, int? compareValue = null)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[CloseCompare]",
            new
            {
                Id = id,
                FoundBlob = foundBlob,
                Error = error,
                CompareValue = compareValue
            }, commandType: CommandType.StoredProcedure);
            }
        }

        public WallItemModel GetWallItemByBlob(int wallId, string blob)
        {
            WallItemModel result;
            using (var connection = Connection())
            {
                result = connection.QueryFirstOrDefault<WallItemModel>("[dbo].[GetWallItemByBlob]", new { WallId = wallId, Blob = blob }, commandType: CommandType.StoredProcedure);
            }
            return result;
        }

        public void SetLoadedWall(int wallId)
        {
            const string sql = @"UPDATE [dbo].[WallItem] SET [Loaded] = 1 WHERE [WallId]=@WallId";
            using (var connection = Connection())
            {
                connection.Execute(sql, new { WallId = wallId });
            }
        }

        public PhotoModel[] GetLoadedPhotos(int wallId)
        {
            const string sql = "SELECT * FROM [dbo].[Photo] WHERE [WallId]=@WallId";
            PhotoModel[] result;
            using (var connection = Connection())
            {
                result = connection.Query<PhotoModel>(sql, new { WallId = wallId }).ToArray();
            }

            return result;
        }

        public bool CheckMessage(int chatId, int messageId, string text = null, string photo = null, string json = null)
        {
            bool result;
            using (var connection = Connection())
            {
                result = connection.ExecuteScalar<bool>("[dbo].[CheckMessage]",
                    new
                    {
                        ChatId = chatId,
                        MessageId = messageId,
                        Text = text,
                        Photo = photo,
                        Json = json
                    }, commandType: CommandType.StoredProcedure);
            }
            return result;
        }
        public void SetMessageError(int chatId, int messageId, string error)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[SetMessageError]",
                new
                {
                    ChatId = chatId,
                    MessageId = messageId,
                    Error = error
                }, commandType: CommandType.StoredProcedure);
            }
        }
        public void ClearAll()
        {
            using (var connection = Connection())
            {
                connection.Execute(@"
DELETE FROM [dbo].[Compare];
DELETE FROM [dbo].[Photo];
DELETE FROM [dbo].[WallItem];
DELETE FROM [dbo].[Compare];
DELETE FROM [dbo].[Message];
UPDATE [dbo].[Wall]
SET [LastUpdate]= NULL,
[LastItemId]=NULL;
");
            }
        }

        public void InsertWallItems(IEnumerable<WallItemModel> items, long? lastItemId, int wallId)
        {
            var sql = "";
            if (items != null && items.Any())
            {
                var insert = "INSERT INTO [dbo].[WallItem]([WallId],[Id],[Url],[Loaded]) VALUES";
                var itemsSql = items.Select(s => $"({s.WallId},{s.Id},'{s.Url}',0)");
                var itemsSqlText = string.Join(",", itemsSql);
                sql = insert + itemsSqlText;
            }
            if (lastItemId.HasValue)
            {
                var lastItemIdSql = $"UPDATE [dbo].[Wall] SET [LastItemId] = {lastItemId} WHERE [Id]={wallId}";
                sql += ";\r\n" + lastItemIdSql;
            }
            if (!string.IsNullOrEmpty(sql))
            {
                using (var connection = Connection())
                {
                    connection.Execute(sql, commandTimeout: 0);
                }
            }
        }

        public void InsertPhotos(IEnumerable<PhotoModel> items)
        {
            if (items == null || !items.Any())
                return;
            var insert = "INSERT INTO [dbo].[Photo]([WallId],[WallItemId],[Url],[Blob],[Loaded]) VALUES";
            var itemsSql = items.Select(s => $"({s.WallId},{s.WallItemId},'{s.Url}','{s.Blob}',0)");
            var itemsSqlText = string.Join(",", itemsSql);
            var sql = insert + itemsSqlText;
            using (var connection = Connection())
            {
                connection.Execute(sql, commandTimeout: 0);
            }

        }

        public void SetVkUser(Guid id, int telegramUserId,bool group,int? groupId)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[SetVkUser]", new { Id = id, TelegramUserId = telegramUserId,Group = group,GroupId = groupId
                },commandType:CommandType.StoredProcedure);
            }
        }
        public void UpdateVkUser(Guid id, long? userId=null, string code = null,string token = null, DateTime? expires=null,bool group = false,int? groupId=null)
        {
            using (var connection = Connection())
            {
                connection.Execute("[dbo].[SetVkUser]", 
                    new
                    {
                        Id = id,
                        UserId= userId,
                        Code =code,
                        Token=token,
                        Expires=expires,
                        Group=group,
                        GroupId=groupId
                    },
                    commandType: CommandType.StoredProcedure);
            }
        }

        public int GetTelegramUserId(Guid id)
        {
            int result;
            using (var connection = Connection())
            {
                result = connection.ExecuteScalar<int>("SELECT TOP(1) [TelegramUserId] FROM [VkUser] WHERE [Id] = @Id", new {Id = id});
            }
            return result;
        }

        public string[] GetTokens(int telegramUserId)
        {
            const string sql = "SELECT [Token] FROM [dbo].[VkUser] WHERE [TelegramUserId]=@TelegramUserId AND GETDATE()<[Expires] AND [Token] IS NOT NULL";
            string[] result;
            using (var connection = Connection())
            {
                result = connection.Query<string>(sql, new { TelegramUserId = telegramUserId }).ToArray();
            }
            return result;
        }

        public VkUser GetToken(Guid id)
        {
            const string sql = "SELECT * FROM [dbo].[VkUser] WHERE [Id] = @Id";
            VkUser result;
            using (var connection = Connection())
            {
                result = connection.Query<VkUser>(sql, new { Id = id }).FirstOrDefault();
            }
            return result;
        }
    }
}