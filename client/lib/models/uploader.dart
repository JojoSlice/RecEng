class Uploader {
  final String id;
  final String username;

  Uploader({required this.id, required this.username});

  factory Uploader.fromJson(Map<String, dynamic> json) => Uploader(
    id: json['id'],
    username: json['username'],
  );
}
