import 'package:client/screens/feed_screen.dart';
import 'package:client/screens/profile_screen.dart';
import 'package:client/screens/upload_screen.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/video_service.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';

class HomeScreen extends StatefulWidget {
  final AuthService authService;
  final VideoService videoService;

  const HomeScreen({
    super.key,
    required this.authService,
    required this.videoService,
  });

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentTab = 0;
  int _feedKey = 0;
  int _profileKey = 0;

  @override
  Widget build(BuildContext context) {
    return NeumorphicBackground(
      child: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: IndexedStack(
                index: _currentTab,
                children: [
                  FeedScreen(
                    key: ValueKey(_feedKey),
                    videoService: widget.videoService,
                    authService: widget.authService,
                    isVisible: _currentTab == 0,
                    onSwitchTab: (i) => setState(() => _currentTab = i),
                  ),
                  UploadScreen(
                    videoService: widget.videoService,
                    isVisible: _currentTab == 1,
                    onUploadAccepted: () => setState(() {
                      _profileKey++;
                      _currentTab = 2;
                    }),
                  ),
                  ProfileScreen(
                    key: ValueKey(_profileKey),
                    videoService: widget.videoService,
                    authService: widget.authService,
                    onSwitchTab: (i) => setState(() => _currentTab = i),
                  ),
                ],
              ),
            ),
            _buildBottomNav(context),
          ],
        ),
      ),
    );
  }

  Widget _buildBottomNav(BuildContext context) {
    return Neumorphic(
      style: NeumorphicStyle(
        depth: 4,
        boxShape: NeumorphicBoxShape.roundRect(
          const BorderRadius.vertical(top: Radius.circular(16)),
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 32),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          children: [
            _navItem(Icons.home_rounded, 'Home', 0),
            _navItem(Icons.add_circle_outline_rounded, 'Upload', 1),
            _navItem(Icons.person_rounded, 'Profile', 2),
          ],
        ),
      ),
    );
  }

  Widget _navItem(IconData icon, String label, int index) {
    final isActive = _currentTab == index;
    final color = isActive
        ? const Color(0xFFB08968)
        : NeumorphicTheme.defaultTextColor(context).withValues(alpha: 0.4);

    return GestureDetector(
      onTap: () => setState(() => _currentTab = index),
      behavior: HitTestBehavior.opaque,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 28, color: color),
            const SizedBox(height: 4),
            Text(
              label,
              style: TextStyle(
                color: color,
                fontSize: 11,
                fontWeight: isActive ? FontWeight.w600 : FontWeight.w400,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
