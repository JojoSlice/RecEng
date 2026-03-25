import 'package:client/screens/feed_screen.dart';
import 'package:client/screens/profile_screen.dart';
import 'package:client/screens/upload_screen.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/video_service.dart';
import 'package:client/widgets/app_bottom_nav.dart';
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
            AppBottomNav(
              activeIndex: _currentTab,
              onTap: (i) => setState(() => _currentTab = i),
            ),
          ],
        ),
      ),
    );
  }

}
