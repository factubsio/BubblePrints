import { getBlueprint } from './app';
export class DialogPage {
    constructor() {
        this.game = 'rt';
        this.speakerEl = document.getElementById('cue-speaker-name');
        this.textEl = document.getElementById('cue-text');
        this.proceedTitleEl = document.getElementById('answers-title');
        this.proceedEl = document.getElementById('cue-answers');
        this.commentEl = document.getElementById('dev-comment');
        this.idEl = document.getElementById('current-id');
        this.cueCache = new Map();
        this.answerCache = new Map();
        this.handleCue('4345a7ac05af431298cc6a3e5b3f8c6b');
    }
    async getCue(id) {
        var _a, _b;
        let cue = this.cueCache.get(id);
        if (!cue) {
            const raw = await getBlueprint(this.game, id, true);
            const { root, strs } = raw;
            const text = (_a = getText(root.Text, strs)) !== null && _a !== void 0 ? _a : "<unknown>";
            cue = {
                obj: {
                    id,
                    text,
                    answers: [],
                    continueCues: [],
                },
                raw,
            };
            this.cueCache.set(id, cue);
            for (const answerId of root.Answers) {
                await this.spreadAnswers(answerId, cue.obj.answers);
            }
            for (const cueId of (_b = root.Continue) === null || _b === void 0 ? void 0 : _b.Cues) {
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
        if (!cue)
            return;
        this.speakerEl.textContent = cue.raw.root.Speaker.m_Blueprint || "Narrator";
        this.textEl.textContent = cue.obj.text;
        this.commentEl.textContent = cue.raw.root.Comment || "[No developer comment]";
        this.proceedEl.innerHTML = '';
        if (cue.obj.answers.length > 0) {
            this.proceedTitleEl.textContent = 'Answers';
            for (const a of cue.obj.answers) {
                const answerItem = document.createElement('li');
                answerItem.textContent = a.text;
                answerItem.addEventListener('click', async (_) => {
                    this.handleAnswer(a);
                });
                this.proceedEl.appendChild(answerItem);
            }
        }
        else if (cue.obj.continueCues.length > 0) {
            this.proceedTitleEl.textContent = '...';
            for (const nextCue of cue.obj.continueCues) {
                const cueItem = document.createElement('li');
                cueItem.textContent = nextCue.text;
                cueItem.addEventListener('click', async (_) => {
                    this.handleCue(nextCue.id);
                });
                this.proceedEl.appendChild(cueItem);
            }
        }
        else {
            this.proceedTitleEl.textContent = 'unknown!';
        }
    }
    async handleAnswer(answer) {
        this.idEl.textContent = answer.id;
        this.speakerEl.textContent = 'YOU';
        // Text: We don't have the text, so we show the reference key.
        this.textEl.textContent = answer.text;
        // Comment: Display the developer comment if it exists.
        this.commentEl.textContent = '';
        this.proceedTitleEl.textContent = 'Next Cues';
        this.proceedEl.textContent = '';
        for (const cueId of answer.nextCues) {
            const cue = await this.getCue(cueId);
            answer.cues.push(cue.obj);
        }
        for (const cue of answer.cues) {
            const cueItem = document.createElement('li');
            cueItem.textContent = cue.text;
            cueItem.addEventListener('click', async (_) => {
                this.handleCue(cue.id);
            });
            this.proceedEl.appendChild(cueItem);
        }
    }
    async spreadAnswers(answerId, answers) {
        var _a;
        const guid = getGuid(answerId);
        let answer = this.answerCache.get(guid);
        if (answer) {
            answers.push(answer.obj);
            return;
        }
        const raw = await getBlueprint(this.game, guid, true);
        const { root, strs } = raw;
        if (root.$type.endsWith('BlueprintAnswersList')) {
            for (const answerId of root.Answers) {
                await this.spreadAnswers(answerId, answers);
            }
        }
        else {
            const nextCues = [];
            for (const cueId of root.NextCue.Cues) {
                nextCues.push(getGuid(cueId));
            }
            answer = {
                obj: {
                    id: guid,
                    text: (_a = getText(root.Text, strs)) !== null && _a !== void 0 ? _a : "<unknown>",
                    nextCues,
                    cues: [],
                },
                raw,
            };
            this.answerCache.set(guid, answer);
            answers.push(answer.obj);
        }
        //answers.appendChild(answerItem);
    }
}
function getGuid(raw) {
    return raw.replace('!bp_', '');
}
function getText(textNode, strings) {
    if (textNode.m_Key && textNode.m_Key !== '') {
        return strings[textNode.m_Key];
    }
    else if (textNode.Shared) {
        return strings[textNode.Shared.stringkey];
    }
    return undefined;
}
