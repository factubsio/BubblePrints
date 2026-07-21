import { Subprocess } from "bun";

interface WorkerResponse {
  id: number;
  result?: unknown;
  error?: string;
}

export class WorkerManager {
  private child: Subprocess<"pipe", "pipe", "pipe"> | null = null;
  private pending = new Map<number, {
    resolve: (response: WorkerResponse) => void;
    reject: (error: Error) => void;
  }>();
  private nextId = 1;
  private buffer = "";
  private path: string | null = null;
  private dead = false;

  constructor(private readonly workerBinary: string) {}

  get isRunning(): boolean {
    return this.child !== null && !this.dead;
  }

  get currentPath(): string | null {
    return this.path;
  }

  async open(path: string): Promise<WorkerResponse> {
    await this.close();

    this.path = path;
    this.dead = false;
    const command = this.workerBinary.endsWith(".dll")
      ? [process.env.DOTNET_HOST_PATH ?? "dotnet", this.workerBinary, path]
      : [this.workerBinary, path];
    const child = Bun.spawn(command, {
      stdin: "pipe",
      stdout: "pipe",
      stderr: "pipe",
    });
    this.child = child;

    void this.readLoop(child);
    void this.stderrLoop(child);
    void child.exited.then((code) => {
      if (this.child === child) this.onExit(code);
    });

    return this.request("ready", {});
  }

  async close(): Promise<void> {
    if (!this.child) return;

    const child = this.child;
    this.child = null;
    this.path = null;
    this.dead = false;
    this.buffer = "";

    for (const pending of this.pending.values()) {
      pending.reject(new Error("Worker closed."));
    }
    this.pending.clear();

    try {
      child.stdin.end();
      child.kill();
      await child.exited;
    } catch {
      // The process already exited.
    }
  }

  request(method: string, params: Record<string, unknown>): Promise<WorkerResponse> {
    if (!this.child || this.dead) {
      return Promise.resolve({ id: -1, error: "No binz is open. Call open_binz first." });
    }

    const id = this.nextId++;
    const line = JSON.stringify({ id, method, params }) + "\n";

    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      try {
        this.child!.stdin.write(line);
        this.child!.stdin.flush();
      } catch (error: unknown) {
        this.pending.delete(id);
        const message = error instanceof Error ? error.message : String(error);
        resolve({ id, error: `Failed to write to worker: ${message}` });
      }
    });
  }

  private onExit(code: number): void {
    this.dead = true;
    const error = new Error(`Worker exited with code ${code}.`);
    console.error(error.message);
    for (const pending of this.pending.values()) {
      pending.reject(error);
    }
    this.pending.clear();
  }

  private async readLoop(child: Subprocess<"pipe", "pipe", "pipe">): Promise<void> {
    const reader = child.stdout.getReader();
    const decoder = new TextDecoder();

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        this.buffer += decoder.decode(value, { stream: true });

        let newline: number;
        while ((newline = this.buffer.indexOf("\n")) >= 0) {
          const line = this.buffer.slice(0, newline).trim();
          this.buffer = this.buffer.slice(newline + 1);
          if (!line) continue;

          try {
            const response = JSON.parse(line) as WorkerResponse;
            const pending = this.pending.get(response.id);
            if (pending) {
              this.pending.delete(response.id);
              pending.resolve(response);
            }
          } catch {
            console.error(`Ignoring malformed worker response: ${line}`);
          }
        }
      }
    } catch {
      // Stream closed with the worker.
    }
  }

  private async stderrLoop(child: Subprocess<"pipe", "pipe", "pipe">): Promise<void> {
    const reader = child.stderr.getReader();
    const decoder = new TextDecoder();
    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        console.error("[bubbleprint-worker] " + decoder.decode(value));
      }
    } catch {
      // Stream closed with the worker.
    }
  }
}
