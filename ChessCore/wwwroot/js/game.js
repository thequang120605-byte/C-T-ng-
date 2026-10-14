document.addEventListener("DOMContentLoaded", function () {
  const SVG_NS = "http://www.w3.org/2000/svg";

  // =====================================================
  // SIGNALR (SAFE INITIALIZATION & REALTIME GROUP)
  // =====================================================
  let connection = null;

  function joinSignalRGroup(gid) {
    if (!gid || !connection || typeof signalR === "undefined") return;
    if (connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke("JoinGame", gid.toString()).catch(err => console.warn("Lỗi JoinGame SignalR:", err));
    }
  }

  if (typeof signalR !== "undefined") {
    try {
      connection = new signalR.HubConnectionBuilder()
          .withUrl("/gameHub")
          .withAutomaticReconnect()
          .build();

      connection.on("GameUpdated", function (data) {
          if (typeof gameId !== 'undefined' && data && data.id == gameId) {
              console.log("Realtime: Nhận GameUpdated từ SignalR:", data);
              updateGameFromApi(data);
          }
      });

      connection.on("DrawOffered", function (data) {
          if (typeof gameId !== 'undefined' && data && data.gameId == gameId) {
              const myColor = localStorage.getItem("onlinePlayerColor");
              if (data.fromColor === myColor) {
                  return;
              }
              showDrawOfferModal(data.timeoutSeconds || 15);
          }
      });

      connection.on("DrawDeclined", function (data) {
          if (typeof gameId !== 'undefined' && data && data.gameId == gameId) {
              alert(data.message || "Đối thủ đã từ chối lời xin hòa.");
              closeDrawOfferModal();
          }
      });

      connection.on("MatchEnded_Draw", function (data) {
          if (typeof gameId !== 'undefined' && data && data.gameId == gameId) {
              closeDrawOfferModal();
          }
      });

      connection.start().then(function () {
          console.log("SignalR kết nối thành công.");
          if (typeof gameId !== 'undefined' && gameId) {
              joinSignalRGroup(gameId);
          }
      }).catch(err => console.warn("SignalR Connection Warning:", err.toString()));

      connection.onreconnected(function () {
          if (typeof gameId !== 'undefined' && gameId) {
              joinSignalRGroup(gameId);
          }
      });
    } catch (err) {
      console.warn("SignalR initialization skipped:", err);
    }
  }

  // =====================================================
  // API
  // =====================================================

  const API_BASE_URL = "/api/games";

  // =====================================================
  // MODE
  // =====================================================

  let VS_COMPUTER = false;

  let gameModeSelected = false;

  // =====================================================
  // COMPUTER DIFFICULTY
  // =====================================================

  // 1 = Dễ        = Depth 1
  // 2 = Trung bình = Depth 2
  // 3 = Khó       = Depth 3
  // 4 = Siêu khó  = Depth 4

  let computerDifficulty = 2;

  const DIFFICULTY_INFO = {
    1: {
      name: "🟢 Dễ",
      depth: 1,
      description: "Máy tính toán ở Depth 1, phù hợp để làm quen.",
    },

    2: {
      name: "🟡 Trung bình",
      depth: 2,
      description:
        "Máy tính toán ở Depth 2, có thể xem xét nước đáp trả của đối thủ.",
    },

    3: {
      name: "🟠 Khó",
      depth: 3,
      description: "Máy tính toán ở Depth 3, phải xét nhiều nhánh nước đi hơn.",
    },

    4: {
      name: "🔴 Siêu khó",
      depth: 4,
      description:
        "Máy tính toán ở Depth 4, số lượng trạng thái cần đánh giá tăng đáng kể.",
    },
  };

  // =====================================================
  // DOM
  // =====================================================

  const modeSelection = document.getElementById("mode-selection");

  const gameContent = document.getElementById("game-content");

  const playerVsPlayerButton = document.getElementById("player-vs-player-btn");

  const playerVsComputerButton = document.getElementById(
    "player-vs-computer-btn",
  );
  
  const playerVsOnlineButton = document.getElementById("player-vs-online-btn");
  const onlineSelection = document.getElementById("online-selection");
  const joinOnlineButton = document.getElementById("join-online-btn");
  const createOnlineButton = document.getElementById("create-online-btn");
  const onlineRoomIdInput = document.getElementById("online-room-id");
  const opponentPlayerIdInput = document.getElementById("opponent-player-id");
  const invitePlayerButton = document.getElementById("invite-player-btn");
  const invitePlayerStatus = document.getElementById("invite-player-status");
  const isAuthenticated = document.getElementById("current-player-id") !== null;
  let isOnlineMode = localStorage.getItem("isOnlineMode") === "true";

  const startGameButton = document.getElementById("start-game-btn");

  const selectedModeText = document.getElementById("selected-mode-text");

  const difficultySelection = document.getElementById("difficulty-selection");

  const difficultyButtons = document.querySelectorAll(".diff-btn");

  const difficultyDescription = document.getElementById(
    "difficulty-description",
  );

  const selectedDifficultyContainer = document.getElementById(
    "selected-difficulty-container",
  );

  const selectedDifficultyText = document.getElementById(
    "selected-difficulty-text",
  );

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

  const drawButton = document.getElementById("draw-btn");
  const confirmDrawModal = document.getElementById("confirm-draw-modal");
  const confirmDrawSubmitBtn = document.getElementById("confirm-draw-submit-btn");
  const confirmDrawCancelBtn = document.getElementById("confirm-draw-cancel-btn");
  const drawOfferModal = document.getElementById("draw-offer-modal");
  const acceptDrawBtn = document.getElementById("accept-draw-btn");
  const declineDrawBtn = document.getElementById("decline-draw-btn");
  const drawOfferCountdown = document.getElementById("draw-offer-countdown");

  // =====================================================
  // GAME STATE
  // =====================================================

  let gameId = null;

  let pendingExitAction = null;

  let isCreatingGame = false;

  let isMakingMove = false;

  let isComputerThinking = false;

  let currentTurn = null;

  let gameStatus = null;

  let isCheck = false;

  let timerInterval = null;

  let remainingSeconds = 180;

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
  // SELECT DIFFICULTY
  // =====================================================

  function selectDifficulty(difficulty) {
    difficulty = parseInt(difficulty, 10);

    if (!DIFFICULTY_INFO[difficulty]) {
      difficulty = 2;
    }

    computerDifficulty = difficulty;

    // -----------------------------------------
    // Remove selected from all buttons
    // -----------------------------------------

    difficultyButtons.forEach(function (button) {
      const buttonDifficulty = parseInt(button.dataset.difficulty, 10);

      const selected = buttonDifficulty === computerDifficulty;

      button.classList.toggle("selected", selected);

      button.setAttribute("aria-pressed", selected ? "true" : "false");
    });

    // -----------------------------------------
    // Description
    // -----------------------------------------

    const info = DIFFICULTY_INFO[computerDifficulty];

    if (difficultyDescription) {
      difficultyDescription.textContent = `${info.name} - ${info.description}`;
    }

    // -----------------------------------------
    // Selected difficulty
    // -----------------------------------------

    if (selectedDifficultyText) {
      selectedDifficultyText.textContent = `${info.name} - Depth ${info.depth}`;
    }

    if (selectedDifficultyContainer) {
      selectedDifficultyContainer.style.display = "block"; selectedDifficultyContainer.classList.remove("hidden");
    }

    console.log("Độ khó máy:", computerDifficulty, "Depth:", info.depth);
  }

  // =====================================================
  // SELECT MODE
  // =====================================================

  function selectGameMode(isComputer) {
    VS_COMPUTER = isComputer;

    gameModeSelected = true;

    // -----------------------------------------
    // Reset selected buttons
    // -----------------------------------------

    if (playerVsPlayerButton) {
      playerVsPlayerButton.classList.remove("selected");

      playerVsPlayerButton.setAttribute("aria-pressed", "false");
    }

    if (playerVsComputerButton) {
      playerVsComputerButton.classList.remove("selected");

      playerVsComputerButton.setAttribute("aria-pressed", "false");
    }

    if (playerVsOnlineButton) {
      playerVsOnlineButton.classList.remove("selected");
      playerVsOnlineButton.setAttribute("aria-pressed", "false");
    }

    // -----------------------------------------
    // COMPUTER
    // -----------------------------------------

    if (VS_COMPUTER) {
      if (playerVsComputerButton) {
        playerVsComputerButton.classList.add("selected");

        playerVsComputerButton.setAttribute("aria-pressed", "true");
      }

      if (selectedModeText) {
        selectedModeText.textContent = "🤖 Người chơi vs Máy";
      }

      if (difficultySelection) {
        difficultySelection.style.display = "block"; difficultySelection.classList.remove("hidden");
      }

      if (difficultyDescription) {
        const info = DIFFICULTY_INFO[computerDifficulty];

        difficultyDescription.textContent = `${info.name} - ${info.description}`;
      }

      if (selectedDifficultyContainer) {
        selectedDifficultyContainer.style.display = "block"; selectedDifficultyContainer.classList.remove("hidden");
      }

      if (selectedDifficultyText) {
        const info = DIFFICULTY_INFO[computerDifficulty];

        selectedDifficultyText.textContent = `${info.name} - Depth ${info.depth}`;
      }
    }

    // -----------------------------------------
    // PLAYER VS PLAYER
    // -----------------------------------------
    else {
      if (playerVsPlayerButton) {
        playerVsPlayerButton.classList.add("selected");

        playerVsPlayerButton.setAttribute("aria-pressed", "true");
      }

      if (selectedModeText) {
        selectedModeText.textContent = "👥 Người chơi vs Người chơi";
      }

      if (difficultySelection) {
        difficultySelection.style.display = "none"; difficultySelection.classList.add("hidden");
      }

      if (selectedDifficultyContainer) {
        selectedDifficultyContainer.style.display = "none"; selectedDifficultyContainer.classList.add("hidden");
      }
    }

    // -----------------------------------------
    // Enable start button
    // -----------------------------------------

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
    localStorage.removeItem("manualBoardFlipped");

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

    updateBoardOrientation();
    updatePlayerNames();

    await createGame();
  }

  // =====================================================
  // BACK TO MODE SELECTION
  // =====================================================

  function backToModeSelection() {
    closeGameResult();

    closeResignModal();
    closeConfirmDrawModal();
    closeDrawOfferModal();

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

    if (timerInterval) {
      clearInterval(timerInterval);
      timerInterval = null;
    }
    const redTimer = document.getElementById("red-player-timer");
    const blackTimer = document.getElementById("black-player-timer");
    if (redTimer) { redTimer.textContent = "03:00"; redTimer.classList.remove("danger"); }
    if (blackTimer) { blackTimer.textContent = "03:00"; blackTimer.classList.remove("danger"); }

    localStorage.removeItem("xiangqiGameId");
    localStorage.removeItem("manualBoardFlipped");
    updateBoardOrientation();

    if (playerVsPlayerButton) {
      playerVsPlayerButton.classList.remove("selected");

      playerVsPlayerButton.setAttribute("aria-pressed", "false");
    }

    if (playerVsComputerButton) {
      playerVsComputerButton.classList.remove("selected");

      playerVsComputerButton.setAttribute("aria-pressed", "false");
    }

    if (difficultySelection) {
      difficultySelection.style.display = "none"; difficultySelection.classList.add("hidden");
    }

    if (selectedDifficultyContainer) {
      selectedDifficultyContainer.style.display = "none"; selectedDifficultyContainer.classList.add("hidden");
    }

    if (startGameButton) {
      startGameButton.disabled = true;

      startGameButton.classList.remove("enabled");
    }

    if (selectedModeText) {
      selectedModeText.textContent = "Chưa chọn chế độ chơi";
    }

    // Mặc định lại Trung bình

    selectDifficulty(2);
  }

  // =====================================================
  // INITIALIZE PAGE
  // =====================================================

  function initializePage() {
    const isOnline = localStorage.getItem("isOnlineMode") === "true";
    const savedGameId = localStorage.getItem("xiangqiGameId");
    if (savedGameId && isOnline) {
      gameId = savedGameId;
      if (modeSelection) modeSelection.classList.add("hidden");
      if (gameContent) gameContent.classList.remove("hidden");
      loadGame();
      return;
    }
    if (modeSelection) {
      modeSelection.classList.remove("hidden");
    }

    if (gameContent) {
      gameContent.classList.add("hidden");
    }

    // Mặc định chọn Người vs Người để nút BẮT ĐẦU VÁN CỜ luôn sẵn sàng
    selectGameMode(false);

    // Mặc định Độ khó Trung bình
    selectDifficulty(2);

    console.log("Khởi tạo trang chơi cờ thành công. Sẵn sàng tạo ván cờ.");
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

      console.log("Đang gửi yêu cầu tạo ván cờ mới tới API:", API_BASE_URL);

      const response = await fetch(API_BASE_URL, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error("Lỗi HTTP tạo game:", response.status, errorText);
        throw new Error(`Mã phản hồi ${response.status}: ${errorText || "Không thể tạo ván cờ"}`);
      }

      const data = await response.json();

      console.log("Tạo ván cờ thành công:", data);

      gameId = data.id;

      localStorage.setItem("xiangqiGameId", gameId);

      updateGameFromApi(data);
    } catch (error) {
      console.error("Lỗi tạo game:", error);

      alert(`Không thể tạo ván cờ: ${error.message || "Vui lòng kiểm tra lại kết nối."}`);
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
    joinSignalRGroup(gameId);

    currentTurn = data.currentTurn;

    gameStatus = data.status;

    isCheck = data.isCheck || false;

    initialPieces = (data.boardPieces || []).map(convertPieceFromApi);

    selectedPieceElement = null;

    selectedPieceData = null;

    currentValidMoves = [];

    clearMoveHighlights();

    updateBoardOrientation();

    renderPieces();

    updatePlayerNames();

    updatePlayerTimers(data);

    const invitePanel = document.getElementById("opponent-invite-panel");
    if (invitePanel && data.status === "InProgress") {
      invitePanel.classList.add("hidden");
    }

    updateTurnStatus();

    updatePlayerStatus();

    handleCheckWarning();

    updateComputerStatus();

    checkGameResult();
    
    updateEvalBar();

    console.log("Bàn cờ đã được cập nhật!");

    if (VS_COMPUTER && gameStatus === "InProgress" && currentTurn === "Black") {
      triggerComputerMove();
    }
  }

  // =====================================================
  // PLAYER TIMERS (3 PHÚT / NƯỚC)
  // =====================================================

  function formatTime(seconds) {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins < 10 ? "0" : ""}${mins}:${secs < 10 ? "0" : ""}${secs}`;
  }

  function updatePlayerTimers(data) {
    if (timerInterval) {
      clearInterval(timerInterval);
      timerInterval = null;
    }

    const redTimer = document.getElementById("red-player-timer");
    const blackTimer = document.getElementById("black-player-timer");
    if (!redTimer || !blackTimer) return;

    if (data.status !== "InProgress") {
      redTimer.classList.remove("danger");
      blackTimer.classList.remove("danger");
      return;
    }

    if (typeof data.remainingSeconds === "number") {
      remainingSeconds = data.remainingSeconds;
    } else if (data.lastMoveAt) {
      const elapsed = Math.floor((Date.now() - new Date(data.lastMoveAt).getTime()) / 1000);
      remainingSeconds = Math.max(0, 180 - elapsed);
    } else {
      remainingSeconds = 180;
    }

    const activeTimer = data.currentTurn === "Red" ? redTimer : blackTimer;
    const waitingTimer = data.currentTurn === "Red" ? blackTimer : redTimer;

    waitingTimer.textContent = "03:00";
    waitingTimer.classList.remove("danger");

    activeTimer.textContent = formatTime(remainingSeconds);
    activeTimer.classList.toggle("danger", remainingSeconds <= 30);

    timerInterval = setInterval(function () {
      if (remainingSeconds > 0) {
        remainingSeconds--;
        activeTimer.textContent = formatTime(remainingSeconds);
        activeTimer.classList.toggle("danger", remainingSeconds <= 30);
      } else {
        clearInterval(timerInterval);
        timerInterval = null;
        activeTimer.textContent = "00:00";
        activeTimer.classList.add("danger");
      }
    }, 1000);
  }

  // =====================================================
  // PLAYER NAMES
  // =====================================================

  function updatePlayerNames() {
    updateBoardOrientation();
    const redPlayerName = document.getElementById("red-player-name");

    const blackPlayerName = document.getElementById("black-player-name");

    if (isOnlineMode) {
      const currentPlayerName = "Bạn";
      let opponentName = localStorage.getItem("onlineOpponentName");
      if (!opponentName) {
        opponentName = (gameStatus === "WaitingForPlayers") ? "Đang chờ đối thủ" : "Đối thủ";
      }
      const playerColor = localStorage.getItem("onlinePlayerColor");

      if (redPlayerName) {
        redPlayerName.textContent = playerColor === "Black" ? opponentName : currentPlayerName;
      }
      if (blackPlayerName) {
        blackPlayerName.textContent = playerColor === "Black" ? currentPlayerName : opponentName;
      }
      return;
    }

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

      const difficultyInfo = DIFFICULTY_INFO[computerDifficulty];

      console.log("🤖 Máy đang tính nước đi...");
      console.log("Độ khó:", difficultyInfo.name);
      console.log("Depth:", difficultyInfo.depth);

      const response = await fetch(
        `${API_BASE_URL}/${gameId}/computer-move?difficulty=${computerDifficulty}`,
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

      showComputerErrorNotification(
        error.message || "Máy không thể thực hiện nước đi",
      );
    } finally {
      isComputerThinking = false;

      updateComputerStatus();

      updateTurnStatus();
    }
  }

  function showComputerErrorNotification(message) {
    const overlay = document.getElementById("check-overlay");

    if (!overlay) {
      console.error("Không tìm thấy #check-overlay");
      return;
    }

    const icon = document.getElementById("check-overlay-icon");
    const title = document.getElementById("check-overlay-title");
    const messageElement = document.getElementById("check-overlay-message");

    if (icon) {
      icon.textContent = "🤖";
    }

    if (title) {
      title.textContent = "MÁY KHÔNG TÌM ĐƯỢC NƯỚC ĐI";
    }

    if (messageElement) {
      messageElement.textContent = message;
    }

    overlay.classList.remove("hidden");
    overlay.classList.add("ai-error");

    setTimeout(() => {
      overlay.classList.add("hidden");
      overlay.classList.remove("ai-error");
    }, 2500);
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
      overlay.classList.add("hidden");
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
        overlay.classList.remove("hidden");

        setTimeout(function () {
          overlay.classList.add("hidden");
        }, 1200);
      }
    }
  }

  // =====================================================
  // CHECK RESULT
  // =====================================================

  function checkGameResult() {
    if (gameStatus === "RedWins" || gameStatus === "BlackWins" || gameStatus === "Draw") {
      if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
      }
    }

    if (gameStatus === "Draw") {
      showGameResult(
        "🤝 HÒA CỜ",
        "HÒA NHAU",
        "Ván cờ kết thúc với kết quả hòa thỏa thuận!"
      );
      return;
    }

    if (gameStatus === "RedWins") {
      const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
      const isMyWin = !isOnline ? true : (localStorage.getItem("onlinePlayerColor") === "Red");
      const isTimeout = remainingSeconds <= 0;
      const subtitle = isTimeout ? (isMyWin ? "Đối thủ đã hết 3 phút (Bị xử thua do rụng kim)!" : "Bạn đã hết 3 phút (Bị xử thua do rụng kim)!") : (isMyWin ? "Bạn đã giành chiến thắng!" : "Đối thủ đã giành chiến thắng!");
      showGameResult(
        isMyWin ? "🏆 CHIẾN THẮNG" : "😔 THẤT BẠI",
        "🔴 QUÂN ĐỎ",
        isOnline
          ? subtitle
          : (VS_COMPUTER ? "Bạn đã đánh bại máy!" : "Quân Đỏ đã giành chiến thắng!"),
      );
      return;
    }

    if (gameStatus === "BlackWins") {
      const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
      const isMyWin = !isOnline ? false : (localStorage.getItem("onlinePlayerColor") === "Black");
      const isTimeout = remainingSeconds <= 0;
      const subtitle = isTimeout ? (isMyWin ? "Đối thủ đã hết 3 phút (Bị xử thua do rụng kim)!" : "Bạn đã hết 3 phút (Bị xử thua do rụng kim)!") : (isMyWin ? "Bạn đã giành chiến thắng!" : "Đối thủ đã giành chiến thắng!");
      showGameResult(
        (VS_COMPUTER || isOnline) ? (isMyWin ? "🏆 CHIẾN THẮNG" : "😔 THẤT BẠI") : "🏆 CHIẾN THẮNG",
        "⚫ QUÂN ĐEN",
        isOnline
          ? subtitle
          : (VS_COMPUTER ? "Máy đã giành chiến thắng!" : "Quân Đen đã giành chiến thắng!"),
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

    const reviewBtn = document.getElementById("result-review-btn");
    if (reviewBtn) {
      if (gameId) {
        reviewBtn.href = `/Game/Review/${gameId}`;
        reviewBtn.style.display = "inline-flex";
      } else {
        reviewBtn.style.display = "none";
      }
    }

    gameResultModal.classList.remove("hidden");
  }

  // =====================================================
  // CLOSE RESULT
  // =====================================================

  function closeGameResult() {
    if (!gameResultModal) {
      return;
    }

    gameResultModal.classList.add("hidden");
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

    const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";

    if (isOnline) {
      const myColor = localStorage.getItem("onlinePlayerColor");
      const isMyTurn = currentTurn === myColor;
      const pieceName = currentTurn === "Red" ? "QUÂN ĐỎ" : "QUÂN ĐEN";
      const icon = currentTurn === "Red" ? "🔴" : "⚫";

      if (gameStatus === "Draw") {
        turnStatus.textContent = "🤝 VÁN CỜ HÒA";
        if (turnIndicator) turnIndicator.textContent = "🤝";
        return;
      }
      if (gameStatus === "RedWins") {
        turnStatus.textContent = myColor === "Red" ? "🏆 BẠN CHIẾN THẮNG" : "❌ ĐỐI THỦ CHIẾN THẮNG";
        if (turnIndicator) turnIndicator.textContent = "🏆";
        return;
      }
      if (gameStatus === "BlackWins") {
        turnStatus.textContent = myColor === "Black" ? "🏆 BẠN CHIẾN THẮNG" : "❌ ĐỐI THỦ CHIẾN THẮNG";
        if (turnIndicator) turnIndicator.textContent = "🏆";
        return;
      }
      if (isCheck) {
        turnStatus.textContent = `⚠️ CHIẾU TƯỚNG! (${isMyTurn ? "Lượt của BẠN" : "Lượt ĐỐI THỦ"})`;
        if (turnIndicator) turnIndicator.textContent = "⚠️";
        return;
      }
      turnStatus.textContent = isMyTurn
        ? `${icon} Lượt của BẠN (${pieceName})`
        : `${icon} Lượt của ĐỐI THỦ (${pieceName})`;
      if (turnIndicator) turnIndicator.textContent = icon;
      return;
    }

    if (gameStatus === "Draw") {
      turnStatus.textContent = "🤝 VÁN CỜ HÒA";
      if (turnIndicator) {
        turnIndicator.textContent = "🤝";
      }
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
  // POSITION & ORIENTATION
  // =====================================================

  function isBoardFlipped() {
    const manualFlipped = localStorage.getItem("manualBoardFlipped");
    if (manualFlipped !== null) {
      return manualFlipped === "true";
    }
    const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
    if (isOnline) {
      return localStorage.getItem("onlinePlayerColor") === "Black";
    }
    return false;
  }

  function updateBoardOrientation() {
    const boardColumn = document.querySelector(".board-column");
    const flipped = isBoardFlipped();
    if (boardColumn) {
      boardColumn.classList.toggle("flipped", flipped);
    }
    const riverLeft = document.getElementById("river-text-left");
    const riverRight = document.getElementById("river-text-right");
    if (riverLeft && riverRight) {
      riverLeft.textContent = flipped ? "漢 界" : "楚 河";
      riverRight.textContent = flipped ? "楚 河" : "漢 界";
    }
  }

  function getPosition(row, col) {
    const flipped = isBoardFlipped();
    const r = flipped ? 9 - row : row;
    const c = flipped ? 8 - col : col;
    return {
      x: START_X + c * CELL_SIZE,
      y: START_Y + r * CELL_SIZE,
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

      const isOnlineMode = localStorage.getItem("isOnlineMode") === "true";
      if (isOnlineMode) {
          const myColor = localStorage.getItem("onlinePlayerColor");
          if (currentTurn !== myColor) {
              return; 
          }
      }

      const piece = getPieceData(pieceGroup);

      // CAPTURE

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

      // TURN

      if (piece.color.toLowerCase() !== currentTurn.toLowerCase()) {
        console.log("Chưa tới lượt quân này");

        return;
      }

      // REMOVE OLD

      if (selectedPieceElement) {
        selectedPieceElement.classList.remove("selected");
      }

      clearMoveHighlights();

      // SELECT

      pieceGroup.classList.add("selected");

      selectedPieceElement = pieceGroup;

      selectedPieceData = piece;

      // GET MOVES

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

    if (!selectedPieceData) return;

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
        if (errorData?.game) {
          updateGameFromApi(errorData.game);
        }
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
      resignModal.classList.remove("hidden");
    }
  }

  function requestExitFromGame(action) {
    const isActiveGame = gameContent &&
      !gameContent.classList.contains("hidden") &&
      gameStatus === "InProgress";

    if (!isActiveGame) {
      action();
      return;
    }

    pendingExitAction = action;
    showResignModal();
  }

  async function completePendingExitAction() {
    const action = pendingExitAction;
    pendingExitAction = null;

    if (resultNewGameButton) resultNewGameButton.classList.remove("hidden");
    if (resultCloseButton) resultCloseButton.textContent = "Đóng";

    if (action) await action();
  }

  function closeResignModal() {
    if (resignModal) {
      resignModal.classList.add("hidden");
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

      if (pendingExitAction) {
        const winner = data.status === "RedWins" ? "Quân Đỏ" : "Quân Đen";
        const resultMessage = document.getElementById("result-message");
        if (resultMessage) resultMessage.textContent = `Bạn đã đầu hàng. ${winner} giành chiến thắng!`;
        if (resultNewGameButton) resultNewGameButton.classList.add("hidden");
        if (resultCloseButton) resultCloseButton.textContent = "Tiếp tục";
      }
    } catch (error) {
      console.error("Lỗi đầu hàng:", error);

      alert(error.message);
    }
  }

  // =====================================================
  // DRAW (XIN HÒA)
  // =====================================================

  let drawOfferTimer = null;

  function openConfirmDrawModal() {
    if (gameStatus !== "InProgress") {
      return;
    }
    if (confirmDrawModal) {
      confirmDrawModal.classList.remove("hidden");
    }
  }

  function closeConfirmDrawModal() {
    if (confirmDrawModal) {
      confirmDrawModal.classList.add("hidden");
    }
  }

  async function sendDrawOffer() {
    if (!gameId || gameStatus !== "InProgress") {
      closeConfirmDrawModal();
      return;
    }

    try {
      closeConfirmDrawModal();

      const response = await fetch(`${API_BASE_URL}/${gameId}/draw/offer`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
      });

      const data = await response.json().catch(() => ({}));

      if (!response.ok) {
        alert(data.message || "Không thể gửi yêu cầu xin hòa.");
        return;
      }

      if (data.accepted === true) {
        showGameNotification("🤝", "HÒA CỜ", data.message || "Ván cờ kết thúc với kết quả hòa!", 3000);
        if (data.game) {
          updateGameFromApi(data.game);
        }
      } else if (data.accepted === false) {
        alert(data.message || "Máy đã từ chối lời xin hòa. Hãy tiếp tục thi đấu!");
      } else {
        showGameNotification("🤝", "XIN HÒA", data.message || "Đã gửi đề nghị hòa. Chờ đối thủ trong 15s...", 3500);
      }
    } catch (error) {
      console.error("Lỗi gửi đề nghị xin hòa:", error);
      alert("Lỗi kết nối khi gửi đề nghị xin hòa.");
    }
  }

  function showDrawOfferModal(timeoutSec = 15) {
    if (drawOfferTimer) {
      clearInterval(drawOfferTimer);
      drawOfferTimer = null;
    }

    if (drawOfferModal) {
      drawOfferModal.classList.remove("hidden");
    }

    let remaining = timeoutSec;
    if (drawOfferCountdown) {
      drawOfferCountdown.textContent = `Tự động từ chối sau: ${remaining}s`;
    }

    drawOfferTimer = setInterval(async () => {
      remaining--;
      if (drawOfferCountdown) {
        drawOfferCountdown.textContent = `Tự động từ chối sau: ${remaining}s`;
      }
      if (remaining <= 0) {
        clearInterval(drawOfferTimer);
        drawOfferTimer = null;
        closeDrawOfferModal();
        await respondDraw(false);
      }
    }, 1000);
  }

  function closeDrawOfferModal() {
    if (drawOfferTimer) {
      clearInterval(drawOfferTimer);
      drawOfferTimer = null;
    }
    if (drawOfferModal) {
      drawOfferModal.classList.add("hidden");
    }
  }

  async function respondDraw(isAccepted) {
    closeDrawOfferModal();
    if (!gameId || gameStatus !== "InProgress") {
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/${gameId}/draw/respond`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          accepted: isAccepted,
          isAccepted: isAccepted,
        }),
      });

      const data = await response.json().catch(() => ({}));

      if (!response.ok) {
        alert(data.message || "Không thể phản hồi yêu cầu xin hòa.");
        return;
      }

      if (isAccepted && data.game) {
        updateGameFromApi(data.game);
      }
    } catch (error) {
      console.error("Lỗi phản hồi xin hòa:", error);
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
      isOnlineMode = false;
      localStorage.removeItem("isOnlineMode");
      localStorage.removeItem("onlinePlayerColor");
      localStorage.removeItem("onlineOpponentName");
      if (onlineSelection) { onlineSelection.style.display = "none"; onlineSelection.classList.add("hidden"); }
      if (startGameButton) { startGameButton.style.display = "block"; startGameButton.classList.remove("hidden"); }
      selectGameMode(false);
    });
  }

  if (playerVsComputerButton) {
    playerVsComputerButton.addEventListener("click", function (event) {
      event.stopPropagation();
      isOnlineMode = false;
      localStorage.removeItem("isOnlineMode");
      localStorage.removeItem("onlinePlayerColor");
      localStorage.removeItem("onlineOpponentName");
      if (onlineSelection) { onlineSelection.style.display = "none"; onlineSelection.classList.add("hidden"); }
      if (startGameButton) { startGameButton.style.display = "block"; startGameButton.classList.remove("hidden"); }
      selectGameMode(true);
    });
  }

  if (playerVsOnlineButton) {
    playerVsOnlineButton.addEventListener("click", function (event) {
      event.stopPropagation();
      if (!isAuthenticated) {
        alert("Bạn cần đăng nhập để chơi online.");
        window.location.href = "/Auth/Login?returnUrl=%2FGame";
        return;
      }
      isOnlineMode = true;
      gameModeSelected = true;
      VS_COMPUTER = false;
      
      if (playerVsPlayerButton) playerVsPlayerButton.classList.remove("selected");
      if (playerVsComputerButton) playerVsComputerButton.classList.remove("selected");
      playerVsOnlineButton.classList.add("selected");
      
      if (selectedModeText) selectedModeText.textContent = "🌐 Online";
      if (difficultySelection) { difficultySelection.style.display = "none"; difficultySelection.classList.add("hidden"); }
      if (selectedDifficultyContainer) { selectedDifficultyContainer.style.display = "none"; selectedDifficultyContainer.classList.add("hidden"); }
      
      if (onlineSelection) { onlineSelection.style.display = "block"; onlineSelection.classList.remove("hidden"); }
      if (startGameButton) { startGameButton.style.display = "none"; startGameButton.classList.add("hidden"); }
    });
  }

  async function createOnlineGame() {
      isCreatingGame = true;
      try {
          const response = await fetch(API_BASE_URL + "/online", { method: "POST" });
          if (response.status === 401) {
              window.location.href = "/Auth/Login?returnUrl=%2FGame";
              return;
          }
          if (!response.ok) throw new Error("Không thể tạo bàn online.");
          const data = await response.json();
          gameId = data.id;
          localStorage.setItem("xiangqiGameId", gameId);
          localStorage.setItem("onlinePlayerColor", "Red");
          localStorage.setItem("isOnlineMode", "true");
          localStorage.removeItem("onlineOpponentName");
          localStorage.removeItem("manualBoardFlipped");
          isOnlineMode = true;
          updateBoardOrientation();
          updateGameFromApi(data);

          modeSelection.classList.add("hidden");
          gameContent.classList.remove("hidden");
          const roomBox = document.getElementById("room-info-box");
          const roomCodeDisplay = document.getElementById("display-room-code");
          if (roomBox && roomCodeDisplay) {
              roomBox.classList.remove("hidden");
              roomCodeDisplay.textContent = data.roomCode;
          }
          const invitePanel = document.getElementById("opponent-invite-panel");
          if (invitePanel) invitePanel.classList.remove("hidden");
          if (invitePlayerStatus) invitePlayerStatus.textContent = "Bàn đang chờ đối thủ tham gia.";
      } catch (e) {
          console.error(e);
          alert(e.message || "Lỗi tạo bàn.");
      } finally {
          isCreatingGame = false;
      }
  }

  if (invitePlayerButton) {
      invitePlayerButton.addEventListener("click", async function (event) {
          event.stopPropagation();
          const playerId = opponentPlayerIdInput.value.trim();
          if (!playerId) {
              alert("Vui lòng nhập ID người chơi cần mời.");
              opponentPlayerIdInput.focus();
              return;
          }

          invitePlayerButton.disabled = true;
          if (invitePlayerStatus) invitePlayerStatus.textContent = "Đang kiểm tra ID người chơi...";
          try {
              const response = await fetch(`${API_BASE_URL}/${gameId}/invite`, {
                  method: "POST",
                  headers: { "Content-Type": "application/json" },
                  body: JSON.stringify({ playerId })
              });
              const result = await response.json();
              if (response.status === 401) {
                  window.location.href = "/Auth/Login?returnUrl=%2FGame";
                  return;
              }
              if (!response.ok) throw new Error(result.message || "Không thể mời người chơi.");

              localStorage.setItem("onlineOpponentName", result.username);
              if (result.game) updateGameFromApi(result.game);
              if (invitePlayerStatus) {
                  invitePlayerStatus.textContent = `Đã thêm ${result.username} (${result.playerId}) vào bàn.`;
              }
              opponentPlayerIdInput.value = "";
          } catch (e) {
              if (invitePlayerStatus) invitePlayerStatus.textContent = e.message || "Không thể mời người chơi.";
          } finally {
              invitePlayerButton.disabled = false;
          }
      });
  }

  if (createOnlineButton) {
      createOnlineButton.addEventListener("click", function (event) {
          event.stopPropagation();
          createOnlineGame();
      });
  }

  if (joinOnlineButton) {
      joinOnlineButton.addEventListener("click", async function (event) {
          event.stopPropagation();
          const roomId = onlineRoomIdInput.value.trim();
          if (!roomId) {
              alert("Vui lòng nhập mã bàn đối thủ cung cấp.");
              onlineRoomIdInput.focus();
              return;
          }

          try {
              const response = await fetch(API_BASE_URL + "/" + encodeURIComponent(roomId) + "/join", { method: "POST" });
              if (response.status === 401) {
                  window.location.href = "/Auth/Login?returnUrl=%2FGame";
                  return;
              }
              if (!response.ok) {
                  const error = await response.json();
                  throw new Error(error.message || "Không thể vào bàn.");
              }
              const joinResult = await response.json();
              const data = joinResult.game;
              gameId = data.id;
              localStorage.setItem("xiangqiGameId", gameId);
              localStorage.setItem("onlinePlayerColor", "Black");
              localStorage.setItem("isOnlineMode", "true");
              localStorage.removeItem("manualBoardFlipped");
              localStorage.setItem("onlineOpponentName", joinResult.opponentUsername || "Đối thủ");
              isOnlineMode = true;
              updateBoardOrientation();
              updateGameFromApi(data);

              modeSelection.classList.add("hidden");
              gameContent.classList.remove("hidden");
              const roomBox = document.getElementById("room-info-box");
              const roomCodeDisplay = document.getElementById("display-room-code");
              if (roomBox && roomCodeDisplay) {
                  roomBox.classList.remove("hidden");
                  roomCodeDisplay.textContent = data.roomCode;
              }
              const invitePanel = document.getElementById("opponent-invite-panel");
              if (invitePanel) invitePanel.classList.add("hidden");
          } catch (e) {
              console.error(e);
              alert(e.message || "Không tìm thấy bàn hoặc lỗi kết nối.");
          }
      });
  }

  // =====================================================
  // DIFFICULTY BUTTONS
  // =====================================================

  difficultyButtons.forEach(function (button) {
    button.addEventListener("click", function (event) {
      event.stopPropagation();

      const difficulty = parseInt(button.dataset.difficulty, 10);

      selectDifficulty(difficulty);
    });
  });

  // =====================================================
  // START GAME
  // =====================================================

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

      requestExitFromGame(async function () {
        await startNewGame();
      });
    });
  }

  // =====================================================
  // CHANGE MODE
  // =====================================================

  if (changeModeButton) {
    changeModeButton.addEventListener("click", function (event) {
      event.stopPropagation();

      requestExitFromGame(backToModeSelection);
    });
  }

  // =====================================================
  // FLIP BOARD
  // =====================================================

  const flipBoardButton = document.getElementById("flip-board-btn");
  if (flipBoardButton) {
    flipBoardButton.addEventListener("click", function (event) {
      event.stopPropagation();
      const nextFlipped = !isBoardFlipped();
      localStorage.setItem("manualBoardFlipped", nextFlipped.toString());
      clearSelection();
      clearMoveHighlights();
      updateBoardOrientation();
      renderPieces();
      updatePlayerNames();
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
    resultCloseButton.addEventListener("click", async function (event) {
      event.stopPropagation();

      closeGameResult();
      await completePendingExitAction();
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

      pendingExitAction = null;
      closeResignModal();
    });
  }

  // =====================================================
  // DRAW (XIN HÒA) LISTENERS
  // =====================================================

  if (drawButton) {
    drawButton.addEventListener("click", function (event) {
      event.stopPropagation();
      openConfirmDrawModal();
    });
  }

  if (confirmDrawSubmitBtn) {
    confirmDrawSubmitBtn.addEventListener("click", async function (event) {
      event.stopPropagation();
      await sendDrawOffer();
    });
  }

  if (confirmDrawCancelBtn) {
    confirmDrawCancelBtn.addEventListener("click", function (event) {
      event.stopPropagation();
      closeConfirmDrawModal();
    });
  }

  if (acceptDrawBtn) {
    acceptDrawBtn.addEventListener("click", async function (event) {
      event.stopPropagation();
      await respondDraw(true);
    });
  }

  if (declineDrawBtn) {
    declineDrawBtn.addEventListener("click", async function (event) {
      event.stopPropagation();
      await respondDraw(false);
    });
  }

  document.querySelectorAll(".sidebar-nav a.nav-item[href]").forEach(function (link) {
    link.addEventListener("click", function (event) {
      const destination = new URL(link.href, window.location.href);
      if (destination.pathname === window.location.pathname || destination.hash === "#") return;

      event.preventDefault();
      event.stopPropagation();
      const href = link.href;
      requestExitFromGame(function () {
        window.location.assign(href);
      });
    });
  });

  window.requestLogoutWithResign = function () {
      const logoutModal = document.getElementById("logout-modal");
      if (logoutModal) logoutModal.classList.remove("show");

      requestExitFromGame(function () {
        document.getElementById("logout-form")?.submit();
      });
  };

  // =====================================================
  // UPDATE PLAYER STATUS
  // =====================================================

  function updatePlayerStatus() {
    const redStatus = document.getElementById("red-player-status");

    const blackStatus = document.getElementById("black-player-status");

    if (!redStatus || !blackStatus) {
      return;
    }

    if (gameStatus === "WaitingForPlayers") {
      redStatus.textContent = "🟠 Đang chờ đối thủ";
      blackStatus.textContent = "⚪ Chờ tham gia";
      return;
    }

    // ==============================================
    // VS COMPUTER
    // ==============================================

    if (VS_COMPUTER) {
      if (gameStatus !== "InProgress") {
        if (gameStatus === "Draw") {
          redStatus.textContent = "🤝 Hòa cờ";
          blackStatus.textContent = "🤝 Hòa cờ";
          return;
        }

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
      if (gameStatus === "Draw") {
        redStatus.textContent = "🤝 Hòa cờ";
        blackStatus.textContent = "🤝 Hòa cờ";
        return;
      }

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

  function showGameNotification(icon, title, message, duration = 2500) {
    const overlay = document.getElementById("check-overlay");
    const iconElement = document.getElementById("check-overlay-icon");
    const titleElement = document.getElementById("check-overlay-title");
    const messageElement = document.getElementById("check-overlay-message");

    if (!overlay) {
      return;
    }

    if (iconElement) {
      iconElement.textContent = icon;
    }

    if (titleElement) {
      titleElement.textContent = title;
    }

    if (messageElement) {
      messageElement.textContent = message;
    }

    overlay.classList.remove("hidden");

    setTimeout(() => {
      overlay.classList.add("hidden");
    }, duration);
  }

  // =====================================================
  // START
  // =====================================================

  initializePage();
});

  // =====================================================
  // EVALUATION BAR
  // =====================================================
  
  async function updateEvalBar() {
      if (!gameId) return;
      try {
          const response = await fetch(API_BASE_URL + "/" + gameId + "/eval");
          if (response.ok) {
              const data = await response.json();
              const evalScore = data.eval;
              
              const evalBarFill = document.getElementById("eval-bar-fill");
              const evalBarText = document.getElementById("eval-bar-text");
              
              if (evalBarFill && evalBarText) {
                  const maxDisplayScore = 1000;
                  let percentage = 50 + (evalScore / maxDisplayScore) * 50;
                  percentage = Math.max(0, Math.min(100, percentage));
                  evalBarFill.style.height = (100 - percentage) + "%";
                  evalBarFill.style.backgroundColor = "#0f172a"; 
                  evalBarText.textContent = (evalScore / 100).toFixed(1);
              }
          }
      } catch (err) {
          console.error("Eval Error:", err);
      }
  }

document.addEventListener("DOMContentLoaded", function() {
    const copyBtn = document.getElementById("copy-room-code-btn");
    if (copyBtn) {
        copyBtn.addEventListener("click", function() {
            const code = document.getElementById("display-room-code").textContent;
            navigator.clipboard.writeText(code).then(() => {
                const originalText = copyBtn.innerHTML;
                copyBtn.innerHTML = '<i class="fa-solid fa-check"></i> Đã chép';
                copyBtn.style.color = '#10b981';
                setTimeout(() => {
                    copyBtn.innerHTML = originalText;
                    copyBtn.style.color = '';
                }, 2000);
            });
        });
    }
});
