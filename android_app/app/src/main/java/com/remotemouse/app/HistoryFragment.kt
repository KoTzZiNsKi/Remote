package com.remotemouse.app

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.ListView
import android.widget.TextView
import androidx.fragment.app.Fragment
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class HistoryFragment(
    private val historyPrefs: ConnectionHistoryPrefs,
    private val onBack: () -> Unit,
    private val onSelectEntry: (ConnectionHistoryEntry) -> Unit
) : Fragment() {

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        val v = inflater.inflate(R.layout.fragment_history, container, false)
        v.findViewById<View>(R.id.btn_back).setOnClickListener { onBack() }
        val list = v.findViewById<ListView>(R.id.history_list)
        val entries = historyPrefs.getAll()
        val adapter = object : ArrayAdapter<ConnectionHistoryEntry>(requireContext(), R.layout.item_history, R.id.item_name, entries) {
            override fun getView(position: Int, convertView: View?, parent: android.view.ViewGroup): View {
                val row = super.getView(position, convertView, parent)
                getItem(position)?.let { e ->
                    row.findViewById<TextView>(R.id.item_name)?.text = e.name
                    row.findViewById<TextView>(R.id.item_version)?.text = e.version
                    row.findViewById<TextView>(R.id.item_date)?.text = SimpleDateFormat("dd.MM.yyyy HH:mm", Locale.getDefault()).format(Date(e.timestamp))
                }
                return row
            }
        }
        list.adapter = adapter
        list.onItemClickListener = AdapterView.OnItemClickListener { _, _, position, _ ->
            if (position in entries.indices) onSelectEntry(entries[position])
        }
        return v
    }
}
