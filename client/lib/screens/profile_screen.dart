import 'dart:async';

import 'package:client/models/user.dart';
import 'package:client/models/video.dart';
import 'package:client/screens/feed_screen.dart';
import 'package:client/screens/login_screen.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/user_service.dart';
import 'package:client/services/video_service.dart';
import 'package:client/utils/errors.dart';
import 'package:client/widgets/app_bottom_nav.dart';
import 'package:client/widgets/hover_neumorphic_button.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';
import 'package:image_picker/image_picker.dart';

class ProfileScreen extends StatefulWidget {
  final VideoService videoService;
  final AuthService authService;
  final UserService userService;
  // If set, shows another user's profile (read-only). If null, shows own profile.
  final String? userId;
  final void Function(int)? onSwitchTab;

  const ProfileScreen({
    super.key,
    required this.videoService,
    required this.authService,
    required this.userService,
    this.userId,
    this.onSwitchTab,
  });

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  User? _user;
  List<Video> _videos = [];
  bool _isLoading = true;
  String? _error;
  bool _isUploadingPicture = false;
  int _pictureCacheKey = DateTime.now().millisecondsSinceEpoch;
  bool _isFollowing = false;
  bool _isFollowLoading = false;
  final _picker = ImagePicker();
  Timer? _pollTimer;

