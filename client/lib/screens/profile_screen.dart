import 'package:client/models/user.dart';
import 'package:client/models/video.dart';
import 'package:client/screens/login_screen.dart';
import 'package:client/services/auth_service.dart';
import 'package:client/services/video_service.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';
import 'package:image_picker/image_picker.dart';

class ProfileScreen extends StatefulWidget {
  final VideoService videoService;
  final AuthService authService;

  const ProfileScreen({
    super.key,
    required this.videoService,
    required this.authService,
  });

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  User? _user;
  List<Video> _videos = [];
  bool _isLoading = true;
  String? _error;
  bool _isUploadingPicture = false;
  int _pictureCacheKey = 0;
  bool _isChangePhotoHovered = false;
  bool _isLogoutHovered = false;
  final _picker = ImagePicker();

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final user = await widget.authService.getCurrentUser();
      final videos = await widget.videoService.getUserVideos(user.id);
      if (mounted) {
        setState(() {
          _user = user;
          _videos = videos;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString().replaceFirst('Exception: ', '');
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _changeProfilePicture() async {
    final image = await _picker.pickImage(source: ImageSource.gallery);
    if (image == null) return;

    setState(() {
      _isUploadingPicture = true;
      _error = null;
    });

    try {
      final bytes = await image.readAsBytes();
      await widget.videoService.uploadProfilePicture(
        fileBytes: bytes,
        fileName: image.name,
      );
      if (mounted) {
        setState(() => _pictureCacheKey++);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _isUploadingPicture = false);
    }
  }

  Future<void> _logout() async {
    try {
      await widget.authService.logout();
      if (mounted) {
        Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(
            builder: (_) => LoginScreen(authService: widget.authService),
          ),
          (_) => false,
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null && _user == null) {
      return Center(
        child: Text(_error!, style: const TextStyle(color: Colors.red)),
      );
    }

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 400),
          child: Column(
            children: [
              _buildProfileHeader(),
              const SizedBox(height: 32),
              _buildVideosGrid(),
              if (_error != null) ...[
                const SizedBox(height: 12),
                Text(_error!, style: const TextStyle(color: Colors.red)),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildProfileHeader() {
    final baseUrl = widget.videoService.client.baseUrl;
    final userId = _user!.id;
    final profilePicUrl =
        '$baseUrl/api/users/$userId/profile-picture?v=$_pictureCacheKey';

    return Column(
      children: [
        NeumorphicText(
          'RecEng',
          style: const NeumorphicStyle(depth: 6, intensity: 0.9),
          textStyle: NeumorphicTextStyle(
            fontSize: 36,
            fontWeight: FontWeight.w900,
            letterSpacing: 4,
          ),
        ),
        const SizedBox(height: 32),
        Neumorphic(
          style: const NeumorphicStyle(
            depth: 4,
            boxShape: NeumorphicBoxShape.circle(),
          ),
          child: _isUploadingPicture
              ? const SizedBox(
                  width: 120,
                  height: 120,
                  child: Center(
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                )
              : CircleAvatar(
                  radius: 60,
                  backgroundColor:
                      const Color(0xFFB08968).withValues(alpha: 0.2),
                  backgroundImage: NetworkImage(profilePicUrl),
                ),
        ),
        const SizedBox(height: 16),
        MouseRegion(
          onEnter: (_) => setState(() => _isChangePhotoHovered = true),
          onExit: (_) => setState(() => _isChangePhotoHovered = false),
          child: NeumorphicButton(
            onPressed: _isUploadingPicture ? null : _changeProfilePicture,
            style: NeumorphicStyle(
              depth: _isChangePhotoHovered ? 1 : 2,
              boxShape:
                  NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
            ),
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
            child: const Text(
              'CHANGE PHOTO',
              style: TextStyle(
                color: Color(0xFFB08968),
                fontWeight: FontWeight.w400,
                fontSize: 12,
                letterSpacing: 2,
              ),
            ),
          ),
        ),
        const SizedBox(height: 8),
        _buildLogoutButton(),
        const SizedBox(height: 20),
        Text(
          _user!.username.replaceAll('_', ' '),
          style: const TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.w700,
            color: Colors.white,
            letterSpacing: 1,
            decoration: TextDecoration.none,
          ),
        ),
      ],
    );
  }

  Widget _buildVideosGrid() {
    final baseUrl = widget.videoService.client.baseUrl;

    if (_videos.isEmpty) {
      return Neumorphic(
        style: NeumorphicStyle(
          depth: -3,
          boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
        ),
        child: const Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'No videos uploaded yet',
            style: TextStyle(
              color: Color(0xFFB08968),
              fontWeight: FontWeight.w300,
              letterSpacing: 1,
            ),
          ),
        ),
      );
    }

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        crossAxisSpacing: 2,
        mainAxisSpacing: 2,
        childAspectRatio: 9 / 16,
      ),
      itemCount: _videos.length,
      itemBuilder: (context, index) {
        final video = _videos[index];
        final thumbnailUrl = '$baseUrl/api/videos/${video.id}/thumbnail';
        return Image.network(
          thumbnailUrl,
          fit: BoxFit.cover,
          errorBuilder: (_, __, ___) => Container(
            color: const Color(0xFFB08968).withValues(alpha: 0.2),
            child: const Icon(
              Icons.videocam_rounded,
              color: Color(0xFFB08968),
            ),
          ),
        );
      },
    );
  }

  Widget _buildLogoutButton() {
    return MouseRegion(
      onEnter: (_) => setState(() => _isLogoutHovered = true),
      onExit: (_) => setState(() => _isLogoutHovered = false),
      child: NeumorphicButton(
        onPressed: _logout,
        style: NeumorphicStyle(
          depth: _isLogoutHovered ? 2 : 4,
          boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
        ),
        padding: const EdgeInsets.symmetric(vertical: 14),
        child: const SizedBox(
          width: double.infinity,
          child: Center(
            child: Text(
              'LOGOUT',
              style: TextStyle(
                color: Color(0xFFB08968),
                fontWeight: FontWeight.w500,
                fontSize: 16,
                letterSpacing: 4,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
