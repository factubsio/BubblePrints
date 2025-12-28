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
          linkEl.href = makeBlueprintLink(element.link);
          linkEl.textContent = `${element.value} -> ${element.target}`;
        }
        linkEl.onclick = (evt) => cb.handleLinkClick(evt, linkEl.href);
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
import dagre from "https://esm.sh/dagre@0.8.5";
var regex = /\^([^|]+)\|([^$]+)\$/g;
function getAncestor(node, level = 1) {
  for (let l = 0; l < level; l++) {
    if (node.dat.in.length !== 1) {
      return null;
    }
    node = node.dat.in[0];
  }
  return node;
}
var portNames = {
  success: "\u2714",
  fail: "\u2716"
};
function createPath(points) {
  if (points.length < 2) return "";
  let d = `M ${points[0].x},${points[0].y}`;
  d += ` L ${points[1].x},${points[1].y}`;
  if (points.length >= 3) {
    d = `M ${points[0].x},${points[0].y}`;
    d += ` L ${(points[0].x + points[1].x) / 2},${(points[0].y + points[1].y) / 2}`;
    for (let i = 1; i < points.length - 1; i++) {
      const p1 = points[i];
      const p2 = points[i + 1];
      const midX = (p1.x + p2.x) / 2;
      const midY = (p1.y + p2.y) / 2;
      d += ` Q ${p1.x},${p1.y} ${midX},${midY}`;
    }
    d += ` L ${points[points.length - 1].x},${points[points.length - 1].y}`;
  }
  return d;
}
function nodeToUse(e, isPanning) {
  if (isPanning) {
    return null;
  }
  const g = e.target.closest("use");
  return g?.getAttribute("data-target") ?? null;
}
function nodeToGuid(e, isPanning) {
  e.preventDefault();
  if (isPanning) {
    return;
  }
  const g = e.target.closest("g");
  return g?.getAttribute("data-graph-node");
}
function nodeToEdge(e, isPanning) {
  e.preventDefault();
  if (isPanning) {
    return null;
  }
  const g = e.target.closest("path");
  const idxStr = g?.getAttribute("data-edge-idx");
  if (idxStr !== void 0 && idxStr !== null)
    return parseInt(idxStr);
  else
    return null;
}
var DialogPage = class {
  constructor() {
    this.speakerEl = document.getElementById("cue-speaker-name");
    this.graphEl = document.getElementById("dialog-graph");
    this.commentEl = document.getElementById("dev-comment");
    this.propGrid = document.getElementById("props");
    this.dialog = null;
    this._selected = null;
    this._highlight = null;
    this.allEdges = [];
    this.btnGoToSelected = document.getElementById("do-goto-selected");
    this.root = null;
    this.panX = 0;
    this.panY = 0;
    this.scale = 1;
    window.addEventListener("popstate", async () => await this.handleRouting());
    this.handleRouting();
    this.btnGoToSelected.addEventListener("click", (_) => {
      this.centerOnNode(this._selected);
    });
  }
  centerOn(x, y) {
    const viewportRect = this.graphEl.getBoundingClientRect();
    const viewportCenterX = viewportRect.width / 2;
    const viewportCenterY = viewportRect.height / 2;
    this.panX = viewportCenterX - x * this.scale;
    this.panY = viewportCenterY - y * this.scale;
    this.root?.classList.add("animating");
    setTimeout(() => this.root?.classList.remove("animating"), 205);
    this.updateTransform(this.panX, this.panY, this.scale);
  }
  addProp(label, val) {
    const labelEl = document.createElement("span");
    labelEl.className = "prop-key";
    labelEl.textContent = label;
    const valEl = document.createElement("span");
    valEl.className = "prop-val";
    valEl.innerHTML = this.makeClickies(val);
    valEl.querySelectorAll("a.local-clicky").forEach((aEl) => {
      const a = aEl;
      const guid = a.getAttribute("data-guid") ?? "_";
      const target = this.dialog?.nodes[guid];
      if (target) {
        a.addEventListener("mouseenter", (_) => {
          this.highlight = target;
        });
        a.addEventListener("mouseleave", (_) => {
          this.highlight = null;
        });
        a.addEventListener("click", (e) => {
          e.preventDefault();
          this.centerOnNode(target);
          this.selected = target;
        });
      }
    });
    this.propGrid.append(labelEl, valEl);
  }
  get highlight() {
    return this._highlight;
  }
  set highlight(val) {
    if (this._highlight) {
      this._highlight.dat.g.classList.remove("highlight");
    }
    this._highlight = val;
    if (this._highlight) {
      this._highlight.dat.g.classList.add("highlight");
    }
  }
  get selected() {
    return this._selected;
  }
  set selected(val) {
    if (this._selected) {
      this._selected.dat.g.classList.remove("selected");
      this.propGrid.innerHTML = "";
    }
    for (const edge of this.allEdges) {
      edge.path.classList.remove("hi-in", "hi-out");
    }
    this._selected = val;
    if (val) {
      val.dat.g.classList.add("selected");
      for (const edgeIn of val.dat.edgesOut) {
        edgeIn.path.classList.add("hi-out");
      }
      for (const edgeIn of val.dat.edgesIn) {
        edgeIn.path.classList.add("hi-in");
      }
      this.addProp("id", val.dat.id);
      this.addProp("typ", val.typ);
      if (val.seq) {
        for (let i = 0; i < val.seq.length; i++) {
          this.addProp(`seq[${i}]`, `<a href="${makeBlueprintLink(val.seq[i])}">${val.seq[i]}</a>`);
        }
      }
      const extra = val.props ?? {};
      for (const [k, v] of Object.entries(extra)) {
        this.addProp(k, v);
      }
    }
  }
  makeClickies(text) {
    return text.replace(regex, (match, name, guid) => {
      const local = this.dialog?.nodes[guid] !== void 0;
      if (local) {
        return `<a class="local-clicky" data-guid="${guid}" href="${makeBlueprintLink(guid)}">${name}</a>`;
      } else {
        return `<a class="clicky" href="${makeBlueprintLink(guid)}">${name}</a>`;
      }
    });
  }
  centerOnNode(node) {
    let dat = node?.dat;
    if (!dat) return;
    if (dat.owner) {
      dat = dat.owner.dat;
    }
    this.centerOn(dat.x, dat.y);
  }
  async navigateTo(prefix, link) {
    let url = makeUrl(`dialog/${prefix}_${link}`);
    history.pushState(null, "", url);
    await this.handleRouting();
  }
  updateTransform(panX, panY, scale) {
    this.root?.setAttribute("transform", `translate(${panX}, ${panY}) scale(${scale})`);
  }
  installHandlers(svg, root) {
    let isPanning = false;
    const zoomSensitivity = 1e-3;
    const updateTransform = () => {
      root.setAttribute("transform", `translate(${this.panX}, ${this.panY}) scale(${this.scale})`);
    };
    svg.addEventListener("wheel", (e) => {
      e.preventDefault();
      const rect = svg.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const mouseY = e.clientY - rect.top;
      const worldX = (mouseX - this.panX) / this.scale;
      const worldY = (mouseY - this.panY) / this.scale;
      const zoomFactor = Math.exp(-e.deltaY * zoomSensitivity);
      const newScale = this.scale * zoomFactor;
      this.scale = newScale;
      this.panX = mouseX - worldX * this.scale;
      this.panY = mouseY - worldY * this.scale;
      updateTransform();
    });
    svg.addEventListener("mousedown", (e) => {
      if (e.button !== 0) return;
      e.preventDefault();
      const startX = e.clientX - this.panX;
      const startY = e.clientY - this.panY;
      const onMove = (moveEvent) => {
        moveEvent.preventDefault();
        const nextPanX = moveEvent.clientX - startX;
        const nextPanY = moveEvent.clientY - startY;
        if (isPanning || Math.abs(nextPanX - this.panX) > 2 || Math.abs(nextPanY - this.panY) > 2) {
          svg.style.cursor = "grabbing";
          svg.style.userSelect = "none";
          this.panX = nextPanX;
          this.panY = nextPanY;
          isPanning = true;
          updateTransform();
        }
      };
      const onUp = () => {
        window.removeEventListener("mousemove", onMove);
        window.removeEventListener("mouseup", onUp);
        svg.style.userSelect = "";
        svg.style.cursor = "";
        setTimeout(() => isPanning = false, 12);
      };
      window.addEventListener("mousemove", onMove);
      window.addEventListener("mouseup", onUp);
    });
    svg.addEventListener("mousemove", (e) => {
      const ret = nodeToUse(e, isPanning);
      const target = this.dialog?.nodes[ret || "_"];
      this.highlight = target ?? null;
    });
    svg.addEventListener("click", (e) => {
      const ret = nodeToUse(e, isPanning);
      if (ret) {
        e.preventDefault();
        const target = this.dialog?.nodes[ret];
        this.centerOnNode(target);
        return;
      }
      const guid = nodeToGuid(e, isPanning);
      if (!guid) return;
      const dialogNode = this.dialog?.nodes[guid];
      if (!dialogNode) return;
      this.selected = dialogNode;
    });
    svg.addEventListener("dblclick", (e) => {
      const guid = nodeToGuid(e, isPanning);
      if (guid) {
        document.location.pathname = makeBlueprintLink(guid);
        return;
      }
      const edgeIdx = nodeToEdge(e, isPanning);
      if (edgeIdx !== null) {
        const edge = this.allEdges[edgeIdx];
        if (edge.src === this._selected) {
          this.centerOnNode(edge.dst);
        } else if (edge.dst == this._selected) {
          this.centerOnNode(edge.src);
        }
        return;
      }
    });
  }
  createNodes(source) {
    const g = new dagre.graphlib.Graph();
    g.setGraph({ rankdir: "LR", nodesep: 50, ranksep: 200 });
    g.setDefaultEdgeLabel(() => ({}));
    const ns = "http://www.w3.org/2000/svg";
    function svg(tag) {
      return document.createElementNS(ns, tag);
    }
    const svgEl = svg("svg");
    this.graphEl.append(svgEl);
    const defs = svg("defs");
    svgEl.append(defs);
    const retGpSymbol = svg("symbol");
    retGpSymbol.setAttribute("id", "ret-gp");
    retGpSymbol.setAttribute("viewBox", "0 0 20 20");
    const retGpImg = svg("image");
    retGpImg.setAttribute("href", "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAA6AAAAOgBhtX2rwAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAHESURBVFiF7dc/aFRBEMfxz5zBExH8V2lhpaBWQhBBbARBsLEQbASxUCtLIdgJdgEttRX804i2EtJEsDIaG0lnoVhb+CcEEx2L5OS8vJd397iXa/KDad7s7nx3d2Z3X2SmUarVT6OIOBERDyPiytAJMrPUMIY7WEbiN/au12dQWy/4YcyuBu7YV2xtFACBG1joCZ64N8zgawCwH1MFgRN/cGjYANGpgoi4iAfYU5Yu+FYjzX7hA95iOjOnexvsxKOSWTdhj7G7a9U92cDgHftktZpgbgQAiacdgHF8HBHEuchMEbENtzCBtnLNWjkLBtF2nMSWAt/93jI8iJfrEN+tVesr4y4WjPemrMMFfC7o8F1XBg8I8aJgvMXCyygzn+MIJrHU5dqBS4PtwD8VnSHt0tswM39m5gSOYabbVROgUGNVDTJzHqcj4qyVU/LZhgJ0gUwNM3BHfT1ImtQmwCbARgLsK/j2o+8yrKuIaOE8zhS45yoBIuIqruNATYY2dpX4ZqsukFOaewssY7wqBy7XmnN/mszMd1UACw0Ff4XbULUFRw132RdxE601/wVliojjuKZ+Ei5hHu/xOjO//Dd+FUDT+guVYt/5i0YlRAAAAABJRU5ErkJggg==");
    retGpImg.classList.add("graph-ret-gp-icon");
    retGpImg.setAttribute("width", "20");
    retGpImg.setAttribute("height", "20");
    retGpSymbol.append(retGpImg);
    defs.append(retGpSymbol);
    const svgRoot = svg("g");
    svgRoot.classList.add("graph-root");
    svgEl.append(svgRoot);
    const nodeWidth = 200;
    const nodeWidthSmall = 100;
    const seqPadding = 20;
    const css = document.styleSheets[0];
    css.insertRule(`
            .graph-node-bg {
                --node-width: ${nodeWidth}px;
                --node-width-small: ${nodeWidthSmall}px;
            }
        `, css.cssRules.length);
    const smallNodes = {
      "AnswerList": true,
      "CueSequence": true,
      "CueSequenceExit": true
    };
    const seqs = [];
    const toSkip = [];
    for (const [guid, node] of Object.entries(source.nodes)) {
      const g2 = svg("g");
      const bg = svg("rect");
      bg.classList.add("graph-node-bg");
      const host = svg("foreignObject");
      let bgCond;
      if (node.flg & 1) {
        bgCond = svg("rect");
        bgCond.classList.add("graph-node-cond-bg");
        bgCond.classList.add("graph-node-bg");
        bgCond.setAttribute("rx", "6px");
        bgCond.setAttribute("ry", "6px");
        g2.append(bgCond);
        g2.classList.add("graph-node-cond");
      }
      bg.setAttribute("rx", "6px");
      bg.setAttribute("ry", "6px");
      g2.setAttribute("data-graph-node", guid);
      g2.classList.add(`graph-node-T-${node.typ}`);
      let width = smallNodes[node.typ] ? nodeWidthSmall : nodeWidth;
      const text = document.createElement("div");
      const out = node.out?.map((x) => source.nodes[x.to]) ?? [];
      if (node.typ == "CueSequence") {
        const seqCount = node.seq.length;
        width = seqCount * nodeWidth + seqCount * seqPadding;
        text.style.width = `${width}px`;
        host.append(text);
        seqs.push(node);
      } else {
        host.width.baseVal.value = width;
        const a = document.createElement("a");
        a.href = makeBlueprintLink(guid);
        text.className = "graph-node-text";
        text.innerHTML = node.text.replace("{n}", "<em>").replace("{/n}", "</em>");
        a.append(text);
        host.append(a);
      }
      g2.append(bg, host);
      svgRoot.appendChild(g2);
      node.dat = {
        g: g2,
        bg,
        bgCond,
        host,
        text,
        width,
        height: 0,
        in: [],
        out,
        id: guid,
        edgesIn: [],
        edgesOut: [],
        x: 0,
        y: 0
      };
    }
    for (const seq of seqs) {
      for (const childId of seq.seq) {
        const child = source.nodes[childId];
        child.dat.owner = seq;
        seq.dat.g.append(child.dat.g);
      }
      const exit = seq.dat.out[0];
      if (exit) {
        exit.dat.skip = true;
        toSkip.push(exit);
        seq.dat.out[0] = exit.dat.out[0];
      }
    }
    for (const skip of toSkip) {
      skip.dat.g.remove();
    }
    for (const node of Object.values(source.nodes)) {
      const size = node.dat.text.getBoundingClientRect();
      node.dat.width = size.width;
      node.dat.height = size.height;
      for (const successor of node.dat.out) {
        successor.dat.in.push(node);
      }
    }
    for (const seq of seqs) {
      let height = 0;
      for (const childId of seq.seq) {
        const child = source.nodes[childId];
        if (child.dat.height > height) height = child.dat.height;
      }
      seq.dat.height = height + seqPadding;
    }
    for (const [guid, node] of Object.entries(source.nodes)) {
      const { width: w, height: h } = node.dat;
      node.dat.bg.width.baseVal.value = w;
      node.dat.bg.height.baseVal.value = h;
      if (node.dat.bgCond) {
        node.dat.bgCond.width.baseVal.value = w;
        node.dat.bgCond.height.baseVal.value = h;
      }
      node.dat.host.width.baseVal.value = w;
      node.dat.host.height.baseVal.value = h;
      node.dat.bg.setAttribute("y", (-h / 2).toString());
      node.dat.bgCond?.setAttribute("y", (-h / 2).toString());
      node.dat.host.setAttribute("y", (-h / 2).toString());
      if (!node.dat.owner && !node.dat.skip) {
        node.dat.bg.setAttribute("x", (-w / 2).toString());
        node.dat.bgCond?.setAttribute("x", (-w / 2).toString());
        node.dat.host.setAttribute("x", (-w / 2).toString());
        g.setNode(guid, { width: w, height: h });
        const gp = getAncestor(node, 2);
        {
          for (const edge of node.dat.out) {
            if (edge) {
              if (edge == gp) {
                const retIcon = svg("use");
                retIcon.setAttribute("x", (w / 2 + 8).toString());
                retIcon.setAttribute("y", (h / 2 - 28).toString());
                retIcon.setAttribute("width", "20");
                retIcon.setAttribute("height", "20");
                retIcon.setAttribute("href", "#ret-gp");
                retIcon.setAttribute("data-target", gp.dat.id);
                node.dat.g.append(retIcon);
                node.dat.ret = retIcon;
              } else {
                g.setEdge(guid, edge.dat.id);
              }
            }
          }
        }
      }
    }
    for (const seq of seqs) {
      let x = -seq.dat.width / 2 + seqPadding / 2;
      for (const childId of seq.seq) {
        const child = source.nodes[childId];
        child.dat.bg.setAttribute("x", x.toString());
        child.dat.bgCond?.setAttribute("x", x.toString());
        child.dat.host.setAttribute("x", x.toString());
        x += child.dat.width + seqPadding;
      }
    }
    g.setGraph({
      rankdir: "LR",
      rankspec: 20
      //nodesep: 20,
    });
    dagre.layout(g);
    for (const [guid, node] of Object.entries(source.nodes)) {
      const layout = g.node(guid);
      if (layout) {
        node.dat.g.setAttribute("transform", `translate(${layout.x}, ${layout.y})`);
        node.dat.x = layout.x;
        node.dat.y = layout.y;
      }
    }
    for (const edge of g.edges()) {
      const dat = g.edge(edge);
      const pathPts = g.edge(edge).points;
      const d = createPath(pathPts);
      const edgeIndex = this.allEdges.length;
      const path = svg("path");
      path.classList.add("graph-edge");
      path.setAttribute("data-edge-idx", edgeIndex.toString());
      path.setAttribute("d", d);
      path.setAttribute("marker-end", "url(#arrowhead)");
      const src = source.nodes[edge.v];
      const dst = source.nodes[edge.w];
      const edgeDat = {
        path,
        src,
        srcPort: src.out.find((x) => x.to === edge.w)?.port,
        dst
      };
      this.allEdges.push(edgeDat);
      src.dat.edgesOut.push(edgeDat);
      dst.dat.edgesIn.push(edgeDat);
      svgRoot.appendChild(path);
      if (edgeDat.srcPort) {
        const labelG = svg("g");
        labelG.classList.add("port-label", "port-out");
        let { x, y } = pathPts[0];
        const yDiff = y - pathPts[1].y;
        const xDiff = x - pathPts[1].x;
        if (Math.abs(yDiff) < 4) {
          y -= 10;
        } else if (yDiff > 20) {
          y -= 20;
        }
        if (Math.abs(xDiff) < 4) {
          x -= 10;
        } else if (xDiff > 20) {
          x -= 20;
        }
        labelG.setAttribute("transform", `translate(${x}, ${y})`);
        const bg = svg("rect");
        bg.setAttribute("rx", "6px");
        bg.setAttribute("ry", "6px");
        const w = 20;
        const h = 20;
        bg.setAttribute("width", `${w}px`);
        bg.setAttribute("height", `${h}px`);
        const host = svg("foreignObject");
        const labelDiv = document.createElement("div");
        const label = document.createElement("div");
        host.setAttribute("width", `${w}px`);
        host.setAttribute("height", `${h}px`);
        labelDiv.style.height = `${h}px`;
        labelDiv.append(label);
        label.textContent = portNames[edgeDat.srcPort] || edgeDat.srcPort;
        host.append(labelDiv);
        labelG.append(bg, host);
        svgRoot.append(labelG);
      }
    }
    this.installHandlers(svgEl, svgRoot);
    this.root = svgRoot;
  }
  async handleRouting() {
    const id = getActualPath()[1];
    const graphResponse = await fetch(makeUrl(`dlg/graph/${id}`));
    const dialog = await graphResponse.json();
    this.allEdges = [];
    this.selected = null;
    this.dialog = dialog;
    this.graphEl.innerHTML = "";
    this.createNodes(dialog);
  }
};

