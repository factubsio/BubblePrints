import { DisplayableElement, ViewCallbacks, createBlueprintView } from "./treeView";

const classToggleInactive = 'toggle-inactive';
const classToggleActive = 'toggle-active';

let apiPrefix = '';
let apiParts = 0;

export function makeUrl(path: string) {
    return `${apiPrefix}/${path}`;
}

export function getActualPath() {
    return window.location.pathname.split('/').filter(p => p).slice(apiParts);
}

function getGameIdentifier(url: Location, baseDomain: string): string | null {
    const host = url.hostname;
    const subdomainSuffix = `.${baseDomain}`;

    if (host.toLowerCase().endsWith(subdomainSuffix.toLowerCase())) {
        const gamePart = host.substring(0, host.length - subdomainSuffix.length);
        if (gamePart && !gamePart.includes('.')) {
            apiPrefix = '';
            return gamePart;
        }
    }

    const segments = url.pathname.split('/').filter(Boolean);
    if (segments.length > 0) {
        apiPrefix = '/' + segments[0];
        apiParts = 1;
        return segments[0];
    }

    return null;
}

export let game = getGameIdentifier(window.location, 'bubbleprints.dev');
interface GameLogo {
    name: string;
}
const knownGames = new Map<string, GameLogo>([
    ['km', {
        name: 'King Maker'
    }],
    ['wrath', {
        name: 'Wrath of the Righetous'
    }],
    ['rt', {
        name: 'Rogue Trader'
    }],
    ['dh', {
        name: 'Dark Heresy'
    }],
]);


if (game == null || !knownGames.has(game)) {
    document.body.style.margin = '0';
    document.body.innerHTML = `<div style="display: flex; justify-content: center; align-items: center; height: 100vh;"> <h1 style="font-size: 8vw; white-space: nowrap;">Unknown game: ${game}</h1></div>`;
}

