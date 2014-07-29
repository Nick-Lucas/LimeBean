using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LimeBean.Tests {

    public abstract class MariaDbFixture {
        IDbConnection _conn;
        internal IDatabaseDetails _details;
        internal IDatabaseAccess _db;

        [TestFixtureSetUp]
        public void TestFixtureSetUp() {
            _conn = new MySqlConnection("server=localhost; uid=root; pwd=qwerty");
            _conn.Open();
            _details = new MariaDbDetails();
        }

        [SetUp]
        public virtual void SetUp() {
            const string dbname = "lime_bean_tests";
            _db = new DatabaseAccess(_conn, _details);
            _db.Exec("set sql_mode=STRICT_TRANS_TABLES");
            _db.Exec("drop database if exists " + dbname);
            _db.Exec("create database " + dbname);
            _db.Exec("use " + dbname);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown() {
            _conn.Dispose();
        }

    }

}
