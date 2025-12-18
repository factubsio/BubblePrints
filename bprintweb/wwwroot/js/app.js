// frontend/treeView.ts
function createBlueprintView(flatElements, apiPrefix2, guid, cb) {
  const rootContainer = document.createElement("div");
  rootContainer.className = "bp-root";
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
    const depth = pathStack.length;
    row.style.setProperty("--depth", depth.toString());
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
        toggle.classList.add("collapsed");
      } else {
        toggle.classList.remove("collapsed");
      }
      toggle.textContent = "\u25BC";
      if (childrenPresent) {
        toggle.onclick = () => {
          const isHidden = childrenContainer.classList.toggle("hide");
          if (isHidden) {
            toggle.classList.add("collapsed");
          } else {
            toggle.classList.remove("collapsed");
          }
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
        row.addEventListener("mousedown", (e) => {
          if (e.detail > 1) {
            e.preventDefault();
            toggle.click();
          }
        });
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
    if (element.string && element.string !== "") {
      const strEl = document.createElement("span");
      strEl.className = "bp-val";
      strEl.textContent = element.string;
      row.appendChild(strEl);
    }
    if (element.value) {
      let valEl;
      if (element.link) {
        const linkEl = document.createElement("a");
        linkEl.className = "bp-link";
        if (element.target === "") {
          linkEl.href = "#";
          linkEl.textContent = `${element.value} -> stale`;
          linkEl.className = "bp-link-dead";
        } else {
          linkEl.href = `${apiPrefix2}/${element.link}`;
          linkEl.textContent = `${element.value} -> ${element.target}`;
        }
        linkEl.onclick = (evt) => cb.handleLinkClick(evt, element.link);
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

// frontend/dialog.ts
var DialogPage = class {
  constructor() {
    this.speakerEl = document.getElementById("cue-speaker-name");
    this.textEl = document.getElementById("cue-text");
    this.proceedTitleEl = document.getElementById("answers-title");
    this.proceedEl = document.getElementById("cue-answers");
    this.commentEl = document.getElementById("dev-comment");
    this.idEl = document.getElementById("current-id");
    this.cueCache = /* @__PURE__ */ new Map();
    this.answerCache = /* @__PURE__ */ new Map();
    this.handleCue("4345a7ac05af431298cc6a3e5b3f8c6b");
  }
  async getCue(id) {
    let cue = this.cueCache.get(id);
    if (!cue) {
      const raw = await getBlueprint(id, true);
      const { root, strs } = raw;
      const text = getText(root.Text, strs) ?? "<unknown>";
      cue = {
        obj: {
          id,
          text,
          answers: [],
          continueCues: []
        },
        raw
      };
      this.cueCache.set(id, cue);
      for (const answerId of root.Answers) {
        await this.spreadAnswers(answerId, cue.obj.answers);
      }
      for (const cueId of root.Continue?.Cues) {
        const nextCue = await this.getCue(getGuid(cueId));
        cue.obj.continueCues.push(nextCue.obj);
      }
    }
    return cue;
  }
  /**
   * Fetches a Cue by its GUID and displays only the data
   * directly available within that Cue's JSON object.
   * @param cueId The GUID of the Cue to display.
   */
  async handleCue(cueId) {
    this.idEl.textContent = cueId;
    const cue = await this.getCue(cueId);
    if (!cue) return;
    this.speakerEl.textContent = cue.raw.root.Speaker.m_Blueprint || "Narrator";
    this.textEl.textContent = cue.obj.text;
    this.commentEl.textContent = cue.raw.root.Comment || "[No developer comment]";
    this.proceedEl.innerHTML = "";
    if (cue.obj.answers.length > 0) {
      this.proceedTitleEl.textContent = "Answers";
      for (const a of cue.obj.answers) {
        const answerItem = document.createElement("li");
        answerItem.textContent = a.text;
        answerItem.addEventListener("click", async (_) => {
          this.handleAnswer(a);
        });
        this.proceedEl.appendChild(answerItem);
      }
    } else if (cue.obj.continueCues.length > 0) {
      this.proceedTitleEl.textContent = "...";
      for (const nextCue of cue.obj.continueCues) {
        const cueItem = document.createElement("li");
        cueItem.textContent = nextCue.text;
        cueItem.addEventListener("click", async (_) => {
          this.handleCue(nextCue.id);
        });
        this.proceedEl.appendChild(cueItem);
      }
    } else {
      this.proceedTitleEl.textContent = "unknown!";
    }
  }
  async handleAnswer(answer) {
    this.idEl.textContent = answer.id;
    this.speakerEl.textContent = "YOU";
    this.textEl.textContent = answer.text;
    this.commentEl.textContent = "";
    this.proceedTitleEl.textContent = "Next Cues";
    this.proceedEl.textContent = "";
    for (const cueId of answer.nextCues) {
      const cue = await this.getCue(cueId);
      answer.cues.push(cue.obj);
    }
    for (const cue of answer.cues) {
      const cueItem = document.createElement("li");
      cueItem.textContent = cue.text;
      cueItem.addEventListener("click", async (_) => {
        this.handleCue(cue.id);
      });
      this.proceedEl.appendChild(cueItem);
    }
  }
  async spreadAnswers(answerId, answers) {
    const guid = getGuid(answerId);
    let answer = this.answerCache.get(guid);
    if (answer) {
      answers.push(answer.obj);
      return;
    }
    const raw = await getBlueprint(guid, true);
    const { root, strs } = raw;
    if (root.$type.endsWith("BlueprintAnswersList")) {
      for (const answerId2 of root.Answers) {
        await this.spreadAnswers(answerId2, answers);
      }
    } else {
      const nextCues = [];
      for (const cueId of root.NextCue.Cues) {
        nextCues.push(getGuid(cueId));
      }
      answer = {
        obj: {
          id: guid,
          text: getText(root.Text, strs) ?? "<unknown>",
          nextCues,
          cues: []
        },
        raw
      };
      this.answerCache.set(guid, answer);
      answers.push(answer.obj);
    }
  }
};
function getGuid(raw) {
  return raw.replace("!bp_", "");
}
function getText(textNode, strings) {
  if (textNode.m_Key && textNode.m_Key !== "") {
    return strings[textNode.m_Key];
  } else if (textNode.Shared) {
    return strings[textNode.Shared.stringkey];
  }
  return void 0;
}

// frontend/app.ts
var classToggleInactive = "toggle-inactive";
var classToggleActive = "toggle-active";
var apiPrefix = "";
function getGameIdentifier(url, baseDomain) {
  const host = url.hostname;
  const subdomainSuffix = `.${baseDomain}`;
  if (host.toLowerCase().endsWith(subdomainSuffix.toLowerCase())) {
    const gamePart = host.substring(0, host.length - subdomainSuffix.length);
    if (gamePart && !gamePart.includes(".")) {
      apiPrefix = "";
      return gamePart;
    }
  }
  const segments = url.pathname.split("/").filter(Boolean);
  if (segments.length > 0) {
    apiPrefix = "/" + segments[0];
    return segments[0];
  }
  return null;
}
var game2 = getGameIdentifier(window.location, "bubbleprints.dev");
var knownGames = /* @__PURE__ */ new Map([
  ["km", {
    name: "King Maker"
  }],
  ["wrath", {
    name: "Wrath of the Righetous"
  }],
  ["rt", {
    name: "Rogue Trader"
  }],
  ["dh", {
    name: "Dark Heresy"
  }]
]);
function makeGameLinks() {
  let response = "";
  for (const [link, logo] of knownGames) {
    response += `<a href="https://${link}.bubbleprints.dev"> <h1 style="white-space: nowrap;">${logo.name}</h1></a>`;
  }
  return response;
}
if (game2 == null) {
  document.body.style.margin = "0";
  document.body.innerHTML = `<div style="display: flex; flex-direction: column; justify-content: center; align-items: center; height: 100vh;"> ${makeGameLinks()}</div>`;
} else if (!knownGames.has(game2)) {
  document.body.style.margin = "0";
  document.body.innerHTML = `<div style="display: flex; justify-content: center; align-items: center; height: 100vh;"> <h1 style="font-size: 8vw; white-space: nowrap;">Unknown game: ${game2}</h1></div>`;
}
function createToggle(id, onChange) {
  const el = document.getElementById(id);
  if (!el) return;
  const isA = el.tagName === "A";
  let state = false;
  const setState = (newState) => {
    state = newState;
    if (state)
      el.classList.replace(classToggleInactive, classToggleActive);
    else
      el.classList.replace(classToggleActive, classToggleInactive);
  };
  el.addEventListener("click", (e) => {
    if (isA) {
      if (e.button != 0 || e.ctrlKey || e.shiftKey) {
        return;
      }
      e.preventDefault();
    }
    setState(!state);
    onChange(state);
  });
  return setState;
}
function setVisible(el, show) {
  if (show) el.classList.remove("hide");
  else el.classList.add("hide");
}
var MainPage = class {
  constructor() {
    this.searchBar = document.getElementById("searchBox");
    this.resultsTable = document.getElementById("resultsTable");
    this.searchView = document.getElementById("searchView");
    this.blueprintView = document.getElementById("blueprintView");
    this.titleSpan = document.getElementById("currentTitle");
    this.titleBar = document.getElementById("title-bar");
    this.backBtn = document.getElementById("backBtn");
    this.showRaw = document.getElementById("show-raw-link");
    this.copyRaw = document.getElementById("copy-raw");
    this.referencePanel = document.getElementById("bp-references");
    this.showReferences = createToggle("toggle-references", (on) => {
      setVisible(this.referencePanel, on);
    });
    this.inFlight = null;
    this.pendingQuery = null;
    this.resultCursor = 0;
    this.activeRow = null;
    this.isShowingRaw = false;
    this.viewCallbacks = {
      handleLinkClick: (e, link) => this.handleLinkClick(e, link)
    };
    this.backBtn.addEventListener("click", () => {
      history.pushState(null, "", "/");
      this.showSearch();
    });
    this.searchBar.addEventListener("keydown", (e) => {
      if (e.key == "ArrowDown") {
        this.moveResultCursor(1);
        e.preventDefault();
      }
      if (e.key == "ArrowUp") {
        this.moveResultCursor(-1);
        e.preventDefault();
      }
      if (e.key == "Enter") {
        const tbody = this.resultsTable.querySelector("tbody");
        if (!tbody) return;
        if (this.resultCursor < 0 || this.resultCursor > tbody.rows.length) return;
        const guid = tbody.rows[this.resultCursor].cells[3].textContent;
        e.preventDefault();
        this.searchBar.blur();
        if (guid && guid !== "") {
          this.navigateTo(guid);
        }
      }
    });
    window.addEventListener("keydown", (e) => {
      if (e.ctrlKey && e.key == "p") {
        e.preventDefault();
        this.searchBar.focus();
      }
    });
    this.showRaw.addEventListener("click", (e) => {
      if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
        const { game: game3, guid } = this.parseRawLink();
        const showRaw = !this.isShowingRaw;
        this.navigateTo(guid, { raw: showRaw });
        e.preventDefault();
      }
    });
    this.copyRaw.addEventListener("click", async (_) => {
      const { game: game3, guid } = this.parseRawLink();
      const { name, bp } = await getRawBlueprint(guid);
      const copyable = new ClipboardItem({
        "text/plain": bp.text()
      });
      await navigator.clipboard.write([copyable]);
    });
    this.initApp();
  }
  parseRawLink() {
    const href = this.showRaw.href.split("/");
    const game3 = href[href.length - 2];
    const guid = href[href.length - 1];
    return { game: game3, guid };
  }
  initApp() {
    window.addEventListener("popstate", () => this.handleRouting());
    this.searchBar.addEventListener("input", () => {
      const query = this.searchBar.value.trim();
      if (!query) return;
      if (this.inFlight) {
        this.pendingQuery = query;
        return;
      }
      this.runQuery(query);
    });
    this.searchBar.addEventListener("focus", () => {
      this.showSearch();
    });
    this.handleRouting();
  }
  handleRouting() {
    const path = window.location.pathname;
    const parts = path.split("/").filter((p) => p);
    const apiParts = apiPrefix.length == 0 ? 0 : 1;
    if (parts.length === apiParts) {
      this.showSearch();
    } else {
      const query = new URLSearchParams(window.location.search);
      this.loadAndShowBlueprint(parts[apiParts], {
        raw: query.get("raw") === "true"
      });
    }
  }
  showSearch() {
    this.moveResultCursor(0);
    this.blueprintView.classList.add("hide");
    this.titleBar.classList.add("hide");
    this.searchView.classList.remove("hide");
  }
  // Helper to change URL without reloading
  navigateTo(guid, opts) {
    let url = `${apiPrefix}/${guid}`;
    if (opts?.raw === true) {
      url += "?raw=true";
    }
    history.pushState(null, "", url);
    this.loadAndShowBlueprint(guid, opts);
  }
  runQuery(query) {
    this.inFlight = this.findBlueprints(query).then((data) => this.renderResults(data)).finally(() => {
      this.inFlight = null;
      if (this.pendingQuery) {
        const next = this.pendingQuery;
        this.pendingQuery = null;
        this.runQuery(next);
      }
    });
  }
  async findBlueprints(query) {
    const response = await fetch(`${apiPrefix}/bp/find?query=${encodeURIComponent(query)}`);
    return await response.json();
  }
  moveResultCursor(dir) {
    const newCursor = this.resultCursor + dir;
    const tbody = this.resultsTable.querySelector("tbody");
    if (!tbody) return;
    if (newCursor < 0 || newCursor >= tbody.rows.length) return;
    this.activeRow?.classList.remove("cursor-active");
    this.activeRow = tbody.rows[newCursor];
    this.activeRow.classList.add("cursor-active");
    this.resultCursor = newCursor;
  }
  renderResults(data) {
    if (!this.resultsTable) return;
    const tbody = this.resultsTable.querySelector("tbody");
    if (!tbody) return;
    tbody.innerHTML = "";
    let index = 0;
    for (const row of data) {
      const tr = document.createElement("tr");
      const active = index == this.resultCursor;
      tr.innerHTML = `
            <td><a href="${apiPrefix}/${row.guidText}">${row.name}</a></td>
            <td class="col-priority-1">${row.typeName}</td>
            <td class="col-priority-2">${row.namespace}</td>
            <td class="col-priority-3">${row.guidText}</td>
        `;
      if (active) {
        this.activeRow = tr;
        tr.classList.add("cursor-active");
      }
      const link = tr.firstElementChild;
      link.addEventListener("click", async (e) => {
        this.handleLinkClick(e, row.guidText);
      });
      tbody.appendChild(tr);
      index++;
    }
  }
  handleLinkClick(e, link) {
    if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
      e.preventDefault();
      this.navigateTo(link);
    }
  }
  async loadAndShowBlueprint(guid, opts) {
    this.showRaw.href = `${apiPrefix}/bp/get/${guid}`;
    this.resultCursor = 0;
    this.activeRow?.classList?.remove("cursor-active");
    this.activeRow = null;
    this.searchView.classList.add("hide");
    this.titleBar.classList.remove("hide");
    this.blueprintView.classList.remove("hide");
    const container = document.getElementById("bp-content");
    container.innerHTML = "Loading...";
    const resultCount = this.resultsTable.querySelector("tbody")?.rows.length;
    if (resultCount && resultCount > 0) {
      this.backBtn.disabled = false;
    } else {
      console.log("disabling");
      this.backBtn.disabled = true;
    }
    try {
      if (opts?.raw === true) {
        this.isShowingRaw = true;
        this.showRaw.classList.replace("toggle-inactive", "toggle-active");
        const { name, bp } = await getRawBlueprint(guid);
        this.titleSpan.innerText = name || "Blueprint";
        container.innerHTML = `<div class="json-view">${JSON.stringify(await bp.json(), null, 2)}</div>`;
      } else {
        this.isShowingRaw = false;
        this.showRaw.classList.replace("toggle-active", "toggle-inactive");
        const response = await fetch(`${apiPrefix}/bp/view/${guid}`);
        const obj = await response.json();
        const elements = obj.blueprint;
        this.titleSpan.innerText = elements[0].key || "Blueprint";
        container.innerHTML = "";
        const blueprintDom = createBlueprintView(elements, apiPrefix, guid, this.viewCallbacks);
        const refs = obj.references;
        this.referencePanel.innerHTML = "<h4>References</h4>";
        for (const ref of refs) {
          const a = document.createElement("a");
          a.className = "bp-link";
          a.href = `${apiPrefix}/${ref.id}`;
          a.addEventListener("click", (e) => this.handleLinkClick(e, ref.id));
          a.textContent = ref.name;
          this.referencePanel.appendChild(a);
        }
        container.appendChild(blueprintDom);
      }
    } catch (err) {
      container.textContent = "Error: " + (err instanceof Error ? err.message : String(err));
    }
  }
};
async function getRawBlueprint(guid) {
  const rawResponse = await fetch(`${apiPrefix}/bp/get/${guid}`);
  const name = rawResponse.headers.get("BP-Name");
  return { name, bp: rawResponse };
}
async function getBlueprint(guid, withStrings = false) {
  const stringQuery = `?strings=${withStrings}`;
  const response = await fetch(`${apiPrefix}/bp/get/${guid}${stringQuery}`);
  return await response.json();
}
export {
  DialogPage,
  MainPage,
  createToggle,
  game2 as game,
  getBlueprint,
  setVisible
};
