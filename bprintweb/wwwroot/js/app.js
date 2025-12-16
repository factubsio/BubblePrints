// frontend/treeView.ts
function createBlueprintView(flatElements, game, guid, cb) {
  const rootContainer = document.createElement("div");
  const parentContainerStack = [rootContainer];
  const localStorageKey = `bp-state-${guid}`;
  const savedState = JSON.parse(localStorage.getItem(localStorageKey) || "{}");
  const pathStack = [];
  for (let eIndex = 0; eIndex < flatElements.length; eIndex++) {
    const element = flatElements[eIndex];
    const nextElement = flatElements[eIndex + 1];
    if (element.levelDelta < 0) {
      parentContainerStack.pop();
      pathStack.pop();
    }
    if (element.key === null) {
      continue;
    }
    const currentPath = [...pathStack, element.key].join("/");
    const nodeWrapper = document.createElement("div");
    nodeWrapper.className = "bp-node";
    const row = document.createElement("div");
    row.className = "bp-row";
    const childrenContainer = document.createElement("div");
    childrenContainer.className = "bp-children-container";
    const hasChildren = element.levelDelta > 0;
    let toggle;
    if (hasChildren) {
      const childrenPresent = nextElement && nextElement.levelDelta != -1;
      toggle = document.createElement("span");
      toggle.className = "bp-toggle";
      if (!childrenPresent) {
        toggle.classList.add("disabled");
      }
      const isInitiallyCollapsed = savedState[currentPath] ?? false;
      if (isInitiallyCollapsed) {
        childrenContainer.classList.add("hide");
        toggle.textContent = "\u25BA";
      } else {
        toggle.textContent = "\u25BC";
      }
      if (childrenPresent) {
        toggle.onclick = () => {
          const isHidden = childrenContainer.classList.toggle("hide");
          toggle.textContent = isHidden ? "\u25BA" : "\u25BC";
          if (isHidden) {
            savedState[currentPath] = true;
          } else {
            delete savedState[currentPath];
          }
          if (Object.keys(savedState).length === 0) {
            localStorage.removeItem(localStorageKey);
          } else {
            localStorage.setItem(localStorageKey, JSON.stringify(savedState));
          }
        };
      }
      row.appendChild(toggle);
    } else {
      const placeholder = document.createElement("span");
      placeholder.className = "bp-toggle-placeholder";
      row.appendChild(placeholder);
    }
    const keySpan = document.createElement("span");
    keySpan.className = "bp-key";
    keySpan.textContent = element.key;
    row.appendChild(keySpan);
    if (element.value) {
      let valEl;
      if (element.link) {
        const linkEl = document.createElement("a");
        linkEl.className = "bp-link";
        linkEl.href = `/${game}/${element.link}`;
        linkEl.textContent = element.value;
        linkEl.onclick = (evt) => cb.handleLinkClick(evt, game, element.link);
        valEl = linkEl;
      } else {
        const valSpan = document.createElement("span");
        valSpan.className = "bp-val";
        valSpan.textContent = element.value;
        valEl = valSpan;
      }
      row.appendChild(valEl);
    }
    if (element.isObj && element.typeName) {
      const typeSpan = document.createElement("span");
      typeSpan.className = "bp-type";
      typeSpan.textContent = ` [${element.typeName}]`;
      row.appendChild(typeSpan);
    }
    nodeWrapper.appendChild(row);
    nodeWrapper.appendChild(childrenContainer);
    const currentParentContainer = parentContainerStack[parentContainerStack.length - 1];
    currentParentContainer.appendChild(nodeWrapper);
    if (element.levelDelta > 0) {
      parentContainerStack.push(childrenContainer);
      pathStack.push(element.key);
    }
  }
  return rootContainer;
}

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
var viewCallbacks = {
  handleLinkClick
};
async function loadAndShowBlueprint(game, guid) {
  searchView.classList.add("hide");
  blueprintView.classList.remove("hide");
  const container = document.getElementById("bp-content");
  container.innerHTML = "Loading...";
  try {
    const response = await fetch(`/bp/view/${game}/${guid}`);
    const elements = await response.json();
    titleSpan.innerText = elements[0].key || "Blueprint";
    container.innerHTML = "";
    const blueprintDom = createBlueprintView(elements, game, guid, viewCallbacks);
    container.appendChild(blueprintDom);
  } catch (err) {
    container.textContent = "Error: " + (err instanceof Error ? err.message : String(err));
  }
}
export {
  initApp
};
