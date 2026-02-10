package com.remotemouse.app

import android.content.Context
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.KeyEvent
import android.view.LayoutInflater
import android.view.inputmethod.EditorInfo
import android.view.inputmethod.InputMethodManager
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.EditText
import androidx.fragment.app.Fragment

class KeyboardFragment(private val connectionManager: ConnectionManager) : Fragment() {

    private var shiftDown = false
    private var ctrlDown = false
    private var altDown = false
    private var ignoreTextChange = false
    private var lastLength = 0
    private var nextNewlineIsShift = false

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        val v = inflater.inflate(R.layout.fragment_keyboard, container, false)
        val input = v.findViewById<EditText>(R.id.keyboard_input)
        val modShift = v.findViewById<Button>(R.id.mod_shift)
        val modCtrl = v.findViewById<Button>(R.id.mod_ctrl)
        val modAlt = v.findViewById<Button>(R.id.mod_alt)
        val modWin = v.findViewById<Button>(R.id.mod_win)
        val btnLayoutSwitch = v.findViewById<View>(R.id.btn_layout_switch)

        btnLayoutSwitch.setOnClickListener {
            if (!connectionManager.isConnected()) return@setOnClickListener
            connectionManager.send(Protocol.keyDown(VKey.LWIN))
            connectionManager.send(Protocol.keyDown(VKey.SPACE))
            connectionManager.send(Protocol.keyUp(VKey.SPACE))
            connectionManager.send(Protocol.keyUp(VKey.LWIN))
        }

        fun updateModifierStyle(btn: Button, pressed: Boolean) {
            btn.isSelected = pressed
            btn.elevation = (if (pressed) 0 else 2) * resources.displayMetrics.density
        }
        modShift.setOnClickListener {
            if (!connectionManager.isConnected()) return@setOnClickListener
            shiftDown = !shiftDown
            connectionManager.send(if (shiftDown) Protocol.keyDown(VKey.SHIFT) else Protocol.keyUp(VKey.SHIFT))
            updateModifierStyle(modShift, shiftDown)
        }
        modCtrl.setOnClickListener {
            if (!connectionManager.isConnected()) return@setOnClickListener
            ctrlDown = !ctrlDown
            connectionManager.send(if (ctrlDown) Protocol.keyDown(VKey.CONTROL) else Protocol.keyUp(VKey.CONTROL))
            updateModifierStyle(modCtrl, ctrlDown)
        }
        modAlt.setOnClickListener {
            if (!connectionManager.isConnected()) return@setOnClickListener
            altDown = !altDown
            connectionManager.send(if (altDown) Protocol.keyDown(VKey.MENU) else Protocol.keyUp(VKey.MENU))
            updateModifierStyle(modAlt, altDown)
        }
        modWin.setOnClickListener {
            if (!connectionManager.isConnected()) return@setOnClickListener
            connectionManager.send(Protocol.keyDown(VKey.LWIN))
            connectionManager.send(Protocol.keyUp(VKey.LWIN))
        }

        input.setOnKeyListener { _, keyCode, event ->
            if (keyCode == KeyEvent.KEYCODE_DEL && event.action == KeyEvent.ACTION_DOWN && connectionManager.isConnected()) {
                if (input.text.isNullOrEmpty() || input.text!!.isEmpty()) sendKey(VKey.BACK)
            }
            if (keyCode == KeyEvent.KEYCODE_ENTER && event.action == KeyEvent.ACTION_DOWN && event.isShiftPressed && connectionManager.isConnected()) nextNewlineIsShift = true
            false
        }

        input.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
            override fun afterTextChanged(s: Editable?) {
                if (ignoreTextChange) return
                val str = s?.toString() ?: ""
                if (str.length < lastLength && connectionManager.isConnected()) {
                    repeat(lastLength - str.length) { sendKey(VKey.BACK) }
                    lastLength = str.length
                    return
                }
                if (str.length > lastLength) {
                    var idx = lastLength
                    while (idx < str.length && connectionManager.isConnected()) {
                        val cp = Character.codePointAt(str, idx)
                        if (cp >= 0x20 && cp != 0x7F) connectionManager.send(Protocol.char(cp))
                        else if (cp == '\n'.code) {
                            if (nextNewlineIsShift) {
                                connectionManager.send(Protocol.keyDown(VKey.SHIFT))
                                connectionManager.send(Protocol.keyDown(VKey.RETURN))
                                connectionManager.send(Protocol.keyUp(VKey.RETURN))
                                connectionManager.send(Protocol.keyUp(VKey.SHIFT))
                                nextNewlineIsShift = false
                            } else {
                                connectionManager.send(Protocol.keyDown(VKey.RETURN))
                                connectionManager.send(Protocol.keyUp(VKey.RETURN))
                            }
                        }
                        idx += Character.charCount(cp)
                    }
                    lastLength = str.length
                }
            }
        })

        input.setOnEditorActionListener { _, actionId, event ->
            if (actionId == EditorInfo.IME_ACTION_DONE || (event?.keyCode == KeyEvent.KEYCODE_ENTER && event.action == KeyEvent.ACTION_DOWN)) {
                if (connectionManager.isConnected()) {
                    if (event?.isShiftPressed == true) {
                        connectionManager.send(Protocol.keyDown(VKey.SHIFT))
                        connectionManager.send(Protocol.keyDown(VKey.RETURN))
                        connectionManager.send(Protocol.keyUp(VKey.RETURN))
                        connectionManager.send(Protocol.keyUp(VKey.SHIFT))
                    } else {
                        connectionManager.send(Protocol.keyDown(VKey.RETURN))
                        connectionManager.send(Protocol.keyUp(VKey.RETURN))
                        ignoreTextChange = true
                        input.setText("")
                        lastLength = 0
                        input.post { ignoreTextChange = false }
                    }
                }
                true
            } else false
        }

        input.setOnFocusChangeListener { _, hasFocus ->
            if (hasFocus) (requireContext().getSystemService(Context.INPUT_METHOD_SERVICE) as? InputMethodManager)?.showSoftInput(input, InputMethodManager.SHOW_IMPLICIT)
        }

        v.postDelayed({
            if (!isAdded) return@postDelayed
            input.requestFocus()
            (requireContext().getSystemService(Context.INPUT_METHOD_SERVICE) as? InputMethodManager)?.showSoftInput(input, InputMethodManager.SHOW_IMPLICIT)
        }, 400)
        return v
    }

    private fun sendKey(vkey: Int) {
        if (!connectionManager.isConnected()) return
        connectionManager.send(Protocol.keyDown(vkey))
        connectionManager.send(Protocol.keyUp(vkey))
    }
}
