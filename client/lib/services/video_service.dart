import 'dart:convert';

import 'package:client/models/video.dart';
import 'package:client/services/api_client.dart';
import 'package:http/http.dart' as http; // for MultipartFile

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
  String thumbnailUrl(String id) => '${client.baseUrl}/api/videos/$id/thumbnail';

  Map<String, String> get streamHeaders => client.authHeaders;

  Future<String> uploadVideo({
    required List<int> fileBytes,
    required String fileName,
    required String title,
    required String description,
    required List<String> tags,
  }) async {
    final response = await client.multipart('/api/videos/', (req) {
      req.fields['title'] = title;
      req.fields['description'] = description;
      for (var i = 0; i < tags.length; i++) {
        req.fields['tags[$i]'] = tags[i];
      }
      req.files.add(http.MultipartFile.fromBytes('file', fileBytes, filename: fileName));
    });

    if (response.statusCode != 202) {
      final body = await response.stream.bytesToString();
      throw Exception(body);
    }

    final body = await response.stream.bytesToString();
    final json = jsonDecode(body) as Map<String, dynamic>;
    return json['id'] as String;
  }

  Future<List<Video>> getFollowVideos() async {
    final res = await client.get('/api/videos/follow');
    if (res.statusCode != 200) throw Exception('Failed to load feed');
    return (jsonDecode(res.body) as List)
        .map((v) => Video.fromJson(v))
        .toList();
  }

  Future<List<Video>> getUserVideos(String userId) async {
    final res = await client.get('/api/users/$userId/videos');
    if (res.statusCode != 200) throw Exception('Failed to load user videos');
    return (jsonDecode(res.body) as List)
        .map((v) => Video.fromJson(v))
        .toList();
  }

  Future<void> likeVideo(String id) async {
    final res = await client.post('/api/videos/$id/like', {});
    if (res.statusCode != 204 && res.statusCode != 409) {
      throw Exception('Failed to like video');
    }
  }

  Future<void> unlikeVideo(String id) async {
    final res = await client.post('/api/videos/$id/unlike', {});
    if (res.statusCode != 204 && res.statusCode != 409) {
      throw Exception('Failed to unlike video');
    }
  }

  Future<void> uploadProfilePicture({
    required List<int> fileBytes,
    required String fileName,
  }) async {
    final response = await client.multipart('/api/users/me/profile-picture', (req) {
      req.files.add(http.MultipartFile.fromBytes('file', fileBytes, filename: fileName));
    });

    if (response.statusCode != 200) {
      final body = await response.stream.bytesToString();
      throw Exception(body);
    }
  }
}