export function createToggle(id: string, onChange: (on: boolean) => void) {
    const el = document.getElementById(id);
    if (!el) return;

    const isA = el.tagName === 'A';

    let state = false;

    const setState = (newState: boolean) => {
        state = newState;
        if (state)
            el.classList.replace(classToggleInactive, classToggleActive);
        else
            el.classList.replace(classToggleActive, classToggleInactive);
    }

    el.addEventListener('click', e => {
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

export function setVisible(el: HTMLElement, show: boolean) {
    if (show) el.classList.remove('hide');
    else el.classList.add('hide');
}

export class MainPage {
    searchBar = document.getElementById('searchBox') as HTMLInputElement;
    resultsTable = document.getElementById('resultsTable') as HTMLTableElement;

    searchView = document.getElementById('searchView') as HTMLDivElement;
    blueprintView = document.getElementById('blueprintView') as HTMLDivElement;
    hideSearch = document.getElementById('hide-search') as HTMLSpanElement;

    titleSpan = document.getElementById('currentTitle') as HTMLSpanElement;
    titleBar = document.getElementById('title-bar') as HTMLDivElement;

    backBtn = document.getElementById('backBtn') as HTMLButtonElement;

    openDialog = document.getElementById('openDialog') as HTMLAnchorElement;
    openDialogButton = this.openDialog?.firstElementChild as HTMLButtonElement;

    showRaw = document.getElementById('show-raw-link') as HTMLAnchorElement;
    copyRaw = document.getElementById('copy-raw') as HTMLButtonElement;

    referencePanel = document.getElementById('bp-references') as HTMLDivElement;
    showReferences = createToggle('toggle-references', on => {
        setVisible(this.referencePanel, on);
    });

    inFlight: Promise<any> | null = null;
    pendingQuery: string | null = null;

    resultCursor = 0;
    activeRow: HTMLTableRowElement | null = null;

    isShowingRaw = false;

    constructor() {
        this.backBtn.addEventListener('click', () => {
            history.pushState(null, "", "/"); // Go to root
            this.showSearch();
        });

        this.searchBar.addEventListener('keydown', e => {

            if (e.key == 'ArrowDown') {
                this.moveResultCursor(1);
                e.preventDefault();
            }
            if (e.key == 'ArrowUp') {
                this.moveResultCursor(-1);
                e.preventDefault();
            }
            if (e.key == 'Escape') {
                e.preventDefault();
                this.searchBar.blur();
                this.handleRouting();
            }
            if (e.key == 'Enter') {
                const tbody = this.resultsTable.querySelector("tbody");
                if (!tbody) return;
                if (this.resultCursor < 0 || this.resultCursor > tbody.rows.length) return;
                const guid = tbody.rows[this.resultCursor].cells[3].textContent;
                e.preventDefault();
                this.searchBar.blur();
                if (guid && guid !== '') {
                    this.navigateTo(makeBlueprintLink(guid));
                }
            }
        });

        window.addEventListener('keydown', e => {
            if (e.ctrlKey && e.key == 'p') {
                e.preventDefault();
                this.searchBar.focus();
            }
        });

        this.showRaw.addEventListener('click', e => {
            if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
                const { game, guid } = this.parseRawLink();

                const showRaw = !this.isShowingRaw;
                this.navigateTo(makeBlueprintLink(guid), { raw: showRaw });
                e.preventDefault();
            }
        });

        this.hideSearch.addEventListener('click', _ => {
            if (this.blueprintView.classList.contains('hide')) {
                this.handleRouting();
            }
        });

        this.copyRaw.addEventListener('click', async _ => {
            const { game, guid } = this.parseRawLink();
            const { name, bp } = await getRawBlueprint(guid);
            const copyable = new ClipboardItem({
                'text/plain': bp.text()
            });
            await navigator.clipboard.write([copyable]);
        });


        this.initApp();
    }

    private parseRawLink() {
        const href = this.showRaw.href.split('/');
        const game = href[href.length - 2];
        const guid = href[href.length - 1];
        return { game, guid };
    }

    initApp() {
        window.addEventListener('popstate', () => this.handleRouting());

        this.searchBar.addEventListener('input', () => {
            const query = this.searchBar.value.trim();
            if (!query) return;

            // if a request is running, just remember the latest query
            if (this.inFlight) {
                this.pendingQuery = query;
                return;
            }

            // otherwise start immediately
            this.runQuery(query);
        });

        this.searchBar.addEventListener('focus', () => {
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
            // Assume first part is game, second is guid
            this.loadAndShowBlueprint(path[0], {
                raw: query.get('raw') === 'true',
            });
        }
    }

    showSearch() {
        this.moveResultCursor(0);

        setVisible(this.blueprintView, false);
        setVisible(this.titleBar, false);
        setVisible(this.searchView, true);
        this.hideSearch.classList.remove('disabled');
    }

    showBlueprintView() {

        setVisible(this.blueprintView, true);
        setVisible(this.titleBar, true);
        setVisible(this.searchView, false);
        this.hideSearch.classList.add('disabled');
    }

    // Helper to change URL without reloading
    navigateTo(link: string, opts?: ViewOpts) {
        let url = link;
        if (opts?.raw === true) {
            url += '?raw=true';
        }
        history.pushState(null, "", url);
        this.handleRouting();
    }

    runQuery(query: string) {
        this.inFlight = this.findBlueprints(query)
            .then(data => this.renderResults(data))
            .finally(() => {
                this.inFlight = null;
                if (this.pendingQuery) {
                    const next = this.pendingQuery;
                    this.pendingQuery = null;
                    this.runQuery(next); // fire the latest queued query
                }
            });
    }

    async findBlueprints(query: string): Promise<BlueprintResult[]> {
        const response = await fetch(`${apiPrefix}/bp/find?query=${encodeURIComponent(query)}`);
        return await response.json();
    }

    moveResultCursor(dir: number) {
        const newCursor = this.resultCursor + dir;
        const tbody = this.resultsTable.querySelector("tbody");
        if (!tbody) return;
        if (newCursor < 0 || newCursor >= tbody.rows.length) return;

        this.activeRow?.classList.remove('cursor-active');
        this.activeRow = tbody.rows[newCursor];
        this.activeRow.classList.add('cursor-active');

        this.resultCursor = newCursor;

    }

    renderResults(data: BlueprintResult[]) {
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
                tr.classList.add('cursor-active');
            }

            const link = tr.firstElementChild as HTMLAnchorElement;
            link.addEventListener('click', e => this.handleLinkClick(e, href));

            tbody.appendChild(tr);
            index++;
        }
    }



    handleLinkClick(e: MouseEvent, link: string) {
        if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
            e.preventDefault();
            this.navigateTo(link);
        }
    }

    viewCallbacks: ViewCallbacks = {
        handleLinkClick: (e: MouseEvent, link: string) => this.handleLinkClick(e, link),
    };

    async loadAndShowBlueprint(guid: string, opts?: ViewOpts) {
        guid = normalizeGuid(guid);
        this.showRaw.href = `${apiPrefix}/bp/get/${guid}`;
        this.resultCursor = 0;
        this.activeRow?.classList?.remove('cursor-active');
        this.activeRow = null;

        this.showBlueprintView();

        const container = document.getElementById('bp-content') as HTMLDivElement;
        container.innerHTML = "Loading...";

        const resultCount = this.resultsTable.querySelector('tbody')?.rows.length;
        if (resultCount && resultCount > 0) {
            this.backBtn.disabled = false;
        } else {
            console.log('disabling');
            this.backBtn.disabled = true;
        }
        let dialogGuid = '';

        try {
            if (opts?.raw === true) {
                this.isShowingRaw = true;
                this.showRaw.classList.replace('toggle-inactive', 'toggle-active');
                const { name, bp } = await getRawBlueprint(guid);
                this.titleSpan.innerText = name || 'Blueprint';
                const json = await bp.json();
                container.innerHTML = `<div class="json-view">${JSON.stringify(json, null, 2)}</div>`;
                const type = json['$type'];
            } else {
                this.isShowingRaw = false;
                this.showRaw.classList.replace('toggle-active', 'toggle-inactive');
                const response = await viewBlueprint(guid);
                const obj = await response.json();
                const elements: DisplayableElement[] = obj.blueprint;

                // Assume the first element always contains the title info
                this.titleSpan.innerText = elements[0].key || "Blueprint";

                // Clear the container
                container.innerHTML = "";

                // Create the entire interactive view with one function call
                const blueprintDom = createBlueprintView(elements, apiPrefix, guid, this.viewCallbacks);

                const refs: { id: string, name: string }[] = obj.references;

                dialogGuid = obj.dialog;

                this.referencePanel.innerHTML = '<h4>References</h4>';
                for (const ref of refs) {
                    const a = document.createElement('a');
                    a.className = 'bp-link';
                    a.href = makeBlueprintLink(ref.id);
                    a.addEventListener('click', e => this.handleLinkClick(e, a.href));
                    a.textContent = ref.name;
                    this.referencePanel.appendChild(a);
                }

                // Append the result to the page
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

}

export { DialogPage } from './dialog';
export interface BlueprintResult {
    name: string;
    namespace: string;
    guidText: string;
    typeName: string;
}

export function makeBlueprintLink(guid: string) {
    return `${apiPrefix}/bp_${guid}`;
}

async function getRawBlueprint(guid: string) {
    const rawResponse = await fetch(`${apiPrefix}/bp/get/${guid}`);
    const name = rawResponse.headers.get('BP-Name');
    return { name, bp: rawResponse };
}
export async function viewBlueprint(guid: string) {
    return await fetch(`${apiPrefix}/bp/view/${guid}`);
}
export async function getBlueprint(guid: string, withStrings = false): Promise<any> {
    const stringQuery = `?strings=${withStrings}`;
    const response = await fetch(`${apiPrefix}/bp/get/${guid}${stringQuery}`);
    return await response.json();
}

export function normalizeGuid(guid: string) {
    return guid.replace('bp_', '');
}
interface ViewOpts {
    raw: boolean;
}
