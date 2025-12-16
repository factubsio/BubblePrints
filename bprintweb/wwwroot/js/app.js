// frontend/app.ts
var searchBar = document.getElementById("searchBox");
var resultsTable = document.getElementById("resultsTable");
var searchView = document.getElementById("searchView");
var blueprintView = document.getElementById("blueprintView");
var titleSpan = document.getElementById("currentTitle");
var inFlight = null;
var pendingQuery = null;
document.getElementById("backBtn")?.addEventListener("click", () => {
  history.pushState(null, "", "/");
  showSearch();
});
function applyTheme(theme) {
  document.documentElement.setAttribute("data-theme", theme);
  localStorage.setItem("theme", theme);
}
var savedTheme = localStorage.getItem("theme");
if (savedTheme) {
  applyTheme(savedTheme);
} else {
  const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  applyTheme(prefersDark ? "dark" : "light");
}
var themeToggle = document.getElementById("theme-toggle");
themeToggle.addEventListener("click", () => {
  const currentTheme = document.documentElement.getAttribute("data-theme");
  const newTheme = currentTheme === "dark" ? "light" : "dark";
  applyTheme(newTheme);
});
function initApp() {
  window.addEventListener("popstate", handleRouting);
  searchBar.addEventListener("input", () => {
    const query = searchBar.value.trim();
    if (!query) return;
    if (inFlight) {
      pendingQuery = query;
      return;
    }
    runQuery(query);
  });
  searchBar.addEventListener("focus", () => {
    searchView.classList.remove("hide");
    blueprintView.classList.add("hide");
  });
  handleRouting();
}
function handleRouting() {
  const path = window.location.pathname;
  const parts = path.split("/").filter((p) => p);
  if (parts.length === 2) {
    loadAndShowBlueprint(parts[0], parts[1]);
  } else {
    showSearch();
  }
}
function showSearch() {
  blueprintView.classList.add("hide");
  searchView.classList.remove("hide");
}
function navigateTo(game, guid) {
  const url = `/${game}/${guid}`;
  history.pushState(null, "", url);
  loadAndShowBlueprint(game, guid);
}
function runQuery(query) {
  inFlight = findBlueprints("rt", query).then(renderResults).finally(() => {
    inFlight = null;
    if (pendingQuery) {
      const next = pendingQuery;
      pendingQuery = null;
      runQuery(next);
    }
  });
}
async function findBlueprints(game, query) {
  const response = await fetch(`/bp/find/${game}?query=${encodeURIComponent(query)}`);
  return await response.json();
}
function renderResults(data) {
  if (!resultsTable) return;
  const tbody = resultsTable.querySelector("tbody");
  if (!tbody) return;
  tbody.innerHTML = "";
  for (const row of data) {
    const tr = document.createElement("tr");
    tr.innerHTML = `
            <td><a href="/rt/${row.guidText}">${row.name}</a></td>
            <td>${row.namespace}</td>
            <td>${row.guidText}</td>
            <td>${row.typeForResults}</td>
        `;
    const link = tr.firstElementChild;
    link.addEventListener("click", async (e) => {
      handleLinkClick(e, "rt", row.guidText);
    });
    tbody.appendChild(tr);
  }
}
function handleLinkClick(e, game, link) {
  if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
    e.preventDefault();
    navigateTo(game, link);
  }
}
async function loadAndShowBlueprint(game, guid) {
  searchView.classList.add("hide");
  blueprintView.classList.remove("hide");
  const container = document.getElementById("bp-content");
  container.innerHTML = "Loading...";
  try {
    const response = await fetch(`/bp/view/${game}/${guid}`);
    const elements = await response.json();
    titleSpan.innerText = elements[0].key;
    container.innerHTML = "";
    let level = 0;
    for (const e of elements) {
      if (e.levelDelta < 0) level += e.levelDelta;
      if (e.key == null) continue;
      const row = document.createElement("div");
      row.className = "bp-row";
      row.style.paddingLeft = level * 20 + "px";
      const keySpan = document.createElement("span");
      keySpan.className = "bp-key";
      keySpan.textContent = e.key;
      row.appendChild(keySpan);
      if (e.value) {
        let valEl;
        if (e.link) {
          const valSpan = document.createElement("a");
          valSpan.className = "bp-link";
          valSpan.href = `/${game}/${e.link}`;
          valSpan.textContent = e.value;
          valSpan.onclick = (evt) => handleLinkClick(evt, game, e.link);
          valEl = valSpan;
        } else {
          const valSpan = document.createElement("span");
          valSpan.className = "bp-val";
          valSpan.textContent = e.value;
          valEl = valSpan;
        }
        row.appendChild(valEl);
      }
      if (e.isObj && e.typeName) {
        const typeSpan = document.createElement("span");
        typeSpan.style.color = "gray";
        typeSpan.textContent = ` [${e.typeName}]`;
        row.appendChild(typeSpan);
      }
      container.appendChild(row);
      if (e.levelDelta > 0) level += e.levelDelta;
    }
  } catch (err) {
    container.textContent = "Error: " + err;
  }
}
export {
  initApp
};
