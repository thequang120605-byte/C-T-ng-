document.addEventListener("DOMContentLoaded", function () {
  const SVG_NS = "http://www.w3.org/2000/svg";

  // =====================================================
  // API
  // =====================================================

  const API_BASE_URL = "/api/games";

  // =====================================================
  // MODE
  // =====================================================

  let VS_COMPUTER = false;
  let gameModeSelected = false;

  const COMPUTER_DIFFICULTY = 2;

  // =====================================================
  // DOM
  // =====================================================

  const modeSelection = document.getElementById("mode-selection");

  const gameContent = document.getElementById("game-content");

  const playerVsPlayerButton = document.getElementById("player-vs-player-btn");

  const playerVsComputerButton = document.getElementById(
    "player-vs-computer-btn",
  );

  const startGameButton = document.getElementById("start-game-btn");

  const selectedModeText = document.getElementById("selected-mode-text");

  const changeModeButton = document.getElementById("change-mode-btn");

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
  // GAME STATE
  // =====================================================

  let gameId = null;
  let isCreatingGame = false;
  let isMakingMove = false;
  let isComputerThinking = false;

  let currentTurn = null;
  let gameStatus = null;
  let isCheck = false;

  // =====================================================
  // BOARD
  // =====================================================

  const START_X = 50;
  const START_Y = 50;
  const CELL_SIZE = 100;

  // =====================================================
  // PIECES
  // =====================================================

  let selectedPieceElement = null;
  let selectedPieceData = null;

  let initialPieces = [];
  let currentValidMoves = [];

  // =====================================================
  // SELECT MODE
  // =====================================================

  function selectGameMode(isComputer) {
    VS_COMPUTER = isComputer;
    gameModeSelected = true;

    if (playerVsPlayerButton) {
      playerVsPlayerButton.classList.remove("selected");
      playerVsPlayerButton.setAttribute("aria-pressed", "false");
    }

    if (playerVsComputerButton) {
      playerVsComputerButton.classList.remove("selected");
      playerVsComputerButton.setAttribute("aria-pressed", "false");
    }

    if (VS_COMPUTER) {
      if (playerVsComputerButton) {
        playerVsComputerButton.classList.add("selected");
        playerVsComputerButton.setAttribute("aria-pressed", "true");
      }

      if (selectedModeText) {
        selectedModeText.textContent = "🤖 Người chơi vs Máy";
      }
    } else {
      if (playerVsPlayerButton) {
        playerVsPlayerButton.classList.add("selected");
        playerVsPlayerButton.setAttribute("aria-pressed", "true");
      }

      if (selectedModeText) {
        selectedModeText.textContent = "👥 Người chơi vs Người chơi";
      }
    }

    if (startGameButton) {
      startGameButton.disabled = false;
      startGameButton.classList.add("enabled");
    }
  }

  // =====================================================
  // START SELECTED GAME
  // =====================================================

  async function startSelectedGame() {
    if (!gameModeSelected) {
      return;
    }

    closeGameResult();
    closeResignModal();

    localStorage.removeItem("xiangqiGameId");

    gameId = null;

    currentTurn = null;
    gameStatus = null;
    isCheck = false;
    isComputerThinking = false;

    clearSelection();

    if (modeSelection) {
      modeSelection.classList.add("hidden");
    }

    if (gameContent) {
      gameContent.classList.remove("hidden");
    }

    updatePlayerNames();

    await createGame();
  }

  // =====================================================
  // BACK TO MODE SELECTION
  // =====================================================

  function backToModeSelection() {
    closeGameResult();
    closeResignModal();

    clearSelection();

    if (piecesLayer) {
      piecesLayer.innerHTML = "";
    }

    if (modeSelection) {
      modeSelection.classList.remove("hidden");
    }

    if (gameContent) {
      gameContent.classList.add("hidden");
    }

    gameModeSelected = false;
    VS_COMPUTER = false;

    gameId = null;
    currentTurn = null;
    gameStatus = null;
    isCheck = false;
    isComputerThinking = false;

    localStorage.removeItem("xiangqiGameId");

    if (playerVsPlayerButton) {
      playerVsPlayerButton.classList.remove("selected");
      playerVsPlayerButton.setAttribute("aria-pressed", "false");
    }

    if (playerVsComputerButton) {
      playerVsComputerButton.classList.remove("selected");
      playerVsComputerButton.setAttribute("aria-pressed", "false");
    }

    if (startGameButton) {
      startGameButton.disabled = true;
      startGameButton.classList.remove("enabled");
    }

    if (selectedModeText) {
      selectedModeText.textContent = "Chưa chọn chế độ chơi";
    }
  }

  // =====================================================
  // INITIALIZE PAGE
  // =====================================================

  function initializePage() {
    if (modeSelection) {
      modeSelection.classList.remove("hidden");
    }

    if (gameContent) {
      gameContent.classList.add("hidden");
    }

    if (startGameButton) {
      startGameButton.disabled = true;
      startGameButton.classList.remove("enabled");
    }

    if (selectedModeText) {
      selectedModeText.textContent = "Chưa chọn chế độ chơi";
    }

    localStorage.removeItem("xiangqiGameId");

    console.log("Đang chờ người dùng chọn chế độ chơi...");
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

      alert("Không thể tạo ván cờ. Vui lòng thử lại.");
    } finally {
      isCreatingGame = false;
    }
  }

  // =====================================================
  // LOAD GAME
  // =====================================================

  async function loadGame() {
    if (!gameId) {
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/${gameId}`);

      if (!response.ok) {
        localStorage.removeItem("xiangqiGameId");

        gameId = null;

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
    if (!data) {
      return;
    }

    gameId = data.id;

    currentTurn = data.currentTurn;

    gameStatus = data.status;

    isCheck = data.isCheck || false;

    initialPieces = (data.boardPieces || []).map(convertPieceFromApi);

    selectedPieceElement = null;
    selectedPieceData = null;

    currentValidMoves = [];

    clearMoveHighlights();

    renderPieces();

    updatePlayerNames();

    updateTurnStatus();

    updatePlayerStatus();

    handleCheckWarning();

    updateComputerStatus();

    checkGameResult();

    console.log("Bàn cờ đã được cập nhật!");

    if (VS_COMPUTER && gameStatus === "InProgress" && currentTurn === "Black") {
      triggerComputerMove();
    }
  }

  // =====================================================
  // PLAYER NAMES
  // =====================================================

  function updatePlayerNames() {
    const redPlayerName = document.getElementById("red-player-name");

    const blackPlayerName = document.getElementById("black-player-name");

    if (redPlayerName) {
      redPlayerName.textContent = VS_COMPUTER ? "Bạn" : "Người chơi 1";
    }

    if (blackPlayerName) {
      blackPlayerName.textContent = VS_COMPUTER ? "Máy" : "Người chơi 2";
    }
  }

  // =====================================================
  // COMPUTER STATUS
  // =====================================================

  function updateComputerStatus() {
    const blackStatus = document.getElementById("black-player-status");

    if (!blackStatus) {
      return;
    }

    if (!VS_COMPUTER) {
      return;
    }

    if (gameStatus === "RedWins") {
      blackStatus.textContent = "❌ Máy thua";

      return;
    }

    if (gameStatus === "BlackWins") {
      blackStatus.textContent = "🏆 Máy chiến thắng";

      return;
    }

    if (gameStatus !== "InProgress") {
      blackStatus.textContent = "⚪ Ván cờ kết thúc";

      return;
    }

    if (isComputerThinking) {
      blackStatus.textContent = "🤖 Máy đang suy nghĩ...";

      return;
    }

    if (currentTurn === "Black") {
      blackStatus.textContent = "🤖 Đến lượt máy";

      return;
    }

    blackStatus.textContent = "🤖 Máy đang chờ";
  }

  // =====================================================
  // COMPUTER MOVE
  // =====================================================

  async function triggerComputerMove() {
    if (isComputerThinking) {
      return;
    }

    if (gameStatus !== "InProgress") {
      return;
    }

    if (currentTurn !== "Black") {
      return;
    }

    if (!VS_COMPUTER) {
      return;
    }

    try {
      isComputerThinking = true;

      clearSelection();

      updateComputerStatus();
      updateTurnStatus();

      console.log("🤖 Máy đang tính nước đi...");

      const response = await fetch(
        `${API_BASE_URL}/${gameId}/computer-move?difficulty=${COMPUTER_DIFFICULTY}`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
        },
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(
          errorData?.message || "Máy không thể thực hiện nước đi",
        );
      }

      const data = await response.json();

      console.log("🤖 Máy đã đi:", data);

      updateGameFromApi(data);
    } catch (error) {
      console.error("Lỗi máy đi:", error);

      alert(error.message);
    } finally {
      isComputerThinking = false;

      updateComputerStatus();
      updateTurnStatus();
    }
  }

  // =====================================================
  // CHECK WARNING
  // =====================================================

  function handleCheckWarning() {
    document
      .querySelectorAll(".chess-piece.in-check")
      .forEach(function (element) {
        element.classList.remove("in-check");
      });

    const overlay = document.getElementById("check-overlay");

    if (overlay) {
      overlay.classList.remove("show");
    }

    if (isCheck && gameStatus === "InProgress" && currentTurn) {
      const activeColor = currentTurn.toLowerCase();

      const kingElement = document.querySelector(
        `.chess-piece[data-type="Tướng"][data-color="${activeColor}"]`,
      );

      if (kingElement) {
        kingElement.classList.add("in-check");
      }

      if (overlay) {
        overlay.classList.add("show");

        setTimeout(function () {
          overlay.classList.remove("show");
        }, 1200);
      }
    }
  }

  // =====================================================
  // CHECK RESULT
  // =====================================================

  function checkGameResult() {
    if (gameStatus === "RedWins") {
      showGameResult(
        "🏆 CHIẾN THẮNG",
        "🔴 QUÂN ĐỎ",
        VS_COMPUTER ? "Bạn đã đánh bại máy!" : "Quân Đỏ đã giành chiến thắng!",
      );

      return;
    }

    if (gameStatus === "BlackWins") {
      showGameResult(
        VS_COMPUTER ? "😔 THẤT BẠI" : "🏆 CHIẾN THẮNG",
        "⚫ QUÂN ĐEN",
        VS_COMPUTER
          ? "Máy đã giành chiến thắng!"
          : "Quân Đen đã giành chiến thắng!",
      );
    }
  }

  // =====================================================
  // SHOW RESULT
  // =====================================================

  function showGameResult(title, winner, message) {
    if (!gameResultModal) {
      return;
    }

    const resultTitle = document.getElementById("result-title");

    const winnerName = document.getElementById("winner-name");

    const resultMessage = document.getElementById("result-message");

    if (!resultTitle || !winnerName || !resultMessage) {
      return;
    }

    resultTitle.textContent = title;

    winnerName.textContent = winner;

    resultMessage.textContent = message;

    gameResultModal.classList.add("show");
  }

  // =====================================================
  // CLOSE RESULT
  // =====================================================

  function closeGameResult() {
    if (!gameResultModal) {
      return;
    }

    gameResultModal.classList.remove("show");
  }

  // =====================================================
  // TURN STATUS
  // =====================================================

  function updateTurnStatus() {
    const turnStatus = document.getElementById("turn-status");

    const turnIndicator = document.getElementById("turn-indicator");

    if (!turnStatus) {
      return;
    }

    if (gameStatus === "RedWins") {
      turnStatus.textContent = VS_COMPUTER
        ? "🏆 BẠN CHIẾN THẮNG"
        : "🏆 QUÂN ĐỎ CHIẾN THẮNG";

      if (turnIndicator) {
        turnIndicator.textContent = "🏆";
      }

      return;
    }

    if (gameStatus === "BlackWins") {
      turnStatus.textContent = VS_COMPUTER
        ? "🤖 MÁY CHIẾN THẮNG"
        : "🏆 QUÂN ĐEN CHIẾN THẮNG";

      if (turnIndicator) {
        turnIndicator.textContent = "🏆";
      }

      return;
    }

    if (isCheck) {
      turnStatus.textContent =
        currentTurn === "Red"
          ? "⚠️ CHIẾU TƯỚNG! (Lượt QUÂN ĐỎ)"
          : "⚠️ CHIẾU TƯỚNG! (Lượt QUÂN ĐEN)";

      if (turnIndicator) {
        turnIndicator.textContent = "⚠️";
      }

      return;
    }

    if (currentTurn === "Red") {
      turnStatus.textContent = VS_COMPUTER
        ? "🔴 Lượt của BẠN"
        : "🔴 Lượt của QUÂN ĐỎ";

      if (turnIndicator) {
        turnIndicator.textContent = "🔴";
      }

      return;
    }

    if (currentTurn === "Black") {
      turnStatus.textContent = VS_COMPUTER
        ? "🤖 Máy đang suy nghĩ..."
        : "⚫ Lượt của QUÂN ĐEN";

      if (turnIndicator) {
        turnIndicator.textContent = VS_COMPUTER ? "🤖" : "⚫";
      }
    }
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
    if (!piecesLayer) {
      return;
    }

    const position = getPosition(piece.row, piece.col);

    const pieceGroup = document.createElementNS(SVG_NS, "g");

    pieceGroup.setAttribute("class", `chess-piece ${piece.color}`);

    pieceGroup.setAttribute("data-type", piece.type);

    pieceGroup.setAttribute("data-color", piece.color);

    pieceGroup.setAttribute("data-row", piece.row);

    pieceGroup.setAttribute("data-col", piece.col);

    const circle = document.createElementNS(SVG_NS, "circle");

    circle.setAttribute("cx", position.x);

    circle.setAttribute("cy", position.y);

    circle.setAttribute("r", 42);

    circle.setAttribute("class", "piece-circle");

    const textElement = document.createElementNS(SVG_NS, "text");

    textElement.setAttribute("x", position.x);

    textElement.setAttribute("y", position.y + 2);

    textElement.setAttribute("text-anchor", "middle");

    textElement.setAttribute("dominant-baseline", "middle");

    textElement.setAttribute("class", "piece-text");

    textElement.textContent = piece.text;

    pieceGroup.appendChild(circle);

    pieceGroup.appendChild(textElement);

    pieceGroup.addEventListener("click", async function (event) {
      event.stopPropagation();

      await handlePieceClick(pieceGroup);
    });

    piecesLayer.appendChild(pieceGroup);
  }

  // =====================================================
  // RENDER
  // =====================================================

  function renderPieces() {
    if (!piecesLayer) {
      return;
    }

    piecesLayer.innerHTML = "";

    initialPieces.forEach(function (piece) {
      createPiece(piece);
    });
  }

  // =====================================================
  // VALID MOVE
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
      if (isComputerThinking) {
        return;
      }

      if (gameStatus !== "InProgress") {
        return;
      }

      if (VS_COMPUTER && currentTurn !== "Red") {
        return;
      }

      const piece = getPieceData(pieceGroup);

      // =========================================
      // CAPTURE
      // =========================================

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

        if (selectedPieceElement === pieceGroup) {
          clearSelection();

          return;
        }
      }

      // =========================================
      // TURN
      // =========================================

      if (piece.color.toLowerCase() !== currentTurn.toLowerCase()) {
        console.log("Chưa tới lượt quân này");

        return;
      }

      // =========================================
      // REMOVE OLD
      // =========================================

      if (selectedPieceElement) {
        selectedPieceElement.classList.remove("selected");
      }

      clearMoveHighlights();

      // =========================================
      // SELECT
      // =========================================

      pieceGroup.classList.add("selected");

      selectedPieceElement = pieceGroup;

      selectedPieceData = piece;

      // =========================================
      // GET MOVES
      // =========================================

      await fetchValidMoves(piece.row, piece.col);
    } catch (error) {
      console.error("Lỗi chọn quân:", error);
    }
  }

  // =====================================================
  // FETCH VALID MOVES
  // =====================================================

  async function fetchValidMoves(row, col) {
    if (!gameId) {
      return;
    }

    try {
      const url = `${API_BASE_URL}/${gameId}/valid-moves?row=${row}&col=${col}`;

      const response = await fetch(url);

      if (!response.ok) {
        throw new Error("Không thể lấy nước đi");
      }

      const data = await response.json();

      currentValidMoves = data || [];

      showValidMovesFromApi(currentValidMoves);
    } catch (error) {
      console.error("Lỗi valid moves:", error);

      clearMoveHighlights();
    }
  }

  // =====================================================
  // CLEAR MOVE HIGHLIGHTS
  // =====================================================

  function clearMoveHighlights() {
    if (movesLayer) {
      movesLayer.innerHTML = "";
    }
  }

  // =====================================================
  // CLEAR SELECTION
  // =====================================================

  function clearSelection() {
    if (selectedPieceElement) {
      selectedPieceElement.classList.remove("selected");
    }

    selectedPieceElement = null;

    selectedPieceData = null;

    currentValidMoves = [];

    clearMoveHighlights();
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
  // HIGHLIGHT MOVE
  // =====================================================

  function highlightMove(row, col, isCapture) {
    if (!movesLayer) {
      return;
    }

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

    if (isComputerThinking) {
      return;
    }

    if (!selectedPieceData) {
      return;
    }

    if (!gameId) {
      return;
    }

    try {
      if (VS_COMPUTER && selectedPieceData.color !== "red") {
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

      alert(error.message);
    } finally {
      isMakingMove = false;
    }
  }

  // =====================================================
  // NEW GAME
  // =====================================================

  async function startNewGame() {
    if (!gameModeSelected) {
      return;
    }

    try {
      closeGameResult();
      closeResignModal();

      localStorage.removeItem("xiangqiGameId");

      gameId = null;

      currentTurn = null;
      gameStatus = null;
      isCheck = false;
      isComputerThinking = false;

      clearSelection();

      if (piecesLayer) {
        piecesLayer.innerHTML = "";
      }

      await createGame();
    } catch (error) {
      console.error("Lỗi tạo ván mới:", error);
    }
  }

  // =====================================================
  // RESIGN MODAL
  // =====================================================

  function showResignModal() {
    if (gameStatus !== "InProgress") {
      return;
    }

    if (resignModal) {
      resignModal.classList.add("show");
    }
  }

  function closeResignModal() {
    if (resignModal) {
      resignModal.classList.remove("show");
    }
  }

  // =====================================================
  // RESIGN GAME
  // =====================================================

  async function resignGame() {
    if (!gameId) {
      return;
    }

    if (gameStatus !== "InProgress") {
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/${gameId}/resign`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(
          errorData?.message || `Không thể đầu hàng. HTTP ${response.status}`,
        );
      }

      const data = await response.json();

      console.log("Đầu hàng thành công:", data);

      closeResignModal();

      clearSelection();

      isComputerThinking = false;

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
    if (isComputerThinking) {
      return;
    }

    if (selectedPieceElement) {
      clearSelection();
    }
  });

  // =====================================================
  // MODE BUTTON
  // =====================================================

  if (playerVsPlayerButton) {
    playerVsPlayerButton.addEventListener("click", function (event) {
      event.stopPropagation();

      selectGameMode(false);
    });
  }

  if (playerVsComputerButton) {
    playerVsComputerButton.addEventListener("click", function (event) {
      event.stopPropagation();

      selectGameMode(true);
    });
  }

  if (startGameButton) {
    startGameButton.addEventListener("click", async function (event) {
      event.stopPropagation();

      await startSelectedGame();
    });
  }

  // =====================================================
  // NEW GAME
  // =====================================================

  if (newGameButton) {
    newGameButton.addEventListener("click", async function (event) {
      event.stopPropagation();

      await startNewGame();
    });
  }

  // =====================================================
  // CHANGE MODE
  // =====================================================

  if (changeModeButton) {
    changeModeButton.addEventListener("click", function (event) {
      event.stopPropagation();

      backToModeSelection();
    });
  }

  // =====================================================
  // RESIGN
  // =====================================================

  if (resignButton) {
    resignButton.addEventListener("click", function (event) {
      event.stopPropagation();

      showResignModal();
    });
  }

  // =====================================================
  // RESULT NEW GAME
  // =====================================================

  if (resultNewGameButton) {
    resultNewGameButton.addEventListener("click", async function (event) {
      event.stopPropagation();

      await startNewGame();
    });
  }

  // =====================================================
  // CLOSE RESULT
  // =====================================================

  if (resultCloseButton) {
    resultCloseButton.addEventListener("click", function (event) {
      event.stopPropagation();

      closeGameResult();
    });
  }

  // =====================================================
  // CONFIRM RESIGN
  // =====================================================

  if (confirmResignButton) {
    confirmResignButton.addEventListener("click", async function (event) {
      event.stopPropagation();

      await resignGame();
    });
  }

  // =====================================================
  // CANCEL RESIGN
  // =====================================================

  if (cancelResignButton) {
    cancelResignButton.addEventListener("click", function (event) {
      event.stopPropagation();

      closeResignModal();
    });
  }

  // =====================================================
  // UPDATE PLAYER STATUS
  // =====================================================

  function updatePlayerStatus() {
    const redStatus = document.getElementById("red-player-status");

    const blackStatus = document.getElementById("black-player-status");

    if (!redStatus || !blackStatus) {
      return;
    }

    // ==============================================
    // VS COMPUTER
    // ==============================================

    if (VS_COMPUTER) {
      if (gameStatus !== "InProgress") {
        if (gameStatus === "RedWins") {
          redStatus.textContent = "🏆 Chiến thắng";

          blackStatus.textContent = "❌ Máy thua";

          return;
        }

        if (gameStatus === "BlackWins") {
          redStatus.textContent = "❌ Bạn thua";

          blackStatus.textContent = "🏆 Máy chiến thắng";

          return;
        }

        redStatus.textContent = "⚪ Ván cờ kết thúc";

        blackStatus.textContent = "⚪ Ván cờ kết thúc";

        return;
      }

      redStatus.textContent =
        currentTurn === "Red"
          ? isCheck
            ? "⚠️ ĐANG BỊ CHIẾU"
            : "🟢 Đang đến lượt"
          : "⚪ Đang chờ";

      if (isComputerThinking) {
        blackStatus.textContent = "🤖 Máy đang suy nghĩ...";
      } else {
        blackStatus.textContent =
          currentTurn === "Black" ? "🤖 Đến lượt máy" : "🤖 Đang chờ";
      }

      return;
    }

    // ==============================================
    // VS PLAYER
    // ==============================================

    if (gameStatus !== "InProgress") {
      if (gameStatus === "RedWins") {
        redStatus.textContent = "🏆 Chiến thắng";

        blackStatus.textContent = "❌ Thua";

        return;
      }

      if (gameStatus === "BlackWins") {
        redStatus.textContent = "❌ Thua";

        blackStatus.textContent = "🏆 Chiến thắng";

        return;
      }

      redStatus.textContent = "⚪ Ván cờ kết thúc";

      blackStatus.textContent = "⚪ Ván cờ kết thúc";

      return;
    }

    if (currentTurn === "Red") {
      redStatus.textContent = isCheck ? "⚠️ ĐANG BỊ CHIẾU" : "🟢 Đang đến lượt";

      blackStatus.textContent = "⚪ Đang chờ";

      return;
    }

    if (currentTurn === "Black") {
      redStatus.textContent = "⚪ Đang chờ";

      blackStatus.textContent = isCheck
        ? "⚠️ ĐANG BỊ CHIẾU"
        : "🟢 Đang đến lượt";
    }
  }

  // =====================================================
  // START
  // =====================================================

  initializePage();
});
