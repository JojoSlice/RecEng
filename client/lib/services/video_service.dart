import 'dart:convert';

import 'package:client/models/video.dart';
import 'package:client/services/api_client.dart';
import 'package:http/http.dart' as http;

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

  Future<void> uploadVideo({
    required String filePath,
    required String fileName,
    required String title,
    required String description,
    required List<String> tags,
  }) async {
    final uri = Uri.parse('${client.baseUrl}/api/videos/');
    final request = http.MultipartRequest('POST', uri);

    if (client.accessToken != null) {
      request.headers['Authorization'] = 'Bearer ${client.accessToken}';
    }

    request.fields['title'] = title;
    request.fields['description'] = description;
    for (final tag in tags) {
      request.files.add(http.MultipartFile.fromString('tags', tag));
    }
    request.files.add(await http.MultipartFile.fromPath('file', filePath, filename: fileName));

    final response = await request.send();
    if (response.statusCode != 201) {
      final body = await response.stream.bytesToString();
      throw Exception(body);
    }
  }
}
