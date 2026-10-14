/* Presentation layer only. Game/API/SignalR rules remain in game.js.
 * Rebuild captured pieces from the initial FEN + history, so undo/reload works.
 * Scores are raw heuristic points; meters are not winning probabilities.
 */
(function (global) {
  "use strict";
  const names = { King: "Tướng", Advisor: "Sĩ", Elephant: "Tượng", Horse: "Mã", Rook: "Xe", Cannon: "Pháo", Pawn: "Tốt" };
  const glyphs = {
    Red: { King: "帥", Advisor: "仕", Elephant: "相", Horse: "傌", Rook: "俥", Cannon: "炮", Pawn: "兵" },
    Black: { King: "將", Advisor: "士", Elephant: "象", Horse: "馬", Rook: "車", Cannon: "砲", Pawn: "卒" }
  };
  const initialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w";
  const typeByFen = { k: "King", a: "Advisor", b: "Elephant", e: "Elephant", n: "Horse", h: "Horse", r: "Rook", c: "Cannon", p: "Pawn" };
  const byId = id => document.getElementById(id);
  const setText = (id, value) => { const el = byId(id); if (el) el.textContent = value; };
  const signed = n => Number.isFinite(n) ? (n > 0 ? `+${n}` : String(n)) : "—";
  const ordered = moves => [...(moves || [])].sort((a, b) => a.moveOrder - b.moveOrder);
  const square = (row, col) => `${String.fromCharCode(97 + Number(col))}${10 - Number(row)}`;
  const notation = m => `${names[m.pieceType] || "Quân"} ${square(m.fromX, m.fromY)} → ${square(m.toX, m.toY)}`;
  const percent = (score, scale) => Math.max(5, Math.min(95, 50 + Math.tanh(score / scale) * 45));
  let historySignature = "";

  function setMeter(id, score, scale) {
    const el = byId(id);
    if (!el) return;
    const value = Number.isFinite(score) ? percent(score, scale) : 50;
    el.setAttribute("aria-valuenow", value.toFixed(0));
    el.setAttribute("aria-valuetext", Number.isFinite(score) ? `Ưu thế Đỏ ${signed(score)} điểm; thanh không phải xác suất thắng` : "Chưa có dữ liệu đánh giá");
    const fill = el.querySelector("span");
    if (fill) fill.style.width = `${value}%`;
  }

  function renderEvaluation(data) {
    const score = data.redScore;
    const label = !Number.isFinite(score) ? "Chưa có dữ liệu" : Math.abs(score) < 30 ? "Thế trận cân bằng" : score > 0 ? "Đỏ đang có ưu thế" : "Đen đang có ưu thế";
    setText("advantage-label", label);
    setText("advantage-score", Number.isFinite(score) ? `${score > 0 ? "+" : ""}${(score / 100).toFixed(2)}` : "—");
    setText("red-score", signed(score));
    setText("black-score", signed(data.blackScore));
    setMeter("advantage-meter", score, 650);
    const components = data.evaluation || {};
    for (const [key, scale] of [["h1", 700], ["h2", 200], ["h3", 250]]) {
      setText(`${key}-value`, signed(components[key]));
      setMeter(`${key}-meter`, components[key], scale);
    }
    const lm = data.lastMoveEval;
    const box = byId("last-move-box");
    if (box) box.hidden = !lm;
    const toggle = byId("last-move-toggle");
    if (toggle) toggle.disabled = !lm;
    if (!lm) { box?.classList.remove("is-open"); toggle?.setAttribute("aria-expanded", "false"); }
    if (!lm) return;
    const last = ordered(data.moves).at(-1);
    setText("lm-piece", last ? `${last.pieceColor === "Red" ? "Đỏ" : "Đen"} · ${names[last.pieceType] || "Quân"}` : names[lm.pieceName] || lm.pieceName);
    setText("lm-desc", last ? `${square(last.fromX, last.fromY)} → ${square(last.toX, last.toY)}` : lm.moveDescription);
    for (const key of ["h1", "h2", "h3"]) setText(`lm-${key}`, signed(lm[`delta${key.toUpperCase()}`]));
    setText("lm-total", signed(lm.totalDelta));
  }

  function renderHistory(moves) {
    const container = byId("move-history-container");
    if (!container) return;
    const list = ordered(moves);
    setText("move-count", `${list.length} nước`);
    const signature = JSON.stringify(list.map(m => [m.moveOrder, m.pieceColor, m.pieceType, m.fromX, m.fromY, m.toX, m.toY]));
    if (signature === historySignature) return; // Do not reset scroll on every online poll.
    const wasAtBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 40;
    historySignature = signature;
    container.replaceChildren();
    if (!list.length) {
      const empty = document.createElement("div"); empty.className = "empty-history";
      empty.textContent = "Chưa có nước đi.";
      const tip = document.createElement("small"); tip.textContent = "Hành trình bắt đầu từ nước đầu tiên.";
      empty.append(tip); container.append(empty); return;
    }
    // Pair by side rather than array parity: imported FEN can start with Black.
    const rounds = [];
    for (const move of list) {
      let round = rounds.at(-1);
      if (!round || move.pieceColor === "Red" || round.black) { round = {}; rounds.push(round); }
      round[move.pieceColor === "Red" ? "red" : "black"] = move;
    }
    rounds.forEach((round, index) => {
      const row = document.createElement("div"); row.className = "history-row";
      const number = document.createElement("span"); number.className = "history-number"; number.textContent = `${index + 1}`; row.append(number);
      for (const side of ["red", "black"]) {
        const move = round[side], cell = document.createElement("span");
        cell.className = `history-move history-${side}`;
        cell.textContent = move ? notation(move) : "—";
        if (move) {
          cell.title = `${side === "red" ? "Đỏ" : "Đen"}: ${notation(move)}; tọa độ gốc (${move.fromX},${move.fromY}) → (${move.toX},${move.toY})`;
          if (move === list.at(-1)) { cell.classList.add("is-latest"); cell.setAttribute("aria-current", "step"); }
        }
        row.append(cell);
      }
      container.append(row);
    });
    if (wasAtBottom) container.scrollTop = container.scrollHeight;
  }

  function capturedFromHistory(data) {
    const grid = Array.from({ length: 10 }, () => Array(9).fill(null));
    const ranks = (data.initialFen || initialFen).trim().split(/\s+/)[0].split("/");
    if (ranks.length !== 10) return { Red: [], Black: [] };
    ranks.forEach((rank, row) => {
      let col = 0;
      for (const ch of rank) {
        if (/\d/.test(ch)) col += Number(ch);
        else { const type = typeByFen[ch.toLowerCase()]; if (type && col < 9) grid[row][col] = { type, color: ch === ch.toUpperCase() ? "Red" : "Black" }; col++; }
      }
    });
    const captured = { Red: [], Black: [] };
    for (const move of ordered(data.moves)) {
      if (![move.fromX, move.fromY, move.toX, move.toY].every(Number.isInteger)) continue;
      if (move.fromX < 0 || move.fromX > 9 || move.toX < 0 || move.toX > 9 || move.fromY < 0 || move.fromY > 8 || move.toY < 0 || move.toY > 8) continue;
      const piece = grid[move.fromX][move.fromY], victim = grid[move.toX][move.toY];
      if (!piece) continue;
      if (victim && victim.color !== piece.color) captured[piece.color].push(victim);
      grid[move.toX][move.toY] = piece; grid[move.fromX][move.fromY] = null;
    }
    return captured;
  }

  function renderCaptured(data) {
    const captured = capturedFromHistory(data);
    for (const color of ["Red", "Black"]) {
      const container = byId(`${color.toLowerCase()}-captured-pieces`);
      if (!container) continue;
      container.replaceChildren();
      if (!captured[color].length) {
        const empty = document.createElement("span"); empty.className = "capture-empty"; empty.textContent = "Chưa có"; container.append(empty); continue;
      }
      // Group repeated captures, keeping the tray compact even in endgames.
      const groups = new Map();
      captured[color].forEach(p => { const key = p.color + p.type; const group = groups.get(key) || { ...p, count: 0 }; group.count++; groups.set(key, group); });
      for (const piece of groups.values()) {
        const chip = document.createElement("span"); chip.className = `captured-piece captured-${piece.color.toLowerCase()}`;
        chip.textContent = glyphs[piece.color][piece.type];
        chip.setAttribute("aria-label", `${piece.count} ${names[piece.type]} ${piece.color === "Red" ? "Đỏ" : "Đen"} bị ăn`);
        chip.title = chip.getAttribute("aria-label");
        if (piece.count > 1) { const count = document.createElement("sup"); count.textContent = piece.count; chip.append(count); }
        container.append(chip);
      }
    }
  }

  function updatePlayerState(state) {
    const active = state.status === "InProgress";
    const waiting = state.status === "WaitingForPlayers";
    const mode = state.online ? "Đấu trực tuyến" : state.computer ? "Đấu luyện với máy" : "Đấu hai người";
    setText("arena-mode-label", mode);
    for (const color of ["Red", "Black"]) {
      const current = active && state.turn === color;
      const card = byId(`${color.toLowerCase()}-player-card`);
      if (card) { card.classList.toggle("is-active", current); card.classList.toggle("is-check", current && state.check); }
      const text = waiting ? "Đang chờ đối thủ" : state.status === "Draw" ? "Ván cờ hòa" : !active ? (state.status === `${color}Wins` ? "Chiến thắng" : "Ván cờ kết thúc") : current ? (state.check ? "Đang bị chiếu" : state.thinking && color === "Black" ? "Máy đang suy nghĩ…" : "Đang đến lượt") : "Đang chờ";
      setText(`${color.toLowerCase()}-player-status`, text);
    }
    const hint = document.querySelector(".action-hint");
    if (hint) hint.textContent = active ? (state.check ? "Cần thoát chiếu" : "Sẵn sàng nước tiếp theo") : waiting ? "Đang chờ" : "Ván đã kết thúc";
    const indicator = byId("arena-turn-indicator");
    if (indicator) { indicator.classList.toggle("turn-red", state.turn === "Red"); indicator.classList.toggle("turn-black", state.turn === "Black"); }
  }

  const lastMoveToggle = byId("last-move-toggle");
  lastMoveToggle?.addEventListener("click", () => {
    const open = lastMoveToggle.getAttribute("aria-expanded") !== "true";
    lastMoveToggle.setAttribute("aria-expanded", String(open));
    byId("last-move-box")?.classList.toggle("is-open", open);
  });
  document.addEventListener("keydown", event => {
    if (event.key === "Escape") {
      byId("last-move-box")?.classList.remove("is-open");
      lastMoveToggle?.setAttribute("aria-expanded", "false");
    }
  });

  global.ArenaUI = Object.freeze({
    update(data) { renderEvaluation(data); renderCaptured(data); },
    renderHistory, updatePlayerState,
    // Pure helpers also support the local UI regression checks.
    capturedFromHistory, notation, percent
  });
})(window);
