package com.example.opticaltransfer.platform

import android.content.Context
import android.hardware.camera2.CameraManager

class CameraOpticsController(private val context: Context) {
    private val cameraManager = context.getSystemService(Context.CAMERA_SERVICE) as CameraManager
    private var cameraId: String? = null
    private var isTorchOn = false

    init {
        try {
            val cameraIds = cameraManager.cameraIdList
            for (id in cameraIds) {
                val characteristics = cameraManager.getCameraCharacteristics(id)
                val facing = characteristics.get(android.hardware.camera2.CameraCharacteristics.LENS_FACING)
                if (facing == android.hardware.camera2.CameraCharacteristics.LENS_FACING_BACK) {
                    cameraId = id
                    break
                }
            }
            if (cameraId == null && cameraIds.isNotEmpty()) {
                cameraId = cameraIds[0]
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    /**
     * Toggle device hardware torch/flashlight state
     */
    fun toggleTorch(enable: Boolean): Boolean {
        val id = cameraId ?: return false
        return try {
            cameraManager.setTorchMode(id, enable)
            isTorchOn = enable
            true
        } catch (e: Exception) {
            e.printStackTrace()
            false
        }
    }

    fun isTorchActive(): Boolean = isTorchOn
}
