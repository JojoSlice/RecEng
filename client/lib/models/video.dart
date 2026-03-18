import 'package:client/models/uploader.dart';

class Video {
  final String id;
  final String title;
  final String description;
  final List<String> tags;
  final Uploader uploader;
  final DateTime createdAt;

  Video({
    required this.id,
    required this.title,
    required this.description,
    required this.tags,
    required this.uploader,
    required this.createdAt,
  });

  factory Video.fromJson(Map<String, dynamic> json) => Video(
    id: json['id'],
    title: json['title'],
    description: json['description'],
    tags: List<String>.from(json['tags']),
    uploader: Uploader.fromJson(json['uploader']),
    createdAt: DateTime.parse(json['createdAt']),
  );
}
