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
  final List<Video>? initialVideos;
  final int initialIndex;

  const FeedScreen({
    super.key,
    required this.videoService,
    required this.authService,
    this.isVisible = true,
    this.onSwitchTab,
    this.initialVideos,
    this.initialIndex = 0,
  });

  @override
  State<FeedScreen> createState() => _FeedScreenState();
}

class _FeedScreenState extends State<FeedScreen> {
  Future<List<Video>>? _videosFuture;
  int _currentIndex = 0;
  late PageController _pageController;
  bool _followingFeed = false;

  @override
  void initState() {
    super.initState();
    _currentIndex = widget.initialIndex;
    _pageController = PageController(initialPage: widget.initialIndex);
    if (widget.initialVideos == null) {
      _videosFuture = widget.videoService.getVideos();
    }
  }

  void _switchFeed(bool following) {
    if (_followingFeed == following) return;
    _pageController.dispose();
    _pageController = PageController(initialPage: 0);
    setState(() {
      _followingFeed = following;
      _currentIndex = 0;
      _videosFuture = following
          ? widget.videoService.getFollowVideos()
          : widget.videoService.getVideos();
    });
  }

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  Widget _buildPageView(List<Video> videos) {
    return PageView.builder(
      controller: _pageController,
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
  }

  Widget _buildToggle() {
    return Positioned(
      top: 12,
      left: 0,
      right: 0,
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          _toggleItem('FOR YOU', !_followingFeed),
          const SizedBox(width: 24),
          _toggleItem('FOLLOWING', _followingFeed),
        ],
      ),
    );
  }

  Widget _toggleItem(String label, bool active) {
    return GestureDetector(
      onTap: () => _switchFeed(label == 'FOLLOWING'),
      child: Text(
        label,
        style: TextStyle(
          color: active ? Colors.white : Colors.white38,
          fontSize: 13,
          fontWeight: active ? FontWeight.w700 : FontWeight.w400,
          letterSpacing: 2,
          decoration: TextDecoration.none,
          shadows: const [Shadow(blurRadius: 6)],
        ),
      ),
    );
  }

  Widget _buildEmptyFollowingFeed() {
    return const Center(
      child: Text(
        'Follow someone to see their videos here',
        textAlign: TextAlign.center,
        style: TextStyle(
          color: Colors.white54,
          fontSize: 14,
          decoration: TextDecoration.none,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (widget.initialVideos != null) {
      return _buildPageView(widget.initialVideos!);
    }
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
        return Stack(
          children: [
            videos.isEmpty && _followingFeed
                ? _buildEmptyFollowingFeed()
                : _buildPageView(videos),
            _buildToggle(),
          ],
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
  final int _pictureCacheKey = DateTime.now().millisecondsSinceEpoch;

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
          Padding(
            padding: const EdgeInsets.all(8),
            child: Center(
              child: Neumorphic(
                style: NeumorphicStyle(
                  depth: -4,
                  boxShape: NeumorphicBoxShape.roundRect(
                    BorderRadius.circular(16),
                  ),
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(16),
                  child: AspectRatio(
                    aspectRatio: _controller.value.aspectRatio,
                    child: VideoPlayer(_controller),
                  ),
                ),
              ),
            ),
          ),
          Positioned(
            bottom: 24,
            left: 16,
            right: 16,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
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
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      CircleAvatar(
                        radius: 18,
                        backgroundColor: Colors.white24,
                        backgroundImage: NetworkImage(
                          '${widget.baseUrl}/api/users/${widget.video.uploader.id}/profile-picture?v=$_pictureCacheKey',
                          headers: widget.accessToken != null
                              ? {'Authorization': 'Bearer ${widget.accessToken}'}
                              : {},
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        widget.video.uploader.username,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                          decoration: TextDecoration.none,
                          shadows: [Shadow(blurRadius: 4)],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
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
          ),
        ],
      ),
    );
  }
}
