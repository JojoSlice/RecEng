import 'dart:convert';

import 'package:client/models/user.dart';
import 'package:client/services/api_client.dart';

class UserService {
  final ApiClient client;
  UserService(this.client);

  Future<User> getCurrentUser() async {
    final res = await client.get('/api/users/me');
    if (res.statusCode != 200) throw Exception('Failed to load user');
    return User.fromJson(jsonDecode(res.body));
  }

  Future<User> getUser(String userId) async {
    final res = await client.get('/api/users/$userId');
    if (res.statusCode != 200) throw Exception('Failed to load user');
    return User.fromJson(jsonDecode(res.body));
  }

  Future<List<User>> getFollowing(String userId) async {
    final res = await client.get('/api/users/$userId/following');
    if (res.statusCode != 200) throw Exception('Failed to load following');
    final List<dynamic> data = jsonDecode(res.body);
    return data.map((e) => User.fromJson(e)).toList();
  }

  Future<void> followUser(String userId) async {
    final res = await client.post('/api/users/$userId/follow', {});
    if (res.statusCode == 409) throw Exception('Already following');
    if (res.statusCode != 204) throw Exception('Failed to follow user');
  }

  String profilePictureUrl(String userId, {int cacheKey = 0}) =>
      '${client.baseUrl}/api/users/$userId/profile-picture?v=$cacheKey';

  Future<void> unfollowUser(String userId) async {
    final res = await client.delete('/api/users/$userId/follow');
    if (res.statusCode != 204) throw Exception('Failed to unfollow user');
  }
}
