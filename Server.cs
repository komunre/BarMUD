using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System;
using Npgsql;

namespace barmud
{
    public class Server
    {
        private Socket _listener;
        private List<MUDSocket> _clients = new List<MUDSocket>();
        private List<int> _removedClients = new List<int>();
        private DBHelper _dbHelper;
        private Dictionary<int, DateTime> _miningEndTimes = new(); 

        public Server(int port) {
            IPAddress address = IPAddress.Any;
            IPEndPoint endPoint = new IPEndPoint(address, port);

            _listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _listener.Bind(endPoint);
            _listener.Listen(100);
            
            _dbHelper = new DBHelper();
            _dbHelper.Connect();
        }

        public async void Listen() {
            await Task.Run(() => {
                while (true) {
                    Socket client = _listener.Accept();
                    MUDSocket cl = new MUDSocket(client);
                    _clients.Add(cl);

                    client.Send(Encoding.UTF8.GetBytes("Hello! Welcome to BarMUD. Enter your character name\n"));
                    client.Blocking = false;

                    Debugger.LogClient(client, "New connection");
                }
            });
        }

        private void HandleDisconnect(int id) {
            _clients[id].Sock.Close();
            _removedClients.Add(id);
        }

        public void SendToClient(int id, string msg) {
            try {
                _clients[id].Sock.Send(Encoding.UTF8.GetBytes(msg + "\n"));
            }
            catch (SocketException) {
                HandleDisconnect(id);
            }
        }

        private byte[] ReceiveAll(int id) {
            Socket sock = _clients[id].Sock;
            int total = 0;
            const uint size = 4096;
            byte[] data = new byte[size];
            while (total < size) {
                if (sock.Available == 0) {
                    break;
                }
                int got = 0;
                got = sock.Receive(data, total, (int)(size - total), SocketFlags.None);
                if (got == 0) {
                    break;
                }
                total += got;
            }
            if (total == 0 ){
                return null;
            }

            return data;
        }

        private void SendToEveryone(string msg) {
            for (int i = 0; i < _clients.Count; i++) {
                SendToClient(i, msg);
            }
        }

        private string FixMesssage(string msg) {
            string result = msg.Replace("\n", "");
            result = result.Replace("\0", "");
            result = result.Replace("\r", "");
            return result;
        }

        private void GetEntDataFromDB(int id) {
            var name = _clients[id].Name;
            var reader = _dbHelper.QueryRequest(String.Format("SELECT id, balance, health FROM users WHERE username='{0}'", name));
            if (!reader.Read()) {
                SendToClient(id, "Your data is corrupted. Please contact administrator");
                HandleDisconnect(id);
                return;
            }
            var userid = reader.GetInt32(0);
            var money = reader.GetInt32(1);
            _clients[id].Entity.Money = money;
            _clients[id].Entity.Health = reader.GetInt32(2);
            reader = _dbHelper.QueryRequest(String.Format("SELECT item_counter, item_id FROM inventory WHERE ownby={0}", (long)userid));
            while (reader.Read()) {
                _clients[id].Entity.Inventory.Add(reader.GetInt32(1));
            }
        }

        private void SaveEntDataToDB(int id) {
            _dbHelper.NonQueryReqest(String.Format("UPDATE users SET balance={0}, health={1}, loc={2} WHERE username='{3}'", _clients[id].Entity.Money, _clients[id].Entity.Health, 0, _clients[id].Name));
            //for (int i = 0; i < _clients[id].Entity.Inventory.Count; i++) {
                //_dbHelper.NonQueryReqest(String.Format("UPDATE inventory SET item_count={0} WHERE ownby={1} AND item_id={2}", ))
            //}
        }

