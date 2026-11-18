"use strict";
// Rendering-only simulation: wandering and poses never mutate authoritative health/time.
window.createZooScene = async function (canvas, labels) {
  const B = window.BABYLON;
  const engine = new B.Engine(canvas, true, {
    preserveDrawingBuffer: false,
    stencil: true,
  });
  engine.setHardwareScalingLevel(Math.max(1, window.devicePixelRatio / 1.5));
  const scene = new B.Scene(engine);
  scene.clearColor = B.Color4.FromHexString("#e5ead9ff");
  scene.ambientColor = new B.Color3(0.5, 0.55, 0.45);
  const camera = new B.ArcRotateCamera(
    "zoo-camera",
    -Math.PI / 2,
    0.91,
    34,
    new B.Vector3(0, 0, 0.7),
    scene,
  );
  camera.mode = B.Camera.ORTHOGRAPHIC_CAMERA;
  camera.minZ = 0.1;
  const light = new B.HemisphericLight(
    "soft-sky",
    new B.Vector3(0, 1, 0),
    scene,
  );
  light.intensity = 0.8;
  const sun = new B.DirectionalLight(
    "sun",
    new B.Vector3(-0.5, -1, 0.5),
    scene,
  );
  sun.intensity = 0.45;
  sun.position = new B.Vector3(5, 15, -8);
  const shadow = new B.ShadowGenerator(1024, sun);
  shadow.useBlurExponentialShadowMap = true;
  shadow.blurKernel = 16;
  const materials = {};
  function material(name, hex) {
    if (materials[name]) return materials[name];
    const mat = new B.StandardMaterial(name, scene);
    mat.diffuseColor = B.Color3.FromHexString(hex);
    mat.specularColor = B.Color3.Black();
    materials[name] = mat;
    return mat;
  }
  function box(name, x, y, z, width, height, depth, mat) {
    const mesh = B.MeshBuilder.CreateBox(name, { width, height, depth }, scene);
    mesh.position.set(x, y, z);
    mesh.material = mat;
    mesh.receiveShadows = true;
    mesh.isPickable = false;
    return mesh;
  }
  const earth = material("earth", "#c8af80");
  const grass = material("grass", "#b5cb91");
  const path = material("path", "#e4d4ae");
  const fence = material("fence", "#a7885d");
  const trunk = material("trunk", "#917350");
  const leaf = material("leaf", "#789b6b");
  box("island-base", 0, -0.45, 0, 35, 0.8, 14, earth);
  box("lawn", 0, -0.02, 0, 34.8, 0.18, 13.8, grass);
  box("front-walk", 0, 0.09, -5.5, 34, 0.06, 1.5, path);
  const centers = [-11, 0, 11];
  const names = ["MONKEY GROVE", "GIRAFFE GARDEN", "ELEPHANT MEADOW"];
  const animals = new Map();
  let generation = null;
  let measureLabels = true;
  let highlightedId = null;
  let hoverPointer = null,
    hoveredId = null,
    lastHoverCheck = 0;
  const selectionColour = B.Color3.FromHexString("#efc86c");
  const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)");
  const svgNamespace = "http://www.w3.org/2000/svg";
  const links = document.createElementNS(svgNamespace, "svg");
  links.classList.add("label-links");
  labels.appendChild(links);
  function enclosure(center, index) {
    box(
      "habitat-" + index,
      center,
      0.09,
      0.7,
      9.6,
      0.1,
      9.4,
      material("habitat-" + index, ["#a5bf7f", "#c6cb93", "#a8c79b"][index]),
    );
    for (const z of [-4.1, 5.4]) {
      box("rail", center, 0.8, z, 9.7, 0.1, 0.13, fence);
      box("rail", center, 0.45, z, 9.7, 0.1, 0.13, fence);
      for (let i = -4; i <= 4; i += 2)
        box("post", center + i, 0.55, z, 0.16, 1.1, 0.16, fence);
    }
    for (const x of [center - 4.85, center + 4.85]) {
      box("rail", x, 0.8, 0.65, 0.13, 0.1, 9.5, fence);
      box("rail", x, 0.45, 0.65, 0.13, 0.1, 9.5, fence);
      for (let z = -3.5; z < 5; z += 2)
        box("post", x, 0.55, z, 0.16, 1.1, 0.16, fence);
    }
    const texture = new B.DynamicTexture(
      "sign-" + index,
      { width: 512, height: 80 },
      scene,
      false,
    );
    texture.drawText(
      names[index],
      null,
      52,
      "bold 30px Segoe UI",
      "#31513b",
      "#f4ebd0",
      true,
    );
    const signMat = new B.StandardMaterial("sign-mat-" + index, scene);
    signMat.diffuseTexture = texture;
    signMat.specularColor = B.Color3.Black();
    const sign = B.MeshBuilder.CreatePlane(
      "sign",
      { width: 4.3, height: 0.66 },
      scene,
    );
    sign.position.set(center, 0.4, -5.6);
    sign.rotation.x = Math.PI / 3;
    sign.material = signMat;
    sign.isPickable = false;
  }
  centers.forEach(enclosure);
  for (let i = 0; i < 16; i++) {
    const x = -16 + i * 2.1,
      z = 6.4 + (i % 3) * 0.28;
    const stem = B.MeshBuilder.CreateCylinder(
      "tree-trunk",
      { height: 1.2, diameter: 0.2, tessellation: 5 },
      scene,
    );
    stem.position.set(x, 0.65, z);
    stem.material = trunk;
    stem.isPickable = false;
    const crown = B.MeshBuilder.CreateCylinder(
      "tree-crown",
      {
        height: 1.9 + (i % 3) * 0.25,
        diameterTop: 0,
        diameterBottom: 1.8,
        tessellation: 6,
      },
      scene,
    );
    crown.position.set(x, 1.9, z);
    crown.material = leaf;
    crown.isPickable = false;
    shadow.addShadowCaster(crown);
  }
  for (const x of [-16.6, 16.6]) {
    for (let z = -2; z <= 4; z += 3) {
      const bush = B.MeshBuilder.CreateSphere(
        "bush",
        { diameter: 1.4, segments: 4 },
        scene,
      );
      bush.position.set(x, 0.5, z);
      bush.scaling.y = 0.7;
      bush.material = leaf;
      bush.isPickable = false;
    }
  }
  // Static low-poly shoreline: no water shader, animated geometry or extra textures.
  const pondPieces = [];
  const pondX = 14.3,
    pondZ = 3.9;
  const bank = B.MeshBuilder.CreateCylinder(
    "pond-bank",
    { height: 0.08, diameter: 2.4, tessellation: 12 },
    scene,
  );
  bank.position.set(pondX, 0.18, pondZ);
  bank.scaling.set(1.12, 1, 0.82);
  bank.material = material("pond-bank", "#9aab84");
  pondPieces.push(bank);
  const pool = B.MeshBuilder.CreateCylinder(
    "pond-water",
    { height: 0.035, diameter: 2.1, tessellation: 16 },
    scene,
  );
  pool.position.set(pondX, 0.23, pondZ);
  pool.scaling.set(1.12, 1, 0.82);
  pool.material = material("water", "#7daea9");
  pondPieces.push(pool);
  const stoneMaterial = material("pond-stone", "#a1a597");
  for (const [index, angle] of [0.1, 0.7, 1.6, 2.6, 3.3, 4.4].entries()) {
    const rock = B.MeshBuilder.CreateSphere(
      "pond-rock",
      { diameter: 0.38 + (index % 2) * 0.12, segments: 3 },
      scene,
    );
    rock.position.set(
      pondX + Math.cos(angle) * 1.22,
      0.28,
      pondZ + Math.sin(angle) * 0.94,
    );
    rock.scaling.set(1.15, 0.65, 0.85);
    rock.rotation.y = angle;
    rock.material = stoneMaterial;
    pondPieces.push(rock);
  }
  for (let i = 0; i < 8; i++) {
    const x = pondX - 0.9 + (i % 4) * 0.2,
      z = pondZ + 0.68 + Math.floor(i / 4) * 0.17;
    const reed = B.MeshBuilder.CreateCylinder(
      "pond-reed",
      { height: 0.5 + (i % 3) * 0.12, diameter: 0.035, tessellation: 3 },
      scene,
    );
    reed.position.set(x, 0.47, z);
    reed.rotation.z = ((i % 3) - 1) * 0.12;
    reed.material = leaf;
    pondPieces.push(reed);
    if (i % 2 === 0) {
      const tip = B.MeshBuilder.CreateCylinder(
        "reed-tip",
        { height: 0.14, diameter: 0.075, tessellation: 4 },
        scene,
      );
      tip.position.set(x, 0.74 + (i % 3) * 0.06, z);
      tip.material = trunk;
      pondPieces.push(tip);
    }
  }
  const ripple = B.MeshBuilder.CreateTorus(
    "pond-ripple",
    { diameter: 0.65, thickness: 0.018, tessellation: 16 },
    scene,
  );
  ripple.position.set(pondX + 0.2, 0.26, pondZ - 0.1);
  ripple.scaling.z = 0.7;
  ripple.material = material("pond-ripple", "#c0d9cd");
  pondPieces.push(ripple);
  // Merge static decoration; shared materials retain their original colours.
  const pond = B.Mesh.MergeMeshes(
    pondPieces,
    true,
    true,
    undefined,
    false,
    true,
  );
  pond.isPickable = false;
  pond.freezeWorldMatrix();
  const containers = await Promise.all(
    ["monkey", "giraffe", "elephant"].map((name) =>
      B.SceneLoader.LoadAssetContainerAsync("models/", name + ".glb", scene),
    ),
  );
  const speciesNames = ["Monkey", "Giraffe", "Elephant"];
  const heights = [1.15, 2.25, 1.5];
  const radii = [0.6, 0.6, 0.82];
  const positions = [
    [-3, -1.9],
    [0, -2.1],
    [3, -1.8],
    [-1.55, 1.5],
    [1.55, 1.5],
  ];
  function spawn(data) {
    const entry = containers[data.species].instantiateModelsToScene(
      (name) => data.id + "-" + name,
      false,
      { doNotInstantiate: true },
    );
    const root = new B.TransformNode("animal-" + data.id, scene);
    entry.rootNodes.forEach((node) => (node.parent = root));
    const meshes = root.getChildMeshes();
    let min = new B.Vector3(Infinity, Infinity, Infinity),
      max = new B.Vector3(-Infinity, -Infinity, -Infinity);
    meshes.forEach((mesh) => {
      mesh.computeWorldMatrix(true);
      const bounds = mesh.getBoundingInfo().boundingBox;
      min = B.Vector3.Minimize(min, bounds.minimumWorld);
      max = B.Vector3.Maximize(max, bounds.maximumWorld);
      mesh.metadata = { animalId: data.id };
      shadow.addShadowCaster(mesh);
    });
    const scale = heights[data.species] / Math.max(0.01, max.y - min.y);
    root.scaling.setAll(scale * 0.001);
    const initial = positions[Math.floor((data.id - 1) / 3) % 5];
    root.position.set(
      centers[data.species] + initial[0],
      0.15 - min.y * scale,
      initial[1],
    );
    const label = document.createElement("div");
    label.className = "animal-label";
    label.dataset.animalId = data.id;
    label.innerHTML = `<div class="health-card"><strong>${speciesNames[data.species]} ${Math.floor((data.id - 1) / 3) + 1}</strong><span class="value"></span><div class="bar"><span></span></div><em></em></div>`;
    labels.appendChild(label);
    const effects = document.createElement("div");
    effects.className = "animal-effects";
    effects.dataset.animalId = data.id;
    const drop =
      '<svg viewBox="0 0 12 18" aria-hidden="true"><path d="M6 0C5 5 0 8 0 12A6 6 0 0012 12C12 8 7 5 6 0Z" fill="#74b7d0"/><path d="M3 11Q2 14 5 15" fill="none" stroke="#d8f4ff" stroke-width="1.4" stroke-linecap="round"/></svg>';
    effects.innerHTML = `<span class="sweat-drop">${drop}</span><span class="sweat-drop">${drop}</span>`;
    labels.appendChild(effects);
    const tail = document.createElementNS(svgNamespace, "path");
    tail.classList.add("label-tail");
    links.appendChild(tail);
    const dot = document.createElementNS(svgNamespace, "circle");
    dot.classList.add("label-tail-dot");
    dot.setAttribute("r", "2.5");
    links.appendChild(dot);
    const animal = {
      id: data.id,
      species: data.species,
      root,
      entry,
      label,
      effects,
      meshes,
      fedAt: -Infinity,
      pulseActive: false,
      health: 100,
      status: 0,
      radius: Math.max(
        radii[data.species],
        (Math.hypot(max.x - min.x, max.z - min.z) * scale) / 2,
      ),
      baseY: root.position.y,
      baseScale: scale,
      bornAt: performance.now(),
      tail,
      dot,
      labelX: null,
      labelY: null,
      labelLift: 0,
      target: null,
      pause: Math.random() * 2,
      clip: null,
      eatUntil: 0,
    };
    animals.set(data.id, animal);
    play(animal, "idle");
    return animal;
  }
  function play(animal, name, loop = true) {
    if (animal.clip === name) return;
    animal.entry.animationGroups.forEach((group) => group.stop());
    const group = animal.entry.animationGroups.find((g) =>
      g.name.toLowerCase().endsWith(name),
    );
    if (group) group.start(loop, 1);
    animal.clip = name;
  }
  function show(snapshot) {
    measureLabels = true;
    const nextGeneration = snapshot.generation ?? 0;
    if (generation !== null && nextGeneration !== generation) {
      animals.forEach((animal) => {
        animal.root
          .getChildMeshes()
          .forEach((mesh) => shadow.removeShadowCaster(mesh));
        animal.entry.animationGroups.forEach((group) => group.dispose());
        animal.entry.skeletons.forEach((skeleton) => skeleton.dispose());
        animal.root.dispose(false, false);
        animal.label.remove();
        animal.effects.remove();
        animal.tail.remove();
        animal.dot.remove();
      });
      animals.clear();
      highlightedId = null;
    }
    generation = nextGeneration;
    snapshot.animals.forEach((data) => {
      const animal = animals.get(data.id) || spawn(data);
      const gain = data.health - animal.health;
      const recovered = gain > 0.001;
      animal.health = data.health;
      animal.status = data.status;
      animal.label.querySelector(".value").textContent =
        Math.round(data.health) + "%";
      animal.label.querySelector(".bar span").style.width = data.health + "%";
      animal.label.classList.toggle(
        "weak",
        data.status === 1 || data.health < 55,
      );
      animal.label.classList.toggle("dead", data.status === 2);
      animal.label.classList.toggle(
        "low",
        data.status !== 2 &&
          (data.status === 1 || data.health < [45, 65, 85][data.species]),
      );
      animal.effects.classList.toggle(
        "low",
        animal.label.classList.contains("low"),
      );
      if (recovered) feedFeedback(animal, gain);
      animal.label.querySelector("em").textContent =
        data.status === 2
          ? "REST IN PEACE"
          : data.status === 1
            ? "CANNOT WALK"
            : "LOW HEALTH";
      if (data.status === 2) {
        animal.entry.animationGroups.forEach((group) => group.stop());
        animal.root.rotation.z = Math.PI / 2;
        animal.root.position.y = 0.65;
        animal.clip = "dead";
        if (highlightedId === animal.id) highlight(null);
      } else if (recovered) react(data.id);
    });
  }
  function feedFeedback(animal, gain = 0) {
    const now = performance.now();
    animal.fedAt = now;
    animal.pulseActive = true;
    let burst = animal.effects.querySelector(".feed-burst");
    if (burst && now - Number(burst.dataset.startedAt) < 250) {
      if (gain > 0)
        burst.querySelector(".health-gain").textContent =
          `+${gain < 0.5 ? "<1" : Math.round(gain)}%`;
      return;
    }
    animal.label.classList.add("fed");
    burst = document.createElement("span");
    burst.className = "feed-burst";
    burst.dataset.startedAt = now;
    burst.innerHTML = `<span class="health-gain">${gain > 0 ? `+${gain < 0.5 ? "<1" : Math.round(gain)}%` : ""}</span><i>✦</i><i>✧</i><i>✦</i><i>✧</i>`;
    animal.effects.appendChild(burst);
    if (!reducedMotion.matches)
      animal.label
        .querySelector(".health-card")
        .animate(
          [
            { transform: "scale(1)" },
            { transform: "scale(1.12)", offset: 0.35 },
            { transform: "scale(0.97)", offset: 0.7 },
            { transform: "scale(1)" },
          ],
          { duration: 440, easing: "ease-out" },
        );
    burst.addEventListener("animationend", (event) => {
      if (event.target !== burst) return;
      burst.remove();
      if (!animal.effects.querySelector(".feed-burst"))
        animal.label.classList.remove("fed");
    });
  }
  function react(id) {
    const animal = animals.get(id);
    if (!animal || animal.status === 2) return;
    feedFeedback(animal);
    animal.eatUntil = performance.now() + 2100;
    play(animal, "eat", false);
  }
  // A per-mesh overlay only while hovered; materials and full-screen buffers are untouched.
  function highlight(id) {
    id = id ?? null;
    if (id === highlightedId) return;
    const previous = animals.get(highlightedId);
    previous?.meshes.forEach((mesh) => (mesh.renderOverlay = false));
    const selected = animals.get(id);
    highlightedId = selected && selected.status !== 2 ? id : null;
    if (highlightedId !== null)
      selected.meshes.forEach((mesh) => {
        mesh.overlayColor = selectionColour;
        mesh.overlayAlpha = 0.32;
        mesh.renderOverlay = true;
      });
  }
  // Coalesce ordinary pointer movement; the existing frame loop owns the short linger.
  function hoverAt(x, y) {
    hoverPointer = x === null ? null : { x, y };
  }
  function pick(clientX, clientY) {
    let best = null,
      bestScore = Infinity,
      paintOrder = 0;
    // Use the painted card, including its pulse, before considering padded hit areas.
    for (const animal of animals.values()) {
      paintOrder++;
      if (animal.label.style.visibility === "hidden") continue;
      const rect = animal.label
        .querySelector(".health-card")
        .getBoundingClientRect();
      const dx = Math.max(rect.left - clientX, 0, clientX - rect.right);
      const dy = Math.max(rect.top - clientY, 0, clientY - rect.bottom);
      if (dx > 10 || dy > 10) continue;
      const inside = dx === 0 && dy === 0;
      const distance = Math.hypot(
        clientX - (rect.left + rect.right) / 2,
        clientY - (rect.top + rect.bottom) / 2,
      );
      const score = inside ? -paintOrder : 10000 + distance;
      if (score < bestScore) {
        best = animal;
        bestScore = score;
      }
    }
    if (best) return best;
    const rect = canvas.getBoundingClientRect();
    if (
      clientX < rect.left ||
      clientX > rect.right ||
      clientY < rect.top ||
      clientY > rect.bottom
    )
      return null;
    const hit = scene.pick(
      clientX - rect.left,
      clientY - rect.top,
      (mesh) => !!mesh.metadata?.animalId,
    );
    return hit?.hit ? animals.get(hit.pickedMesh.metadata.animalId) : null;
  }
  function wander(animal, dt, now) {
    if (animal.status === 2) return;
    if (animal.status === 1) {
      play(animal, "idle");
      animal.root.rotation.z = 0.1;
      return;
    }
    animal.root.rotation.z = 0;
    if (animal.eatUntil > now) return;
    animal.pause -= dt;
    if (animal.pause > 0) {
      play(animal, "idle");
      return;
    }
    const halfWidth = 4.85 - animal.radius - 0.2;
    const minZ = -4.1 + animal.radius + 0.2;
    const maxZ = 5.4 - animal.radius - 0.2;
    if (!animal.target) {
      animal.target = new B.Vector3(
        centers[animal.species] + (Math.random() - 0.5) * halfWidth * 2,
        animal.baseY,
        minZ + Math.random() * (maxZ - minZ),
      );
    }
    const delta = animal.target.subtract(animal.root.position);
    delta.y = 0;
    const distance = delta.length();
    if (distance < 0.12) {
      animal.target = null;
      animal.pause = 1 + Math.random() * 3;
      return;
    }
    const desiredHeading = Math.atan2(delta.x, delta.z);
    const turn = Math.atan2(
      Math.sin(desiredHeading - animal.root.rotation.y),
      Math.cos(desiredHeading - animal.root.rotation.y),
    );
    animal.root.rotation.y += Math.max(-5.5 * dt, Math.min(5.5 * dt, turn));
    const alignment = Math.max(0.12, Math.cos(turn));
    const step = delta.scale(
      Math.min(distance, dt * 0.75 * alignment) / distance,
    );
    const next = animal.root.position.add(step);
    let blocked = false;
    for (const other of animals.values()) {
      if (other.id !== animal.id && other.species === animal.species) {
        const dx = other.root.position.x - next.x,
          dz = other.root.position.z - next.z;
        if (
          dx * dx + dz * dz <
          Math.pow(other.radius + animal.radius + 0.12, 2)
        )
          blocked = true;
      }
    }
    if (animal.species === 2) {
      const currentDistance = Math.hypot(
        animal.root.position.x - pondX,
        animal.root.position.z - pondZ,
      );
      const nextDistance = Math.hypot(next.x - pondX, next.z - pondZ);
      if (nextDistance < animal.radius + 1.15 && nextDistance < currentDistance)
        blocked = true;
    }
    if (blocked) {
      animal.target = null;
      animal.pause = 0.3 + Math.random();
      play(animal, "idle");
      return;
    }
    next.x = Math.max(
      centers[animal.species] - halfWidth,
      Math.min(centers[animal.species] + halfWidth, next.x),
    );
    next.z = Math.max(minZ, Math.min(maxZ, next.z));
    animal.root.position.copyFrom(next);
    play(animal, "walk");
  }
  function resize() {
    engine.resize();
    measureLabels = true;
    const aspect = engine.getRenderWidth() / engine.getRenderHeight();
    const h = Math.max(8.5, 18 / aspect);
    camera.orthoLeft = -h * aspect;
    camera.orthoRight = h * aspect;
    camera.orthoTop = h;
    camera.orthoBottom = -h;
    scene.updateTransformMatrix(true);
  }
  let resizePending = true;
  new ResizeObserver(() => {
    resizePending = true;
  }).observe(canvas);
  engine.maxFPS = 30;
  let last = performance.now();
  const world = {
    show,
    pick,
    react,
    highlight,
    hoverAt,
    animals,
    scene,
    engine,
  };
  engine.runRenderLoop(() => {
    const now = performance.now();
    const resized = resizePending;
    if (resized) {
      resize();
      resizePending = false;
    }
    const dt = Math.min(0.1, (now - last) / 1000);
    last = now;
    const viewWidth = canvas.clientWidth,
      viewHeight = canvas.clientHeight;
    if (measureLabels) {
      // Batch layout reads before any per-frame DOM writes.
      animals.forEach((animal) => {
        animal.labelWidth = animal.label.offsetWidth;
        animal.labelHeight = animal.label.offsetHeight;
      });
      measureLabels = false;
    }
    // Drag hit-testing shares this read phase instead of forcing layout after writes.
    world.updateDrag?.();
    // Read hit geometry before frame writes, at most ten times per second.
    if (hoverPointer && now - lastHoverCheck >= 100) {
      hoveredId = pick(hoverPointer.x, hoverPointer.y)?.id ?? null;
      lastHoverCheck = now;
    } else if (!hoverPointer) hoveredId = null;
    const labelBoxes = [];
    animals.forEach((animal) => {
      if (animal.id === hoveredId || animal.id === highlightedId)
        animal.hoverUntil = now + 650;
      const emphasised = now < (animal.hoverUntil ?? 0);
      if (animal.emphasised !== emphasised) {
        animal.label.classList.toggle("hovered", emphasised);
        animal.emphasised = emphasised;
      }
      const arrival = Math.min(1, (now - animal.bornAt) / 550);
      const pulseTime = (now - animal.fedAt) / 450;
      const pulsing = pulseTime >= 0 && pulseTime < 1 && !reducedMotion.matches;
      if (!animal.arrived || pulsing || animal.pulseActive) {
        const growth = reducedMotion.matches ? 1 : 1 - Math.pow(1 - arrival, 3);
        const pulse = pulsing ? 1 + 0.1 * Math.sin(pulseTime * Math.PI) : 1;
        animal.root.scaling.setAll(
          animal.baseScale * Math.max(0.001, growth) * pulse,
        );
        animal.label.style.opacity = Math.min(1, arrival * 1.5);
        if (animal.status !== 2)
          animal.root.position.y =
            0.15 + (animal.baseY - 0.15) * growth * pulse;
        animal.arrived = arrival === 1;
        animal.pulseActive = pulsing;
        if (animal.arrived) animal.label.style.removeProperty("opacity");
      }
      wander(animal, dt, now);
      const point = new B.Vector3(
        animal.root.position.x,
        animal.status === 2 ? 1.4 : heights[animal.species] + 0.7,
        animal.root.position.z,
      );
      const screen = B.Vector3.Project(
        point,
        B.Matrix.Identity(),
        scene.getTransformMatrix(),
        camera.viewport.toGlobal(
          engine.getRenderWidth(),
          engine.getRenderHeight(),
        ),
      );
      const anchorX = (screen.x / engine.getRenderWidth()) * viewWidth;
      const anchorY = (screen.y / engine.getRenderHeight()) * viewHeight;
      const effectsVisible =
        animal.label.classList.contains("low") ||
        animal.label.classList.contains("fed");
      if (effectsVisible !== animal.effectsVisible) {
        animal.effects.style.display = effectsVisible ? "block" : "none";
        animal.effectsVisible = effectsVisible;
      }
      if (effectsVisible) {
        // Feedback follows the model's head, never the collision-avoiding health label.
        const modelPoint = B.Vector3.Project(
          new B.Vector3(
            animal.root.position.x,
            heights[animal.species] + 0.15,
            animal.root.position.z,
          ),
          B.Matrix.Identity(),
          scene.getTransformMatrix(),
          camera.viewport.toGlobal(
            engine.getRenderWidth(),
            engine.getRenderHeight(),
          ),
        );
        animal.effectX = Math.max(
          36,
          Math.min(
            viewWidth - 36,
            (modelPoint.x / engine.getRenderWidth()) * viewWidth,
          ),
        );
        animal.effectY = Math.max(
          64,
          Math.min(
            viewHeight - 44,
            (modelPoint.y / engine.getRenderHeight()) * viewHeight,
          ),
        );
        animal.effects.style.transform = `translate3d(${animal.effectX}px,${animal.effectY}px,0)`;
      }
      const width = animal.labelWidth;
      const height = animal.labelHeight;
      // Leave a margin for the danger/feeding pulse without moving the label anchor.
      const minX = width / 2 + 8,
        maxX = viewWidth - minX;
      const minY = height + 44,
        maxY = viewHeight - 8;
      const visible = maxX >= minX && maxY >= minY;
      animal.label.style.visibility = visible ? "visible" : "hidden";
      animal.effects.style.visibility = visible ? "visible" : "hidden";
      animal.tail.style.visibility = animal.dot.style.visibility = visible
        ? "visible"
        : "hidden";
      if (!visible) return;
      const clampX = (value) => Math.max(minX, Math.min(maxX, value));
      const clampY = (value) => Math.max(minY, Math.min(maxY, value));
      const overlaps = (box) =>
        labelBoxes.some(
          (other) =>
            box.left < other.right + 6 &&
            box.right > other.left - 6 &&
            box.top < other.bottom + 6 &&
            box.bottom > other.top - 6,
        );
      const makeBox = (x, y) => ({
        left: x - width / 2,
        right: x + width / 2,
        top: y - height,
        bottom: y,
      });
      // Retain a safe slot; only search when another label actually needs this space.
      let x = clampX(anchorX + (animal.labelOffsetX || 0));
      let y = clampY(anchorY - animal.labelLift);
      let box = makeBox(x, y);
      if (overlaps(box)) {
        search: for (const column of [0, -1, 1, -2, 2, -3, 3]) {
          for (const row of [0, -1, 1, -2, 2, -3, 3, -4, 4]) {
            const candidateX = clampX(anchorX + column * (width + 8));
            const candidateY = clampY(anchorY + row * (height + 8));
            const candidate = makeBox(candidateX, candidateY);
            if (!overlaps(candidate)) {
              x = candidateX;
              y = candidateY;
              box = candidate;
              break search;
            }
          }
        }
        animal.labelOffsetX = x - anchorX;
        animal.labelLift = anchorY - y;
      }
      // Keep the selected label under the pointer while its animal continues moving.
      if (!resized && highlightedId === animal.id && animal.labelX !== null) {
        animal.labelX = clampX(animal.labelX);
        animal.labelY = clampY(animal.labelY);
        x = animal.labelX;
        y = animal.labelY;
        box = makeBox(x, y);
      }
      labelBoxes.push(box);
      // Viewport changes must snap; ordinary following alone uses the gentle ease.
      if (resized || animal.labelX === null) {
        animal.labelX = x;
        animal.labelY = y;
      } else {
        const follow = 1 - Math.exp(-2.2 * dt);
        const dx = (x - animal.labelX) * follow,
          dy = (y - animal.labelY) * follow;
        const fraction = Math.min(
          1,
          (70 * dt) / Math.max(0.001, Math.hypot(dx, dy)),
        );
        animal.labelX = clampX(animal.labelX + dx * fraction);
        animal.labelY = clampY(animal.labelY + dy * fraction);
      }
      animal.label.style.transform = `translate3d(${animal.labelX}px, ${animal.labelY}px, 0) translate(-50%, -100%)`;
      animal.tail.setAttribute(
        "d",
        `M ${animal.labelX} ${animal.labelY} Q ${animal.labelX} ${anchorY} ${anchorX} ${anchorY}`,
      );
      animal.dot.setAttribute("cx", anchorX);
      animal.dot.setAttribute("cy", anchorY);
      animal.tail.style.opacity = animal.dot.style.opacity = Math.min(
        1,
        arrival * 1.5,
      );
    });
    world.updateClock?.(now);
    scene.render();
  });
  // Read-only diagnostics support geometry/behaviour verification without changing health.
  return world;
};
