import 'package:client/screens/login_screen.dart';
import 'package:client/services/api_client.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/secure_storage_service.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final client = ApiClient();
  final storage = SecureStorageService();
  final authService = AuthService(client, storage);

  await authService.restoreSession();

  runApp(MainApp(authService: authService));
}

class MainApp extends StatelessWidget {
  final AuthService authService;
  const MainApp({super.key, required this.authService});

  @override
  Widget build(BuildContext context) {
    return NeumorphicApp(
      debugShowCheckedModeBanner: false,
      theme: const NeumorphicThemeData(
        baseColor: Color(0xFFE0E5EC),
        depth: 6,
        intensity: 0.7,
      ),
      home: LoginScreen(authService: authService),
    );
  }
}
