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
        private SqlConnection _connection;
        private SqlConnection Connection => _connection?? (_connection = new SqlConnection(_connectionName));
        private readonly string _connectionName ;
        public DbService(string connectionName)
        {
            _connectionName = connectionName;
        }

        public WallModel[] GetWalls()
        {
            const string sql = "SELECT * FROM [dbo].[Wall]";
            var result = Connection.Query<WallModel>(sql);
            return result.ToArray();
        }

        public WallModel GetWall(int id)
        {
            const string sql = "SELECT * FROM [dbo].[Wall] WHERE [Id] = @Id";
            var result = Connection.QueryFirstOrDefault<WallModel>(sql, new { Id = id });
            return result;
        }

        public void InsertWallItem(WallItemModel item)
        {
            Connection.Execute("[dbo].[InsertWallItem]", item, commandType: CommandType.StoredProcedure);
        }

        public void SetLoadedWallItem(long id)
        {
            const string sql = @"UPDATE [dbo].[WallItem] SET [Loaded] = 1 WHERE [Id]=@Id";
            Connection.Execute(sql, new { Id = id });
        }


        public void InsertPhoto(PhotoModel item)
        {
            Connection.Execute("[dbo].[InsertPhoto]", item, commandType: CommandType.StoredProcedure);
        }

        public void SetLoadedPhoto(string url, string blob)
        {
            const string sql = "UPDATE [dbo].[Photo] SET [Loaded] = 1, [Blob]=@Blob WHERE [Url]=@Url";
            Connection.Execute(sql, new { Url = url, Blob = blob });
        }

        public void SetLoadedPhoto(KeyValuePair<string,string>[] values)
        {
            const string sql = "UPDATE [dbo].[Photo] SET [Loaded] = 1, [Blob]='{0}' WHERE [Url]='{1}';\r\n";
            var query = "";
            foreach (var pair in values)
            {
                query += string.Format(sql, pair.Value, pair.Key);
            }
            Connection.Execute(query);
        }

        public void SetWallUpdate(int id)
        {
            const string sql = "UPDATE [dbo].[Wall] SET [LastUpdate] = GETDATE() WHERE [Id]=@Id";
            Connection.Execute(sql, new { Id = id });
        }

        public bool CheckWallItem(int ownerId, long id)
        {
            const string sql = "SELECT * FROM [dbo].[WallItem] WHERE [WallId]=@WallId AND [Id] = @Id";
            var item = Connection.QueryFirstOrDefault(sql, new { WallId = ownerId, Id = id });
            return item != null;
        }

        public IEnumerable<IGrouping<long, PhotoModel>> GetNotLoadedItems(int ownerId)
        {
            var items = Connection.Query<PhotoModel>("[dbo].[GetNotLoadedItems]", new { WallId = ownerId }, commandType: CommandType.StoredProcedure);
            var grouped = items.GroupBy(g => g.WallItemId);
            return grouped;
        }

        public void UpdateState(int userId, string lastName, string firstName, string username, int chatId)
        {
            Connection.Execute("[dbo].[QuickUpdateState]",
            new {
                UserId=userId,
                UserLastName=lastName,
                UserFirstName=firstName,
                Username=username,
                UserChatId=chatId
            }, commandType: CommandType.StoredProcedure);
        }

        public void SetState(int id, string state,string stateParams=null)
        {
            Connection.Execute("[dbo].[SetState]",
            new
            {
                UserId = id,
                State = state,
                StateParams=stateParams
            }, commandType: CommandType.StoredProcedure);
        }

        public StateModel GetState(int id)
        {
            const string sql = "SELECT * FROM[dbo].[State] WHERE [UserId] = @Id";
            var result = Connection.QueryFirstOrDefault<StateModel>(sql, new { Id = id });
            return result;
        }

        public void InsertAuthQuery(int userId,int userChatId,string userLastName,string userFirstName,string username, int wallId)
        {
            Connection.Execute("[dbo].[InsertAuthQuery]",
            new
            {
                UserId = userId,
                UserChatId=userChatId,
                UserLastName=userLastName,
                UserFirstName=userFirstName,
                Username=username,
                WallId = wallId
            }, commandType: CommandType.StoredProcedure);
        }

        public AuthModel[] GetAuths(int userId)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [UserId]=@UserId";
            var result = Connection.Query<AuthModel>(sql,new {UserId=userId});
            return result.ToArray();
        }

        public AuthModel[] GetWallAdmins(int wallId)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [WallId]=@WallId AND [Auth]=1";
            var result = Connection.Query<AuthModel>(sql, new { WallId = wallId });
            return result.ToArray();
        }

        public AuthModel GetAuth(int userId, int wallId)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [UserId]=@UserId AND [WallId]=@WallId AND [Auth]=0";
            var result = Connection.QueryFirstOrDefault<AuthModel>(sql, new { UserId = userId, WallId = wallId });
            return result;
        }
        public AuthModel GetAuth(Guid id)
        {
            const string sql = "SELECT * FROM [dbo].[Auth] WHERE [Id]=@Id";
            var result = Connection.QueryFirstOrDefault<AuthModel>(sql, new { Id = id});
            return result;
        }

        public void GrantAuth(Guid id, bool grant)
        {
            const string sql = "UPDATE [dbo].[Auth] SET [Auth] = @Grant, [Solved]=1 WHERE [Id]=@Id";
            Connection.Execute(sql, new { Id = id, Grant= grant });
        }

        public string[] GetWorkers()
        {
            return Connection.Query<string>("SELECT [Name] FROM[dbo].[Worker]").ToArray();
        }

        public Guid InsertCompare(string inputBlob,int authorId,int authorChatId,string authorLastName,string authorFirstName,string algorithm,int workers, int wallId)
        {
            var id = Connection.ExecuteScalar<Guid>("[dbo].[InsertCompare]",
            new
            {
                InputBlob=inputBlob,
                AuthorId=authorId,
                AuthorChatId= authorChatId,
                AuthorLastName =authorLastName,
                AuthorFirstName=authorFirstName,
                Algorithm=algorithm,
                Workers=workers,
                WallId=wallId
            }, commandType: CommandType.StoredProcedure);
            return id;
        }

        public CompareModel GetCompare(Guid id)
        {
            const string sql = "SELECT * FROM [dbo].[Compare] WHERE [Id]=@Id";
            var result = Connection.QueryFirstOrDefault<CompareModel>(sql, new { Id = id });
            return result;
        }

        public void CloseCompare(Guid id, string foundBlob=null,string error=null, int? compareValue=null)
        {
            Connection.Execute("[dbo].[CloseCompare]",
            new
            {
                Id=id,
                FoundBlob=foundBlob,
                Error=error,
                CompareValue= compareValue
            }, commandType: CommandType.StoredProcedure);
        }

        public WallItemModel GetWallItemByBlob(int wallId,string blob)
        {
            var result = Connection.QueryFirstOrDefault<WallItemModel>("[dbo].[GetWallItemByBlob]", new { WallId=wallId,Blob=blob },commandType:CommandType.StoredProcedure);
            return result;
        }

        public void SetLoadedWall(int wallId)
        {
            const string sql = @"UPDATE [dbo].[WallItem] SET [Loaded] = 1 WHERE [WallId]=@WallId";
            Connection.Execute(sql, new { WallId = wallId });
        }

        public PhotoModel[] GetLoadedPhotos(int wallId)
        {
            const string sql = "SELECT * FROM [dbo].[Photo] WHERE [WallId]=@WallId";
            var result = Connection.Query<PhotoModel>(sql,new {WallId=wallId}).ToArray();
            return result;
        }

        public bool CheckMessage(int chatId, int messageId, string text=null)
        {
            return Connection.ExecuteScalar<bool>("[dbo].[CheckMessage]",
            new
            {
               ChatId=chatId,
               MessageId=messageId,
               Text=text
            }, commandType: CommandType.StoredProcedure);
        }
    }
}