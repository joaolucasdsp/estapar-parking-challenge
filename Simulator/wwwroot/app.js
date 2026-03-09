const REFRESH_MS = 3000;

const outputEl = document.getElementById("output");
const topologyEl = document.getElementById("topology");
const summaryEl = document.getElementById("summary");
const statusLineEl = document.getElementById("statusLine");
const updateStampEl = document.getElementById("updateStamp");
const platePrefixEl = document.getElementById("platePrefix");
const topologySelectEl = document.getElementById("topologySelect");
const btnApplyTopologyEl = document.getElementById("btnApplyTopology");
const exitStayMinutesEl = document.getElementById("exitStayMinutes");
const quickStayOptionsEl = document.getElementById("quickStayOptions");
const revenueSectorEl = document.getElementById("revenueSector");
const revenueDateEl = document.getElementById("revenueDate");
const sectorTemplate = document.getElementById("sectorTemplate");

let loadingState = false;
let nextPlateCounter = 1;

function setStatus(message, isError = false) {
  statusLineEl.textContent = message;
  statusLineEl.classList.toggle("status-error", isError);
}

function selectQuickStayButton(minutes) {
  if (!quickStayOptionsEl) {
    return;
  }

  const buttons = quickStayOptionsEl.querySelectorAll(".quick-stay-btn");
  buttons.forEach((button) => {
    const buttonMinutes = Number(button.dataset.minutes);
    button.classList.toggle("is-active", buttonMinutes === minutes);
  });
}

function log(title, payload) {
  const stamp = new Date().toISOString();
  const line = `\n[${stamp}] ${title}\n${JSON.stringify(payload, null, 2)}\n`;
  outputEl.textContent = line + outputEl.textContent;
}

async function api(method, path, body) {
  const options = {
    method,
    headers: { "Content-Type": "application/json" }
  };

  if (body !== undefined) {
    options.body = JSON.stringify(body);
  }

  const response = await fetch(path, options);
  const data = await response.json();
  return { ok: response.ok, status: response.status, data };
}

function renderTopologies(payload) {
  if (!topologySelectEl || !payload || !Array.isArray(payload.availableTopologies)) {
    return;
  }

  topologySelectEl.innerHTML = "";
  payload.availableTopologies.forEach((topology) => {
    const option = document.createElement("option");
    option.value = topology.name;
    option.textContent = `${topology.name} (${topology.sectorCount} sectors / ${topology.spotCount} spots)`;
    option.selected = topology.name === payload.activeTopology;
    topologySelectEl.appendChild(option);
  });
}

async function loadTopologies() {
  const result = await api("GET", "/simulator/topologies");
  if (!result.ok) {
    throw new Error("Failed to load simulator topologies.");
  }

  renderTopologies(result.data);
}

async function applyTopologyAndSync() {
  const selected = topologySelectEl?.value?.trim();
  if (!selected) {
    setStatus("Select a topology before applying.", true);
    return;
  }

  setStatus(`Applying topology ${selected}...`);
  const selectResult = await api("POST", "/simulator/topologies/select", { name: selected });
  log("Topology selected", selectResult);
  if (!selectResult.ok) {
    setStatus(`Failed to select topology ${selected}.`, true);
    return;
  }

  const syncResult = await api("POST", "/target/parking/sync");
  log("Site topology sync", syncResult);
  if (!syncResult.ok || !syncResult.data || syncResult.data.statusCode < 200 || syncResult.data.statusCode >= 300) {
    setStatus("Topology selected, but sync with Site failed.", true);
    return;
  }

  await loadParkingState();
  setStatus(`Topology ${selected} applied and synced.`);
}

function buildNextPlate() {
  const prefix = (platePrefixEl.value || "SIM").trim().toUpperCase() || "SIM";
  const number = String(nextPlateCounter++).padStart(4, "0");
  return `${prefix}${number}`;
}

async function sendEvent(payload) {
  const result = await api("POST", "/simulate/event", payload);
  log(`Event ${payload.event_type}`, result);
  return result;
}

