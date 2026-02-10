using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteMouse.PC;

public class RemoteMouseServer
{
    private readonly string _password;
    private readonly int _tcpPort;
    private readonly int _udpPort;
    private readonly Action<string> _onStatus;
    private readonly InputController _controller = new();
    private readonly ConcurrentDictionary<TcpClient, bool> _authenticated = new();
    private TcpListener? _tcpListener;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private bool _running;

    private readonly Action<string>? _onClientConnected;
    private readonly Func<bool>? _isSilentMode;
    private readonly string _pcName;
    private readonly string _pcVersionFallback;
    private readonly Func<string?>? _getPcVersion;

    public RemoteMouseServer(string password, int tcpPort, int udpPort, Action<string> onStatus, Action<string>? onClientConnected = null, Func<bool>? isSilentMode = null, string? pcName = null, string? pcVersion = null, Func<string?>? getPcVersion = null)
    {
        _password = password ?? "";
        _tcpPort = tcpPort;
        _udpPort = udpPort;
        _onStatus = onStatus;
        _onClientConnected = onClientConnected;
        _isSilentMode = isSilentMode;
        _pcName = pcName ?? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Environment.MachineName.ToLowerInvariant());
        _pcVersionFallback = pcVersion ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0";
        _getPcVersion = getPcVersion;
    }

    private string GetPcVersion() => _getPcVersion?.Invoke() ?? _pcVersionFallback;

    public static string GetLocalIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 80);
            var endPoint = (IPEndPoint?)s.LocalEndPoint;
            return endPoint?.Address.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public static List<string> GetLocalIps()
    {
        var list = new List<string>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    var ip = addr.Address.ToString();
                    if (ip != "127.0.0.1" && !list.Contains(ip))
                        list.Add(ip);
                }
            }
        }
        catch { }
        if (list.Count == 0) list.Add(GetLocalIp());
        else
        {
            var primary = GetLocalIp();
            var idx = list.IndexOf(primary);
            if (idx > 0)
            {
                list.RemoveAt(idx);
                list.Insert(0, primary);
            }
        }
        return list;
    }

    public void Start()
    {
        if (_running) return;
        _cts = new CancellationTokenSource();
        _running = true;

        _tcpListener = new TcpListener(IPAddress.Any, _tcpPort);
        _tcpListener.Start();
        _onStatus($"Сервер TCP {_tcpPort} запущен");

        _ = AcceptLoopAsync(_cts.Token);

        try
        {
            _udpClient = new UdpClient(_udpPort);
            _udpClient.EnableBroadcast = true;
            _ = UdpLoopAsync(_cts.Token);
            _onStatus($"UDP discovery {_udpPort} запущен");
        }
        catch (Exception ex)
        {
            _onStatus($"UDP не запущен: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        _cts?.Cancel();
        _tcpListener?.Stop();
        _tcpListener = null;
        _udpClient?.Close();
        _udpClient = null;
        foreach (var c in _authenticated.Keys.ToList())
        {
            try { c.Close(); } catch { }
        }
        _authenticated.Clear();
        _onStatus("Сервер остановлен");
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (_running && _tcpListener != null && !ct.IsCancellationRequested)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { }
        }
    }

    private async Task UdpLoopAsync(CancellationToken ct)
    {
        while (_udpClient != null && !ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct);
                var data = result.Buffer;
                if (data.Length >= 12 && Encoding.ASCII.GetString(data.AsSpan(0, 12)) == "RM_DISCOVER?")
                {
                    if (_isSilentMode?.Invoke() == true)
                        continue;
                    var resp = JsonSerializer.Serialize(new { tcp_port = _tcpPort, ip = GetLocalIp() });
                    var bytes = Encoding.UTF8.GetBytes("RM_RESPONSE " + resp);
                    await _udpClient.SendAsync(bytes, result.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "?";
        _onStatus($"Подключение: {remote}");
        bool authenticated = string.IsNullOrEmpty(_password);
        if (authenticated)
            _onClientConnected?.Invoke(remote);
        var stream = client.GetStream();
        var buf = new List<byte>(8192);
        var readBuf = new byte[4096];

        try
        {
            while (_running && client.Connected && !ct.IsCancellationRequested)
            {
                int n = await stream.ReadAsync(readBuf.AsMemory(0, readBuf.Length), ct);
                if (n <= 0) break;
                for (int i = 0; i < n; i++) buf.Add(readBuf[i]);

                while (buf.Count >= Protocol.HeaderSize)
                {
                    var headerArr = new byte[Protocol.HeaderSize];
                    for (int i = 0; i < Protocol.HeaderSize; i++) headerArr[i] = buf[i];
                    if (!Protocol.TryParseHeader(headerArr, out byte cmd, out int plen))
                    {
                        buf.RemoveAt(0);
                        continue;
                    }
                    int need = Protocol.HeaderSize + plen;
                    if (buf.Count < need) break;

                    var payload = buf.Skip(Protocol.HeaderSize).Take(plen).ToArray();
                    buf.RemoveRange(0, need);

                    if (cmd == Protocol.CMD_AUTH)
                    {
                        var authPass = Encoding.UTF8.GetString(payload);
                        if (authPass == _password)
                        {
                            authenticated = true;
                            _authenticated[client] = true;
                            _onClientConnected?.Invoke(remote);
                            var ok = Protocol.MakeHeader(Protocol.CMD_AUTH_OK, 0);
                            await stream.WriteAsync(ok, ct);
                            var info = Protocol.PackServerInfo(_pcName, GetPcVersion());
                            await stream.WriteAsync(info, ct);
                        }
                        else
                        {
                            var fail = Protocol.MakeHeader(Protocol.CMD_AUTH_FAIL, 0);
                            await stream.WriteAsync(fail, ct);
                            return;
                        }
                        continue;
                    }

                    if (cmd == Protocol.CMD_PING)
                    {
                        var pong = Protocol.PackPong();
                        await stream.WriteAsync(pong, ct);
                        continue;
                    }

                    if (!authenticated) continue;

                    if (cmd == Protocol.CMD_MOUSE_MOVE && plen >= 8)
                    {
                        int dx = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4));
                        int dy = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(4, 4));
                        _controller.MouseMove(dx, dy);
                    }
                    else if (cmd == Protocol.CMD_MOUSE_BUTTON && plen >= 2)
                    {
                        _controller.MouseButton(payload[0], payload[1] != 0);
                    }
                    else if (cmd == Protocol.CMD_MOUSE_SCROLL && plen >= 8)
                    {
                        int dx = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4));
                        int dy = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(4, 4));
                        _controller.MouseScroll(dx, dy);
                    }
                    else if ((cmd == Protocol.CMD_KEY_DOWN || cmd == Protocol.CMD_KEY_UP) && plen >= 4)
                    {
                        uint vkey = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(0, 4));
                        _controller.Key(vkey, cmd == Protocol.CMD_KEY_DOWN);
                    }
                    else if (cmd == Protocol.CMD_KEY_PRESS && plen >= 4)
                    {
                        uint vkey = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(0, 4));
                        _controller.KeyPress(vkey);
                    }
                    else if (cmd == Protocol.CMD_CHAR && plen >= 4)
                    {
                        uint codePoint = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(0, 4));
                        _controller.SendChar(codePoint);
                    }
                    else if (cmd == Protocol.CMD_CLIPBOARD_SET)
                    {
                        try { _controller.ClipboardSet(Encoding.UTF8.GetString(payload)); } catch { }
                    }
                    else if (cmd == Protocol.CMD_CLIPBOARD_GET)
                    {
                        try
                        {
                            var text = _controller.ClipboardGet();
                            var data = Protocol.PackClipboardData(text);
                            await stream.WriteAsync(data, ct);
                        }
                        catch { }
                    }
                    else if (cmd == Protocol.CMD_POWER_SHUTDOWN) _controller.PowerShutdown();
                    else if (cmd == Protocol.CMD_POWER_REBOOT) _controller.PowerReboot();
                    else if (cmd == Protocol.CMD_POWER_SLEEP) _controller.PowerSleep();
                    else if (cmd == Protocol.CMD_POWER_LOGOUT) _controller.PowerLogout();
                    else if (cmd == Protocol.CMD_POWER_LOCK) _controller.PowerLock();
                    else if (cmd == Protocol.CMD_VOLUME_UP) _controller.VolumeUp();
                    else if (cmd == Protocol.CMD_VOLUME_DOWN) _controller.VolumeDown();
                    else if (cmd == Protocol.CMD_VOLUME_MUTE) _controller.VolumeMute();
                }
            }
        }
        catch (Exception) { }
        finally
        {
            _authenticated.TryRemove(client, out _);
            try { client.Close(); } catch { }
            _onStatus($"Отключён: {remote}");
        }
    }
}
