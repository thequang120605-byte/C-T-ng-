document.addEventListener("DOMContentLoaded", function () {
  const SVG_NS = "http://www.w3.org/2000/svg";

  const piecesLayer = document.getElementById("pieces-layer");

  const movesLayer = document.getElementById("moves-layer");

  const newGameButton = document.getElementById("new-game-btn");

  const resignButton = document.getElementById("resign-btn");

  const resultNewGameButton = document.getElementById("result-new-game-btn");

  const resultCloseButton = document.getElementById("result-close-btn");

  const confirmResignButton = document.getElementById("confirm-resign-btn");

  const cancelResignButton = document.getElementById("cancel-resign-btn");

  const gameResultModal = document.getElementById("game-result-modal");

  const resignModal = document.getElementById("resign-modal");

  // =====================================================
  // API
  // =====================================================

  const API_BASE_URL = "/api/games";

  // =====================================================
  // GAME
  // =====================================================

  let gameId = null;

  let isCreatingGame = false;

  let isMakingMove = false;

  // =====================================================
  // STATE
  // =====================================================

  let currentTurn = null;

  let gameStatus = null;

  // =====================================================
  // BOARD
  // =====================================================

  const START_X = 50;

  const START_Y = 50;

  const CELL_SIZE = 100;

  // =====================================================
  // SELECTED PIECE
  // =====================================================

  let selectedPieceElement = null;

  let selectedPieceData = null;

  // =====================================================
  // PIECES
  // =====================================================

  let initialPieces = [];

  // =====================================================
  // VALID MOVES
  // =====================================================

  let currentValidMoves = [];

  // =====================================================
  // INITIALIZE GAME
  // =====================================================

  async function initializeGame() {
    const savedGameId = localStorage.getItem("xiangqiGameId");

    if (savedGameId) {
      gameId = parseInt(savedGameId);

      console.log("Tìm thấy Game ID:", gameId);

      await loadGame();

      return;
    }

    await createGame();
  }

  // =====================================================
  // CREATE GAME
  // =====================================================

  async function createGame() {
    if (isCreatingGame) {
      return;
    }

    try {
      isCreatingGame = true;

      console.log("Đang tạo ván cờ mới...");

      const response = await fetch(API_BASE_URL, {
        method: "POST",

        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        throw new Error("Không thể tạo ván cờ");
      }

      const data = await response.json();

      console.log("Game mới:", data);

      gameId = data.id;

      localStorage.setItem("xiangqiGameId", gameId);

      updateGameFromApi(data);
    } catch (error) {
      console.error("Lỗi tạo game:", error);
    } finally {
      isCreatingGame = false;
    }
  }

  // =====================================================
  // LOAD GAME
  // =====================================================

  async function loadGame() {
    try {
      if (!gameId) {
        return;
      }

      const response = await fetch(`${API_BASE_URL}/${gameId}`);

      if (!response.ok) {
        localStorage.removeItem("xiangqiGameId");

        gameId = null;

        await createGame();

        return;
      }

      const data = await response.json();

      updateGameFromApi(data);
    } catch (error) {
      console.error("Lỗi load game:", error);
    }
  }

  // =====================================================
  // UPDATE GAME
  // =====================================================

  function updateGameFromApi(data) {
    gameId = data.id;

    currentTurn = data.currentTurn;

    gameStatus = data.status;

    initialPieces = data.boardPieces.map(convertPieceFromApi);

    selectedPieceElement = null;

    selectedPieceData = null;

    currentValidMoves = [];

    clearMoveHighlights();

    renderPieces();

    updateTurnStatus();

    updatePlayerStatus();

    console.log("Bàn cờ đã được cập nhật!");

    checkGameResult();
  }

  // =====================================================
  // CHECK GAME RESULT
  // =====================================================

  function checkGameResult() {
    if (gameStatus === "RedWins") {
      showGameResult(
        "🏆 CHIẾN THẮNG",
        "🔴 QUÂN ĐỎ",
        "Quân Đỏ đã giành chiến thắng!",
      );
    } else if (gameStatus === "BlackWins") {
      showGameResult(
        "🏆 CHIẾN THẮNG",
        "⚫ QUÂN ĐEN",
        "Quân Đen đã giành chiến thắng!",
      );
    }
  }

  // =====================================================
  // SHOW GAME RESULT
  // =====================================================

  function showGameResult(title, winner, message) {
    const resultTitle = document.getElementById("result-title");

    const winnerName = document.getElementById("winner-name");

    const resultMessage = document.getElementById("result-message");

    resultTitle.textContent = title;

    winnerName.textContent = winner;

    resultMessage.textContent = message;

    gameResultModal.classList.add("show");
  }

  // =====================================================
  // CLOSE GAME RESULT
  // =====================================================

  function closeGameResult() {
    gameResultModal.classList.remove("show");
  }

  // =====================================================
  // TURN STATUS
  // =====================================================

  function updateTurnStatus() {
    const turnStatus = document.getElementById("turn-status");

    if (!turnStatus) {
      console.warn("Không tìm thấy turn-status");

      return;
    }

    // =====================================
    // VÁN CỜ KẾT THÚC
    // =====================================

    if (gameStatus !== "InProgress") {
      if (gameStatus === "RedWins") {
        turnStatus.textContent = "🏆 QUÂN ĐỎ CHIẾN THẮNG!";
      } else if (gameStatus === "BlackWins") {
        turnStatus.textContent = "🏆 QUÂN ĐEN CHIẾN THẮNG!";
      } else {
        turnStatus.textContent = "Ván cờ đã kết thúc";
      }

      return;
    }

    // =====================================
    // LƯỢT QUÂN ĐỎ
    // =====================================

    if (currentTurn === "Red") {
      turnStatus.textContent = "🔴 Lượt của QUÂN ĐỎ";
    }

    // =====================================
    // LƯỢT QUÂN ĐEN
    // =====================================
    else if (currentTurn === "Black") {
      turnStatus.textContent = "⚫ Lượt của QUÂN ĐEN";
    }

    // =====================================
    // ANIMATION
    // =====================================

    turnStatus.classList.remove("turn-change");

    void turnStatus.offsetWidth;

    turnStatus.classList.add("turn-change");
  }

  // =====================================================
  // CONVERT PIECE
  // =====================================================

  function convertPieceFromApi(piece) {
    const color = piece.color.toLowerCase();

    let type = "";

    let text = "";

    if (piece.type === "Rook") {
      type = "Xe";

      text = "車";
    } else if (piece.type === "Horse") {
      type = "Mã";

      text = "馬";
    } else if (piece.type === "Elephant") {
      type = "Tượng";

      text = color === "red" ? "相" : "象";
    } else if (piece.type === "Advisor") {
      type = "Sĩ";

      text = color === "red" ? "仕" : "士";
    } else if (piece.type === "King") {
      type = "Tướng";

      text = color === "red" ? "帥" : "將";
    } else if (piece.type === "Cannon") {
      type = "Pháo";

      text = color === "red" ? "砲" : "炮";
    } else if (piece.type === "Pawn") {
      type = "Tốt";

      text = color === "red" ? "兵" : "卒";
    }

    return {
      type: type,

      text: text,

      color: color,

      row: piece.row,

      col: piece.col,
    };
  }

  // =====================================================
  // POSITION
  // =====================================================

  function getPosition(row, col) {
    return {
      x: START_X + col * CELL_SIZE,

      y: START_Y + row * CELL_SIZE,
    };
  }

  // =====================================================
  // CREATE PIECE
  // =====================================================

  function createPiece(piece) {
    const position = getPosition(piece.row, piece.col);

    const pieceGroup = document.createElementNS(SVG_NS, "g");

    pieceGroup.setAttribute("class", `chess-piece ${piece.color}`);

    pieceGroup.setAttribute("data-type", piece.type);

    pieceGroup.setAttribute("data-color", piece.color);

    pieceGroup.setAttribute("data-row", piece.row);

    pieceGroup.setAttribute("data-col", piece.col);

    // ===============================================
    // CIRCLE
    // ===============================================

    const circle = document.createElementNS(SVG_NS, "circle");

    circle.setAttribute("cx", position.x);

    circle.setAttribute("cy", position.y);

    circle.setAttribute("r", 42);

    circle.setAttribute("class", "piece-circle");

    // ===============================================
    // TEXT
    // ===============================================

    const textElement = document.createElementNS(SVG_NS, "text");

    textElement.setAttribute("x", position.x);

    textElement.setAttribute("y", position.y + 2);

    textElement.setAttribute("text-anchor", "middle");

    textElement.setAttribute("dominant-baseline", "middle");

    textElement.setAttribute("class", "piece-text");

    textElement.textContent = piece.text;

    pieceGroup.appendChild(circle);

    pieceGroup.appendChild(textElement);

    // ===============================================
    // CLICK
    // ===============================================

    pieceGroup.addEventListener("click", async function (event) {
      event.stopPropagation();

      await handlePieceClick(pieceGroup);
    });

    piecesLayer.appendChild(pieceGroup);
  }

  // =====================================================
  // RENDER PIECES
  // =====================================================

  function renderPieces() {
    piecesLayer.innerHTML = "";

    initialPieces.forEach(function (piece) {
      createPiece(piece);
    });
  }

  // =====================================================
  // CHECK VALID MOVE
  // =====================================================

  function isValidMove(row, col) {
    return currentValidMoves.some(function (move) {
      return move.row === row && move.col === col;
    });
  }

  // =====================================================
  // HANDLE PIECE CLICK
  // =====================================================

  async function handlePieceClick(pieceGroup) {
    try {
      const piece = getPieceData(pieceGroup);

      // =============================================
      // GAME ENDED
      // =============================================

      if (gameStatus !== "InProgress") {
        return;
      }

      // =============================================
      // CAPTURE
      // =============================================

      if (selectedPieceData !== null) {
        if (
          piece.color.toLowerCase() !== selectedPieceData.color.toLowerCase()
        ) {
          const canCapture = isValidMove(piece.row, piece.col);

          if (canCapture) {
            await makeMove(piece.row, piece.col);

            return;
          }
        }

        // CLICK SAME PIECE

        if (selectedPieceElement === pieceGroup) {
          pieceGroup.classList.remove("selected");

          selectedPieceElement = null;

          selectedPieceData = null;

          currentValidMoves = [];

          clearMoveHighlights();

          return;
        }
      }

      // =============================================
      // CHECK TURN
      // =============================================

      if (piece.color.toLowerCase() !== currentTurn.toLowerCase()) {
        console.log("Chưa tới lượt quân này");

        return;
      }

      // =============================================
      // REMOVE OLD
      // =============================================

      if (selectedPieceElement !== null) {
        selectedPieceElement.classList.remove("selected");
      }

      clearMoveHighlights();

      // =============================================
      // SELECT
      // =============================================

      pieceGroup.classList.add("selected");

      selectedPieceElement = pieceGroup;

      selectedPieceData = piece;

      // =============================================
      // GET MOVES
      // =============================================

      await fetchValidMoves(piece.row, piece.col);
    } catch (error) {
      console.error("Lỗi chọn quân:", error);
    }
  }

  // =====================================================
  // FETCH VALID MOVES
  // =====================================================

  async function fetchValidMoves(row, col) {
    try {
      const url = `${API_BASE_URL}/${gameId}/valid-moves?row=${row}&col=${col}`;

      const response = await fetch(url);

      if (!response.ok) {
        throw new Error("Không thể lấy nước đi");
      }

      const data = await response.json();

      currentValidMoves = data;

      showValidMovesFromApi(data);
    } catch (error) {
      console.error("Lỗi valid moves:", error);

      clearMoveHighlights();
    }
  }

  // =====================================================
  // CLEAR HIGHLIGHT
  // =====================================================

  function clearMoveHighlights() {
    movesLayer.innerHTML = "";
  }

  // =====================================================
  // SHOW VALID MOVES
  // =====================================================

  function showValidMovesFromApi(moves) {
    clearMoveHighlights();

    moves.forEach(function (move) {
      highlightMove(move.row, move.col, move.isCapture);
    });
  }

  // =====================================================
  // HIGHLIGHT
  // =====================================================

  function highlightMove(row, col, isCapture) {
    const position = getPosition(row, col);

    const moveCircle = document.createElementNS(SVG_NS, "circle");

    moveCircle.setAttribute("cx", position.x);

    moveCircle.setAttribute("cy", position.y);

    moveCircle.setAttribute("r", isCapture ? 25 : 15);

    moveCircle.setAttribute(
      "class",

      isCapture ? "valid-move capture-move" : "valid-move",
    );

    moveCircle.addEventListener("click", async function (event) {
      event.stopPropagation();

      await makeMove(row, col);
    });

    movesLayer.appendChild(moveCircle);
  }

  // =====================================================
  // MAKE MOVE
  // =====================================================

  async function makeMove(toRow, toCol) {
    if (isMakingMove) {
      return;
    }

    try {
      if (!selectedPieceData) {
        return;
      }

      isMakingMove = true;

      const requestData = {
        fromX: selectedPieceData.row,

        fromY: selectedPieceData.col,

        toX: toRow,

        toY: toCol,
      };

      const response = await fetch(`${API_BASE_URL}/${gameId}/moves`, {
        method: "POST",

        headers: {
          "Content-Type": "application/json",
        },

        body: JSON.stringify(requestData),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(errorData?.message || "Không thể thực hiện nước đi");
      }

      const data = await response.json();

      updateGameFromApi(data);
    } catch (error) {
      console.error("Lỗi di chuyển:", error);
    } finally {
      isMakingMove = false;
    }
  }

  // =====================================================
  // NEW GAME
  // =====================================================

  async function startNewGame() {
    try {
      console.log("Bắt đầu ván mới");

      // CLOSE MODAL

      closeGameResult();

      resignModal.classList.remove("show");

      // REMOVE OLD GAME

      localStorage.removeItem("xiangqiGameId");

      gameId = null;

      // RESET

      selectedPieceElement = null;

      selectedPieceData = null;

      currentValidMoves = [];

      clearMoveHighlights();

      piecesLayer.innerHTML = "";

      // CREATE

      await createGame();
    } catch (error) {
      console.error("Lỗi tạo ván mới:", error);
    }
  }

  // =====================================================
  // SHOW RESIGN MODAL
  // =====================================================

  function showResignModal() {
    if (gameStatus !== "InProgress") {
      return;
    }

    resignModal.classList.add("show");
  }

  // =====================================================
  // CLOSE RESIGN MODAL
  // =====================================================

  function closeResignModal() {
    resignModal.classList.remove("show");
  }

  // =====================================================
  // RESIGN GAME
  // =====================================================

  async function resignGame() {
    try {
      if (!gameId) {
        return;
      }

      const response = await fetch(`${API_BASE_URL}/${gameId}/resign`, {
        method: "POST",
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(errorData?.message || "Không thể đầu hàng");
      }

      const data = await response.json();

      closeResignModal();

      updateGameFromApi(data);
    } catch (error) {
      console.error("Lỗi đầu hàng:", error);

      alert(error.message);
    }
  }

  // =====================================================
  // GET PIECE DATA
  // =====================================================

  function getPieceData(pieceGroup) {
    return {
      type: pieceGroup.getAttribute("data-type"),

      color: pieceGroup.getAttribute("data-color"),

      row: parseInt(pieceGroup.getAttribute("data-row")),

      col: parseInt(pieceGroup.getAttribute("data-col")),
    };
  }

  // =====================================================
  // CLICK OUTSIDE
  // =====================================================

  document.addEventListener("click", function () {
    if (selectedPieceElement !== null) {
      selectedPieceElement.classList.remove("selected");

      selectedPieceElement = null;

      selectedPieceData = null;

      currentValidMoves = [];

      clearMoveHighlights();
    }
  });

  // =====================================================
  // BUTTON EVENTS
  // =====================================================

  if (newGameButton) {
    newGameButton.addEventListener("click", async function () {
      await startNewGame();
    });
  }

  if (resignButton) {
    resignButton.addEventListener("click", function () {
      showResignModal();
    });
  }

  if (resultNewGameButton) {
    resultNewGameButton.addEventListener("click", async function () {
      await startNewGame();
    });
  }

  if (resultCloseButton) {
    resultCloseButton.addEventListener("click", function () {
      closeGameResult();
    });
  }

  if (confirmResignButton) {
    confirmResignButton.addEventListener("click", async function () {
      await resignGame();
    });
  }

  if (cancelResignButton) {
    cancelResignButton.addEventListener("click", function () {
      closeResignModal();
    });
  }

  function updatePlayerStatus() {
    const redPlayerStatus = document.getElementById("red-player-status");

    const blackPlayerStatus = document.getElementById("black-player-status");

    if (!redPlayerStatus || !blackPlayerStatus) {
      return;
    }

    // ===============================================
    // GAME KẾT THÚC
    // ===============================================

    if (gameStatus === "RedWins") {
      redPlayerStatus.textContent = "🏆 Chiến thắng";

      blackPlayerStatus.textContent = "❌ Thất bại";

      return;
    }

    if (gameStatus === "BlackWins") {
      redPlayerStatus.textContent = "❌ Thất bại";

      blackPlayerStatus.textContent = "🏆 Chiến thắng";

      return;
    }

    // ===============================================
    // LƯỢT QUÂN ĐỎ
    // ===============================================

    if (currentTurn === "Red") {
      redPlayerStatus.textContent = "🟢 Đang đi";

      blackPlayerStatus.textContent = "⚪ Đang chờ";
    }

    // ===============================================
    // LƯỢT QUÂN ĐEN
    // ===============================================
    else if (currentTurn === "Black") {
      redPlayerStatus.textContent = "⚪ Đang chờ";

      blackPlayerStatus.textContent = "🟢 Đang đi";
    }
  }

  // =====================================================
  // START
  // =====================================================

  initializeGame();
});
