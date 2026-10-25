import 'package:audioplayers/audioplayers.dart';
import 'package:flutter/foundation.dart';
import 'config_service.dart';

class SoundService {
  static final SoundService instance = SoundService._();
  SoundService._();

  final AudioPlayer _player = AudioPlayer();
  bool _initialized = false;
  late ConfigService _config;

  bool get isEnabled => _initialized ? _config.soundEnabled : true;
  set isEnabled(bool value) {
    if (_initialized) {
      _config.soundEnabled = value;
    }
  }

  Future<void> init(ConfigService config) async {
    if (_initialized) return;
    _config = config;
    try {
      await _player.setReleaseMode(ReleaseMode.stop);
      _initialized = true;
    } catch (e) {
      debugPrint('[SoundService] Init error: $e');
    }
  }

  Future<void> playMove() async {
    await _play('sounds/move.wav');
  }

  Future<void> playCapture() async {
    await _play('sounds/capture.wav');
  }

  Future<void> playCheck() async {
    await _play('sounds/check.wav');
  }

  Future<void> playWin() async {
    await _play('sounds/win.wav');
  }

  Future<void> playIllegal() async {
    await _play('sounds/illegal.wav');
  }

  Future<void> _play(String assetPath) async {
    if (!isEnabled) return;
    try {
      await _player.stop();
      await _player.play(AssetSource(assetPath));
    } catch (e) {
      debugPrint('[SoundService] Audio playback warning: $e');
    }
  }

  void dispose() {
    _player.dispose();
  }
}
