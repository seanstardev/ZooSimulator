"use strict";
(async function () {
  let host;
  const loading = document.getElementById("loading");
  const canvas = document.getElementById("scene");
  let ready = false,
    zooRevision = -1,
    timeRevision = -1,
    drag = null,
    hovered = null,
    dragPoint = null,
    dragFrame = 0,
    dragPickPending = false,
    toastTimer;
  function send(actor, event, extra = {}) {
    host?.postMessage({ version: 1, actor, event, ...extra });
  }
  function toast(message) {
    const element = document.getElementById("toast");
    element.textContent = message;
    element.classList.add("visible");
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => element.classList.remove("visible"), 2300);
  }
  function error(message) {
    loading.style.display = "flex";
    loading.querySelector("h2").textContent = "The zoo needs a little help";
    loading.querySelector("p").textContent = message;
    send("main", "error", { message });
  }
  window.addEventListener("error", (event) => error(event.message));
  window.addEventListener("unhandledrejection", (event) =>
    error(String(event.reason?.message || event.reason)),
  );
  document
    .querySelectorAll("button")
    .forEach((button) => (button.disabled = true));
  try {
    const [connection, world, clockAnimal] = await Promise.all([
      window.zooHostReady,
      window.createZooScene(canvas, document.getElementById("labels")),
      window.createClockMascot(document.getElementById("mascot-scene")),
    ]);
    host = connection;
    world.clockAnimal = clockAnimal;
    window.zooDiagnostics = world;
    let clockSample = null,
      lastHour = null,
      lastClockText = "";
    const readout = document.getElementById("time");
    const hourHand = document.getElementById("clock-hand");
    const minuteHand = document.getElementById("minute-hand");
    const mascot = document.getElementById("clock-mascot");
    world.updateClock = (now) => {
      if (!clockSample) return;
      const duration = clockSample.hourDurationMilliseconds;
      const elapsed =
        clockSample.millisecondsIntoHour +
        Math.max(0, now - clockSample.receivedAt) +
        clockSample.deliveryDelay;
      // Never display an uncommitted next hour; wait for the authoritative transition.
      const fraction = Math.max(0, Math.min(3599 / 3600, elapsed / duration));
      const seconds = Math.floor(fraction * 3600);
      const text = [
        clockSample.hoursElapsed,
        Math.floor(seconds / 60),
        seconds % 60,
      ]
        .map((n) => String(n).padStart(2, "0"))
        .join(":");
      if (text !== lastClockText) {
        readout.textContent = text;
        lastClockText = text;
      }
      hourHand.style.transform = `rotate(${((clockSample.hoursElapsed % 12) + fraction) * 30}deg)`;
      minuteHand.style.transform = `rotate(${fraction * 360}deg)`;
    };
    mascot.addEventListener("animationend", (event) => {
      if (event.target.classList.contains("clock-cheer"))
        mascot.classList.remove("excited");
    });
    const actors = {
      zoo: {
        show(data) {
          if (data.revision < zooRevision) return;
          zooRevision = data.revision;
          world.show(data);
          const alive = data.animals.filter(
            (animal) => animal.status !== 2,
          ).length;
          send("main", "status", {
            message: `${alive} animals alive · Hour ${data.hoursElapsed} · Drop snacks to feed one animal; Feed whole zoo follows the brief.`,
          });
        },
      },
      clock: {
        show(data) {
          if (data.revision < timeRevision) return;
          timeRevision = data.revision;
          const duration = data.hourDurationMilliseconds;
          if (!(duration > 0) || !Number.isFinite(data.millisecondsIntoHour))
            return;
          clockSample = {
            ...data,
            receivedAt: performance.now(),
            deliveryDelay: Math.max(
              0,
              Date.now() - data.observedAtUnixMilliseconds,
            ),
          };
          readout.dataset.hours = data.hoursElapsed;
          if (lastHour !== null && data.hoursElapsed > lastHour) {
            clockAnimal.celebrate();
            mascot.classList.remove("excited");
            // Restart the one-shot celebration without measuring/reflowing layout.
            requestAnimationFrame(() => mascot.classList.add("excited"));
          }
          lastHour = data.hoursElapsed;
          world.updateClock(performance.now());
        },
      },
    };
    function receive(message) {
      if (message?.version !== 1) return;
      const actor = actors[message.actor];
      if (actor && typeof actor[message.operation] === "function")
        actor[message.operation](message.data);
    }
    host?.addEventListener("message", (event) => receive(event.data));
    // Browser-only diagnostics can deliver the same protocol via a local test harness.
    window.addEventListener("message", (event) => {
      if (event.source === window) receive(event.data);
    });
    document.getElementById("feed-all").addEventListener("click", () => {
      send("feeding", "feedZoo");
      world.animals.forEach((animal) => world.react(animal.id));
      toast("A snack for every living animal.");
    });
    document.getElementById("reset-game").addEventListener("click", () => {
      clearDrag();
      send("main", "reset");
      toast("A fresh start for fifteen friends.");
    });
    const ghost = document.getElementById("drag-ghost");
    document.querySelectorAll("[data-food]").forEach((button) =>
      button.addEventListener("pointerdown", (event) => {
        if (!ready) return;
        event.preventDefault();
        world.hoverAt(null, null);
        drag = event.pointerId;
        if (event.isTrusted) button.setPointerCapture(event.pointerId);
        ghost.textContent = button.querySelector("span").textContent;
        ghost.style.transform = `translate3d(${event.clientX}px,${event.clientY}px,0) translate(-50%,-70%)`;
        ghost.style.display = "block";
        move(event);
      }),
    );
    function move(event) {
      if (drag === null) {
        if (event.target.closest?.("#world"))
          world.hoverAt(event.clientX, event.clientY);
        else world.hoverAt(null, null);
        return;
      }
      if (drag !== event.pointerId) return;
      // Mouse polling can exceed display refresh: retain only its newest position.
      dragPoint = { x: event.clientX, y: event.clientY };
      dragPickPending = true;
      if (!dragFrame) dragFrame = requestAnimationFrame(updateDrag);
    }
    function setHovered(animal) {
      const next = animal?.status === 2 ? null : animal;
      if (next === hovered) return;
      hovered?.label.classList.remove("drag-over");
      hovered = next;
      world.highlight(hovered?.id ?? null);
      hovered?.label.classList.add("drag-over");
    }
    function updateDrag() {
      dragFrame = 0;
      if (drag === null || !dragPoint) return;
      const { x, y } = dragPoint;
      ghost.style.transform = `translate3d(${x}px,${y}px,0) translate(-50%,-70%)`;
    }
    // Read hit geometry in the scene's read phase, before its health-label writes.
    world.updateDrag = () => {
      if (drag === null || !dragPoint || !dragPickPending) return;
      dragPickPending = false;
      setHovered(world.pick(dragPoint.x, dragPoint.y));
    };
    function clearDrag() {
      cancelAnimationFrame(dragFrame);
      dragFrame = 0;
      dragPoint = null;
      dragPickPending = false;
      drag = null;
      setHovered(null);
      ghost.style.display = "none";
    }
    function finish(event, cancelled) {
      if (drag !== event.pointerId) return;
      const target = cancelled
        ? null
        : world.pick(event.clientX, event.clientY);
      clearDrag();
      if (!target) {
        if (!cancelled)
          toast("Drop your snack on an animal or its health bar.");
        return;
      }
      if (target.status === 2) {
        toast("This animal has died and cannot be revived.");
        return;
      }
      send("feeding", "feedAnimal", { animalId: target.id });
      world.react(target.id);
      toast("A little snack for friend " + target.id + ".");
    }
    document.addEventListener("pointermove", move);
    document.addEventListener("pointerleave", () => world.hoverAt(null, null));
    document.addEventListener("pointerup", (event) => finish(event, false));
    document.addEventListener("pointercancel", (event) => finish(event, true));
    ready = true;
    loading.style.display = "none";
    document
      .querySelectorAll("button")
      .forEach((button) => (button.disabled = false));
    send("main", "ready");
    send("main", "status", {
      message:
        "Animal models loaded · Rendering 15 independent animated residents locally.",
    });
  } catch (failure) {
    error(String(failure?.message || failure));
  }
})();
