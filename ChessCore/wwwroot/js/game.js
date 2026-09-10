document.addEventListener("DOMContentLoaded", function () {
  const SVG_NS = "http://www.w3.org/2000/svg";

  const piecesLayer = document.getElementById("pieces-layer");

  const movesLayer = document.getElementById("moves-layer");

  // =====================================================
  // API CONFIG
  // =====================================================

  const API_BASE_URL = "/api/games";

  // =====================================================
  // GAME ID
  // =====================================================

  let gameId = null;

  let isCreatingGame = false;

  let isMakingMove = false;

  // =====================================================
  // GAME STATE
  // =====================================================

  let currentTurn = null;

  let gameStatus = null;

  // =====================================================
  // BOARD CONFIG
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
  // START GAME
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
    if (gameId !== null) {
      console.log("Game đã tồn tại:", gameId);

      return;
    }

    if (isCreatingGame) {
      console.log("Đang tạo game...");

      return;
    }

    try {
      isCreatingGame = true;

      console.log("Đang tạo ván cờ...");

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

      console.log("Đã render bàn cờ từ Backend!");
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

      console.log("Đang load Game:", gameId);

      const response = await fetch(`${API_BASE_URL}/${gameId}`);

      if (!response.ok) {
        console.log("Game cũ không tồn tại.");

        localStorage.removeItem("xiangqiGameId");

        gameId = null;

        await createGame();

        return;
      }

      const data = await response.json();

      console.log("Load game:", data);

      updateGameFromApi(data);
    } catch (error) {
      console.error("Lỗi load game:", error);
    }
  }

  // =====================================================
  // UPDATE GAME FROM API
  // =====================================================

  function updateGameFromApi(data) {
    gameId = data.id;

    currentTurn = data.currentTurn;

    gameStatus = data.status;

    localStorage.setItem("xiangqiGameId", gameId);

    console.log("Game ID:", gameId);

    console.log("Current Turn:", currentTurn);

    console.log("Game Status:", gameStatus);

    initialPieces = data.boardPieces.map(function (piece) {
      return convertPieceFromApi(piece);
    });

    console.log("Số quân còn lại:", initialPieces.length);

    selectedPieceElement = null;

    selectedPieceData = null;

    currentValidMoves = [];

    clearMoveHighlights();

    renderPieces();

    updateTurnStatus();

    console.log("Bàn cờ đã được cập nhật!");
  }

  // =====================================================
  // UPDATE TURN STATUS
  // =====================================================

  function updateTurnStatus() {
    const turnStatus = document.getElementById("turn-status");

    if (!turnStatus) {
      return;
    }

    // ===============================================
    // GAME ENDED
    // ===============================================

    if (gameStatus !== "InProgress") {
      if (gameStatus === "RedWins") {
        turnStatus.textContent = "Quân Đỏ chiến thắng!";

        return;
      }

      if (gameStatus === "BlackWins") {
        turnStatus.textContent = "Quân Đen chiến thắng!";

        return;
      }

      turnStatus.textContent = "Ván cờ đã kết thúc.";

      return;
    }

    // ===============================================
    // RED TURN
    // ===============================================

    if (currentTurn === "Red") {
      turnStatus.textContent = "Lượt của quân Đỏ";
    }

    // ===============================================
    // BLACK TURN
    // ===============================================
    else {
      turnStatus.textContent = "Lượt của quân Đen";
    }
  }

  // =====================================================
  // CONVERT API PIECE
  // =====================================================

  function convertPieceFromApi(piece) {
    const color = piece.color.toLowerCase();

    let type = "";

    let text = "";

    // ===============================================
    // XE
    // ===============================================

    if (piece.type === "Rook") {
      type = "Xe";

      text = "車";
    }

    // ===============================================
    // MÃ
    // ===============================================
    else if (piece.type === "Horse") {
      type = "Mã";

      text = "馬";
    }

    // ===============================================
    // TƯỢNG
    // ===============================================
    else if (piece.type === "Elephant") {
      type = "Tượng";

      text = color === "red" ? "相" : "象";
    }

    // ===============================================
    // SĨ
    // ===============================================
    else if (piece.type === "Advisor") {
      type = "Sĩ";

      text = color === "red" ? "仕" : "士";
    }

    // ===============================================
    // TƯỚNG
    // ===============================================
    else if (piece.type === "King") {
      type = "Tướng";

      text = color === "red" ? "帥" : "將";
    }

    // ===============================================
    // PHÁO
    // ===============================================
    else if (piece.type === "Cannon") {
      type = "Pháo";

      text = color === "red" ? "砲" : "炮";
    }

    // ===============================================
    // TỐT
    // ===============================================
    else if (piece.type === "Pawn") {
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
  // GET POSITION
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

      console.log("Click quân:", piece);

      // ===============================================
      // GAME ENDED
      // ===============================================

      if (gameStatus !== "InProgress") {
        alert("Ván cờ đã kết thúc!");

        return;
      }

      // ===============================================
      // ĐÃ CHỌN QUÂN
      // ===============================================

      if (selectedPieceData !== null) {
        // =============================================
        // CLICK QUÂN ĐỐI THỦ
        // XỬ LÝ ĂN QUÂN
        // =============================================

        if (
          piece.color.toLowerCase() !== selectedPieceData.color.toLowerCase()
        ) {
          const canCapture = isValidMove(piece.row, piece.col);

          if (canCapture) {
            console.log("Ăn quân đối thủ:", piece);

            await makeMove(piece.row, piece.col);

            return;
          }
        }

        // =============================================
        // CLICK LẠI QUÂN ĐANG CHỌN
        // =============================================

        if (selectedPieceElement === pieceGroup) {
          pieceGroup.classList.remove("selected");

          selectedPieceElement = null;

          selectedPieceData = null;

          currentValidMoves = [];

          clearMoveHighlights();

          return;
        }
      }

      // ===============================================
      // KIỂM TRA LƯỢT
      // ===============================================

      if (piece.color.toLowerCase() !== currentTurn.toLowerCase()) {
        console.log("Chưa tới lượt quân này");

        return;
      }

      // ===============================================
      // BỎ CHỌN QUÂN CŨ
      // ===============================================

      if (selectedPieceElement !== null) {
        selectedPieceElement.classList.remove("selected");
      }

      clearMoveHighlights();

      // ===============================================
      // CHỌN QUÂN MỚI
      // ===============================================

      pieceGroup.classList.add("selected");

      selectedPieceElement = pieceGroup;

      selectedPieceData = piece;

      // ===============================================
      // GET VALID MOVES
      // ===============================================

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
      if (!gameId) {
        console.error("Game ID chưa tồn tại");

        return;
      }

      console.log("Lấy nước đi hợp lệ:", row, col);

      const url = `${API_BASE_URL}/${gameId}/valid-moves?row=${row}&col=${col}`;

      const response = await fetch(url);

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(errorData?.message || "Không thể lấy nước đi");
      }

      const data = await response.json();

      console.log("Valid moves từ Backend:", data);

      currentValidMoves = data;

      showValidMovesFromApi(data);
    } catch (error) {
      console.error("Lỗi valid moves:", error);

      clearMoveHighlights();
    }
  }

  // =====================================================
  // CLEAR HIGHLIGHTS
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
  // HIGHLIGHT MOVE
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
    // ===============================================
    // DOUBLE CLICK PROTECTION
    // ===============================================

    if (isMakingMove) {
      console.log("Đang thực hiện nước đi...");

      return;
    }

    try {
      if (!selectedPieceData) {
        console.error("Chưa chọn quân");

        return;
      }

      isMakingMove = true;

      console.log("Thực hiện nước đi");

      console.log("From:", selectedPieceData.row, selectedPieceData.col);

      console.log("To:", toRow, toCol);

      const requestData = {
        fromX: selectedPieceData.row,

        fromY: selectedPieceData.col,

        toX: toRow,

        toY: toCol,
      };

      console.log("Request:", requestData);

      const response = await fetch(`${API_BASE_URL}/${gameId}/moves`, {
        method: "POST",

        headers: {
          "Content-Type": "application/json",
        },

        body: JSON.stringify(requestData),
      });

      // =============================================
      // ERROR
      // =============================================

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(errorData?.message || "Không thể thực hiện nước đi");
      }

      // =============================================
      // RESPONSE
      // =============================================

      const data = await response.json();

      console.log("Nước đi thành công:", data);

      // =============================================
      // UPDATE BOARD
      // =============================================

      updateGameFromApi(data);

      console.log("Đã cập nhật bàn cờ!");

      console.log("Lượt tiếp theo:", currentTurn);
    } catch (error) {
      console.error("Lỗi di chuyển:", error);

      alert(error.message);
    } finally {
      isMakingMove = false;
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
  // UPDATE TURN STATUS
  // =====================================================

  function updateTurnStatus() {
    const turnStatus = document.getElementById("turn-status");

    if (!turnStatus) {
      console.warn("Không tìm thấy turn-status");

      return;
    }

    if (currentTurn === "Red") {
      turnStatus.textContent = "🔴 Lượt của QUÂN ĐỎ";
    } else if (currentTurn === "Black") {
      turnStatus.textContent = "⚫ Lượt của QUÂN ĐEN";
    } else {
      turnStatus.textContent = "Ván cờ đã kết thúc";
    }
  }

  // =====================================================
  // START
  // =====================================================

  initializeGame();
});
