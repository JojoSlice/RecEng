import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiClient {
  final String baseUrl = "http://localhost:5000";
  String? accessToken;

  Map<String, String> get headers => {
    'Content-Type': 'application/json',
    if (accessToken != null) 'Authorization': 'Berear $accessToken',
  };

  Future<http.Response> get(String path) =>
      http.get(Uri.parse('$baseUrl$path'), headers: headers);

  Future<http.Response> post(String path, Object body) => http.post(
    Uri.parse('$baseUrl$path'),
    headers: headers,
    body: jsonEncode(body),
  );
}
