using System;
using System.Buffers.Binary;
using System.Text;

namespace RemoteMouse.PC;

public static class Protocol
{
    public const byte CMD_MOUSE_MOVE = 0x01;
    public const byte CMD_MOUSE_BUTTON = 0x02;
    public const byte CMD_MOUSE_SCROLL = 0x03;
    public const byte CMD_KEY_DOWN = 0x10;
    public const byte CMD_KEY_UP = 0x11;
    public const byte CMD_KEY_PRESS = 0x12;
    public const byte CMD_CHAR = 0x13;
    public const byte CMD_CLIPBOARD_GET = 0x20;
    public const byte CMD_CLIPBOARD_SET = 0x21;
    public const byte CMD_CLIPBOARD_DATA = 0x22;
    public const byte CMD_POWER_SHUTDOWN = 0x30;
    public const byte CMD_POWER_REBOOT = 0x31;
    public const byte CMD_POWER_SLEEP = 0x32;
    public const byte CMD_POWER_LOGOUT = 0x33;
    public const byte CMD_POWER_LOCK = 0x34;
    public const byte CMD_VOLUME_UP = 0x40;
    public const byte CMD_VOLUME_DOWN = 0x41;
    public const byte CMD_VOLUME_MUTE = 0x42;
    public const byte CMD_AUTH = 0xF0;
    public const byte CMD_AUTH_OK = 0xF1;
    public const byte CMD_AUTH_FAIL = 0xF2;
    public const byte CMD_SERVER_INFO = 0xF3;
    public const byte CMD_PING = 0xFE;
    public const byte CMD_PONG = 0xFF;

    public const int HeaderSize = 7;

    public static byte[] MakeHeader(byte cmd, int payloadLen)
    {
        var buf = new byte[HeaderSize];
        buf[0] = (byte)'R';
        buf[1] = (byte)'M';
        buf[2] = (byte)'1';
        buf[3] = 1;
        buf[4] = cmd;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(5), (ushort)payloadLen);
        return buf;
    }

    public static bool TryParseHeader(ReadOnlySpan<byte> data, out byte cmd, out int payloadLen)
    {
        cmd = 0;
        payloadLen = 0;
        if (data.Length < HeaderSize) return false;
        if (data[0] != 'R' || data[1] != 'M' || data[2] != '1') return false;
        cmd = data[4];
        payloadLen = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(5, 2));
        return true;
    }

    public static byte[] PackMouseMove(int dx, int dy)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0, 4), dx);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4, 4), dy);
        return Concat(MakeHeader(CMD_MOUSE_MOVE, 8), payload);
    }

    public static byte[] PackMouseButton(int button, bool down)
    {
        return Concat(MakeHeader(CMD_MOUSE_BUTTON, 2), (byte)button, (byte)(down ? 1 : 0));
    }

    public static byte[] PackMouseScroll(int dx, int dy)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0, 4), dx);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4, 4), dy);
        return Concat(MakeHeader(CMD_MOUSE_SCROLL, 8), payload);
    }

    public static byte[] PackKey(uint vkey, bool down)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(payload, vkey);
        return Concat(MakeHeader(down ? CMD_KEY_DOWN : CMD_KEY_UP, 4), payload);
    }

    public static byte[] PackKeyPress(uint vkey)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(payload, vkey);
        return Concat(MakeHeader(CMD_KEY_PRESS, 4), payload);
    }

    public static byte[] PackClipboardSet(string text)
    {
        var raw = Encoding.UTF8.GetBytes(text);
        return Concat(MakeHeader(CMD_CLIPBOARD_SET, raw.Length), raw);
    }

    public static byte[] PackAuth(string password)
    {
        var raw = Encoding.UTF8.GetBytes(password);
        return Concat(MakeHeader(CMD_AUTH, raw.Length), raw);
    }

    public static byte[] PackPong() => MakeHeader(CMD_PONG, 0);
    public static byte[] PackServerInfo(string name, string version)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name ?? "");
        var versionBytes = Encoding.UTF8.GetBytes(version ?? "");
        var payload = new byte[2 + nameBytes.Length + 2 + versionBytes.Length];
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), (ushort)nameBytes.Length);
        nameBytes.CopyTo(payload, 2);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2 + nameBytes.Length, 2), (ushort)versionBytes.Length);
        versionBytes.CopyTo(payload, 2 + nameBytes.Length + 2);
        return Concat(MakeHeader(CMD_SERVER_INFO, payload.Length), payload);
    }
    public static byte[] PackClipboardData(string text)
    {
        var raw = Encoding.UTF8.GetBytes(text);
        return Concat(MakeHeader(CMD_CLIPBOARD_DATA, raw.Length), raw);
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var r = new byte[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }

    private static byte[] Concat(byte[] a, byte b1, byte b2)
    {
        var r = new byte[a.Length + 2];
        a.CopyTo(r, 0);
        r[a.Length] = b1;
        r[a.Length + 1] = b2;
        return r;
    }
}
