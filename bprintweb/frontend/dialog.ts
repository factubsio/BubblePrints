import { getBlueprint, getActualPath, normalizeGuid, makeUrl, makeBlueprintLink } from './app';
import dagre from 'https://esm.sh/dagre@0.8.5';

interface EdgeDat {
    path: SVGPathElement;
    src: DialogNode;
    srcPort?: string;
    dst: DialogNode;
    dstPort?: string;
}

interface GraphDat {
    text: HTMLDivElement;
    host: SVGForeignObjectElement;
    bg: SVGRectElement;
    bgCond?: SVGRectElement;
    g: SVGGElement;
    width: number;
    height: number;
    owner?: DialogNode;

    ret?: SVGUseElement;

    // do not process this (dummy node like seq exit)
    skip?: boolean;

    in: DialogNode[];
    out: DialogNode[];
    id: string;

    edgesIn: EdgeDat[];
    edgesOut: EdgeDat[];

    x: number;
    y: number;

    seqItem?: boolean;
}

type DialogNode = {
    text: string;
    typ: string;
    flg: number;
    dat: GraphDat;
    seq?: string[];
    out: {
        to: string, port?: string
    }[]
    props?: Record<string, any>;
};
type DialogSource = {
    nodes: Record<string, DialogNode>
};

interface XY {
    x: number;
    y: number;
}

const regex = /\^([^|]+)\|([^$]+)\$/g;
function getAncestor(node: DialogNode, level: number = 1): DialogNode | null {
    for (let l = 0; l < level; l++) {
        if (node.dat.in.length !== 1) {
            return null;
        }
        node = node.dat.in[0];
    }
    return node;
}

const portNames: Record<string, string> = {
    success: '✔',
    fail: '✖',

};
function createPath(points: XY[]) {
   if (points.length < 2) return '';
    
    // 1. Move to strict start
    let d = `M ${points[0].x},${points[0].y}`;

    // 2. Draw line to first approach
    // (If only 2 points, this draws the whole line)
    d += ` L ${points[1].x},${points[1].y}`;

    // 3. Curve through intermediates (if > 2 points)
    // We treat the points between 0 and N as control points for a spline
    // This implies we don't pass THROUGH them, but curve AROUND them.
    // To replicate the Mermaid/Dagre "Basis" look:
    // We assume the points provided are B-Spline control points.
    // The standard SVG approach for this is complicated, 
    // but the midpoint approximation is visually nearly identical.
    
    // Overwrite for >2 points to get the curve:
    if (points.length >= 3) {
        // Reset d to avoid the straight line L above
        d = `M ${points[0].x},${points[0].y}`;
        
        // L to the midpoint of the first segment
        d += ` L ${(points[0].x + points[1].x)/2},${(points[0].y + points[1].y)/2}`;
        
        // Curve between midpoints
        for (let i = 1; i < points.length - 1; i++) {
            const p1 = points[i];
            const p2 = points[i+1];
            const midX = (p1.x + p2.x) / 2;
            const midY = (p1.y + p2.y) / 2;
            // Quadratic Bezier: Control Point = p1, End = Midpoint
            d += ` Q ${p1.x},${p1.y} ${midX},${midY}`;
        }
        
        // L to final exact point
        d += ` L ${points[points.length-1].x},${points[points.length-1].y}`;
    }

    return d;
}
function nodeToUse(e: MouseEvent, isPanning: boolean) {
    if (isPanning) {
        return null;
    }
    const g = (e.target as Element).closest('use');

    return g?.getAttribute('data-target') ?? null;
}

function nodeToGuid(e: MouseEvent, isPanning: boolean) {
    e.preventDefault();
    if (isPanning) {
        return;
    }
    const g = (e.target as Element).closest('g');

    return g?.getAttribute('data-graph-node');
}

function nodeToEdge(e: MouseEvent, isPanning: boolean) {
    e.preventDefault();
    if (isPanning) {
        return null;
    }
    const g = (e.target as Element).closest('path');

    const idxStr = g?.getAttribute('data-edge-idx');
    if (idxStr !== undefined && idxStr !== null)
        return parseInt(idxStr);
    else
        return null;
}

