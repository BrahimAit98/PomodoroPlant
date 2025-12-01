// plant.js
(() => {
  'use strict';

  // ========== UTILITY FUNCTIONS ==========

  function pseudoRandom(index) {
    const x = Math.sin(index * 12.9898 + 78.233) * 43758.5453;
    return x - Math.floor(x);
  }

  function lerpColor(c1, c2, t) {
    const r1 = parseInt(c1.substring(1, 3), 16);
    const g1 = parseInt(c1.substring(3, 5), 16);
    const b1 = parseInt(c1.substring(5, 7), 16);
    const r2 = parseInt(c2.substring(1, 3), 16);
    const g2 = parseInt(c2.substring(3, 5), 16);
    const b2 = parseInt(c2.substring(5, 7), 16);
    const r = Math.round(r1 + (r2 - r1) * t);
    const g = Math.round(g1 + (g2 - g1) * t);
    const b = Math.round(b1 + (b2 - b1) * t);
    return `#${((1 << 24) + (r << 16) + (g << 8) + b)
      .toString(16)
      .slice(1)
      .toUpperCase()
      .padStart(6, '0')}`;
  }

  // ========== L-SYSTEM PLANT LOGIC ==========

  const CONFIG = {
    axiom: "X",
    rules: {
      X: "F[+X][-X]FX",
      F: "FF"
    },
    angle: 35,
    iterations: 5,
    baseLength: 18
  };

  let lSystemString = "";
let canvas = null;
let ctx = null;

// The plant will never be less grown than this.
// 0.35 = 35% grown as a "standard" default.
const BASE_GROWTH = 0.35;

// Actual value used when drawing:
let plantProgress = 1;

let plantBounds = {
  minX: 0,
  maxX: 0,
  minY: 0,
  maxY: 0,
  width: 0,
  height: 0,
  centerX: 0
};

  function generateString() {
    let current = CONFIG.axiom;
    for (let i = 0; i < CONFIG.iterations; i++) {
      let next = "";
      for (let char of current) {
        next += CONFIG.rules[char] || char;
      }
      current = next;
    }
    lSystemString = current;
  }

  function measurePlant() {
    let x = 0, y = 0, dir = -Math.PI / 2;
    let stack = [];
    let minX = 0, maxX = 0, minY = 0, maxY = 0;
    const len = CONFIG.baseLength;

    for (let i = 0; i < lSystemString.length; i++) {
      const char = lSystemString[i];
      if (char === 'F') {
        x += Math.cos(dir) * len;
        y += Math.sin(dir) * len;
        minX = Math.min(minX, x);
        maxX = Math.max(maxX, x);
        minY = Math.min(minY, y);
        maxY = Math.max(maxY, y);
      } else if (char === '+') {
        dir += (CONFIG.angle * Math.PI) / 180;
      } else if (char === '-') {
        dir -= (CONFIG.angle * Math.PI) / 180;
      } else if (char === '[') {
        stack.push({ x, y, dir });
      } else if (char === ']') {
        const state = stack.pop();
        x = state.x;
        y = state.y;
        dir = state.dir;
      }
    }

    plantBounds = {
      minX,
      maxX,
      minY,
      maxY,
      width: maxX - minX,
      height: maxY - minY,
      centerX: (minX + maxX) / 2
    };
  }

  function initCanvas() {
    canvas = document.getElementById("plantCanvas");
    if (!canvas) return;
    ctx = canvas.getContext("2d");

    const wrapper = canvas.parentElement;
    const dpr = window.devicePixelRatio || 1;
    const w = wrapper.offsetWidth;
    const h = wrapper.offsetHeight;

    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';

    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  // ========== DRAWING FUNCTIONS ==========

  function drawPot(ctx, x, y, size) {
    ctx.save();
    ctx.translate(x, y);

    ctx.fillStyle = "#c06b3e";
    ctx.fillRect(-size / 2 - 4, 0, size + 8, 12);

    ctx.beginPath();
    ctx.moveTo(-size / 2, 12);
    ctx.lineTo(size / 2, 12);
    ctx.lineTo(size / 2 - 8, size * 0.8);
    ctx.lineTo(-size / 2 + 8, size * 0.8);
    ctx.closePath();
    ctx.fillStyle = "#d37b4a";
    ctx.fill();

    ctx.beginPath();
    ctx.ellipse(0, 0, size / 2 - 2, 6, 0, 0, Math.PI * 2);
    ctx.fillStyle = "#3e2723";
    ctx.fill();

    ctx.restore();
  }

  function drawDetailedLeaf(ctx, size) {
    const leafColor = "#4E7B43";
    const highlightColor = "#83A96A";
    const veinColor = "#3D5F36";

    ctx.save();

    const stemLen = size * 1.1;
    ctx.strokeStyle = veinColor;
    ctx.lineWidth = Math.max(0.8, size * 0.1);
    ctx.lineCap = "round";
    ctx.beginPath();
    ctx.moveTo(0, 0);
    ctx.lineTo(0, -stemLen);
    ctx.stroke();

    const drawLeaflet = (w, h, offsetY, side) => {
      ctx.save();
      ctx.translate(side * w * 0.5, offsetY);
      ctx.rotate(side * (Math.PI / 12));

      ctx.fillStyle = leafColor;
      ctx.beginPath();
      ctx.ellipse(0, 0, w, h, 0, 0, Math.PI * 2);
      ctx.fill();

      ctx.fillStyle = highlightColor;
      ctx.globalAlpha = 0.45;
      ctx.beginPath();
      ctx.ellipse(-w * 0.2, -h * 0.15, w * 0.55, h * 0.5, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.globalAlpha = 1;

      ctx.strokeStyle = veinColor;
      ctx.lineWidth = Math.max(0.5, w * 0.08);
      ctx.beginPath();
      ctx.moveTo(-w * 0.1, -h * 0.5);
      ctx.lineTo(w * 0.15, h * 0.5);
      ctx.stroke();

      ctx.restore();
    };

    const steps = 3;
    for (let i = 0; i < steps; i++) {
      const t = (i + 1) / (steps + 1);
      const y = -stemLen * t;
      const baseW = size * (0.75 - 0.2 * t);
      const baseH = baseW * 0.7;

      drawLeaflet(baseW, baseH, y, 1);
      drawLeaflet(baseW, baseH, y, -1);
    }

    const tipW = size * 0.8;
    const tipH = size * 1.1;
    ctx.translate(0, -stemLen);
    ctx.fillStyle = leafColor;
    ctx.beginPath();
    ctx.ellipse(0, 0, tipW, tipH, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = highlightColor;
    ctx.globalAlpha = 0.5;
    ctx.beginPath();
    ctx.ellipse(-tipW * 0.25, -tipH * 0.25, tipW * 0.5, tipH * 0.5, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.globalAlpha = 1;

    ctx.restore();
  }

  function drawRealisticTomato(ctx, size, ripeness) {
    ctx.save();

    let baseColor;
    if (ripeness < 0.3) {
      baseColor = lerpColor("#6E8C3A", "#D08C3A", ripeness / 0.3);
    } else {
      baseColor = lerpColor("#D08C3A", "#C53A34", (ripeness - 0.3) / 0.7);
    }

    const grad = ctx.createRadialGradient(
      -size * 0.3,
      -size * 0.3,
      size * 0.1,
      0,
      0,
      size
    );
    const lightColor = lerpColor(baseColor, "#FFFFFF", 0.25);
    const darkColor = lerpColor(baseColor, "#5A1612", 0.35);
    grad.addColorStop(0, lightColor);
    grad.addColorStop(0.7, baseColor);
    grad.addColorStop(1, darkColor);

    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(0, 0, size, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = "rgba(255, 255, 255, 0.18)";
    ctx.beginPath();
    ctx.ellipse(
      -size * 0.28,
      -size * 0.38,
      size * 0.2,
      size * 0.12,
      Math.PI / 5,
      0,
      Math.PI * 2
    );
    ctx.fill();

    ctx.fillStyle = "#3C6A2A";
    ctx.beginPath();
    const calyxRadius = size * 0.55;
    const innerRadius = size * 0.18;
    for (let i = 0; i < 5; i++) {
      const angle = (Math.PI * 2 * i) / 5 - Math.PI / 2;
      ctx.lineTo(Math.cos(angle) * calyxRadius, Math.sin(angle) * calyxRadius);
      ctx.lineTo(
        Math.cos(angle + 0.35) * innerRadius,
        Math.sin(angle + 0.35) * innerRadius
      );
    }
    ctx.closePath();
    ctx.fill();

    ctx.restore();
  }

  function drawLSystemRecursive(ctx) {
    const currentLen = CONFIG.baseLength;
    const stateStack = [];

    ctx.lineCap = "round";
    ctx.lineJoin = "round";

    let currentAngle = -Math.PI / 2;

    for (let i = 0; i < lSystemString.length; i++) {
      const char = lSystemString[i];
      const rnd = pseudoRandom(i);

      if (char === 'F') {
        const relPos = i / lSystemString.length;

        const thickness = Math.max(1.2, 9 * Math.pow(1 - relPos, 0.8));
        ctx.lineWidth = thickness;

        const stemColor = lerpColor("#476243", "#76996A", relPos);
        ctx.strokeStyle = stemColor;

        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, -currentLen);
        ctx.stroke();
        ctx.translate(0, -currentLen);

        if (plantProgress > 0.25 && relPos > 0.2 && i % 5 === 0) {
          ctx.save();
          const side = (rnd > 0.5) ? 1 : -1;
          ctx.rotate(side * (Math.PI / 4));
          const leafSize = currentLen * (0.9 + 0.3 * (1 - relPos)) * plantProgress;
          drawDetailedLeaf(ctx, leafSize);
          ctx.restore();
        }

      } else if (char === 'X') {
        const relPos = i / lSystemString.length;
        if (plantProgress > 0.3 && relPos > 0.25 && relPos < 0.9 && i % 6 === 0) {
          const ripeness = Math.min(1, (plantProgress - 0.3) / 0.7);

          ctx.save();

          ctx.rotate(-currentAngle + Math.PI);

          ctx.strokeStyle = "#43a047";
          ctx.lineWidth = 2;
          ctx.beginPath();
          ctx.moveTo(0, 0);
          ctx.lineTo(0, 12);
          ctx.stroke();
          ctx.translate(0, 12);

          const baseSize = currentLen * 1.6;
          drawRealisticTomato(ctx, baseSize, ripeness);

          if (pseudoRandom(i + 101) > 0.65) {
            ctx.save();
            ctx.translate(baseSize * 0.95, baseSize * 0.15);
            drawRealisticTomato(ctx, baseSize * 0.85, ripeness);
            ctx.restore();
          }

          ctx.restore();
        }
      } else if (char === '+') {
        const angleChange = (CONFIG.angle + rnd * 5) * Math.PI / 180;
        ctx.rotate(angleChange);
        currentAngle += angleChange;
      } else if (char === '-') {
        const angleChange = -(CONFIG.angle + rnd * 5) * Math.PI / 180;
        ctx.rotate(angleChange);
        currentAngle += angleChange;
      } else if (char === '[') {
        stateStack.push({ t: ctx.getTransform(), a: currentAngle });
      } else if (char === ']') {
        const state = stateStack.pop();
        ctx.setTransform(state.t);
        currentAngle = state.a;
      }
    }
  }

  function drawPlantLoop() {
    if (!ctx || !canvas) return;

    const w = canvas.width / (window.devicePixelRatio || 1);
    const h = canvas.height / (window.devicePixelRatio || 1);

    ctx.clearRect(0, 0, w, h);

    const cx = w / 2;
    const cy = h / 2;
    const radius = Math.min(w, h) / 2 - 20;

    ctx.save();
    ctx.beginPath();
    ctx.arc(cx, cy, radius, 0, Math.PI * 2);
    ctx.clip();

    const potY = cy + radius * 0.6;
    const potSize = radius * 0.5;
    drawPot(ctx, cx, potY, potSize);

    const availableH = radius * 1.5;
    const availableW = radius * 1.4;

    const pW = plantBounds.width || 1;
    const pH = plantBounds.height || 1;

    const scaleH = availableH / pH;
    const scaleW = availableW / pW;
    const fitScale = Math.min(scaleH, scaleW) * 0.9;

    ctx.translate(cx, potY);

    const currentScale = fitScale * Math.max(0.05, plantProgress);
    const scaleX = currentScale * 1.25;
    const scaleY = currentScale * 0.9;
    ctx.scale(scaleX, scaleY);

    ctx.translate(-plantBounds.centerX, -plantBounds.maxY);

    drawLSystemRecursive(ctx);

    ctx.restore();
    requestAnimationFrame(drawPlantLoop);
  }

// Expose a hook for the timer to control growth
// progress is 0..1 from the timer, we map it to BASE_GROWTH..1
window.setPlantProgress = function setPlantProgress(progress) {
  const clamped = Math.max(0, Math.min(1, progress));
  plantProgress = progress <= 0 ? 1 : progress;
};

  window.addEventListener("load", () => {
    generateString();
    measurePlant();
    initCanvas();
    drawPlantLoop();
  });

  window.addEventListener("resize", () => {
    initCanvas();
  });
})();