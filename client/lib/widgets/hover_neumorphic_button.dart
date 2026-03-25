import 'package:flutter_neumorphic_plus/flutter_neumorphic.dart';

class HoverNeumorphicButton extends StatefulWidget {
  final VoidCallback? onPressed;
  final Widget child;
  final NeumorphicBoxShape boxShape;
  final EdgeInsets padding;
  final double depth;
  final double hoveredDepth;

  const HoverNeumorphicButton({
    super.key,
    required this.onPressed,
    required this.child,
    required this.boxShape,
    this.padding = const EdgeInsets.all(8),
    this.depth = 2,
    this.hoveredDepth = 1,
  });

  @override
  State<HoverNeumorphicButton> createState() => _HoverNeumorphicButtonState();
}

class _HoverNeumorphicButtonState extends State<HoverNeumorphicButton> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: NeumorphicButton(
        onPressed: widget.onPressed,
        style: NeumorphicStyle(
          depth: _hovered ? widget.hoveredDepth : widget.depth,
          boxShape: widget.boxShape,
        ),
        padding: widget.padding,
        child: widget.child,
      ),
    );
  }
}
