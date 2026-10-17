/* =========================================================
   CỜ THẾ - VƯỢT MÀN
   - Bàn cờ SVG dùng lại hệ toạ độ & CSS của game.css
     (START = 50, ô 100, hàng 0 ở trên, hàng 9 ở dưới).
   - Mọi việc kiểm tra luật + chấm lời giải do server làm (/api/puzzle-run).
========================================================= */
(function () {
  "use strict";

  const SVG_NS = "http://www.w3.org/2000/svg";
  const API = "/api/puzzle-run";
  const START = 50;
  const CELL = 100;

  const LS_DONE = "ct_puzzle_done_v1"; // khách: danh sách bài đã hoàn thành
  const LS_CURRENT = "ct_puzzle_current_v1"; // bài đang giải dở
  const LS_SCORE = "ct_puzzle_score_v1"; // khách: điểm tích lũy cờ thế

  const $ = (id) => document.getElementById(id);
  const piecesLayer = $("pieces-layer");
  const movesLayer = $("moves-layer");

  // ---------- trạng thái ----------
  let catalog = null;
  let completed = new Set();
  let puzzle = null; // chi tiết bài đang giải
  let pieces = []; // [{row, col, type, color}]
  let legalMoves = [];
  let attackerMoves = [];
  let selected = null;
  let busy = false;
  let phase = "idle"; // idle | playing | wrong | solved
  let flip = false;
  let hintFrom = null;
  let hintTo = null;
  let counterMove = null; // { fromRow, fromCol, toRow, toCol } khi AI phản đòn
  let checkedColor = null;
  const MAX_PUZZLE_HINTS = 3;
  let remainingPuzzleHints = MAX_PUZZLE_HINTS;
  let userScore = 500;
  let userLevelInfo = null;
  let hintCosts = { piece: 150, move: 300 };
  let currentLevel = 1;
  let initialPieces = [];
  let initialLegal = [];
  let initialChecked = null;
  let pollTimer = null;

  // =====================================================
  // TIỆN ÍCH
  // =====================================================

  let activeMode = "hub"; // "hub" | "campaign" | "custom"
  let isCustomMode = false;

  // =====================================================
  // HỆ THỐNG ÂM THANH CHÂN THỰC (WEB AUDIO SYNTHESIZER)
  // =====================================================
  const SoundFX = {
    ctx: null,
    init() {
      if (!this.ctx && (window.AudioContext || window.webkitAudioContext)) {
        this.ctx = new (window.AudioContext || window.webkitAudioContext)();
      }
    },
    isMuted() {
      return localStorage.getItem("chess_sound") === "false";
    },
    playMove() {
      if (this.isMuted()) return;
      this.init();
      if (!this.ctx) return;
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = "sine";
      osc.frequency.setValueAtTime(360, this.ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(120, this.ctx.currentTime + 0.08);
      gain.gain.setValueAtTime(0.35, this.ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + 0.08);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + 0.08);
    },
    playCapture() {
      if (this.isMuted()) return;
      this.init();
      if (!this.ctx) return;
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = "triangle";
      osc.frequency.setValueAtTime(450, this.ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(80, this.ctx.currentTime + 0.12);
      gain.gain.setValueAtTime(0.5, this.ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + 0.12);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + 0.12);
    },
    playCheck() {
      if (this.isMuted()) return;
      this.init();
      if (!this.ctx) return;
      [580, 880].forEach((freq, idx) => {
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = "sawtooth";
        const t = this.ctx.currentTime + idx * 0.07;
        osc.frequency.setValueAtTime(freq, t);
        gain.gain.setValueAtTime(0.25, t);
        gain.gain.exponentialRampToValueAtTime(0.01, t + 0.12);
        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start(t);
        osc.stop(t + 0.12);
      });
    },
    playWrong() {
      if (this.isMuted()) return;
      this.init();
      if (!this.ctx) return;
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = "sawtooth";
      osc.frequency.setValueAtTime(150, this.ctx.currentTime);
      osc.frequency.linearRampToValueAtTime(90, this.ctx.currentTime + 0.2);
      gain.gain.setValueAtTime(0.3, this.ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + 0.2);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + 0.2);
    },
    playWin() {
      if (this.isMuted()) return;
      this.init();
      if (!this.ctx) return;
      [523.25, 659.25, 783.99, 1046.5].forEach((freq, idx) => {
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = "triangle";
        const t = this.ctx.currentTime + idx * 0.09;
        osc.frequency.setValueAtTime(freq, t);
        gain.gain.setValueAtTime(0.3, t);
        gain.gain.exponentialRampToValueAtTime(0.01, t + 0.25);
        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start(t);
        osc.stop(t + 0.25);
      });
    }
  };

  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

  function show(sectionId) {
    [
      "pz-hub",
      "pz-home",
      "pz-timerush-screen",
      "pz-history-screen",
      "pz-custom-screen",
      "pz-play",
      "pz-summary",
    ].forEach((id) => {
      const el = $(id);
      if (el) el.classList.toggle("pz-hidden", id !== sectionId);
    });
    window.scrollTo({ top: 0 });
  }

  function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
  }

  function loadLocalDone() {
    try {
      const arr = JSON.parse(localStorage.getItem(LS_DONE) || "[]");
      return new Set(Array.isArray(arr) ? arr : []);
    } catch (e) {
      return new Set();
    }
  }

  function saveLocalDone() {
    localStorage.setItem(LS_DONE, JSON.stringify(Array.from(completed)));
  }

  function calculateLocalLevel(totalScore) {
    let score = Math.max(0, totalScore || 0);
    if (score < 500) {
      const min = 0, max = 500;
      const inLevel = score - min;
      const needed = max - min;
      return {
        level: 1,
        title: "Nhập Môn Cờ Thế",
        icon: "fa-seedling",
        badgeColor: "#94a3b8",
        currentScore: score,
        scoreForNextLevel: max,
        pointsInCurrentLevel: inLevel,
        pointsNeededForCurrentLevel: needed,
        progressPercent: Math.min(100, Math.round((inLevel / needed) * 100)),
        isMaxLevel: false
      };
    } else if (score < 1200) {
      const min = 500, max = 1200;
      const inLevel = score - min;
      const needed = max - min;
      return {
        level: 2,
        title: "Sơ Cấp Kỳ Thủ",
        icon: "fa-chess-pawn",
        badgeColor: "#38bdf8",
        currentScore: score,
        scoreForNextLevel: max,
        pointsInCurrentLevel: inLevel,
        pointsNeededForCurrentLevel: needed,
        progressPercent: Math.min(100, Math.round((inLevel / needed) * 100)),
        isMaxLevel: false
      };
    } else if (score < 2500) {
      const min = 1200, max = 2500;
      const inLevel = score - min;
      const needed = max - min;
      return {
        level: 3,
        title: "Trung Cấp Sát Cục",
        icon: "fa-chess-knight",
        badgeColor: "#a855f7",
        currentScore: score,
        scoreForNextLevel: max,
        pointsInCurrentLevel: inLevel,
        pointsNeededForCurrentLevel: needed,
        progressPercent: Math.min(100, Math.round((inLevel / needed) * 100)),
        isMaxLevel: false
      };
    } else if (score < 4500) {
      const min = 2500, max = 4500;
      const inLevel = score - min;
      const needed = max - min;
      return {
        level: 4,
        title: "Cao Thủ Tàn Cuộc",
        icon: "fa-chess-rook",
        badgeColor: "#f59e0b",
        currentScore: score,
        scoreForNextLevel: max,
        pointsInCurrentLevel: inLevel,
        pointsNeededForCurrentLevel: needed,
        progressPercent: Math.min(100, Math.round((inLevel / needed) * 100)),
        isMaxLevel: false
      };
    } else {
      return {
        level: 5,
        title: "Đại Sư Cờ Thế",
        icon: "fa-crown",
        badgeColor: "#ef4444",
        currentScore: score,
        scoreForNextLevel: 4500,
        pointsInCurrentLevel: score - 4500,
        pointsNeededForCurrentLevel: 0,
        progressPercent: 100,
        isMaxLevel: true
      };
    }
  }

  function loadLocalScore() {
    const raw = localStorage.getItem(LS_SCORE);
    const parsed = parseInt(raw, 10);
    return isNaN(parsed) ? 500 : Math.max(0, parsed);
  }

  function saveLocalScore(score) {
    userScore = Math.max(0, score);
    localStorage.setItem(LS_SCORE, userScore.toString());
  }

  function getRewardForLevel(level, isFirstClear) {
    if (isFirstClear) {
      switch (level) {
        case 1: return 100;
        case 2: return 180;
        case 3: return 300;
        case 4: return 450;
        case 5: return 700;
        default: return 150;
      }
    } else {
      switch (level) {
        case 1: return 20;
        case 2: return 30;
        case 3: return 50;
        case 4: return 75;
        case 5: return 100;
        default: return 25;
      }
    }
  }

  function updateScoreUI() {
    const info = userLevelInfo || calculateLocalLevel(userScore);
    const scoreVal = $("pz-user-score-val");
    if (scoreVal) scoreVal.textContent = userScore.toLocaleString();

    const hubScoreVal = $("pz-hub-score-val");
    if (hubScoreVal) hubScoreVal.textContent = userScore.toLocaleString();

    const hubLevelTitle = $("pz-hub-level-title");
    if (hubLevelTitle) hubLevelTitle.textContent = `Cấp ${info.level} — ${info.title}`;

    const hubLevelIcon = $("pz-hub-level-icon");
    if (hubLevelIcon) {
      hubLevelIcon.className = `fa-solid ${info.icon}`;
      hubLevelIcon.style.color = info.badgeColor;
    }

    const titleEl = $("pz-user-level-title");
    if (titleEl) titleEl.textContent = `Cấp ${info.level} — ${info.title}`;

    const iconEl = $("pz-user-level-icon");
    if (iconEl) {
      iconEl.className = `fa-solid ${info.icon}`;
      iconEl.style.color = info.badgeColor;
    }

    const badgeEl = $("pz-user-level-badge");
    if (badgeEl && info.badgeColor) {
      badgeEl.style.borderColor = info.badgeColor;
    }

    const cardEl = $("pz-score-card");
    if (cardEl && info.badgeColor) {
      cardEl.style.borderTopColor = info.badgeColor;
    }

    const progText = $("pz-score-progress-text");
    if (progText) {
      if (info.isMaxLevel) {
        progText.textContent = "Đã đạt cấp tối đa (Đại Sư)";
      } else {
        progText.textContent = `Tiến độ cấp: ${info.pointsInCurrentLevel || 0} / ${info.pointsNeededForCurrentLevel || 500} điểm`;
      }
    }

    const progPct = $("pz-score-progress-pct");
    if (progPct) progPct.textContent = `${info.progressPercent}%`;

    const progFill = $("pz-score-bar-fill");
    if (progFill) progFill.style.width = `${info.progressPercent}%`;

    // Cập nhật trạng thái nút Gợi ý theo số điểm hiện có
    const pzHintPieceBtn = $("pz-hint-piece-btn");
    const pzHintMoveBtn = $("pz-hint-move-btn");
    const pieceCost = hintCosts.piece || 150;
    const moveCost = hintCosts.move || 300;

    if (pzHintPieceBtn) {
      if (userScore < pieceCost) {
        pzHintPieceBtn.style.opacity = "0.5";
        pzHintPieceBtn.title = `Không đủ điểm (cần ${pieceCost} điểm, bạn có ${userScore} điểm)`;
      } else {
        pzHintPieceBtn.style.opacity = "1";
        pzHintPieceBtn.title = `Gợi ý quân cờ (Tiêu tốn ${pieceCost} điểm)`;
      }
    }

    if (pzHintMoveBtn) {
      if (userScore < moveCost) {
        pzHintMoveBtn.style.opacity = "0.5";
        pzHintMoveBtn.title = `Không đủ điểm (cần ${moveCost} điểm, bạn có ${userScore} điểm)`;
      } else {
        pzHintMoveBtn.style.opacity = "1";
        pzHintMoveBtn.title = `Gợi ý nước đi (Tiêu tốn ${moveCost} điểm)`;
      }
    }
  }

  const PIECE_TEXT = {
    Rook: () => "車",
    Horse: () => "馬",
    Elephant: (c) => (c === "red" ? "相" : "象"),
    Advisor: (c) => (c === "red" ? "仕" : "士"),
    King: (c) => (c === "red" ? "帥" : "將"),
    Cannon: (c) => (c === "red" ? "砲" : "炮"),
    Pawn: (c) => (c === "red" ? "兵" : "卒"),
  };

  function pos(row, col) {
    return {
      x: START + (flip ? 8 - col : col) * CELL,
      y: START + (flip ? 9 - row : row) * CELL,
    };
  }

  // =====================================================
  // DANH MỤC & TIẾN ĐỘ
  // =====================================================

  function isUnlocked(level) {
    for (let l = 1; l < level; l++) {
      const lv = catalog.levels.find((x) => x.level === l);
      if (!lv || lv.total === 0) continue;
      const done = lv.keys.filter((k) => completed.has(k)).length;
      if (done < lv.required) return false;
    }
    return true;
  }

  function levelDone(lv) {
    return lv.keys.filter((k) => completed.has(k)).length;
  }

  function pickNext(onlyLevel) {
    for (const lv of catalog.levels) {
      if (onlyLevel && lv.level !== onlyLevel) continue;
      if (lv.total === 0 || !isUnlocked(lv.level)) continue;
      const key = lv.keys.find((k) => !completed.has(k));
      if (key) return { key, level: lv.level };
    }
    return null;
  }

  async function loadCatalog() {
    const res = await fetch(`${API}/catalog`, { cache: "no-store" });
    if (!res.ok) throw new Error("Không tải được danh mục Cờ Thế.");
    const data = await res.json();

    if (!data.ready) return data;

    catalog = data;
    completed = data.loggedIn ? new Set(data.completed) : loadLocalDone();

    if (data.loggedIn) {
      userScore = data.puzzleScore !== undefined ? data.puzzleScore : 500;
      userLevelInfo = data.levelInfo;
    } else {
      userScore = loadLocalScore();
      userLevelInfo = calculateLocalLevel(userScore);
    }

    if (data.hintCosts) {
      hintCosts = data.hintCosts;
    }

    updateScoreUI();
    return data;
  }

  // =====================================================
  // TRANG CHÍNH
  // =====================================================

  async function renderHome() {
    show("pz-home");
    const body = $("pz-home-body");

    let data;
    try {
      data = await loadCatalog();
    } catch (e) {
      body.innerHTML = `<div class="pz-error">${escapeHtml(e.message)}</div>`;
      return;
    }

    if (!data.ready) {
      const progress = data.toAnalyze ? ` (${data.analyzed}/${data.toAnalyze})` : "";
      body.innerHTML = `<div class="pz-loading"><i class="fa-solid fa-spinner fa-spin"></i>
        Đang chuẩn bị và xác minh lời giải các thế cờ${progress}...<br>Lần đầu có thể mất vài phút.</div>`;
      clearTimeout(pollTimer);
      pollTimer = setTimeout(renderHome, 2500);
      return;
    }

    const totalAll = catalog.levels.reduce((s, l) => s + l.total, 0);
    const doneAll = catalog.levels.reduce((s, l) => s + levelDone(l), 0);

    if (totalAll === 0) {
      body.innerHTML = `<div class="pz-error">Chưa có bài Cờ Thế nào dùng được từ dataset.</div>`;
      return;
    }

    const next = pickNext();
    const current = next ? catalog.levels.find((l) => l.level === next.level) : null;
    const hasProgress = doneAll > 0;
    const pct = Math.round((doneAll / totalAll) * 100);

    const heroText = current
      ? `<div class="pz-stat">Cấp hiện tại: <strong>Cấp ${current.level} — ${escapeHtml(current.name)}</strong></div>`
      : `<div class="pz-stat"><strong>Bạn đã hoàn thành tất cả các bài hiện có.</strong></div>`;

    const levelsHtml = catalog.levels
      .filter((l) => l.total > 0)
      .map((l) => {
        const unlocked = isUnlocked(l.level);
        const done = levelDone(l);
        const p = Math.round((done / l.total) * 100);
        const hasNext = l.keys.some((k) => !completed.has(k));
        let action = "";
        if (!unlocked) {
          action = `<div class="pz-lock"><i class="fa-solid fa-lock"></i> Hoàn thành ${catalog.unlockPercent}% các bài ở cấp trước để mở</div>`;
        } else {
          action = `
            <div style="display: flex; gap: 8px;">
              <button type="button" class="pz-btn pz-btn-secondary" style="flex: 1;" data-level="${l.level}">${done > 0 && hasNext ? "Tiếp tục" : (hasNext ? "Vào chơi" : "Chơi lại")}</button>
              <button type="button" class="pz-btn pz-btn-secondary" data-pick-level="${l.level}" title="Xem danh sách bài"><i class="fa-solid fa-list-ol"></i> Chọn bài</button>
            </div>`;
        }
        return `<div class="pz-card pz-level ${unlocked ? "" : "locked"}">
          <h3>Cấp ${l.level} — ${escapeHtml(l.name)}</h3>
          <div class="pz-rule">${escapeHtml(l.rule)}</div>
          <div class="pz-bar"><span style="width:${p}%"></span></div>
          <div class="pz-stat">Tiến độ: <strong>${done}/${l.total}</strong> bài hoàn thành</div>
          ${action}
        </div>`;
      })
      .join("");

    body.innerHTML = `
      <div class="pz-card pz-hero">
        <div class="pz-hero-info">
          ${heroText}
          <div class="pz-stat">Tiến độ chung: <strong>${doneAll}/${totalAll}</strong> bài (${pct}%)</div>
          <div class="pz-bar"><span style="width:${pct}%"></span></div>
          ${catalog.loggedIn ? "" : `<div class="pz-stat" style="margin-top:10px">Bạn đang chơi với tư cách khách: tiến độ được lưu trên trình duyệt này. <a href="/Auth/Login" style="color:var(--accent-gold-light)">Đăng nhập</a> để lưu theo tài khoản.</div>`}
        </div>
        <div>
          <button type="button" class="pz-btn pz-btn-primary" id="pz-start" ${next ? "" : "disabled"}>${hasProgress ? "Tiếp tục" : "Bắt đầu"}</button>
        </div>
      </div>
      <div class="pz-levels">${levelsHtml}</div>`;

    const startBtn = $("pz-start");
    if (startBtn) startBtn.addEventListener("click", () => startFlow());
    body.querySelectorAll("[data-level]").forEach((b) =>
      b.addEventListener("click", () => startFlow(parseInt(b.dataset.level, 10))),
    );
    body.querySelectorAll("[data-pick-level]").forEach((b) =>
      b.addEventListener("click", () => openPuzzleSelectorModal(parseInt(b.dataset.pickLevel, 10))),
    );
  }

  // =====================================================
  // BẮT ĐẦU / TIẾP TỤC
  // =====================================================

  function startFlow(onlyLevel) {
    let target = null;

    // Nếu chỉ định cấp độ cụ thể (vd: bấm "Chơi lại" hoặc "Tiếp tục" ở thẻ Cấp 1, Cấp 2...)
    if (onlyLevel) {
      const lv = catalog.levels.find((l) => l.level === onlyLevel);
      if (lv && lv.keys.length > 0) {
        // Tìm bài chưa hoàn thành trong cấp này
        const unfinishedKey = lv.keys.find((k) => !completed.has(k));
        if (unfinishedKey) {
          target = { key: unfinishedKey, level: lv.level };
        } else {
          // Nếu đã hoàn thành hết cấp độ này (Chơi lại) -> bắt đầu lại từ bài đầu tiên của cấp!
          target = { key: lv.keys[0], level: lv.level };
        }
      }
    } else {
      // Ưu tiên khôi phục bài đang giải dở.
      const saved = localStorage.getItem(LS_CURRENT);
      if (saved && !completed.has(saved)) {
        for (const lv of catalog.levels) {
          if (lv.keys.includes(saved) && isUnlocked(lv.level)) {
            target = { key: saved, level: lv.level };
            break;
          }
        }
      }

      if (!target) target = pickNext();
      if (!target && catalog.levels.length > 0 && catalog.levels[0].keys.length > 0) {
        // Nếu đã hoàn thành tất cả -> chơi lại từ bài 1 Cấp 1
        target = { key: catalog.levels[0].keys[0], level: catalog.levels[0].level };
      }
    }

    if (!target) {
      renderHome();
      return;
    }

    startPuzzle(target.key);
  }

  async function startPuzzle(key, isCustom = false) {
    busy = true;
    isCustomMode = isCustom;
    setMsg("info", "Đang tải bài...");
    show("pz-play");

    let data;
    try {
      const url = `${API}/puzzles/${encodeURIComponent(key)}` + (isCustom ? "?isCustom=true" : "");
      const res = await fetch(url, { cache: "no-store" });
      data = await res.json();
      if (!res.ok) throw new Error(data.message || "Không tải được bài Cờ Thế.");
    } catch (e) {
      busy = false;
      setMsg("wrong", e.message);
      return;
    }

    puzzle = data;
    currentLevel = data.level;
    localStorage.setItem(LS_CURRENT, key);

    // Người chơi luôn ở phía dưới bàn cờ.
    flip = data.attacker === "Black";

    initialPieces = data.state.pieces.map((p) => ({ ...p }));
    initialLegal = data.state.legalMoves;
    initialChecked = data.state.checkedColor;
    remainingPuzzleHints = MAX_PUZZLE_HINTS;

    resetToInitial();
    renderInfo();
    setMsg("info", "Hãy chọn quân cờ để đi.");
    updateButtons();
    busy = false;
  }

  function resetToInitial() {
    pieces = initialPieces.map((p) => ({ ...p }));
    legalMoves = initialLegal;
    checkedColor = initialChecked;
    attackerMoves = [];
    selected = null;
    hintFrom = null;
    hintTo = null;
    counterMove = null;
    phase = "playing";
    renderBoard();
  }

  function cleanTitle(title) {
    if (!title) return "";
    return title.replace(/\s*\([Bb]ài\s*\d+\)/g, "").trim();
  }

  function renderInfo() {
    const lv = catalog.levels.find((l) => l.level === puzzle.level);
    $("pz-badge").textContent = `Cấp ${puzzle.level} — ${puzzle.levelName} · Bài ${puzzle.number}/${puzzle.levelTotal}`;
    $("pz-puzzle-title").textContent = cleanTitle(puzzle.title);
    $("pz-goal").innerHTML = `<b>Mục tiêu:</b> ${escapeHtml(puzzle.goal)}.`;

    const isDone = completed.has(puzzle.key);
    const reward = getRewardForLevel(puzzle.level, !isDone);
    const rewardTag = $("pz-reward-tag");
    if (rewardTag) {
      rewardTag.innerHTML = `<i class="fa-solid fa-gift"></i> ${isDone ? "Luyện lại: +" : "Thưởng: +"}${reward} điểm`;
      rewardTag.title = isDone 
        ? "Bạn đã hoàn thành bài này trước đó. Luyện tập lại nhận +20-100 điểm." 
        : `Giải thế cờ Cấp ${puzzle.level} này lần đầu tiên nhận trọn vẹn +${reward} điểm!`;
    }

    updateLevelProgress(lv);
    updateScoreUI();
  }

  function updateLevelProgress(lv) {
    if (!lv) return;
    const done = levelDone(lv);
    $("pz-level-bar").style.width = `${Math.round((done / lv.total) * 100)}%`;
    $("pz-level-text").textContent = `Tiến độ cấp: ${done}/${lv.total} bài hoàn thành`;
  }

  // =====================================================
  // VẼ BÀN CỜ
  // =====================================================

  function renderBoard() {
    piecesLayer.innerHTML = "";
    movesLayer.innerHTML = "";
    const hintLayer = $("hint-layer");
    if (hintLayer) hintLayer.innerHTML = "";

    pieces.forEach((p) => {
      const color = p.color.toLowerCase();
      const { x, y } = pos(p.row, p.col);

      const g = document.createElementNS(SVG_NS, "g");
      const classes = ["chess-piece", color];
      if (selected && selected.row === p.row && selected.col === p.col) classes.push("selected");
      if (hintFrom && hintFrom.row === p.row && hintFrom.col === p.col) classes.push("hint");
      if (checkedColor && p.type === "King" && p.color === checkedColor) classes.push("in-check");
      if (phase !== "playing") classes.push("pz-disabled");
      g.setAttribute("class", classes.join(" "));
      g.setAttribute("data-row", p.row);
      g.setAttribute("data-col", p.col);

      const skin = localStorage.getItem("chess_piece_skin") || "traditional";
      
      function getPuzzlePieceFileName(piece, cStr, targetSkin) {
        const isRed = cStr === "Red";
        const actualSkin = targetSkin === "Minimalist" ? "Minimalist White" : targetSkin;
        let pName = "";
        const t = (piece.type || "").toLowerCase();
        if (t === "king" || t === "tướng") pName = isRed ? "Shuai" : "Jiang";
        else if (t === "advisor" || t === "sĩ" || t === "si") pName = "Shi";
        else if (t === "elephant" || t === "tượng" || t === "tuong") pName = "Xiang";
        else if (t === "horse" || t === "knight" || t === "mã" || t === "ma") pName = "Ma";
        else if (t === "rook" || t === "chariot" || t === "xe") pName = "Ju";
        else if (t === "cannon" || t === "pháo" || t === "phao") pName = "Pao";
        else pName = isRed ? "Bing" : "Zu";
        return `${actualSkin} ${cStr} ${pName}.png`;
      }

      function drawVectorPiece() {
        const circle = document.createElementNS(SVG_NS, "circle");
        circle.setAttribute("cx", x);
        circle.setAttribute("cy", y);
        circle.setAttribute("r", 42);
        circle.setAttribute("class", "piece-circle");

        const text = document.createElementNS(SVG_NS, "text");
        text.setAttribute("x", x);
        text.setAttribute("y", y + 2);
        text.setAttribute("text-anchor", "middle");
        text.setAttribute("dominant-baseline", "middle");
        text.setAttribute("class", "piece-text");
        text.textContent = (PIECE_TEXT[p.type] || (() => "?"))(color);

        g.appendChild(circle);
        g.appendChild(text);
      }

      if (skin !== "traditional") {
        const fileName = getPuzzlePieceFileName(p, color, skin);
        const imgPath = `/images/pieces/${fileName}`;
        const img = document.createElementNS(SVG_NS, "image");
        img.setAttribute("x", x - 44);
        img.setAttribute("y", y - 44);
        img.setAttribute("width", 88);
        img.setAttribute("height", 88);
        img.setAttribute("href", imgPath);
        img.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", imgPath);
        img.setAttribute("class", "piece-image-skin");
        img.style.pointerEvents = "all";
        img.style.filter = "drop-shadow(0 4px 6px rgba(0,0,0,0.35))";
        img.addEventListener("error", function() {
          img.remove();
          drawVectorPiece();
        });
        g.appendChild(img);
      } else {
        drawVectorPiece();
      }

      g.addEventListener("click", (ev) => {
        ev.stopPropagation();
        onPieceClick(p);
      });
      piecesLayer.appendChild(g);
    });

    if (selected && phase === "playing") {
      legalMoves
        .filter((m) => m.fromRow === selected.row && m.fromCol === selected.col)
        .forEach((m) => {
          const { x, y } = pos(m.toRow, m.toCol);
          const dot = document.createElementNS(SVG_NS, "circle");
          dot.setAttribute("cx", x);
          dot.setAttribute("cy", y);
          dot.setAttribute("r", m.isCapture ? 25 : 15);
          dot.setAttribute("class", m.isCapture ? "valid-move capture-move" : "valid-move");
          dot.addEventListener("click", (ev) => {
            ev.stopPropagation();
            submitMove({ fromRow: selected.row, fromCol: selected.col, toRow: m.toRow, toCol: m.toCol });
          });
          movesLayer.appendChild(dot);
        });
    }

    if (hintFrom && hintTo && phase === "playing") {
      const fromPos = pos(hintFrom.row, hintFrom.col);
      const toPos = pos(hintTo.row, hintTo.col);

      // Đảm bảo có marker arrowhead trong SVG defs
      const boardSvg = $("board-svg");
      if (boardSvg) {
        let defs = boardSvg.querySelector("defs");
        if (!defs) {
          defs = document.createElementNS(SVG_NS, "defs");
          boardSvg.insertBefore(defs, boardSvg.firstChild);
        }
        if (!document.getElementById("pz-hint-arrowhead")) {
          const marker = document.createElementNS(SVG_NS, "marker");
          marker.setAttribute("id", "pz-hint-arrowhead");
          marker.setAttribute("markerWidth", "10");
          marker.setAttribute("markerHeight", "10");
          marker.setAttribute("refX", "7");
          marker.setAttribute("refY", "5");
          marker.setAttribute("orient", "auto");
          const path = document.createElementNS(SVG_NS, "path");
          path.setAttribute("d", "M 0 1 L 9 5 L 0 9 L 2.5 5 Z");
          path.setAttribute("fill", "#f59e0b");
          marker.appendChild(path);
          defs.appendChild(marker);
        }
      }

      const dx = toPos.x - fromPos.x;
      const dy = toPos.y - fromPos.y;
      const dist = Math.hypot(dx, dy);

      if (dist > 0) {
        const startOffset = 36;
        const endOffset = 34;
        const startX = fromPos.x + (dx / dist) * startOffset;
        const startY = fromPos.y + (dy / dist) * startOffset;
        const endX = toPos.x - (dx / dist) * endOffset;
        const endY = toPos.y - (dy / dist) * endOffset;

        const targetLayer = $("hint-layer") || movesLayer;
        
        // Viền nền tương phản cao
        const arrowOutline = document.createElementNS(SVG_NS, "line");
        arrowOutline.setAttribute("x1", startX);
        arrowOutline.setAttribute("y1", startY);
        arrowOutline.setAttribute("x2", endX);
        arrowOutline.setAttribute("y2", endY);
        arrowOutline.setAttribute("class", "hint-arrow-bg");
        targetLayer.appendChild(arrowOutline);

        // Mũi tên chỉ thẳng đích
        const arrowLine = document.createElementNS(SVG_NS, "line");
        arrowLine.setAttribute("x1", startX);
        arrowLine.setAttribute("y1", startY);
        arrowLine.setAttribute("x2", endX);
        arrowLine.setAttribute("y2", endY);
        arrowLine.setAttribute("class", "hint-arrow");
        arrowLine.setAttribute("marker-end", "url(#pz-hint-arrowhead)");
        targetLayer.appendChild(arrowLine);
      }

      const targetLayer = $("hint-layer") || movesLayer;
      const targetCircle = document.createElementNS(SVG_NS, "circle");
      targetCircle.setAttribute("cx", toPos.x);
      targetCircle.setAttribute("cy", toPos.y);
      targetCircle.setAttribute("r", 28);
      targetCircle.setAttribute("class", "hint-target-circle");
      targetCircle.setAttribute("title", "Bấm vào đây để đi nước gợi ý!");
      targetCircle.addEventListener("click", (ev) => {
        ev.stopPropagation();
        submitMove({ fromRow: hintFrom.row, fromCol: hintFrom.col, toRow: hintTo.row, toCol: hintTo.col });
      });
      targetLayer.appendChild(targetCircle);
    }

    // Hiển thị mũi tên đỏ trực quan khi AI phản đòn hóa giải nước đi sai
    if (counterMove && phase === "wrong") {
      const fromPos = pos(counterMove.fromRow, counterMove.fromCol);
      const toPos = pos(counterMove.toRow, counterMove.toCol);

      const boardSvg = $("board-svg");
      if (boardSvg) {
        let defs = boardSvg.querySelector("defs");
        if (!defs) {
          defs = document.createElementNS(SVG_NS, "defs");
          boardSvg.insertBefore(defs, boardSvg.firstChild);
        }
        if (!document.getElementById("pz-counter-arrowhead")) {
          const marker = document.createElementNS(SVG_NS, "marker");
          marker.setAttribute("id", "pz-counter-arrowhead");
          marker.setAttribute("markerWidth", "10");
          marker.setAttribute("markerHeight", "10");
          marker.setAttribute("refX", "7");
          marker.setAttribute("refY", "5");
          marker.setAttribute("orient", "auto");
          const path = document.createElementNS(SVG_NS, "path");
          path.setAttribute("d", "M 0 1 L 9 5 L 0 9 L 2.5 5 Z");
          path.setAttribute("fill", "#ef4444");
          marker.appendChild(path);
          defs.appendChild(marker);
        }
      }

      const dx = toPos.x - fromPos.x;
      const dy = toPos.y - fromPos.y;
      const dist = Math.hypot(dx, dy);

      if (dist > 0) {
        const startOffset = 34;
        const endOffset = 32;
        const startX = fromPos.x + (dx / dist) * startOffset;
        const startY = fromPos.y + (dy / dist) * startOffset;
        const endX = toPos.x - (dx / dist) * endOffset;
        const endY = toPos.y - (dy / dist) * endOffset;

        const targetLayer = $("hint-layer") || movesLayer;

        const arrowOutline = document.createElementNS(SVG_NS, "line");
        arrowOutline.setAttribute("x1", startX);
        arrowOutline.setAttribute("y1", startY);
        arrowOutline.setAttribute("x2", endX);
        arrowOutline.setAttribute("y2", endY);
        arrowOutline.setAttribute("class", "counter-arrow-bg");
        targetLayer.appendChild(arrowOutline);

        const arrowLine = document.createElementNS(SVG_NS, "line");
        arrowLine.setAttribute("x1", startX);
        arrowLine.setAttribute("y1", startY);
        arrowLine.setAttribute("x2", endX);
        arrowLine.setAttribute("y2", endY);
        arrowLine.setAttribute("class", "counter-arrow");
        arrowLine.setAttribute("marker-end", "url(#pz-counter-arrowhead)");
        targetLayer.appendChild(arrowLine);

        // Vòng tròn highlight vị trí quân phản đòn xuất phát và điểm đích
        const srcCircle = document.createElementNS(SVG_NS, "circle");
        srcCircle.setAttribute("cx", fromPos.x);
        srcCircle.setAttribute("cy", fromPos.y);
        srcCircle.setAttribute("r", 32);
        srcCircle.setAttribute("class", "counter-source-circle");
        targetLayer.appendChild(srcCircle);

        const destCircle = document.createElementNS(SVG_NS, "circle");
        destCircle.setAttribute("cx", toPos.x);
        destCircle.setAttribute("cy", toPos.y);
        destCircle.setAttribute("r", 32);
        destCircle.setAttribute("class", "counter-target-circle");
        targetLayer.appendChild(destCircle);
      }
    }
  }

  function onPieceClick(p) {
    if (busy || phase !== "playing") return;

    if (p.color === puzzle.attacker) {
      hintFrom = null;
      hintTo = null;
      selected = selected && selected.row === p.row && selected.col === p.col ? null : { row: p.row, col: p.col };
      renderBoard();
      return;
    }

    // Bắt quân đối phương bằng cách bấm vào quân đó (nếu hợp lệ).
    if (selected) {
      const ok = legalMoves.some(
        (m) => m.fromRow === selected.row && m.fromCol === selected.col && m.toRow === p.row && m.toCol === p.col,
      );
      if (ok) {
        submitMove({ fromRow: selected.row, fromCol: selected.col, toRow: p.row, toCol: p.col });
      }
    }
  }

  function applyLocal(list, mv) {
    const next = list.filter((p) => !(p.row === mv.toRow && p.col === mv.toCol));
    const piece = next.find((p) => p.row === mv.fromRow && p.col === mv.fromCol);
    if (piece) {
      piece.row = mv.toRow;
      piece.col = mv.toCol;
    }
    return next;
  }

  // =====================================================
  // ĐI QUÂN
  // =====================================================

  async function submitMove(mv) {
    if (busy || phase !== "playing") return;
    busy = true;
    selected = null;
    hintFrom = null;
    hintTo = null;
    movesLayer.innerHTML = "";

    const moves = attackerMoves.concat([mv]);
    let data;
    try {
      const res = await fetch(`${API}/puzzles/${encodeURIComponent(puzzle.key)}/move`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ moves }),
      });
      data = await res.json();
      if (!res.ok) throw new Error(data.message || "Có lỗi khi kiểm tra nước đi.");
    } catch (e) {
      setMsg("wrong", e.message);
      renderBoard();
      busy = false;
      return;
    }

    if (data.result === "illegal") {
      setMsg("wrong", data.message);
      renderBoard();
      busy = false;
      return;
    }

    // Hiện nước đi của người chơi.
    const isCapture = pieces.some((p) => p.row === mv.toRow && p.col === mv.toCol);
    pieces = applyLocal(pieces, mv);
    checkedColor = null;

    if (isCapture) {
      SoundFX.playCapture();
    } else {
      SoundFX.playMove();
    }

    if (data.result === "wrong") {
      phase = "wrong";
      SoundFX.playWrong();
      setMsg("wrong", data.message);
      counterMove = null;
      renderBoard();

      // Nếu đang trong chế độ Sinh tồn (Survival) và đi sai -> Kết thúc ngay!
      if (isTimeRush && timerushType === "survival") {
        setTimeout(() => {
          endTimerush("Bạn đã đi sai 1 nước trong chế độ Sinh tồn!");
        }, 1500);
      }

      // Nếu AI có nước phản đòn, biểu diễn trực quan trên bàn cờ
      if (data.reply) {
        await sleep(500);
        // Lưu toạ độ nước phản đòn để vẽ mũi tên đỏ cảnh báo
        counterMove = {
          fromRow: data.reply.fromRow,
          fromCol: data.reply.fromCol,
          toRow: data.reply.toRow,
          toCol: data.reply.toCol
        };

        const isDefCapture = pieces.some((p) => p.row === data.reply.toRow && p.col === data.reply.toCol);
        pieces = applyLocal(pieces, data.reply);
        checkedColor = null;

        if (isDefCapture) {
          SoundFX.playCapture();
        } else {
          SoundFX.playMove();
        }
        renderBoard();
        await sleep(250);

        if (data.state && data.state.pieces) {
          pieces = data.state.pieces.map((p) => ({ ...p }));
          checkedColor = data.state.checkedColor;
          if (checkedColor) SoundFX.playCheck();
          renderBoard();
        }
      }

      updateButtons();
      busy = false;
      return;
    }

    attackerMoves = moves;
    renderBoard();

    if (data.result === "correct") {
      setMsg("ok", data.message);
      await sleep(450);
      pieces = applyLocal(pieces, data.reply);
      checkedColor = null;
      SoundFX.playMove();
      renderBoard();
      await sleep(250);
      pieces = data.state.pieces.map((p) => ({ ...p })); // trạng thái chuẩn từ server
      legalMoves = data.state.legalMoves;
      checkedColor = data.state.checkedColor;
      if (checkedColor) SoundFX.playCheck();
      renderBoard();
      busy = false;
      return;
    }

    // solved
    pieces = data.state.pieces.map((p) => ({ ...p }));
    legalMoves = [];
    checkedColor = data.state.checkedColor;
    phase = "solved";
    SoundFX.playWin();

    const isFirstTime = !completed.has(puzzle.key);
    markCompleted(puzzle.key);
    renderBoard();

    let pointsGot = data.pointsAwarded;
    if (!pointsGot) {
      pointsGot = getRewardForLevel(puzzle.level, isFirstTime);
    }

    if (catalog && catalog.loggedIn && data.puzzleScore !== undefined) {
      userScore = data.puzzleScore;
      userLevelInfo = data.levelInfo;
    } else {
      saveLocalScore(userScore + pointsGot);
      userLevelInfo = calculateLocalLevel(userScore);
    }
    updateScoreUI();

    if (isTimeRush) {
      timerushScore++;
      updateTimerushUI();
      setMsg("done", `🎯 Xuất sắc! Bạn đã giải xong (${timerushScore} bài, +${pointsGot} điểm). Đang tải bài tiếp theo...`);
      setTimeout(() => {
        loadNextTimerushPuzzle();
      }, 1000);
      busy = false;
      return;
    }

    const lv = catalog.levels.find((l) => l.level === puzzle.level);
    updateLevelProgress(lv);
    setMsg("done", data.message || `Chiếu bí thành công! Bạn nhận được +${pointsGot} điểm Cờ Thế.`);
    updateButtons();
    busy = false;
  }

  function markCompleted(key) {
    // Idempotent: Set không đếm trùng; người đã đăng nhập do server ghi (đúng một lần).
    completed.add(key);
    if (!catalog.loggedIn) saveLocalDone();
    localStorage.removeItem(LS_CURRENT);
  }

  // =====================================================
  // NÚT BẤM
  // =====================================================

  function setMsg(kind, text) {
    const el = $("pz-msg");
    el.className = `pz-msg ${kind}`;
    const icon = kind === "wrong" 
      ? '<i class="fa-solid fa-triangle-exclamation" style="margin-right: 8px; font-size: 16px;"></i>' 
      : kind === "ok" 
      ? '<i class="fa-solid fa-circle-check" style="margin-right: 8px; font-size: 16px;"></i>' 
      : kind === "done" 
      ? '<i class="fa-solid fa-trophy" style="margin-right: 8px; font-size: 16px; color: #d97706;"></i>' 
      : '<i class="fa-solid fa-circle-info" style="margin-right: 8px; font-size: 16px;"></i>';
    el.innerHTML = `${icon}${escapeHtml(text)}`;
  }

  function levelFullyDone(level) {
    const lv = catalog.levels.find((l) => l.level === level);
    return lv && lv.keys.every((k) => completed.has(k));
  }

  function updateButtons() {
    $("pz-next").classList.toggle("pz-hidden", phase !== "solved");
    $("pz-retry").classList.toggle("pz-hidden", phase !== "wrong");
    const hintBtn = $("pz-hint");
    if (hintBtn) {
      hintBtn.classList.toggle("pz-hidden", phase !== "playing");
      const minCost = hintCosts.piece || 150;
      if (userScore >= minCost) {
        hintBtn.innerHTML = `<i class="fa-solid fa-lightbulb"></i> Gợi ý giải cờ <i class="fa-solid fa-caret-up"></i>`;
        hintBtn.disabled = false;
        hintBtn.style.opacity = "1";
      } else {
        hintBtn.innerHTML = `<i class="fa-solid fa-gem" style="color: #ef4444;"></i> Cần tối thiểu ${minCost} đ để gợi ý <i class="fa-solid fa-caret-up"></i>`;
        hintBtn.disabled = false;
        hintBtn.style.opacity = "0.85";
      }
    }

    if (phase === "solved") {
      $("pz-next").textContent = levelFullyDone(puzzle.level) ? "Xem tổng kết cấp độ" : "Chơi tiếp";
    }
  }

  $("pz-retry").addEventListener("click", () => {
    if (busy) return;
    resetToInitial();
    setMsg("info", "Đã khôi phục thế cờ ban đầu. Hãy thử lại.");
    updateButtons();
  });

  // =====================================================
  // CHỌN BÀI & ĐIỀU HƯỚNG BÀI CỜ THẾ
  // =====================================================

  function openPuzzleSelectorModal(targetLevel) {
    const lv = catalog.levels.find((l) => l.level === (targetLevel || currentLevel));
    if (!lv) return;

    const modal = $("pz-modal-puzzles");
    const title = $("pz-modal-level-title");
    const grid = $("pz-modal-grid");
    if (!modal || !grid) return;

    title.innerHTML = `<i class="fa-solid fa-list-ol"></i> Cấp ${lv.level} — ${escapeHtml(lv.name)}`;
    grid.innerHTML = "";

    lv.keys.forEach((k, idx) => {
      const num = idx + 1;
      const isDone = completed.has(k);
      const isCurrent = puzzle && puzzle.key === k;

      const btn = document.createElement("button");
      btn.type = "button";
      btn.className = `pz-pick-btn ${isDone ? "done" : ""} ${isCurrent ? "current" : ""}`;
      btn.innerHTML = `<span style="font-size: 15px; font-weight: 800;">Bài ${num}</span>`;
      btn.title = `Bài ${num}${isDone ? " (Đã giải thành công)" : ""}`;

      btn.addEventListener("click", () => {
        closePuzzleSelectorModal();
        startPuzzle(k);
      });

      grid.appendChild(btn);
    });

    modal.classList.add("show");
  }

  function closePuzzleSelectorModal() {
    const modal = $("pz-modal-puzzles");
    if (modal) modal.classList.remove("show");
  }

  // Đóng modal khi bấm ra ngoài vùng card
  const modalElem = $("pz-modal-puzzles");
  if (modalElem) {
    modalElem.addEventListener("click", (e) => {
      if (e.target === modalElem) {
        closePuzzleSelectorModal();
      }
    });
  }

  const pzModalClose = $("pz-modal-close");
  const pzModalCloseBtn = $("pz-modal-close-btn");
  const pzSelectPuzzle = $("pz-select-puzzle");
  if (pzModalClose) pzModalClose.addEventListener("click", closePuzzleSelectorModal);
  if (pzModalCloseBtn) pzModalCloseBtn.addEventListener("click", closePuzzleSelectorModal);
  if (pzSelectPuzzle) pzSelectPuzzle.addEventListener("click", () => openPuzzleSelectorModal(puzzle ? puzzle.level : currentLevel));

  // Bài trước / Bài sau
  function navigatePuzzle(direction) {
    if (busy || !puzzle) return;
    const lv = catalog.levels.find((l) => l.level === puzzle.level);
    if (!lv) return;

    const currentIdx = lv.keys.indexOf(puzzle.key);
    if (currentIdx === -1) return;

    const newIdx = currentIdx + direction;
    if (newIdx < 0) {
      setMsg("info", "Đây là bài đầu tiên trong cấp này!");
      return;
    }
    if (newIdx >= lv.keys.length) {
      setMsg("info", "Đây là bài cuối cùng trong cấp này!");
      return;
    }

    startPuzzle(lv.keys[newIdx]);
  }

  const pzPrevBtn = $("pz-prev-puzzle");
  const pzSkipBtn = $("pz-skip-puzzle");
  if (pzPrevBtn) pzPrevBtn.addEventListener("click", () => navigatePuzzle(-1));
  if (pzSkipBtn) pzSkipBtn.addEventListener("click", () => navigatePuzzle(1));

  // =====================================================
  // GỢI Ý (2 CẤP ĐỘ: QUÂN CỜ HOẶC NƯỚC ĐI)
  // =====================================================

  const pzHintBtn = $("pz-hint");
  const pzHintDropdown = $("pz-hint-dropdown");
  const pzHintPieceBtn = $("pz-hint-piece-btn");
  const pzHintMoveBtn = $("pz-hint-move-btn");

  if (pzHintBtn) {
    pzHintBtn.addEventListener("click", (ev) => {
      ev.stopPropagation();
      if (pzHintDropdown) pzHintDropdown.classList.toggle("hidden");
    });
  }

  async function handlePuzzleHint(mode = "move") {
    if (pzHintDropdown) pzHintDropdown.classList.add("hidden");
    if (busy || phase !== "playing") return;

    const cost = mode === "piece" ? (hintCosts.piece || 150) : (hintCosts.move || 300);

    if (userScore < cost) {
      setMsg("wrong", `Bạn không đủ điểm! Cần ${cost} điểm để nhận gợi ý ${mode === "piece" ? "quân cờ" : "nước đi"}, nhưng bạn hiện chỉ có ${userScore} điểm. Hãy giải thêm thế cờ để tích lũy điểm nhé!`);
      return;
    }

    busy = true;

    try {
      const res = await fetch(`${API}/puzzles/${encodeURIComponent(puzzle.key)}/hint?mode=${encodeURIComponent(mode)}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ moves: attackerMoves }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Không lấy được gợi ý.");
      if (data.available) {
        if (catalog && catalog.loggedIn && data.remainingScore !== undefined) {
          userScore = data.remainingScore;
          userLevelInfo = data.levelInfo;
        } else {
          saveLocalScore(Math.max(0, userScore - cost));
          userLevelInfo = calculateLocalLevel(userScore);
        }
        updateScoreUI();
        updateButtons();

        hintFrom = { row: data.fromRow, col: data.fromCol };
        if (mode === "move") {
          hintTo = { row: data.toRow, col: data.toCol };
          setMsg("info", `🎯 Gợi ý nước đi (-${cost} điểm): Hãy đi ${data.pieceType || 'quân'} (${data.fromRow}, ${data.fromCol}) ➜ (${data.toRow}, ${data.toCol})`);
        } else {
          hintTo = null; // Chỉ highlight quân
          setMsg("info", `💡 Gợi ý quân cờ (-${cost} điểm): Hãy di chuyển ${data.pieceType || 'quân'} tại ô (${data.fromRow}, ${data.fromCol})`);
        }
        selected = null;
        renderBoard();
      } else {
        setMsg("info", data.message);
      }
    } catch (e) {
      setMsg("wrong", e.message);
    }
    busy = false;
  }

  if (pzHintPieceBtn) {
    pzHintPieceBtn.addEventListener("click", (ev) => {
      ev.stopPropagation();
      handlePuzzleHint("piece");
    });
  }

  if (pzHintMoveBtn) {
    pzHintMoveBtn.addEventListener("click", (ev) => {
      ev.stopPropagation();
      handlePuzzleHint("move");
    });
  }

  document.addEventListener("click", (ev) => {
    if (pzHintDropdown && !pzHintDropdown.classList.contains("hidden")) {
      if (!pzHintDropdown.contains(ev.target) && ev.target !== pzHintBtn && !pzHintBtn.contains(ev.target)) {
        pzHintDropdown.classList.add("hidden");
      }
    }
  });


  $("pz-next").addEventListener("click", async () => {
    if (busy || phase !== "solved") return;

    // Làm mới danh mục (tiến độ chuẩn từ server) rồi mới chọn bài kế tiếp.
    const finishedLevel = puzzle.level;
    try {
      await loadCatalog();
    } catch (e) {
      /* giữ trạng thái cục bộ */
    }
    completed.add(puzzle.key);

    if (levelFullyDone(finishedLevel) && !summaryShown(finishedLevel)) {
      markSummaryShown(finishedLevel);
      renderSummary(finishedLevel);
      return;
    }

    const next = pickNext(finishedLevel) || pickNext();
    if (!next) {
      renderSummary(finishedLevel);
      return;
    }
    startPuzzle(next.key);
  });

  // =====================================================
  // TỔNG KẾT CẤP ĐỘ
  // =====================================================

  const summarySeen = new Set();
  function summaryShown(level) { return summarySeen.has(level); }
  function markSummaryShown(level) { summarySeen.add(level); }

  function renderSummary(level) {
    const lv = catalog.levels.find((l) => l.level === level);
    const done = levelDone(lv);
    const nextLv = catalog.levels.find((l) => l.level > level && l.total > 0);
    const body = $("pz-summary-body");
    const full = done === lv.total;

    let nextInfo = "";
    let actions = "";

    if (nextLv) {
      if (isUnlocked(nextLv.level)) {
        nextInfo = `<p>Cấp ${nextLv.level} — ${escapeHtml(nextLv.name)} đã được mở.</p>`;
        if (nextLv.keys.some((k) => !completed.has(k)))
          actions += `<button type="button" class="pz-btn pz-btn-primary pz-btn-block" id="pz-sum-next">Sang cấp ${nextLv.level}</button>`;
      } else {
        const prev = catalog.levels.find((l) => l.level === level);
        const need = Math.max(0, prev.required - done);
        nextInfo = `<p>Để mở Cấp ${nextLv.level}, cần hoàn thành ít nhất ${catalog.unlockPercent}% bài của cấp này (còn thiếu ${need} bài).</p>`;
      }
    } else {
      nextInfo = `<p>Đây là cấp cuối cùng hiện có.</p>`;
    }

    const remaining = pickNext(level);
    if (!full && remaining) {
      actions += `<button type="button" class="pz-btn pz-btn-secondary pz-btn-block" id="pz-sum-continue">Tiếp tục cấp này</button>`;
    }
    actions += `<button type="button" class="pz-btn pz-btn-secondary pz-btn-block" id="pz-sum-home">Về trang chính</button>`;

    body.innerHTML = `
      <h2><i class="fa-solid fa-trophy"></i> Tổng kết Cấp ${level} — ${escapeHtml(lv.name)}</h2>
      <p>Bạn đã hoàn thành <strong>${done}/${lv.total}</strong> bài của cấp này${full ? " (hoàn thành toàn bộ)." : "."}</p>
      ${nextInfo}
      <div class="pz-actions">${actions}</div>`;

    const bn = $("pz-sum-next");
    if (bn) bn.addEventListener("click", () => startFlow(nextLv.level));
    const bc = $("pz-sum-continue");
    if (bc) bc.addEventListener("click", () => startFlow(level));
    $("pz-sum-home").addEventListener("click", renderHome);

    show("pz-summary");
  }

  // =====================================================
  // CHẾ ĐỘ: HUB, CAMPAIGN, CUSTOM, TIMERUSH, HISTORY
  // =====================================================

  let isTimeRush = false;
  let timerushType = "3min"; // "3min" | "5min" | "survival"
  let timerushSeconds = 180;
  let timerushInterval = null;
  let timerushScore = 0;
  let timerushPuzzles = [];
  let timerushIndex = 0;

  function openHub() {
    activeMode = "hub";
    stopTimerush();
    show("pz-hub");
    updateScoreUI();
  }

  function openCampaign() {
    activeMode = "campaign";
    isCustomMode = false;
    stopTimerush();
    show("pz-home");
    renderHome();
  }

  function openCustomGenerator() {
    activeMode = "custom";
    stopTimerush();
    show("pz-custom-screen");
    executeCustomSearch();
  }

  function openTimerushScreen() {
    activeMode = "timerush";
    stopTimerush();
    show("pz-timerush-screen");
  }

  function openHistoryScreen() {
    activeMode = "history";
    stopTimerush();
    show("pz-history-screen");
    loadHistoryData();
  }

  // Bắt sự kiện trên màn hình Hub
  const btnModeCampaign = $("btn-mode-campaign");
  const btnModeTimerush = $("btn-mode-timerush");
  const btnModeCustom = $("btn-mode-custom");
  const btnModeHistory = $("btn-mode-history");
  const btnCampaignBack = $("btn-campaign-back-to-hub");
  const btnCustomBack = $("btn-custom-back-to-hub");
  const btnTimerushBack = $("btn-timerush-back-to-hub");
  const btnHistoryBack = $("btn-history-back-to-hub");

  if (btnModeCampaign) btnModeCampaign.addEventListener("click", openCampaign);
  if (btnModeTimerush) btnModeTimerush.addEventListener("click", openTimerushScreen);
  if (btnModeCustom) btnModeCustom.addEventListener("click", openCustomGenerator);
  if (btnModeHistory) btnModeHistory.addEventListener("click", openHistoryScreen);

  if (btnCampaignBack) btnCampaignBack.addEventListener("click", openHub);
  if (btnCustomBack) btnCustomBack.addEventListener("click", openHub);
  if (btnTimerushBack) btnTimerushBack.addEventListener("click", openHub);
  if (btnHistoryBack) btnHistoryBack.addEventListener("click", openHub);

  // CHỌN CHẾ ĐỘ CHẠY ĐUA THỜI GIAN
  document.querySelectorAll(".timerush-choice").forEach((card) => {
    card.addEventListener("click", () => {
      document.querySelectorAll(".timerush-choice").forEach((c) => {
        c.classList.remove("active");
        c.querySelector(".timerush-check")?.classList.add("hidden");
      });
      card.classList.add("active");
      card.querySelector(".timerush-check")?.classList.remove("hidden");
      timerushType = card.dataset.type || "3min";
    });
  });

  const btnStartTimerush = $("btn-start-timerush");
  if (btnStartTimerush) {
    btnStartTimerush.addEventListener("click", async () => {
      await startTimerushMode();
    });
  }

  async function startTimerushMode() {
    isTimeRush = true;
    timerushScore = 0;
    timerushIndex = 0;

    if (timerushType === "3min") timerushSeconds = 180;
    else if (timerushType === "5min") timerushSeconds = 300;
    else timerushSeconds = 9999; // Sinh tồn

    const timerBanner = $("pz-timer-banner");
    if (timerBanner) timerBanner.classList.remove("hidden");

    const modeTitle = $("timerush-mode-title");
    if (modeTitle) {
      modeTitle.textContent = timerushType === "3min"
        ? "Siêu tốc 3 phút"
        : timerushType === "5min"
        ? "Tập trung 5 phút"
        : "Sinh tồn (1 lỗi là thua)";
    }

    updateTimerushUI();

    // Lấy danh sách câu đố ngẫu nhiên từ cấp 1 đến cấp 4
    try {
      const res = await fetch(`${API}/custom-search?minElo=700&maxElo=1800&type=all`);
      const data = await res.json();
      timerushPuzzles = (data.puzzles || []).sort(() => Math.random() - 0.5);
    } catch (e) {
      timerushPuzzles = [];
    }

    if (timerushPuzzles.length === 0) {
      if (window.AppDialog) {
        AppDialog.alert("Chưa nạp đủ bài tập cờ thế để bắt đầu.", "Thông báo bài tập", "warning");
      }
      return;
    }

    // Bắt đầu đếm ngược
    clearInterval(timerushInterval);
    if (timerushType !== "survival") {
      timerushInterval = setInterval(() => {
        timerushSeconds--;
        updateTimerushUI();
        if (timerushSeconds <= 0) {
          endTimerush("Hết thời gian! Bạn đã hoàn thành xuất sắc bài thi.");
        }
      }, 1000);
    }

    loadNextTimerushPuzzle();
  }

  function updateTimerushUI() {
    const scoreVal = $("timerush-score-val");
    if (scoreVal) scoreVal.textContent = timerushScore;

    const countdown = $("timerush-countdown");
    if (countdown) {
      if (timerushType === "survival") {
        countdown.textContent = "♥ 1";
      } else {
        const m = Math.floor(timerushSeconds / 60);
        const s = timerushSeconds % 60;
        countdown.textContent = `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
      }
    }
  }

  function stopTimerush() {
    isTimeRush = false;
    clearInterval(timerushInterval);
    const timerBanner = $("pz-timer-banner");
    if (timerBanner) timerBanner.classList.add("hidden");
  }

  function endTimerush(reason) {
    const finalScore = timerushScore;
    const finalType = timerushType;
    stopTimerush();

    // Điểm luyện tập của phiên, không thay đổi Elo tài khoản.
    const practicePoints = finalScore * 5;

    const modal = $("pz-modal-timerush-result");
    const reasonEl = $("timerush-result-reason");
    const scoreEl = $("tr-res-score");
    const timeEl = $("tr-res-time");
    const eloEl = $("tr-res-elo");

    if (reasonEl) reasonEl.textContent = reason || "Bạn đã hoàn thành lượt chạy đua!";
    if (scoreEl) scoreEl.textContent = finalScore;
    if (timeEl) {
      timeEl.textContent = finalType === "3min" ? "3 Phút" : finalType === "5min" ? "5 Phút" : "Sinh Tồn";
    }
    if (eloEl) {
      eloEl.textContent = `${practicePoints} điểm luyện tập (phiên này)`;
    }

    if (modal) {
      modal.classList.add("show");
    }


  }

  // Sự kiện nút trong modal kết quả
  const btnTrResHub = $("btn-tr-res-hub");
  const btnTrResRetry = $("btn-tr-res-retry");
  if (btnTrResHub) {
    btnTrResHub.addEventListener("click", () => {
      const modal = $("pz-modal-timerush-result");
      if (modal) modal.classList.remove("show");
      openHub();
    });
  }
  if (btnTrResRetry) {
    btnTrResRetry.addEventListener("click", () => {
      const modal = $("pz-modal-timerush-result");
      if (modal) modal.classList.remove("show");
      startTimerushMode();
    });
  }

  function loadNextTimerushPuzzle() {
    if (timerushIndex >= timerushPuzzles.length) {
      endTimerush("Chúc mừng! Bạn đã hoàn thành toàn bộ kho bài tập!");
      return;
    }
    const current = timerushPuzzles[timerushIndex++];
    startPuzzle(current.key, true);
  }

  // TẢI DỮ LIỆU LỊCH SỬ GIẢI ĐỀ
  async function loadHistoryData() {
    const listEl = $("history-items-list");
    const summaryText = $("history-summary-text");
    if (!listEl) return;

    listEl.innerHTML = `<div class="pz-loading"><i class="fa-solid fa-spinner fa-spin"></i> Đang nạp lịch sử giải đề...</div>`;

    try {
      const res = await fetch(`${API}/history`);
      if (!res.ok) throw new Error("Không thể tải lịch sử giải đề.");
      const data = await res.json();

      let items = data.history || [];

      // Nếu là khách hoặc database chưa có attempt log nhưng đã có completed puzzles trong catalog
      if (items.length === 0 && completed.size > 0 && catalog && catalog.levels) {
        items = [];
        completed.forEach((key) => {
          for (const lv of catalog.levels) {
            if (lv.keys.includes(key)) {
              items.push({
                key: key,
                title: key,
                level: lv.level,
                levelName: lv.name,
                mateIn: 1,
                attempts: 1,
                isCompleted: true,
                completedAt: "Gần đây"
              });
              break;
            }
          }
        });
      }

      const totalSolved = data.totalSolved || items.filter(x => x.isCompleted).length;
      if (summaryText) {
        summaryText.innerHTML = `<i class="fa-solid fa-trophy" style="color: var(--accent-gold);"></i> Tổng số bài đã giải thành công: <strong>${totalSolved}</strong> bài`;
      }

      if (items.length === 0) {
        listEl.innerHTML = `
          <div class="pz-loading" style="padding: 40px 20px; color: var(--text-muted); text-align: center;">
            <i class="fa-solid fa-chess-board" style="font-size: 36px; color: #cbd5e1; margin-bottom: 12px; display: block;"></i>
            Bạn chưa giải bài cờ thế nào.<br>
            Hãy thử sức với chế độ <strong>Thử thách</strong> hoặc <strong>Tạo đề tùy chỉnh</strong> để ghi lại lịch sử nhé!
          </div>`;
        return;
      }

      let historyPage = 1;
      const historyPageSize = 5;
      const totalHistoryPages = Math.ceil(items.length / historyPageSize) || 1;
      const histPaginationWrapper = $("history-pagination");
      const histPaginationInfo = $("history-pagination-info");
      const histPaginationControls = $("history-pagination-controls");

      function renderHistoryPage(p) {
        historyPage = Math.max(1, Math.min(p, totalHistoryPages));
        const start = (historyPage - 1) * historyPageSize;
        const end = Math.min(start + historyPageSize, items.length);
        const pageItems = items.slice(start, end);

        listEl.innerHTML = pageItems.map((item, idx) => {
          const actualIdx = start + idx;
          return `
          <div class="pz-result-item" style="border-left: 4px solid ${item.isCompleted ? "#10b981" : "#f59e0b"}; display: flex; flex-direction: column; gap: 10px;">
            <div style="display: flex; justify-content: space-between; align-items: center; width: 100%; flex-wrap: wrap; gap: 8px;">
              <div class="pz-result-info">
                <span class="pz-result-title" style="cursor: pointer;" onclick="document.getElementById('hist-detail-${actualIdx}').classList.toggle('pz-hidden')">
                  ${cleanTitle(item.title)} <i class="fa-solid fa-circle-info" style="font-size: 13px; color: var(--accent-gold); margin-left: 4px;" title="Bấm để xem/ẩn chi tiết bài thế"></i>
                </span>
                <div class="pz-result-meta">
                  <span><i class="fa-solid fa-shield-halved"></i> ${item.levelName}</span>
                  <span><i class="fa-solid fa-bolt"></i> Chiếu bí ${item.mateIn} nước</span>
                  <span><i class="fa-solid fa-rotate-right"></i> ${item.attempts || 1} lần thử</span>
                  <span style="color: ${item.isCompleted ? "#059669" : "#d97706"};">
                    ${item.isCompleted ? `<i class="fa-solid fa-circle-check"></i> Đã hoàn thành` : '<i class="fa-solid fa-clock"></i> Đang dở dang'}
                  </span>
                </div>
              </div>
              <div style="display: flex; gap: 6px; flex-wrap: wrap;">
                <button type="button" class="pz-btn pz-btn-secondary" onclick="document.getElementById('hist-detail-${actualIdx}').classList.toggle('pz-hidden')" style="padding: 6px 10px; font-size: 12px;" title="Xem chi tiết">
                  <i class="fa-solid fa-eye"></i> Chi tiết
                </button>
                <button type="button" class="pz-btn pz-btn-secondary btn-inspect-solution" data-key="${item.key}" style="padding: 6px 10px; font-size: 12px; border-color: var(--accent-gold); color: var(--accent-gold);" title="Xem các nước đi đáp án & biến sát cục">
                  <i class="fa-solid fa-graduation-cap"></i> Xem đáp án
                </button>
                <button type="button" class="pz-btn pz-btn-primary btn-replay-history" data-key="${item.key}" style="padding: 6px 14px; font-size: 13px;">
                  <i class="fa-solid fa-rotate-left"></i> ${item.isCompleted ? "Giải lại" : "Giải tiếp"}
                </button>
              </div>
            </div>

            <!-- KHUNG CHI TIẾT BÀI THẾ -->
            <div id="hist-detail-${actualIdx}" class="pz-hidden" style="background: var(--bg-navy-dark); border-radius: 8px; padding: 10px 14px; border: 1px solid var(--border-color); font-size: 12.5px; color: var(--text-muted); line-height: 1.6;">
              <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 8px; margin-bottom: 6px;">
                <div><strong>Mã bài thế:</strong> <code style="color: var(--accent-gold);">${item.key}</code></div>
                <div><strong>Thời gian hoàn thành:</strong> <span style="color: var(--text-main); font-weight: 600;">${item.completedAt || "Chưa giải xong"}</span></div>
                <div><strong>Cập nhật lần cuối:</strong> <span style="color: var(--text-main);">${item.updatedAt || "Gần đây"}</span></div>
                <div><strong>Tổng số quân cờ:</strong> <span style="color: var(--text-main); font-weight: 600;">${item.pieces ? item.pieces + ' quân' : 'Tiêu chuẩn'}</span></div>
              </div>
              <div style="font-size: 12px; color: var(--text-muted); border-top: 1px dashed var(--border-color); padding-top: 6px; margin-top: 6px;">
                <i class="fa-solid fa-lightbulb" style="color: var(--accent-gold);"></i> <strong>Mẹo:</strong> Bấm vào <em>"${item.isCompleted ? "Giải lại" : "Giải tiếp"}"</em> để mở lại bàn cờ và trực tiếp luyện tập, hoặc bấm <em>"Xem đáp án"</em> để nghiên cứu chi tiết đường đi nước bước.
              </div>
            </div>
          </div>
        `;
        }).join("");

        listEl.querySelectorAll(".btn-replay-history").forEach((btn) => {
          btn.addEventListener("click", () => {
            startPuzzle(btn.dataset.key, true);
          });
        });

        listEl.querySelectorAll(".btn-inspect-solution").forEach((btn) => {
          btn.addEventListener("click", () => {
            inspectPuzzleSolution(btn.dataset.key);
          });
        });

        if (histPaginationWrapper && histPaginationInfo && histPaginationControls) {
          if (items.length > historyPageSize) {
            histPaginationWrapper.classList.remove("pz-hidden");
            histPaginationInfo.textContent = `Đang hiển thị ${start + 1} - ${end} / tổng số ${items.length} bài`;

            histPaginationControls.innerHTML = "";
            const prevBtn = document.createElement("button");
            prevBtn.type = "button";
            prevBtn.className = "page-btn";
            prevBtn.innerHTML = '<i class="fa-solid fa-angle-left"></i>';
            prevBtn.disabled = historyPage === 1;
            prevBtn.onclick = () => renderHistoryPage(historyPage - 1);
            histPaginationControls.appendChild(prevBtn);

            for (let i = 1; i <= totalHistoryPages; i++) {
              const pBtn = document.createElement("button");
              pBtn.type = "button";
              pBtn.className = "page-btn" + (i === historyPage ? " active" : "");
              pBtn.textContent = i;
              pBtn.onclick = () => renderHistoryPage(i);
              histPaginationControls.appendChild(pBtn);
            }

            const nextBtn = document.createElement("button");
            nextBtn.type = "button";
            nextBtn.className = "page-btn";
            nextBtn.innerHTML = '<i class="fa-solid fa-angle-right"></i>';
            nextBtn.disabled = historyPage === totalHistoryPages;
            nextBtn.onclick = () => renderHistoryPage(historyPage + 1);
            histPaginationControls.appendChild(nextBtn);
          } else {
            histPaginationWrapper.classList.add("pz-hidden");
          }
        }
      }

      renderHistoryPage(1);
    } catch (e) {
      listEl.innerHTML = `<div class="pz-error">${escapeHtml(e.message)}</div>`;
    }
  }

  // SỰ KIỆN BỘ LỌC TÙY CHỈNH
  const eloSlider = $("custom-elo-slider");
  const eloDisplay = $("custom-elo-display");
  const mateSelectGroup = $("custom-mate-select");
  let selectedMateIn = 0;

  const ELO_LABELS = {
    1: "700 (Người mới) - 900 (Nhập môn)",
    2: "900 (Nhập môn) - 1200 (Cơ bản)",
    3: "1200 (Cơ bản) - 1500 (Trung cấp)",
    4: "1500 (Trung cấp) - 1800 (Nâng cao)",
    5: "1800 (Nâng cao) - 2200+ (Cao thủ)",
  };

  const ELO_RANGES = {
    1: { min: 700, max: 950 },
    2: { min: 950, max: 1250 },
    3: { min: 1250, max: 1550 },
    4: { min: 1550, max: 1850 },
    5: { min: 1850, max: 2400 },
  };

  if (eloSlider && eloDisplay) {
    eloSlider.addEventListener("input", () => {
      const val = parseInt(eloSlider.value, 10);
      eloDisplay.textContent = ELO_LABELS[val] || "Tất cả";
      executeCustomSearch();
    });
  }

  if (mateSelectGroup) {
    mateSelectGroup.querySelectorAll(".pz-pill-btn").forEach((btn) => {
      btn.addEventListener("click", () => {
        mateSelectGroup.querySelectorAll(".pz-pill-btn").forEach((b) => b.classList.remove("active"));
        btn.classList.add("active");
        selectedMateIn = parseInt(btn.dataset.mate, 10);
        executeCustomSearch();
      });
    });
  }

  document.querySelectorAll('input[name="custom-type"]').forEach((radio) => {
    radio.addEventListener("change", executeCustomSearch);
  });

  const btnCustomSearchPlay = $("btn-custom-search-play");
  if (btnCustomSearchPlay) {
    btnCustomSearchPlay.addEventListener("click", () => {
      executeCustomSearch(false);
    });
  }

  let customPuzzlesCache = [];

  async function executeCustomSearch(autoPlayFirst = false) {
    const listEl = $("custom-results-list");
    const countEl = $("custom-results-count");
    if (!listEl) return;

    listEl.innerHTML = `<div class="pz-loading"><i class="fa-solid fa-spinner fa-spin"></i> Đang lọc bài phù hợp...</div>`;
    if (countEl) countEl.textContent = "Đang tìm kiếm bài tập...";

    const sliderVal = eloSlider ? parseInt(eloSlider.value, 10) : 2;
    const range = ELO_RANGES[sliderVal] || { min: 800, max: 1800 };
    const typeChecked = document.querySelector('input[name="custom-type"]:checked');
    const selectedType = typeChecked ? typeChecked.value : "all";

    try {
      const params = new URLSearchParams({
        minElo: range.min,
        maxElo: range.max,
        type: selectedType,
      });
      if (selectedMateIn > 0) {
        params.append("mateIn", selectedMateIn);
      }

      const res = await fetch(`${API}/custom-search?${params.toString()}`);
      if (!res.ok) throw new Error("Lỗi khi tìm bài tập tùy chỉnh.");
      const data = await res.json();
      customPuzzlesCache = data.puzzles || [];

      if (countEl) {
        countEl.innerHTML = `<i class="fa-solid fa-list-check" style="color: var(--accent-gold);"></i> Tìm thấy <strong>${customPuzzlesCache.length}</strong> bài tập phù hợp`;
      }

      if (customPuzzlesCache.length === 0) {
        listEl.innerHTML = `<div class="pz-loading" style="color: var(--text-muted);">Không tìm thấy bài tập thỏa mãn bộ lọc này. Hãy thử nới rộng khoảng Elo hoặc chọn 'Tất cả'.</div>`;
        return;
      }

      if (autoPlayFirst && customPuzzlesCache.length > 0) {
        startPuzzle(customPuzzlesCache[0].key, true);
        return;
      }

      let customPage = 1;
      const customPageSize = 5;
      const totalCustomPages = Math.ceil(customPuzzlesCache.length / customPageSize) || 1;
      const custPaginationWrapper = $("custom-pagination");
      const custPaginationInfo = $("custom-pagination-info");
      const custPaginationControls = $("custom-pagination-controls");

      function renderCustomPage(p) {
        customPage = Math.max(1, Math.min(p, totalCustomPages));
        const start = (customPage - 1) * customPageSize;
        const end = Math.min(start + customPageSize, customPuzzlesCache.length);
        const pageItems = customPuzzlesCache.slice(start, end);

        listEl.innerHTML = pageItems.map((p, idx) => {
          const actualIdx = start + idx;
          const isDone = data.completedKeys && data.completedKeys.includes(p.key);
          return `
            <div class="pz-result-item" data-key="${p.key}">
              <div class="pz-result-info">
                <span class="pz-result-title">${cleanTitle(p.title) || `Bài thế ${actualIdx + 1}`}</span>
                <div class="pz-result-meta">
                  <span><i class="fa-solid fa-bolt"></i> Chiếu bí ${p.mateIn} nước</span>
                  <span><i class="fa-solid fa-chess"></i> ${p.pieces} quân</span>
                  <span><i class="fa-solid fa-shield-halved"></i> ${p.levelName}</span>
                  ${isDone ? '<span style="color: #10b981; background: #ecfdf5;"><i class="fa-solid fa-check"></i> Đã giải</span>' : ""}
                </div>
              </div>
              <button type="button" class="pz-btn pz-btn-secondary btn-play-custom-puzzle" data-key="${p.key}" style="padding: 6px 14px; font-size: 13px;">
                <i class="fa-solid fa-play"></i> Giải ngay
              </button>
            </div>
          `;
        }).join("");

        listEl.querySelectorAll(".btn-play-custom-puzzle").forEach((btn) => {
          btn.addEventListener("click", () => {
            startPuzzle(btn.dataset.key, true);
          });
        });

        if (custPaginationWrapper && custPaginationInfo && custPaginationControls) {
          if (customPuzzlesCache.length > customPageSize) {
            custPaginationWrapper.classList.remove("pz-hidden");
            custPaginationInfo.textContent = `Đang hiển thị ${start + 1} - ${end} / tổng số ${customPuzzlesCache.length} bài`;

            custPaginationControls.innerHTML = "";
            const prevBtn = document.createElement("button");
            prevBtn.type = "button";
            prevBtn.className = "page-btn";
            prevBtn.innerHTML = '<i class="fa-solid fa-angle-left"></i>';
            prevBtn.disabled = customPage === 1;
            prevBtn.onclick = () => renderCustomPage(customPage - 1);
            custPaginationControls.appendChild(prevBtn);

            for (let i = 1; i <= totalCustomPages; i++) {
              const pBtn = document.createElement("button");
              pBtn.type = "button";
              pBtn.className = "page-btn" + (i === customPage ? " active" : "");
              pBtn.textContent = i;
              pBtn.onclick = () => renderCustomPage(i);
              custPaginationControls.appendChild(pBtn);
            }

            const nextBtn = document.createElement("button");
            nextBtn.type = "button";
            nextBtn.className = "page-btn";
            nextBtn.innerHTML = '<i class="fa-solid fa-angle-right"></i>';
            nextBtn.disabled = customPage === totalCustomPages;
            nextBtn.onclick = () => renderCustomPage(customPage + 1);
            custPaginationControls.appendChild(nextBtn);
          } else {
            custPaginationWrapper.classList.add("pz-hidden");
          }
        }
      }

      renderCustomPage(1);
    } catch (e) {
      listEl.innerHTML = `<div class="pz-error">${escapeHtml(e.message)}</div>`;
    }
  }

  // =====================================================
  // XEM ĐÁP ÁN & DIỄN BIẾN NƯỚC ĐI CHI TIẾT
  // =====================================================

  let currentSolutionData = null;

  async function inspectPuzzleSolution(key) {
    const modal = $("pz-modal-solution");
    const titleEl = $("pz-sol-title");
    const metaEl = $("pz-sol-meta");
    const stepsEl = $("pz-sol-steps");
    const noteEl = $("pz-sol-note");
    if (!modal || !stepsEl) return;

    modal.classList.add("show");
    stepsEl.innerHTML = '<div style="color: var(--text-muted); padding: 12px; text-align: center;"><i class="fa-solid fa-spinner fa-spin"></i> Đang tính toán và truy vết các bước giải sát cục...</div>';

    try {
      const res = await fetch(`${API}/puzzles/${encodeURIComponent(key)}/solution`);
      if (!res.ok) throw new Error("Không thể truy xuất lời giải bài này.");
      const data = await res.json();
      currentSolutionData = data;

      if (titleEl) titleEl.textContent = `Thế cờ: ${cleanTitle(data.title)}`;
      if (metaEl) metaEl.innerHTML = `Mục tiêu: <strong>${data.attacker === "Red" ? "Đỏ" : "Đen"}</strong> chiếu bí cưỡng bức trong <strong>${data.mateIn}</strong> nước.`;

      if (!data.steps || data.steps.length === 0) {
        stepsEl.innerHTML = '<div style="color: #ef4444; padding: 10px;">Chưa tìm thấy biến sát cục ngắn nhất cho bài này.</div>';
        return;
      }

      stepsEl.innerHTML = data.steps.map((st) => {
        const att = st.attackerMove;
        const def = st.defenderMove;
        return `
          <div style="background: var(--bg-navy-panel); border: 1px solid var(--border-color); border-radius: 8px; padding: 10px 12px; font-size: 13px;">
            <div style="font-weight: 700; color: var(--accent-gold); margin-bottom: 4px;">
              <i class="fa-solid fa-arrow-turn-down"></i> Nước thứ ${st.step}:
            </div>
            <div style="display: flex; flex-direction: column; gap: 4px; padding-left: 8px;">
              <div style="color: #dc2626; font-weight: 600;">
                <i class="fa-solid fa-chess-king"></i> Tấn công: ${att.description} 
                ${att.isCheckmate ? '<span style="color: #10b981; font-weight: 800; margin-left: 6px;"><i class="fa-solid fa-flag-checkered"></i> SÁT CỤC (CHIẾU BÍ)!</span>' : ''}
              </div>
              ${def ? `
                <div style="color: #475569; font-weight: 500;">
                  <i class="fa-solid fa-shield"></i> Đối phương đỡ: ${def.description}
                </div>
              ` : ''}
            </div>
          </div>
        `;
      }).join("");

    } catch (err) {
      stepsEl.innerHTML = `<div style="color: #ef4444; padding: 10px;">${escapeHtml(err.message)}</div>`;
    }
  }

  // Đóng modal lời giải
  $("pz-modal-solution-close").addEventListener("click", () => {
    $("pz-modal-solution").classList.remove("show");
  });
  $("pz-modal-solution-close-btn").addEventListener("click", () => {
    $("pz-modal-solution").classList.remove("show");
  });

  // Tự động phát lại trên bàn cờ từ modal lời giải
  $("pz-sol-autoplay-btn").addEventListener("click", async () => {
    if (!currentSolutionData) return;
    $("pz-modal-solution").classList.remove("show");

    // Bắt đầu mở bài này ở chế độ thực hành
    await startPuzzle(currentSolutionData.key, true);

    // Di chuyển tự động lần lượt các nước
    if (currentSolutionData.steps && currentSolutionData.steps.length > 0) {
      setMsg("info", "Đang tự động phát lại lời giải mẫu...");
      let delay = 800;
      for (const st of currentSolutionData.steps) {
        const att = st.attackerMove;
        setTimeout(() => {
          submitMove({
            fromRow: att.fromRow,
            fromCol: att.fromCol,
            toRow: att.toRow,
            toCol: att.toCol
          });
        }, delay);
        delay += 1200;
      }
    }
  });

  // Nút xem lời giải ở màn hình chơi bài cờ thế
  const btnShowSol = $("pz-show-solution");
  if (btnShowSol) {
    btnShowSol.addEventListener("click", () => {
      if (!puzzle || !puzzle.key) return;
      inspectPuzzleSolution(puzzle.key);
    });
  }

  // Xử lý nút quay về từ màn chơi bài thế
  $("pz-exit").addEventListener("click", async () => {
    if (busy) return;
    if (isTimeRush) {
      const ok = window.AppDialog 
        ? await AppDialog.confirm("Bạn có chắc chắn muốn dừng chế độ Chạy đua thời gian?", "Dừng thử thách", { confirmText: "Dừng chơi", cancelText: "Tiếp tục chơi", isDanger: true })
        : true;
      if (ok) {
        stopTimerush();
        openTimerushScreen();
      }
    } else if (isCustomMode) {
      openCustomGenerator();
    } else if (activeMode === "history") {
      openHistoryScreen();
    } else {
      openCampaign();
    }
  });

  // =====================================================
  // KHỞI ĐỘNG
  // =====================================================

  document.addEventListener("click", function (ev) {
    // Bấm ra ngoài bàn cờ để bỏ chọn quân.
    if (phase === "playing" && selected && !ev.target.closest("#board-svg")) {
      selected = null;
      renderBoard();
    }
  });

  // Lắng nghe đổi kiểu quân cờ
  window.addEventListener("chess-skin-changed", function () {
    if (phase === "playing" || phase === "solved") {
      renderBoard();
    }
  });

  // Mặc định tải danh mục và hiển thị Hub chọn chế độ
  loadCatalog().then(() => {
    openHub();
  }).catch((e) => {
    console.error("Lỗi nạp danh mục:", e);
    openHub();
  });
})();
