package com.example.opticaltransfer.platform

import android.app.Activity
import android.view.WindowManager

object ScreenBrightnessHelper {

    /**
     * Maximize display brightness and keep screen ON during optical transmission
     */
    fun setMaxBrightnessAndKeepAwake(activity: Activity, enable: Boolean) {
        activity.runOnUiThread {
            val window = activity.window
            val layoutParams = window.attributes

            if (enable) {
                layoutParams.screenBrightness = WindowManager.LayoutParams.BRIGHTNESS_OVERRIDE_FULL
                window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            } else {
                layoutParams.screenBrightness = WindowManager.LayoutParams.BRIGHTNESS_OVERRIDE_NONE
                window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            }

            window.attributes = layoutParams
        }
    }
}
