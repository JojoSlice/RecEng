class User {
  final String id;
  final String username;
  final bool hasProfilePicture;

  User({
    required this.id,
    required this.username,
    required this.hasProfilePicture,
  });

  factory User.fromJson(Map<String, dynamic> json) => User(
        id: json['id'],
        username: json['username'],
        hasProfilePicture: json['hasProfilePicture'],
      );
}
