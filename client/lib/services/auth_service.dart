import 'dart:convert';
import 'secure_storage_service.dart';
import 'package:client/services/api_client.dart';
import 'package:client/models/auth.dart';
import 'package:client/models/user.dart';

class AuthService {
  final ApiClient client;
  final SecureStorageService storage;
  AuthService(this.client, this.storage);

  Future<AuthResponse> login(String username, String password) async {
    final res = await client.post('/api/auth/login', {
      'username': username,
      'password': password,
    });
    if (res.statusCode == 401) throw Exception('Invalid username or password');
    if (res.statusCode != 200) throw Exception('Login failed');
    final data = AuthResponse.fromJson(jsonDecode(res.body));
    client.accessToken = data.accessToken;
    await storage.saveTokens(data.accessToken, data.refreshToken);
    return data;
  }

  Future<AuthResponse> register(String username, String password) async {
    final res = await client.post('/api/auth/register', {
      'username': username,
      'password': password,
    });
    if (res.statusCode == 409) throw Exception('Username already taken');
    if (res.statusCode == 400) throw Exception('Username and password are required');
    if (res.statusCode != 201) throw Exception('Registration failed');
    final data = AuthResponse.fromJson(jsonDecode(res.body));
    client.accessToken = data.accessToken;
    await storage.saveTokens(data.accessToken, data.refreshToken);
    return data;
  }

  Future<void> logout() async {
    final refreshToken = await storage.getRefreshToken();
    if (refreshToken != null) {
      await client.post('/api/auth/logout', {'refreshToken': refreshToken});
    }
    client.accessToken = null;
    await storage.logout();
  }

  Future<void> restoreSession() async {
    final token = await storage.getAccessToken();
    if (token != null) client.accessToken = token;
  }

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

  Future<void> unfollowUser(String userId) async {
    final res = await client.delete('/api/users/$userId/follow');
    if (res.statusCode != 204) throw Exception('Failed to unfollow user');
  }
}
