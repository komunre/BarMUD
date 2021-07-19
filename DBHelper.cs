using System.Data.SQLite;
using System;
using System.IO;

namespace barmud
{
    public class DBHelper
    {
        private SQLiteConnection _conn;
        public void Connect() {
            _conn = new SQLiteConnection(@"URI=file:./userdata.db");
            _conn.Open();
        }
        public bool FindRequest(string req) {
            var cmd = new SQLiteCommand(_conn);
            cmd.CommandText = req;
            var reader = cmd.ExecuteReader();
            if (reader.Read()) {
                return true;
            }
            return false;
        }

        public SQLiteDataReader QueryRequest(string req) {
            var cmd = new SQLiteCommand(_conn);
            cmd.CommandText = req;
            var reader = cmd.ExecuteReader();
            return reader;
        }

        public void NonQueryReqest(string req) {
            var cmd = new SQLiteCommand(_conn);
            cmd.CommandText = req;
            cmd.ExecuteNonQuery();
        }

        /*private MongoClient _conn;
        private IMongoDatabase _db;

        public void Connect() {
            _conn = new MongoClient("mongodb://localhost");
            _db = _conn.GetDatabase("mud");
        }

        public bool FindRequest(string req) {
            return false;
        }

        public SQLiteDataReader QueryRequest(string req) {
            
        }

        public void NonQueryReqest(string req) {
            
        }*/
    }
}