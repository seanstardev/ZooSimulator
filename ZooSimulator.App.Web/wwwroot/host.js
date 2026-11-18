"use strict";
// Set synchronously: actors.js can load graphics while the WASM runtime starts.
window.zooHostReady = (async function () {
  const host = new EventTarget();
  const heading = document.querySelector("#loading h2");
  const detail = document.querySelector("#loading p");
  let application;
  const diagnostics = [];
  window.zooBrowserState = { ready: false, diagnostics };
  globalThis.zooBrowserPostMessage = (json) => {
    const message = JSON.parse(json);
    host.dispatchEvent(new MessageEvent("message", { data: message }));
  };
  globalThis.zooBrowserDiagnostic = (message) => {
    // Bound diagnostic memory during long sessions.
    if (diagnostics.length === 20) diagnostics.shift();
    diagnostics.push(message);
    console.info("Zoo:", message);
  };
  globalThis.zooBrowserMessageBox = (title, message, isYesNo) => {
    if (isYesNo) return window.confirm(title + "\n\n" + message);
    window.alert(title + "\n\n" + message);
    return true;
  };
  try {
    detail.textContent = "Loading the game runtime and animal models…";
    const { dotnet } = await import("./_framework/dotnet.js");
    const runtime = await dotnet.create();
    const config = runtime.getConfig();
    const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
    application = exports.ZooSimulator.App.Web.Program;
    host.postMessage = (message) => application.Receive(JSON.stringify(message));
    await runtime.runMain(config.mainAssemblyName, []);
    window.zooBrowserState.ready = true;
    window.addEventListener("pagehide", (event) => {
      if (!event.persisted) application.Shutdown().catch(console.error);
    });
    return host;
  } catch (error) {
    document.getElementById("loading").style.display = "flex";
    heading.textContent = "The zoo could not start";
    detail.textContent = "Please refresh. If this persists, check that all upload files are present and WASM is served correctly. " + error.message;
    throw error;
  }
})();
