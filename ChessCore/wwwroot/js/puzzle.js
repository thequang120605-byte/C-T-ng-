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
  let hintTo = null;
  let checkedColor = null;
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

  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

  function show(sectionId) {
    ["pz-hub", "pz-home", "pz-custom-screen", "pz-play", "pz-summary"].forEach((id) => {
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
    pieces = applyLocal(pieces, mv);
    checkedColor = null;

    if (data.result === "wrong") {
      phase = "wrong";
      setMsg("wrong", data.message);
      renderBoard();

      // Nếu đang trong chế độ Sinh tồn (Survival) và đi sai -> Kết thúc ngay!
      if (isTimeRush && timerushType === "survival") {
        setTimeout(() => {
          endTimerush("Bạn đã đi sai 1 nước trong chế độ Sinh tồn!");
        }, 1200);
      }

      // Nếu AI có nước phản đòn, biểu diễn trực quan trên bàn cờ
      if (data.reply) {
        await sleep(500);
        pieces = applyLocal(pieces, data.reply);
        checkedColor = null;
        renderBoard();
        await sleep(250);

        if (data.state && data.state.pieces) {
          pieces = data.state.pieces.map((p) => ({ ...p }));
          checkedColor = data.state.checkedColor;
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

    if (isTimeRush) {
      timerushScore++;
      updateTimerushUI();
      setMsg("done", `🎯 Xuất sắc! Bạn đã giải xong (${timerushScore} bài). Đang tải bài tiếp theo...`);
      setTimeout(() => {
        loadNextTimerushPuzzle();
      }, 1000);
      busy = false;
      return;
    }

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
        hintFrom = { row: data.fromRow, col: data.fromCol };
        const pointNote = data.pointsDeducted ? ` (-${data.pointsDeducted} Elo)` : "";
        if (data.remainingElo !== undefined && data.remainingElo !== null) {
          const eloBadge = document.getElementById("navbar-user-elo");
          if (eloBadge) {
            eloBadge.innerHTML = `<i class="fa-solid fa-star" style="font-size: 9px;"></i> ${data.remainingElo}`;
          }
        }
        if (mode === "move") {
          hintTo = { row: data.toRow, col: data.toCol };
          setMsg("info", `🎯 Gợi ý nước đi${pointNote}: Hãy đi ${data.pieceType || 'quân'} (${data.fromRow}, ${data.fromCol}) ➜ (${data.toRow}, ${data.toCol})`);
        } else {
          hintTo = null; // Chỉ highlight quân
          setMsg("info", `💡 Gợi ý quân cờ${pointNote}: Hãy di chuyển ${data.pieceType || 'quân'} tại ô (${data.fromRow}, ${data.fromCol})`);
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
      alert("Chưa nạp đủ bài tập cờ thế để bắt đầu.");
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
    stopTimerush();
    alert(`🏁 KẾT THÚC THỬ THÁCH THỜI GIAN!\n${reason}\n\nĐiểm số của bạn: ${timerushScore} bài giải đúng!`);
    openHub();
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

    listEl.innerHTML = `<div class="pz-loading"><i class="fa-solid fa-spinner fa-spin"></i> Đang tải lịch sử...</div>`;

    try {
      const res = await fetch(`${API}/history`);
      if (!res.ok) throw new Error("Không thể tải lịch sử giải đề.");
      const data = await res.json();

      if (summaryText) {
        summaryText.innerHTML = `<i class="fa-solid fa-trophy" style="color: var(--accent-gold);"></i> Tổng số bài đã giải thành công: <strong>${data.totalSolved || 0}</strong> bài`;
      }

      const items = data.history || [];
      if (items.length === 0) {
        listEl.innerHTML = `<div class="pz-loading" style="color: var(--text-muted);">Bạn chưa có lịch sử giải đề nào. Hãy bắt đầu giải thử thách hoặc tự tạo đề bài nhé!</div>`;
        return;
      }

      listEl.innerHTML = items.map((item) => `
        <div class="pz-result-item" style="border-left: 4px solid ${item.isCompleted ? "#10b981" : "#f59e0b"};">
          <div class="pz-result-info">
            <span class="pz-result-title">${cleanTitle(item.title)}</span>
            <div class="pz-result-meta">
              <span><i class="fa-solid fa-shield-halved"></i> ${item.levelName}</span>
              <span><i class="fa-solid fa-bolt"></i> Chiếu bí ${item.mateIn} nước</span>
              <span><i class="fa-solid fa-rotate-right"></i> ${item.attempts} lần thử</span>
              <span style="color: ${item.isCompleted ? "#059669" : "#d97706"};">
                ${item.isCompleted ? `<i class="fa-solid fa-circle-check"></i> Hoàn thành: ${item.completedAt || ""}` : '<i class="fa-solid fa-clock"></i> Chưa xong'}
              </span>
            </div>
          </div>
          <button type="button" class="pz-btn pz-btn-secondary btn-replay-history" data-key="${item.key}" style="padding: 6px 14px; font-size: 13px;">
            <i class="fa-solid fa-rotate-left"></i> Giải lại
          </button>
        </div>
      `).join("");

      listEl.querySelectorAll(".btn-replay-history").forEach((btn) => {
        btn.addEventListener("click", () => {
          startPuzzle(btn.dataset.key, true);
        });
      });
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

      listEl.innerHTML = customPuzzlesCache.map((p, idx) => {
        const isDone = data.completedKeys && data.completedKeys.includes(p.key);
        return `
          <div class="pz-result-item" data-key="${p.key}">
            <div class="pz-result-info">
              <span class="pz-result-title">${cleanTitle(p.title) || `Bài thế ${idx + 1}`}</span>
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
    } catch (e) {
      listEl.innerHTML = `<div class="pz-error">${escapeHtml(e.message)}</div>`;
    }
  }

  // Xử lý nút quay về từ màn chơi bài thế
  $("pz-exit").addEventListener("click", () => {
    if (busy) return;
    if (isCustomMode) {
      openCustomGenerator();
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

  // Mặc định tải danh mục và hiển thị Hub chọn chế độ
  loadCatalog().then(() => {
    openHub();
  }).catch((e) => {
    console.error("Lỗi nạp danh mục:", e);
    openHub();
  });
})();
