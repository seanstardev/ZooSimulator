"use strict";
// A small presentation-only scene: fixed 96px render target, no shadows or postprocessing.
window.createClockMascot = async function (canvas) {
  const B = window.BABYLON;
  const engine = new B.Engine(canvas, true, {
    stencil: false,
    powerPreference: "low-power",
  });
  engine.setSize(96, 96);
  engine.maxFPS = 15;
  engine.renderEvenInBackground = false;
  const scene = new B.Scene(engine);
  scene.clearColor = new B.Color4(251 / 255, 249 / 255, 241 / 255, 1);
  const camera = new B.FreeCamera(
    "pip-camera",
    new B.Vector3(0, 1.2, -4),
    scene,
  );
  camera.setTarget(new B.Vector3(0, 0.85, 0));
  camera.mode = B.Camera.ORTHOGRAPHIC_CAMERA;
  camera.orthoLeft = -1.15;
  camera.orthoRight = 1.15;
  camera.orthoTop = 1.15;
  camera.orthoBottom = -1.15;
  new B.HemisphericLight("pip-light", new B.Vector3(-0.3, 1, -0.5), scene);
  const container = await B.SceneLoader.LoadAssetContainerAsync(
    "models/",
    "chick.glb",
    scene,
  );
  container.addAllToScene();
  const root = new B.TransformNode("pip", scene);
  container.rootNodes.forEach((node) => (node.parent = root));
  const meshes = root.getChildMeshes();
  let min = new B.Vector3(Infinity, Infinity, Infinity),
    max = new B.Vector3(-Infinity, -Infinity, -Infinity);
  meshes.forEach((mesh) => {
    mesh.computeWorldMatrix(true);
    const bounds = mesh.getBoundingInfo().boundingBox;
    min = B.Vector3.Minimize(min, bounds.minimumWorld);
    max = B.Vector3.Maximize(max, bounds.maximumWorld);
    mesh.isPickable = false;
  });
  const scale = 1.3 / (max.y - min.y);
  root.scaling.setAll(scale);
  root.position.y = -min.y * scale;
  let clip = null,
    excitedUntil = 0;
  const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)");
  function play(name) {
    if (name === clip) return;
    container.animationGroups.forEach((group) => group.stop());
    container.animationGroups.find((group) => group.name === name)?.start(true);
    clip = name;
  }
  play("idle");
  engine.runRenderLoop(() => {
    const now = performance.now();
    const excited = now < excitedUntil;
    play(excited && !reducedMotion.matches ? "gesture-positive" : "idle");
    root.rotation.y =
      Math.PI +
      (reducedMotion.matches
        ? 0
        : Math.sin(now * 0.0006) * 0.24 + Math.sin(now * 0.00023) * 0.12);
    scene.render();
  });
  window.addEventListener(
    "pagehide",
    () => {
      scene.dispose();
      engine.dispose();
    },
    { once: true },
  );
  return {
    engine,
    scene,
    root,
    celebrate() {
      excitedUntil = performance.now() + 1650;
    },
    get clip() {
      return clip;
    },
  };
};