        public void ProcessMessages() {
            if (_clients.Count < 1 || _clients == null) {
                return;
            }

            for (int i = 0; i < _clients.Count; i++) {
                MUDSocket client = _clients[i];

                byte[] data = ReceiveAll(i);
                if (data == null) { 
                    continue;
                }

                string msg = Encoding.UTF8.GetString(data);
                msg = FixMesssage(msg);
                if (client.Status != PlayerStatus.Password) {
                    Debugger.LogClient(_clients[i].Sock, "Got messsage: " + msg);
                }
                string[] splitted = msg.Split(' ');
                string command = splitted[0];
                if (command.Equals("")) {
                    command = msg;
                }
                string[] cmdParams = splitted[1..];

                string total = "";
                foreach (string param in cmdParams) {
                    total += param + " ";
                }

                if (client.Status == PlayerStatus.Name && command == "new") {
                    SendToClient(i, "Enter your character name");
                    client.Status = PlayerStatus.New;
                    continue;
                }
                else if (client.Status == PlayerStatus.Name) {
                    client.Name = msg;
                    bool founded = _dbHelper.FindRequest(String.Format("SELECT username FROM users WHERE username='{0}'", client.Name));
                    if (founded) {
                        SendToClient(i, "Enter your password");
                        client.Status = PlayerStatus.Password;
                    }
                    else {
                        SendToClient(i, "We can't find your character. To create new one, enter 'new'");
                    }
                    continue;
                }

                if (client.Status == PlayerStatus.Password) {
                    var founded = _dbHelper.QueryRequest(String.Format("SELECT pass FROM users WHERE username='{0}'", client.Name));
                    if (founded.Read()) {
                        if (BCrypt.Net.BCrypt.Verify(msg, (string)founded[0])){
                            SendToClient(i, "Logged in");
                            client.LoggedIn = true;
                            client.Status = PlayerStatus.Ready;
                            GetEntDataFromDB(i);
                        }
                        else {
                            SendToClient(i, "Wrong password. Try again or relogin to choose another account");
                        }
                    }
                    else {
                        SendToClient(i, "Can't find your account");
                    }
                    continue;
                }

                if (client.Status == PlayerStatus.New) {
                    client.Name = msg;
                    SendToClient(i, "Enter your password");
                    client.Status = PlayerStatus.NewPassword;
                    continue;
                }

                if (client.Status == PlayerStatus.NewPassword) {
                    bool found = _dbHelper.FindRequest(String.Format("SELECT username FROM users WHERE username='{0}'", client.Name));
                    if (found) {
                        SendToClient(i, "You already have this account. Please enter your character name");
                        client.Status = PlayerStatus.Name;
                        continue;
                    }
                    _dbHelper.NonQueryReqest(String.Format("INSERT INTO users (username, pass) VALUES ('{0}', '{1}')", client.Name, BCrypt.Net.BCrypt.HashPassword(msg)));
                    SendToClient(i, "Account registered");
                    Debugger.LogClient(_clients[i].Sock, "New account");
                    client.LoggedIn = true;
                    client.Status = PlayerStatus.Ready;
                    continue;
                }


                if (!client.LoggedIn) {
                    SendToClient(i, "You can't perform action while you're not logged in.");
                    return;
                }

                switch (command) {
                    case "say": 
                        SendToEveryone(client.Name + " says: " + total);
                        break;
                    case "ooc":
                        SendToEveryone("OOC Chat - " + client.Name + ": " + total);
                        break;
                    case "sit":
                        SendToEveryone(client.Name + " sit on the " + total);
                        break;
                    case "me":
                        SendToEveryone(client.Name + " " + total);
                        break;
                    case "drink":
                        SendToEveryone(client.Name + " drinks " + total);
                        client.Drunk += (uint)new Random().Next(0, 14);
                        break;
                    /*case "name":
                        client.Name = total;
                        bool founded = _dbHelper.FindRequest(String.Format("SELECT username FROM users WHERE username='{0}'", client.Name));
                        if (founded) {
                            var reader =  _dbHelper.QueryRequest(String.Format("SELECT balance FROM users WHERE username='{0}'", client.Name));
                            reader.Read();
                            client.Money = (long)reader[0];
                            SendToClient(i, "You logged in");
                        }
                        else {
                            _dbHelper.NonQueryReqest(String.Format("INSERT INTO users (username) VALUES ('{0}')", client.Name));
                            SendToClient(i, "Account registered");
                            Debugger.LogClient(_clients[i].Sock, "New account");
                        }
                        SendToClient(i, "Name changed");
                        break;*/
                    case "disconnect":
                        HandleDisconnect(i);
                        break;
                    case "mine":
                        if (!_miningEndTimes.ContainsKey(i)) {
                            _miningEndTimes.Add(i, DateTime.Now.AddSeconds(new Random().Next(2, 11)));
                            SendToClient(i, "You started mining");
                        }
                        else {
                            if (_miningEndTimes[i] < DateTime.Now) {
                                int added = new Random().Next(10, 35);
                                client.Money += added;
                                _miningEndTimes.Remove(i);
                                SendToClient(i, "You got money from mining: " + added.ToString());
                            }
                            else {
                                SendToClient(i, "You're still mining...");
                            }
                        }
                        break;
                    case "look":
                        string players = "";
                        for (int j = 0; j < _clients.Count; j++) {
                            players += _clients[j].Name + " ";
                        }
                        SendToClient(i, "You see these people around: " + players);
                        break;
                    case "inv":
                        string inventory = "You have:\n";
                        for (int j = 0; j < _clients[i].Entity.Inventory.Count; j++) {
                            var reader = _dbHelper.QueryRequest(String.Format("SELECT title, descr FROM items_list WHERE id={0}", _clients[i].Entity.Inventory[j]));
                            if (!reader.Read()) {
                                SendToClient(i, "Nothing in your inventory!");
                                break;
                            }
                            inventory += reader.GetString(0) + " || " + reader.GetString(1) + "\n";
                        }
                        SendToClient(i, inventory);
                        break;
                    default:
                        SendToClient(i, "Wrong command");
                        break;
                }

                //_dbHelper.NonQueryReqest(String.Format("UPDATE users SET balance={0} WHERE username='{1}'", client.Money, client.Name));
                SaveEntDataToDB(i);
            }

            foreach (int id in _removedClients) {
                _clients.RemoveAt(id);
            }

            _removedClients.Clear();
        }
    }
}