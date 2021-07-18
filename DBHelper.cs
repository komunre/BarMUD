using System.Data.SQLite;
using System;

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

        public SQLiteDataReader QueryRequet(string req) {
            var cmd = new SQLiteCommand(_conn);
            cmd.CommandText = req;
            var reader = cmd.ExecuteReader();
            return reader;
        }

        public void NonQueryReqest(string req) {{
            var cmd = new SQLiteCommand(_conn);
            cmd.CommandText = req;
            cmd.ExecuteNonQuery();
        }}
    }
}