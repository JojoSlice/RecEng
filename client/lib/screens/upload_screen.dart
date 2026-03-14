import 'package:client/services/video_service.dart';
import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';
import 'package:image_picker/image_picker.dart';

class UploadScreen extends StatefulWidget {
  final VideoService videoService;
  final VoidCallback onUploadComplete;

  const UploadScreen({
    super.key,
    required this.videoService,
    required this.onUploadComplete,
  });

  @override
  State<UploadScreen> createState() => _UploadScreenState();
}

class _UploadScreenState extends State<UploadScreen> {
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _tagsController = TextEditingController();
  final _picker = ImagePicker();

  XFile? _selectedVideo;
  bool _isUploading = false;
  String? _error;
  bool _isUploadHovered = false;

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    _tagsController.dispose();
    super.dispose();
  }

  Future<void> _pickVideo(ImageSource source) async {
    final video = await _picker.pickVideo(source: source);
    if (video != null) {
      setState(() {
        _selectedVideo = video;
        _error = null;
      });
    }
  }

  Future<void> _upload() async {
    if (_selectedVideo == null) {
      setState(() => _error = 'Select a video first');
      return;
    }
    if (_titleController.text.trim().isEmpty) {
      setState(() => _error = 'Title is required');
      return;
    }

    setState(() {
      _isUploading = true;
      _error = null;
    });

    try {
      final tags = _tagsController.text
          .split(',')
          .map((t) => t.trim())
          .where((t) => t.isNotEmpty)
          .toList();

      await widget.videoService.uploadVideo(
        filePath: _selectedVideo!.path,
        fileName: _selectedVideo!.name,
        title: _titleController.text.trim(),
        description: _descriptionController.text.trim(),
        tags: tags,
      );

      if (mounted) {
        widget.onUploadComplete();
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _isUploading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 400),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              NeumorphicText(
                'Upload',
                style: const NeumorphicStyle(depth: 6, intensity: 0.9),
                textStyle: NeumorphicTextStyle(
                  fontSize: 36,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 4,
                ),
              ),
              const SizedBox(height: 32),
              Row(
                children: [
                  Expanded(
                    child: _SourceButton(
                      icon: Icons.video_library_rounded,
                      label: 'GALLERY',
                      onTap: () => _pickVideo(ImageSource.gallery),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: _SourceButton(
                      icon: Icons.videocam_rounded,
                      label: 'CAMERA',
                      onTap: () => _pickVideo(ImageSource.camera),
                    ),
                  ),
                ],
              ),
              if (_selectedVideo != null) ...[
                const SizedBox(height: 16),
                Neumorphic(
                  style: NeumorphicStyle(
                    depth: -3,
                    boxShape: NeumorphicBoxShape.roundRect(
                      BorderRadius.circular(12),
                    ),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 12,
                    ),
                    child: Row(
                      children: [
                        const Icon(
                          Icons.check_circle_rounded,
                          color: Color(0xFFB08968),
                          size: 20,
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            _selectedVideo!.name,
                            style: const TextStyle(fontSize: 13),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
              const SizedBox(height: 24),
              _buildTextField(_titleController, 'Title'),
              const SizedBox(height: 16),
              _buildTextField(_descriptionController, 'Description'),
              const SizedBox(height: 16),
              _buildTextField(_tagsController, 'Tags (comma-separated)'),
              if (_error != null) ...[
                const SizedBox(height: 12),
                Text(_error!, style: const TextStyle(color: Colors.red)),
              ],
              const SizedBox(height: 24),
              MouseRegion(
                onEnter: (_) => setState(() => _isUploadHovered = true),
                onExit: (_) => setState(() => _isUploadHovered = false),
                child: NeumorphicButton(
                  onPressed: _isUploading ? null : _upload,
                  style: NeumorphicStyle(
                    depth: _isUploadHovered ? 2 : 4,
                    boxShape: NeumorphicBoxShape.roundRect(
                      BorderRadius.circular(12),
                    ),
                  ),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  child: SizedBox(
                    width: double.infinity,
                    child: Center(
                      child: _isUploading
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Text(
                              'UPLOAD',
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
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTextField(TextEditingController controller, String hint) {
    return Neumorphic(
      style: NeumorphicStyle(
        depth: -4,
        boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
      ),
      child: Material(
        color: Colors.transparent,
        child: TextField(
          controller: controller,
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: const TextStyle(
              color: Color(0xFFB08968),
              fontWeight: FontWeight.w300,
              letterSpacing: 1,
            ),
            border: InputBorder.none,
            filled: true,
            fillColor: Colors.transparent,
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 16,
              vertical: 14,
            ),
          ),
        ),
      ),
    );
  }
}

class _SourceButton extends StatefulWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _SourceButton({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  @override
  State<_SourceButton> createState() => _SourceButtonState();
}

class _SourceButtonState extends State<_SourceButton> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: NeumorphicButton(
        onPressed: widget.onTap,
        style: NeumorphicStyle(
          depth: _isHovered ? 2 : 4,
          boxShape: NeumorphicBoxShape.roundRect(BorderRadius.circular(12)),
        ),
        padding: const EdgeInsets.symmetric(vertical: 20),
        child: Column(
          children: [
            Icon(
              widget.icon,
              color: const Color(0xFFB08968),
              size: 32,
            ),
            const SizedBox(height: 8),
            Text(
              widget.label,
              style: const TextStyle(
                color: Color(0xFFB08968),
                fontWeight: FontWeight.w500,
                fontSize: 12,
                letterSpacing: 2,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
