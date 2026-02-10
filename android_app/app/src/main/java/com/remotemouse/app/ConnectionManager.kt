package com.remotemouse.app

import android.os.Handler
import android.os.Looper
import android.util.Log
import java.io.IOException
import java.io.InputStream
import java.io.OutputStream
import java.net.InetSocketAddress
import java.net.Socket
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors
import java.util.concurrent.LinkedBlockingQueue
import java.util.concurrent.atomic.AtomicBoolean

class ConnectionManager(
    private val onConnected: (String, String) -> Unit,
    private val onDisconnected: (String?) -> Unit,
    private val onAuthResult: (Boolean, String?) -> Unit
) {
    @Volatile
    var clipboardReceiver: ((String) -> Unit)? = null
        private set
    fun setClipboardReceiver(cb: ((String) -> Unit)?) { clipboardReceiver = cb }

    private var socket: Socket? = null
    private var output: OutputStream? = null
    private var input: InputStream? = null
    private val writerExecutor: ExecutorService = Executors.newSingleThreadExecutor()
    private val mainHandler = Handler(Looper.getMainLooper())
    private val connected = AtomicBoolean(false)
    private val authenticated = AtomicBoolean(false)
    private val sendQueue = LinkedBlockingQueue<ByteArray>()
    private val readBuf = ByteArray(Protocol.HEADER_SIZE + 65536)
    private val headerBuf = ByteArray(Protocol.HEADER_SIZE)

    fun connect(host: String, port: Int, password: String?) {
        writerExecutor.execute {
            try {
                closeConnection()
                sendQueue.drainTo(mutableListOf<ByteArray>())
                val s = Socket()
                s.tcpNoDelay = true
                s.connect(InetSocketAddress(host, port), 15000)
                s.soTimeout = 0
                socket = s
                output = s.getOutputStream()
                input = s.getInputStream()
                connected.set(true)
                output?.write(Protocol.auth(password ?: ""))
                output?.flush()
                readFully(input!!, headerBuf, 0, Protocol.HEADER_SIZE)
                when (headerBuf[4]) {
                    Protocol.CMD_AUTH_OK -> {
                        authenticated.set(true)
                        var serverName = ""
                        var serverVersion = ""
                        try {
                            s.soTimeout = 2000
                            readFully(input!!, readBuf, 0, Protocol.HEADER_SIZE)
                            if (readBuf[4] == Protocol.CMD_SERVER_INFO) {
                                val plen = ((readBuf[5].toInt() and 0xFF) shl 8) or (readBuf[6].toInt() and 0xFF)
                                if (plen >= 4 && plen <= readBuf.size - Protocol.HEADER_SIZE) {
                                    readFully(input!!, readBuf, Protocol.HEADER_SIZE, plen)
                                    val nameLen = ((readBuf[Protocol.HEADER_SIZE].toInt() and 0xFF) shl 8) or (readBuf[Protocol.HEADER_SIZE + 1].toInt() and 0xFF)
                                    val versionLen = ((readBuf[Protocol.HEADER_SIZE + 2 + nameLen].toInt() and 0xFF) shl 8) or (readBuf[Protocol.HEADER_SIZE + 3 + nameLen].toInt() and 0xFF)
                                    serverName = if (nameLen > 0) String(readBuf, Protocol.HEADER_SIZE + 2, nameLen, Charsets.UTF_8) else ""
                                    serverVersion = if (versionLen > 0) String(readBuf, Protocol.HEADER_SIZE + 4 + nameLen, versionLen, Charsets.UTF_8) else ""
                                }
                            }
                        } catch (_: Exception) { }
                        try { s.soTimeout = 0 } catch (_: Exception) { }
                        val name = serverName
                        val ver = serverVersion
                        mainHandler.post {
                            onConnected(name, ver)
                            onAuthResult(true, null)
                        }
                    }
                    Protocol.CMD_AUTH_FAIL -> {
                        connected.set(false)
                        try { s.close() } catch (_: Exception) {}
                        socket = null
                        output = null
                        input = null
                        val msg = if (!password.isNullOrEmpty()) "Неверный пароль" else "Требуется пароль"
                        mainHandler.post {
                            onAuthResult(false, msg)
                        }
                        return@execute
                    }
                    else -> {
                        connected.set(false)
                        try { s.close() } catch (_: Exception) {}
                        socket = null
                        output = null
                        input = null
                        mainHandler.post {
                            onAuthResult(false, "Требуется пароль")
                        }
                        return@execute
                    }
                }
                Thread { readLoop() }.start()
                while (connected.get()) {
                    val data = sendQueue.take()
                    if (data.isEmpty()) break
                    try {
                        output?.write(data)
                        output?.flush()
                    } catch (e: IOException) {
                        if (connected.get()) mainHandler.post { onDisconnected(e.message) }
                        break
                    }
                }
            } catch (e: Exception) {
                Log.e("ConnectionManager", "connect", e)
                mainHandler.post { onDisconnected(e.message ?: "Ошибка подключения") }
            } finally {
                connected.set(false)
            }
        }
    }

    private fun closeConnection() {
        connected.set(false)
        authenticated.set(false)
        try { socket?.close() } catch (_: Exception) {}
        socket = null
        output = null
        input = null
    }

    private fun readFully(input: InputStream, buf: ByteArray, offset: Int, len: Int) {
        var n = 0
        while (n < len) {
            val r = input.read(buf, offset + n, len - n)
            if (r <= 0) throw IOException("EOF")
            n += r
        }
    }

    private fun readLoop() {
        val inp = input
        try {
            while (connected.get() && inp != null) {
                readFully(inp, readBuf, 0, Protocol.HEADER_SIZE)
                val parsed = Protocol.parseHeader(readBuf) ?: break
                val (_, cmd, plen) = parsed
                if (plen > readBuf.size - Protocol.HEADER_SIZE) break
                if (plen > 0) readFully(inp, readBuf, Protocol.HEADER_SIZE, plen)
                when (cmd) {
                    Protocol.CMD_CLIPBOARD_DATA -> if (plen > 0) {
                        val text = String(readBuf, Protocol.HEADER_SIZE, plen, Charsets.UTF_8)
                        clipboardReceiver?.invoke(text)
                    }
                }
            }
        } catch (e: Exception) {
            if (connected.get()) {
                Log.e("ConnectionManager", "readLoop", e)
                connected.set(false)
                sendQueue.offer(ByteArray(0))
                closeConnection()
                mainHandler.post { onDisconnected("Соединение разорвано") }
            }
        }
    }

    fun send(data: ByteArray) {
        if (connected.get()) sendQueue.offer(data)
    }

    fun disconnect() {
        connected.set(false)
        authenticated.set(false)
        sendQueue.offer(ByteArray(0))
        try { socket?.close() } catch (_: Exception) {}
        socket = null
        output = null
        input = null
        mainHandler.post { onDisconnected(null) }
    }

    fun isConnected(): Boolean = connected.get()
    fun isAuthenticated(): Boolean = authenticated.get()
}