export class DialogPage {
    speakerEl = document.getElementById('cue-speaker-name')!;
    graphEl = document.getElementById('dialog-graph')!;
    commentEl = document.getElementById('dev-comment')!;
    propGrid = document.getElementById('props') as HTMLDivElement;

    dialog: DialogSource | null = null;
    _selected: DialogNode | null = null;
    _highlight: DialogNode | null = null;

    allEdges: EdgeDat[] = [];

    btnGoToSelected = document.getElementById('do-goto-selected') as HTMLButtonElement;

    root: SVGGElement | null = null;

    centerOn(x: number, y: number) {
        const viewportRect = this.graphEl.getBoundingClientRect();
        const viewportCenterX = viewportRect.width / 2;
        const viewportCenterY = viewportRect.height / 2;

        // Calculate the required pan to move the node's scaled position to the viewport center
        this.panX = viewportCenterX - (x * this.scale);
        this.panY = viewportCenterY - (y * this.scale);

        // Apply the new transform
        this.root?.classList.add('animating');
        setTimeout(() => this.root?.classList.remove('animating'), 205);
        this.updateTransform(this.panX, this.panY, this.scale);
    }

    addProp(label: string, val: any) {
        const labelEl = document.createElement('span');
        labelEl.className = 'prop-key';
        labelEl.textContent = label;

        const valEl = document.createElement('span');
        valEl.className = 'prop-val';
        valEl.innerHTML = this.makeClickies(val);

        valEl.querySelectorAll('a.local-clicky').forEach(aEl => {
            const a = aEl as HTMLAnchorElement;
            const guid = a.getAttribute('data-guid') ?? '_';
            const target = this.dialog?.nodes[guid];
            if (target) {
                a.addEventListener('mouseenter', _ => {
                    this.highlight = target;
                });
                a.addEventListener('mouseleave', _ => {
                    this.highlight = null;
                });
                a.addEventListener('click', e => {
                    e.preventDefault();
                    this.centerOnNode(target);
                    this.selected = target;
                })
            }
        });

        this.propGrid.append(labelEl, valEl);
    }

    get highlight() { return this._highlight; }

    set highlight(val: DialogNode | null) {
        if (this._highlight) {
            this._highlight.dat.g.classList.remove('highlight');
        }

        this._highlight = val;

        if (this._highlight) {
            this._highlight.dat.g.classList.add('highlight');
        }

    }

