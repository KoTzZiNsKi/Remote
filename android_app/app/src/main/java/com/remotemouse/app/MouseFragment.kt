package com.remotemouse.app

import android.os.Bundle
import android.os.SystemClock
import android.view.LayoutInflater
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment

class MouseFragment(private val connectionManager: ConnectionManager, private val pointerSpeed: () -> Float = { 1f }) : Fragment() {

    private var lastX = 0f
    private var lastY = 0f
    private var lastPointerCount = 0
    private var maxPointerCount = 0
    private var scrollStripLastY = 0f
    private var twoFingerDownTime = 0L
    private var multiFingerGestureTime = 0L
    private val gestureDelayMs = 100L
    private var gestureStartX = 0f
    private var multiFingerGesture = false

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        val v = inflater.inflate(R.layout.fragment_mouse, container, false)
        val touchpad = v.findViewById<View>(R.id.touchpad_area)
        val scrollStrip = v.findViewById<View>(R.id.scroll_strip)
        val btnLeft = v.findViewById<View>(R.id.btn_mouse_left)
        val btnMiddle = v.findViewById<View>(R.id.btn_mouse_middle)
        val btnRight = v.findViewById<View>(R.id.btn_mouse_right)
        val minSwipe = 80 * resources.displayMetrics.density

        touchpad.setOnTouchListener { _, event ->
            when (event.actionMasked) {
                MotionEvent.ACTION_DOWN -> {
                    lastX = event.x
                    lastY = event.y
                    lastPointerCount = 1
                    maxPointerCount = 1
                }
                MotionEvent.ACTION_POINTER_DOWN -> {
                    if (event.pointerCount == 2) {
                        twoFingerDownTime = SystemClock.uptimeMillis()
                        lastX = (event.getX(0) + event.getX(1)) / 2
                        lastY = (event.getY(0) + event.getY(1)) / 2
                    }
                    if (event.pointerCount == 3 || event.pointerCount == 4) {
                        multiFingerGesture = true
                        multiFingerGestureTime = SystemClock.uptimeMillis()
                        gestureStartX = (0 until event.pointerCount).map { event.getX(it) }.average().toFloat()
                    }
                    if (event.pointerCount > maxPointerCount) maxPointerCount = event.pointerCount
                }
                MotionEvent.ACTION_MOVE -> {
                    if (event.pointerCount == 1 && maxPointerCount == 1) {
                        val dx = (event.x - lastX) * pointerSpeed()
                        val dy = (event.y - lastY) * pointerSpeed()
                        lastX = event.x
                        lastY = event.y
                        if (connectionManager.isConnected() && (dx != 0f || dy != 0f)) connectionManager.send(Protocol.mouseMove(dx.toInt(), dy.toInt()))
                    } else if (event.pointerCount == 2 && (SystemClock.uptimeMillis() - twoFingerDownTime) >= gestureDelayMs && connectionManager.isConnected()) {
                        val cy = (event.getY(0) + event.getY(1)) / 2
                        val dy = (cy - lastY).toInt()
                        lastY = cy
                        if (dy != 0) connectionManager.send(Protocol.mouseScroll(0, dy))
                    } else if ((event.pointerCount == 3 || event.pointerCount == 4) && multiFingerGesture && (SystemClock.uptimeMillis() - multiFingerGestureTime) >= gestureDelayMs && connectionManager.isConnected()) {
                        val nowX = (0 until event.pointerCount).map { event.getX(it) }.average().toFloat()
                        val delta = nowX - gestureStartX
                        if (kotlin.math.abs(delta) > minSwipe) {
                            when {
                                event.pointerCount == 3 && delta > 0 -> { connectionManager.send(Protocol.keyDown(0x5B)); connectionManager.send(Protocol.keyUp(0x5B)); multiFingerGesture = false }
                                event.pointerCount == 3 && delta < 0 -> { connectionManager.send(Protocol.keyDown(0x12)); connectionManager.send(Protocol.keyDown(0x09)); connectionManager.send(Protocol.keyUp(0x09)); connectionManager.send(Protocol.keyUp(0x12)); multiFingerGesture = false }
                                event.pointerCount == 4 && delta > 0 -> { connectionManager.send(Protocol.keyDown(0x5B)); connectionManager.send(Protocol.keyDown(0x44)); connectionManager.send(Protocol.keyUp(0x44)); connectionManager.send(Protocol.keyUp(0x5B)); multiFingerGesture = false }
                                event.pointerCount == 4 && delta < 0 -> { connectionManager.send(Protocol.keyDown(0x5B)); connectionManager.send(Protocol.keyDown(0x53)); connectionManager.send(Protocol.keyUp(0x53)); connectionManager.send(Protocol.keyUp(0x5B)); multiFingerGesture = false }
                            }
                        }
                    }
                }
                MotionEvent.ACTION_POINTER_UP -> {
                    if (event.pointerCount == 2 && (SystemClock.uptimeMillis() - twoFingerDownTime) >= gestureDelayMs && !multiFingerGesture && connectionManager.isConnected()) {
                        connectionManager.send(Protocol.mouseButton(2, true))
                        connectionManager.send(Protocol.mouseButton(2, false))
                    }
                    if (event.pointerCount <= 2) multiFingerGesture = false
                }
                MotionEvent.ACTION_UP -> {
                    if (event.pointerCount == 1) {
                        lastPointerCount = 0
                        maxPointerCount = 0
                        multiFingerGesture = false
                    }
                }
            }
            true
        }

        scrollStrip.setOnTouchListener { _, event ->
            when (event.action) {
                MotionEvent.ACTION_DOWN -> scrollStripLastY = event.y
                MotionEvent.ACTION_MOVE -> {
                    val dy = (event.y - scrollStripLastY).toInt()
                    scrollStripLastY = event.y
                    if (connectionManager.isConnected() && dy != 0) connectionManager.send(Protocol.mouseScroll(0, dy))
                }
            }
            true
        }

        btnLeft.setOnTouchListener { _, event ->
            when (event.action) {
                MotionEvent.ACTION_DOWN -> if (connectionManager.isConnected()) connectionManager.send(Protocol.mouseButton(1, true))
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> if (connectionManager.isConnected()) connectionManager.send(Protocol.mouseButton(1, false))
            }
            false
        }
        btnMiddle.setOnTouchListener { _, event ->
            when (event.action) {
                MotionEvent.ACTION_DOWN -> if (connectionManager.isConnected()) connectionManager.send(Protocol.mouseButton(3, true))
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> if (connectionManager.isConnected()) connectionManager.send(Protocol.mouseButton(3, false))
            }
            false
        }
        btnRight.setOnTouchListener { _, event ->
            when (event.action) {
                MotionEvent.ACTION_DOWN -> if (connectionManager.isConnected()) connectionManager.send(Protocol.mouseButton(2, true))
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> if (connectionManager.isConnected()) connectionManager.send(Protocol.mouseButton(2, false))
            }
            false
        }
        return v
    }
}