async function sendEntryAndPark(spot) {
  const plate = buildNextPlate();
  const entryTime = new Date().toISOString();
  const entry = await sendEvent({
    license_plate: plate,
    event_type: "ENTRY",
    entry_time: entryTime
  });

  const parked = await sendEvent({
    license_plate: plate,
    event_type: "PARKED",
    lat: spot.lat,
    lng: spot.lng
  });

  log("Simulated spot occupancy", {
    spotId: spot.id,
    sector: spot.sector,
    plate,
    entry,
    parked
  });
}

async function sendExit(spot) {
  let plate = spot.licensePlate;
  if (!plate) {
    plate = window.prompt(`Spot #${spot.id} is occupied. Enter the plate to emit EXIT:`) || "";
    plate = plate.trim().toUpperCase();
  }

  if (!plate) {
    setStatus("Exit event cancelled: missing plate.", true);
    return;
  }

  const stayMinutes = Number(exitStayMinutesEl.value);
  const hasValidStayMinutes = Number.isFinite(stayMinutes) && stayMinutes > 0;
  const entryAt = spot.entryTime ? new Date(spot.entryTime) : null;

  let exitTime = new Date().toISOString();
  if (hasValidStayMinutes && entryAt && !Number.isNaN(entryAt.getTime())) {
    exitTime = new Date(entryAt.getTime() + (stayMinutes * 60 * 1000)).toISOString();
  }

  const exit = await sendEvent({
    license_plate: plate,
    event_type: "EXIT",
    exit_time: exitTime
  });

  log("Simulated vehicle exit", {
    spotId: spot.id,
    sector: spot.sector,
    plate,
    selectedStayMinutes: hasValidStayMinutes ? stayMinutes : null,
    entryTimeUsed: spot.entryTime || null,
    exitTimeSent: exitTime,
    exit
  });
}

function spotClass(spot) {
  if (spot.isOccupied) {
    return "spot spot-occupied";
  }
  return "spot spot-available";
}

function renderSummary(data) {
  const totals = data.sectors.reduce((acc, sector) => {
    acc.capacity += sector.capacity;
    acc.occupied += sector.occupiedCount;
    acc.pending += sector.entryPendingCount;
    return acc;
  }, { capacity: 0, occupied: 0, pending: 0 });

  const available = Math.max(0, totals.capacity - totals.occupied);
  const occupancy = totals.capacity === 0 ? 0 : Math.round((totals.occupied / totals.capacity) * 100);

  summaryEl.innerHTML = [
    ["Sections", data.sectors.length],
    ["Total Spots", totals.capacity],
    ["Occupied", totals.occupied],
    ["Available", available],
    ["Pending", totals.pending],
    ["Occupancy", `${occupancy}%`]
  ].map(([label, value]) => `<div class="metric"><span>${label}</span><strong>${value}</strong></div>`).join("");
}