  bool get _isOwnProfile => widget.userId == null;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    super.dispose();
  }

  Future<void> _loadProfile() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      User user;
      List<Video> videos;
      bool isFollowing = false;

      if (_isOwnProfile) {
        user = await widget.userService.getCurrentUser();
        videos = await widget.videoService.getUserVideos(user.id);
      } else {
        // Fetch profile user, current user, and profile videos in parallel.
        final results = await Future.wait([
          widget.userService.getUser(widget.userId!),
          widget.userService.getCurrentUser(),
          widget.videoService.getUserVideos(widget.userId!),
        ]);
        user = results[0] as User;
        final currentUser = results[1] as User;
        videos = results[2] as List<Video>;
        final following = await widget.userService.getFollowing(currentUser.id);
        isFollowing = following.any((u) => u.id == widget.userId);
      }

      if (mounted) {
        setState(() {
          _user = user;
          _videos = videos.reversed.toList();
          _isFollowing = isFollowing;
          _isLoading = false;
        });
        _updatePolling();
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _pollVideos() async {
    if (_user == null) return;
    try {
      final videos = await widget.videoService.getUserVideos(_user!.id);
      if (mounted) {
        setState(() => _videos = videos.reversed.toList());
        _updatePolling();
      }
    } catch (_) {}
  }

  void _updatePolling() {
    final hasProcessing = _videos.any((v) => v.status == VideoStatus.processing);
    if (hasProcessing && _pollTimer == null) {
      _pollTimer = Timer.periodic(const Duration(seconds: 3), (_) => _pollVideos());
    } else if (!hasProcessing && _pollTimer != null) {
      _pollTimer!.cancel();
      _pollTimer = null;
    }
  }

  Future<void> _changeProfilePicture() async {
    final image = await _picker.pickImage(source: ImageSource.gallery);
    if (image == null) return;

    setState(() {
      _isUploadingPicture = true;
      _error = null;
    });

    try {
      final bytes = await image.readAsBytes();
      await widget.videoService.uploadProfilePicture(
        fileBytes: bytes,
        fileName: image.name,
      );
      if (mounted) {
        setState(() => _pictureCacheKey = DateTime.now().millisecondsSinceEpoch);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } finally {
      if (mounted) setState(() => _isUploadingPicture = false);
    }
  }

  Future<void> _toggleFollow() async {
    setState(() => _isFollowLoading = true);
    try {
      if (_isFollowing) {
        await widget.userService.unfollowUser(widget.userId!);
      } else {
        await widget.userService.followUser(widget.userId!);
      }
      if (mounted) setState(() => _isFollowing = !_isFollowing);
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } finally {
      if (mounted) setState(() => _isFollowLoading = false);
    }
  }

  Future<void> _logout() async {
    try {
      await widget.authService.logout();
      if (mounted) {
        Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(
            builder: (_) => LoginScreen(authService: widget.authService),
          ),
          (_) => false,
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null && _user == null) {
      return Center(
        child: Text(_error!, style: const TextStyle(color: Colors.red)),
      );
    }

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 400),
          child: Column(
            children: [
              if (!_isOwnProfile) _buildBackButton(context),
              _buildProfileHeader(),
              const SizedBox(height: 32),
              _buildVideosGrid(),
              if (_error != null) ...[
                const SizedBox(height: 12),
                Text(_error!, style: const TextStyle(color: Colors.red)),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBackButton(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: NeumorphicButton(
        onPressed: () => Navigator.of(context).pop(),
        style: const NeumorphicStyle(
          depth: 2,
          boxShape: NeumorphicBoxShape.circle(),
        ),
        padding: const EdgeInsets.all(10),
        child: const Icon(
          Icons.arrow_back_rounded,
          color: Color(0xFFB08968),
          size: 20,
        ),
      ),
    );
  }

  Widget _buildProfileHeader() {
    final baseUrl = widget.videoService.client.baseUrl;
    final userId = _user!.id;
    final profilePicUrl =
        '$baseUrl/api/users/$userId/profile-picture?v=$_pictureCacheKey';

    return Column(
      children: [
        NeumorphicText(
          'RecEng',
          style: const NeumorphicStyle(depth: 6, intensity: 0.9),
          textStyle: NeumorphicTextStyle(
            fontSize: 36,
            fontWeight: FontWeight.w900,
            letterSpacing: 4,
          ),
        ),
        const SizedBox(height: 32),
        Neumorphic(
          style: const NeumorphicStyle(
            depth: 4,
            boxShape: NeumorphicBoxShape.circle(),
          ),
          child: _isUploadingPicture
              ? const SizedBox(
                  width: 120,
                  height: 120,
                  child: Center(
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                )
              : CircleAvatar(
                  radius: 60,
                  backgroundColor:
                      const Color(0xFFB08968).withValues(alpha: 0.2),
                  backgroundImage: NetworkImage(profilePicUrl),
                ),
        ),
        const SizedBox(height: 16),
        Text(
          _user!.username.replaceAll('_', ' '),
          style: const TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.w700,
            color: Colors.white,
            letterSpacing: 1,
            decoration: TextDecoration.none,
          ),
        ),
        const SizedBox(height: 16),
        if (!_isOwnProfile) ...[
          const SizedBox(height: 4),
          HoverNeumorphicButton(
            onPressed: _isFollowLoading ? null : _toggleFollow,
            boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
            padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 10),
            child: _isFollowLoading
                ? const SizedBox(
                    width: 16,
                    height: 16,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Color(0xFFB08968),
                    ),
                  )
                : Text(
                    _isFollowing ? 'FOLLOWING' : 'FOLLOW',
                    style: TextStyle(
                      color: _isFollowing
                          ? const Color(0xFFB08968).withValues(alpha: 0.5)
                          : const Color(0xFFB08968),
                      fontWeight: FontWeight.w400,
                      fontSize: 12,
                      letterSpacing: 2,
                    ),
                  ),
          ),
        ],
        if (_isOwnProfile) ...[
          HoverNeumorphicButton(
            onPressed: _isUploadingPicture ? null : _changeProfilePicture,
            boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
            child: const Text(
              'CHANGE PHOTO',
              style: TextStyle(
                color: Color(0xFFB08968),
                fontWeight: FontWeight.w400,
                fontSize: 12,
                letterSpacing: 2,
              ),
            ),
          ),
          const SizedBox(height: 8),
          _buildLogoutButton(),
        ],
      ],
    );
  }

  void _openVideoFeed(List<Video> videos, int initialIndex) {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (ctx) => NeumorphicBackground(
          child: SafeArea(
            child: Column(
              children: [
                Expanded(
                  child: Stack(
                    children: [
                      FeedScreen(
                        videoService: widget.videoService,
                        authService: widget.authService,
                        userService: widget.userService,
                        initialVideos: videos,
                        initialIndex: initialIndex,
                        onSwitchTab: widget.onSwitchTab,
                      ),
                      Positioned(
                        top: 8,
                        left: 8,
                        child: NeumorphicButton(
                          onPressed: () => Navigator.of(ctx).pop(),
                          style: const NeumorphicStyle(
                            depth: 2,
                            boxShape: NeumorphicBoxShape.circle(),
                          ),
                          padding: const EdgeInsets.all(10),
                          child: const Icon(
                            Icons.arrow_back_rounded,
                            color: Color(0xFFB08968),
                            size: 20,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                AppBottomNav(
                  onTap: (i) {
                    Navigator.of(ctx).pop();
                    widget.onSwitchTab?.call(i);
                  },
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildVideosGrid() {
    final baseUrl = widget.videoService.client.baseUrl;

    if (_videos.isEmpty) {
      return Neumorphic(
        style: NeumorphicStyle(
          depth: -3,
          boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
        ),
        child: const Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'No videos uploaded yet',
            style: TextStyle(
              color: Color(0xFFB08968),
              fontWeight: FontWeight.w300,
              letterSpacing: 1,
            ),
          ),
        ),
      );
    }

    final readyVideos =
        _videos.where((v) => v.status == VideoStatus.ready).toList();
    final readyIndex = {
      for (var i = 0; i < readyVideos.length; i++) readyVideos[i].id: i,
    };

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        crossAxisSpacing: 2,
        mainAxisSpacing: 2,
        childAspectRatio: 9 / 16,
      ),
      itemCount: _videos.length,
      itemBuilder: (context, index) {
        final video = _videos[index];

        if (video.status == VideoStatus.processing) {
          return _buildProcessingCard();
        }
        if (video.status == VideoStatus.failed) {
          return _buildFailedCard();
        }

        final feedIndex = readyIndex[video.id]!;
        final thumbnailUrl = '$baseUrl/api/videos/${video.id}/thumbnail';
        return GestureDetector(
          onTap: () => _openVideoFeed(readyVideos, feedIndex),
          child: Image.network(
            thumbnailUrl,
            fit: BoxFit.cover,
            errorBuilder: (context, error, stackTrace) => Container(
              color: const Color(0xFFB08968).withValues(alpha: 0.2),
              child: const Icon(
                Icons.videocam_rounded,
                color: Color(0xFFB08968),
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _buildProcessingCard() {
    return Container(
      color: const Color(0xFFB08968).withValues(alpha: 0.15),
      child: const Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              color: Color(0xFFB08968),
            ),
          ),
          SizedBox(height: 8),
          Text(
            'Processing',
            style: TextStyle(
              color: Color(0xFFB08968),
              fontSize: 10,
              fontWeight: FontWeight.w400,
              letterSpacing: 1,
              decoration: TextDecoration.none,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFailedCard() {
    return Container(
      color: Colors.red.withValues(alpha: 0.15),
      child: const Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.error_outline_rounded, color: Colors.red, size: 20),
          SizedBox(height: 8),
          Text(
            'Failed',
            style: TextStyle(
              color: Colors.red,
              fontSize: 10,
              fontWeight: FontWeight.w400,
              letterSpacing: 1,
              decoration: TextDecoration.none,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLogoutButton() {
    return HoverNeumorphicButton(
      onPressed: _logout,
      boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
      padding: const EdgeInsets.symmetric(vertical: 14),
      depth: 4,
      hoveredDepth: 2,
      child: const SizedBox(
        width: double.infinity,
        child: Center(
          child: Text(
            'LOGOUT',
            style: TextStyle(
              color: Color(0xFFB08968),
              fontWeight: FontWeight.w500,
              fontSize: 16,
              letterSpacing: 4,
            ),
          ),
        ),
      ),
    );
  }
}
