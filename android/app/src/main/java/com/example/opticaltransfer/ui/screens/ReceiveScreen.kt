package com.example.opticaltransfer.ui.screens

import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.*
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import com.example.opticaltransfer.platform.ApkInstaller
import com.example.opticaltransfer.platform.CameraOpticsController
import com.example.opticaltransfer.receiver.ReceiverController
import com.example.opticaltransfer.ui.theme.*
import com.google.zxing.*
import com.google.zxing.common.HybridBinarizer
import java.util.concurrent.Executors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReceiveScreen(
    onBackClicked: () -> Unit
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current

    val receiverController = remember { ReceiverController() }
    val receiverState by receiverController.state.collectAsState()

    val opticsController = remember { CameraOpticsController(context) }
    var isTorchOn by remember { mutableStateOf(false) }

    var hasCameraPermission by remember {
        mutableStateOf(
            ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED
        )
    }

    val permissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission()
    ) { granted ->
        hasCameraPermission = granted
    }

    LaunchedEffect(Unit) {
        if (!hasCameraPermission) {
            permissionLauncher.launch(Manifest.permission.CAMERA)
        }
        receiverController.startScanning()
    }

    DisposableEffect(Unit) {
        onDispose {
            receiverController.stopScanning()
            opticsController.toggleTorch(false)
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "Receive Optical Stream",
                        color = TextPrimary,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBackClicked) {
                        Icon(
                            imageVector = Icons.Default.ArrowBack,
                            contentDescription = "Back",
                            tint = TextPrimary
                        )
                    }
                },
                actions = {
                    // Flashlight / Torch toggle button
                    IconButton(
                        onClick = {
                            isTorchOn = !isTorchOn
                            opticsController.toggleTorch(isTorchOn)
                        }
                    ) {
                        Icon(
                            imageVector = if (isTorchOn) Icons.Default.FlashOn else Icons.Default.FlashOff,
                            contentDescription = "Torch",
                            tint = if (isTorchOn) CyanAccent else TextSecondary
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = SlateSurface
                )
            )
        },
        containerColor = DeepSlateBackground
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            if (hasCameraPermission) {
                // Live Camera Preview Feed
                CameraXPreviewView(
                    context = context,
                    lifecycleOwner = lifecycleOwner,
                    onQrScanned = { qrText ->
                        receiverController.processScannedQrText(context, qrText)
                    }
                )

                // Central Alignment HUD Target Frame Overlay
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Box(
                        modifier = Modifier
                            .size(280.dp)
                            .border(2.dp, CyanAccent, RoundedCornerShape(20.dp))
                    )
                }

                // Bottom Real-time HUD Progress Meter Overlay
                Column(
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .fillMaxWidth()
                        .padding(16.dp)
                ) {
                    HudOverlayCard(
                        totalBlocks = receiverState.totalBlocks,
                        solvedBlocks = receiverState.solvedBlocks,
                        progressPercent = receiverState.progressPercent,
                        goodputKbps = receiverState.goodputKbps,
                        dropsReceived = receiverState.dropsReceived,
                        fileName = receiverState.fileName
                    )
                }
            } else {
                // Permission Denied View
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(32.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    Icon(
                        imageVector = Icons.Default.CameraAlt,
                        contentDescription = "Camera Required",
                        tint = RedError,
                        modifier = Modifier.size(64.dp)
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        text = "Camera Permission Required",
                        color = TextPrimary,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = "The app needs camera access to scan fountain-coded light streams.",
                        color = TextSecondary,
                        fontSize = 14.sp,
                        textAlign = TextAlign.Center
                    )
                    Spacer(modifier = Modifier.height(24.dp))
                    Button(
                        onClick = { permissionLauncher.launch(Manifest.permission.CAMERA) },
                        colors = ButtonDefaults.buttonColors(containerColor = CyanAccent)
                    ) {
                        Text("Grant Camera Permission", color = DeepSlateBackground, fontWeight = FontWeight.Bold)
                    }
                }
            }

            // Completion Dialog / Sheet
            if (receiverState.isCompleted && receiverState.savedFile != null) {
                CompletionSuccessDialog(
                    fileName = receiverState.fileName,
                    savedFile = receiverState.savedFile!!,
                    isApk = receiverState.isApk,
                    onDismiss = onBackClicked
                )
            }
        }
    }
}