function renderState(data) {
  renderSummary(data);
  topologyEl.innerHTML = "";

  data.sectors.forEach((sector) => {
    const sectorNode = sectorTemplate.content.firstElementChild.cloneNode(true);
    sectorNode.querySelector(".sector-title").textContent = `Section ${sector.sector}`;
    sectorNode.querySelector(".sector-meta").textContent = `Price R$ ${Number(sector.basePrice).toFixed(2)} | Capacity ${sector.capacity}`;

    const badgesEl = sectorNode.querySelector(".sector-badges");
    badgesEl.innerHTML = [
      `<span class="badge badge-occupied">Occupied ${sector.occupiedCount}</span>`,
      `<span class="badge badge-available">Available ${sector.availableCount}</span>`,
      `<span class="badge badge-pending">Pending ${sector.entryPendingCount}</span>`
    ].join("");

    const spotsEl = sectorNode.querySelector(".spots");
    sector.spots.forEach((spot) => {
      const spotEl = document.createElement("button");
      spotEl.type = "button";
      spotEl.className = spotClass(spot);
      spotEl.innerHTML = `
        <span class="spot-id">#${spot.id}</span>
        <span class="spot-dot" aria-hidden="true"></span>
        <span class="spot-plate">${spot.licensePlate || "free"}</span>
      `;

      const hint = spot.isOccupied
        ? `Spot ${spot.id} occupied by ${spot.licensePlate || "vehicle"}. Click to emit EXIT.`
        : `Spot ${spot.id} available. Click to emit ENTRY + PARKED.`;
      spotEl.title = hint;

      spotEl.addEventListener("click", async () => {
        revenueSectorEl.value = sector.sector;
        setStatus(`Running simulation for spot #${spot.id}...`);
        try {
          if (spot.isOccupied) {
            await sendExit(spot);
          } else {
            await sendEntryAndPark(spot);
          }

          await loadParkingState();
          setStatus(`Spot #${spot.id} simulation completed.`);
        } catch (error) {
          log("Simulation failed", { message: error.message, spot });
          setStatus(`Failed to simulate spot #${spot.id}.`, true);
        }
      });

      spotsEl.appendChild(spotEl);
    });

    topologyEl.appendChild(sectorNode);
  });

  updateStampEl.textContent = `Updated at ${new Date(data.timestamp).toLocaleTimeString()}`;
}

async function loadParkingState() {
  if (loadingState) {
    return;
  }

  loadingState = true;
  try {
    const result = await api("GET", "/target/parking/state");
    if (!result.ok || !result.data || result.data.statusCode < 200 || result.data.statusCode >= 300) {
      throw new Error(`Target state unavailable (${result.status})`);
    }

    const parsed = JSON.parse(result.data.responseBody);
    renderState(parsed);
  } catch (error) {
    setStatus(error.message || "Unexpected error while loading topology.", true);
    log("Load topology failed", { message: error.message });
  } finally {
    loadingState = false;
  }
}

document.getElementById("btnRefresh").addEventListener("click", loadParkingState);
document.getElementById("btnHealth").addEventListener("click", async () => {
  const result = await api("GET", "/target/health");
  log("Target health", result);
});
document.getElementById("btnRevenue").addEventListener("click", async () => {
  const sector = (revenueSectorEl.value || "A").trim();
  const date = revenueDateEl.value || new Date().toISOString().slice(0, 10);
  const result = await api("GET", `/target/revenue?date=${encodeURIComponent(date)}&sector=${encodeURIComponent(sector)}`);
  log("Revenue query", { date, sector, result });
});
document.getElementById("btnClear").addEventListener("click", () => {
  outputEl.textContent = "";
});
if (btnApplyTopologyEl) {
  btnApplyTopologyEl.addEventListener("click", async () => {
    try {
      await applyTopologyAndSync();
    } catch (error) {
      log("Topology apply failed", { message: error.message });
      setStatus("Failed to apply topology.", true);
    }
  });
}

if (quickStayOptionsEl) {
  quickStayOptionsEl.addEventListener("click", (event) => {
    const button = event.target.closest(".quick-stay-btn");
    if (!button) {
      return;
    }

    const minutes = Number(button.dataset.minutes);
    if (!Number.isFinite(minutes) || minutes <= 0) {
      return;
    }

    exitStayMinutesEl.value = String(minutes);
    selectQuickStayButton(minutes);
  });
}

exitStayMinutesEl.addEventListener("input", () => {
  const value = Number(exitStayMinutesEl.value);
  if (Number.isFinite(value) && value > 0) {
    selectQuickStayButton(value);
    return;
  }

  selectQuickStayButton(-1);
});

revenueDateEl.value = new Date().toISOString().slice(0, 10);
setStatus("Ready. Click on spots to simulate events.");
selectQuickStayButton(Number(exitStayMinutesEl.value));
(async () => {
  try {
    await loadTopologies();
  } catch (error) {
    log("Load topologies failed", { message: error.message });
    setStatus("Failed to load topology profiles.", true);
  }

  await loadParkingState();
})();

setInterval(loadParkingState, REFRESH_MS);
