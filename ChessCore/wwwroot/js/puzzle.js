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
  let checkedColor = null;
  let currentLevel = 1;
  let initialPieces = [];
  let initialLegal = [];
  let initialChecked = null;
  let pollTimer = null;

  // =====================================================
  // TIỆN ÍCH
  // =====================================================

  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

  function show(sectionId) {
    ["pz-home", "pz-play", "pz-summary"].forEach((id) =>
      $(id).classList.toggle("pz-hidden", id !== sectionId),
    );
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
        } else if (hasNext) {
          action = `<button type="button" class="pz-btn pz-btn-secondary pz-btn-block" data-level="${l.level}">${done > 0 ? "Tiếp tục cấp này" : "Vào cấp này"}</button>`;
        } else {
          action = `<div class="pz-lock" style="color:#6ee7b7"><i class="fa-solid fa-circle-check"></i> Đã hoàn thành</div>`;
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
  }

  // =====================================================
  // BẮT ĐẦU / TIẾP TỤC
  // =====================================================

  function startFlow(onlyLevel) {
    let target = null;

    // Ưu tiên khôi phục bài đang giải dở.
    const saved = localStorage.getItem(LS_CURRENT);
    if (!onlyLevel && saved && !completed.has(saved)) {
      for (const lv of catalog.levels) {
        if (lv.keys.includes(saved) && isUnlocked(lv.level)) {
          target = { key: saved, level: lv.level };
          break;
        }
      }
    }

    if (!target) target = pickNext(onlyLevel);
    if (!target) {
      renderHome();
      return;
    }

    startPuzzle(target.key);
  }

  async function startPuzzle(key) {
    busy = true;
    setMsg("info", "Đang tải bài...");
    show("pz-play");

    let data;
    try {
      const res = await fetch(`${API}/puzzles/${encodeURIComponent(key)}`, { cache: "no-store" });
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
    phase = "playing";
    renderBoard();
  }

  function renderInfo() {
    const lv = catalog.levels.find((l) => l.level === puzzle.level);
    $("pz-badge").textContent = `Cấp ${puzzle.level} — ${puzzle.levelName} · Bài ${puzzle.number}/${puzzle.levelTotal}`;
    $("pz-puzzle-title").textContent = puzzle.title;
    $("pz-goal").innerHTML = `<b>Mục tiêu:</b> ${escapeHtml(puzzle.goal)}.`;
    updateLevelProgress(lv);
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
  }

  function onPieceClick(p) {
    if (busy || phase !== "playing") return;

    if (p.color === puzzle.attacker) {
      hintFrom = null;
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
    pieces = applyLocal(pieces, mv);
    checkedColor = null;

    if (data.result === "wrong") {
      phase = "wrong";
      setMsg("wrong", data.message);
      renderBoard();
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
      renderBoard();
      await sleep(250);
      pieces = data.state.pieces.map((p) => ({ ...p })); // trạng thái chuẩn từ server
      legalMoves = data.state.legalMoves;
      checkedColor = data.state.checkedColor;
      renderBoard();
      busy = false;
      return;
    }

    // solved
    pieces = data.state.pieces.map((p) => ({ ...p }));
    legalMoves = [];
    checkedColor = data.state.checkedColor;
    phase = "solved";
    markCompleted(puzzle.key);
    renderBoard();

    const lv = catalog.levels.find((l) => l.level === puzzle.level);
    updateLevelProgress(lv);
    setMsg("done", "Hoàn thành! Bạn đã chiếu bí đúng cách.");
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
    el.textContent = text;
  }

  function levelFullyDone(level) {
    const lv = catalog.levels.find((l) => l.level === level);
    return lv && lv.keys.every((k) => completed.has(k));
  }

  function updateButtons() {
    $("pz-next").classList.toggle("pz-hidden", phase !== "solved");
    $("pz-retry").classList.toggle("pz-hidden", phase !== "wrong");
    $("pz-hint").classList.toggle("pz-hidden", phase !== "playing");

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

  $("pz-hint").addEventListener("click", async () => {
    if (busy || phase !== "playing") return;
    busy = true;
    try {
      const res = await fetch(`${API}/puzzles/${encodeURIComponent(puzzle.key)}/hint`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ moves: attackerMoves }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Không lấy được gợi ý.");
      if (data.available) {
        hintFrom = { row: data.fromRow, col: data.fromCol };
        selected = null;
        renderBoard();
      }
      setMsg("info", data.message);
    } catch (e) {
      setMsg("wrong", e.message);
    }
    busy = false;
  });

  $("pz-exit").addEventListener("click", () => {
    if (busy) return;
    renderHome();
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
  // KHỞI ĐỘNG
  // =====================================================

  document.addEventListener("click", function (ev) {
    // Bấm ra ngoài bàn cờ để bỏ chọn quân.
    if (phase === "playing" && selected && !ev.target.closest("#board-svg")) {
      selected = null;
      renderBoard();
    }
  });

  renderHome();
})();
