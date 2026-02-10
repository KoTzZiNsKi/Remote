package com.remotemouse.app

import android.content.Context
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import com.journeyapps.barcodescanner.ScanContract
import com.journeyapps.barcodescanner.ScanOptions

class ConnectFragment(
    private val connectionManager: ConnectionManager,
    private val prefs: ConnectionPrefs,
    private val onBack: () -> Unit,
    private val showBackButton: Boolean = true
) : Fragment() {

    private lateinit var statusLabel: TextView
    private lateinit var editIp: EditText
    private lateinit var editPassword: EditText
    private lateinit var btnConnect: Button
    private var btnScanQr: View? = null

    private val requestCamera = registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
        if (granted) launchQrScannerInternal()
        else Toast.makeText(requireContext(), "Нужен доступ к камере для сканирования QR", Toast.LENGTH_SHORT).show()
    }

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        val v = inflater.inflate(R.layout.fragment_connect, container, false)
        val btnBack = v.findViewById<View>(R.id.btn_back)
        btnBack.visibility = if (showBackButton) View.VISIBLE else View.GONE
        btnBack.setOnClickListener { onBack() }
        statusLabel = v.findViewById(R.id.status_label)
        editIp = v.findViewById(R.id.edit_ip)
        editPassword = v.findViewById(R.id.edit_password)
        btnConnect = v.findViewById(R.id.btn_connect)
        btnScanQr = v.findViewById(R.id.btn_scan_qr)
        editIp.setText(prefs.host)
        editPassword.setText(prefs.password)
        if (connectionManager.isConnected()) {
            setConnectButtonDisconnect()
        } else {
            btnConnect.setText(R.string.connect)
            btnConnect.setOnClickListener { doConnect() }
        }
        btnScanQr?.setOnClickListener { launchQrScanner() }
        updateStatus(connectionManager.isConnected())
        return v
    }

    private fun doConnect() {
        val raw = editIp.text?.toString()?.trim() ?: ""
        val password = editPassword.text?.toString()?.trim()
        if (raw.isEmpty()) {
            Toast.makeText(requireContext(), "Введите IP", Toast.LENGTH_SHORT).show()
            return
        }
        var host = raw
        var port = DEFAULT_PORT
        val colon = raw.lastIndexOf(':')
        if (colon > 0 && colon < raw.length - 1) {
            val portStr = raw.substring(colon + 1)
            portStr.toIntOrNull()?.takeIf { it in 1..65535 }?.let {
                host = raw.substring(0, colon).trim()
                port = it
            }
        }
        statusLabel.visibility = View.VISIBLE
        statusLabel.text = "Подключение…"
        statusLabel.setTextColor(resources.getColor(android.R.color.white, null))
        prefs.save(host, port, password)
        connectionManager.connect(host, port, password)
    }

    fun showPasswordField() {
        if (::editPassword.isInitialized) editPassword.visibility = View.VISIBLE
    }

    companion object {
        private const val DEFAULT_PORT = 1978
    }

    private val qrLauncher = registerForActivityResult(ScanContract()) { result ->
        view?.post {
            if (!isAdded) return@post
            try {
                val contents = result.contents
                if (contents.isNullOrBlank()) return@post
                applyQrContent(contents)
            } catch (e: Exception) {
                Toast.makeText(requireContext(), "Ошибка: ${e.message}", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun launchQrScanner() {
        when {
            ContextCompat.checkSelfPermission(requireContext(), android.Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED ->
                launchQrScannerInternal()
            else -> requestCamera.launch(android.Manifest.permission.CAMERA)
        }
    }

    private fun launchQrScannerInternal() {
        try {
            qrLauncher.launch(
                ScanOptions()
                    .setDesiredBarcodeFormats(ScanOptions.QR_CODE)
                    .setPrompt("Наведите камеру на QR-код с ПК")
                    .setOrientationLocked(false)
                    .setCaptureActivity(PortraitCaptureActivity::class.java)
            )
        } catch (e: Exception) {
            Toast.makeText(requireContext(), "Ошибка запуска камеры", Toast.LENGTH_SHORT).show()
        }
    }

    private fun applyQrContent(contents: String) {
        try {
            val s = contents.trim()
            when {
                s.startsWith("remotemouse://") || s.startsWith("remotemouse:") -> {
                    val uri = Uri.parse(if (s.contains("://")) s else s.replace("remotemouse:", "remotemouse://"))
                    if (!uri.host.isNullOrBlank()) {
                        editIp.setText(uri.host)
                        uri.getQueryParameter("pwd")?.let {
                            editPassword.setText(it)
                            editPassword.visibility = View.VISIBLE
                        }
                        Toast.makeText(requireContext(), "Данные подставлены", Toast.LENGTH_SHORT).show()
                        return
                    }
                }
                s.contains(":") -> {
                    val parts = s.split(":")
                    if (parts.size >= 1 && parts[0].isNotBlank()) {
                        editIp.setText(s)
                        Toast.makeText(requireContext(), "Данные подставлены", Toast.LENGTH_SHORT).show()
                        return
                    }
                }
                s.isNotBlank() -> {
                    editIp.setText(s)
                    Toast.makeText(requireContext(), "Данные подставлены", Toast.LENGTH_SHORT).show()
                    return
                }
            }
            Toast.makeText(requireContext(), "Неверный QR-код", Toast.LENGTH_SHORT).show()
        } catch (e: Exception) {
            Toast.makeText(requireContext(), "Ошибка: ${e.message}", Toast.LENGTH_SHORT).show()
        }
    }

    fun updateStatus(connected: Boolean) {
        if (!isAdded || view == null) return
        try {
            statusLabel.setText(if (connected) R.string.connected else R.string.disconnected)
            statusLabel.setTextColor(resources.getColor(android.R.color.white, null))
            statusLabel.visibility = View.VISIBLE
            btnConnect.setText(if (connected) R.string.disconnect else R.string.connect)
        } catch (_: Exception) { }
    }

    fun setConnectButtonDisconnect() {
        if (!isAdded || view == null) return
        try {
            btnConnect.setText(R.string.disconnect)
            btnConnect.setOnClickListener { connectionManager.disconnect() }
        } catch (_: Exception) { }
    }

    fun setConnectButtonConnect() {
        if (!isAdded || view == null) return
        try {
            btnConnect.setText(R.string.connect)
            btnConnect.setOnClickListener { doConnect() }
        } catch (_: Exception) { }
    }
}

class ConnectionPrefs(context: Context) {
    private val prefs = context.getSharedPreferences("connection", Context.MODE_PRIVATE)
    var host: String get() = prefs.getString("host", "") ?: ""
        set(v) { prefs.edit().putString("host", v).apply() }
    var port: Int get() = prefs.getInt("port", 1978)
        set(v) { prefs.edit().putInt("port", v).apply() }
    var password: String get() = prefs.getString("password", "") ?: ""
        set(v) { prefs.edit().putString("password", v).apply() }
    var pointerSpeed: Float get() = prefs.getFloat("pointer_speed", 1f).coerceIn(0.25f, 3f)
        set(v) { prefs.edit().putFloat("pointer_speed", v.coerceIn(0.25f, 3f)).apply() }
    fun save(host: String, port: Int, password: String?) {
        this.host = host
        this.port = port
        this.password = password ?: ""
    }
}
