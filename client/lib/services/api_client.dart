import 'dart:convert';
import 'package:client/services/secure_storage_service.dart';
import 'package:http/http.dart' as http;

class ApiClient {
  final String baseUrl = const String.fromEnvironment(
    'BASE_URL',
    defaultValue: 'http://localhost:5033',
  );
  final SecureStorageService storage;
  String? accessToken;

  ApiClient(this.storage);

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    if (accessToken != null) 'Authorization': 'Bearer $accessToken',
  };

  Future<http.Response> get(String path) async {
    final res = await http.get(Uri.parse('$baseUrl$path'), headers: _headers);
    if (res.statusCode == 401) {
      await _refresh();
      return http.get(Uri.parse('$baseUrl$path'), headers: _headers);
    }
    return res;
  }

  Future<http.Response> post(String path, Object body) async {
    final res = await http.post(
      Uri.parse('$baseUrl$path'),
      headers: _headers,
      body: jsonEncode(body),
    );
    if (res.statusCode == 401 && path != '/api/auth/refresh') {
      await _refresh();
      return http.post(
        Uri.parse('$baseUrl$path'),
        headers: _headers,
        body: jsonEncode(body),
      );
    }
    return res;
  }

  Future<void> _refresh() async {
    final refreshToken = await storage.getRefreshToken();
    if (refreshToken == null) throw Exception('No refresh token');
    final res = await http.post(
      Uri.parse('$baseUrl/api/auth/refresh'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'refreshToken': refreshToken}),
    );
    if (res.statusCode != 200) {
      accessToken = null;
      await storage.logout();
      throw Exception('Session expired');
    }
    final data = jsonDecode(res.body) as Map<String, dynamic>;
    accessToken = data['accessToken'] as String;
    await storage.saveTokens(accessToken!, data['refreshToken'] as String);
  }
}
