import { DisplayableElement, ViewCallbacks, createBlueprintView } from "./treeView";

const searchBar = document.getElementById('searchBox') as HTMLInputElement;
const resultsTable = document.getElementById('resultsTable') as HTMLTableElement;

const searchView = document.getElementById('searchView') as HTMLDivElement;
const blueprintView = document.getElementById('blueprintView') as HTMLDivElement;

const titleSpan = document.getElementById('currentTitle') as HTMLSpanElement;

let inFlight: Promise<any> | null = null;
let pendingQuery: string | null = null;

document.getElementById('backBtn')?.addEventListener('click', () => {
    history.pushState(null, "", "/"); // Go to root
    showSearch();
});

export function initApp() {
    window.addEventListener('popstate', handleRouting);

    searchBar.addEventListener('input', () => {
        const query = searchBar.value.trim();
        if (!query) return;

        // if a request is running, just remember the latest query
        if (inFlight) {
            pendingQuery = query;
            return;
        }

        // otherwise start immediately
        runQuery(query);
    });

    searchBar.addEventListener('focus', () => {
        // Switch immediately to search view
        searchView.classList.remove('hide');
        blueprintView.classList.add('hide');
    });

    handleRouting();
}

function handleRouting() {
    const path = window.location.pathname; // e.g. "/rt/b602..."
    const parts = path.split('/').filter(p => p); // ["rt", "b602..."]

    if (parts.length === 2) {
        // Assume first part is game, second is guid
        loadAndShowBlueprint(parts[0], parts[1]);
    } else {
        showSearch();
    }
}

function showSearch() {
    blueprintView.classList.add('hide');
    searchView.classList.remove('hide');
}

// Helper to change URL without reloading
function navigateTo(game: string, guid: string) {
    const url = `/${game}/${guid}`;
    history.pushState(null, "", url);
    loadAndShowBlueprint(game, guid);
}

function runQuery(query: string) {
    inFlight = findBlueprints("rt", query)
        .then(renderResults)
        .finally(() => {
            inFlight = null;
            if (pendingQuery) {
                const next = pendingQuery;
                pendingQuery = null;
                runQuery(next); // fire the latest queued query
            }
        });
}

async function findBlueprints(game: string, query: string): Promise<BlueprintResult[]> {
    const response = await fetch(`/bp/find/${game}?query=${encodeURIComponent(query)}`);
    return await response.json();
}

async function getBlueprint(game: string, guid: string): Promise<string> {
    const response = await fetch(`/bp/get/${game}/${guid}`);
    return await response.text();
}

function renderResults(data: BlueprintResult[]) {
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

        const link = tr.firstElementChild as HTMLLinkElement;
        link.addEventListener('click', async e => {
            handleLinkClick(e, 'rt', row.guidText);
        }); 

        tbody.appendChild(tr);
    }
}
export interface BlueprintResult {
    name: string;
    namespace: string;
    guidText: string;
    typeForResults: string;
}


export interface Blueprint {

}


function handleLinkClick(e: MouseEvent, game: string, link: string) {
    if (e.button == 0 && !e.ctrlKey && !e.shiftKey) {
        e.preventDefault();
        navigateTo(game, link);
    }
}

const viewCallbacks: ViewCallbacks = {
    handleLinkClick,
};

async function loadAndShowBlueprint(game: string, guid: string) {
    searchView.classList.add('hide');
    blueprintView.classList.remove('hide');
    const container = document.getElementById('bp-content') as HTMLDivElement;
    container.innerHTML = "Loading...";

    try {
        const response = await fetch(`/bp/view/${game}/${guid}`);
        const elements: DisplayableElement[] = await response.json(); // This is now a flat array

        // Assume the first element always contains the title info
        titleSpan.innerText = elements[0].key || "Blueprint"; 

        // Clear the container
        container.innerHTML = "";
        
        // Create the entire interactive view with one function call
        const blueprintDom = createBlueprintView(elements, game, guid, viewCallbacks);
        
        // Append the result to the page
        container.appendChild(blueprintDom);

    } catch (err) {
        container.textContent = "Error: " + (err instanceof Error ? err.message : String(err));
    }
}


