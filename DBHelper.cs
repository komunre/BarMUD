using Npgsql;
using System;
using System.IO;
using System.Threading;

namespace barmud
{
    public class DBHelper
    {
        private NpgsqlConnection _conn;
        public void Connect() {
            _conn = new NpgsqlConnection("Host=localhost;Username=barmud;Password=KoFhLpjGaaT5GTL525Ca;Database=barmud_data");
            _conn.Open();
        }
        public bool FindRequest(string req) {
            var cmd = new NpgsqlCommand(req, _conn);
            return cmd.ExecuteScalar() != null;
        }

        public NpgsqlDataReader QueryRequest(string req) {
            var cmd = new NpgsqlCommand(req, _conn);
            var reader = cmd.ExecuteReader();
            return reader;
        }

        public void NonQueryReqest(string req) {
            var cmd = new NpgsqlCommand(req, _conn);
            cmd.ExecuteNonQuery();
        }

        public void CloseReader(NpgsqlDataReader reader)
        {
            reader.Close();
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