package com.example.opticaltransfer.platform

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Environment
import android.provider.Settings
import androidx.core.content.FileProvider
import java.io.File
import java.io.FileOutputStream

object ApkInstaller {

    /**
     * Saves received byte payload into public Downloads/OpticalTransfer folder
     */
    fun saveToDownloads(context: Context, fileName: String, data: ByteArray): File? {
        return try {
            val downloadsDir = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS)
            val outputFolder = File(downloadsDir, "OpticalTransfer")
            if (!outputFolder.exists()) {
                outputFolder.mkdirs()
            }
            val targetFile = File(outputFolder, fileName)
            FileOutputStream(targetFile).use { fos ->
                fos.write(data)
                fos.flush()
            }
            targetFile
        } catch (e: Exception) {
            e.printStackTrace()
            null
        }
    }

    /**
     * Checks if app has permission to request package installations on Android 8.0+ (API 26+)
     */
    fun canInstallApks(context: Context): Boolean {
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            context.packageManager.canRequestPackageInstalls()
        } else {
            true
        }
    }

    /**
     * Prompt user to grant package install permission via System Settings
     */
    fun requestInstallPermissionIntent(context: Context): Intent? {
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O && !canInstallApks(context)) {
            Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES).apply {
                data = Uri.parse("package:${context.packageName}")
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            }
        } else {
            null
        }
    }

    /**
     * Launches native Android package installer intent for an APK file
     */
    fun installApk(context: Context, apkFile: File): Boolean {
        if (!apkFile.exists()) return false

        return try {
            val authority = "${context.packageName}.fileprovider"
            val contentUri: Uri = FileProvider.getUriForFile(context, authority, apkFile)

            val installIntent = Intent(Intent.ACTION_VIEW).apply {
                setDataAndType(contentUri, "application/vnd.android.package-archive")
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            }
            context.startActivity(installIntent)
            true
        } catch (e: Exception) {
            e.printStackTrace()
            false
        }
    }

    /**
     * Shares a received file via standard Android system Share sheet
     */
    fun shareFile(context: Context, file: File) {
        if (!file.exists()) return
        try {
            val authority = "${context.packageName}.fileprovider"
            val uri: Uri = FileProvider.getUriForFile(context, authority, file)

            val shareIntent = Intent(Intent.ACTION_SEND).apply {
                type = "*/*"
                putExtra(Intent.EXTRA_STREAM, uri)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            }
            context.startActivity(Intent.createChooser(shareIntent, "Share file via...").apply {
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            })
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }
}
