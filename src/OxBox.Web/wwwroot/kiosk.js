"use strict";

// Browser-side helpers for the unattended box display.
window.oxboxKiosk = (() => {
  const LANGUAGE_KEY = "oxbox.kiosk.language";
  const PROBE_INTERVAL_MS = 5000;

  // Storage can be unavailable (private mode, blocked site data); the display then just uses its default.
  function loadLanguage() {
    try {
      return localStorage.getItem(LANGUAGE_KEY);
    } catch {
      return null;
    }
  }

  function saveLanguage(code) {
    try {
      localStorage.setItem(LANGUAGE_KEY, code);
    } catch {
      // Not persisted; the choice still applies until the page reloads.
    }
    showReconnectTextIn(code);
  }

  // The "out of service" screen is rendered once, in every language; show the current one.
  function showReconnectTextIn(code) {
    const modal = document.getElementById("components-reconnect-modal");
    if (modal && code) {
      modal.dataset.language = code;
    }
  }

  // When Blazor gives up reconnecting (for example after a long server outage), keep checking
  // whether the server is back and reload only then. Reloading while it is still down would
  // replace the "out of service" screen with the browser's own error page, which never retries.
  function reloadWhenServerIsBack() {
    const modal = document.getElementById("components-reconnect-modal");
    if (!modal) {
      return;
    }

    let probing = false;

    async function serverIsUp() {
      try {
        const response = await fetch(location.href, {
          cache: "no-store",
          signal: AbortSignal.timeout(PROBE_INTERVAL_MS),
        });
        return response.ok;
      } catch {
        return false;
      }
    }

    async function probeUntilServerIsUp() {
      if (await serverIsUp()) {
        location.reload();
      } else {
        setTimeout(probeUntilServerIsUp, PROBE_INTERVAL_MS);
      }
    }

    new MutationObserver(() => {
      const gaveUp = modal.classList.contains("components-reconnect-failed")
        || modal.classList.contains("components-reconnect-rejected");
      if (gaveUp && !probing) {
        probing = true;
        setTimeout(probeUntilServerIsUp, PROBE_INTERVAL_MS);
      }
    }).observe(modal, { attributes: true, attributeFilter: ["class"] });
  }

  // Development only: a barcode reader plugged into this PC types the code in one burst and ends
  // with Enter. Collect that burst and send it to the server, which handles it as if the box's BU
  // had read it. Nothing is shown. Some readers pause between characters, so the gap is generous;
  // a longer pause starts a new code.
  const MAX_SCAN_KEY_GAP_MS = 300;
  const MIN_SCAN_LENGTH = 3;
  let capturingScans = false;

  function captureScans(scanUrl) {
    if (capturingScans) {
      return;
    }

    capturingScans = true;
    let buffer = "";
    let lastKeyAt = 0;

    document.addEventListener("keydown", (event) => {
      const now = performance.now();
      if (now - lastKeyAt > MAX_SCAN_KEY_GAP_MS) {
        buffer = "";
      }
      lastKeyAt = now;

      if (event.key === "Enter") {
        if (buffer.length >= MIN_SCAN_LENGTH) {
          sendScan(scanUrl, buffer);
        }
        buffer = "";
      } else if (event.key.length === 1) {
        buffer += event.key;
      }
    });
  }

  async function sendScan(scanUrl, code) {
    try {
      await fetch(scanUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ code }),
      });
    } catch {
      // The server is down; the display already shows "out of service".
    }
  }

  showReconnectTextIn(loadLanguage());
  reloadWhenServerIsBack();

  return { loadLanguage, saveLanguage, captureScans };
})();
