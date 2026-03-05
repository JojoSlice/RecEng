class Video {
  final String id;
  final String title;
  final String description;
  final List<String> tags;
  final String uploadedBy;
  final DateTime createdAt;

  Video({
    required this.id,
    required this.title,
    required this.description,
    required this.tags,
    required this.uploadedBy,
    required this.createdAt,
  });

  factory Video.fromJson(Map<String, dynamic> json) => Video(
    id: json['id'],
    title: json['title'],
    description: json['description'],
    tags: List<String>.from(json['tags']),
    uploadedBy: json['uploadedBy'],
    createdAt: DateTime.parse(json['createdAt']),
  );
}
