import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';

class AppBottomNav extends StatelessWidget {
  /// If set, the matching tab is highlighted as active.
  final int? activeIndex;
  final void Function(int) onTap;

  const AppBottomNav({
    super.key,
    this.activeIndex,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Neumorphic(
      style: NeumorphicStyle(
        depth: 4,
        boxShape: NeumorphicBoxShape.roundRect(
          const BorderRadius.vertical(top: Radius.circular(16)),
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 32),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          children: [
            _navItem(context, Icons.home_rounded, 'Home', 0),
            _navItem(context, Icons.add_circle_outline_rounded, 'Upload', 1),
            _navItem(context, Icons.person_rounded, 'Profile', 2),
          ],
        ),
      ),
    );
  }

  Widget _navItem(BuildContext context, IconData icon, String label, int index) {
    final isActive = activeIndex == index;
    final color = isActive
        ? const Color(0xFFB08968)
        : NeumorphicTheme.defaultTextColor(context).withValues(alpha: 0.4);

    return GestureDetector(
      onTap: () => onTap(index),
      behavior: HitTestBehavior.opaque,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 28, color: color),
            const SizedBox(height: 4),
            Text(
              label,
              style: TextStyle(
                color: color,
                fontSize: 11,
                fontWeight: isActive ? FontWeight.w600 : FontWeight.w400,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
