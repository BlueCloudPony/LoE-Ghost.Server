﻿using Ghost.Server.Core.Classes;
using Ghost.Server.Core.Players;
using Ghost.Server.Core.Structs;
using Ghost.Server.Mgrs;
using Ghost.Server.Mgrs.Map;
using Ghost.Server.Scripts.Servers;
using Ghost.Server.Utilities;
using Ghost.Server.Utilities.Interfaces;
using PNet;
using PNetR;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ServerTime = System.Diagnostics.Stopwatch;

namespace Ghost.Server.Core.Servers
{
    public class MapServer : IRoom
    {
        public static int _staticPort;
        static MapServer()
        {
            _staticPort = Configs.Get<int>(Configs.Map_Port);
            _staticPort--;
        }
        private readonly int _port;
        private readonly DB_Map _map;
        private readonly string _name;
        private string _id;
        private Room _room;
        private bool _running;
        private Thread _rLoop;
        private int _maxPlayers;
        private ObjectsMgr _objects;
        private DialogsMgr _dialogs;
        private MapServerScript _script;
        private NetworkConfiguration _cfg;
        private List<IUpdatable> _updatables;
        private Dictionary<ushort, MapPlayer> _players;
        public string ID
        {
            get
            {
                return _id;
            }
        }
        public Guid Guid
        {
            get { return _room?.RoomId ?? Guid.Empty; }
        }
        public Room Room
        {
            get { return _room; }
        }
        public DB_Map Map
        {
            get { return _map; }
        }
        public string Name
        {
            get
            {
                return _name;
            }
        }
        public string Status
        {
            get
            {
                return $"{_name}[{_id}]: Status {_room.ServerStatus}; Port: {_port}; Players: {_players.Count}/{_maxPlayers}; Updatable: {_updatables.Count}";
            }
        }
        public bool IsRunning
        {
            get
            {
                return _running;
            }
        }
        public ObjectsMgr Objects
        {
            get { return _objects; }
        }
        public DialogsMgr Dialogs
        {
            get { return _dialogs; }
        }
        public MapPlayer this[ushort index]
        {
            get { MapPlayer entry; _players.TryGetValue(index, out entry); return entry; }
        }
        public MapServer(DB_Map map)
        {
            _port = Interlocked.Increment(ref _staticPort);
            _map = map;
            _id = "########";
            _dialogs = new DialogsMgr(this);
            _updatables = new List<IUpdatable>();
            _players = new Dictionary<ushort, MapPlayer>();
            _name = _map.Name.Normalize(Constants.MaxServerName);
#if DEBUG
            if (!(Debug.Logger is DefaultConsoleLogger))
                Debug.Logger = new DefaultConsoleLogger();
#endif
        }
        public void Stop()
        {
            if (_room == null || !_running) return;
            ServersMgr.Remove(this);
            _running = false;
            _room.Shutdown();
            _rLoop.Join();
            Thread.Sleep(50);
            _room.PlayerAdded -= Room_PlayerAdded;
            _room.PlayerRemoved -= Room_PlayerRemoved;
            _room.ConstructNetData -= Room_ConstructNetData;
            _room.ServerStatusChanged -= Room_ServerStatusChanged;
            _objects.Destroy();
            lock (_updatables) _updatables.Clear();
            _players.Clear();
            _cfg = null;
            _room = null;
            _rLoop = null;
            _script = null;
            _objects = null;
        }
        public void Start()
        {
            if (_room != null || _running) return;
            CreateRoom();
            ServerLogger.LogServer(this, $" Started");
        }
        public void Restart()
        {
            if (_running) Stop();
            CreateRoom();
            ServerLogger.LogServer(this, $" Restarted");
        }
        public void RigisterOnUpdate(IUpdatable entry)
        {
            lock (_updatables) _updatables.Add(entry);
        }
        public void RemoveFromUpdate(IUpdatable entry)
        {
            lock (_updatables) _updatables.Remove(entry);
        }
        #region Server Loop
        private void ServerLoop()
        {
            int delay = Configs.Get<int>(Configs.Map_Reconnect);
            int tick = _cfg.TickRate; if (tick < 0) tick = 0;
            IUpdatable[] toUpdate; if (delay <= 0) delay = 2000; 
            ServerTime timer = ServerTime.StartNew();
            TimeSpan time01, time02 = timer.Elapsed, time03;
            int timeout = delay, time;
            while (_running)
            {
                time01 = timer.Elapsed;
                _room.ReadQueue();
                if (_room.ServerStatus == ConnectionStatus.Disconnected)
                {
                    timeout += tick;
                    if (timeout >= delay) { _room.StartConnection(); timeout = 0; }
                }
                else
                {
                    time03 = timer.Elapsed - time02;
                    lock (_updatables)
                        if (_updatables.Count > 0)
                        {
                            toUpdate = _updatables.ToArray();
                            for (int i = 0; i < toUpdate.Length; i++)
                                toUpdate[i].Update(time03);
                            toUpdate = null;
                        }
                }
                time02 = timer.Elapsed;
                time = tick - (time02 - time01).Milliseconds;
                if (time > 0) Thread.Sleep(time);
            }
        }
        #endregion
        #region Events Handlers
        private void Room_ServerStatusChanged()
        {
            if (_room.ServerStatus == ConnectionStatus.Connected)
            {
                ServersMgr.Add(this);
                _id = _room.RoomId.ToString().Normalize(8);
            }
            else
                ServersMgr.Remove(this);
            ServerLogger.LogServer(this, $" Status {_room.ServerStatus}");
        }
        private void Room_PlayerAdded(Player obj)
        {
            _players[obj.Id] = new MapPlayer(obj, this);
            ServerLogger.LogServer(this, $" Player {obj.Id} added");
        }
        private void Room_PlayerRemoved(Player obj)
        {
            _players[obj.Id].Destroy();
            _players.Remove(obj.Id);
            _dialogs.RemoveClones(obj.Id);
            ServerLogger.LogServer(this, $" Player {obj.Id} removed");
        }
        private INetSerializable Room_ConstructNetData()
        {
            return new UserData();
        }
        #endregion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateRoom()
        {
            _maxPlayers = Configs.Get<int>(Configs.Map_MaxPlayers);
            _cfg = new NetworkConfiguration(_maxPlayers, _port, Configs.Get<int>(Configs.Map_Tick), _map.Name, dispatcherAddress: Configs.Get<string>(Configs.Server_Host),
                userDefinedAuthData: $"{ServersMgr.Dedicated}@{GetType().Name}{(ServersMgr.Dedicated ? string.Empty : $"@{ServersMgr.Master.Guid}")}",
                dispatcherPort: Configs.Get<int>(Configs.Server_Maps_Port), listenAddress: Configs.Get<string>(Configs.Map_Host));
            _room = new Room(_cfg);
            _objects = new ObjectsMgr(this);
            _script = new MapServerScript(this);
            _room.PlayerAdded += Room_PlayerAdded;
            _room.PlayerRemoved += Room_PlayerRemoved;
            _room.ConstructNetData += Room_ConstructNetData;
            _room.ServerStatusChanged += Room_ServerStatusChanged;
            _rLoop = new Thread(ServerLoop);
            _rLoop.IsBackground = true;
            _running = true;
            _rLoop.Start();
        }
    }
}