using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using TsabSharedLib;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        public const int Wall1 = -109339484;
        [TestMethod]
        public void T1UpdateWall()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            comparer.UpdateWall(Wall1);
        }

        [TestMethod]
        public void T2LoadWall()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            comparer.LoadWall(Wall1);
        }

        [TestMethod]
        public void T3LoadPhots()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            comparer.LoadPhotos(Wall1);
        }

        [TestMethod]
        public void T4CheckPhoto1()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            var result = comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "AgADAgADFagxG1lC7AzjPW88igO-ahXugQ0ABMVin79RXXfl4xcAAgI",  Wall1));
            Assert.IsNotNull(result.FoundBlob);
            Assert.AreEqual("5ebfc58d904040db909c016a956089a3.bmp",result.FoundBlob);
        }
        [TestMethod]
        public void T4CheckPhoto2()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            var result = comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "AgADAgADIqgxG1lC7AzCYYVsT8bh3n7hgQ0ABK14N__XJlCx4BoAAgI", Wall1));
            Assert.IsNull(result.FoundBlob);
        }
        [TestMethod]
        public void T4CheckPhoto3()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            var result = comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "AgADAgADH6gxG1lC7AzgK2R_r8GXcZsOcQ0ABFedzRp24PPE0JEBAAEC", Wall1));
            Assert.IsNotNull(result.FoundBlob);
            Assert.AreEqual("0efc841366234cb1ab9ed07c29dfb572.bmp", result.FoundBlob);
        }
        [TestMethod]
        public void T4CheckPhoto4()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "AgADAgADF6gxG1lC7AxPEyI89z8GqJLTgQ0ABI8FKTNjEZVKGBoAAgI", Wall1));
        }
    }
}
