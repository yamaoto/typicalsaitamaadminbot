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
            comparer.LoadPhots(Wall1);
        }

        [TestMethod]
        public void T4CheckPhoto1()
        {
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            var comparer = new CompareService(db, storage);
            comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "AgADAgADFagxG1lC7AzjPW88igO-ahXugQ0ABMVin79RXXfl4xcAAgI", 1, 1, Wall1));
        }
        //[TestMethod]
        //public void T4CheckPhoto2()
        //{
        //    var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
        //    var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
        //    var comparer = new CompareService(db, storage);
        //    comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "AgADAgADIqgxG1lC7AzCYYVsT8bh3n7hgQ0ABK14N__XJlCx4BoAAgI", 1, 1, Wall1));
        //}
        //[TestMethod]
        //public void T4CheckPhoto3()
        //{
        //    var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
        //    var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
        //    var comparer = new CompareService(db, storage);
        //    comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "blob", 1, 1, Wall1));
        //}
        //[TestMethod]
        //public void T4CheckPhoto4()
        //{
        //    var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
        //    var db = new DbService(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
        //    var comparer = new CompareService(db, storage);
        //    comparer.CheckPhoto(new CheckPhotoModel(Guid.NewGuid(), 1, "blob", 1, 1, Wall1));
        //}
    }
}
