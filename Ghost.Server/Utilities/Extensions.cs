﻿using Ghost.Server.Core.Classes;
using Ghost.Server.Core.Players;
using MySql.Data.MySqlClient;
using PNet;
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ghost.Server.Utilities
{
    #region Enums
    public enum StatIndex
    {
        Null = -1,
        Mul,
        Mod,
        Min,
        Cur,
        Max,
        Base,
        Item,
        Length
    }
    public enum StatsOffset
    {
        Null = -1,
        Stats,
        Multiplers = 200,
        Player = 225,
        Length = 256
    }

    public enum CreatureMultipler
    {
        Null = -1,
        Damage = StatsOffset.Multiplers,
        Length
    }

    public enum PlayerMultiplers
    {
        Null = -1,
        XP = StatsOffset.Player,
        Bits,
        Length
    }

    public enum Gender : byte
    {
        Filly,
        Colt,
        Mare,
        Stallion
    }
    public enum MovementType : byte
    {
        Move = 0,
        Stay = 1,
    }
    public enum MovementCommand : byte
    {
        None, SetSpeed, GoTo = 5
    }
    public enum OnlineStatus : byte
    {
        Offline,
        Online,
        Remove = 20,
        Blocker = 22,
        Blockee,
        Incoming = 25,
        Outgoing
    }
    [Flags]
    public enum SpellTarget : byte
    {
        None, Position, Creature, Player = 4, Self = 8,
        NotMain = 128
    }
    public enum SpellEffectType : byte
    {
        Init,
        Damage,
        SplashDamage,
        FrontAreaDamage,
        MagickDamage = 5,
        MagicSplashDamage = 6,
        MagicFrontAreaDamage = 7,
        Heal = 10,
        Modifier = 20,
        AuraModifier = 127,
        AreaInit = 250,
        AreaPeriodicDamage = 252,
        AreaPeriodicHeal = 253,
        AreaPeriodicMagickDamage = 254,
        Teleport = 255
    }
    public enum DialogType : byte
    {
        Command = 255,
        Choice = 254,
        Greetings = 253,
        Say = 0
    }
    public enum DialogCommand : byte
    {
        None, DialogEnd, GoTo, AddXP, AddBits, AddItem, RemoveBits, RemoveItem,
        AddQuest, RemoveQuest, SetQuestState,
        CloneNPCIndex = 32, SetCloneMoveState
    }
    public enum DialogCondition : byte
    {
        Always = 0x00,

        Quest_NotStarted = 0x01,
        Quest_InProgress = 0x11,
        Quest_Failed = 0x21,
        Quest_Completed = 0x31,
        Quest_StateInProgress = 0x41,
        Quest_StateFailed = 0x51,
        Quest_StateCompleted = 0x61,

        NpcIndex_Equal = 0x02,
        NpcIndex_NotEqual = 0x12,
        Race_Equal = 0x22,
        Race_NotEqual = 0x32,
        Movement_Equal = 0x42,
        Movement_NotEqual = 0x52,

        State_Equal = 0x03,
        State_NotEqual = 0x13,
        State_Lower = 0x23,
        State_Greater = 0x33,
        State_LowerOrEqual = 0x43,
        State_GreaterOrEqual = 0x53,

        Level_Equal = 0x04,
        Level_NotEqual = 0x14,
        Level_Lower = 0x24,
        Level_Greater = 0x34,
        Level_LowerOrEqual = 0x44,
        Level_GreaterOrEqual = 0x54,

        LastChoice_Equal = 0x05,
        LastChoice_NotEqual = 0x15,

        TalentLevel_Equal = 0x06,
        TalentLevel_NotEqual = 0x16,
        TalentLevel_Lower = 0x26,
        TalentLevel_Greater = 0x36,
        TalentLevel_LowerOrEqual = 0x46,
        TalentLevel_GreaterOrEqual = 0x56,

        Item_HasCount = 0x07,
    }
    [Flags]
    public enum CreatureFlags : byte
    {
        None, Scripted, Elite
    }
    public enum CharacterType : byte
    {
        None,
        Earth_Pony,
        Unicorn,
        Pegasus,
        Moose
    }
    public enum Stats : byte
    {
        None,
        Health,
        HealthRegen,
        Energy,
        EnergyRegen,
        Attack,
        Dodge,
        Armor,
        MagicResist,
        Resiliance,
        Fortitude,
        Evade,
        Tension,
        Speed,
        MagicCasting,
        PhysicalCasting,
        Taunt,
        Unspecified = 255
    }
    [Flags]
    public enum NPCFlags : byte
    {
        None, Trader, Wears, Dialog = 4, ScriptedMovement = 8
    }
    [Flags]
    public enum ChatType : byte
    {
        None = 0,
        Untyped = 1,
        System = 2,
        Global = 4,
        Local = 8,
        Party = 16,
        Herd = 32,
        Whisper = 64
    }
    public enum ChatIcon : byte
    {
        None = 0,
        Mod = 1,
        System = 2,
        Admin = 3
    }
    [Flags]
    public enum WearablePosition : uint
    {
        None = 0,
        Tail = 1,
        Pants = 2,
        FrontSocks = 4,
        BackSocks = 8,
        Saddle = 64,
        Necklace = 256,
        Mouth = 512,
        Eyes = 2048,
        Ears = 4096,
        FrontKnees = 8192,
        BackKnees = 16384,
        FrontShoes = 16,
        BackShoes = 32,
        Shirt = 128,
        Mask = 1024,
        SaddleBags = 1073741824,
        Hat = 2147483648
    }
    [Flags]
    public enum TalentMarkId
    {
        None = -1,
        Medical = 4,
        Partying = 8,
        Music = 16,
        Animal = 128,
        Flying = 256,
        Magic = 1024,
        Artisan = 8192,
        Combat = 32768,
        Foal = 65536,
        CombatRelated = 99724,
        All = 42396
    }
    [Flags]
    public enum ItemFlags : byte
    {
        None, Stackable, Salable, Stats = 4, Usable = 8
    }
    [Flags]
    public enum MapFlags : byte
    {
        None, NoAccess, NoObjects, Instance = 4, Scripted = 8,
    }
    public enum AccessLevel
    {
        Default,
        Player,
        TeamMember = 20,
        Implementer = 25,
        Moderator = 30,
        Admin = 255
    }
    public enum ContainmentType : int
    {
        Disjoint,
        Contains,
        Intersects
    }

    public enum PlaneIntersectionType : int
    {
        Back,
        Front,
        Intersecting
    }
    #endregion
    public static class New<T>
    {
        public static readonly Func<T> Create;

        static New()
        {
            var type = typeof(T);
            var func = typeof(Func<T>);
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null) return;
            var dyn = new DynamicMethod($"New<{type.Name}>", type, Type.EmptyTypes, typeof(New<T>));
            var il = dyn.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            Create = (Func<T>)dyn.CreateDelegate(func);
        }

    }
    public static class ArrayEx
    {
        private const int GrowFactor = 4;

        private class ArrayContainer<T>
        {
            public static readonly T[] Empty = new T[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Empty<T>()
        {
            return ArrayContainer<T>.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Trim<T>(ref T[] array, ref int size)
        {
            if (size == 0)
            {
                array = ArrayContainer<T>.Empty;
                return;
            }
            var nSize = (size + (GrowFactor - 1)) & -GrowFactor;
            if (array.Length > nSize)
            {
                size = nSize;
                Array.Resize(ref array, nSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(T[] array, int size, T item)
        {
            return Array.BinarySearch(array, 0, size, item) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<T>(T[] array, ref int size, T item)
        {
            var index = Array.BinarySearch(array, 0, size, item);
            if (index < 0) return false;
            size--;
            Array.Copy(array, index + 1, array, index, size - index);
            array[size] = default(T);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(ref T[] array, ref int size, T item)
        {
            if (size == array.Length)
                Array.Resize(ref array, array.Length + GrowFactor);
            array[size] = item;
            size++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAt<T>(T[] array, ref int size, int index)
        {
            size--;
            Array.Copy(array, index + 1, array, index, size - index);
            array[size] = default(T);
        }
    }
    public static class StateEx
    {
        private const int AttemptCount = 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Set(ref int state, int flags)
        {
            var count = AttemptCount;
            do
            {
                var oState = Volatile.Read(ref state);
                if ((state & flags) == flags) return false;
                if (oState == Interlocked.CompareExchange(ref state, oState | flags, oState)) return true;
            } while (--count > 0);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Clean(ref int state, int flags)
        {
            var count = AttemptCount;
            do
            {
                var oState = Volatile.Read(ref state);
                if ((state & flags) == 0) return false;
                if (oState == Interlocked.CompareExchange(ref state, oState & ~flags, oState)) return true;
            } while (--count > 0);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(int state, int isSet, int isNotSet)
        {
            return (state & isSet) != 0 && (state & isNotSet) == 0;
        }
    }
    public static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            if (value > max) return max;
            if (value < min) return min;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetDirection(Abstracts.WorldObject obj)
        {
            return Vector3.Transform(Vector3.UnitZ, Quaternion.CreateFromAxisAngle(Vector3.UnitY, obj.Rotation.Y));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetRotation(Vector3 source, Vector3 dest, Vector3 up)
        {
            float dot = Vector3.Dot(source, dest);
            if (Math.Abs(dot + 1.0f) < 0.000001f)
                return new Quaternion(up, (float)Math.PI).QuatToEul2();
            if (Math.Abs(dot - 1.0f) < 0.000001f)
                return Quaternion.Identity.QuatToEul2();
            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(source, dest)), (float)Math.Acos(dot)).QuatToEul2();
        }
    }
    public static class HashCodeHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }
    }
    public static class StringExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char DecodeChar(this int @char)
        {
            if (@char >= '\0' && @char <= '\u0019')
                return (char)(@char + 'a');
            if (@char >= '\u001a' && @char <= '3')
                return (char)(@char + 'A' - '\u001a');
            if (@char >= '4' && @char <= '=')
                return (char)(@char + '0' - '4');
            return ' ';
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EncodeChar(this char @char)
        {
            if (@char >= 'a' && @char <= 'z')
                return @char - 'a';
            if (@char >= 'A' && @char <= 'Z')
                return @char - 'A' + '\u001a';
            if (@char >= '0' && @char <= '9')
                return @char - '0' + '4';
            return '>';
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static PonyData ToPonyData(this string ponycode)
        {
            var index = 0;
            var nameIndex = 1;
            var ret = new PonyData();
            var bits = new BitArray(Convert.FromBase64String(ponycode));
            ret.Race = (byte)bits.GetBits(ref index, 2);
            ret.HairColor0 = bits.GetBits(ref index, 24);
            ret.HairColor1 = bits.GetBits(ref index, 24);
            ret.BodyColor = bits.GetBits(ref index, 24);
            ret.EyeColor = bits.GetBits(ref index, 24);
            ret.HoofColor = bits.GetBits(ref index, 24);
            ret.Mane = (short)bits.GetBits(ref index, 8);
            ret.Tail = (short)bits.GetBits(ref index, 8);
            ret.Eye = (short)bits.GetBits(ref index, 8);
            ret.Hoof = (short)bits.GetBits(ref index, 5);
            ret.CutieMark0 = bits.GetBits(ref index, 10);
            ret.CutieMark1 = bits.GetBits(ref index, 10);
            ret.CutieMark2 = bits.GetBits(ref index, 10);
            ret.Gender = (byte)bits.GetBits(ref index, 2);
            ret.BodySize = MathHelper.Clamp(bits.GetBits(ref index, 32).BitsToFloat(), 0.9f, 1.1f);
            ret.HornSize = MathHelper.Clamp(bits.GetBits(ref index, 32).BitsToFloat(), 0.8f, 1.25f);
            ret.Name = new string(new char[nameIndex += bits.GetBits(ref index, 5)]);
            fixed (char* ptr = ret.Name)
            {
                for (char* dest = ptr; nameIndex > 0; nameIndex--, dest++)
                    *dest = bits.GetBits(ref index, 6).DecodeChar();
            }
            if ((bits.Length - index) >= 24)
                ret.HairColor2 = bits.GetBits(ref index, 24);
            return ret;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string PassHash(string login, string password)
        {
            using (var sha1 = new SHA1Managed())
            {
                return string.Join(string.Empty, sha1.ComputeHash(Encoding.UTF8.GetBytes(login.ToLower() + password)).Select(x => x.ToString("x2")));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Normalize(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value.PadRight(maxLength) : value.Substring(0, maxLength);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetMessage(this string msg, MapPlayer player)
        {
            string literal; char mod;
            int index01 = 0, index02;
            while ((index01 = msg.IndexOf('{', index01)) > 0)
            {
                index02 = msg.IndexOf('}');
                if (msg.Length > (index02 + 2) && msg[index02 + 1] == ':')
                    mod = msg[index02 + 2];
                else mod = '\0';
                literal = msg.Substring(++index01, index02 - index01);
                switch (literal)
                {
                    case "n":
                        literal = Environment.NewLine;
                        break;
                    case "name":
                        literal = player.Char.Pony.Name;
                        break;
                    case "race":
                        literal = player.Char.Pony.Race.GetRaceName();
                        break;
                    case "level":
                        literal = player.Stats.Level.ToString();
                        break;
                    case "gender":
                        literal = player.Char.Pony.Gender.GetGenderName();
                        break;

                }
                if (mod != '\0')
                {
                    index02 += 2;
                    switch (mod)
                    {
                        case 'l':
                            literal = literal.ToLowerInvariant();
                            break;
                        case 'u':
                            literal = literal.ToUpperInvariant();
                            break;
                    }
                }
                msg = msg.Remove(--index01) + literal + msg.Substring(++index02);
            }
            return msg;
        }
    }
    public static class PlayeRPCExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendPonies(this CharPlayer player)
        {
            player.Player.Rpc(1, player.Data);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(this MapPlayer player, string msg)
        {
            player.Player.Rpc(127, msg);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(this PNetR.Player player, string msg)
        {
            player.Rpc(127, msg);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SystemMsg(this MapPlayer player, string msg)
        {
            player.Player.Rpc(15, ChatType.System, string.Empty, string.Empty, msg, -1, -1, ChatIcon.System, DateTime.Now);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SystemMsg(this PNetS.Player player, string msg)
        {
            player.PlayerRpc(15, ChatType.System, string.Empty, msg, DateTime.Now, -1, ChatIcon.System, -1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBounds(this PNetR.Player player)
        {
            player.Rpc(210, (byte)70, Constants.DefaultRoomBoundsMin.X, Constants.DefaultRoomBoundsMin.Y, Constants.DefaultRoomBoundsMin.Z,
                Constants.DefaultRoomBoundsMax.X, Constants.DefaultRoomBoundsMax.Y, Constants.DefaultRoomBoundsMax.Z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBounds(this PNetR.Player player, Vector3 min, Vector3 max)
        {
            player.Rpc(210, (byte)70, min.X, min.Y, min.Z, max.X, max.Y, max.Z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SystemMsg(this PNetR.Player player, string msg)
        {
            player.Rpc(15, ChatType.System, string.Empty, string.Empty, msg, -1, -1, ChatIcon.System, DateTime.Now);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateFriend(this PNetS.Player player, FriendStatus status)
        {
            player.PlayerRpc(20, (byte)21, status);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Whisper(this PNetS.Player player, PNetS.Player target, ChatMsg msg)
        {
            player.PlayerRpc(15, msg);
            target.PlayerRpc(15, msg);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Announce(this MapPlayer player, string msg, float duration = Constants.AnnounceDuration)
        {
            player.Player.Rpc(201, msg, duration);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Announce(this PNetR.Player player, string msg, float duration = Constants.AnnounceDuration)
        {
            player.Rpc(201, msg, duration);
        }
    }
    public static class BitArrayExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBits(this BitArray bits, ref int index, int count)
        {
            int value = 0;
            for (int i = 0; i < count; i++, index++)
                value |= (bits.Get(index) ? 1 : 0) << i;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddBits(this BitArray bit, ref int index, int value, int count)
        {
            for (int i = 0; i < count; i++, index++)
                bit.Set(index, (value & 1 << i) > 0);
        }
    }
    public static class Vector3Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToDegrees(this Vector3 vec)
        {
            return vec * (180f / (float)Math.PI);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToRadians(this Vector3 vec)
        {
            return vec * ((float)Math.PI / 180f);
        }
    }
    public static class ValueTypeExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetRaceName(this byte race)
        {
            switch (race)
            {
                case 1: return "Earth Pony";
                case 2: return "Unicorn";
                case 3: return "Pegasus";
                default: return string.Empty;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToPonyCode(this PonyData data)
        {
            var bits = new BitArray(data.Name.Length * 6 + 276);
            var index = 0;
            bits.AddBits(ref index, data.Race, 2);
            bits.AddBits(ref index, data.HairColor0, 24);
            bits.AddBits(ref index, data.HairColor1, 24);
            bits.AddBits(ref index, data.BodyColor, 24);
            bits.AddBits(ref index, data.EyeColor, 24);
            bits.AddBits(ref index, data.HoofColor, 24);
            bits.AddBits(ref index, data.Mane, 8);
            bits.AddBits(ref index, data.Tail, 8);
            bits.AddBits(ref index, data.Eye, 8);
            bits.AddBits(ref index, data.Hoof, 5);
            bits.AddBits(ref index, data.CutieMark0, 10);
            bits.AddBits(ref index, data.CutieMark1, 10);
            bits.AddBits(ref index, data.CutieMark2, 10);
            bits.AddBits(ref index, data.Gender, 2);
            bits.AddBits(ref index, BitConverter.ToInt32(BitConverter.GetBytes(data.BodySize), 0), 32);
            bits.AddBits(ref index, BitConverter.ToInt32(BitConverter.GetBytes(data.HornSize), 0), 32);
            bits.AddBits(ref index, Math.Min(31, data.Name.Length - 1), 5);
            string name = data.Name;
            for (int i = 0; i < name.Length; i++)
            {
                char @char = name[i];
                bits.AddBits(ref index, @char.EncodeChar(), 6);
            }
            bits.AddBits(ref index, data.HairColor2, 24);
            var buffer = new byte[(bits.Length + 7) >> 3];
            bits.CopyTo(buffer, 0);
            return Convert.ToBase64String(buffer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetGenderName(this byte gender)
        {
            switch (gender)
            {
                case 0: return "Filly";
                case 1: return "Colt";
                case 2: return "Mare";
                case 3: return "Stallion";
                default: return string.Empty;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float BitsToFloat(this int @value)
        {
            return *(float*)&@value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToWearableIndex(this WearablePosition slot)
        {
            if (slot == WearablePosition.None)
                return 0;
            else
                return (byte)(Math.Log((uint)slot, 2.0) + 1.0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WearablePosition ToWearablePosition(this byte slot)
        {
            if (slot == 0)
                return WearablePosition.None;
            else if (slot >= 32)
                return WearablePosition.Hat;
            else
                return (WearablePosition)Math.Pow(2, slot - 1);
        }
    }
    public static class NetMessageExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadVector3(this NetMessage msg)
        {
            return new Vector3(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this NetMessage msg, Vector3 vec)
        {
            msg.Write(vec.X);
            msg.Write(vec.Y);
            msg.Write(vec.Z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WritePosition(this NetMessage msg, Vector3 vec)
        {
            msg.WriteRangedSingle(vec.X, Constants.DefaultRoomBoundsMin.X, Constants.DefaultRoomBoundsMax.X, 16);
            msg.WriteRangedSingle(vec.Y, Constants.DefaultRoomBoundsMin.Y, Constants.DefaultRoomBoundsMax.Y, 16);
            msg.WriteRangedSingle(vec.Z, Constants.DefaultRoomBoundsMin.Z, Constants.DefaultRoomBoundsMax.Z, 16);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadRotation(this NetMessage msg, ref bool full)
        {
            var vector = default(Vector3);
            full = msg.RemainingBits > 24;
            if (full)
            {
                vector.X = msg.ReadRangedSingle(-6.28318548f, 6.28318548f, 8);
                vector.Y = msg.ReadRangedSingle(-6.28318548f, 6.28318548f, 8);
                vector.Z = msg.ReadRangedSingle(-6.28318548f, 6.28318548f, 8);
            }
            else
            {
                vector.Y = msg.ReadRangedSingle(-6.28318548f, 6.28318548f, 8);
            }
            return vector;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRotation(this NetMessage msg, Vector3 vec, bool full = false)
        {
            if (full)
            {
                msg.WriteRangedSingle(vec.X, -6.28318548f, 6.28318548f, 8);
                msg.WriteRangedSingle(vec.Y, -6.28318548f, 6.28318548f, 8);
                msg.WriteRangedSingle(vec.Z, -6.28318548f, 6.28318548f, 8);
            }
            else
                msg.WriteRangedSingle(vec.Y, -6.28318548f, 6.28318548f, 8);
        }
    }
    public static class QuaternionExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 QuatToEul2(this Quaternion quaternion)
        {
            Vector3 result = default(Vector3);
            var num = quaternion.W * quaternion.W;
            var num2 = quaternion.X * quaternion.X;
            var num3 = quaternion.Y * quaternion.Y;
            var num4 = quaternion.Z * quaternion.Z;
            var num5 = num2 + num3 + num4 + num;
            var num6 = quaternion.X * quaternion.Y + quaternion.Z * quaternion.W;
            bool flag = num6 > 0.499f * num5;
            if (flag)
            {
                result.Y = 2.0f * (float)Math.Atan2(quaternion.X, quaternion.W);
                result.X = 1.57079637f;
                result.Z = 0f;
            }
            else
            {
                bool flag2 = num6 < -0.499f * num5;
                if (flag2)
                {
                    result.Y = -2.0f * (float)Math.Atan2(quaternion.X, quaternion.W);
                    result.X = -1.57079637f;
                    result.Z = 0f;
                }
                else
                {
                    result.Y = (float)Math.Atan2(2f * quaternion.Y * quaternion.W - 2f * quaternion.X * quaternion.Z, num2 - num3 - num4 + num);
                    result.X = (float)Math.Asin(2.0 * num6 / num5);
                    result.Z = (float)Math.Atan2(2f * quaternion.X * quaternion.W - 2f * quaternion.Y * quaternion.Z, -num2 + num3 - num4 + num);
                }
            }
            return result;
        }
    }
    public static class NetworkViewRPCExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FaintPony(this PNetR.NetworkView view)
        {
            view.Rpc(4, 57, RpcMode.AllUnordered);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CloseTrade(this PNetR.NetworkView view)
        {
            view.Rpc(9, 4, RpcMode.OwnerOrdered);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FailedTrade(this PNetR.NetworkView view)
        {
            view.Rpc(9, 5, RpcMode.OwnerOrdered);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Lock(this PNetR.NetworkView view, bool state)
        {
            view.Rpc(2, 204, RpcMode.OwnerOrdered, (object)state);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBits(this PNetR.NetworkView view, int bits)
        {
            view.Rpc(7, 16, RpcMode.OwnerOrdered, (object)bits);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnwearItem(this PNetR.NetworkView view, byte slot, WearablePosition usedSlots)
        {
            view.Rpc(7, 9, RpcMode.AllOrdered, slot, (int)usedSlots);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequestTrade(this PNetR.NetworkView view, ushort id)
        {
            view.Rpc(9, 1, RpcMode.OwnerOrdered, (object)id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Teleport(this PNetR.NetworkView view, Vector3 position)
        {
            view.Rpc(2, 201, RpcMode.AllOrdered, position.X, position.Y, position.Z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WearItem(this PNetR.NetworkView view, InventoryItem item, byte slot, WearablePosition usedSlots)
        {
            view.Rpc(7, 8, RpcMode.AllOrdered, item, slot, (int)usedSlots);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetInventory(this PNetR.NetworkView view, CharData data)
        {
            view.Rpc(7, 5, RpcMode.OwnerOrdered, data.SerInventory);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateWornItems(this PNetR.NetworkView view, CharData data)
        {
            view.Rpc(7, 4, RpcMode.AllOrdered, data.SerWears);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PerformSkill(this PNetR.NetworkView view, TargetEntry target)
        {
            view.Rpc(5, 61, RpcMode.AllOrdered, target);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteSlot(this PNetR.NetworkView view, InventorySlot slot, int index)
        {
            slot.Amount = 0;
            view.Rpc(7, 6, RpcMode.OwnerOrdered, slot, index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateSlot(this PNetR.NetworkView view, InventorySlot slot, int index)
        {
            view.Rpc(7, 6, RpcMode.OwnerOrdered, slot, index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddItem(this PNetR.NetworkView view, int id, int amount, byte slot)
        {
            view.Rpc(7, 6, RpcMode.OwnerOrdered, id, amount, slot);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendStatUpdate(this PNetR.NetworkView view, Stats stat, StatValue value)
        {
            view.Rpc(4, 50, RpcMode.AllOrdered, (byte)stat, value.Current);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendStatFullUpdate(this PNetR.NetworkView view, Stats stat, StatValue value)
        {
            view.Rpc(4, 51, RpcMode.AllOrdered, (byte)stat, value.Max);
            view.Rpc(4, 50, RpcMode.AllOrdered, (byte)stat, value.Current);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCombat(this PNetR.NetworkView view, PNetR.Player player, bool inCombat)
        {
            view.Rpc(4, 55, player, inCombat);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WornItems(this PNetR.NetworkView view, PNetR.Player player, CharData data)
        {
            view.Rpc(7, 4, player, data.SerWears);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendTradeState(this PNetR.NetworkView view, INetSerializable offer01, INetSerializable offer02, bool ready01, bool ready02)
        {
            view.Rpc(9, 12, RpcMode.OwnerOrdered, true, offer01, offer02, ready01, ready02);
        }
    }
    public static class MySqlDataReaderExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PonyData GetPony(this MySqlDataReader reader, int i)
        {
            var ret = new PonyData();
            ret.Race = reader.GetByte(i);
            ret.Gender = reader.GetByte(++i);
            ret.Name = reader.GetString(++i);
            if (reader.IsDBNull(++i)) return ret;
            using (var mem = new MemoryStream())
            {
                var data = (byte[])reader.GetValue(i);
                mem.Write(data, 0, data.Length); mem.Position = 0;
                using (var zip = new DeflateStream(mem, CompressionMode.Decompress))
                    return ProtoBuf.Serializer.Merge(zip, ret);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PonyData GetPonyOld(this MySqlDataReader reader, int i)
        {
            return new PonyData()
            {
                Name = reader.GetString(i),
                Race = reader.GetByte(++i),
                Gender = reader.GetByte(++i),
                Eye = reader.GetInt16(++i),
                Tail = reader.GetInt16(++i),
                Hoof = reader.GetInt16(++i),
                Mane = reader.GetInt16(++i),
                BodySize = reader.GetFloat(++i),
                HornSize = reader.GetFloat(++i),
                EyeColor = reader.GetInt32(++i),
                HoofColor = reader.GetInt32(++i),
                BodyColor = reader.GetInt32(++i),
                HairColor0 = reader.GetInt32(++i),
                HairColor1 = reader.GetInt32(++i),
                HairColor2 = reader.GetInt32(++i),
                CutieMark0 = reader.GetInt32(++i),
                CutieMark1 = reader.GetInt32(++i),
                CutieMark2 = reader.GetInt32(++i)
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetProtoBuf<T>(this MySqlDataReader reader, int i)
        {
            if (reader.IsDBNull(i)) return default(T);
            using (var mem = new MemoryStream())
            {
                var data = (byte[])reader.GetValue(i);
                mem.Write(data, 0, data.Length); mem.Position = 0;
                using (var zip = new DeflateStream(mem, CompressionMode.Decompress))
                    return ProtoBuf.Serializer.Deserialize<T>(zip);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetVector3(this MySqlDataReader reader, int i)
        {
            return new Vector3(reader.GetFloat(i), reader.GetFloat(++i), reader.GetFloat(++i));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetNullString(this MySqlDataReader reader, int i)
        {
            return reader.IsDBNull(i) ? null : reader.GetString(i);
        }
    }
}