    get selected() { return this._selected; }
    set selected(val: DialogNode | null) {
        if (this._selected) {
            this._selected.dat.g.classList.remove('selected');
            this.propGrid.innerHTML = '';
        }

        for (const edge of this.allEdges) {
            edge.path.classList.remove('hi-in', 'hi-out');
        }

        this._selected = val;

        if (val) {
            val.dat.g.classList.add('selected');

            for (const edgeIn of val.dat.edgesOut) {
                edgeIn.path.classList.add('hi-out');
            }
            for (const edgeIn of val.dat.edgesIn) {
                edgeIn.path.classList.add('hi-in');
            }
            this.addProp('id', val.dat.id);
            this.addProp('typ', val.typ);
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

    makeClickies(text: string) {
        return text.replace(regex, (match, name, guid) => {
            const local = this.dialog?.nodes[guid] !== undefined;
            if (local) {
                return `<a class="local-clicky" data-guid="${guid}" href="${makeBlueprintLink(guid)}">${name}</a>`;
            } else {
                return `<a class="clicky" href="${makeBlueprintLink(guid)}">${name}</a>`;
            }
        });
    }

    constructor() {
        window.addEventListener('popstate', async () => await this.handleRouting());
        this.handleRouting();

        this.btnGoToSelected.addEventListener('click', _ => {
            this.centerOnNode(this._selected);

        });

    }
    centerOnNode(node: DialogNode | null | undefined) {
        let dat = node?.dat;
        if (!dat) return;

        if (dat.owner) {
            dat = dat.owner.dat;
        }

        this.centerOn(dat.x, dat.y);
    }

    async navigateTo(prefix: string, link: string) {
        let url = makeUrl(`dialog/${prefix}_${link}`);
        history.pushState(null, "", url);
        await this.handleRouting();
    }

    updateTransform(panX: number, panY: number, scale: number) {
        this.root?.setAttribute('transform', `translate(${panX}, ${panY}) scale(${scale})`);
    }

    panX = 0;
    panY = 0;
    scale = 1;

    installHandlers(svg: SVGSVGElement, root: SVGGElement) {
        let isPanning = false;

        const zoomSensitivity = 0.001; // Adjust for speed

        const updateTransform = () => {
            root.setAttribute('transform', `translate(${this.panX}, ${this.panY}) scale(${this.scale})`);
        }

        svg.addEventListener('wheel', (e: WheelEvent) => {
            e.preventDefault();

            const rect = svg.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;

            const worldX = (mouseX - this.panX) / this.scale;
            const worldY = (mouseY - this.panY) / this.scale;

            // Negative deltaY means zooming in
            const zoomFactor = Math.exp(-e.deltaY * zoomSensitivity);
            const newScale = this.scale * zoomFactor;

            this.scale = newScale;

            this.panX = mouseX - worldX * this.scale;
            this.panY = mouseY - worldY * this.scale;

            updateTransform();
        });

        svg.addEventListener('mousedown', (e: MouseEvent) => {
            if (e.button !== 0) return;
            e.preventDefault();

            const startX = e.clientX - this.panX;
            const startY = e.clientY - this.panY;

            const onMove = (moveEvent: MouseEvent) => {
                moveEvent.preventDefault();

                const nextPanX = moveEvent.clientX - startX;
                const nextPanY = moveEvent.clientY - startY;

                if (isPanning || Math.abs(nextPanX - this.panX) > 2 || Math.abs(nextPanY - this.panY) > 2) {
                    svg.style.cursor = 'grabbing';
                    svg.style.userSelect = 'none';
                    this.panX = nextPanX;
                    this.panY = nextPanY;
                    isPanning = true;
                    updateTransform();
                }
            };

            const onUp = () => {
                window.removeEventListener('mousemove', onMove);
                window.removeEventListener('mouseup', onUp);
                svg.style.userSelect = '';
                svg.style.cursor = '';
                setTimeout(() => isPanning = false, 12);
            };

            window.addEventListener('mousemove', onMove);
            window.addEventListener('mouseup', onUp);
        });

        svg.addEventListener('mousemove', e => {
            const ret = nodeToUse(e, isPanning);
            const target = this.dialog?.nodes[ret || '_'];
            this.highlight = target ?? null;
        });

        svg.addEventListener('click', (e: MouseEvent) => {
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

        svg.addEventListener('dblclick', (e: MouseEvent) => {
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

    createNodes(source: DialogSource) {
        // 1. Initialize Dagre
        const g = new dagre.graphlib.Graph();
        g.setGraph({ rankdir: 'LR', nodesep: 50, ranksep: 200 });
        g.setDefaultEdgeLabel(() => ({}));

        const ns = 'http://www.w3.org/2000/svg';

        function svg<K extends keyof SVGElementTagNameMap>(tag: K): SVGElementTagNameMap[K] {
            return document.createElementNS(ns, tag);
        }

        const svgEl = svg('svg');
        this.graphEl.append(svgEl);

        const defs = svg('defs');
        svgEl.append(defs);

        const retGpSymbol = svg('symbol');
        retGpSymbol.setAttribute('id', 'ret-gp');
        retGpSymbol.setAttribute('viewBox', '0 0 20 20');

        const retGpImg = svg('image');
        retGpImg.setAttribute('href', 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAA6AAAAOgBhtX2rwAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAHESURBVFiF7dc/aFRBEMfxz5zBExH8V2lhpaBWQhBBbARBsLEQbASxUCtLIdgJdgEttRX804i2EtJEsDIaG0lnoVhb+CcEEx2L5OS8vJd397iXa/KDad7s7nx3d2Z3X2SmUarVT6OIOBERDyPiytAJMrPUMIY7WEbiN/au12dQWy/4YcyuBu7YV2xtFACBG1joCZ64N8zgawCwH1MFgRN/cGjYANGpgoi4iAfYU5Yu+FYjzX7hA95iOjOnexvsxKOSWTdhj7G7a9U92cDgHftktZpgbgQAiacdgHF8HBHEuchMEbENtzCBtnLNWjkLBtF2nMSWAt/93jI8iJfrEN+tVesr4y4WjPemrMMFfC7o8F1XBg8I8aJgvMXCyygzn+MIJrHU5dqBS4PtwD8VnSHt0tswM39m5gSOYabbVROgUGNVDTJzHqcj4qyVU/LZhgJ0gUwNM3BHfT1ImtQmwCbARgLsK/j2o+8yrKuIaOE8zhS45yoBIuIqruNATYY2dpX4ZqsukFOaewssY7wqBy7XmnN/mszMd1UACw0Ff4XbULUFRw132RdxE601/wVliojjuKZ+Ei5hHu/xOjO//Dd+FUDT+guVYt/5i0YlRAAAAABJRU5ErkJggg==');
        retGpImg.classList.add('graph-ret-gp-icon');
        retGpImg.setAttribute('width', '20');
        retGpImg.setAttribute('height', '20');
        retGpSymbol.append(retGpImg);

        defs.append(retGpSymbol);


        const svgRoot = svg('g');
        svgRoot.classList.add('graph-root');
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
        `, css.cssRules.length)

        const smallNodes: Record<string, boolean> = {
            'AnswerList': true,
            'CueSequence': true,
            'CueSequenceExit': true,
        };

        const seqs = [];
        const toSkip = [];

        for (const [guid, node] of Object.entries(source.nodes)) {
            const g = svg('g');
            const bg = svg('rect');
            bg.classList.add('graph-node-bg');
            const host = svg('foreignObject');

            let bgCond;

            if (node.flg & 1) {
                bgCond = svg('rect');
                bgCond.classList.add('graph-node-cond-bg');
                bgCond.classList.add('graph-node-bg');
                bgCond.setAttribute('rx', '6px');
                bgCond.setAttribute('ry', '6px');
                g.append(bgCond);
                g.classList.add('graph-node-cond');
            }

            bg.setAttribute('rx', '6px');
            bg.setAttribute('ry', '6px');

            g.setAttribute('data-graph-node', guid);
            g.classList.add(`graph-node-T-${node.typ}`);

            let width = smallNodes[node.typ] ? nodeWidthSmall : nodeWidth;

            const text = document.createElement('div');

            const out = node.out?.map(x => source.nodes[x.to]) ?? [];

            if (node.typ == 'CueSequence') {
                const seqCount = node.seq!.length;
                width = seqCount * nodeWidth + seqCount * seqPadding;
                text.style.width = `${width}px`;
                host.append(text);
                seqs.push(node);

            } else {
                host.width.baseVal.value = width;
                const a = document.createElement('a');
                a.href = makeBlueprintLink(guid);
                text.className = 'graph-node-text';
                text.innerHTML = node.text.replace('{n}', '<em>').replace('{/n}', '</em>');
                a.append(text);
                host.append(a);
            }

            g.append(bg, host);
            svgRoot.appendChild(g);

            node.dat = {
                g,
                bg,
                bgCond,
                host,
                text,
                width: width,
                height: 0,
                in: [],
                out,
                id: guid,
                edgesIn: [],
                edgesOut: [],
                x: 0,
                y: 0,
            };
        }

        for (const seq of seqs) {
            for (const childId of seq.seq!) {
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
            for (const childId of seq.seq!) {
                const child = source.nodes[childId];
                if (child.dat.height > height) height = child.dat.height;
            }
            seq.dat.height = height + seqPadding;
        }

        for (const [guid, node] of Object.entries(source.nodes)) {
            const { width: w, height: h } = node.dat;

            // 1. Center the Background
            node.dat.bg.width.baseVal.value = w;
            node.dat.bg.height.baseVal.value = h;

            if (node.dat.bgCond) {
                node.dat.bgCond.width.baseVal.value = w;
                node.dat.bgCond.height.baseVal.value = h;
            }

            // 2. Center the ForeignObject
            node.dat.host.width.baseVal.value = w;
            node.dat.host.height.baseVal.value = h;

            node.dat.bg.setAttribute('y', (-h / 2).toString());
            node.dat.bgCond?.setAttribute('y', (-h / 2).toString());
            node.dat.host.setAttribute('y', (-h / 2).toString());

            if (!node.dat.owner && !node.dat.skip) {

                node.dat.bg.setAttribute('x', (-w / 2).toString());
                node.dat.bgCond?.setAttribute('x', (-w / 2).toString());
                node.dat.host.setAttribute('x', (-w / 2).toString());

                g.setNode(guid, { width: w, height: h }); // Set size to match your CSS/HTML

                const gp = getAncestor(node, 2);
                {
                    for (const edge of node.dat.out) {
                        if (edge) {
                            if (edge == gp) {
                                const retIcon = svg('use');
                                retIcon.setAttribute('x', (w / 2 + 8).toString());
                                retIcon.setAttribute('y', (h / 2 - 28).toString());
                                retIcon.setAttribute('width', '20');
                                retIcon.setAttribute('height', '20');
                                retIcon.setAttribute('href', '#ret-gp');
                                retIcon.setAttribute('data-target', gp.dat.id);
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
            for (const childId of seq.seq!) {
                const child = source.nodes[childId];
                child.dat.bg.setAttribute('x', x.toString());
                child.dat.bgCond?.setAttribute('x', x.toString());
                child.dat.host.setAttribute('x', x.toString());

                x += child.dat.width + seqPadding;
            }
        }

        g.setGraph({
            rankdir: 'LR',
            rankspec: 20,
            //nodesep: 20,
        });
        dagre.layout(g);

        for (const [guid, node] of Object.entries(source.nodes)) {
            const layout = g.node(guid);
            if (layout) {
                node.dat.g.setAttribute('transform', `translate(${layout.x}, ${layout.y})`);
                node.dat.x = layout.x;
                node.dat.y = layout.y;
            }
        }

        for (const edge of g.edges()) {
            const dat = g.edge(edge);
            const pathPts = g.edge(edge).points;
            const d = createPath(pathPts);

            const edgeIndex = this.allEdges.length;

            const path = svg('path');
            path.classList.add('graph-edge');
            path.setAttribute('data-edge-idx', edgeIndex.toString());
            path.setAttribute('d', d);
            path.setAttribute('marker-end', 'url(#arrowhead)');

            const src = source.nodes[edge.v];
            const dst = source.nodes[edge.w];

            const edgeDat: EdgeDat = {
                path,
                src,
                srcPort: src.out.find(x => x.to === edge.w)?.port,
                dst,
            };
            this.allEdges.push(edgeDat);

            src.dat.edgesOut.push(edgeDat);
            dst.dat.edgesIn.push(edgeDat);

            svgRoot.appendChild(path);
            if (edgeDat.srcPort) {
                const labelG = svg('g');
                labelG.classList.add('port-label', 'port-out');

                let { x, y } = pathPts[0] as { x: number, y: number };

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



                labelG.setAttribute('transform', `translate(${x}, ${y})`);

                const bg = svg('rect');
                bg.setAttribute('rx', '6px');
                bg.setAttribute('ry', '6px');
                const w = 20;
                const h = 20;
                bg.setAttribute('width', `${w}px`);
                bg.setAttribute('height', `${h}px`);
                const host = svg('foreignObject');

                const labelDiv = document.createElement('div');
                const label = document.createElement('div');

                host.setAttribute('width', `${w}px`);
                host.setAttribute('height', `${h}px`);
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
        const dialog = await graphResponse.json() as DialogSource;

        this.allEdges = [];
        this.selected = null;
        this.dialog = dialog;

        this.graphEl.innerHTML = '';

        this.createNodes(dialog);
    }

}

interface Cue {
    id: string;
    text: string;
    answers: Answer[];
    continueCues: Cue[];
}
interface Answer {
    id: string;
    text: string;
    nextCues: string[];
    cues: Cue[];
}

function getGuid(raw: string) {
    return raw.replace('!bp_', '');
}

interface TextNode {
    m_Key: string;
    Shared?: {
        stringkey: string;
    }
}

interface BlueprintWithStrings {
    root: any;
    strs: { [key: string]: string; };
}

function getText(textNode: TextNode, strings: { [key: string]: string; }): string | undefined {
    if (textNode.m_Key && textNode.m_Key !== '') {
        return strings[textNode.m_Key];
    } else if (textNode.Shared) {
        return strings[textNode.Shared.stringkey];
    }

    return undefined;
}
