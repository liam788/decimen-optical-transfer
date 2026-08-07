package com.example.opticaltransfer.platform

import android.content.Context
import android.content.pm.ApplicationInfo
import android.content.pm.PackageManager
import android.graphics.drawable.Drawable
import java.io.File

data class InstalledAppItem(
    val name: String,
    val packageName: String,
    val apkPath: String,
    val sizeBytes: Long,
    val versionName: String,
    val icon: Drawable?
)

object ApkExtractor {

    /**
     * Query all installed applications (filtering for user apps or all non-system apps by default)
     */
    fun getInstalledApps(context: Context, includeSystemApps: Boolean = false): List<InstalledAppItem> {
        val pm = context.packageManager
        val packages = pm.getInstalledPackages(PackageManager.GET_META_DATA)
        val appList = mutableListOf<InstalledAppItem>()

        for (pkg in packages) {
            val appInfo = pkg.applicationInfo ?: continue

            val isSystemApp = (appInfo.flags and ApplicationInfo.FLAG_SYSTEM) != 0
            if (isSystemApp && !includeSystemApps) continue

            val apkFile = File(appInfo.sourceDir)
            if (!apkFile.exists()) continue

            val appName = pm.getApplicationLabel(appInfo).toString()
            val packageName = pkg.packageName
            val apkPath = appInfo.sourceDir
            val size = apkFile.length()
            val versionName = pkg.versionName ?: "1.0"
            val icon = try {
                pm.getApplicationIcon(appInfo)
            } catch (_: Exception) {
                null
            }

            appList.add(
                InstalledAppItem(
                    name = appName,
                    packageName = packageName,
                    apkPath = apkPath,
                    sizeBytes = size,
                    versionName = versionName,
                    icon = icon
                )
            )
        }

        return appList.sortedBy { it.name.lowercase() }
    }

    /**
     * Reads the complete byte array of an installed APK file
     */
    fun readApkBytes(apkPath: String): ByteArray? {
        val file = File(apkPath)
        return if (file.exists() && file.canRead()) {
            file.readBytes()
        } else {
            null
        }
    }
}
