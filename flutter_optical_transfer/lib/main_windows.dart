import 'package:flutter/material.dart';
import 'ui/screens/home_screen_desktop.dart';

/// Windows desktop entry point — uses desktop-compatible home screen
/// without camera/permission_handler/share_plus dependencies.
void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const OpticalTransferApp());
}

class OpticalTransferApp extends StatelessWidget {
  const OpticalTransferApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Decimen Optical Transfer',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        brightness: Brightness.dark,
        primarySwatch: Colors.cyan,
        scaffoldBackgroundColor: const Color(0xFF0F172A),
        useMaterial3: true,
      ),
      home: const HomeScreenDesktop(),
    );
  }
}
