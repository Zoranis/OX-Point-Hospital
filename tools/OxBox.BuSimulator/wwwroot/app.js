"use strict";

// Polls the simulator and renders it. Everything the page does goes through the control API.

const POLL_INTERVAL_MS = 300;
const LOCKS_PER_BOARD = 16;

const elements = {
  clients: document.getElementById("clients"),
  scanForm: document.getElementById("scan-form"),
  scanCode: document.getElementById("scan-code"),
  quickCodes: document.getElementById("quick-codes"),
  slots: document.getElementById("slots"),
  greenLamp: document.getElementById("lamp-green"),
  redLamp: document.getElementById("lamp-red"),
  keypadLed: document.getElementById("keypad-led"),
  sound: document.getElementById("sound"),
  traffic: document.getElementById("traffic"),
};

let previous = { green: false, red: false };
let renderedQuickCodes = false;
const slotButtons = new Map();

async function post(path, body) {
  const response = await fetch(path, {
    method: "POST",
    headers: body ? { "Content-Type": "application/json" } : {},
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!response.ok) {
    console.warn(`${path} failed with ${response.status}`);
  }
}

function scan(code) {
  return post("/api/scan", { code });
}

// Positions are wired in order (see Box.CreateWithSequentialWiring): CU0 locks 1–16 are slots 1–16, CU1 starts at 17.
function slotNumber(lock) {
  return lock.board * LOCKS_PER_BOARD + lock.lock;
}

function renderSlots(locks) {
  for (const lock of locks) {
    const key = `${lock.board}-${lock.lock}`;
    let button = slotButtons.get(key);
    if (!button) {
      button = createSlotButton(lock);
      slotButtons.set(key, button);
      elements.slots.appendChild(button);
    }
    updateSlotButton(button, lock);
  }
}

// Each slot button is created once and then only updated, so a click is never lost to a re-render.
function createSlotButton(lock) {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "slot";
  button.innerHTML = `<strong>Slot ${slotNumber(lock)}</strong><span>CU${lock.board} · L${lock.lock}</span><span class="state"></span>`;
  button.addEventListener("click", () => {
    if (button.dataset.state === "open") {
      post(`/api/locks/${lock.board}/${lock.lock}/close`).then(refresh);
    }
  });
  button.addEventListener("contextmenu", (event) => {
    event.preventDefault();
    post(`/api/locks/${lock.board}/${lock.lock}/toggle-jam`).then(refresh);
  });
  return button;
}

function updateSlotButton(button, lock) {
  const state = lock.isJammed ? "jammed" : lock.isOpen ? "open" : "closed";
  if (button.dataset.state === state) {
    return;
  }
  button.dataset.state = state;
  button.classList.toggle("open", state === "open");
  button.classList.toggle("jammed", state === "jammed");
  button.title = state === "open" ? "Close this door" : "Right-click to jam or free the lock";
  button.setAttribute("aria-label", `Slot ${slotNumber(lock)}, ${state}`);
  button.querySelector(".state").textContent = state;
}

function renderQuickCodes(codes) {
  if (renderedQuickCodes) {
    return;
  }
  renderedQuickCodes = true;
  for (const code of codes) {
    const chip = document.createElement("button");
    chip.type = "button";
    chip.textContent = code;
    chip.title = `Scan ${code}`;
    chip.addEventListener("click", () => scan(code));
    elements.quickCodes.appendChild(chip);
  }
}

function setLamp(lamp, on, label) {
  lamp.classList.toggle("on", on);
  lamp.setAttribute("aria-label", `${label} ${on ? "on" : "off"}`);
}

function renderSignals(state) {
  setLamp(elements.greenLamp, state.greenLampOn, "Green lamp");
  setLamp(elements.redLamp, state.redLampAndBuzzerOn, "Red lamp and buzzer");
  elements.keypadLed.classList.toggle("on-green", state.keypadLed === "green");
  elements.keypadLed.classList.toggle("on-red", state.keypadLed === "red");

  if (elements.sound.checked) {
    if (state.greenLampOn && !previous.green) beep(880, 0.15);
    if (state.redLampAndBuzzerOn && !previous.red) beep(220, 0.8);
  }
  previous = { green: state.greenLampOn, red: state.redLampAndBuzzerOn };
}

function renderTraffic(entries) {
  const fragment = document.createDocumentFragment();
  for (const entry of entries) {
    const item = document.createElement("li");
    item.className = entry.direction === "In" ? "in" : "out";
    const time = new Date(entry.at).toLocaleTimeString();
    const direction = entry.direction === "In" ? "→ BU" : "← BU";
    item.innerHTML = `<span class="dir">${direction}</span>`;
    item.append(`${time}  ${entry.json}`);
    fragment.appendChild(item);
  }
  elements.traffic.replaceChildren(fragment);
}

function render(state) {
  const count = state.connectedClients;
  elements.clients.textContent = count === 0 ? "No server connected" : `${count} server connection${count > 1 ? "s" : ""}`;
  elements.clients.classList.toggle("connected", count > 0);
  renderQuickCodes(state.quickScanCodes);
  renderSlots(state.locks);
  renderSignals(state);
  renderTraffic(state.traffic);
}

let audio;
function beep(frequency, seconds) {
  audio ??= new AudioContext();
  const oscillator = audio.createOscillator();
  const gain = audio.createGain();
  oscillator.frequency.value = frequency;
  gain.gain.value = 0.08;
  oscillator.connect(gain).connect(audio.destination);
  oscillator.start();
  oscillator.stop(audio.currentTime + seconds);
}

async function refresh() {
  try {
    const response = await fetch("/api/state");
    if (response.ok) {
      render(await response.json());
    }
  } catch (error) {
    elements.clients.textContent = "Simulator not reachable";
    elements.clients.classList.remove("connected");
  }
}

elements.scanForm.addEventListener("submit", (event) => {
  event.preventDefault();
  scan(elements.scanCode.value).then(() => elements.scanCode.select());
});

refresh();
setInterval(refresh, POLL_INTERVAL_MS);
