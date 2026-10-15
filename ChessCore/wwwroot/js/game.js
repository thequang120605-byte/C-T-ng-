document.addEventListener("DOMContentLoaded", function () {
  const SVG_NS = "http://www.w3.org/2000/svg";

  // =====================================================
  // UI TOAST NOTIFICATION (Thay thế alert trình duyệt)
  // =====================================================
  let toastTimer = null;
  function showToast(message, type = "info") {
    let toast = document.getElementById("game-ui-toast");
    if (!toast) {
      toast = document.createElement("div");
      toast.id = "game-ui-toast";
      toast.className = "game-ui-toast";
      document.body.appendChild(toast);
    }

    let icon = '<i class="fa-solid fa-circle-info" style="color: #38bdf8;"></i>';
    if (type === "warning") {
      icon = '<i class="fa-solid fa-triangle-exclamation" style="color: #f59e0b;"></i>';
    } else if (type === "danger" || type === "error") {
      icon = '<i class="fa-solid fa-circle-xmark" style="color: #ef4444;"></i>';
    } else if (type === "success") {
      icon = '<i class="fa-solid fa-circle-check" style="color: #10b981;"></i>';
    }

    toast.className = `game-ui-toast ${type === "error" ? "danger" : type}`;
    toast.innerHTML = `${icon} <span>${message}</span>`;
    toast.classList.add("show");

    if (toastTimer) clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      toast.classList.remove("show");
    }, 3800);
  }

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
              showToast(data.message || "Đối thủ đã từ chối lời xin hòa.", "warning");
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
  let IS_AI_VS_AI = false;
  let aiVsAiPaused = false;
  let aiVsAiTimer = null;

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
  const playerVsAiButton = document.getElementById("player-vs-ai-btn");
  const aivaiSelection = document.getElementById("aivai-selection");
  const aivaiRedDepth = document.getElementById("aivai-red-depth");
  const aivaiBlackDepth = document.getElementById("aivai-black-depth");
  const aivaiSpeed = document.getElementById("aivai-speed");
  const aivaiControls = document.getElementById("aivai-controls");
  const aivaiToggleBtn = document.getElementById("aivai-toggle-btn");
  const aivaiStepBtn = document.getElementById("aivai-step-btn");
  const aivaiStatusPill = document.getElementById("aivai-status-pill");

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
  const hintMenuContainer = document.querySelector(".hint-menu-container");
  const quickSoundBtn = document.getElementById("quick-sound-btn");
  const onlineChatBox = document.getElementById("online-chat-box");
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

    if (playerVsAiButton) {
      playerVsAiButton.classList.remove("selected");
      playerVsAiButton.setAttribute("aria-pressed", "false");
    }

    // -----------------------------------------
    // AI VS AI (MÁY VS MÁY)
    // -----------------------------------------
    if (IS_AI_VS_AI) {
      if (playerVsAiButton) {
        playerVsAiButton.classList.add("selected");
        playerVsAiButton.setAttribute("aria-pressed", "true");
      }
      if (selectedModeText) {
        selectedModeText.textContent = "⚡ AI vs AI (Tự động đấu)";
      }
      if (difficultySelection) {
        difficultySelection.style.display = "none"; difficultySelection.classList.add("hidden");
      }
      if (aivaiSelection) {
        aivaiSelection.style.display = "block"; aivaiSelection.classList.remove("hidden");
      }
      if (selectedDifficultyContainer) {
        selectedDifficultyContainer.style.display = "none"; selectedDifficultyContainer.classList.add("hidden");
      }
    }
    // -----------------------------------------
    // COMPUTER (NGƯỜI VS MÁY)
    // -----------------------------------------
    else if (VS_COMPUTER) {
      if (playerVsComputerButton) {
        playerVsComputerButton.classList.add("selected");

        playerVsComputerButton.setAttribute("aria-pressed", "true");
      }

      if (selectedModeText) {
        selectedModeText.textContent = "🤖 Người chơi vs Máy";
      }

      if (aivaiSelection) {
        aivaiSelection.style.display = "none"; aivaiSelection.classList.add("hidden");
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

      if (aivaiSelection) {
        aivaiSelection.style.display = "none"; aivaiSelection.classList.add("hidden");
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

    if (IS_AI_VS_AI) {
      aiVsAiPaused = false;
      if (aivaiControls) aivaiControls.classList.remove("hidden");
      if (aivaiStatusPill) {
        aivaiStatusPill.textContent = "Đang chạy";
        aivaiStatusPill.style.background = "rgba(168, 85, 247, 0.2)";
        aivaiStatusPill.style.color = "#9333ea";
      }
      if (aivaiToggleBtn) {
        aivaiToggleBtn.innerHTML = '<i class="fa-solid fa-pause"></i> Tạm dừng';
      }
    } else {
      if (aivaiControls) aivaiControls.classList.add("hidden");
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

    if (playerVsAiButton) {
      playerVsAiButton.classList.remove("selected");
      playerVsAiButton.setAttribute("aria-pressed", "false");
    }

    IS_AI_VS_AI = false;
    aiVsAiPaused = false;
    if (aiVsAiTimer) {
      clearTimeout(aiVsAiTimer);
      aiVsAiTimer = null;
    }
    if (aivaiControls) {
      aivaiControls.classList.add("hidden");
    }
    if (aivaiSelection) {
      aivaiSelection.style.display = "none";
      aivaiSelection.classList.add("hidden");
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
    const urlParams = new URLSearchParams(window.location.search);
    const joinRoomCode = urlParams.get("joinRoom");
    if (joinRoomCode) {
      if (onlineRoomIdInput) onlineRoomIdInput.value = joinRoomCode;
      if (playerVsOnlineButton) playerVsOnlineButton.click();
      setTimeout(() => {
        if (joinOnlineButton) joinOnlineButton.click();
      }, 300);
      return;
    }

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

      showToast(`Không thể tạo ván cờ: ${error.message || "Vui lòng kiểm tra lại kết nối."}`, "danger");
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

    // Cập nhật Đánh giá thế trận (H1, H2, H3, Thước đo ưu thế) từ ArenaUI
    if (window.ArenaUI && typeof window.ArenaUI.update === "function") {
      try {
        window.ArenaUI.update(data);
      } catch (err) {
        console.warn("Lỗi khi cập nhật bảng đánh giá ArenaUI:", err);
      }
    }

    console.log("Bàn cờ đã được cập nhật!");

    if (VS_COMPUTER && gameStatus === "InProgress" && currentTurn === "Black") {
      triggerComputerMove();
    } else if (IS_AI_VS_AI && gameStatus === "InProgress") {
      scheduleNextAiVsAiMove();
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

    if (IS_AI_VS_AI) {
      const redDepth = aivaiRedDepth ? aivaiRedDepth.value : 2;
      const blackDepth = aivaiBlackDepth ? aivaiBlackDepth.value : 3;
      if (redPlayerName) {
        redPlayerName.textContent = `Bot Đỏ (Depth ${redDepth})`;
      }
      if (blackPlayerName) {
        blackPlayerName.textContent = `Bot Đen (Depth ${blackDepth})`;
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

  // =====================================================
  // AI VS AI (TỰ ĐỘNG ĐẤU)
  // =====================================================

  async function triggerAiVsAiMove(force = false) {
    if (!IS_AI_VS_AI || !gameId) return;
    if (gameStatus !== "InProgress") return;
    if (isComputerThinking) return;
    if (aiVsAiPaused && !force) return;

    try {
      isComputerThinking = true;
      clearSelection();
      updateTurnStatus();

      const color = (currentTurn || "Red").toLowerCase();
      const depth = color === "red"
        ? (aivaiRedDepth ? parseInt(aivaiRedDepth.value, 10) : 2)
        : (aivaiBlackDepth ? parseInt(aivaiBlackDepth.value, 10) : 3);

      console.log(`⚡ AI vs AI: Bot ${color.toUpperCase()} đang tính với Depth ${depth}...`);

      const response = await fetch(
        `${API_BASE_URL}/${gameId}/computer-move?difficulty=${depth}&computerColor=${color}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" }
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "AI không thể đi nước này.");
      }

      const data = await response.json();
      updateGameFromApi(data);
    } catch (err) {
      console.error("Lỗi AI vs AI:", err);
      showToast(`AI vs AI lỗi: ${err.message}`, "warning");
    } finally {
      isComputerThinking = false;
      updateTurnStatus();
    }
  }

  function scheduleNextAiVsAiMove() {
    if (!IS_AI_VS_AI || gameStatus !== "InProgress" || aiVsAiPaused) return;
    if (aiVsAiTimer) clearTimeout(aiVsAiTimer);

    const speed = aivaiSpeed ? parseInt(aivaiSpeed.value, 10) : 800;
    aiVsAiTimer = setTimeout(() => {
      triggerAiVsAiMove();
    }, speed);
  }

  if (aivaiToggleBtn) {
    aivaiToggleBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      aiVsAiPaused = !aiVsAiPaused;
      if (aiVsAiPaused) {
        if (aiVsAiTimer) { clearTimeout(aiVsAiTimer); aiVsAiTimer = null; }
        this.innerHTML = '<i class="fa-solid fa-play"></i> Tiếp tục';
        if (aivaiStatusPill) {
          aivaiStatusPill.textContent = "Đã tạm dừng";
          aivaiStatusPill.style.background = "rgba(234, 179, 8, 0.2)";
          aivaiStatusPill.style.color = "#d97706";
        }
      } else {
        this.innerHTML = '<i class="fa-solid fa-pause"></i> Tạm dừng';
        if (aivaiStatusPill) {
          aivaiStatusPill.textContent = "Đang chạy";
          aivaiStatusPill.style.background = "rgba(168, 85, 247, 0.2)";
          aivaiStatusPill.style.color = "#9333ea";
        }
        triggerAiVsAiMove();
      }
    });
  }

  if (aivaiStepBtn) {
    aivaiStepBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      triggerAiVsAiMove(true);
    });
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

    const skin = localStorage.getItem("chess_piece_skin") || "traditional";
    
    // Chuẩn hóa tên quân và màu sắc cho skin hình ảnh
    function getPieceImageFileName(piece, targetSkin) {
      const isRed = (piece.color || "").toLowerCase() === "red";
      const colorStr = isRed ? "Red" : "Black";
      const actualSkin = targetSkin === "Minimalist" ? "Minimalist White" : targetSkin;

      let pieceName = "";
      const t = (piece.type || "").toLowerCase();
      if (t === "king" || t === "tướng") {
        pieceName = isRed ? "Shuai" : "Jiang";
      } else if (t === "advisor" || t === "sĩ" || t === "si") {
        pieceName = "Shi";
      } else if (t === "elephant" || t === "tượng" || t === "tuong") {
        pieceName = "Xiang";
      } else if (t === "horse" || t === "knight" || t === "mã" || t === "ma") {
        pieceName = "Ma";
      } else if (t === "rook" || t === "chariot" || t === "xe") {
        pieceName = "Ju";
      } else if (t === "cannon" || t === "pháo" || t === "phao") {
        pieceName = "Pao";
      } else if (t === "pawn" || t === "tốt" || t === "tot" || t === "binh") {
        pieceName = isRed ? "Bing" : "Zu";
      } else {
        pieceName = isRed ? "Bing" : "Zu";
      }

      return `${actualSkin} ${colorStr} ${pieceName}.png`;
    }

    if (skin !== "traditional") {
      // Dùng ảnh quân cờ thực tế (Wooden, Shadowed, Brown, Minimalist White)
      const fileName = getPieceImageFileName(piece, skin);
      const imgPath = `/images/pieces/${fileName}`;

      const img = document.createElementNS(SVG_NS, "image");
      img.setAttribute("x", position.x - 44);
      img.setAttribute("y", position.y - 44);
      img.setAttribute("width", 88);
      img.setAttribute("height", 88);
      img.setAttribute("href", imgPath);
      img.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", imgPath);
      img.setAttribute("class", "piece-image-skin");
      img.style.pointerEvents = "all";
      img.style.filter = "drop-shadow(0 4px 6px rgba(0,0,0,0.35))";

      // Fallback về quân vector nếu ảnh bị lỗi tải
      img.addEventListener("error", function() {
        console.warn("Lỗi tải ảnh quân cờ:", imgPath, "- chuyển về vector");
        img.remove();
        renderVectorPiece(pieceGroup, position, piece);
      });

      pieceGroup.appendChild(img);
    } else {
      renderVectorPiece(pieceGroup, position, piece);
    }

    function renderVectorPiece(parent, pos, p) {
      const circle = document.createElementNS(SVG_NS, "circle");
      circle.setAttribute("cx", pos.x);
      circle.setAttribute("cy", pos.y);
      circle.setAttribute("r", 42);
      circle.setAttribute("class", "piece-circle");

      const textElement = document.createElementNS(SVG_NS, "text");
      textElement.setAttribute("x", pos.x);
      textElement.setAttribute("y", pos.y + 2);
      textElement.setAttribute("text-anchor", "middle");
      textElement.setAttribute("dominant-baseline", "middle");
      textElement.setAttribute("class", "piece-text");
      textElement.textContent = p.text;

      parent.appendChild(circle);
      parent.appendChild(textElement);
    }

    pieceGroup.addEventListener("click", async function (event) {
      event.stopPropagation();
      await handlePieceClick(pieceGroup);
    });

    piecesLayer.appendChild(pieceGroup);
  }

  // Lắng nghe sự kiện thay đổi kiểu quân cờ để re-render ngay
  window.addEventListener("chess-skin-changed", function() {
    renderPieces();
  });

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

      if (IS_AI_VS_AI) {
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
    const hintLayer = document.getElementById("hint-layer");
    if (hintLayer) {
      hintLayer.innerHTML = "";
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

      showToast(error.message, "warning");
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

      if (aiVsAiTimer) {
        clearTimeout(aiVsAiTimer);
        aiVsAiTimer = null;
      }
      aiVsAiPaused = false;
      if (IS_AI_VS_AI) {
        if (aivaiControls) aivaiControls.classList.remove("hidden");
        if (aivaiStatusPill) {
          aivaiStatusPill.textContent = "Đang chạy";
          aivaiStatusPill.style.background = "rgba(168, 85, 247, 0.2)";
          aivaiStatusPill.style.color = "#9333ea";
        }
        if (aivaiToggleBtn) {
          aivaiToggleBtn.innerHTML = '<i class="fa-solid fa-pause"></i> Tạm dừng';
        }
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

      showToast(error.message, "warning");
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

  function formatMoveNotation(m) {
    const piece = PIECE_NAMES_VI[m.pieceType] || m.pieceType || "Quân";
    const colorTag = m.pieceColor === "Red" ? "Đỏ" : "Đen";
    return `${piece} (${m.fromX},${m.fromY}) ➜ (${m.toX},${m.toY})`;
  }

  function renderMoveHistory(moves) {
    if (!moveHistoryContainer) return;

    if (!moves || moves.length === 0) {
      moveHistoryContainer.innerHTML = '<div class="empty-history">Chưa có nước đi nào</div>';
      return;
    }

    let html = '';
    // Ghép theo từng hiệp (Cặp nước Đỏ - Đen)
    for (let i = 0; i < moves.length; i += 2) {
      const moveNum = Math.floor(i / 2) + 1;
      const redMove = moves[i];
      const blackMove = i + 1 < moves.length ? moves[i + 1] : null;

      html += `<div style="display: grid; grid-template-columns: 28px 1fr 1fr; gap: 8px; padding: 6px 8px; border-bottom: 1px solid rgba(255,255,255,0.06); align-items: center; font-size: 13px; white-space: nowrap;">`;
      html += `<span style="color: var(--text-muted); font-weight: 700;">${moveNum}.</span>`;
      
      if (redMove) {
        html += `<span style="color: #c00000; font-weight: 700;" title="${formatMoveNotation(redMove)}">${formatMoveNotation(redMove)}</span>`;
      } else {
        html += `<span></span>`;
      }

      if (blackMove) {
        html += `<span style="color: #0f172a; font-weight: 700;" title="${formatMoveNotation(blackMove)}">${formatMoveNotation(blackMove)}</span>`;
      } else {
        html += `<span></span>`;
      }

      html += `</div>`;
    }

    moveHistoryContainer.innerHTML = html;
    moveHistoryContainer.scrollTop = moveHistoryContainer.scrollHeight;

    // Tự động nhận diện thế trận khai cuộc từ các nước đầu tiên
    updateOpeningRecognition(moves);
  }

  function updateOpeningRecognition(moves) {
    const openingNameEl = document.getElementById("opening-book-name");
    const openingCodeEl = document.getElementById("opening-book-code");
    if (!openingNameEl || !openingCodeEl) return;

    if (!moves || moves.length === 0) {
      openingNameEl.textContent = "Chưa định hình";
      openingCodeEl.textContent = "ECCO";
      return;
    }

    const firstRed = moves[0];
    const firstBlack = moves.length > 1 ? moves[1] : null;

    // Nước 1 của Đỏ
    if (firstRed.pieceType === "Cannon") {
      // Pháo 2 bình 5 (col 7 -> 4) hoặc Pháo 8 bình 5 (col 1 -> 4)
      if (firstRed.toX === 4) {
        if (!firstBlack) {
          openingNameEl.textContent = "Pháo Đầu (Trung Pháo)";
          openingCodeEl.textContent = "ECCO-01";
        } else if (firstBlack.pieceType === "Horse") {
          openingNameEl.textContent = "Pháo Đầu đối Bình Phong Mã";
          openingCodeEl.textContent = "ECCO-02";
        } else if (firstBlack.pieceType === "Cannon" && firstBlack.toX === 4) {
          openingNameEl.textContent = "Thuận Pháo (Đại chiến)";
          openingCodeEl.textContent = "ECCO-03";
        } else {
          openingNameEl.textContent = "Pháo Đầu (Trung Pháo)";
          openingCodeEl.textContent = "ECCO-01";
        }
        return;
      }
    } else if (firstRed.pieceType === "Pawn") {
      // Tốt 7 tiến 1 hoặc Tốt 3 tiến 1
      if (firstRed.fromX === 2 || firstRed.fromX === 6) {
        openingNameEl.textContent = "Tiên Nhân Chỉ Lộ";
        openingCodeEl.textContent = "ECCO-04";
        return;
      }
    } else if (firstRed.pieceType === "Horse") {
      openingNameEl.textContent = "Khởi Mã Cuộc (Trận Thượng Tiên)";
      openingCodeEl.textContent = "ECCO-05";
      return;
    } else if (firstRed.pieceType === "Elephant") {
      openingNameEl.textContent = "Phi Tượng Cuộc";
      openingCodeEl.textContent = "ECCO-06";
      return;
    }

    openingNameEl.textContent = "Khai cuộc tự do";
    openingCodeEl.textContent = "ECCO-00";
  }

  // =====================================================
  // UNDO (ĐI LẠI)
  // =====================================================

  let isUndoing = false;

  async function handleUndo() {
    const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
    if (isOnline) {
      showToast("Chế độ chơi Online không hỗ trợ chức năng đi lại!", "warning");
      return;
    }

    if (!gameId || gameStatus !== "InProgress") {
      showToast("Chỉ có thể xin đi lại khi ván cờ đang diễn ra!", "warning");
      return;
    }

    if (undoCount >= MAX_UNDO_LIMIT) {
      showToast(`Bạn đã sử dụng hết số lần đi lại cho phép trong ván này (tối đa ${MAX_UNDO_LIMIT} lần)!`, "warning");
      return;
    }

    if (isComputerThinking) {
      showToast("Máy đang tính nước đi, vui lòng đợi trong giây lát!", "warning");
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
      showToast(error.message, "danger");
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
        showToast(data.message || "Không thể gửi yêu cầu xin hòa.", "warning");
        return;
      }

      if (data.accepted === true) {
        showGameNotification("🤝", "HÒA CỜ", data.message || "Ván cờ kết thúc với kết quả hòa!", 3000);
        if (data.game) {
          updateGameFromApi(data.game);
        }
      } else if (data.accepted === false) {
        showToast(data.message || "Máy đã từ chối lời xin hòa. Hãy tiếp tục thi đấu!", "warning");
      } else {
        showGameNotification("🤝", "XIN HÒA", data.message || "Đã gửi đề nghị hòa. Chờ đối thủ trong 15s...", 3500);
      }
    } catch (error) {
      console.error("Lỗi gửi đề nghị xin hòa:", error);
      showToast("Lỗi kết nối khi gửi đề nghị xin hòa.", "danger");
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
        showToast(data.message || "Không thể phản hồi yêu cầu xin hòa.", "warning");
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
      IS_AI_VS_AI = false;
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
      IS_AI_VS_AI = false;
      localStorage.removeItem("isOnlineMode");
      localStorage.removeItem("onlinePlayerColor");
      localStorage.removeItem("onlineOpponentName");
      if (onlineSelection) { onlineSelection.style.display = "none"; onlineSelection.classList.add("hidden"); }
      if (startGameButton) { startGameButton.style.display = "block"; startGameButton.classList.remove("hidden"); }
      selectGameMode(true);
    });
  }

  if (playerVsAiButton) {
    playerVsAiButton.addEventListener("click", function (event) {
      event.stopPropagation();
      isOnlineMode = false;
      VS_COMPUTER = false;
      IS_AI_VS_AI = true;
      localStorage.removeItem("isOnlineMode");
      localStorage.removeItem("onlinePlayerColor");
      localStorage.removeItem("onlineOpponentName");
      if (onlineSelection) { onlineSelection.style.display = "none"; onlineSelection.classList.add("hidden"); }
      if (startGameButton) { startGameButton.style.display = "block"; startGameButton.classList.remove("hidden"); }
      selectGameMode(false);
    });
  }

  if (playerVsOnlineButton) {
    playerVsOnlineButton.addEventListener("click", function (event) {
      event.stopPropagation();
      if (!isAuthenticated) {
        showToast("Bạn cần đăng nhập để chơi online.", "warning");
        window.location.href = "/Auth/Login?returnUrl=%2FGame";
        return;
      }
      isOnlineMode = true;
      gameModeSelected = true;
      VS_COMPUTER = false;
      IS_AI_VS_AI = false;
      
      if (playerVsPlayerButton) playerVsPlayerButton.classList.remove("selected");
      if (playerVsComputerButton) playerVsComputerButton.classList.remove("selected");
      if (playerVsAiButton) playerVsAiButton.classList.remove("selected");
      playerVsOnlineButton.classList.add("selected");
      
      if (selectedModeText) selectedModeText.textContent = "🌐 Online";
      if (difficultySelection) { difficultySelection.style.display = "none"; difficultySelection.classList.add("hidden"); }
      if (aivaiSelection) { aivaiSelection.style.display = "none"; aivaiSelection.classList.add("hidden"); }
      if (selectedDifficultyContainer) { selectedDifficultyContainer.style.display = "none"; selectedDifficultyContainer.classList.add("hidden"); }
      
      if (onlineSelection) { onlineSelection.style.display = "block"; onlineSelection.classList.remove("hidden"); }
      if (startGameButton) { startGameButton.style.display = "none"; startGameButton.classList.add("hidden"); }
      loadOpenRooms();
      loadLiveMatches();
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
          showToast(e.message || "Lỗi tạo bàn.", "danger");
      } finally {
          isCreatingGame = false;
      }
  }

  if (invitePlayerButton) {
      invitePlayerButton.addEventListener("click", async function (event) {
          event.stopPropagation();
          const playerId = opponentPlayerIdInput.value.trim();
          if (!playerId) {
              showToast("Vui lòng nhập ID người chơi cần mời.", "warning");
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
                  invitePlayerStatus.textContent = `Đã thêm ${result.username} (${result.playerId}) vào bàn và gửi thông báo thách đấu!`;
              }
              // Gửi tín hiệu thách đấu thời gian thực qua SignalR
              if (signalrConnection && signalrConnection.state === signalR.HubConnectionState.Connected) {
                  const myNameEl = document.querySelector(".user-display-name") || document.getElementById("red-player-name");
                  const myName = myNameEl ? myNameEl.textContent.trim() : "Đối thủ";
                  const myPidEl = document.getElementById("current-player-id");
                  const myPid = myPidEl ? myPidEl.textContent.trim() : "";
                  const rCode = document.getElementById("display-room-code")?.textContent?.trim() || "";
                  signalrConnection.invoke("SendChallenge", result.playerId, myName, myPid, rCode).catch(e => console.warn("Lỗi gửi SignalR thách đấu:", e));
              }
              showToast(`Đã gửi lời thách đấu tới ${result.username}!`, "success");
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
              showToast("Vui lòng nhập mã bàn đối thủ cung cấp.", "warning");
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
              showToast(e.message || "Không tìm thấy bàn hoặc lỗi kết nối.", "danger");
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
  // FEN & MOVE RECORD (CHÉP FEN, NẠP FEN, BIÊN BẢN)
  // =====================================================

  const copyRoomCodeBtn = document.getElementById("copy-room-code-btn");
  if (copyRoomCodeBtn) {
    copyRoomCodeBtn.addEventListener("click", function () {
      const codeEl = document.getElementById("display-room-code");
      const code = codeEl ? codeEl.textContent.trim() : "";
      if (!code) return;
      navigator.clipboard.writeText(code).then(() => {
        const originalText = copyRoomCodeBtn.innerHTML;
        copyRoomCodeBtn.innerHTML = '<i class="fa-solid fa-check"></i> Đã chép';
        copyRoomCodeBtn.style.color = '#10b981';
        setTimeout(() => {
          copyRoomCodeBtn.innerHTML = originalText;
          copyRoomCodeBtn.style.color = '';
        }, 2000);
      });
    });
  }

  // Sao chép FEN thế cờ hiện tại
  const copyFenBtn = document.getElementById("copy-fen-btn");
  if (copyFenBtn) {
    copyFenBtn.addEventListener("click", async function (event) {
      event.stopPropagation();
      if (!gameId) {
        showToast("Chưa có ván cờ nào để lấy FEN!", "warning");
        return;
      }
      try {
        const res = await fetch(`${API_BASE_URL}/${gameId}/fen`);
        if (!res.ok) throw new Error("Không thể lấy FEN.");
        const data = await res.json();
        const fen = data.fen || data;
        const fenStr = typeof fen === "string" ? fen : JSON.stringify(fen);
        await navigator.clipboard.writeText(fenStr);
        showToast("Đã sao chép chuỗi FEN vào bộ nhớ tạm!", "success");
      } catch (err) {
        showToast("Lỗi khi sao chép FEN: " + err.message, "danger");
      }
    });
  }

  // Nhập thế cờ từ FEN (Import FEN)
  const importFenBtn = document.getElementById("import-fen-btn");
  const modeImportFenBtn = document.getElementById("mode-import-fen-btn");
  const importFenModal = document.getElementById("import-fen-modal");
  const importFenCancelBtn = document.getElementById("import-fen-cancel-btn");
  const importFenSubmitBtn = document.getElementById("import-fen-submit-btn");
  const importFenInput = document.getElementById("import-fen-input");

  function openImportFenModal(event) {
    if (event) event.stopPropagation();
    if (importFenModal) {
      importFenModal.classList.remove("hidden");
      if (importFenInput) importFenInput.focus();
    }
  }

  if (importFenBtn) {
    importFenBtn.addEventListener("click", openImportFenModal);
  }
  if (modeImportFenBtn) {
    modeImportFenBtn.addEventListener("click", openImportFenModal);
  }
  if (importFenCancelBtn && importFenModal) {
    importFenCancelBtn.addEventListener("click", function (event) {
      event.stopPropagation();
      importFenModal.classList.add("hidden");
    });
  }
  document.querySelectorAll(".fen-preset-btn").forEach(btn => {
    btn.addEventListener("click", function (event) {
      event.stopPropagation();
      if (importFenInput) {
        importFenInput.value = this.dataset.fen || "";
      }
    });
  });
  if (importFenSubmitBtn && importFenModal) {
    importFenSubmitBtn.addEventListener("click", async function (event) {
      event.stopPropagation();
      const fenVal = (importFenInput?.value || "").trim();
      if (!fenVal) {
        showToast("Vui lòng nhập chuỗi FEN hợp lệ!", "warning");
        return;
      }

      // Không cho phép nạp FEN khi đang chơi online
      const isOnline = isOnlineMode || localStorage.getItem("isOnlineMode") === "true";
      if (isOnline) {
        showToast("Không thể nạp FEN khi đang chơi chế độ Online!", "warning");
        return;
      }

      try {
        const res = await fetch(`${API_BASE_URL}?initialFen=${encodeURIComponent(fenVal)}`, {
          method: "POST",
          headers: { "Content-Type": "application/json" }
        });
        if (!res.ok) {
          const err = await res.text();
          throw new Error(err || "Chuỗi FEN không hợp lệ.");
        }
        const data = await res.json();
        
        // Reset game state trước khi load ván mới từ FEN
        undoCount = 0;
        previousMovesCount = 0;
        previousPieceCount = 0;
        isComputerThinking = false;
        clearSelection();
        
        gameId = data.id;
        localStorage.setItem("xiangqiGameId", gameId);
        
        // Đảm bảo game content hiển thị
        if (modeSelection) modeSelection.classList.add("hidden");
        if (gameContent) gameContent.classList.remove("hidden");
        
        importFenModal.classList.add("hidden");
        updateGameFromApi(data);
        updateUndoButtonUI();
        showToast("Đã thiết lập ván cờ từ FEN thành công!", "success");
      } catch (err) {
        showToast("Lỗi nạp FEN: " + err.message, "danger");
      }
    });
  }

  // Xuất / Tải biên bản ván cờ
  const exportPgnBtn = document.getElementById("export-pgn-btn");
  if (exportPgnBtn) {
    exportPgnBtn.addEventListener("click", function (event) {
      event.stopPropagation();
      const container = document.getElementById("move-history-container");
      if (!container || !container.children.length) {
        showToast("Chưa có nước cờ nào để xuất biên bản!", "warning");
        return;
      }
      let text = "=== BIÊN BẢN VÁN CỜ TƯỚNG ===\n";
      text += `Ngày đấu: ${new Date().toLocaleString("vi-VN")}\n`;
      text += `Mã ván: ${gameId || "N/A"}\n\n`;
      text += "--- DANH SÁCH NƯỚC ĐI ---\n";
      const rows = container.querySelectorAll("div[style*='grid']");
      rows.forEach(r => {
        const spans = r.querySelectorAll("span");
        if (spans.length >= 3) {
          text += `${spans[0].textContent.trim()} ${spans[1].textContent.trim()} | ${spans[2].textContent.trim()}\n`;
        }
      });
      const blob = new Blob([text], { type: "text/plain;charset=utf-8" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `BienBan_VanCo_${gameId || "game"}.txt`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      showToast("Đã tải về biên bản ván cờ!", "success");
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
      if (hintMenuContainer) hintMenuContainer.style.display = "none";
      else if (hintButton) hintButton.style.display = "none";
      if (drawButton) drawButton.style.display = "flex";
      if (resignButton) resignButton.style.display = "flex";
      if (onlineChatBox) onlineChatBox.classList.remove("hidden");
    } else if (VS_COMPUTER) {
      // Chơi với máy: Có Đi lại, có Gợi ý, có Đầu hàng, ẩn Xin hòa; Ẩn Chat box
      if (undoButton) undoButton.style.display = "flex";
      if (hintMenuContainer) hintMenuContainer.style.display = "inline-block";
      else if (hintButton) hintButton.style.display = "flex";
      if (drawButton) drawButton.style.display = "none";
      if (resignButton) resignButton.style.display = "flex";
      if (onlineChatBox) onlineChatBox.classList.add("hidden");
      updateUndoButtonUI();
    } else {
      // Người vs Người trên cùng máy: Có Đi lại, có Gợi ý; Bỏ Đầu hàng và Xin hòa; Ẩn Chat box
      if (undoButton) undoButton.style.display = "flex";
      if (hintMenuContainer) hintMenuContainer.style.display = "inline-block";
      else if (hintButton) hintButton.style.display = "flex";
      if (drawButton) drawButton.style.display = "none";
      if (resignButton) resignButton.style.display = "none";
      if (onlineChatBox) onlineChatBox.classList.add("hidden");
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
  // AI HINT (GỢI Ý QUÂN CỜ & GỢI Ý CÓ NƯỚC ĐI)
  // =====================================================
  let isHinting = false;
  const MAX_GAME_HINTS = 3;
  let remainingGameHints = MAX_GAME_HINTS;
  const hintDropdown = document.getElementById("hint-dropdown");
  const hintPieceBtn = document.getElementById("hint-piece-btn");
  const hintFullBtn = document.getElementById("hint-full-btn");

  function updateHintButtonUI() {
    if (!hintButton) return;
    const span = hintButton.querySelector("span");
    if (span) {
      if (remainingGameHints > 0) {
        span.innerHTML = `Gợi ý (${remainingGameHints}) <i class="fa-solid fa-caret-up" style="font-size: 10px;"></i>`;
        hintButton.disabled = false;
        hintButton.style.opacity = "1";
      } else {
        span.innerHTML = `Hết gợi ý <i class="fa-solid fa-ban" style="font-size: 10px; color: #ef4444;"></i>`;
        hintButton.disabled = true;
        hintButton.style.opacity = "0.5";
      }
    }
  }

  async function handleGetHint(mode = "full") {
    if (hintDropdown) {
      hintDropdown.classList.add("hidden");
    }

    if (!gameId || gameStatus !== "InProgress") {
      showToast("Chỉ có thể lấy gợi ý khi ván cờ đang diễn ra!", "warning");
      return;
    }

    if (remainingGameHints <= 0) {
      showToast("Bạn đã dùng hết 3 lượt gợi ý trong ván này. Hãy tự suy nghĩ nhé!", "warning");
      return;
    }

    if (isComputerThinking) {
      showToast("Máy đang tính nước đi, vui lòng đợi trong giây lát!", "warning");
      return;
    }

    if (isHinting) return;

    try {
      isHinting = true;
      if (hintButton) {
        hintButton.disabled = true;
        hintButton.style.opacity = "0.6";
      }

      showToast(mode === "piece" ? "Đang tìm quân cờ tối ưu..." : "Đang tính nước đi tối ưu...", "info");

      const res = await fetch(`${API_BASE_URL}/${gameId}/hint?mode=${encodeURIComponent(mode)}`);
      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.message || "Không thể tìm nước đi gợi ý.");
      }

      const hintData = await res.json();
      console.log("Nước đi gợi ý:", hintData);

      remainingGameHints--;
      updateHintButtonUI();

      // Hiển thị gợi ý trực quan trên bàn cờ theo chế độ
      await highlightHintMove(hintData, mode);
      playSound('move');
    } catch (err) {
      console.error("Lỗi lấy gợi ý:", err);
      showToast(err.message || "Lỗi lấy gợi ý.", "danger");
    } finally {
      isHinting = false;
      updateHintButtonUI();
    }
  }

  async function highlightHintMove(hintData, mode = "full") {
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

    if (mode === "full") {
      const targetLayer = document.getElementById("hint-layer") || movesLayer;
      if (targetLayer) {
        // Đảm bảo có marker arrowhead trong SVG defs
        const boardSvg = document.getElementById("board-svg");
        if (boardSvg) {
          let defs = boardSvg.querySelector("defs");
          if (!defs) {
            defs = document.createElementNS(SVG_NS, "defs");
            boardSvg.insertBefore(defs, boardSvg.firstChild);
          }
          if (!document.getElementById("hint-arrowhead")) {
            const marker = document.createElementNS(SVG_NS, "marker");
            marker.setAttribute("id", "hint-arrowhead");
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

        // 2. Tính toán điểm đầu và điểm cuối của mũi tên chỉ thẳng đích
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

          // Đường viền nền ngoài tăng tương phản rõ rệt trên mọi bàn cờ
          const arrowOutline = document.createElementNS(SVG_NS, "line");
          arrowOutline.setAttribute("x1", startX);
          arrowOutline.setAttribute("y1", startY);
          arrowOutline.setAttribute("x2", endX);
          arrowOutline.setAttribute("y2", endY);
          arrowOutline.setAttribute("class", "hint-arrow-bg");
          targetLayer.appendChild(arrowOutline);

          // Vẽ đường mũi tên vàng chỉ thẳng tới đích
          const arrowLine = document.createElementNS(SVG_NS, "line");
          arrowLine.setAttribute("x1", startX);
          arrowLine.setAttribute("y1", startY);
          arrowLine.setAttribute("x2", endX);
          arrowLine.setAttribute("y2", endY);
          arrowLine.setAttribute("class", "hint-arrow");
          arrowLine.setAttribute("marker-end", "url(#hint-arrowhead)");
          targetLayer.appendChild(arrowLine);
        }

        // 3. Vòng tròn đích đến nổi bật, bấm vào là đi ngay tới đích
        const targetCircle = document.createElementNS(SVG_NS, "circle");
        targetCircle.setAttribute("cx", toPos.x);
        targetCircle.setAttribute("cy", toPos.y);
        targetCircle.setAttribute("r", 28);
        targetCircle.setAttribute("class", "hint-target-circle");
        targetCircle.setAttribute("title", `Nước gợi ý: Đi tới ô (${toX}, ${toY}). Bấm để đi ngay!`);

        targetCircle.addEventListener("click", async function (event) {
          event.stopPropagation();
          if (pieceObj) {
            selectedPieceData = pieceObj;
            selectedPieceElement = sourcePieceElement;
            await makeMove(toX, toY);
          }
        });
        targetLayer.appendChild(targetCircle);
      }
    }

    // 4. Chọn quân cờ gợi ý nhưng KHÔNG load danh sách các chấm xanh khác để tránh rối mắt
    if (sourcePieceElement && pieceObj) {
      selectedPieceData = pieceObj;
      selectedPieceElement = sourcePieceElement;
      selectedPieceElement.classList.add("selected");
    }

    // 5. Cập nhật dòng trạng thái và hiển thị toast đẹp mắt kèm số điểm Elo
    const pointNote = hintData.pointsDeducted ? ` (-${hintData.pointsDeducted} Elo)` : "";
    const turnStatus = document.getElementById("turn-status");
    if (mode === "piece") {
      showToast(`💡 Gợi ý quân${pointNote}: Hãy đi quân ${pieceName} tại ô (${fromX}, ${fromY})`, "info");
      if (turnStatus) {
        turnStatus.innerHTML = `<span style="color: #38bdf8; font-weight: 800;">💡 Gợi ý quân${pointNote}:</span> ${pieceName} (Cột ${9 - fromY >= 1 ? 9 - fromY : fromY + 1})`;
        setTimeout(() => {
          if (gameStatus === "InProgress") {
            updateTurnStatus();
          }
        }, 7000);
      }
    } else {
      showToast(`🎯 Gợi ý nước đi${pointNote}: ${pieceName} (${fromX}, ${fromY}) ➜ (${toX}, ${toY})`, "success");
      if (turnStatus) {
        turnStatus.innerHTML = `<span style="color: #f59e0b; font-weight: 800;">🎯 Nước tối ưu${pointNote}:</span> ${pieceName} ➜ (${toX}, ${toY}) (Bấm vào đích màu vàng)`;
        setTimeout(() => {
          if (gameStatus === "InProgress") {
            updateTurnStatus();
          }
        }, 7000);
      }
    }
  }

  if (hintButton) {
    hintButton.addEventListener("click", function (event) {
      event.stopPropagation();
      if (hintDropdown) {
        hintDropdown.classList.toggle("hidden");
      }
    });
  }

  if (hintPieceBtn) {
    hintPieceBtn.addEventListener("click", async function (event) {
      event.stopPropagation();
      await handleGetHint("piece");
    });
  }

  if (hintFullBtn) {
    hintFullBtn.addEventListener("click", async function (event) {
      event.stopPropagation();
      await handleGetHint("full");
    });
  }

  document.addEventListener("click", function (event) {
    if (hintDropdown && !hintDropdown.classList.contains("hidden")) {
      if (!hintDropdown.contains(event.target) && event.target !== hintButton && !hintButton.contains(event.target)) {
        hintDropdown.classList.add("hidden");
      }
    }
  });

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

  // Tải danh sách các trận đang đấu trực tiếp (Khán giả)
  async function loadLiveMatches() {
    const liveList = document.getElementById("live-matches-list");
    if (!liveList) return;
    try {
      liveList.innerHTML = '<div style="color: var(--text-muted); font-size: 12px; text-align: center; padding: 10px;">Đang tải danh sách trận đấu...</div>';
      const res = await fetch(`${API_BASE_URL}/online/live`);
      if (!res.ok) throw new Error("Không thể tải trận trực tiếp.");
      const matches = await res.json();

      if (!matches || matches.length === 0) {
        liveList.innerHTML = '<div style="color: var(--text-muted); font-size: 12px; text-align: center; padding: 10px;">Hiện chưa có trận đấu trực tiếp nào.</div>';
        return;
      }

      let html = "";
      matches.forEach(m => {
        html += `
          <div style="display: flex; justify-content: space-between; align-items: center; background: var(--bg-navy-dark); padding: 6px 10px; border-radius: 6px; border: 1px solid var(--border-color);">
            <div>
              <div style="font-size: 12px; font-weight: bold; color: var(--text-main);">
                <span style="color: #ef4444;">${m.redPlayerName}</span> vs <span style="color: #3b82f6;">${m.blackPlayerName}</span>
              </div>
              <div style="font-size: 10px; color: var(--text-muted);">Phòng ${m.roomCode} · ${m.moveCount} nước đi</div>
            </div>
            <a href="/Game?joinRoom=${encodeURIComponent(m.roomCode)}" class="btn-primary" style="padding: 4px 10px; font-size: 11px; text-decoration: none; background: #ef4444; border-radius: 4px; display: inline-flex; align-items: center; gap: 4px;">
              <i class="fa-solid fa-eye"></i> Xem
            </a>
          </div>
        `;
      });
      liveList.innerHTML = html;
    } catch (e) {
      console.warn("Lỗi load live matches:", e);
      liveList.innerHTML = '<div style="color: #ef4444; font-size: 12px; text-align: center; padding: 10px;">Lỗi tải trận trực tiếp.</div>';
    }
  }

  const refreshLiveBtn = document.getElementById("refresh-live-btn");
  if (refreshLiveBtn) {
    refreshLiveBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      loadLiveMatches();
    });
  }

  if (refreshRoomsBtn) {
    refreshRoomsBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      loadOpenRooms();
      loadLiveMatches();
    });
  }

  // =====================================================
  // TOGGLE EVALUATION ENGINE (HEURISTIC vs NEURAL NETWORK)
  // =====================================================
  window.evaluationEngineMode = "heuristic";
  const engineModeToggle = document.getElementById("engine-mode-toggle");
  const engineModeName = document.getElementById("engine-mode-name");

  if (engineModeToggle && engineModeName) {
    engineModeToggle.addEventListener("click", function(e) {
      e.stopPropagation();
      if (window.evaluationEngineMode === "heuristic") {
        window.evaluationEngineMode = "neural";
        engineModeName.textContent = "Mạng Nơ-ron (NNUE)";
        engineModeToggle.style.background = "rgba(16, 185, 129, 0.15)";
        engineModeToggle.style.color = "#059669";
        engineModeToggle.style.borderColor = "rgba(16, 185, 129, 0.3)";
        showToast("Đã kích hoạt Động cơ Định giá Mạng Nơ-ron (Value Network)", "success");
      } else {
        window.evaluationEngineMode = "heuristic";
        engineModeName.textContent = "Heuristic";
        engineModeToggle.style.background = "rgba(212, 175, 55, 0.15)";
        engineModeToggle.style.color = "var(--accent-gold-dark)";
        engineModeToggle.style.borderColor = "rgba(212, 175, 55, 0.3)";
        showToast("Đã kích hoạt Động cơ Heuristic đa tầng (H1 + H2 + H3)", "info");
      }
      if (lastGameData && window.ArenaUI) {
        window.ArenaUI.update(lastGameData);
      }
    });
  }

  // =====================================================
  // THIẾT LẬP BÀN CỜ TÙY CHỈNH (BOARD EDITOR)
  // =====================================================
  const editorModal = document.getElementById("board-editor-modal");
  const editorOpenBtn = document.getElementById("board-editor-open-btn");
  const editorCloseBtn = document.getElementById("editor-close-btn");
  const editorClearBtn = document.getElementById("editor-clear-btn");
  const editorStandardBtn = document.getElementById("editor-standard-btn");
  const editorPlayBtn = document.getElementById("editor-play-btn");
  const editorFenOutput = document.getElementById("editor-fen-output");
  const editorBoardSvg = document.getElementById("editor-board-svg");
  const editorPiecesLayer = document.getElementById("editor-pieces-layer");

  let editorSelectedPiece = null;
  let editorGrid = Array.from({ length: 10 }, () => Array(9).fill(null));

  function initStandardEditorGrid() {
    editorGrid = Array.from({ length: 10 }, () => Array(9).fill(null));
    const initFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1";
    loadFenIntoEditor(initFen);
  }

  function loadFenIntoEditor(fen) {
    editorGrid = Array.from({ length: 10 }, () => Array(9).fill(null));
    const ranks = (fen || "").trim().split(/\s+/)[0].split("/");
    if (ranks.length === 10) {
      ranks.forEach((rank, r) => {
        let c = 0;
        for (const ch of rank) {
          if (ch >= "1" && ch <= "9") {
            c += parseInt(ch, 10);
          } else {
            const isRed = ch === ch.toUpperCase();
            const lower = ch.toLowerCase();
            const typeMap = { k: "King", a: "Advisor", b: "Elephant", n: "Horse", r: "Rook", c: "Cannon", p: "Pawn" };
            const textMap = {
              k: isRed ? "帥" : "將",
              a: isRed ? "仕" : "士",
              b: isRed ? "相" : "象",
              n: isRed ? "傌" : "馬",
              r: isRed ? "俥" : "車",
              c: isRed ? "炮" : "砲",
              p: isRed ? "兵" : "卒"
            };
            if (typeMap[lower]) {
              editorGrid[r][c] = {
                type: typeMap[lower],
                color: isRed ? "Red" : "Black",
                text: textMap[lower],
                fenChar: ch
              };
            }
            c++;
          }
        }
      });
    }
    renderEditorPieces();
    updateEditorFenOutput();
  }

  function renderEditorPieces() {
    if (!editorPiecesLayer) return;
    editorPiecesLayer.replaceChildren();

    for (let r = 0; r < 10; r++) {
      for (let c = 0; c < 9; c++) {
        const p = editorGrid[r][c];
        if (p) {
          const x = 50 + c * 100;
          const y = 50 + r * 100;
          const g = document.createElementNS(SVG_NS, "g");
          g.setAttribute("class", `editor-piece ${p.color.toLowerCase()}`);
          g.setAttribute("data-row", r);
          g.setAttribute("data-col", c);
          g.style.cursor = "pointer";

          g.innerHTML = `
            <circle cx="${x}" cy="${y}" r="40" fill="#E2C28E" stroke="${p.color === 'Red' ? '#c00000' : '#111'}" stroke-width="2" style="filter: drop-shadow(1px 2px 3px rgba(0,0,0,0.3));"></circle>
            <circle cx="${x}" cy="${y}" r="33" fill="none" stroke="#F5DEB3" stroke-width="2"></circle>
            <text x="${x}" y="${y + 2}" font-family="SimSun, STSong, serif" font-size="46" font-weight="900" text-anchor="middle" dominant-baseline="middle" fill="${p.color === 'Red' ? '#c00000' : '#111'}">${p.text}</text>
          `;
          editorPiecesLayer.appendChild(g);
        }
      }
    }
  }

  function updateEditorFenOutput() {
    if (!editorFenOutput) return;
    const ranks = [];
    for (let r = 0; r < 10; r++) {
      let emptyCount = 0;
      let rankStr = "";
      for (let c = 0; c < 9; c++) {
        const p = editorGrid[r][c];
        if (!p) {
          emptyCount++;
        } else {
          if (emptyCount > 0) {
            rankStr += emptyCount;
            emptyCount = 0;
          }
          const charMap = {
            King: "k", Advisor: "a", Elephant: "b", Horse: "n", Rook: "r", Cannon: "c", Pawn: "p"
          };
          const base = charMap[p.type] || "p";
          rankStr += (p.color === "Red") ? base.toUpperCase() : base.toLowerCase();
        }
      }
      if (emptyCount > 0) rankStr += emptyCount;
      ranks.push(rankStr);
    }

    const turnRadio = document.querySelector('input[name="editor-turn"]:checked');
    const turn = turnRadio ? turnRadio.value : "w";
    editorFenOutput.value = `${ranks.join("/")} ${turn} - - 0 1`;
  }

  if (editorOpenBtn && editorModal) {
    editorOpenBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      initStandardEditorGrid();
      editorModal.classList.remove("hidden");
    });
  }

  if (editorCloseBtn && editorModal) {
    editorCloseBtn.addEventListener("click", () => editorModal.classList.add("hidden"));
  }

  if (editorClearBtn) {
    editorClearBtn.addEventListener("click", () => {
      editorGrid = Array.from({ length: 10 }, () => Array(9).fill(null));
      renderEditorPieces();
      updateEditorFenOutput();
    });
  }

  if (editorStandardBtn) {
    editorStandardBtn.addEventListener("click", () => initStandardEditorGrid());
  }

  // Chọn quân trên khay
  document.querySelectorAll(".editor-piece-btn").forEach(btn => {
    btn.addEventListener("click", function() {
      document.querySelectorAll(".editor-piece-btn").forEach(b => b.style.outline = "none");
      const eraser = document.getElementById("editor-eraser-btn");
      if (eraser) eraser.style.borderColor = "var(--border-color)";

      this.style.outline = "3px solid #d97706";
      editorSelectedPiece = {
        type: this.getAttribute("data-type"),
        color: this.getAttribute("data-color"),
        text: this.getAttribute("data-text")
      };
    });
  });

  const editorEraser = document.getElementById("editor-eraser-btn");
  if (editorEraser) {
    editorEraser.addEventListener("click", function() {
      document.querySelectorAll(".editor-piece-btn").forEach(b => b.style.outline = "none");
      this.style.borderColor = "#ef4444";
      editorSelectedPiece = { isEraser: true };
    });
  }

  // Click vào bàn cờ editor để đặt/xóa quân
  if (editorBoardSvg) {
    editorBoardSvg.addEventListener("click", function(e) {
      const rect = editorBoardSvg.getBoundingClientRect();
      const scaleX = 900 / rect.width;
      const scaleY = 1000 / rect.height;
      const x = (e.clientX - rect.left) * scaleX;
      const y = (e.clientY - rect.top) * scaleY;

      const col = Math.round((x - 50) / 100);
      const row = Math.round((y - 50) / 100);

      if (row >= 0 && row <= 9 && col >= 0 && col <= 8) {
        if (!editorSelectedPiece) {
          // Nếu không chọn quân gì thì click vào quân đang có sẽ xóa nó
          editorGrid[row][col] = null;
        } else if (editorSelectedPiece.isEraser) {
          editorGrid[row][col] = null;
        } else {
          editorGrid[row][col] = {
            type: editorSelectedPiece.type,
            color: editorSelectedPiece.color,
            text: editorSelectedPiece.text
          };
        }
        renderEditorPieces();
        updateEditorFenOutput();
      }
    });
  }

  document.querySelectorAll('input[name="editor-turn"]').forEach(radio => {
    radio.addEventListener("change", updateEditorFenOutput);
  });

  if (editorPlayBtn) {
    editorPlayBtn.addEventListener("click", async function() {
      const fen = editorFenOutput.value.trim();
      if (!fen) return;
      editorModal.classList.add("hidden");
      await createGame(fen);
      showToast("Đã khởi tạo ván cờ từ thế xếp tùy chỉnh của bạn!", "success");
    });
  }

  // =====================================================
  // TRA CỨU KHAI CUỘC (OPENING BOOK EXPLORER)
  // =====================================================
  const openingModal = document.getElementById("opening-explorer-modal");
  const openingOpenBtn = document.getElementById("opening-explorer-open-btn");
  const openingCloseBtn = document.getElementById("opening-explorer-close-btn");
  const openingPracticeBtn = document.getElementById("opening-practice-btn");

  const openingData = {
    1: {
      title: "Pháo Đầu (Trung Pháo) - ECCO-01",
      desc: "Thế trận công thủ toàn diện, Pháo vào trung lộ kiểm soát lộ 5, tạo sức ép thẳng vào cung Tướng đối phương.",
      fen: "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
      moves: [
        { notation: "Pháo 2 bình 5 (C2.5)", win: "58.4%", draw: "24.1%", loss: "17.5%", note: "Chuẩn thức khai cuộc công phá trung lộ dũng mãnh nhất." },
        { notation: "Pháo 8 bình 5 (C8.5)", win: "56.2%", draw: "25.0%", loss: "18.8%", note: "Tả Pháo nhập trung, tạo thế uy hiếp đối xứng." }
      ]
    },
    2: {
      title: "Bình Phong Mã - ECCO-02",
      desc: "Thế trận phòng ngự phản công kinh điển chống lại Trung Pháo. Hai Mã nhảy lên lộ 3 và 7 giữ chặt trung tâm và bảo vệ Tốt đầu vững chắc.",
      fen: "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1",
      moves: [
        { notation: "Mã 8 tiến 7 (M8.7)", win: "52.1%", draw: "30.4%", loss: "17.5%", note: "Phương án Bình Phong Mã cổ điển kiên cố nhất." },
        { notation: "Mã 2 tiến 3 (M2.3)", win: "51.5%", draw: "31.2%", loss: "17.3%", note: "Bình Phong Mã hiện đại kết hợp tiến Tốt 7 hoặc 3." }
      ]
    },
    3: {
      title: "Thuận Pháo - ECCO-03",
      desc: "Hai bên cùng đưa Pháo vào cùng một lộ trung tâm, thế cờ đối công kịch liệt, ăn miếng trả miếng không khoan nhượng.",
      fen: "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1",
      moves: [
        { notation: "Pháo 8 bình 5 (C8.5)", win: "49.8%", draw: "22.5%", loss: "27.7%", note: "Đối công rực lửa, quyết chiến từ những nước đầu tiên." }
      ]
    },
    4: {
      title: "Tiên Nhân Chỉ Lộ - ECCO-04",
      desc: "Bên Đỏ khai cuộc bằng nước Tốt 7 tiến 1 hoặc Tốt 3 tiến 1 để thăm dò ý đồ chiến thuật của đối phương.",
      fen: "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
      moves: [
        { notation: "Binh 7 tiến 1 (P7.1)", win: "54.2%", draw: "28.6%", loss: "17.2%", note: "Mở đường Mã thông suốt, linh hoạt chuyển biến theo phản ứng đối phương." }
      ]
    },
    5: {
      title: "Khởi Mã Cuộc (Trận Thượng Tiên) - ECCO-05",
      desc: "Khai cuộc bằng nước phát triển Mã sớm, cấu trúc phòng thủ kiên cố, linh hoạt và chuyển biến sâu sắc.",
      fen: "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
      moves: [
        { notation: "Mã 2 tiến 3 (H2.3)", win: "53.0%", draw: "33.5%", loss: "13.5%", note: "Lối đánh trầm tĩnh, dẻo dai thích hợp cho kỳ thủ chuộng trung tàn cuộc." }
      ]
    }
  };

  let currentOpeningId = 1;

  function renderOpeningDetail(id) {
    currentOpeningId = id;
    const data = openingData[id];
    if (!data) return;

    const titleEl = document.getElementById("opening-detail-title");
    const descEl = document.getElementById("opening-detail-desc");
    const movesTable = document.getElementById("opening-moves-table");

    if (titleEl) titleEl.textContent = data.title;
    if (descEl) descEl.textContent = data.desc;

    if (movesTable) {
      let html = "";
      data.moves.forEach(m => {
        html += `
          <div style="background: var(--bg-navy-panel); border: 1px solid var(--border-color); border-radius: 8px; padding: 10px 12px;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px;">
              <strong style="color: #c00000; font-size: 13.5px;">${m.notation}</strong>
              <div style="font-size: 11.5px; font-weight: 700; display: flex; gap: 8px;">
                <span style="color: #10b981;">Thắng: ${m.win}</span>
                <span style="color: var(--text-muted);">Hòa: ${m.draw}</span>
                <span style="color: #ef4444;">Thua: ${m.loss}</span>
              </div>
            </div>
            <div style="font-size: 12px; color: var(--text-muted); line-height: 1.4;">${m.note}</div>
          </div>
        `;
      });
      movesTable.innerHTML = html;
    }
  }

  if (openingOpenBtn && openingModal) {
    openingOpenBtn.addEventListener("click", function(e) {
      e.stopPropagation();
      renderOpeningDetail(1);
      openingModal.classList.remove("hidden");
    });
  }

  if (openingCloseBtn && openingModal) {
    openingCloseBtn.addEventListener("click", () => openingModal.classList.add("hidden"));
  }

  document.querySelectorAll(".opening-menu-item").forEach(item => {
    item.addEventListener("click", function() {
      document.querySelectorAll(".opening-menu-item").forEach(i => {
        i.classList.remove("active");
        i.style.background = "transparent";
        i.style.borderColor = "transparent";
      });
      this.classList.add("active");
      this.style.background = "rgba(212, 175, 55, 0.15)";
      this.style.borderColor = "rgba(212, 175, 55, 0.3)";

      const id = parseInt(this.getAttribute("data-id"), 10);
      renderOpeningDetail(id);
    });
  });

  if (openingPracticeBtn) {
    openingPracticeBtn.addEventListener("click", async function() {
      const data = openingData[currentOpeningId];
      if (!data) return;
      openingModal.classList.add("hidden");
      await createGame(data.fen);
      showToast(`Đã mở bàn cờ luyện tập thế trận: ${data.title}!`, "success");
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


