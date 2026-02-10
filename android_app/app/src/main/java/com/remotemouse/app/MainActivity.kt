package com.remotemouse.app

import android.animation.AnimatorSet
import android.animation.ObjectAnimator
import android.os.Bundle
import android.view.View
import android.view.animation.AccelerateDecelerateInterpolator
import android.view.animation.OvershootInterpolator
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.fragment.app.commit
import com.google.android.material.bottomnavigation.BottomNavigationView

class MainActivity : AppCompatActivity() {

    private lateinit var connectionManager: ConnectionManager
    private lateinit var connectionPrefs: ConnectionPrefs
    private lateinit var historyPrefs: ConnectionHistoryPrefs
    private var chooseComputerFragment: ChooseComputerFragment? = null
    private var connectFragment: ConnectFragment? = null
    private var bottomNav: BottomNavigationView? = null
    private var headerBar: View? = null
    private var headerStatus: TextView? = null
    private var headerStatusPill: LinearLayout? = null
    private var headerStatusDot: ImageView? = null
    private var lastServerName: String = ""
    private var lastServerVersion: String = ""
    private var statusPulseAnim: AnimatorSet? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
        connectionPrefs = ConnectionPrefs(this)
        historyPrefs = ConnectionHistoryPrefs(this)
        bottomNav = findViewById(R.id.bottom_nav)
        bottomNav?.visibility = View.GONE
        headerBar = findViewById(R.id.header_bar)
        headerStatus = findViewById(R.id.header_status)
        headerStatusPill = findViewById(R.id.header_status_pill)
        headerStatusDot = findViewById(R.id.header_status_dot)
        connectionManager = ConnectionManager(
            onConnected = { name, version ->
                runOnUiThread {
                    lastServerName = name
                    lastServerVersion = version
                    onConnectionChanged(true)
                }
            },
            onDisconnected = { msg -> runOnUiThread { onConnectionChanged(false, msg) } },
            onAuthResult = { ok, message ->
                runOnUiThread {
                    if (!ok) {
                        connectFragment?.showPasswordField()
                        Toast.makeText(this, message ?: "Требуется пароль", Toast.LENGTH_SHORT).show()
                    }
                }
            }
        )
        findViewById<View>(R.id.header_btn_back).setOnClickListener { connectionManager.disconnect() }
        findViewById<View>(R.id.header_btn_disconnect).setOnClickListener { connectionManager.disconnect() }
        chooseComputerFragment = ChooseComputerFragment(
            connectionManager = connectionManager,
            prefs = connectionPrefs,
            onNavigateToConnect = { showConnectFragment() },
            onNavigateToHistory = { showHistoryFragment() },
            onConnected = { showMainTabs() }
        )
        if (savedInstanceState == null) {
            supportFragmentManager.commit {
                setCustomAnimations(R.anim.fade_scale_in, R.anim.fade_scale_out)
                replace(R.id.fragment_container, chooseComputerFragment!!)
            }
        }
    }

    private fun showConnectFragment() {
        connectFragment = ConnectFragment(connectionManager, connectionPrefs, onBack = { supportFragmentManager.popBackStack() }, showBackButton = true)
        supportFragmentManager.commit {
            setCustomAnimations(R.anim.slide_in_right, R.anim.slide_out_left, R.anim.slide_in_left, R.anim.slide_out_right)
            replace(R.id.fragment_container, connectFragment!!)
            addToBackStack(null)
        }
    }

    private fun showHistoryFragment() {
        val historyFragment = HistoryFragment(historyPrefs, onBack = { supportFragmentManager.popBackStack() }, onSelectEntry = { entry ->
            supportFragmentManager.popBackStack()
            connectionPrefs.host = entry.host
            connectionPrefs.port = entry.port
            showConnectFragment()
        })
        supportFragmentManager.commit {
            setCustomAnimations(R.anim.slide_in_right, R.anim.slide_out_left, R.anim.slide_in_left, R.anim.slide_out_right)
            replace(R.id.fragment_container, historyFragment)
            addToBackStack(null)
        }
    }

    private fun onConnectionChanged(connected: Boolean, errorMessage: String? = null) {
        try {
            if (isFinishing) return
            chooseComputerFragment?.updateStatus()
            connectFragment?.updateStatus(connected)
            if (connected) {
                val name = if (lastServerName.isNotEmpty()) lastServerName else "Компьютер"
                historyPrefs.add(name, lastServerVersion, connectionPrefs.host, connectionPrefs.port)
                chooseComputerFragment?.setConnectedPcName(name)
                connectFragment?.setConnectButtonDisconnect()
                headerBar?.findViewById<View>(R.id.header_btn_disconnect)?.visibility = View.VISIBLE
                showMainTabs()
            } else {
                connectFragment?.setConnectButtonConnect()
                val wasShowingMainTabs = bottomNav?.visibility == View.VISIBLE
                if (wasShowingMainTabs) {
                    headerBar?.visibility = View.GONE
                    setStatusUi(connected = false)
                    stopStatusPulse()
                    headerBar?.findViewById<View>(R.id.header_btn_disconnect)?.visibility = View.GONE
                    headerBar?.findViewById<View>(R.id.header_btn_back)?.visibility = View.GONE
                    bottomNav?.visibility = View.GONE
                    supportFragmentManager.commit {
                        setCustomAnimations(R.anim.fade_scale_in, R.anim.fade_scale_out)
                        replace(R.id.fragment_container, chooseComputerFragment!!)
                    }
                }
                errorMessage?.let { msg ->
                    val friendly = getString(R.string.error_connect_failed, connectionPrefs.port)
                    val hint = if (msg.contains("EHOSTUNREACH", ignoreCase = true) || msg.contains("No route to host", ignoreCase = true)) {
                        " " + getString(R.string.error_no_route_hint)
                    } else ""
                    val full = if (msg.isNotBlank()) "$friendly $msg$hint" else friendly
                    Toast.makeText(this, full, Toast.LENGTH_LONG).show()
                }
            }
        } catch (e: Exception) {
            android.util.Log.e("MainActivity", "onConnectionChanged", e)
        }
    }

    private fun showMainTabs() {
        supportFragmentManager.popBackStack(null, 1)
        headerBar?.visibility = View.VISIBLE
        setStatusUi(connected = true)
        startStatusPulse()
        headerBar?.findViewById<View>(R.id.header_btn_back)?.visibility = View.VISIBLE
        headerBar?.findViewById<View>(R.id.header_btn_disconnect)?.visibility = View.VISIBLE
        bottomNav?.visibility = View.VISIBLE
        connectFragment = ConnectFragment(connectionManager, connectionPrefs, onBack = {}, showBackButton = false)
        supportFragmentManager.commit {
            setCustomAnimations(R.anim.fade_scale_in, R.anim.fade_scale_out)
            replace(R.id.fragment_container, connectFragment!!)
        }
        bottomNav?.setOnItemSelectedListener { item ->
            val fragment = when (item.itemId) {
                R.id.nav_connect -> connectFragment
                R.id.nav_mouse -> MouseFragment(connectionManager) { connectionPrefs.pointerSpeed }
                R.id.nav_keyboard -> KeyboardFragment(connectionManager)
                R.id.nav_more -> MoreFragment(connectionManager, connectionPrefs)
                else -> null
            }
            if (fragment != null) {
                supportFragmentManager.commit {
                    setCustomAnimations(R.anim.fade_scale_in, R.anim.fade_scale_out)
                    replace(R.id.fragment_container, fragment)
                }
            }
            true
        }
    }

    private fun setStatusUi(connected: Boolean) {
        try {
            if (connected) {
                headerStatusPill?.setBackgroundResource(R.drawable.bg_status_pill_connected)
                headerStatus?.setText(R.string.connected)
                headerStatus?.setTextColor(ContextCompat.getColor(this, R.color.status_connected))
                headerStatusDot?.setColorFilter(ContextCompat.getColor(this, R.color.status_connected))
            } else {
                headerStatusPill?.setBackgroundResource(R.drawable.bg_status_pill)
                headerStatus?.setText(R.string.disconnected)
                headerStatus?.setTextColor(ContextCompat.getColor(this, R.color.status_disconnected))
                headerStatusDot?.setColorFilter(ContextCompat.getColor(this, R.color.status_disconnected))
            }
        } catch (e: Exception) {
            android.util.Log.e("MainActivity", "setStatusUi", e)
        }
    }

    private fun startStatusPulse() {
        stopStatusPulse()
        val dot = headerStatusDot ?: return
        val scaleX = ObjectAnimator.ofFloat(dot, View.SCALE_X, 0.85f, 1.15f).apply { duration = 800; repeatCount = ObjectAnimator.INFINITE; repeatMode = ObjectAnimator.REVERSE }
        val scaleY = ObjectAnimator.ofFloat(dot, View.SCALE_Y, 0.85f, 1.15f).apply { duration = 800; repeatCount = ObjectAnimator.INFINITE; repeatMode = ObjectAnimator.REVERSE }
        val alpha = ObjectAnimator.ofFloat(dot, View.ALPHA, 0.6f, 1f).apply { duration = 800; repeatCount = ObjectAnimator.INFINITE; repeatMode = ObjectAnimator.REVERSE }
        statusPulseAnim = AnimatorSet().apply { playTogether(scaleX, scaleY, alpha); interpolator = OvershootInterpolator(1.2f); start() }
    }

    private fun stopStatusPulse() {
        statusPulseAnim?.cancel()
        statusPulseAnim = null
        headerStatusDot?.animate()?.scaleX(1f)?.scaleY(1f)?.alpha(1f)?.setDuration(0)?.start()
    }

    @Deprecated("Deprecated in Java")
    override fun onBackPressed() {
        if (supportFragmentManager.backStackEntryCount > 0) supportFragmentManager.popBackStack()
        else super.onBackPressed()
    }

    override fun onDestroy() {
        connectionManager.disconnect()
        stopStatusPulse()
        super.onDestroy()
    }
}
