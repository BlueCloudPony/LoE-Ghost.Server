﻿using Ghost.Server.Core.Classes;
using Ghost.Server.Core.Objects;
using Ghost.Server.Core.Servers;
using Ghost.Server.Core.Structs;
using Ghost.Server.Mgrs;
using Ghost.Server.Utilities;
using Ghost.Server.Utilities.Interfaces;
using PNet;
using PNetS;
using System;
using System.Linq;

namespace Ghost.Server.Core.Players
{
    public class MasterPlayer : IPlayer
    {
        private static readonly int SpamDelay;
        static MasterPlayer()
        {
            SpamDelay = Configs.Get<int>(Configs.Game_SpamDelay);
        }
        private string m_name;
        private Player _player;
        private UserData _user;
        private DateTime _time;
        private UserSave _save;
        private Character _char;
        private IPlayer _rPlayer;
        private string _lastWhisper;
        private FriendStatus _status;
        private MasterServer _server;
        public bool OnMap
        {
            get
            {
                return _rPlayer is MapPlayer;
            }
        }
        public UserSave Save
        {
            get
            {
                return _save;
            }
        }
        public Player Player
        {
            get
            {
                return _player;
            }
        }
        public UserData User
        {
            get
            {
                return _user;
            }
        }
        public string Status
        {
            get
            {
                return _rPlayer?.Status ?? $"Player[{_player.Id}|{_user.ID}:{_user.Name}]: at dedicated server {_player.Room.RoomId.Normalize(Constants.MaxServerName)}[{_server.Guid.ToString().Normalize(8)}]";
            }
        }
        public Character Char
        {
            get
            {
                return _char;
            }
        }
        public IServer Server
        {
            get
            {
                return _server;
            }
        }
        public WO_Player Object
        {
            get
            {
                return (_rPlayer as MapPlayer)?.Object;
            }
        }
        public IPlayer RoomPlayer
        {
            get
            {
                return _rPlayer;
            }
            set
            {
                _rPlayer = value;
            }
        }
        PNetR.Player IPlayer.Player
        {
            get
            {
                return _rPlayer?.Player;
            }
        }
        public MasterPlayer(Player player, MasterServer server)
        {
            _server = server;
            _player = player;
            _status = new FriendStatus();
            _user = player.TnUser<UserData>();
            _player.SubscribeRpcsOnObject(this);
            //if (!ServerDB.SelectUserSave(_user.ID, out _save) || _save == null)
            //{
            //    _save = new UserSave();
            //    if (!ServerDB.UpdateUserSave(_user.ID, _save))
            //        ServerLogger.LogError($"Couldn't save user[{_user.ID}] data");
            //}
            _player.NetUserDataChanged += Player_NetUserDataChanged;
            _player.FinishedSwitchingRooms += Player_FinishedSwitchingRooms;
        }
        public void Destroy()
        {
            //MasterPlayer target;
            _player.ClearSubscriptions();
            CharsMgr.RemoveCharacter(_user.Char);
            ServerDB.UpdateUserSave(_user.ID, _save);
            _player.NetUserDataChanged -= Player_NetUserDataChanged;
            _player.FinishedSwitchingRooms -= Player_FinishedSwitchingRooms;
            //foreach (var item in _save.Friends)
            //{
            //    if (item.Value.Item1 == 1 && _server.TryGetByUserId(item.Key, out target))
            //        target.Player.UpdateFriend(_status.Fill(this, OnlineStatus.Offline));
            //}
            _char = null;
            _save = null;
            _user = null;
            _status = null;
            _player = null;
            _server = null;
            _rPlayer = null;
            _lastWhisper = null;
        }
        #region RPC Handlers
        [Rpc(15, false)]
        private void RPC_015(NetMessage arg1, PlayerMessageInfo arg2)
        {
            var message = default(ChatMsg);
            message.OnDeserialize(arg1);
            if (message.Icon != 0 && _user.Access < AccessLevel.Moderator) message.Icon = 0;
            if (message.Text.Sum(x => char.IsUpper(x) ? 1 : 0) > message.Text.Length / 4 + 4 || message.Time < _time)
                _player.SystemMsg(Constants.ChatWarning);
            else
            {
                message.CharID = _user.Char;
                message.PlayerID = _player.Id;
                message.Name = m_name ?? _user.Name;
                
                _time = message.Time.AddMilliseconds(SpamDelay);
                switch (message.Type)
                {
                    case ChatType.Global:
                        _player.Server.AllPlayersRpc(15, message);
                        ServerLogger.LogChat(_user.Name, message.Name, message.Type, message.Text);
                        break;
                    case ChatType.Herd:
                    //break;
                    case ChatType.Party:
                        _player.SystemMsg($"Chat type {message.Type} not yet implemented!");
                        break;
                    case ChatType.Whisper:
                        if (_lastWhisper != null)
                        {
                            MasterPlayer player;
                            if (_server.TryGetByPonyName(_lastWhisper, out player))
                                _player.Whisper(player.Player, message);
                            else
                                _lastWhisper = null;
                        }
                        break;
                    default:
                        _player.SystemMsg($"Chat type {message.Type} not allowed!");
                        break;
                }
            }
        }
        [Rpc(16, false)]
        private void RPC_016(NetMessage arg1, PlayerMessageInfo arg2)
        {
            string targetName = arg1.ReadString();
            string text = arg1.ReadString();
            ChatIcon icon = (ChatIcon)arg1.ReadByte();
            if (targetName == _user.Name || targetName == m_name)
                return;
            if (icon != ChatIcon.None && _user.Access < AccessLevel.Moderator)
                icon = ChatIcon.None;
            MasterPlayer player;
            if (_server.TryGetByPonyName(targetName, out player))
            {
                var message = default(ChatMsg);
                message.Icon = icon;
                message.Text = text;
                message.Time = DateTime.Now;
                message.CharID = _user.Char;
                message.PlayerID = _player.Id;
                message.Type = ChatType.Whisper;
                message.Name = m_name ?? _user.Name;
                _lastWhisper = player.Char?.Pony.Name;
                //_player.PlayerRpc(15, ChatType.Whisper, _message.Name ?? _user.Name, message, time, _user.Char, icon, (int)_player.Id);
                player.Player.PlayerRpc(15, message);
            }
            else
                _lastWhisper = null;
        }
        //[Rpc(20, false)]
        //private void RPC_020(NetMessage arg1, PlayerMessageInfo arg2)
        //{
        //    switch (arg1.ReadByte())
        //    {
        //        case 21:
        //            _status.OnDeserialize(arg1);
        //            UpdateStatus();
        //            break;
        //    }
        //}
        [Rpc(150, false)]
        private void RPC_150(NetMessage arg1, PlayerMessageInfo arg2)
        {
            switch (arg1.ReadByte())
            {
                case 10:
                    Guid guid = arg1.ReadGuid(); Room room;
                    if (_server.Server.TryGetRoom(guid, out room))
                        _player.ChangeRoom(room);
                    else _player.SystemMsg($"Couldn't find room {guid.ToString().Normalize(8)}");
                    break;
                case 11:
                    Room[] rooms;
                    if (_server.Server.TryGetRooms(_player.Room.RoomId, out rooms))
                        _player.PlayerSubRpc(150, 11, new SER_RoomInfo(rooms));
                    break;
                default:
                    break;
            }
        }
        #endregion
        private void UpdateStatus()
        {
            MasterPlayer target = null; Tuple<byte, string, short, DateTime> state;
            if (_status.ID != 0)
            {
                switch (_status.Status)
                {
                    case OnlineStatus.Online:
                    case OnlineStatus.Offline:
                    case OnlineStatus.Blocker:
                    case OnlineStatus.Blockee:
                    case OnlineStatus.Incoming:
                        ServerLogger.LogInfo($"Player[{_player.Id}] received update friend status for user[{_status.ID}] status: {_status.Status}");
                        break;
                    case OnlineStatus.Remove:
                        if (_server.TryGetByUserId(_status.ID, out target))
                        {
                            _save.Friends.Remove(_status.ID);
                            target._save.Friends.Remove(_user.ID);
                            _player.UpdateFriend(_status.Fill(target, OnlineStatus.Remove));
                            target._player.UpdateFriend(_status.Fill(this, OnlineStatus.Remove));
                        }
                        else
                            ServerLogger.LogInfo($"Player[{_player.Id}] received update friend status for offline user[{_status.ID}] status: {_status.Status}");
                        break;
                    case OnlineStatus.Outgoing:
                        if (_server.TryGetByUserId(_status.ID, out target))
                        {
                            if (_save.Friends.TryGetValue(target._user.ID, out state))
                            {
                                _save.Friends[target._user.ID] =
                                   new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Online, target._char.Pony.Name, (short)target._char.Pony.CutieMark0, DateTime.Now);
                                target._save.Friends[_user.ID] =
                                    new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Online, _char.Pony.Name, (short)_char.Pony.CutieMark0, DateTime.Now);
                                _player.UpdateFriend(_status.Fill(target, OnlineStatus.Online));
                                target._player.UpdateFriend(_status.Fill(this, OnlineStatus.Online));
                            }
                            else
                            {
                                _save.Friends[target._user.ID] =
                                    new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Outgoing, target._char.Pony.Name, (short)target._char.Pony.CutieMark0, DateTime.Now);
                                target._save.Friends[_user.ID] =
                                    new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Incoming, _char.Pony.Name, (short)_char.Pony.CutieMark0, DateTime.Now);
                                target._player.UpdateFriend(_status.Fill(this, OnlineStatus.Incoming));
                                _player.UpdateFriend(_status.Fill(target, OnlineStatus.Outgoing));
                            }
                        }
                        else
                            ServerLogger.LogInfo($"Player[{_player.Id}] received update friend status for offline user[{_status.ID}] status: {_status.Status}");
                        break;
                }
                ServerDB.UpdateUserSave(_user.ID, _save);
                if (target != null)
                    ServerDB.UpdateUserSave(target._user.ID, target._save);
            }
            else if (_status.PlayerID != 0)
            {
                switch (_status.Status)
                {
                    case OnlineStatus.Online:
                    case OnlineStatus.Remove:
                    case OnlineStatus.Offline:
                    case OnlineStatus.Blocker:
                    case OnlineStatus.Blockee:
                    case OnlineStatus.Incoming:
                        ServerLogger.LogInfo($"Player[{_player.Id}] received update friend status for player[{_status.PlayerID}] status: {_status.Status}");
                        break;
                    case OnlineStatus.Outgoing:
                        if (_server.TryGetById(_status.PlayerID, out target))
                        {
                            if (_save.Friends.TryGetValue(target._user.ID, out state))
                            {
                                _save.Friends[target._user.ID] =
                                   new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Online, target._char.Pony.Name, (short)target._char.Pony.CutieMark0, DateTime.Now);
                                target._save.Friends[_user.ID] =
                                    new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Online, _char.Pony.Name, (short)_char.Pony.CutieMark0, DateTime.Now);
                                _player.UpdateFriend(_status.Fill(target, OnlineStatus.Online));
                                target._player.UpdateFriend(_status.Fill(this, OnlineStatus.Online));
                            }
                            else
                            {
                                _save.Friends[target._user.ID] =
                                    new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Outgoing, target._char.Pony.Name, (short)target._char.Pony.CutieMark0, DateTime.Now);
                                target._save.Friends[_user.ID] =
                                    new Tuple<byte, string, short, DateTime>((byte)OnlineStatus.Incoming, _char.Pony.Name, (short)_char.Pony.CutieMark0, DateTime.Now);
                                target._player.UpdateFriend(_status.Fill(this, OnlineStatus.Incoming));
                                _player.UpdateFriend(_status.Fill(target, OnlineStatus.Outgoing));
                            }
                        }
                        break;
                }
            } 
        }
        #region Events Handlers
        private void Player_NetUserDataChanged(Player obj)
        {
            if (_user.Char != 0 && (_user.Char != (_char?.ID ?? -1)))
            {
                m_name = null;
                if (!CharsMgr.SelectCharacter(_user.Char, out _char))
                    ServerLogger.LogServer(_server, $"{obj.Id} couldn't load character {_user.Char}");
                else
                    m_name = _char.Pony.Name;
            }
        }
        private void Player_FinishedSwitchingRooms(Room obj)
        {
            _rPlayer = null;
            if (ServersMgr.Contains(obj.Guid))
                _rPlayer = ServersMgr.GetPlayer(obj.Guid, _player.Id);
            //if (!(_rPlayer is MapPlayer)) return;
            //MasterPlayer target; DB_User user;
            //foreach (var item in _save.Friends)
            //{
            //    if (_server.TryGetByUserId(item.Key, out target))
            //    {
            //        if (item.Value.Item1 == 1)
            //        {
            //            _player.UpdateFriend(_status.Fill(target, OnlineStatus.Online));
            //            _player.UpdateFriend(_status.Fill(this, OnlineStatus.Online));
            //        }
            //        else if (item.Value.Item1 == 25)
            //            _player.UpdateFriend(_status.Fill(target, OnlineStatus.Incoming));
            //    }
            //    else
            //    {
            //        if (ServerDB.SelectUser(item.Key, out user))
            //        {
            //            if (item.Value.Item1 == 1)
            //                _player.UpdateFriend(_status.Fill(user, OnlineStatus.Offline));
            //            else if (item.Value.Item1 == 25)
            //                _player.UpdateFriend(_status.Fill(user, OnlineStatus.Incoming));
            //        }
            //    }
            //}
        }
        #endregion
    }
}