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

  showReconnectTextIn(loadLanguage());
  reloadWhenServerIsBack();

  return { loadLanguage, saveLanguage };
})();
