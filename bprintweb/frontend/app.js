import { createBlueprintView } from "./treeView";
export class MainPage {
    constructor() {
        var _a;
        this.searchBar = document.getElementById('searchBox');
        this.resultsTable = document.getElementById('resultsTable');
        this.searchView = document.getElementById('searchView');
        this.blueprintView = document.getElementById('blueprintView');
        this.titleSpan = document.getElementById('currentTitle');
        this.titleBar = document.getElementById('title-bar');
        this.showRaw = document.getElementById('show-raw-link');
        this.copyRaw = document.getElementById('copy-raw');
        this.inFlight = null;
        this.pendingQuery = null;
        this.resultCursor = 0;
        this.activeRow = null;
        this.isShowingRaw = false;
        this.viewCallbacks = {
            handleLinkClick: (e, game, link) => this.handleLinkClick(e, game, link),
        };
        (_a = document.getElementById('backBtn')) === null || _a === void 0 ? void 0 : _a.addEventListener('click', () => {
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
                if (!tbody)
                    return;
                if (this.resultCursor < 0 || this.resultCursor > tbody.rows.length)
                    return;
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
        this.copyRaw.addEventListener('click', async (_) => {
            const { game, guid } = this.parseRawLink();
            const { name, bp } = await getRawBlueprint(game, guid);
            const copyable = new ClipboardItem({
                'text/plain': bp.text()
            });
            await navigator.clipboard.write([copyable]);
        });
        this.initApp();
    }
    parseRawLink() {
        const href = this.showRaw.href.split('/');
        const game = href[href.length - 2];
        const guid = href[href.length - 1];
        return { game, guid };
    }
    initApp() {
        window.addEventListener('popstate', () => this.handleRouting());
        this.searchBar.addEventListener('input', () => {
            const query = this.searchBar.value.trim();
            if (!query)
                return;
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
        }
        else {
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
    navigateTo(game, guid, opts) {
        let url = `/${game}/${guid}`;
        if ((opts === null || opts === void 0 ? void 0 : opts.raw) === true) {
            url += '?raw=true';
        }
        history.pushState(null, "", url);
        this.loadAndShowBlueprint(game, guid, opts);
    }
    runQuery(query) {
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
    async findBlueprints(game, query) {
        const response = await fetch(`/bp/find/${game}?query=${encodeURIComponent(query)}`);
        return await response.json();
    }
    moveResultCursor(dir) {
        var _a;
        const newCursor = this.resultCursor + dir;
        const tbody = this.resultsTable.querySelector("tbody");
        if (!tbody)
            return;
        if (newCursor < 0 || newCursor >= tbody.rows.length)
            return;
        (_a = this.activeRow) === null || _a === void 0 ? void 0 : _a.classList.remove('cursor-active');
        this.activeRow = tbody.rows[newCursor];
        this.activeRow.classList.add('cursor-active');
        this.resultCursor = newCursor;
    }
    renderResults(data) {
        if (!this.resultsTable)
            return;
        const tbody = this.resultsTable.querySelector("tbody");
        if (!tbody)
            return;
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
            const link = tr.firstElementChild;
            link.addEventListener('click', async (e) => {
                this.handleLinkClick(e, 'rt', row.guidText);
            });
            tbody.appendChild(tr);
            index++;
        }
    }
    handleLinkClick(e, game, link) {
        if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
            e.preventDefault();
            this.navigateTo(game, link);
        }
    }
    async loadAndShowBlueprint(game, guid, opts) {
        var _a, _b;
        this.showRaw.href = `/bp/get/${game}/${guid}`;
        this.resultCursor = 0;
        (_b = (_a = this.activeRow) === null || _a === void 0 ? void 0 : _a.classList) === null || _b === void 0 ? void 0 : _b.remove('cursor-active');
        this.activeRow = null;
        this.searchView.classList.add('hide');
        this.titleBar.classList.remove('hide');
        this.blueprintView.classList.remove('hide');
        const container = document.getElementById('bp-content');
        container.innerHTML = "Loading...";
        try {
            if ((opts === null || opts === void 0 ? void 0 : opts.raw) === true) {
                this.isShowingRaw = true;
                this.showRaw.classList.replace('toggle-inactive', 'toggle-active');
                const { name, bp } = await getRawBlueprint(game, guid);
                this.titleSpan.innerText = name || 'Blueprint';
                container.innerHTML = `<div class="json-view">${JSON.stringify(await bp.json(), null, 2)}</div>`;
            }
            else {
                this.isShowingRaw = false;
                this.showRaw.classList.replace('toggle-active', 'toggle-inactive');
                const response = await fetch(`/bp/view/${game}/${guid}`);
                const elements = await response.json(); // This is now a flat array
                // Assume the first element always contains the title info
                this.titleSpan.innerText = elements[0].key || "Blueprint";
                // Clear the container
                container.innerHTML = "";
                // Create the entire interactive view with one function call
                const blueprintDom = createBlueprintView(elements, game, guid, this.viewCallbacks);
                // Append the result to the page
                container.appendChild(blueprintDom);
            }
        }
        catch (err) {
            container.textContent = "Error: " + (err instanceof Error ? err.message : String(err));
        }
    }
}
export { DialogPage } from './dialog';
async function getRawBlueprint(game, guid) {
    const rawResponse = await fetch(`/bp/get/${game}/${guid}`);
    const name = rawResponse.headers.get('BP-Name');
    return { name, bp: rawResponse };
}
export async function getBlueprint(game, guid, withStrings = false) {
    const stringQuery = `?strings=${withStrings}`;
    const response = await fetch(`/bp/get/${game}/${guid}${stringQuery}`);
    return await response.json();
}
