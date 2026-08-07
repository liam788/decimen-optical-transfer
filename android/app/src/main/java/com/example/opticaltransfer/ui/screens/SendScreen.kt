package com.example.opticaltransfer.ui.screens

import android.app.Activity
import android.graphics.Bitmap
import android.graphics.Color
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.FlashOn
import androidx.compose.material.icons.filled.Speed
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.opticaltransfer.core.codec.MatrixGridMode
import com.example.opticaltransfer.core.codec.QrFrameConfig
import com.example.opticaltransfer.core.codec.QrMatrixEncoder
import com.example.opticaltransfer.platform.ScreenBrightnessHelper
import com.example.opticaltransfer.sender.SenderController
import com.google.zxing.BarcodeFormat
import com.google.zxing.qrcode.QRCodeWriter
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SendScreen(
    fileName: String,
    fileBytes: ByteArray,
    onBackClicked: () -> Unit
) {
    val context = LocalContext.current
    val activity = context as? Activity
    val coroutineScope = rememberCoroutineScope()

    val senderController = remember { SenderController() }
    val senderState by senderController.state.collectAsState()

    var gridMode by remember { mutableStateOf(MatrixGridMode.SINGLE_1X1) }
    var targetFps by remember { mutableStateOf(30) }
    var isMaxBrightness by remember { mutableStateOf(true) }

    // Initialize sender controller with file payload
    LaunchedEffect(fileName, fileBytes) {
        senderController.prepareFile(fileName, fileBytes)
        senderController.startStreaming(coroutineScope)
    }

    // Toggle max display brightness
    LaunchedEffect(isMaxBrightness) {
        activity?.let {
            ScreenBrightnessHelper.setMaxBrightnessAndKeepAwake(it, isMaxBrightness)
        }
    }

    DisposableEffect(Unit) {
        onDispose {
            senderController.stopStreaming()
            activity?.let {
                ScreenBrightnessHelper.setMaxBrightnessAndKeepAwake(it, false)
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "Streaming: $fileName",
                        color = com.example.opticaltransfer.ui.theme.TextPrimary,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBackClicked) {
                        Icon(
                            imageVector = Icons.Default.ArrowBack,
                            contentDescription = "Back",
                            tint = com.example.opticaltransfer.ui.theme.TextPrimary
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = com.example.opticaltransfer.ui.theme.SlateSurface
                )
            )
        },
        containerColor = com.example.opticaltransfer.ui.theme.DeepSlateBackground
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Stats Header Card
            Surface(
                shape = RoundedCornerShape(12.dp),
                color = com.example.opticaltransfer.ui.theme.SlateSurface,
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(14.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column {
                        Text(
                            text = "Size: ${String.format("%.1f KB", senderState.fileSize / 1024f)}",
                            color = com.example.opticaltransfer.ui.theme.TextSecondary,
                            fontSize = 13.sp
                        )
                        Text(
                            text = "Total Blocks: ${senderState.totalBlocks}",
                            color = com.example.opticaltransfer.ui.theme.TextSecondary,
                            fontSize = 13.sp
                        )
                    }

                    Column(horizontalAlignment = Alignment.End) {
                        Text(
                            text = "Drops Sent: ${senderState.dropsSent}",
                            color = com.example.opticaltransfer.ui.theme.CyanAccent,
                            fontSize = 15.sp,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            text = "Rate: $targetFps FPS",
                            color = com.example.opticaltransfer.ui.theme.TealAccent,
                            fontSize = 13.sp
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Main Visual QR Matrix Stream Box
            Box(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(16.dp))
                    .background(com.example.opticaltransfer.ui.theme.SlateSurface)
                    .padding(16.dp),
                contentAlignment = Alignment.Center
            ) {
                if (senderState.currentGridDropTexts.isNotEmpty()) {
                    QrMatrixBitmapViewer(
                        dropTexts = senderState.currentGridDropTexts,
                        gridMode = gridMode
                    )
                } else {
                    CircularProgressIndicator(color = com.example.opticaltransfer.ui.theme.CyanAccent)
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Control Tuning Panel
            Surface(
                shape = RoundedCornerShape(14.dp),
                color = com.example.opticaltransfer.ui.theme.SlateSurface,
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(
                    modifier = Modifier.padding(14.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        // Grid Mode Selector
                        Text(
                            text = "Spatial Grid:",
                            color = com.example.opticaltransfer.ui.theme.TextPrimary,
                            fontSize = 14.sp
                        )
                        Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                            FilterChip(
                                selected = gridMode == MatrixGridMode.SINGLE_1X1,
                                onClick = {
                                    gridMode = MatrixGridMode.SINGLE_1X1
                                    senderController.updateConfig(QrFrameConfig(gridMode, targetFps = targetFps))
                                },
                                label = { Text("1x1") }
                            )
                            FilterChip(
                                selected = gridMode == MatrixGridMode.GRID_2X2,
                                onClick = {
                                    gridMode = MatrixGridMode.GRID_2X2
                                    senderController.updateConfig(QrFrameConfig(gridMode, targetFps = targetFps))
                                },
                                label = { Text("2x2 (4 QR)") }
                            )
                            FilterChip(
                                selected = gridMode == MatrixGridMode.GRID_3X3,
                                onClick = {
                                    gridMode = MatrixGridMode.GRID_3X3
                                    senderController.updateConfig(QrFrameConfig(gridMode, targetFps = targetFps))
                                },
                                label = { Text("3x3 (9 QR)") }
                            )
                        }
                    }

                    Divider(color = com.example.opticaltransfer.ui.theme.SlateBorder)

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        // FPS Rate Selector
                        Text(
                            text = "Target Speed:",
                            color = com.example.opticaltransfer.ui.theme.TextPrimary,
                            fontSize = 14.sp
                        )
                        Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                            listOf(15, 30, 60).forEach { fps ->
                                FilterChip(
                                    selected = targetFps == fps,
                                    onClick = {
                                        targetFps = fps
                                        senderController.updateConfig(QrFrameConfig(gridMode, targetFps = fps))
                                    },
                                    label = { Text("$fps FPS") }
                                )
                            }
                        }
                    }

                    Divider(color = com.example.opticaltransfer.ui.theme.SlateBorder)

                    // Max Brightness Toggle
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Icon(
                                imageVector = Icons.Default.FlashOn,
                                contentDescription = "Brightness",
                                tint = com.example.opticaltransfer.ui.theme.CyanAccent,
                                modifier = Modifier.size(20.dp)
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = "Force Max Screen Brightness",
                                color = com.example.opticaltransfer.ui.theme.TextPrimary,
                                fontSize = 14.sp
                            )
                        }
                        Switch(
                            checked = isMaxBrightness,
                            onCheckedChange = { isMaxBrightness = it },
                            colors = SwitchDefaults.colors(
                                checkedThumbColor = com.example.opticaltransfer.ui.theme.CyanAccent,
                                checkedTrackColor = com.example.opticaltransfer.ui.theme.CyanAccent.copy(alpha = 0.3f)
                            )
                        )
                    }
                }
            }
        }
    }
}

