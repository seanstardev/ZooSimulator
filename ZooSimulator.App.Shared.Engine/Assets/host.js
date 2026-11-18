"use strict";
// Desktop transport; the browser publish supplies its own host bootstrap.
window.zooHostReady = Promise.resolve(window.chrome?.webview);
