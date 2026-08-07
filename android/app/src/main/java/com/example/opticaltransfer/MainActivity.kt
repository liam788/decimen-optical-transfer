package com.example.opticaltransfer

import android.net.Uri
import android.os.Bundle
import android.provider.OpenableColumns
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import com.example.opticaltransfer.platform.ApkExtractor
import com.example.opticaltransfer.platform.InstalledAppItem
import com.example.opticaltransfer.ui.components.ApkPickerBottomSheet
import com.example.opticaltransfer.ui.screens.HomeScreen
import com.example.opticaltransfer.ui.screens.ReceiveScreen
import com.example.opticaltransfer.ui.screens.SendScreen
import com.example.opticaltransfer.ui.theme.DeepSlateBackground
import com.example.opticaltransfer.ui.theme.OpticalTransferTheme

enum class ScreenState {
    HOME,
    SEND,
    RECEIVE
}

class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContent {
            OpticalTransferTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = DeepSlateBackground
                ) {
                    var currentScreen by remember { mutableStateOf(ScreenState.HOME) }
                    var showApkPickerSheet by remember { mutableStateOf(false) }

                    var selectedFileName by remember { mutableStateOf("") }
                    var selectedFileBytes by remember { mutableStateOf<ByteArray?>(null) }

                    // Storage File Picker Launcher
                    val filePickerLauncher = rememberLauncherForActivityResult(
                        contract = ActivityResultContracts.GetContent()
                    ) { uri: Uri? ->
                        if (uri != null) {
                            val bytes = contentResolver.openInputStream(uri)?.use { it.readBytes() }
                            val name = getFileNameFromUri(uri) ?: "selected_file.bin"
                            if (bytes != null && bytes.isNotEmpty()) {
                                selectedFileName = name
                                selectedFileBytes = bytes
                                currentScreen = ScreenState.SEND
                            }
                        }
                    }

                    when (currentScreen) {
                        ScreenState.HOME -> {
                            HomeScreen(
                                onSendFileClicked = {
                                    filePickerLauncher.launch("*/*")
                                },
                                onSendApkClicked = {
                                    showApkPickerSheet = true
                                },
                                onReceiveClicked = {
                                    currentScreen = ScreenState.RECEIVE
                                }
                            )

                            if (showApkPickerSheet) {
                                ApkPickerBottomSheet(
                                    onDismiss = { showApkPickerSheet = false },
                                    onAppSelected = { app: InstalledAppItem ->
                                        showApkPickerSheet = false
                                        val apkBytes = ApkExtractor.readApkBytes(app.apkPath)
                                        if (apkBytes != null) {
                                            selectedFileName = "${app.name.replace(" ", "_")}_v${app.versionName}.apk"
                                            selectedFileBytes = apkBytes
                                            currentScreen = ScreenState.SEND
                                        }
                                    }
                                )
                            }
                        }

                        ScreenState.SEND -> {
                            if (selectedFileBytes != null && selectedFileName.isNotBlank()) {
                                SendScreen(
                                    fileName = selectedFileName,
                                    fileBytes = selectedFileBytes!!,
                                    onBackClicked = {
                                        currentScreen = ScreenState.HOME
                                    }
                                )
                            } else {
                                currentScreen = ScreenState.HOME
                            }
                        }

                        ScreenState.RECEIVE -> {
                            ReceiveScreen(
                                onBackClicked = {
                                    currentScreen = ScreenState.HOME
                                }
                            )
                        }
                    }
                }
            }
        }
    }

    private fun getFileNameFromUri(uri: Uri): String? {
        var result: String? = null
        if (uri.scheme == "content") {
            contentResolver.query(uri, null, null, null, null)?.use { cursor ->
                if (cursor.moveToFirst()) {
                    val index = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME)
                    if (index != -1) {
                        result = cursor.getString(index)
                    }
                }
            }
        }
        if (result == null) {
            result = uri.path
            val cut = result?.lastIndexOf('/')
            if (cut != null && cut != -1) {
                result = result?.substring(cut + 1)
            }
        }
        return result
    }
}