// frontend/app.ts
var classToggleInactive = "toggle-inactive";
var classToggleActive = "toggle-active";
var apiPrefix = "";
var apiParts = 0;
function makeUrl(path) {
  return `${apiPrefix}/${path}`;
}
function getActualPath() {
  return window.location.pathname.split("/").filter((p) => p).slice(apiParts);
}
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
    apiParts = 1;
    return segments[0];
  }
  return null;
}
var game = getGameIdentifier(window.location, "bubbleprints.dev");
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
if (game == null || !knownGames.has(game)) {
  document.body.style.margin = "0";
  document.body.innerHTML = `<div style="display: flex; justify-content: center; align-items: center; height: 100vh;"> <h1 style="font-size: 8vw; white-space: nowrap;">Unknown game: ${game}</h1></div>`;
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
    this.hideSearch = document.getElementById("hide-search");
    this.titleSpan = document.getElementById("currentTitle");
    this.titleBar = document.getElementById("title-bar");
    this.backBtn = document.getElementById("backBtn");
    this.openDialog = document.getElementById("openDialog");
    this.openDialogButton = this.openDialog?.firstElementChild;
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
      if (e.key == "Escape") {
        e.preventDefault();
        this.searchBar.blur();
        this.handleRouting();
      }
      if (e.key == "Enter") {
        const tbody = this.resultsTable.querySelector("tbody");
        if (!tbody) return;
        if (this.resultCursor < 0 || this.resultCursor > tbody.rows.length) return;
        const guid = tbody.rows[this.resultCursor].cells[3].textContent;
        e.preventDefault();
        this.searchBar.blur();
        if (guid && guid !== "") {
          this.navigateTo(makeBlueprintLink(guid));
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
        const { game: game2, guid } = this.parseRawLink();
        const showRaw = !this.isShowingRaw;
        this.navigateTo(makeBlueprintLink(guid), { raw: showRaw });
        e.preventDefault();
      }
    });
    this.hideSearch.addEventListener("click", (_) => {
      if (this.blueprintView.classList.contains("hide")) {
        this.handleRouting();
      }
    });
    this.copyRaw.addEventListener("click", async (_) => {
      const { game: game2, guid } = this.parseRawLink();
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
    const game2 = href[href.length - 2];
    const guid = href[href.length - 1];
    return { game: game2, guid };
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
    const path = getActualPath();
    if (path.length === 0) {
      this.showSearch();
    } else {
      const query = new URLSearchParams(window.location.search);
      this.loadAndShowBlueprint(path[0], {
        raw: query.get("raw") === "true"
      });
    }
  }
  showSearch() {
    this.moveResultCursor(0);
    setVisible(this.blueprintView, false);
    setVisible(this.titleBar, false);
    setVisible(this.searchView, true);
    this.hideSearch.classList.remove("disabled");
  }
  showBlueprintView() {
    setVisible(this.blueprintView, true);
    setVisible(this.titleBar, true);
    setVisible(this.searchView, false);
    this.hideSearch.classList.add("disabled");
  }
  // Helper to change URL without reloading
  navigateTo(link, opts) {
    let url = link;
    if (opts?.raw === true) {
      url += "?raw=true";
    }
    history.pushState(null, "", url);
    this.handleRouting();
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
      const href = makeBlueprintLink(row.guidText);
      tr.innerHTML = `
            <td><a href="${href}">${row.name}</a></td>
            <td class="col-priority-1">${row.typeName}</td>
            <td class="col-priority-2">${row.namespace}</td>
            <td class="col-priority-3">${row.guidText}</td>
        `;
      if (active) {
        this.activeRow = tr;
        tr.classList.add("cursor-active");
      }
      const link = tr.firstElementChild;
      link.addEventListener("click", (e) => this.handleLinkClick(e, href));
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
    guid = normalizeGuid2(guid);
    this.showRaw.href = `${apiPrefix}/bp/get/${guid}`;
    this.resultCursor = 0;
    this.activeRow?.classList?.remove("cursor-active");
    this.activeRow = null;
    this.showBlueprintView();
    const container = document.getElementById("bp-content");
    container.innerHTML = "Loading...";
    const resultCount = this.resultsTable.querySelector("tbody")?.rows.length;
    if (resultCount && resultCount > 0) {
      this.backBtn.disabled = false;
    } else {
      console.log("disabling");
      this.backBtn.disabled = true;
    }
    let dialogGuid = "";
    try {
      if (opts?.raw === true) {
        this.isShowingRaw = true;
        this.showRaw.classList.replace("toggle-inactive", "toggle-active");
        const { name, bp } = await getRawBlueprint(guid);
        this.titleSpan.innerText = name || "Blueprint";
        const json = await bp.json();
        container.innerHTML = `<div class="json-view">${JSON.stringify(json, null, 2)}</div>`;
        const type = json["$type"];
      } else {
        this.isShowingRaw = false;
        this.showRaw.classList.replace("toggle-active", "toggle-inactive");
        const response = await viewBlueprint(guid);
        const obj = await response.json();
        const elements = obj.blueprint;
        this.titleSpan.innerText = elements[0].key || "Blueprint";
        container.innerHTML = "";
        const blueprintDom = createBlueprintView(elements, apiPrefix, guid, this.viewCallbacks);
        const refs = obj.references;
        dialogGuid = obj.dialog;
        this.referencePanel.innerHTML = "<h4>References</h4>";
        for (const ref of refs) {
          const a = document.createElement("a");
          a.className = "bp-link";
          a.href = makeBlueprintLink(ref.id);
          a.addEventListener("click", (e) => this.handleLinkClick(e, a.href));
          a.textContent = ref.name;
          this.referencePanel.appendChild(a);
        }
        container.appendChild(blueprintDom);
      }
      if (dialogGuid?.length > 0) {
        this.openDialogButton.disabled = false;
        this.openDialog.href = makeUrl(`dialog/${dialogGuid}`);
      } else {
        this.openDialogButton.disabled = true;
      }
    } catch (err) {
      container.textContent = "Error: " + (err instanceof Error ? err.message : String(err));
    }
  }
};
function makeBlueprintLink(guid) {
  return `${apiPrefix}/bp_${guid}`;
}
async function getRawBlueprint(guid) {
  const rawResponse = await fetch(`${apiPrefix}/bp/get/${guid}`);
  const name = rawResponse.headers.get("BP-Name");
  return { name, bp: rawResponse };
}
async function viewBlueprint(guid) {
  return await fetch(`${apiPrefix}/bp/view/${guid}`);
}
async function getBlueprint2(guid, withStrings = false) {
  const stringQuery = `?strings=${withStrings}`;
  const response = await fetch(`${apiPrefix}/bp/get/${guid}${stringQuery}`);
  return await response.json();
}
function normalizeGuid2(guid) {
  return guid.replace("bp_", "");
}
export {
  DialogPage,
  MainPage,
  createToggle,
  game,
  getActualPath,
  getBlueprint2 as getBlueprint,
  makeBlueprintLink,
  makeUrl,
  normalizeGuid2 as normalizeGuid,
  setVisible,
  viewBlueprint
};
