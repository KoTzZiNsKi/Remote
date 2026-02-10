package com.remotemouse.app

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageButton
import android.widget.LinearLayout
import android.widget.TextView
import androidx.fragment.app.Fragment

class ChooseComputerFragment(
    private val connectionManager: ConnectionManager,
    private val prefs: ConnectionPrefs,
    private val onNavigateToConnect: () -> Unit,
    private val onNavigateToHistory: () -> Unit,
    private val onConnected: () -> Unit
) : Fragment() {

    private var statusText: TextView? = null
    private var panelOptions: View? = null
    private var btnExpand: ImageButton? = null
    private var connectedPcName: String? = null

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        val v = inflater.inflate(R.layout.fragment_choose_computer, container, false)
        v.findViewById<View>(R.id.btn_back).setOnClickListener { requireActivity().onBackPressedDispatcher.onBackPressed() }
        btnExpand = v.findViewById(R.id.btn_expand)
        panelOptions = v.findViewById(R.id.panel_options)
        statusText = v.findViewById(R.id.status_text)
        btnExpand?.setOnClickListener {
            val isExpanded = panelOptions?.visibility == View.VISIBLE
            if (isExpanded) {
                panelOptions?.visibility = View.GONE
                btnExpand?.setImageResource(R.drawable.ic_add_modern)
            } else {
                panelOptions?.visibility = View.VISIBLE
                btnExpand?.setImageResource(R.drawable.ic_remove_modern)
            }
        }
        v.findViewById<View>(R.id.option_history)?.setOnClickListener { onNavigateToHistory() }
        v.findViewById<View>(R.id.option_connect_ip)?.setOnClickListener { onNavigateToConnect() }
        v.findViewById<View>(R.id.option_scan_qr)?.setOnClickListener { onNavigateToConnect() }
        v.findViewById<View>(R.id.faq_how_to_start)?.setOnClickListener { startActivity(Intent(requireContext(), HowToStartActivity::class.java)) }
        updateStatus()
        return v
    }

    fun updateStatus() {
        statusText?.text = connectedPcName ?: getString(R.string.computer_not_found)
    }

    fun setConnectedPcName(name: String?) {
        connectedPcName = name
        updateStatus()
    }
}
