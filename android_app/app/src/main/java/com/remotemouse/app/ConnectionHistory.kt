package com.remotemouse.app

data class ConnectionHistoryEntry(val name: String, val version: String, val host: String, val port: Int, val timestamp: Long)

class ConnectionHistoryPrefs(private val context: android.content.Context) {
    private val prefs = context.getSharedPreferences("history", android.content.Context.MODE_PRIVATE)
    private val maxEntries = 50

    fun add(name: String, version: String, host: String, port: Int) {
        val list = getAll().toMutableList()
        list.removeAll { it.host == host && it.port == port }
        list.add(0, ConnectionHistoryEntry(name, version, host, port, System.currentTimeMillis()))
        val toSave = list.take(maxEntries)
        prefs.edit().putString("entries", toSave.joinToString("\u0001") { "${it.name}\t${it.version}\t${it.host}\t${it.port}\t${it.timestamp}" }).apply()
    }

    fun getAll(): List<ConnectionHistoryEntry> {
        val raw = prefs.getString("entries", "") ?: return emptyList()
        if (raw.isEmpty()) return emptyList()
        return raw.split("\u0001").mapNotNull { s ->
            val p = s.split("\t")
            if (p.size >= 5) ConnectionHistoryEntry(p[0], p[1], p[2], p[3].toIntOrNull() ?: 1978, p[4].toLongOrNull() ?: 0L) else null
        }
    }
}
