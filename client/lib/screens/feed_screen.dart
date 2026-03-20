import 'package:client/screens/profile_screen.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/video_service.dart';
import 'package:client/models/video.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';
import 'package:video_player/video_player.dart';

class FeedScreen extends StatefulWidget {
  final VideoService videoService;
  final AuthService authService;
  final bool isVisible;
  final void Function(int)? onSwitchTab;

  const FeedScreen({
    super.key,
    required this.videoService,
    required this.authService,
    this.isVisible = true,
    this.onSwitchTab,
  });

  @override
  State<FeedScreen> createState() => _FeedScreenState();
}

class _FeedScreenState extends State<FeedScreen> {
  late Future<List<Video>> _videosFuture;
  int _currentIndex = 0;

  @override
  void initState() {
    super.initState();
    _videosFuture = widget.videoService.getVideos();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Video>>(
      future: _videosFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(child: Text(snapshot.error.toString()));
        }
        final videos = snapshot.data!;
        return PageView.builder(
          scrollDirection: Axis.vertical,
          itemCount: videos.length,
          onPageChanged: (index) => setState(() => _currentIndex = index),
          itemBuilder: (context, index) {
            return _VideoItem(
              key: ValueKey(videos[index].id),
              video: videos[index],
              streamUrl: widget.videoService.getStreamUrl(videos[index].id),
              baseUrl: widget.videoService.client.baseUrl,
              accessToken: widget.videoService.client.accessToken,
              isActive: index == _currentIndex && widget.isVisible,
              videoService: widget.videoService,
              authService: widget.authService,
              onSwitchTab: widget.onSwitchTab,
            );
          },
        );
      },
    );
  }

}

class _VideoItem extends StatefulWidget {
  final Video video;
  final String streamUrl;
  final String baseUrl;
  final String? accessToken;
  final bool isActive;
  final VideoService videoService;
  final AuthService authService;
  final void Function(int)? onSwitchTab;

  const _VideoItem({
    super.key,
    required this.video,
    required this.streamUrl,
    required this.baseUrl,
    required this.accessToken,
    required this.isActive,
    required this.videoService,
    required this.authService,
    this.onSwitchTab,
  });

  @override
  State<_VideoItem> createState() => _VideoItemState();
}

class _VideoItemState extends State<_VideoItem> {
  late VideoPlayerController _controller;
  bool _initialized = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _initController();
  }

  Future<void> _initController() async {
    try {
      _controller = VideoPlayerController.networkUrl(
        Uri.parse(widget.streamUrl),
        httpHeaders: widget.accessToken != null
            ? {'Authorization': 'Bearer ${widget.accessToken}'}
            : {},
      );
      await _controller.initialize();
      _controller.setLooping(true);
      if (widget.isActive) _controller.play();
      if (mounted) setState(() => _initialized = true);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    }
  }

  @override
  void didUpdateWidget(_VideoItem oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!_initialized) return;
    if (widget.isActive && !oldWidget.isActive) {
      _controller.play();
    } else if (!widget.isActive && oldWidget.isActive) {
      _controller.pause();
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Widget _buildRouteBottomNav(BuildContext ctx) {
    return Neumorphic(
      style: NeumorphicStyle(
        depth: 4,
        boxShape: NeumorphicBoxShape.roundRect(
          const BorderRadius.vertical(top: Radius.circular(16)),
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 32),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          children: [
            _routeNavItem(ctx, Icons.home_rounded, 'Home', 0),
            _routeNavItem(ctx, Icons.add_circle_outline_rounded, 'Upload', 1),
            _routeNavItem(ctx, Icons.person_rounded, 'Profile', 2),
          ],
        ),
      ),
    );
  }

  Widget _routeNavItem(BuildContext ctx, IconData icon, String label, int index) {
    final color = NeumorphicTheme.defaultTextColor(ctx).withValues(alpha: 0.4);
    return GestureDetector(
      onTap: () {
        Navigator.of(ctx).pop();
        widget.onSwitchTab?.call(index);
      },
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
                fontWeight: FontWeight.w400,
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _togglePlayPause() {
    setState(() {
      _controller.value.isPlaying ? _controller.pause() : _controller.play();
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) {
      return Center(child: Text(_error!, style: const TextStyle(color: Colors.red)));
    }
    if (!_initialized) {
      return const Center(child: CircularProgressIndicator());
    }
    return GestureDetector(
      onTap: _togglePlayPause,
      child: Stack(
        fit: StackFit.expand,
        children: [
          Center(
            child: AspectRatio(
              aspectRatio: _controller.value.aspectRatio,
              child: VideoPlayer(_controller),
            ),
          ),
          Positioned(
            bottom: 24,
            left: 16,
            right: 16,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                GestureDetector(
                  onTap: () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (ctx) => NeumorphicBackground(
                        child: SafeArea(
                          child: Column(
                            children: [
                              Expanded(
                                child: ProfileScreen(
                                  videoService: widget.videoService,
                                  authService: widget.authService,
                                  userId: widget.video.uploader.id,
                                ),
                              ),
                              _buildRouteBottomNav(ctx),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 18,
                        backgroundColor: Colors.white24,
                        backgroundImage: NetworkImage(
                          '${widget.baseUrl}/api/users/${widget.video.uploader.id}/profile-picture',
                          headers: widget.accessToken != null
                              ? {'Authorization': 'Bearer ${widget.accessToken}'}
                              : {},
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text(
                        widget.video.uploader.username,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                          decoration: TextDecoration.none,
                          shadows: [Shadow(blurRadius: 4)],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  widget.video.title,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    decoration: TextDecoration.none,
                    shadows: [Shadow(blurRadius: 4)],
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  widget.video.description,
                  style: const TextStyle(
                    color: Colors.white70,
                    fontSize: 13,
                    decoration: TextDecoration.none,
                    shadows: [Shadow(blurRadius: 4)],
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
