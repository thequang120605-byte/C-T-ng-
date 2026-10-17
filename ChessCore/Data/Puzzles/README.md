# ♟️ Xiangqi Arena - Chinese Chess Platform (棋弈江湖)

<div align="center">

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Web%20%7C%20PWA-green.svg)
![AI](https://img.shields.io/badge/AI-Pikafish-orange.svg)
[![Mentioned in Awesome Xiangqi](https://awesome.re/mentioned-badge.svg)](https://github.com/lucaferranti/awesome-xiangqi)

**An open-source Chinese Chess (Xiangqi) learning and playing platform**

[🎮 Live Demo](https://android-xiangqi-offline.netlify.app/) | [📖 Documentation](#features) | [🐛 Report Issues](https://docs.google.com/forms/d/1GYzy-riAftOFiZNwsibmmnYMHih0T4ccjt1Ritm-g2k/viewform)

</div>

---

## 🌟 Overview

A comprehensive Xiangqi platform featuring AI gameplay, board recognition, and extensive puzzle libraries. Built with modern web technologies and fully functional offline.

**Key Highlights:**
- 🤖 Local AI engine powered by Pikafish
- 📸 Open-source board recognition system (physical boards + screenshots)
- 📚 7,300+ puzzles covering openings, endgames, and classical positions
- 📱 Complete offline support via PWA
- ⚖️ Intelligent rule enforcement (checkmate, stalemate, perpetual check/chase detection)

---

## Features

### Game Modes
- ♟️ **Two-Player Mode** - Local or online play
- 🤖 **AI Opponent** - Multiple difficulty levels (PC & Android)
- ⏱️ **Chess Clock** - Tournament-style time controls

### Learning Tools
- 🧠 **Game Review** - Deep AI analysis of completed games (PC only)
- 📊 **Position Analysis** - Real-time move suggestions (PC & Android)
- 📚 **Puzzle Library** - 7,300+ curated positions:
  - Opening patterns (~200 puzzles)
  - Classical endgames (366 puzzles)
  - Complex tactical positions (183 puzzles)
  - Fundamental checkmate patterns
  - Historical game studies

### Board Recognition System

**Industry-first open-source implementation** for Xiangqi board recognition.

#### Two Recognition Modes

**1. Physical Board Recognition**
- Supports wooden, paper, and various physical boards
- Automatic perspective correction (handles camera angles)
- Intelligent coordinate mapping
- Best results with light-colored pieces

**2. Digital Screenshot Recognition**
- Optimized for digital board interfaces
- Dual-model architecture: Detection + Classification
- Automatic side (red/black) identification
- No perspective correction needed

#### Model Files

| Model | Purpose | File |
|-------|---------|------|
| Online Detection | Locate piece positions | `online_xiangqi_piece_detector.onnx` |
| Online Classification | Identify piece types | `online_xiangqi_classifier.onnx` |
| Physical Board | Unified physical board recognition | `Entity_chess_recognition_model.onnx` |

#### Usage Example

```javascript
// Example: Integrate recognition (pseudocode)
const recognizer = new ChessRecognizer();

// Upload image
recognizer.uploadImage(imageFile);

// Mark four corners and center pawn (for calibration)
recognizer.markCorners([topLeft, topRight, bottomRight, bottomLeft]);
recognizer.markCenterPawn(centerPosition);

// Execute recognition
const fenCode = await recognizer.recognize();
console.log(fenCode); // Outputs FEN notation
```

> 💡 See source code for detailed API documentation

### Intelligent Rule System

Automatic detection of:
- ✅ Checkmate and stalemate
- ✅ Draw by insufficient material
- ✅ Draw by repetition
- ✅ Perpetual check/chase detection
- 📜 Based on ROC Xiangqi Rules (2024 revision)

Most positions are correctly adjudicated automatically. In edge cases, players should apply official rules manually.

### Utilities
- 🔄 **Undo Move** - Take back moves
- 💾 **Save/Load Positions** - Store and retrieve games
- ⚙️ **Position Editor** - Create custom legal positions
- 🎨 **Customization** - 5 board themes + 5 piece styles

---

## Platform Support

| Feature | PC (Win/Mac/Linux) | Android | iOS |
|---------|-------------------|---------|-----|
| Two-Player | ✅ | ✅ | ✅ |
| Chess Clock | ✅ | ✅ | ✅ |
| AI Opponent | ✅ | ✅ | ❌* |
| AI Analysis | ✅ | ✅ | ❌* |
| Game Review | ✅ | ❌** | ❌* |
| Board Recognition | ✅ | ✅ | ✅ |

\* iOS: WebAssembly local computation limitations  
** Mobile: Insufficient CPU for deep analysis

---

## Quick Start

### Online Play
Visit: https://android-xiangqi-offline.netlify.app/

### Local Development

```bash
# Clone repository
git clone https://github.com/dffge552/xiangqi-pwa-offline.git

# Navigate to directory
cd xiangqi-pwa-offline

# Install dependencies (if applicable)
npm install

# Start development server
npm start
```

**Requirements**: Modern browser (Chrome 90+, Firefox 88+, Safari 14+)

---

## Technical Stack

- **Frontend**: Vanilla JavaScript (ES6+), HTML5, CSS3
- **AI Engine**: Pikafish (WebAssembly)
- **Computer Vision**: ONNX Runtime (custom-trained models)
- **PWA**: Service Worker + Workbox
- **Storage**: LocalStorage / IndexedDB

---

## AI Engine

Powered by **[Pikafish](https://pikafish.org/)**, an open-source Xiangqi engine:

- 🔓 Free and open source
- ⚡ Continuously updated with strong playing strength
- 🚀 Multi-threaded parallel computation
- 📖 Widely used in Xiangqi education

**Resources:**
- [Pikafish Analysis Platform](https://pikafish.org/)
- [Official Website](https://pikafish.org/)

---

## Documentation

- [📸 Board Recognition Guide](./docs/photo-recognition.md)
- [📚 Puzzle Library Manual](./docs/opening-endgame.md)
- [⚙️ Settings & Features](./docs/game-settings.md)
- [🤖 AI Features Tutorial](./docs/ai-features.md)

---

## Project Motivation

> Hi, I'm a high school sophomore who developed Xiangqi Arena initially as a school project, but mainly driven by my passion for Chinese Chess.
> 
> Existing Xiangqi software often suffers from either overly complex interfaces or lack of modern features. I combined my programming skills to create a platform that's both professional and accessible.
> 
> The core mission is **improving players' skills** through AI analysis, game review, and diverse practice modes. I hope to nurture talented Xiangqi players and bring honor to Taiwan's Xiangqi community!

---

## Contributing

Contributions are welcome! 

### How to Contribute

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Especially Welcome

- 🧠 **AI Model Improvements** - Enhance recognition accuracy
- 🌍 **Internationalization** - Translate interface text
- 🎨 **UI/UX Enhancements** - Improve user experience
- 📚 **Puzzle Expansion** - Add more practice positions
- 🐛 **Bug Fixes** - Report and fix issues

---

## License

This project is licensed under the [MIT License](LICENSE).

**You are free to:**
- ✅ Use commercially
- ✅ Modify
- ✅ Distribute
- ✅ Use privately
- ✅ **Copy and integrate into other projects**

**Only requirement:**
- ⭐ **Attribution required**
- 📄 Preserve copyright notice

### Attribution Example
```
This project uses the board recognition system from "Xiangqi Arena"
Author: dffge552
Source: https://github.com/dffge552/xiangqi-pwa-offline
```
## Other Projects
- [Block Blast Training Platform](https://github.com/dffge552/block-blast) - Puzzle game with guaranteed solvable generation algorithms
---

## Acknowledgments

### Puzzle Sources
- [从寬象棋 YouTube Channel](https://youtube.com/@从宽象棋) - Endgame puzzles and complex positions

### Recommended Learning Resources
- [从寬象棋](https://youtube.com/@从宽象棋)
- Additional resources available in-game

---

## Contact

- 🐛 [Issue Report Form](https://docs.google.com/forms/d/1GYzy-riAftOFiZNwsibmmnYMHih0T4ccjt1Ritm-g2k/edit)
- 📧 Email: xianqitw@gmail.com
- 🌐 Live Demo: https://android-xiangqi-offline.netlify.app/

---

## Star History

If this project helps you, please give it a ⭐ star!

[![Star History Chart](https://api.star-history.com/svg?repos=dffge552/xiangqi-pwa-offline&type=Date)](https://star-history.com/#dffge552/xiangqi-pwa-offline&Date)

---

<div align="center">

**Crafted with Care · Shared Openly · Growing Together**

Made with ❤️ by [dffge552](https://github.com/dffge552)

</div>
