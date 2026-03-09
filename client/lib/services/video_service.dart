import 'dart:convert';

import 'package:client/models/video.dart';
import 'package:client/services/api_client.dart';

class VideoService {
  final ApiClient client;
  VideoService(this.client);

  Future<List<Video>> getVideos() async {
    final res = await client.get('/api/videos');
    if (res.statusCode != 200) throw Exception('Failed to load videos');
    return (jsonDecode(res.body) as List)
        .map((v) => Video.fromJson(v))
        .toList();
  }

  Future<Video> getVideo(String id) async {
    final res = await client.get('/api/videos/$id');
    if (res.statusCode != 200) throw Exception('Failed to load video');
    return Video.fromJson(jsonDecode(res.body));
  }

  String getStreamUrl(String id) => '${client.baseUrl}/api/videos/$id/stream';
}
