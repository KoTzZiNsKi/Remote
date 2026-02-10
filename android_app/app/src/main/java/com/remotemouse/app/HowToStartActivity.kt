package com.remotemouse.app

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.animation.OvershootInterpolator
import android.widget.FrameLayout
import android.widget.LinearLayout
import androidx.appcompat.app.AppCompatActivity
import androidx.viewpager2.widget.ViewPager2

class HowToStartActivity : AppCompatActivity() {

    private val githubUrl by lazy { getString(R.string.github_repo_url) }
    private var dotsContainer: LinearLayout? = null
    private val dotSizePx by lazy { (12 * resources.displayMetrics.density).toInt() }
    private val dotMarginPx by lazy { (6 * resources.displayMetrics.density).toInt() }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_how_to_start)
        val pager = findViewById<ViewPager2>(R.id.pager)
        dotsContainer = findViewById(R.id.dots)
        val layouts = listOf(R.layout.page_how_to_1, R.layout.page_how_to_2, R.layout.page_how_to_3, R.layout.page_how_to_4)
        pager.adapter = object : androidx.recyclerview.widget.RecyclerView.Adapter<androidx.recyclerview.widget.RecyclerView.ViewHolder>() {
            override fun getItemCount() = layouts.size
            override fun onBindViewHolder(holder: androidx.recyclerview.widget.RecyclerView.ViewHolder, position: Int) {
                val container = holder.itemView as FrameLayout
                container.removeAllViews()
                try {
                    val page = LayoutInflater.from(container.context).inflate(layouts[position], container, false)
                    if (position == 1) page.findViewById<View>(R.id.btn_github)?.setOnClickListener { startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(githubUrl))) }
                    if (position == 3) page.findViewById<View>(R.id.btn_got_it)?.setOnClickListener { finish() }
                    container.addView(page)
                } catch (e: Exception) {
                    android.util.Log.e("HowToStart", "inflate page $position", e)
                }
            }
            override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): androidx.recyclerview.widget.RecyclerView.ViewHolder {
                val v = FrameLayout(parent.context).apply { layoutParams = ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT) }
                return object : androidx.recyclerview.widget.RecyclerView.ViewHolder(v) {}
            }
        }
        for (i in layouts.indices) {
            val dot = View(this).apply {
                layoutParams = LinearLayout.LayoutParams(dotSizePx, dotSizePx).apply { setMargins(dotMarginPx, 0, dotMarginPx, 0) }
                setBackgroundResource(if (i == 0) R.drawable.dot_how_to_selected else R.drawable.dot_how_to)
                scaleX = if (i == 0) 1.2f else 0.85f
                scaleY = if (i == 0) 1.2f else 0.85f
                alpha = if (i == 0) 1f else 0.5f
            }
            dotsContainer?.addView(dot)
        }
        pager.registerOnPageChangeCallback(object : ViewPager2.OnPageChangeCallback() {
            override fun onPageSelected(position: Int) {
                val container = dotsContainer ?: return
                for (i in 0 until container.childCount) {
                    val dot = container.getChildAt(i)
                    val isSelected = i == position
                    dot.setBackgroundResource(if (isSelected) R.drawable.dot_how_to_selected else R.drawable.dot_how_to)
                    dot.animate().scaleX(if (isSelected) 1.2f else 0.85f).scaleY(if (isSelected) 1.2f else 0.85f).alpha(if (isSelected) 1f else 0.5f).setDuration(260).setInterpolator(OvershootInterpolator(1.1f)).start()
                }
            }
        })
    }
}