@Composable
fun QrMatrixBitmapViewer(
    dropTexts: List<String>,
    gridMode: MatrixGridMode
) {
    var generatedBitmap by remember { mutableStateOf<Bitmap?>(null) }

    LaunchedEffect(dropTexts, gridMode) {
        withContext(Dispatchers.Default) {
            val writer = QRCodeWriter()
            val qrBitmaps = mutableListOf<Bitmap>()

            for (text in dropTexts) {
                try {
                    val bitMatrix = writer.encode(text, BarcodeFormat.QR_CODE, 260, 260)
                    val width = bitMatrix.width
                    val height = bitMatrix.height
                    val bmp = Bitmap.createBitmap(width, height, Bitmap.Config.RGB_565)
                    for (x in 0 until width) {
                        for (y in 0 until height) {
                            bmp.setPixel(x, y, if (bitMatrix[x, y]) Color.BLACK else Color.WHITE)
                        }
                    }
                    qrBitmaps.add(bmp)
                } catch (_: Exception) {}
            }

            if (qrBitmaps.isNotEmpty()) {
                val combined = QrMatrixEncoder.combineGridBitmaps(qrBitmaps, gridMode, 512)
                generatedBitmap = combined
            }
        }
    }

    generatedBitmap?.let { bmp ->
        Image(
            bitmap = bmp.asImageBitmap(),
            contentDescription = "QR Stream Matrix",
            modifier = Modifier.fillMaxSize()
        )
    }
}
