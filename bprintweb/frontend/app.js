var _a;
import { createBlueprintView } from "./treeView";
const searchBar = document.getElementById('searchBox');
const resultsTable = document.getElementById('resultsTable');
const searchView = document.getElementById('searchView');
const blueprintView = document.getElementById('blueprintView');
const titleSpan = document.getElementById('currentTitle');
let inFlight = null;
let pendingQuery = null;
(_a = document.getElementById('backBtn')) === null || _a === void 0 ? void 0 : _a.addEventListener('click', () => {
    history.pushState(null, "", "/"); // Go to root
    showSearch();
});
export function initApp() {
    window.addEventListener('popstate', handleRouting);
    searchBar.addEventListener('input', () => {
        const query = searchBar.value.trim();
        if (!query)
            return;
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
        titleSpan.classList.add('hide');
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
    }
    else {
        showSearch();
    }
}
function showSearch() {
    blueprintView.classList.add('hide');
    titleSpan.classList.add('hide');
    searchView.classList.remove('hide');
}
// Helper to change URL without reloading
function navigateTo(game, guid) {
    const url = `/${game}/${guid}`;
    history.pushState(null, "", url);
    loadAndShowBlueprint(game, guid);
}
function runQuery(query) {
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
async function findBlueprints(game, query) {
    const response = await fetch(`/bp/find/${game}?query=${encodeURIComponent(query)}`);
    return await response.json();
}
async function getBlueprint(game, guid) {
    const response = await fetch(`/bp/get/${game}/${guid}`);
    return await response.text();
}
function renderResults(data) {
    if (!resultsTable)
        return;
    const tbody = resultsTable.querySelector("tbody");
    if (!tbody)
        return;
    tbody.innerHTML = "";
    for (const row of data) {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><a href="/rt/${row.guidText}">${row.name}</a></td>
            <td class="col-priority-2">${row.namespace}</td>
            <td class="col-priority-3">${row.guidText}</td>
            <td class="col-priority-1">${row.typeForResults}</td>
        `;
        const link = tr.firstElementChild;
        link.addEventListener('click', async (e) => {
            handleLinkClick(e, 'rt', row.guidText);
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
const viewCallbacks = {
    handleLinkClick,
};
async function loadAndShowBlueprint(game, guid) {
    searchView.classList.add('hide');
    titleSpan.classList.remove('hide');
    blueprintView.classList.remove('hide');
    const container = document.getElementById('bp-content');
    container.innerHTML = "Loading...";
    try {
        const response = await fetch(`/bp/view/${game}/${guid}`);
        const elements = await response.json(); // This is now a flat array
        // Assume the first element always contains the title info
        titleSpan.innerText = elements[0].key || "Blueprint";
        // Clear the container
        container.innerHTML = "";
        // Create the entire interactive view with one function call
        const blueprintDom = createBlueprintView(elements, game, guid, viewCallbacks);
        // Append the result to the page
        container.appendChild(blueprintDom);
    }
    catch (err) {
        container.textContent = "Error: " + (err instanceof Error ? err.message : String(err));
    }
}
