import 'package:client/screens/home_screen.dart';
import 'package:client/screens/login_screen.dart';
import 'package:client/services/api_client.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/secure_storage_service.dart';
import 'package:client/services/user_service.dart';
import 'package:client/services/video_service.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';
import 'package:google_fonts/google_fonts.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final storage = SecureStorageService();
  final client = ApiClient(storage);
  final authService = AuthService(client, storage);
  final userService = UserService(client);

  await authService.restoreSession();

  bool isLoggedIn = false;
  if (client.hasSession) {
    try {
      await userService.getCurrentUser();
      isLoggedIn = true;
    } catch (_) {
      // Token invalid or expired, user must log in
    }
  }

  runApp(MainApp(authService: authService, userService: userService, isLoggedIn: isLoggedIn));
}

class MainApp extends StatelessWidget {
  final AuthService authService;
  final UserService userService;
  final bool isLoggedIn;
  const MainApp({super.key, required this.authService, required this.userService, required this.isLoggedIn});

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
          child: isLoggedIn
              ? HomeScreen(
                  authService: authService,
                  userService: userService,
                  videoService: VideoService(authService.client),
                )
              : LoginScreen(authService: authService),
        ),
      ),
    );
  }
}
