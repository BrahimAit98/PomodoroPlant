// timer.js
(() => {
  "use strict";

  // ========== TIMER CONFIG ==========

  const DURATIONS = {
    focus: 25 * 60,
    short: 5 * 60,
    long: 15 * 60,
  };

  let sessionsUntilLongBreak = 4;
  let completedSessions = 0;
  let autoStartBreaks = false;
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

  // ========== LOAD USER SETTINGS ==========

  function loadUserSettings() {
    return fetch("/Account/GetTimerSettings", {
      method: "GET",
    })
      .then((res) => res.json())
      .then((data) => {
        if (data.focusDuration) DURATIONS.focus = data.focusDuration * 60;
        if (data.shortBreak) DURATIONS.short = data.shortBreak * 60;
        if (data.longBreak) DURATIONS.long = data.longBreak * 60;
        if (data.sessionsUntilLongBreak)
          sessionsUntilLongBreak = data.sessionsUntilLongBreak;
        if (data.autoStartBreaks !== undefined)
          autoStartBreaks = data.autoStartBreaks;

        remainingTime = DURATIONS[currentMode];
        updateDisplay();

        console.log("Loaded custom timer settings:", data);
      })
      .catch((err) => {
        console.log("Using default timer settings");
      });
  }

  // ========== CUSTOM MODAL ==========

  function showModal(message, onYes, onNo) {
    // Create modal overlay
    const overlay = document.createElement("div");
    overlay.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(33, 95, 58, 0.15);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 9999;
      animation: fadeIn 0.3s ease;
    `;

    // Create modal box
    const modal = document.createElement("div");
    modal.style.cssText = `
      background: white;
      padding: 40px;
      border-radius: 16px;
      max-width: 420px;
      width: 90%;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
      border: 1px solid rgba(33, 95, 58, 0.1);
      animation: slideUp 0.4s cubic-bezier(0.16, 1, 0.3, 1);
    `;

    modal.innerHTML = `
      <div style="text-align: center;">
        <div style="
          width: 80px;
          height: 80px;
          margin: 0 auto 20px;
          background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 40px;
        ">🍅</div>
        <p style="
          font-size: 20px;
          font-weight: 600;
          color: #1a1a1a;
          margin: 0 0 30px 0;
          line-height: 1.6;
        ">${message}</p>
        <div style="display: flex; gap: 12px; justify-content: center;">
          <button id="modalYes" style="
            background: #215f3a;
            color: white;
            border: none;
            padding: 14px 32px;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            box-shadow: 0 4px 12px rgba(33, 95, 58, 0.2);
          ">Yes, take a break</button>
          <button id="modalNo" style="
            background: white;
            color: #666;
            border: 2px solid #e0e0e0;
            padding: 14px 32px;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
          ">Skip</button>
        </div>
      </div>
    `;

    overlay.appendChild(modal);
    document.body.appendChild(overlay);

    // Add hover effects
    const yesBtn = modal.querySelector("#modalYes");
    const noBtn = modal.querySelector("#modalNo");

    yesBtn.onmouseover = () => {
      yesBtn.style.background = "#1a4d2e";
      yesBtn.style.transform = "translateY(-2px)";
      yesBtn.style.boxShadow = "0 6px 16px rgba(33, 95, 58, 0.3)";
    };
    yesBtn.onmouseout = () => {
      yesBtn.style.background = "#215f3a";
      yesBtn.style.transform = "translateY(0)";
      yesBtn.style.boxShadow = "0 4px 12px rgba(33, 95, 58, 0.2)";
    };

    noBtn.onmouseover = () => {
      noBtn.style.background = "#f5f5f5";
      noBtn.style.borderColor = "#ccc";
    };
    noBtn.onmouseout = () => {
      noBtn.style.background = "white";
      noBtn.style.borderColor = "#e0e0e0";
    };

    // Handle clicks
    yesBtn.onclick = () => {
      overlay.style.animation = "fadeOut 0.2s ease";
      setTimeout(() => {
        document.body.removeChild(overlay);
        if (onYes) onYes();
      }, 200);
    };

    noBtn.onclick = () => {
      overlay.style.animation = "fadeOut 0.2s ease";
      setTimeout(() => {
        document.body.removeChild(overlay);
        if (onNo) onNo();
      }, 200);
    };

    // Close on overlay click
    overlay.onclick = (e) => {
      if (e.target === overlay) {
        overlay.style.animation = "fadeOut 0.2s ease";
        setTimeout(() => {
          document.body.removeChild(overlay);
          if (onNo) onNo();
        }, 200);
      }
    };

    // Add animations
    const style = document.createElement("style");
    style.textContent = `
      @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
      }
      @keyframes fadeOut {
        from { opacity: 1; }
        to { opacity: 0; }
      }
      @keyframes slideUp {
        from { 
          transform: translateY(30px) scale(0.95);
          opacity: 0;
        }
        to { 
          transform: translateY(0) scale(1);
          opacity: 1;
        }
      }
    `;
    document.head.appendChild(style);
  }

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
    logSessionToServer();
    buzzESP(); // ← This WILL buzz for breaks too! ✅

    completionSound.currentTime = 0;
    completionSound.play().catch(() => {}); // ← This WILL play for breaks too! ✅

    // Track completed focus sessions
    if (currentMode === "focus") {
      completedSessions++;

      // Long break after X sessions
      if (completedSessions >= sessionsUntilLongBreak) {
        completedSessions = 0;

        if (autoStartBreaks) {
          setTimeout(() => {
            setMode("long");
            startTimer();
          }, 500);
        } else {
          setTimeout(() => {
            showModal(
              `You've completed ${sessionsUntilLongBreak} focus sessions! 🎉<br><br>Take a long break?`,
              () => {
                setMode("long");
                startTimer();
              },
              () => {
                /* onNo - do nothing */
              }
            );
          }, 500);
        }
      } else {
        // Short break
        if (autoStartBreaks) {
          setTimeout(() => {
            setMode("short");
            startTimer();
          }, 500);
        } else {
          setTimeout(() => {
            showModal(
              "Focus session complete!<br><br>Take a short break?",
              () => {
                setMode("short");
                startTimer();
              },
              () => {
                /* onNo - do nothing */
              }
            );
          }, 500);
        }
      }
    } else {
      // ← ADD THIS: Break completed (short or long)
      if (autoStartBreaks) {
        setTimeout(() => {
          setMode("focus");
          startTimer();
        }, 500);
      } else {
        setTimeout(() => {
          showModal(
            "Break complete!<br><br>Ready to focus again?",
            () => {
              setMode("focus");
              startTimer();
            },
            () => {
              setMode("focus"); // Just switch, don't auto-start
            }
          );
        }, 500);
      }
    }

    // Desktop notification
    if (typeof window !== "undefined" && "Notification" in window) {
      if (Notification.permission === "granted") {
        new Notification("Pomodoro complete! 🍅");
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

    if (typeof window.setPlantProgress === "function") {
      window.setPlantProgress(0);
    }

    updateModeButtonAria();
    updateDisplay();
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

    loadUserSettings().then(() => {
      updateModeButtonAria();
      updateDisplay();
      sendModeToServer(currentMode);
    });
  }

  window.addEventListener("load", initTimer);
})();
