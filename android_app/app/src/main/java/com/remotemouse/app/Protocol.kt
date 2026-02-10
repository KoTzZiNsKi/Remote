package com.remotemouse.app

import java.io.ByteArrayOutputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

object Protocol {
    const val MAGIC = "RM1"
    const val VERSION: Byte = 1
    const val CMD_MOUSE_MOVE: Byte = 0x01
    const val CMD_MOUSE_BUTTON: Byte = 0x02
    const val CMD_MOUSE_SCROLL: Byte = 0x03
    const val CMD_KEY_DOWN: Byte = 0x10
    const val CMD_KEY_UP: Byte = 0x11
    const val CMD_KEY_PRESS: Byte = 0x12
    const val CMD_CHAR: Byte = 0x13
    const val CMD_CLIPBOARD_GET: Byte = 0x20
    const val CMD_CLIPBOARD_SET: Byte = 0x21
    const val CMD_CLIPBOARD_DATA: Byte = 0x22
    const val CMD_POWER_SHUTDOWN: Byte = 0x30
    const val CMD_POWER_REBOOT: Byte = 0x31
    const val CMD_POWER_SLEEP: Byte = 0x32
    const val CMD_POWER_LOGOUT: Byte = 0x33
    const val CMD_POWER_LOCK: Byte = 0x34
    const val CMD_VOLUME_UP: Byte = 0x40
    const val CMD_VOLUME_DOWN: Byte = 0x41
    const val CMD_VOLUME_MUTE: Byte = 0x42
    const val CMD_AUTH: Byte = 0xF0.toByte()
    const val CMD_AUTH_OK: Byte = 0xF1.toByte()
    const val CMD_AUTH_FAIL: Byte = 0xF2.toByte()
    const val CMD_SERVER_INFO: Byte = 0xF3.toByte()
    const val CMD_PING: Byte = 0xFE.toByte()
    const val CMD_PONG: Byte = 0xFF.toByte()
    const val HEADER_SIZE = 7

    private fun header(cmd: Byte, payloadLen: Int): ByteArray {
        return byteArrayOf(
            MAGIC[0].code.toByte(), MAGIC[1].code.toByte(), MAGIC[2].code.toByte(),
            VERSION, cmd, (payloadLen shr 8).toByte(), (payloadLen and 0xFF).toByte()
        )
    }

    fun mouseMove(dx: Int, dy: Int): ByteArray {
        val payload = ByteBuffer.allocate(8).order(ByteOrder.BIG_ENDIAN).putInt(dx).putInt(dy).array()
        return header(CMD_MOUSE_MOVE, 8) + payload
    }

    fun mouseButton(button: Int, down: Boolean): ByteArray {
        val payload = byteArrayOf(button.toByte(), if (down) 1 else 0)
        return header(CMD_MOUSE_BUTTON, 2) + payload
    }

    fun mouseScroll(dx: Int, dy: Int): ByteArray {
        val payload = ByteBuffer.allocate(8).order(ByteOrder.BIG_ENDIAN).putInt(dx).putInt(dy).array()
        return header(CMD_MOUSE_SCROLL, 8) + payload
    }

    fun keyDown(vkey: Int): ByteArray {
        val payload = ByteBuffer.allocate(4).order(ByteOrder.BIG_ENDIAN).putInt(vkey).array()
        return header(CMD_KEY_DOWN, 4) + payload
    }

    fun keyUp(vkey: Int): ByteArray {
        val payload = ByteBuffer.allocate(4).order(ByteOrder.BIG_ENDIAN).putInt(vkey).array()
        return header(CMD_KEY_UP, 4) + payload
    }

    fun keyPress(vkey: Int): ByteArray {
        val payload = ByteBuffer.allocate(4).order(ByteOrder.BIG_ENDIAN).putInt(vkey).array()
        return header(CMD_KEY_PRESS, 4) + payload
    }

    fun char(codePoint: Int): ByteArray {
        val payload = ByteBuffer.allocate(4).order(ByteOrder.BIG_ENDIAN).putInt(codePoint).array()
        return header(CMD_CHAR, 4) + payload
    }

    fun clipboardSet(text: String): ByteArray {
        val raw = text.toByteArray(Charsets.UTF_8)
        return header(CMD_CLIPBOARD_SET, raw.size) + raw
    }

    fun clipboardGet(): ByteArray = header(CMD_CLIPBOARD_GET, 0)

    fun auth(password: String): ByteArray {
        val raw = password.toByteArray(Charsets.UTF_8)
        return header(CMD_AUTH, raw.size) + raw
    }

    fun powerShutdown(): ByteArray = header(CMD_POWER_SHUTDOWN, 0)
    fun powerReboot(): ByteArray = header(CMD_POWER_REBOOT, 0)
    fun powerSleep(): ByteArray = header(CMD_POWER_SLEEP, 0)
    fun powerLogout(): ByteArray = header(CMD_POWER_LOGOUT, 0)
    fun powerLock(): ByteArray = header(CMD_POWER_LOCK, 0)
    fun volumeUp(): ByteArray = header(CMD_VOLUME_UP, 0)
    fun volumeDown(): ByteArray = header(CMD_VOLUME_DOWN, 0)
    fun volumeMute(): ByteArray = header(CMD_VOLUME_MUTE, 0)
    fun ping(): ByteArray = header(CMD_PING, 0)

    fun parseHeader(data: ByteArray): Triple<Byte, Byte, Int>? {
        if (data.size < HEADER_SIZE) return null
        if (data[0].toInt().toChar() != MAGIC[0] || data[1].toInt().toChar() != MAGIC[1] || data[2].toInt().toChar() != MAGIC[2]) return null
        val version = data[3]
        val cmd = data[4]
        val plen = ((data[5].toInt() and 0xFF) shl 8) or (data[6].toInt() and 0xFF)
        return Triple(version, cmd, plen)
    }

    fun payloadLength(data: ByteArray): Int {
        if (data.size < HEADER_SIZE) return -1
        return ((data[5].toInt() and 0xFF) shl 8) or (data[6].toInt() and 0xFF)
    }
}
