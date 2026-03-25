import 'package:client/models/user.dart';

enum VideoStatus { processing, ready, failed }

class Video {
  final String id;
  final String title;
  final String description;
  final List<String> tags;
  final User uploader;
  final DateTime createdAt;
  final VideoStatus status;

  Video({
    required this.id,
    required this.title,
    required this.description,
    required this.tags,
    required this.uploader,
    required this.createdAt,
    required this.status,
  });

  factory Video.fromJson(Map<String, dynamic> json) => Video(
    id: json['id'],
    title: json['title'],
    description: json['description'],
    tags: List<String>.from(json['tags']),
    uploader: User.fromJson(json['uploader']),
    createdAt: DateTime.parse(json['createdAt']),
    status: _parseStatus(json['status']),
  );

  static VideoStatus _parseStatus(dynamic s) {
    switch ((s as String?)?.toLowerCase()) {
      case 'ready':
        return VideoStatus.ready;
      case 'failed':
        return VideoStatus.failed;
      default:
        return VideoStatus.processing;
    }
  }
}
