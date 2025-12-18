import { getBlueprint } from './app'
export class DialogPage {
    game = 'rt';

    speakerEl = document.getElementById('cue-speaker-name')!;
    textEl = document.getElementById('cue-text')!;
    proceedTitleEl = document.getElementById('answers-title')!;
    proceedEl = document.getElementById('cue-answers')!;
    commentEl = document.getElementById('dev-comment')!;
    idEl = document.getElementById('current-id')!;

    constructor() {
        this.handleCue('4345a7ac05af431298cc6a3e5b3f8c6b');
    }

    cueCache = new Map<string, { raw: BlueprintWithStrings, obj: Cue }>();
    answerCache = new Map<string, { raw: BlueprintWithStrings, obj: Answer }>();

    async getCue(id: string) {
        let cue = this.cueCache.get(id);
        if (!cue) {
            const raw = await getBlueprint(this.game, id, true) as BlueprintWithStrings;
            const { root, strs } = raw;

            const text = getText(root.Text, strs) ?? "<unknown>";

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
    async handleCue(cueId: string): Promise<void> {
        this.idEl.textContent = cueId;

        const cue = await this.getCue(cueId);
        if (!cue) return;

        this.speakerEl.textContent = cue.raw.root.Speaker.m_Blueprint || "Narrator";
        this.textEl.textContent = cue.obj.text;
        this.commentEl.textContent = cue.raw.root.Comment || "[No developer comment]";

        this.proceedEl.innerHTML = '';

        if (cue.obj.answers.length > 0) {
            this.proceedTitleEl.textContent = 'Answers';
            for (const a of cue.obj.answers) {
                const answerItem = document.createElement('li');
                answerItem.textContent = a.text;
                answerItem.addEventListener('click', async _ => {
                    this.handleAnswer(a);
                });
                this.proceedEl.appendChild(answerItem);
            }
        } else if (cue.obj.continueCues.length > 0) {
            this.proceedTitleEl.textContent = '...';
            for (const nextCue of cue.obj.continueCues) {
                const cueItem = document.createElement('li');
                cueItem.textContent = nextCue.text;
                cueItem.addEventListener('click', async _ => {
                    this.handleCue(nextCue.id);
                });
                this.proceedEl.appendChild(cueItem);
            }
        } else {
            this.proceedTitleEl.textContent = 'unknown!';
        }


    }

    async handleAnswer(answer: Answer) {
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
            cueItem.addEventListener('click', async _ => {
                this.handleCue(cue.id);
            });
            this.proceedEl.appendChild(cueItem);
        }
    }

    private async spreadAnswers(answerId: string, answers: Answer[]) {
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
        } else {
            const nextCues: string[] = [];

            for (const cueId of root.NextCue.Cues) {
                nextCues.push(getGuid(cueId));
            }

            answer = {
                obj: {
                    id: guid,
                    text: getText(root.Text, strs) ?? "<unknown>",
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
