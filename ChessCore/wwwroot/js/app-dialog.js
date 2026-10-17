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
