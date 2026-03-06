import 'package:client/screens/login_screen.dart';
import 'package:client/services/api_client.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/secure_storage_service.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';
import 'package:google_fonts/google_fonts.dart';

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
      themeMode: ThemeMode.system,
      theme: const NeumorphicThemeData(
        baseColor: Color(0xFFE7E2DA),
        accentColor: Color(0xFFB08968),
        depth: 6,
        intensity: 0.58,
        lightSource: LightSource.topLeft,
        shadowLightColor: Color(0xFFFFFFFF),
        shadowDarkColor: Color(0xFFD2CCC2),
      ),
      darkTheme: const NeumorphicThemeData(
        baseColor: Color(0xFF3D3830),
        accentColor: Color(0xFFB08968),
        depth: 6,
        intensity: 0.58,
        lightSource: LightSource.topLeft,
        shadowLightColor: Color(0xFF4A4540),
        shadowDarkColor: Color(0xFF252018),
      ),
      home: Builder(
        builder: (context) => Theme(
          data: Theme.of(context).copyWith(
            textTheme: GoogleFonts.nunitoTextTheme(Theme.of(context).textTheme),
          ),
          child: LoginScreen(authService: authService),
        ),
      ),
    );
  }
}
