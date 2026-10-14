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

      connection.on("ReceiveChatMessage", function (data) {
          if (typeof gameId !== 'undefined' && data && data.gameId == gameId) {
              appendChatMessage(data.sender, data.message, data.sentAt);
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

  let computerDifficulty = 1;

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

  const undoButton = document.getElementById("undo-btn");
  const moveHistoryContainer = document.getElementById("move-history-container");
  const resignButton = document.getElementById("resign-btn");

  const resultNewGameButton = document.getElementById("result-new-game-btn");

  const resultCloseButton = document.getElementById("result-close-btn");

  const confirmResignButton = document.getElementById("confirm-resign-btn");

  const cancelResignButton = document.getElementById("cancel-resign-btn");

  const gameResultModal = document.getElementById("game-result-modal");

  const resignModal = document.getElementById("resign-modal");

  const drawButton = document.getElementById("draw-btn");
  const hintButton = document.getElementById("hint-btn");
  const quickSoundBtn = document.getElementById("quick-sound-btn");
  const onlineChatBox = document.getElementById("online-chat-box");
  const arenaRoomToggle = document.getElementById("arena-room-toggle");
  arenaRoomToggle?.addEventListener("click", () => {
    const open = arenaRoomToggle.getAttribute("aria-expanded") !== "true";
    arenaRoomToggle.setAttribute("aria-expanded", String(open));
    document.getElementById("room-info-box")?.classList.toggle("is-open", open);
  });
  document.addEventListener("keydown", event => {
    if (event.key === "Escape") {
      document.getElementById("room-info-box")?.classList.remove("is-open");
      arenaRoomToggle?.setAttribute("aria-expanded", "false");
    }
  });
  const arenaChatToggle = document.getElementById("arena-chat-toggle");
  arenaChatToggle?.addEventListener("click", () => {
    const open = arenaChatToggle.getAttribute("aria-expanded") !== "true";
    arenaChatToggle.setAttribute("aria-expanded", String(open));
    onlineChatBox?.classList.toggle("hidden", !open);
  });
  const chatMessagesContainer = document.getElementById("chat-messages");
  const chatInput = document.getElementById("chat-input");
  const sendChatBtn = document.getElementById("send-chat-btn");
  const openRoomsList = document.getElementById("open-rooms-list");
  const refreshRoomsBtn = document.getElementById("refresh-rooms-btn");
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

  const MAX_UNDO_LIMIT = 3;

  let undoCount = 0;

  let previousMovesCount = 0;

  let previousPieceCount = 0;

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
      difficulty = 1;
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

    updateActionButtonsVisibility();
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

    undoCount = 0;

    clearSelection();

    if (modeSelection) {
      modeSelection.classList.add("hidden");
    }

    if (gameContent) {
      gameContent.classList.remove("hidden");
    }

    updateBoardOrientation();
    updatePlayerNames();
    updateActionButtonsVisibility();

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

    undoCount = 0;

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

    // Mặc định lại Dễ
    selectDifficulty(1);
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

    // Mặc định Độ khó Dễ (Depth 1)
    selectDifficulty(1);

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

  // Presentation is isolated in arena-ui.js; game logic keeps one snapshot entry point.
  function updateEvaluationPanel(data) {
    window.ArenaUI.update(data);
    gameEndMessage = data.endReason === "Checkmate"
      ? "Chiếu bí! Bên đến lượt không còn nước đi hợp lệ và bị xử thua."
      : data.endReason === "NoLegalMoves" ? "Hết nước đi hợp lệ! Bên đến lượt bị xử thua." : "";
  }

  let gameEndMessage = "";

  function updateGameFromApi(data) {
    if (!data) {
      return;
    }

    gameId = data.id;
    joinSignalRGroup(gameId);

    currentTurn = data.currentTurn;

    gameStatus = data.status;
    updateEvaluationPanel(data);

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

    updateActionButtonsVisibility();

    checkGameResult();
    
    // Phát âm thanh khi có nước đi mới
    const currentMovesCount = (data.moves || []).length;
    const currentPieceCount = (data.boardPieces || []).length;
    if (currentMovesCount > previousMovesCount && previousMovesCount > 0) {
      if (isCheck) {
        playSound('check');
      } else if (currentPieceCount < previousPieceCount) {
        playSound('capture');
      } else {
        playSound('move');
      }
    } else if (isCheck && gameStatus === "InProgress") {
      playSound('check');
    }
    previousMovesCount = currentMovesCount;
    previousPieceCount = currentPieceCount;

    renderMoveHistory(data.moves);

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
    updatePlayerStatus();
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

    if (title.includes("CHIẾN THẮNG")) {
      playSound('win');
    } else if (title.includes("THẤT BẠI")) {
      playSound('loss');
    }

    const resultTitle = document.getElementById("result-title");

    const winnerName = document.getElementById("winner-name");

    const resultMessage = document.getElementById("result-message");

    if (!resultTitle || !winnerName || !resultMessage) {
      return;
    }

    resultTitle.textContent = title;
    winnerName.textContent = winner;
    resultMessage.textContent = gameEndMessage || message;

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
    const label = document.getElementById("turn-status");
    if (!label) return;
    if (gameStatus === "WaitingForPlayers") { label.textContent = "Đang chờ đối thủ tham gia"; return; }
    if (gameStatus === "Draw") { label.textContent = "Ván cờ kết thúc hòa"; return; }
    if (gameStatus === "RedWins" || gameStatus === "BlackWins") {
      label.textContent = gameStatus === "RedWins" ? "Quân Đỏ chiến thắng" : "Quân Đen chiến thắng";
      return;
    }
    const side = currentTurn === "Red" ? "Đỏ" : "Đen";
    const online = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
    const mine = online ? currentTurn === localStorage.getItem("onlinePlayerColor") : VS_COMPUTER && currentTurn === "Red";
    const who = online ? (mine ? "bạn" : "đối thủ") : VS_COMPUTER ? (mine ? "bạn" : "máy") : `quân ${side}`;
    label.textContent = isCheck ? `Chiếu tướng · Lượt ${who}` : `Lượt của ${who} · ${side}`;
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
    if (piecesLayer) {
      piecesLayer.querySelectorAll(".hint-piece").forEach(function (el) {
        el.classList.remove("hint-piece");
      });
    }
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

  async function startNewGame() {
    try {
      closeGameResult();
      closeResignModal();
      closeConfirmDrawModal();
      closeDrawOfferModal();

      const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
      if (isOnline) {
        // Nếu vừa chơi ván online xong mà bấm ván mới, đưa về màn hình chọn chế độ hoặc tạo lại phòng
        backToModeSelection();
        return;
      }

      // Đảm bảo chế độ chơi hợp lệ nếu reload trang
      if (!gameModeSelected) {
        gameModeSelected = true;
      }

      localStorage.removeItem("xiangqiGameId");
      localStorage.removeItem("manualBoardFlipped");

      gameId = null;
      currentTurn = null;
      gameStatus = null;
      isCheck = false;
      isComputerThinking = false;
      undoCount = 0;
      previousMovesCount = 0;
      previousPieceCount = 0;

      clearSelection();
      clearMoveHighlights();
      updateBoardOrientation();
      updateUndoButtonUI();

      if (piecesLayer) {
        piecesLayer.innerHTML = "";
      }

      if (moveHistoryContainer) {
        moveHistoryContainer.innerHTML = '<div class="empty-history">Chưa có nước đi nào</div>';
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
  // MOVE HISTORY RENDERING & NOTATION
  // =====================================================

  const PIECE_NAMES_VI = {
    "King": "Tướng",
    "Advisor": "Sĩ",
    "Elephant": "Tượng",
    "Horse": "Mã",
    "Rook": "Xe",
    "Cannon": "Pháo",
    "Pawn": "Tốt"
  };

  function renderMoveHistory(moves) {
    window.ArenaUI.renderHistory(moves);
  }

  // =====================================================
  // UNDO (ĐI LẠI)
  // =====================================================

  let isUndoing = false;

  async function handleUndo() {
    const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
    if (isOnline) {
      alert("Chế độ chơi Online không hỗ trợ chức năng đi lại!");
      return;
    }

    if (!gameId || gameStatus !== "InProgress") {
      alert("Chỉ có thể xin đi lại khi ván cờ đang diễn ra!");
      return;
    }

    if (undoCount >= MAX_UNDO_LIMIT) {
      alert(`Bạn đã sử dụng hết số lần đi lại cho phép trong ván này (tối đa ${MAX_UNDO_LIMIT} lần)!`);
      return;
    }

    if (isComputerThinking) {
      alert("Máy đang tính nước đi, vui lòng đợi trong giây lát!");
      return;
    }

    if (isUndoing) return;

    try {
      isUndoing = true;
      const isPvE = VS_COMPUTER;
      const response = await fetch(`${API_BASE_URL}/${gameId}/undo?isPvE=${isPvE}`, {
        method: "POST"
      });

      if (!response.ok) {
        const err = await response.json().catch(() => null);
        throw new Error(err?.message || "Không thể đi lại vào lúc này.");
      }

      const data = await response.json();
      undoCount++;
      clearSelection();
      updateGameFromApi(data);
      updateUndoButtonUI();
    } catch (error) {
      console.error("Lỗi xin đi lại:", error);
      alert(error.message);
    } finally {
      isUndoing = false;
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
      loadOpenRooms();
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
  // UNDO (ĐI LẠI) LISTENER
  // =====================================================

  if (undoButton) {
    undoButton.addEventListener("click", async function (event) {
      event.stopPropagation();
      await handleUndo();
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
    window.ArenaUI.updatePlayerState({
      status: gameStatus, turn: currentTurn, check: isCheck,
      online: isOnlineMode, computer: VS_COMPUTER, thinking: isComputerThinking
    });
  }

  // =====================================================
  // ACTION BUTTONS VISIBILITY & UNDO UI
  // =====================================================

  function updateUndoButtonUI() {
    if (!undoButton) return;
    const remaining = Math.max(0, MAX_UNDO_LIMIT - undoCount);
    const span = undoButton.querySelector("span");
    if (span) {
      span.textContent = `Đi lại (${remaining}/${MAX_UNDO_LIMIT})`;
    }
    if (remaining <= 0) {
      undoButton.disabled = true;
      undoButton.style.opacity = "0.5";
      undoButton.style.cursor = "not-allowed";
      undoButton.title = "Đã hết lượt đi lại trong ván này";
    } else {
      undoButton.disabled = false;
      undoButton.style.opacity = "1";
      undoButton.style.cursor = "pointer";
      undoButton.title = `Đi lại (${remaining} lượt còn lại)`;
    }
  }

  function updateActionButtonsVisibility() {
    const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";

    if (isOnline) {
      // Chơi Online: Xóa/ẩn Đi lại, ẩn Gợi ý; Giữ Xin hòa và Đầu hàng; Hiện Chat box
      if (undoButton) undoButton.style.display = "none";
      if (hintButton) hintButton.style.display = "none";
      if (drawButton) drawButton.style.display = "flex";
      if (resignButton) resignButton.style.display = "flex";
      const chatToggle = document.getElementById("arena-chat-toggle");
      if (chatToggle) chatToggle.classList.remove("hidden");
      if (onlineChatBox && chatToggle?.getAttribute("aria-expanded") !== "true") onlineChatBox.classList.add("hidden");
    } else if (VS_COMPUTER) {
      // Chơi với máy: Có Đi lại, có Gợi ý, có Đầu hàng, ẩn Xin hòa; Ẩn Chat box
      if (undoButton) undoButton.style.display = "flex";
      if (hintButton) hintButton.style.display = "flex";
      if (drawButton) drawButton.style.display = "none";
      if (resignButton) resignButton.style.display = "flex";
      if (onlineChatBox) onlineChatBox.classList.add("hidden");
      arenaChatToggle?.classList.add("hidden");
      arenaChatToggle?.setAttribute("aria-expanded", "false");
      updateUndoButtonUI();
    } else {
      // Người vs Người trên cùng máy: Có Đi lại, có Gợi ý; Bỏ Đầu hàng và Xin hòa; Ẩn Chat box
      if (undoButton) undoButton.style.display = "flex";
      if (hintButton) hintButton.style.display = "flex";
      if (drawButton) drawButton.style.display = "none";
      if (resignButton) resignButton.style.display = "none";
      if (onlineChatBox) onlineChatBox.classList.add("hidden");
      arenaChatToggle?.classList.add("hidden");
      arenaChatToggle?.setAttribute("aria-expanded", "false");
      updateUndoButtonUI();
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
  // WEB AUDIO SOUND EFFECTS (SYNTHESIZED CHESS SOUNDS)
  // =====================================================
  let audioCtx = null;

  function isSoundEnabled() {
    return localStorage.getItem('chess_sound') !== 'false';
  }

  function getAudioContext() {
    if (!audioCtx) {
      const AudioContextClass = window.AudioContext || window.webkitAudioContext;
      if (AudioContextClass) {
        audioCtx = new AudioContextClass();
      }
    }
    if (audioCtx && audioCtx.state === 'suspended') {
      audioCtx.resume();
    }
    return audioCtx;
  }

  function playSound(type) {
    if (!isSoundEnabled()) return;
    try {
      const ctx = getAudioContext();
      if (!ctx) return;
      const now = ctx.currentTime;

      if (type === 'move') {
        // Tiếng gõ quân cờ gỗ dứt khoát
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(320, now);
        osc.frequency.exponentialRampToValueAtTime(120, now + 0.08);
        gain.gain.setValueAtTime(0.5, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.08);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start(now);
        osc.stop(now + 0.08);
      } else if (type === 'capture') {
        // Tiếng ăn quân đanh giòn
        const osc1 = ctx.createOscillator();
        const osc2 = ctx.createOscillator();
        const gain = ctx.createGain();
        osc1.type = 'sine';
        osc2.type = 'square';
        osc1.frequency.setValueAtTime(450, now);
        osc1.frequency.exponentialRampToValueAtTime(140, now + 0.12);
        osc2.frequency.setValueAtTime(220, now);
        osc2.frequency.exponentialRampToValueAtTime(80, now + 0.12);
        gain.gain.setValueAtTime(0.6, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.12);
        osc1.connect(gain);
        osc2.connect(gain);
        gain.connect(ctx.destination);
        osc1.start(now);
        osc2.start(now);
        osc1.stop(now + 0.12);
        osc2.stop(now + 0.12);
      } else if (type === 'check') {
        // Âm thanh cảnh báo chiếu tướng sắc bén
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(700, now);
        osc.frequency.exponentialRampToValueAtTime(880, now + 0.15);
        gain.gain.setValueAtTime(0.4, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.25);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start(now);
        osc.stop(now + 0.25);
      } else if (type === 'win') {
        // Âm thanh chiến thắng tươi vui
        [523.25, 659.25, 783.99, 1046.50].forEach((freq, i) => {
          const osc = ctx.createOscillator();
          const gain = ctx.createGain();
          osc.type = 'triangle';
          osc.frequency.setValueAtTime(freq, now + i * 0.1);
          gain.gain.setValueAtTime(0.3, now + i * 0.1);
          gain.gain.exponentialRampToValueAtTime(0.01, now + i * 0.1 + 0.25);
          osc.connect(gain);
          gain.connect(ctx.destination);
          osc.start(now + i * 0.1);
          osc.stop(now + i * 0.1 + 0.25);
        });
      } else if (type === 'loss') {
        // Âm thanh thất bại
        [440, 415.3, 392, 349.2].forEach((freq, i) => {
          const osc = ctx.createOscillator();
          const gain = ctx.createGain();
          osc.type = 'sawtooth';
          osc.frequency.setValueAtTime(freq, now + i * 0.15);
          gain.gain.setValueAtTime(0.3, now + i * 0.15);
          gain.gain.exponentialRampToValueAtTime(0.01, now + i * 0.15 + 0.3);
          osc.connect(gain);
          gain.connect(ctx.destination);
          osc.start(now + i * 0.15);
          osc.stop(now + i * 0.15 + 0.3);
        });
      }
    } catch (e) {
      console.warn('Lỗi phát âm thanh:', e);
    }
  }

  // Cập nhật trạng thái icon loa nhanh
  function updateQuickSoundBtnUI() {
    if (!quickSoundBtn) return;
    const enabled = isSoundEnabled();
    quickSoundBtn.innerHTML = enabled
      ? '<i class="fa-solid fa-volume-high"></i>'
      : '<i class="fa-solid fa-volume-xmark" style="color: var(--text-muted);"></i>';
    quickSoundBtn.title = enabled ? 'Đang bật âm thanh (Bấm để tắt)' : 'Đang tắt âm thanh (Bấm để bật)';
  }

  if (quickSoundBtn) {
    quickSoundBtn.addEventListener('click', function(e) {
      e.stopPropagation();
      const current = isSoundEnabled();
      localStorage.setItem('chess_sound', (!current).toString());
      updateQuickSoundBtnUI();
      if (!current) playSound('move');
    });
    updateQuickSoundBtnUI();
  }

  // =====================================================
  // AI HINT (GỢI Ý NƯỚC ĐI TỐI ƯU)
  // =====================================================
  let isHinting = false;

  async function handleGetHint() {
    if (!gameId || gameStatus !== "InProgress") {
      alert("Chỉ có thể lấy gợi ý khi ván cờ đang diễn ra!");
      return;
    }

    if (isComputerThinking) {
      alert("Máy đang tính nước đi, vui lòng đợi trong giây lát!");
      return;
    }

    if (isHinting) return;

    try {
      isHinting = true;
      if (hintButton) {
        hintButton.disabled = true;
        hintButton.style.opacity = "0.6";
      }

      const res = await fetch(`${API_BASE_URL}/${gameId}/hint`);
      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.message || "Không thể tìm nước đi gợi ý.");
      }

      const hintData = await res.json();
      console.log("Nước đi gợi ý:", hintData);

      // Hiển thị gợi ý trực quan trên bàn cờ
      highlightHintMove(hintData);
      playSound('move');
    } catch (err) {
      console.error("Lỗi lấy gợi ý:", err);
      alert(err.message || "Lỗi lấy gợi ý.");
    } finally {
      isHinting = false;
      if (hintButton) {
        hintButton.disabled = false;
        hintButton.style.opacity = "1";
      }
    }
  }

  function highlightHintMove(hintData) {
    clearSelection();
    clearMoveHighlights();

    const fromX = hintData.fromX;
    const fromY = hintData.fromY;
    const toX = hintData.toX;
    const toY = hintData.toY;

    // Tìm quân cờ xuất phát
    const pieceObj = initialPieces.find(p => p.row === fromX && p.col === fromY);
    const pieceName = pieceObj ? pieceObj.type : (PIECE_NAMES_VI[hintData.pieceType] || "Quân");
    const pieceColorName = (pieceObj?.color === "red" || hintData.pieceColor === "Red") ? "Đỏ" : "Đen";

    // 1. Highlight quân cờ xuất phát
    let sourcePieceElement = null;
    if (piecesLayer) {
      const pieceGroups = piecesLayer.querySelectorAll(".chess-piece");
      pieceGroups.forEach(function (group) {
        const r = parseInt(group.getAttribute("data-row"), 10);
        const c = parseInt(group.getAttribute("data-col"), 10);
        if (r === fromX && c === fromY) {
          group.classList.add("hint-piece");
          sourcePieceElement = group;
        }
      });
    }

    const fromPos = getPosition(fromX, fromY);
    const toPos = getPosition(toX, toY);

    if (movesLayer) {
      // Đảm bảo có marker arrowhead trong SVG defs
      const boardSvg = document.getElementById("board-svg");
      if (boardSvg && !document.getElementById("hint-arrowhead")) {
        let defs = boardSvg.querySelector("defs");
        if (!defs) {
          defs = document.createElementNS(SVG_NS, "defs");
          boardSvg.insertBefore(defs, boardSvg.firstChild);
        }
        const marker = document.createElementNS(SVG_NS, "marker");
        marker.setAttribute("id", "hint-arrowhead");
        marker.setAttribute("markerWidth", "8");
        marker.setAttribute("markerHeight", "8");
        marker.setAttribute("refX", "6");
        marker.setAttribute("refY", "4");
        marker.setAttribute("orient", "auto");
        const path = document.createElementNS(SVG_NS, "path");
        path.setAttribute("d", "M 1 1 L 7 4 L 1 7 Z");
        path.setAttribute("fill", "#f59e0b");
        marker.appendChild(path);
        defs.appendChild(marker);
      }

      // 2. Tính toán điểm đầu và điểm cuối của mũi tên (để không đè trọn tâm quân cờ)
      const dx = toPos.x - fromPos.x;
      const dy = toPos.y - fromPos.y;
      const dist = Math.hypot(dx, dy);

      if (dist > 0) {
        const startOffset = 38;
        const endOffset = 36;
        const startX = fromPos.x + (dx / dist) * startOffset;
        const startY = fromPos.y + (dy / dist) * startOffset;
        const endX = toPos.x - (dx / dist) * endOffset;
        const endY = toPos.y - (dy / dist) * endOffset;

        // Vẽ đường mũi tên chỉ hướng
        const arrowLine = document.createElementNS(SVG_NS, "line");
        arrowLine.setAttribute("x1", startX);
        arrowLine.setAttribute("y1", startY);
        arrowLine.setAttribute("x2", endX);
        arrowLine.setAttribute("y2", endY);
        arrowLine.setAttribute("class", "hint-arrow");
        arrowLine.setAttribute("marker-end", "url(#hint-arrowhead)");
        movesLayer.appendChild(arrowLine);
      }

      // 3. Vòng tròn đích đến có thể click trực tiếp để đi nước này
      const targetCircle = document.createElementNS(SVG_NS, "circle");
      targetCircle.setAttribute("cx", toPos.x);
      targetCircle.setAttribute("cy", toPos.y);
      targetCircle.setAttribute("r", 28);
      targetCircle.setAttribute("class", "hint-target-circle");
      targetCircle.setAttribute("title", "Bấm vào đây để thực hiện ngay nước đi gợi ý này!");

      targetCircle.addEventListener("click", async function (event) {
        event.stopPropagation();
        // Chọn quân cờ gợi ý và thực hiện ngay nước đi
        if (pieceObj) {
          selectedPieceData = pieceObj;
          selectedPieceElement = sourcePieceElement;
          await makeMove(toX, toY);
        }
      });
      movesLayer.appendChild(targetCircle);
    }

    // 4. Nếu người chơi bấm vào chính quân cờ gợi ý, tự động load các nước hợp lệ
    if (sourcePieceElement && pieceObj) {
      selectedPieceData = pieceObj;
      selectedPieceElement = sourcePieceElement;
      selectedPieceElement.classList.add("selected");
    }

    // 5. Cập nhật dòng trạng thái hoặc toast nhẹ nhàng không che bàn cờ
    const turnStatus = document.getElementById("turn-status");
    if (turnStatus) {
      const origText = turnStatus.textContent;
      turnStatus.innerHTML = `<span style="color: #f59e0b; font-weight: 800;">💡 Gợi ý:</span> ${pieceName} (${9 - fromY >= 1 ? 9 - fromY : fromY + 1}) ➜ cột ${9 - toY >= 1 ? 9 - toY : toY + 1} (Bấm ô vàng để đi)`;
      setTimeout(() => {
        if (gameStatus === "InProgress") {
          updateTurnStatus();
        }
      }, 7000);
    }
  }

  if (hintButton) {
    hintButton.addEventListener("click", async function (event) {
      event.stopPropagation();
      await handleGetHint();
    });
  }

  // =====================================================
  // ONLINE CHAT & OPEN ROOMS
  // =====================================================
  function appendChatMessage(sender, message, time) {
    if (!chatMessagesContainer) return;
    const msgDiv = document.createElement("div");
    msgDiv.style.display = "flex";
    msgDiv.style.flexDirection = "column";
    msgDiv.style.background = "rgba(255, 255, 255, 0.05)";
    msgDiv.style.padding = "4px 8px";
    msgDiv.style.borderRadius = "4px";

    const isMe = sender === (document.getElementById("current-player-name")?.textContent || "Bạn");

    msgDiv.innerHTML = `
      <div style="display: flex; justify-content: space-between; font-size: 10px; color: var(--accent-gold);">
        <strong>${sender}</strong>
        <span style="color: var(--text-muted);">${time || ""}</span>
      </div>
      <div style="color: var(--text-main); font-size: 12px; margin-top: 2px; word-break: break-word;">${message}</div>
    `;

    chatMessagesContainer.appendChild(msgDiv);
    chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;
  }

  async function sendCurrentChat(msgText) {
    const text = msgText || (chatInput ? chatInput.value.trim() : "");
    if (!text || !gameId) return;

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      const myName = localStorage.getItem("onlinePlayerColor") === "Red" ? "Quân Đỏ" : "Quân Đen";
      try {
        await connection.invoke("SendChatMessage", gameId.toString(), myName, text);
        if (chatInput) chatInput.value = "";
      } catch (err) {
        console.error("Lỗi gửi chat:", err);
      }
    }
  }

  if (sendChatBtn) {
    sendChatBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      sendCurrentChat();
    });
  }

  if (chatInput) {
    chatInput.addEventListener("keydown", function(e) {
      if (e.key === "Enter") {
        e.preventDefault();
        sendCurrentChat();
      }
    });
  }

  document.querySelectorAll(".quick-chat-tag").forEach(tag => {
    tag.addEventListener("click", function(e) {
      e.stopPropagation();
      const msg = this.getAttribute("data-msg");
      if (msg) sendCurrentChat(msg);
    });
  });

  // Tải danh sách phòng mở
  async function loadOpenRooms() {
    if (!openRoomsList) return;
    try {
      openRoomsList.innerHTML = '<div style="color: var(--text-muted); font-size: 12px; text-align: center; padding: 10px;">Đang tải danh sách phòng...</div>';
      const res = await fetch(`${API_BASE_URL}/online/rooms`);
      if (res.status === 401) {
        openRoomsList.innerHTML = '<div style="color: var(--text-muted); font-size: 12px; text-align: center; padding: 10px;">Vui lòng đăng nhập để xem phòng.</div>';
        return;
      }
      if (!res.ok) throw new Error("Không thể tải phòng.");
      const rooms = await res.json();

      if (!rooms || rooms.length === 0) {
        openRoomsList.innerHTML = '<div style="color: var(--text-muted); font-size: 12px; text-align: center; padding: 10px;">Hiện chưa có phòng nào đang chờ. Hãy tạo phòng mới!</div>';
        return;
      }

      let html = "";
      rooms.forEach(r => {
        html += `
          <div style="display: flex; justify-content: space-between; align-items: center; background: var(--bg-navy-dark); padding: 6px 10px; border-radius: 6px; border: 1px solid var(--border-color);">
            <div>
              <strong style="color: var(--accent-gold); font-size: 13px;">Phòng ${r.roomCode}</strong>
              <div style="font-size: 11px; color: var(--text-muted);">Tạo bởi: ${r.creatorUsername} (Elo: ${r.creatorElo || 1200})</div>
            </div>
            <button class="join-room-fast-btn btn-primary" data-code="${r.roomCode}" style="padding: 4px 10px; font-size: 11px;">
              Vào chơi
            </button>
          </div>
        `;
      });
      openRoomsList.innerHTML = html;

      // Gắn sự kiện vào phòng nhanh
      openRoomsList.querySelectorAll(".join-room-fast-btn").forEach(btn => {
        btn.addEventListener("click", function(e) {
          e.stopPropagation();
          const code = this.getAttribute("data-code");
          if (onlineRoomIdInput) onlineRoomIdInput.value = code;
          if (joinOnlineButton) joinOnlineButton.click();
        });
      });
    } catch (e) {
      console.warn("Lỗi load rooms:", e);
      openRoomsList.innerHTML = '<div style="color: #ef4444; font-size: 12px; text-align: center; padding: 10px;">Lỗi tải danh sách phòng.</div>';
    }
  }

  if (refreshRoomsBtn) {
    refreshRoomsBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      loadOpenRooms();
    });
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
