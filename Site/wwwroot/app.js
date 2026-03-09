const sectorsEl = document.getElementById("sectors");
const summaryEl = document.getElementById("summary");
const lastUpdateEl = document.getElementById("lastUpdate");
const refreshBtn = document.getElementById("refreshBtn");
const sectorTemplate = document.getElementById("sectorTemplate");

const REFRESH_MS = 3000;

async function fetchState() {
  const response = await fetch("/api/parking/state", { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`Failed to load parking state (${response.status})`);
  }

  return response.json();
}

function renderSummary(data) {
  const totals = data.sectors.reduce((acc, sector) => {
    acc.capacity += sector.capacity;
    acc.occupied += sector.occupiedCount;
    acc.available += sector.availableCount;
    acc.pending += sector.entryPendingCount;
    return acc;
  }, { capacity: 0, occupied: 0, available: 0, pending: 0 });

  const occupancy = totals.capacity === 0 ? 0 : Math.round((totals.occupied / totals.capacity) * 100);
  const cards = [
    ["Sections", data.sectors.length],
    ["Spots", totals.capacity],
    ["Occupied", totals.occupied],
    ["Available", totals.available],
    ["Pending Entries", totals.pending],
    ["Occupancy", `${occupancy}%`]
  ];

  summaryEl.innerHTML = cards.map(([label, value]) => (
    `<article class="metric"><div class="label">${label}</div><div class="value">${value}</div></article>`
  )).join("");
}

function renderSpot(spot) {
  const item = document.createElement("div");
  item.className = `spot ${spot.isOccupied ? "spot-occupied" : "spot-available"}`;
  const plate = spot.licensePlate ? `<span class="spot-plate">${spot.licensePlate}</span>` : "";

  item.innerHTML = `
    <span class="spot-id">#${spot.id}</span>
    <span class="spot-state" aria-hidden="true"></span>
    ${plate}
  `;

  item.title = spot.isOccupied
    ? `Spot ${spot.id}: occupied by ${spot.licensePlate || "vehicle"}`
    : `Spot ${spot.id}: available`;

  return item;
}

function renderSector(sector) {
  const node = sectorTemplate.content.firstElementChild.cloneNode(true);

  node.querySelector(".sector-title").textContent = `Section ${sector.sector}`;
  node.querySelector(".sector-meta").textContent = `Base price R$ ${Number(sector.basePrice).toFixed(2)} | Capacity ${sector.capacity}`;

  const badgesEl = node.querySelector(".badges");
  badgesEl.innerHTML = [
    `<span class="badge badge-busy">Occupied ${sector.occupiedCount}</span>`,
    `<span class="badge badge-ok">Available ${sector.availableCount}</span>`,
    `<span class="badge badge-pending">Pending ${sector.entryPendingCount}</span>`
  ].join("");

  const spotsGrid = node.querySelector(".spots-grid");
  sector.spots.forEach((spot) => {
    spotsGrid.appendChild(renderSpot(spot));
  });

  return node;
}

function renderState(data) {
  renderSummary(data);

  sectorsEl.innerHTML = "";
  data.sectors.forEach((sector) => {
    sectorsEl.appendChild(renderSector(sector));
  });

  const when = new Date(data.timestamp);
  lastUpdateEl.textContent = `updated ${when.toLocaleTimeString()}`;
}

function renderError(message) {
  sectorsEl.innerHTML = `<p class="error">${message}</p>`;
}

let loading = false;
async function loadAndRender() {
  if (loading) {
    return;
  }

  loading = true;
  try {
    const data = await fetchState();
    renderState(data);
  } catch (error) {
    renderError(error.message || "Unexpected error while loading parking state.");
  } finally {
    loading = false;
  }
}

refreshBtn.addEventListener("click", loadAndRender);
loadAndRender();
setInterval(loadAndRender, REFRESH_MS);
