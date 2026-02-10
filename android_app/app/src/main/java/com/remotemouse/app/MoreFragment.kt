package com.remotemouse.app

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import androidx.fragment.app.Fragment

class MoreFragment(private val connectionManager: ConnectionManager, private val prefs: ConnectionPrefs) : Fragment() {

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        val v = inflater.inflate(R.layout.fragment_more, container, false)
        v.findViewById<View>(R.id.btn_lock).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.powerLock()) }
        v.findViewById<View>(R.id.btn_sleep).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.powerSleep()) }
        v.findViewById<View>(R.id.btn_logout).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.powerLogout()) }
        v.findViewById<View>(R.id.btn_reboot).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.powerReboot()) }
        v.findViewById<View>(R.id.btn_shutdown).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.powerShutdown()) }
        v.findViewById<View>(R.id.btn_vol_down).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.volumeDown()) }
        v.findViewById<View>(R.id.btn_vol_up).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.volumeUp()) }
        v.findViewById<View>(R.id.btn_vol_mute).setOnClickListener { if (connectionManager.isConnected()) connectionManager.send(Protocol.volumeMute()) }
        val clipboardText = v.findViewById<EditText>(R.id.clipboard_text)
        connectionManager.setClipboardReceiver { text -> clipboardText?.setText(text) }
        v.findViewById<View>(R.id.btn_clipboard_get)?.setOnClickListener {
            if (connectionManager.isConnected()) connectionManager.send(Protocol.clipboardGet())
        }
        v.findViewById<View>(R.id.btn_clipboard_send)?.setOnClickListener {
            if (connectionManager.isConnected()) connectionManager.send(Protocol.clipboardSet(clipboardText?.text?.toString() ?: ""))
        }
        val seek = v.findViewById<android.widget.SeekBar>(R.id.pointer_speed)
        seek?.progress = (prefs.pointerSpeed * 100).toInt().coerceIn(25, 300)
        seek?.setOnSeekBarChangeListener(object : android.widget.SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: android.widget.SeekBar?, progress: Int, fromUser: Boolean) { if (fromUser) prefs.pointerSpeed = progress / 100f }
            override fun onStartTrackingTouch(seekBar: android.widget.SeekBar?) {}
            override fun onStopTrackingTouch(seekBar: android.widget.SeekBar?) {}
        })
        return v
    }
}
