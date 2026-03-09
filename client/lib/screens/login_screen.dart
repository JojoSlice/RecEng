import 'package:client/screens/register_screen.dart';
import 'package:client/services/auth_service.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';
import 'package:client/screens/feed_screen.dart';
import 'package:client/services/video_service.dart';

class LoginScreen extends StatefulWidget {
  final AuthService authService;
  const LoginScreen({super.key, required this.authService});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  String? _error;
  bool _isLoading = false;
  bool _isLoginHovered = false;
  bool _isRegisterHovered = false;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      await widget.authService.login(
        _usernameController.text.trim(),
        _passwordController.text,
      );
      if (mounted) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => FeedScreen(
              videoService: VideoService(widget.authService.client),
              authService: widget.authService,
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return NeumorphicBackground(
      child: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  NeumorphicText(
                    'RecEng',
                    style: const NeumorphicStyle(depth: 6, intensity: 0.9),
                    textStyle: NeumorphicTextStyle(
                      fontSize: 52,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 8,
                    ),
                  ),
                  const SizedBox(height: 48),
                  Neumorphic(
                    style: NeumorphicStyle(
                      depth: -4,
                      boxShape: NeumorphicBoxShape.roundRect(
                        BorderRadius.circular(12),
                      ),
                    ),
                    child: Material(
                      color: Colors.transparent,
                      child: TextField(
                        controller: _usernameController,
                        decoration: const InputDecoration(
                          hintText: 'Username',
                          hintStyle: TextStyle(
                            color: Color(0xFFB08968),
                            fontWeight: FontWeight.w300,
                            letterSpacing: 1,
                          ),
                          border: InputBorder.none,
                          filled: true,
                          fillColor: Colors.transparent,
                          contentPadding: EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 14,
                          ),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Neumorphic(
                    style: NeumorphicStyle(
                      depth: -4,
                      boxShape: NeumorphicBoxShape.roundRect(
                        BorderRadius.circular(12),
                      ),
                    ),
                    child: Material(
                      color: Colors.transparent,
                      child: TextField(
                        controller: _passwordController,
                        obscureText: true,
                        decoration: const InputDecoration(
                          hintText: 'Password',
                          hintStyle: TextStyle(
                            color: Color(0xFFB08968),
                            fontWeight: FontWeight.w300,
                            letterSpacing: 1,
                          ),
                          border: InputBorder.none,
                          filled: true,
                          fillColor: Colors.transparent,
                          contentPadding: EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 14,
                          ),
                        ),
                        onSubmitted: (_) => _login(),
                      ),
                    ),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Text(_error!, style: const TextStyle(color: Colors.red)),
                  ],
                  const SizedBox(height: 24),
                  MouseRegion(
                    onEnter: (_) => setState(() => _isLoginHovered = true),
                    onExit: (_) => setState(() => _isLoginHovered = false),
                    child: NeumorphicButton(
                      onPressed: _isLoading ? null : _login,
                      style: NeumorphicStyle(
                        depth: _isLoginHovered ? 2 : 4,
                        boxShape: NeumorphicBoxShape.roundRect(
                          BorderRadius.circular(12),
                        ),
                      ),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      child: SizedBox(
                        width: double.infinity,
                        child: Center(
                          child: _isLoading
                              ? const SizedBox(
                                  height: 20,
                                  width: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Text(
                                  'LOGIN',
                                  style: TextStyle(
                                    color: Color(0xFFB08968),
                                    fontWeight: FontWeight.w500,
                                    fontSize: 16,
                                    letterSpacing: 4,
                                  ),
                                ),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  MouseRegion(
                    onEnter: (_) => setState(() => _isRegisterHovered = true),
                    onExit: (_) => setState(() => _isRegisterHovered = false),
                    child: NeumorphicButton(
                      onPressed: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) =>
                              RegisterScreen(authService: widget.authService),
                        ),
                      ),
                      style: NeumorphicStyle(
                        depth: _isRegisterHovered ? 1 : 2,
                        boxShape: NeumorphicBoxShape.roundRect(
                          BorderRadius.circular(12),
                        ),
                      ),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      child: const SizedBox(
                        width: double.infinity,
                        child: Center(
                          child: Text(
                            'REGISTER',
                            style: TextStyle(
                              color: Color(0xFFB08968),
                              fontWeight: FontWeight.w400,
                              fontSize: 16,
                              letterSpacing: 4,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
