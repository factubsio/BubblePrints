/**
 * Builds a nested HTMLDivElement tree directly from a flat list of elements,
 * with collapsible state persisted in localStorage.
 *
 * @param flatElements The flat array of data from the server.
 * @param game The name of the game, used for generating links.
 * @param guid The GUID of the blueprint being displayed, for unique localStorage key.
 * @param cb An object containing callback functions, like handleLinkClick.
 * @returns An HTMLDivElement containing the complete, interactive blueprint view.
 */
export function createBlueprintView(flatElements, game, guid, cb) {
    var _a;
    const rootContainer = document.createElement('div');
    const parentContainerStack = [rootContainer];
    const localStorageKey = `bp-state-${guid}`;
    // Load state - no change here, this still works perfectly.
    const savedState = JSON.parse(localStorage.getItem(localStorageKey) || '{}');
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
        const currentPath = [...pathStack, element.key].join('/');
        const nodeWrapper = document.createElement('div');
        nodeWrapper.className = 'bp-node';
        const row = document.createElement('div');
        row.className = 'bp-row';
        const childrenContainer = document.createElement('div');
        childrenContainer.className = 'bp-children-container';
        const hasChildren = element.levelDelta > 0;
        let toggle;
        if (hasChildren) {
            const childrenPresent = nextElement && nextElement.levelDelta != -1;
            toggle = document.createElement('span');
            toggle.className = 'bp-toggle';
            if (!childrenPresent) {
                toggle.classList.add("disabled");
            }
            // Reading the state: If a path isn't in savedState, it will be undefined.
            // `undefined ?? false` correctly results in `false` (expanded).
            const isInitiallyCollapsed = (_a = savedState[currentPath]) !== null && _a !== void 0 ? _a : false;
            if (isInitiallyCollapsed) {
                childrenContainer.classList.add('hide');
                toggle.textContent = '►';
            }
            else {
                toggle.textContent = '▼';
            }
            if (childrenPresent) {
                toggle.onclick = () => {
                    const isHidden = childrenContainer.classList.toggle('hide');
                    toggle.textContent = isHidden ? '►' : '▼';
                    // --- THE OPTIMIZATION IS HERE ---
                    if (isHidden) {
                        // It's collapsed (non-default state), so we ADD it to our state object.
                        savedState[currentPath] = true;
                    }
                    else {
                        // It's expanded (the default state), so we REMOVE it to save space.
                        delete savedState[currentPath];
                    }
                    // If the state object is now empty, we can remove the entire key.
                    if (Object.keys(savedState).length === 0) {
                        localStorage.removeItem(localStorageKey);
                    }
                    else {
                        localStorage.setItem(localStorageKey, JSON.stringify(savedState));
                    }
                    // --- END OF OPTIMIZATION ---
                };
            }
            row.appendChild(toggle);
        }
        else {
            // ... placeholder logic ...
            const placeholder = document.createElement('span');
            placeholder.className = 'bp-toggle-placeholder';
            row.appendChild(placeholder);
        }
        // ... rest of the row creation (key, value, type) remains identical ...
        const keySpan = document.createElement('span');
        keySpan.className = 'bp-key';
        keySpan.textContent = element.key;
        row.appendChild(keySpan);
        if (element.value) {
            let valEl;
            if (element.link) {
                const linkEl = document.createElement('a');
                linkEl.className = 'bp-link';
                linkEl.href = `/${game}/${element.link}`;
                linkEl.textContent = element.value;
                linkEl.onclick = evt => cb.handleLinkClick(evt, game, element.link);
                valEl = linkEl;
            }
            else {
                const valSpan = document.createElement('span');
                valSpan.className = 'bp-val';
                valSpan.textContent = element.value;
                valEl = valSpan;
            }
            row.appendChild(valEl);
        }
        if (element.isObj && element.typeName) {
            const typeSpan = document.createElement('span');
            typeSpan.className = 'bp-type';
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
