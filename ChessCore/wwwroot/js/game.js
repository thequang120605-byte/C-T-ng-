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

  let gameId = localStorage.getItem("currentGameId");

  if (gameId !== null) {
    gameId = parseInt(gameId);
  }

  let isCreatingGame = false;

  // =====================================================
  // GAME STATE
  // =====================================================

  let currentTurn = null;

  let gameStatus = null;

  let isMakingMove = false;

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
  // CREATE GAME
  // =====================================================

  async function createGame() {
    // =============================================
    // ĐÃ CÓ GAME
    // =============================================

    if (gameId !== null) {
      console.log("Game đã tồn tại:", gameId);

      return;
    }

    // =============================================
    // ĐANG TẠO GAME
    // =============================================

    if (isCreatingGame) {
      console.log("Đang tạo game, bỏ qua request trùng lặp...");

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

      // =============================================
      // ERROR
      // =============================================

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(errorData?.message || "Không thể tạo ván cờ");
      }

      // =============================================
      // RESPONSE
      // =============================================

      const data = await response.json();

      console.log("Game mới:", data);

      // =============================================
      // UPDATE GAME
      // =============================================

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

      // =============================================
      // GAME KHÔNG TỒN TẠI
      // =============================================

      if (response.status === 404) {
        console.log("Game cũ không tồn tại");

        localStorage.removeItem("currentGameId");

        gameId = null;

        await createGame();

        return;
      }

      // =============================================
      // ERROR
      // =============================================

      if (!response.ok) {
        throw new Error("Không thể tải game");
      }

      // =============================================
      // DATA
      // =============================================

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
    // =============================================
    // GAME ID
    // =============================================

    gameId = data.id;

    localStorage.setItem("currentGameId", gameId);

    // =============================================
    // GAME STATE
    // =============================================

    currentTurn = data.currentTurn;

    gameStatus = data.status;

    console.log("Game ID:", gameId);

    console.log("Current Turn:", currentTurn);

    console.log("Game Status:", gameStatus);

    // =============================================
    // PIECES
    // =============================================

    initialPieces = data.boardPieces.map(function (piece) {
      return convertPieceFromApi(piece);
    });

    console.log("Số quân còn lại:", initialPieces.length);

    // =============================================
    // RESET SELECT
    // =============================================

    selectedPieceElement = null;

    selectedPieceData = null;

    currentValidMoves = [];

    // =============================================
    // CLEAR MOVE
    // =============================================

    clearMoveHighlights();

    // =============================================
    // RENDER
    // =============================================

    renderPieces();

    console.log("Bàn cờ đã được cập nhật!");
  }

  // =====================================================
  // CONVERT API PIECE
  // =====================================================

  function convertPieceFromApi(piece) {
    const color = piece.color.toLowerCase();

    let type = "";

    let text = "";

    // =================================================
    // XE
    // =================================================

    if (piece.type === "Rook") {
      type = "Xe";

      text = "車";
    }

    // =================================================
    // MÃ
    // =================================================
    else if (piece.type === "Horse") {
      type = "Mã";

      text = "馬";
    }

    // =================================================
    // TƯỢNG
    // =================================================
    else if (piece.type === "Elephant") {
      type = "Tượng";

      if (color === "red") {
        text = "相";
      } else {
        text = "象";
      }
    }

    // =================================================
    // SĨ
    // =================================================
    else if (piece.type === "Advisor") {
      type = "Sĩ";

      if (color === "red") {
        text = "仕";
      } else {
        text = "士";
      }
    }

    // =================================================
    // TƯỚNG
    // =================================================
    else if (piece.type === "King") {
      type = "Tướng";

      if (color === "red") {
        text = "帥";
      } else {
        text = "將";
      }
    }

    // =================================================
    // PHÁO
    // =================================================
    else if (piece.type === "Cannon") {
      type = "Pháo";

      if (color === "red") {
        text = "砲";
      } else {
        text = "炮";
      }
    }

    // =================================================
    // TỐT
    // =================================================
    else if (piece.type === "Pawn") {
      type = "Tốt";

      if (color === "red") {
        text = "兵";
      } else {
        text = "卒";
      }
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
  // GET SVG POSITION
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

    // =============================================
    // GROUP
    // =============================================

    const pieceGroup = document.createElementNS(SVG_NS, "g");

    pieceGroup.setAttribute("class", `chess-piece ${piece.color}`);

    pieceGroup.setAttribute("data-type", piece.type);

    pieceGroup.setAttribute("data-color", piece.color);

    pieceGroup.setAttribute("data-row", piece.row);

    pieceGroup.setAttribute("data-col", piece.col);

    // =============================================
    // CIRCLE
    // =============================================

    const circle = document.createElementNS(SVG_NS, "circle");

    circle.setAttribute("cx", position.x);

    circle.setAttribute("cy", position.y);

    circle.setAttribute("r", 42);

    circle.setAttribute("class", "piece-circle");

    // =============================================
    // TEXT
    // =============================================

    const textElement = document.createElementNS(SVG_NS, "text");

    textElement.setAttribute("x", position.x);

    textElement.setAttribute("y", position.y + 2);

    textElement.setAttribute("text-anchor", "middle");

    textElement.setAttribute("dominant-baseline", "middle");

    textElement.setAttribute("class", "piece-text");

    textElement.textContent = piece.text;

    // =============================================
    // ADD
    // =============================================

    pieceGroup.appendChild(circle);

    pieceGroup.appendChild(textElement);

    // =============================================
    // CLICK PIECE
    // =============================================

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
  // HANDLE PIECE CLICK
  // =====================================================

  async function handlePieceClick(pieceGroup) {
    try {
      const piece = getPieceData(pieceGroup);

      console.log("Click quân:", piece);

      // =============================================
      // GAME ENDED
      // =============================================

      if (gameStatus !== "InProgress") {
        alert("Ván cờ đã kết thúc!");

        return;
      }

      // =============================================
      // CHECK TURN
      // =============================================

      if (currentTurn === null) {
        console.error("Chưa xác định lượt chơi");

        return;
      }

      if (piece.color.toLowerCase() !== currentTurn.toLowerCase()) {
        console.log("Chưa tới lượt quân này");

        return;
      }

      // =============================================
      // CLICK AGAIN
      // =============================================

      if (selectedPieceElement === pieceGroup) {
        clearSelectedPiece();

        return;
      }

      // =============================================
      // REMOVE OLD SELECT
      // =============================================

      if (selectedPieceElement !== null) {
        selectedPieceElement.classList.remove("selected");
      }

      clearMoveHighlights();

      // =============================================
      // SELECT NEW
      // =============================================

      pieceGroup.classList.add("selected");

      selectedPieceElement = pieceGroup;

      selectedPieceData = piece;

      // =============================================
      // GET VALID MOVES
      // =============================================

      await fetchValidMoves(piece.row, piece.col);
    } catch (error) {
      console.error("Lỗi chọn quân:", error);
    }
  }

  // =====================================================
  // GET VALID MOVES FROM API
  // =====================================================

  async function fetchValidMoves(row, col) {
    try {
      if (gameId === null) {
        console.error("Game ID chưa tồn tại");

        return;
      }

      console.log("Lấy nước đi hợp lệ:", row, col);

      const url = `${API_BASE_URL}/${gameId}/valid-moves?row=${row}&col=${col}`;

      const response = await fetch(url, {
        method: "GET",
      });

      // =============================================
      // ERROR
      // =============================================

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(errorData?.message || "Không thể lấy nước đi");
      }

      // =============================================
      // RESPONSE
      // =============================================

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
  // CLEAR SELECTED PIECE
  // =====================================================

  function clearSelectedPiece() {
    if (selectedPieceElement !== null) {
      selectedPieceElement.classList.remove("selected");
    }

    selectedPieceElement = null;

    selectedPieceData = null;

    currentValidMoves = [];

    clearMoveHighlights();
  }

  // =====================================================
  // CLEAR MOVE HIGHLIGHTS
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

    // =============================================
    // CLICK MOVE
    // =============================================

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
    // =============================================
    // ĐANG THỰC HIỆN NƯỚC ĐI
    // =============================================

    if (isMakingMove) {
      console.log("Đang thực hiện nước đi, bỏ qua click trùng lặp...");

      return;
    }

    // =============================================
    // CHƯA CHỌN QUÂN
    // =============================================

    if (!selectedPieceData) {
      console.error("Chưa chọn quân");

      return;
    }

    try {
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
      // =============================================
      // KẾT THÚC THỰC HIỆN NƯỚC ĐI
      // =============================================

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
  // CLICK OUTSIDE BOARD
  // =====================================================

  document.addEventListener("click", function () {
    clearSelectedPiece();
  });

  // =====================================================
  // START GAME
  // =====================================================

  async function startGame() {
    // =============================================
    // ĐÃ CÓ GAME TRONG LOCAL STORAGE
    // =============================================

    if (gameId !== null) {
      console.log("Tìm thấy Game ID:", gameId);

      await loadGame();
    }

    // =============================================
    // CHƯA CÓ GAME
    // =============================================
    else {
      console.log("Chưa có game cũ");

      await createGame();
    }
  }

  startGame();
});
