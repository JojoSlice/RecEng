import 'package:client/services/auth_service.dart';
import 'package:client/services/video_service.dart';
import 'package:client/models/video.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';

class FeedScreen extends StatefulWidget {
  final VideoService videoService;
  final AuthService authService;

  const FeedScreen({
    super.key,
    required this.videoService,
    required this.authService,
  });

  @override
  State<FeedScreen> createState() => _FeedScreenState();
}

class _FeedScreenState extends State<FeedScreen> {
  late Future<List<Video>> _videosFuture;

  @override
  void initState() {
    super.initState();
    _videosFuture = widget.videoService.getVideos();
  }

  @override
  Widget build(BuildContext context) {
    return NeumorphicBackground(
      child: SafeArea(
        child: FutureBuilder<List<Video>>(
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
              itemBuilder: (context, index) {
                return Center(child: Text(videos[index].title));
              },
            );
          },
        ),
      ),
    );
  }
}
