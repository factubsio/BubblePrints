import { DisplayableElement, ViewCallbacks, createBlueprintView } from "./treeView";

const classToggleInactive = 'toggle-inactive';
const classToggleActive = 'toggle-active';
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

    titleSpan = document.getElementById('currentTitle') as HTMLSpanElement;
    titleBar = document.getElementById('title-bar') as HTMLDivElement;

    backBtn = document.getElementById('backBtn') as HTMLButtonElement;

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
            if (e.key == 'Enter') {
                const tbody = this.resultsTable.querySelector("tbody");
                if (!tbody) return;
                if (this.resultCursor < 0 || this.resultCursor > tbody.rows.length) return;
                const guid = tbody.rows[this.resultCursor].cells[3].textContent;
                e.preventDefault();
                this.searchBar.blur();
                if (guid && guid !== '') {
                    this.navigateTo('rt', guid);
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
                this.navigateTo(game, guid, { raw: showRaw });
                e.preventDefault();
            }
        });

        this.copyRaw.addEventListener('click', async _ => {
            const { game, guid } = this.parseRawLink();
            const { name, bp } = await getRawBlueprint(game, guid);
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
        const path = window.location.pathname; // e.g. "/rt/b602..."
        const parts = path.split('/').filter(p => p); // ["rt", "b602..."]

        const query = new URLSearchParams(window.location.search);

        if (parts.length === 2) {
            // Assume first part is game, second is guid
            this.loadAndShowBlueprint(parts[0], parts[1], {
                raw: query.get('raw') === 'true',
            });
        } else {
            this.showSearch();
        }
    }

    showSearch() {
        this.moveResultCursor(0);
        this.blueprintView.classList.add('hide');
        this.titleBar.classList.add('hide');
        this.searchView.classList.remove('hide');
    }

    // Helper to change URL without reloading
    navigateTo(game: string, guid: string, opts?: ViewOpts) {
        let url = `/${game}/${guid}`;
        if (opts?.raw === true) {
            url += '?raw=true';
        }
        history.pushState(null, "", url);
        this.loadAndShowBlueprint(game, guid, opts);
    }

    runQuery(query: string) {
        this.inFlight = this.findBlueprints("rt", query)
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

    async findBlueprints(game: string, query: string): Promise<BlueprintResult[]> {
        const response = await fetch(`/bp/find/${game}?query=${encodeURIComponent(query)}`);
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

            tr.innerHTML = `
            <td><a href="/rt/${row.guidText}">${row.name}</a></td>
            <td class="col-priority-1">${row.typeName}</td>
            <td class="col-priority-2">${row.namespace}</td>
            <td class="col-priority-3">${row.guidText}</td>
        `;

            if (active) {
                this.activeRow = tr;
                tr.classList.add('cursor-active');
            }

            const link = tr.firstElementChild as HTMLLinkElement;
            link.addEventListener('click', async e => {
                this.handleLinkClick(e, 'rt', row.guidText);
            });

            tbody.appendChild(tr);
            index++;
        }
    }



    handleLinkClick(e: MouseEvent, game: string, link: string) {
        if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
            e.preventDefault();
            this.navigateTo(game, link);
        }
    }

    viewCallbacks: ViewCallbacks = {
        handleLinkClick: (e: MouseEvent, game: string, link: string) => this.handleLinkClick(e, game, link),
    };

    async loadAndShowBlueprint(game: string, guid: string, opts?: ViewOpts) {
        this.showRaw.href = `/bp/get/${game}/${guid}`;
        this.resultCursor = 0;
        this.activeRow?.classList?.remove('cursor-active');
        this.activeRow = null;

        this.searchView.classList.add('hide');
        this.titleBar.classList.remove('hide');
        this.blueprintView.classList.remove('hide');
        const container = document.getElementById('bp-content') as HTMLDivElement;
        container.innerHTML = "Loading...";

        const resultCount = this.resultsTable.querySelector('tbody')?.rows.length;
        if (resultCount && resultCount > 0) {
            this.backBtn.disabled = false;
        } else {
            console.log('disabling');
            this.backBtn.disabled = true;
        }


        try {
            if (opts?.raw === true) {
                this.isShowingRaw = true;
                this.showRaw.classList.replace('toggle-inactive', 'toggle-active');
                const { name, bp } = await getRawBlueprint(game, guid);
                this.titleSpan.innerText = name || 'Blueprint';
                container.innerHTML = `<div class="json-view">${JSON.stringify(await bp.json(), null, 2)}</div>`;
            } else {
                this.isShowingRaw = false;
                this.showRaw.classList.replace('toggle-active', 'toggle-inactive');
                const response = await fetch(`/bp/view/${game}/${guid}`);
                const obj = await response.json();
                const elements: DisplayableElement[] = obj.blueprint;

                // Assume the first element always contains the title info
                this.titleSpan.innerText = elements[0].key || "Blueprint";

                // Clear the container
                container.innerHTML = "";

                // Create the entire interactive view with one function call
                const blueprintDom = createBlueprintView(elements, game, guid, this.viewCallbacks);

                const refs: { id: string, name: string }[] = obj.references;

                this.referencePanel.innerHTML = '<h4>References</h4>';
                for (const ref of refs) {
                    const a = document.createElement('a');
                    a.className = 'bp-link';
                    a.href = `/${game}/${ref.id}`;
                    a.addEventListener('click', e => this.handleLinkClick(e, game, ref.id));
                    a.textContent = ref.name;
                    this.referencePanel.appendChild(a);
                }

                // Append the result to the page
                container.appendChild(blueprintDom);
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
export interface Blueprint {

}


async function getRawBlueprint(game: string, guid: string) {
    const rawResponse = await fetch(`/bp/get/${game}/${guid}`);
    const name = rawResponse.headers.get('BP-Name');
    return { name, bp: rawResponse };
}

export async function getBlueprint(game: string, guid: string, withStrings = false): Promise<any> {
    const stringQuery = `?strings=${withStrings}`;
    const response = await fetch(`/bp/get/${game}/${guid}${stringQuery}`);
    return await response.json();
}
interface ViewOpts {
    raw: boolean;
}
