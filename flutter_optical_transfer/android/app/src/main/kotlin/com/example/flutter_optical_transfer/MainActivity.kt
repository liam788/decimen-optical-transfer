package com.example.flutter_optical_transfer

import android.content.pm.ApplicationInfo
import android.content.pm.PackageManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import java.io.File

class MainActivity: FlutterActivity() {
    private val CHANNEL = "optical_transfer/apk"

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, CHANNEL).setMethodCallHandler { call, result ->
            if (call.method == "getInstalledApps") {
                val pm = packageManager
                val apps = pm.getInstalledApplications(PackageManager.GET_META_DATA)
                val appList = mutableListOf<Map<String, Any>>()

                for (appInfo in apps) {
                    // Filter out system apps unless updated
                    if ((appInfo.flags and ApplicationInfo.FLAG_SYSTEM) == 0 || (appInfo.flags and ApplicationInfo.FLAG_UPDATED_SYSTEM_APP) != 0) {
                        val name = pm.getApplicationLabel(appInfo).toString()
                        val packageName = appInfo.packageName
                        val apkPath = appInfo.sourceDir
                        val size = File(apkPath).length()

                        val appMap = mapOf(
                            "name" to name,
                            "packageName" to packageName,
                            "apkPath" to apkPath,
                            "size" to size
                        )
                        appList.add(appMap)
                    }
                }
                result.success(appList)
            } else {
                result.notImplemented()
            }
        }
    }
}
