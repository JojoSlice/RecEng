import 'dart:convert';
import 'package:client/services/secure_storage_service.dart';
import 'package:http/http.dart' as http;

class ApiClient {
  final String baseUrl = const String.fromEnvironment(
    'BASE_URL',
    defaultValue: 'http://localhost:5033',
  );
  final SecureStorageService storage;
  String? _accessToken;

  ApiClient(this.storage);

  bool get hasSession => _accessToken != null;

  Map<String, String> get authHeaders => _accessToken != null
      ? {'Authorization': 'Bearer $_accessToken'}
      : {};

  void setToken(String token) => _accessToken = token;
  void clearToken() => _accessToken = null;

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    ...authHeaders,
  };

  Future<http.Response> get(String path) =>
      _withRetry(() => http.get(Uri.parse('$baseUrl$path'), headers: _headers));

  Future<http.Response> post(String path, Object body) {
    Future<http.Response> send() => http.post(
      Uri.parse('$baseUrl$path'),
      headers: _headers,
      body: jsonEncode(body),
    );
    // Skip retry for the refresh endpoint itself to avoid loops.
    if (path == '/api/auth/refresh') return send();
    return _withRetry(send);
  }

  Future<http.Response> delete(String path) =>
      _withRetry(() => http.delete(Uri.parse('$baseUrl$path'), headers: _headers));

  Future<http.Response> _withRetry(Future<http.Response> Function() send) async {
    final res = await send();
    if (res.statusCode == 401) {
      await _refresh();
      return send();
    }
    return res;
  }

  Future<http.StreamedResponse> multipart(
    String path,
    void Function(http.MultipartRequest) build,
  ) async {
    final request = http.MultipartRequest('POST', Uri.parse('$baseUrl$path'));
    request.headers.addAll(authHeaders);
    build(request);
    return request.send();
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
      clearToken();
      await storage.logout();
      throw Exception('Session expired');
    }
    final data = jsonDecode(res.body) as Map<String, dynamic>;
    setToken(data['accessToken'] as String);
    await storage.saveTokens(_accessToken!, data['refreshToken'] as String);
  }
}
