# Decimen Optical Transfer — Release Binaries

This folder contains pre-built distribution files for **Decimen Optical Transfer**.

---

## 📦 Included Standalone Web Releases

| File | Description | Needs Server? | Offline? |
|---|---|---|---|
| **`decimen-sender.html`** (57 KB) | Single-file optical sender. Generates screen stream of fountain QR frames. | No (runs from `file://` or any browser) | Always |
| **`decimen-receiver.html`** (1.3 MB) | Single-file optical receiver. Carries inlined ZXing WASM engine & web worker. | Serve over HTTP/HTTPS or local host | Always |

---

## 📱 Cross-Platform Native Apps (Android APK, Windows EXE, macOS APP)

The repository includes a complete Flutter cross-platform application in [`flutter_optical_transfer/`](../flutter_optical_transfer/).

### Building Native Binaries via GitHub Actions (Automated)
Whenever a version tag (e.g. `v1.0.0`) is pushed to GitHub:
```bash
git tag v1.0.0
git push origin v1.0.0
```
The automated GitHub Actions workflow (`.github/workflows/release-flutter.yml`) will build:
1. **`app-release.apk`** — Android APK (with native installed app extractor)
2. **`flutter_optical_transfer_windows.zip`** — Windows PC Desktop Executable
3. **`flutter_optical_transfer_macos.zip`** — macOS Desktop Bundle

All compiled binaries are automatically attached to the GitHub Releases page!

---

## 🛠️ Building Native Binaries Locally

### Build Android APK:
```bash
cd flutter_optical_transfer
flutter build apk --release
```

### Build Windows EXE:
```bash
cd flutter_optical_transfer
flutter build windows --release
```

### Build macOS App:
```bash
cd flutter_optical_transfer
flutter build macos --release
```