@Composable
fun HudOverlayCard(
    totalBlocks: Int,
    solvedBlocks: Int,
    progressPercent: Float,
    goodputKbps: Float,
    dropsReceived: Int,
    fileName: String
) {
    Surface(
        shape = RoundedCornerShape(16.dp),
        color = SlateSurface.copy(alpha = 0.92f),
        border = androidx.compose.foundation.BorderStroke(1.dp, SlateBorder),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    text = if (fileName.isNotBlank()) fileName else "Waiting for optical stream...",
                    color = TextPrimary,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = String.format("%.1f KB/s", goodputKbps),
                    color = TealAccent,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Bold
                )
            }

            // Progress Bar
            LinearProgressIndicator(
                progress = (progressPercent / 100f).coerceIn(0f, 1f),
                modifier = Modifier
                    .fillMaxWidth()
                    .height(8.dp)
                    .clip(CircleShape),
                color = CyanAccent,
                trackColor = SlateSurfaceLight
            )

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    text = "Solved: $solvedBlocks / $totalBlocks (${String.format("%.1f%%", progressPercent)})",
                    color = TextSecondary,
                    fontSize = 12.sp
                )
                Text(
                    text = "Drops Recv: $dropsReceived",
                    color = TextSecondary,
                    fontSize = 12.sp
                )
            }
        }
    }
}

@Composable
fun CompletionSuccessDialog(
    fileName: String,
    savedFile: java.io.File,
    isApk: Boolean,
    onDismiss: () -> Unit
) {
    val context = LocalContext.current

    AlertDialog(
        onDismissRequest = onDismiss,
        containerColor = SlateSurface,
        title = {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = Icons.Default.CheckCircle,
                    contentDescription = "Success",
                    tint = GreenSuccess,
                    modifier = Modifier.size(28.dp)
                )
                Spacer(modifier = Modifier.width(10.dp))
                Text("Transfer Completed!", color = TextPrimary, fontSize = 18.sp, fontWeight = FontWeight.Bold)
            }
        },
        text = {
            Column {
                Text(
                    text = "File successfully reconstructed & SHA-256 verified:",
                    color = TextSecondary,
                    fontSize = 13.sp
                )
                Spacer(modifier = Modifier.height(6.dp))
                Text(
                    text = fileName,
                    color = CyanAccent,
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Bold
                )
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = "Saved to: ${savedFile.absolutePath}",
                    color = TextSecondary,
                    fontSize = 11.sp
                )
            }
        },
        confirmButton = {
            Column(
                modifier = Modifier.fillMaxWidth(),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                if (isApk) {
                    // Direct APK Package Installer Action Button
                    Button(
                        onClick = {
                            if (!ApkInstaller.canInstallApks(context)) {
                                val intent = ApkInstaller.requestInstallPermissionIntent(context)
                                intent?.let { context.startActivity(it) }
                            } else {
                                ApkInstaller.installApk(context, savedFile)
                            }
                        },
                        modifier = Modifier.fillMaxWidth(),
                        colors = ButtonDefaults.buttonColors(containerColor = CyanAccent)
                    ) {
                        Icon(Icons.Default.Android, contentDescription = null, tint = DeepSlateBackground)
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Direct Install APK", color = DeepSlateBackground, fontWeight = FontWeight.Bold)
                    }
                }

                // Share / Open File Button
                OutlinedButton(
                    onClick = {
                        ApkInstaller.shareFile(context, savedFile)
                    },
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.outlinedButtonColors(contentColor = TextPrimary)
                ) {
                    Icon(Icons.Default.Share, contentDescription = null, tint = TextPrimary)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Share / Open File")
                }
            }
        }
    )
}

@Composable
fun CameraXPreviewView(
    context: Context,
    lifecycleOwner: androidx.lifecycle.LifecycleOwner,
    onQrScanned: (String) -> Unit
) {
    val cameraExecutor = remember { Executors.newSingleThreadExecutor() }
    val reader = remember { MultiFormatReader() }

    AndroidView(
        factory = { ctx ->
            val previewView = PreviewView(ctx)
            val cameraProviderFuture = ProcessCameraProvider.getInstance(ctx)

            cameraProviderFuture.addListener({
                val cameraProvider = cameraProviderFuture.get()

                val preview = Preview.Builder().build().also {
                    it.setSurfaceProvider(previewView.surfaceProvider)
                }

                val imageAnalysis = ImageAnalysis.Builder()
                    .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                    .build()

                imageAnalysis.setAnalyzer(cameraExecutor) { imageProxy ->
                    val buffer = imageProxy.planes[0].buffer
                    val data = ByteArray(buffer.remaining())
                    buffer.get(data)

                    val width = imageProxy.width
                    val height = imageProxy.height

                    val source = PlanarYUVLuminanceSource(
                        data, width, height, 0, 0, width, height, false
                    )
                    val bitmap = BinaryBitmap(HybridBinarizer(source))

                    try {
                        val result = reader.decodeWithState(bitmap)
                        if (result != null && result.text.isNotBlank()) {
                            onQrScanned(result.text)
                        }
                    } catch (_: Exception) {
                    } finally {
                        reader.reset()
                        imageProxy.close()
                    }
                }

                try {
                    cameraProvider.unbindAll()
                    cameraProvider.bindToLifecycle(
                        lifecycleOwner,
                        CameraSelector.DEFAULT_BACK_CAMERA,
                        preview,
                        imageAnalysis
                    )
                } catch (e: Exception) {
                    e.printStackTrace()
                }
            }, ContextCompat.getMainExecutor(ctx))

            previewView
        },
        modifier = Modifier.fillMaxSize()
    )
}
