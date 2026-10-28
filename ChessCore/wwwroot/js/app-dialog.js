/**
 * App Global Dialog System
 * Thay thế hoàn toàn browser window.alert & window.confirm bằng modal giao diện đồng bộ, sang trọng.
 */
window.AppDialog = (function () {
  let overlay, iconEl, titleEl, msgEl, cancelBtn, confirmBtn;

  function init() {
    overlay = document.getElementById("app-dialog-overlay");
    iconEl = document.getElementById("app-dialog-icon");
    titleEl = document.getElementById("app-dialog-title");
    msgEl = document.getElementById("app-dialog-message");
    cancelBtn = document.getElementById("app-dialog-cancel-btn");
    confirmBtn = document.getElementById("app-dialog-confirm-btn");
  }

  function show({ title = "Thông báo", message = "", type = "info", confirmText = "Đồng ý", cancelText = "Hủy bỏ", showCancel = false, isDanger = false }) {
    if (!overlay) init();
    if (!overlay) return Promise.resolve(false);

    return new Promise((resolve) => {
      titleEl.textContent = title;
      msgEl.textContent = message;

      // Icon & Type
      iconEl.className = `app-dialog-icon-circle ${type}`;
      if (type === "warning") {
        iconEl.innerHTML = '<i class="fa-solid fa-triangle-exclamation"></i>';
      } else if (type === "danger") {
        iconEl.innerHTML = '<i class="fa-solid fa-circle-exmark"></i>';
      } else {
        iconEl.innerHTML = '<i class="fa-solid fa-circle-info"></i>';
      }

      // Buttons
      confirmBtn.textContent = confirmText;
      confirmBtn.className = isDanger ? "app-dialog-btn app-dialog-btn-danger" : "app-dialog-btn app-dialog-btn-confirm";

      if (showCancel) {
        cancelBtn.style.display = "inline-flex";
        cancelBtn.textContent = cancelText;
      } else {
        cancelBtn.style.display = "none";
      }

      function cleanUp() {
        overlay.classList.remove("show");
        confirmBtn.removeEventListener("click", onConfirm);
        cancelBtn.removeEventListener("click", onCancel);
      }

      function onConfirm() {
        cleanUp();
        resolve(true);
      }

      function onCancel() {
        cleanUp();
        resolve(false);
      }

      confirmBtn.addEventListener("click", onConfirm);
      cancelBtn.addEventListener("click", onCancel);

      overlay.classList.add("show");
    });
  }

  return {
    alert: function (message, title = "Thông báo", type = "info") {
      return show({ title, message, type, showCancel: false, confirmText: "Đã hiểu" });
    },
    confirm: function (message, title = "Xác nhận", { confirmText = "Đồng ý", cancelText = "Hủy bỏ", isDanger = false, type = "warning" } = {}) {
      return show({ title, message, type, showCancel: true, confirmText, cancelText, isDanger });
    }
  };
})();

/**
 * App Confetti Celebration Engine
 * Hiệu ứng pháo hoa giấy rực rỡ, nhẹ nhàng khi người chơi chiến thắng ván cờ hoặc giải đúng cờ thế
 */
window.AppConfetti = (function () {
  let activeCanvas = null;
  let animId = null;

  const COLORS = ["#f59e0b", "#d97706", "#ef4444", "#10b981", "#3b82f6", "#8b5cf6", "#ec4899", "#fcd34d"];

  function fire({ particleCount = 85, duration = 3200 } = {}) {
    if (activeCanvas) {
      cancelAnimationFrame(animId);
      if (activeCanvas.parentNode) activeCanvas.parentNode.removeChild(activeCanvas);
      activeCanvas = null;
    }

    const canvas = document.createElement("canvas");
    canvas.style.position = "fixed";
    canvas.style.top = "0";
    canvas.style.left = "0";
    canvas.style.width = "100vw";
    canvas.style.height = "100vh";
    canvas.style.pointerEvents = "none";
    canvas.style.zIndex = "999999";
    document.body.appendChild(canvas);
    activeCanvas = canvas;

    const ctx = canvas.getContext("2d");
    const width = (canvas.width = window.innerWidth);
    const height = (canvas.height = window.innerHeight);

    const particles = [];
    for (let i = 0; i < particleCount; i++) {
      particles.push({
        x: width * (0.2 + 0.6 * Math.random()),
        y: height * 0.4 + (Math.random() * 60 - 30),
        vx: (Math.random() - 0.5) * 16,
        vy: -Math.random() * 15 - 5,
        size: Math.random() * 9 + 6,
        color: COLORS[Math.floor(Math.random() * COLORS.length)],
        rotation: Math.random() * 360,
        rotationSpeed: (Math.random() - 0.5) * 12,
        opacity: 1
      });
    }

    const startTime = Date.now();

    function render() {
      const elapsed = Date.now() - startTime;
      if (elapsed > duration) {
        if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
        activeCanvas = null;
        return;
      }

      ctx.clearRect(0, 0, width, height);

      const fadeStart = duration * 0.65;
      const globalAlpha = elapsed > fadeStart ? Math.max(0, 1 - (elapsed - fadeStart) / (duration - fadeStart)) : 1;

      particles.forEach((p) => {
        p.x += p.vx;
        p.y += p.vy;
        p.vy += 0.42; // Gravity
        p.vx *= 0.985; // Air resistance
        p.rotation += p.rotationSpeed;

        ctx.save();
        ctx.globalAlpha = Math.max(0, globalAlpha * p.opacity);
        ctx.translate(p.x, p.y);
        ctx.rotate((p.rotation * Math.PI) / 180);
        ctx.fillStyle = p.color;
        ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 0.6);
        ctx.restore();
      });

      animId = requestAnimationFrame(render);
    }

    animId = requestAnimationFrame(render);
  }

  return { fire };
})();
