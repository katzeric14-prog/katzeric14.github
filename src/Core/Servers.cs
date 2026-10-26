using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class ServerEntry
    {
        public readonly string Name;
        public readonly string Host;
        public readonly int Port;
        public readonly string Description;

        public ServerEntry(string name, string host, int port, string description)
        {
            Name = name;
            Host = host;
            Port = port;
            Description = description;
        }

        public string Address
        {
            get { return Port == 25565 ? Host : Host + ":" + Port; }
        }
    }

    public sealed class ServerStatus
    {
        public bool Online;
        public int PingMs;
        public int OnlinePlayers;
        public int MaxPlayers;
        public string Version = "";
        public string Motd = "";
        public string Error = "";
    }

    public static class ServerCatalog
    {
        public static readonly ServerEntry[] All = new[]
        {
            new ServerEntry("Hypixel", "mc.hypixel.net", 25565,
                "Крупнейший международный проект: BedWars, SkyWars, SkyBlock"),
            new ServerEntry("FunTime", "mc.funtime.su", 25565,
                "Популярный русский сервер: анархия, гриферство, мини-игры"),
            new ServerEntry("HolyWorld", "mc.holyworld.ru", 25565,
                "Русский сервер выживания с кланами и экономикой"),
            new ServerEntry("ReallyWorld", "play.reallyworld.me", 25565,
                "Крупнейший русский сервер выживания с приватами"),
            new ServerEntry("MineBlaze", "play.mineblaze.net", 25565,
                "Русская сеть режимов: выживание, анархия, мини-игры"),
            new ServerEntry("MasedWorld", "mc.masedworld.net", 25565,
                "Русский проект: выживание, анархия и мини-игры"),
            new ServerEntry("McSkill", "play.mcskill.net", 25565,
                "Старейший русский сервер: выживание и мини-игры"),
            new ServerEntry("CheatMine", "mc.cheatmine.net", 25565,
                "Русский сервер: выживание, анархия, гриферство"),
            new ServerEntry("CubeCraft", "play.cubecraft.net", 25565,
                "Международные мини-игры, BedWars и SkyBlock"),
            new ServerEntry("2b2t", "connect.2b2t.org", 25565,
                "Старейший анархический сервер без правил и приватов"),
            new ServerEntry("WildNetwork", "play.wildprison.net", 25565,
                "SkyBlock, Prison и выживание на WildNetwork"),
            new ServerEntry("Minecraft Central", "play.mc-central.net", 25565,
                "Мини-игры и SkyBlock крупной международной сети"),
            new ServerEntry("Complex Gaming", "play.mc-complex.com", 25565,
                "Международная сеть: SkyBlock, Prison и Pixelmon"),
            new ServerEntry("BlocksMC", "play.blocksmc.com", 25565,
                "Турецкая сеть мини-игр: BedWars, SkyWars, Practice"),
            new ServerEntry("CraftRise", "play.craftrise.tc", 25565,
                "Крупнейший турецкий сервер: выживание и мини-игры"),
            new ServerEntry("UltimisMC", "play.ultimismc.com", 25565,
                "Международные мини-игры и PvP-режимы"),
            new ServerEntry("Gamster", "mc.gamster.org", 25565,
                "Сервер мини-игр: BedWars, SkyWars и Duels"),
            new ServerEntry("LemonCloud", "play.lemoncloud.net", 25565,
                "SkyBlock и фракции на международной сети"),
            new ServerEntry("Foxcraft", "play.foxcraft.net", 25565,
                "Выживание, паркур и мини-игры"),
            new ServerEntry("Minewind", "play.minewind.com", 25565,
                "Анархический сервер без правил и приватов"),
            new ServerEntry("EcoCityCraft", "play.ecocitycraft.com", 25565,
                "Долгоживущий сервер выживания с экономикой"),
            new ServerEntry("OldFag", "play.oldfag.org", 25565,
                "Анархия в духе старых серверов без правил"),
            new ServerEntry("PvPWars", "play.pvpwars.net", 25565,
                "PvP-арена и SkyBlock с регулярными событиями")
        };
    }

    public static class ServerPing
    {
        public const int DefaultTimeoutMs = 4000;

        public static Task<ServerStatus> PingAsync(ServerEntry entry, int timeoutMs, CancellationToken ct)
        {
            return Task.Run(delegate { return Ping(entry, timeoutMs, ct); }, ct);
        }

        private static ServerStatus Ping(ServerEntry entry, int timeoutMs, CancellationToken ct)
        {
            var status = new ServerStatus();
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                ct.ThrowIfCancellationRequested();
                using (var client = new TcpClient())
                {
                    client.NoDelay = true;
                    Task connect = client.ConnectAsync(entry.Host, entry.Port);
                    try
                    {
                        if (!connect.Wait(timeoutMs, ct)) throw new TimeoutException();
                    }
                    catch (AggregateException aggregate)
                    {
                        if (aggregate.InnerExceptions.Count > 0) throw aggregate.InnerExceptions[0];
                        throw;
                    }

                    ct.ThrowIfCancellationRequested();
                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = timeoutMs;
                    stream.WriteTimeout = timeoutMs;

                    byte[] handshake = BuildHandshake(entry);
                    stream.Write(handshake, 0, handshake.Length);
                    stream.Write(new byte[] { 0x01, 0x00 }, 0, 2);
                    stream.Flush();

                    string json = ReadStatusJson(stream);
                    status.PingMs = (int)watch.ElapsedMilliseconds;
                    ApplyStatus(status, json);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                status.Error = "тайм-аут";
            }
            catch (Exception ex)
            {
                status.Error = ShortError(ex);
            }
            if (!status.Online && status.PingMs == 0) status.PingMs = (int)watch.ElapsedMilliseconds;
            return status;
        }

        private static void ApplyStatus(ServerStatus status, string json)
        {
            JsonValue root = JsonParser.Parse(json);
            status.Online = true;
            status.Version = root["version"]["name"].AsString("");
            JsonValue players = root["players"];
            status.OnlinePlayers = Math.Max(0, players["online"].AsInt(0));
            status.MaxPlayers = Math.Max(0, players["max"].AsInt(0));
            status.Motd = FlattenDescription(root["description"]);
        }

        private static string ReadStatusJson(NetworkStream stream)
        {
            int length = ReadVarInt(stream);
            if (length <= 0 || length > 4 * 1024 * 1024)
                throw new InvalidDataException("некорректный размер пакета");

            byte[] packet = ReadExact(stream, length);
            int position = 0;
            int packetId = ReadVarInt(packet, ref position);
            if (packetId != 0)
                throw new InvalidDataException("неожиданный ответ сервера");

            int jsonLength = ReadVarInt(packet, ref position);
            if (jsonLength < 0 || position + jsonLength > packet.Length)
                throw new InvalidDataException("повреждён ответ сервера");

            return Encoding.UTF8.GetString(packet, position, jsonLength);
        }

        private static byte[] BuildHandshake(ServerEntry entry)
        {
            var payload = new MemoryStream();
            WriteVarInt(payload, 0);
            WriteVarInt(payload, 47);
            WriteString(payload, entry.Host);
            payload.WriteByte((byte)((entry.Port >> 8) & 0xFF));
            payload.WriteByte((byte)(entry.Port & 0xFF));
            WriteVarInt(payload, 1);

            var packet = new MemoryStream();
            WriteVarInt(packet, (int)payload.Length);
            payload.WriteTo(packet);
            return packet.ToArray();
        }

        private static void WriteVarInt(Stream stream, int value)
        {
            uint current = (uint)value;
            while ((current & 0xFFFFFF80u) != 0)
            {
                stream.WriteByte((byte)((current & 0x7F) | 0x80));
                current >>= 7;
            }
            stream.WriteByte((byte)current);
        }

        private static void WriteString(Stream stream, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
            WriteVarInt(stream, bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static int ReadVarInt(Stream stream)
        {
            int value = 0;
            int position = 0;
            while (true)
            {
                int read = stream.ReadByte();
                if (read < 0) throw new IOException("соединение закрыто сервером");
                value |= (read & 0x7F) << position;
                if ((read & 0x80) == 0) return value;
                position += 7;
                if (position >= 32) throw new InvalidDataException("слишком длинный VarInt");
            }
        }

        private static int ReadVarInt(byte[] buffer, ref int position)
        {
            int value = 0;
            int shift = 0;
            while (true)
            {
                if (position >= buffer.Length) throw new InvalidDataException("ответ сервера обрезан");
                int read = buffer[position++];
                value |= (read & 0x7F) << shift;
                if ((read & 0x80) == 0) return value;
                shift += 7;
                if (shift >= 32) throw new InvalidDataException("слишком длинный VarInt");
            }
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) throw new IOException("соединение закрыто сервером");
                offset += read;
            }
            return buffer;
        }

        public static string FlattenDescription(JsonValue value)
        {
            var sb = new StringBuilder();
            AppendChat(value, sb);
            string text = StripSectionCodes(sb.ToString());
            text = text.Replace("\r", "").Replace("\n", " · ");
            return text.Trim();
        }

        private static void AppendChat(JsonValue value, StringBuilder sb)
        {
            if (value == null || value.IsNull) return;
            if (value.IsString)
            {
                sb.Append(value.AsString(""));
                return;
            }
            if (value.IsArray)
            {
                foreach (JsonValue item in value.Items) AppendChat(item, sb);
                return;
            }
            if (value.IsObject)
            {
                AppendChat(value["text"], sb);
                AppendChat(value["extra"], sb);
            }
        }

        private static string StripSectionCodes(string text)
        {
            if (text.IndexOf('\u00A7') < 0) return text;
            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\u00A7' && i + 1 < text.Length)
                {
                    i++;
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static string ShortError(Exception ex)
        {
            var socket = ex as SocketException;
            if (socket != null)
            {
                switch (socket.SocketErrorCode)
                {
                    case SocketError.ConnectionRefused: return "порт закрыт";
                    case SocketError.HostNotFound: return "хост не найден";
                    case SocketError.TimedOut: return "тайм-аут";
                    case SocketError.NetworkUnreachable: return "сеть недоступна";
                    case SocketError.HostUnreachable: return "сервер недоступен";
                    default: return socket.Message;
                }
            }
            if (ex is TimeoutException) return "тайм-аут";
            if (ex is InvalidDataException || ex is IOException) return ex.Message;
            return ex.Message;
        }
    }
}
