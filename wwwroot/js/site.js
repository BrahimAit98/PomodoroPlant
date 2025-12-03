// timer.js
(() => {
  "use strict";

  // ========== TIMER CONFIG ==========

  const DURATIONS = {
    focus: 0.1 * 60,
    short: 5 * 60,
    long: 15 * 60,
  };

  let currentMode = "focus";
  let remainingTime = DURATIONS[currentMode];
  let timerInterval = null;
  let isRunning = false;
  let timerCompleted = false;
  let endTimestamp = null;

  let timerEl;
  let labelEl;
  let startPauseBtn;
  let resetBtn;
  let modeButtons;

  const completionSound = new Audio(
    "data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBSuByPDTgjMGHm7A7+OZSA0PVqzn77BdGAU+ltryxnMoBSh+zPLaizsIGGS57OihUBELTKXh8bllHAU2jdXzzn0vBSV7yvHejj8JE1yw6+6oVBMKRp/g8r5sIQUrgcjw04IzBh5uwO/jmUgND1as5++wXRgFPpba8sZzKAUofczy2os7CBhkuezmn1ASC0yl4fG5ZRwNNo3V8859LwUle8rx3o4/CRNcsOvuqFQTCkaf4PK+bCEFK4HI8NOCMwYebs"
  );

  // ========== HELPERS ==========

  function formatTime(seconds) {
    const m = String(Math.floor(seconds / 60)).padStart(2, "0");
    const s = String(seconds % 60).padStart(2, "0");
    return `${m}:${s}`;
  }

  function updatePlantProgress() {
    const total = DURATIONS[currentMode];
    const elapsed = total - remainingTime;
    const progress = Math.max(0, Math.min(1, elapsed / total));

    // Tell the plant to grow/shrink:
    if (typeof window.setPlantProgress === "function") {
      window.setPlantProgress(progress);
    }
  }

  function updateModeButtonAria() {
    modeButtons.forEach((btn) => {
      const btnMode = btn.getAttribute("data-mode");
      const isActive = btnMode === currentMode;
      btn.setAttribute("aria-pressed", isActive ? "true" : "false");
      if (isActive) btn.classList.add("mode-btn--active");
      else btn.classList.remove("mode-btn--active");
    });
  }

  function updateDisplay() {
    if (timerEl) timerEl.textContent = formatTime(remainingTime);

    if (labelEl) {
      if (currentMode === "focus") {
        labelEl.textContent = timerCompleted
          ? "Pomodoro finished"
          : isRunning
          ? "Focus session in progress..."
          : "Focus session ready to start";
      } else if (currentMode === "short") {
        labelEl.textContent = timerCompleted
          ? "Short break finished"
          : "Taking a short break";
      } else {
        labelEl.textContent = timerCompleted
          ? "Long break finished"
          : "Taking a longer break";
      }
    }

    if (startPauseBtn) {
      startPauseBtn.textContent = isRunning ? "Pause" : "Start timer";
      startPauseBtn.disabled = timerCompleted;
    }

    // only plant now, no ring
    updatePlantProgress();
  }

  // ========== ESP BUZZER ==========

  function buzzESP() {
    fetch("/Pomodoro/Buzz", {
      method: "GET",
    })
      .then((res) => res.text())
      .then((txt) => console.log("Server response:", txt))
      .catch((err) => console.error("Server error:", err));
  }

  function sendModeToServer(mode) {
    const seconds = Math.round(DURATIONS[mode]);
    const csrfToken = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    )?.value;

    fetch("/Pomodoro/UpdateMode", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: csrfToken,
      },
      body: JSON.stringify({
        mode: mode,
        seconds: seconds,
      }),
    })
      .then((res) => res.text())
      .then((txt) => console.log("Mode update response:", txt))
      .catch((err) => console.error("Mode update error:", err));
  }

  function logSessionToServer() {
    const total = DURATIONS[currentMode];
    const csrfToken = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    )?.value;

    fetch("/Analytics/TrackSession", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: csrfToken,
      },
      body: JSON.stringify({
        mode: currentMode,
        durationSeconds: total,
      }),
    }).catch((err) => console.error("TrackSession error:", err));
  }

  function notifyCompletion() {
    // log to your backend
    logSessionToServer();

    // Trigger the physical buzzer
    buzzESP();

    completionSound.currentTime = 0;
    completionSound.play().catch(() => {});

    if (typeof window !== "undefined" && "Notification" in window) {
      if (Notification.permission === "granted") {
        new Notification("Pomodoro voltooid! 🍅");
      } else if (Notification.permission !== "denied") {
        Notification.requestPermission();
      }
    }
  }

  // ========== TIMER FLOW ==========

  function tick() {
    if (!isRunning || endTimestamp == null) return;
    const now = Date.now();
    const diffSeconds = Math.max(0, Math.round((endTimestamp - now) / 1000));
    remainingTime = diffSeconds;
    updateDisplay();

    if (remainingTime <= 0) {
      remainingTime = 0;
      stopTimerInternal(false);
      timerCompleted = true;
      updateDisplay();
      notifyCompletion();
    }
  }

  function startTimer() {
    if (isRunning || timerCompleted) return;
    isRunning = true;
    endTimestamp = Date.now() + remainingTime * 1000;
    updateDisplay();
    timerInterval = window.setInterval(tick, 250);
  }

  function stopTimerInternal(updateUI = true) {
    isRunning = false;
    endTimestamp = null;
    if (timerInterval !== null) {
      clearInterval(timerInterval);
      timerInterval = null;
    }
    if (updateUI) updateDisplay();
  }

  function stopTimer() {
    stopTimerInternal(true);
  }

  function resetTimer() {
    stopTimerInternal(false);
    remainingTime = DURATIONS[currentMode];
    timerCompleted = false;
    // reset plant growth
    if (typeof window.setPlantProgress === "function") {
      window.setPlantProgress(0);
    }
    updateDisplay();
  }

  function setMode(mode) {
    if (!DURATIONS[mode]) return;
    currentMode = mode;
    stopTimerInternal(false);
    remainingTime = DURATIONS[mode];
    timerCompleted = false;

    // reset plant
    if (typeof window.setPlantProgress === "function") {
      window.setPlantProgress(0);
    }

    updateModeButtonAria();
    updateDisplay();

    // tell ESP to update its screen
    sendModeToServer(mode);
  }

  // ========== INIT ==========

  function initTimer() {
    timerEl = document.getElementById("timer");
    labelEl = document.getElementById("label");
    startPauseBtn = document.getElementById("start-pause");
    resetBtn = document.getElementById("reset");
    modeButtons = document.querySelectorAll(".mode-btn");

    if (startPauseBtn) {
      startPauseBtn.addEventListener("click", () => {
        isRunning ? stopTimer() : startTimer();
      });
    }

    if (resetBtn) {
      resetBtn.addEventListener("click", resetTimer);
    }

    modeButtons.forEach((btn) =>
      btn.addEventListener("click", () => {
        const newMode = btn.getAttribute("data-mode");
        if (newMode) setMode(newMode);
      })
    );

    // Initial state
    updateModeButtonAria();
    updateDisplay();

    // tell ESP initial mode (focus) when page loads
    sendModeToServer(currentMode);
  }

  window.addEventListener("load", initTimer);
})();
