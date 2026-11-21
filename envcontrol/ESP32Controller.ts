import { EventEmitter } from "node:events";

interface Backend {
    readonly isOpen: boolean;
    write(data: string): Promise<void> | void;
    close(): Promise<void> | void;
}

export class ESP32Controller extends EventEmitter {
    private backend?: Backend;
    private buffer = "";

    constructor(
        private portName: string,
        private baudRate = 115200,
    ) {
        super();
    }

    private onChunk(data: Buffer | string): void {
        this.buffer += data.toString();

        // Processar mensagens completas
        const messages = this.buffer.split("\n");
        this.buffer = messages.pop() ?? "";

        for (const raw of messages) {
            const message = raw.trim();
            if (message.length > 0) this.onReceiver(message);
        }
    }

    // Callback para receber mensagens
    private onReceiver(message: string): void {
        this.emit("message", message);
        this.emit("data", message);
    }

    async connect(): Promise<void> {
        if (process.platform === "win32") {
            // Windows: API serial nativa via bun:ffi (funciona tambem no .exe compilado)
            const { WinSerial } = await import("./WinSerial");
            const serial = new WinSerial(
                this.portName,
                this.baudRate,
                (chunk) => this.onChunk(chunk),
                (err) => this.emit("error", err),
            );
            serial.open(); // lanca erro se nao abrir
            this.backend = serial;
        } else {
            // Linux/macOS/WSL: usa o modulo serialport (import dinamico, fora do .exe)
            const moduleName = "serialport";
            const { SerialPort } = await import(moduleName);
            const port = new SerialPort({
                path: this.portName,
                baudRate: this.baudRate,
                hupcl: false,
                autoOpen: false,
            });
            port.on("data", (c: Buffer) => this.onChunk(c));
            port.on("error", (e: Error) => this.emit("error", e));

            await new Promise<void>((resolve, reject) =>
                port.open((e: Error | null) => (e ? reject(e) : resolve())),
            );
            await new Promise<void>((resolve) => port.set({ dtr: false, rts: false }, () => resolve()));

            this.backend = {
                get isOpen() {
                    return port.isOpen as boolean;
                },
                write: (data: string) =>
                    new Promise<void>((resolve, reject) =>
                        port.write(data, (e: Error | null) =>
                            e ? reject(e) : port.drain((e2: Error | null) => (e2 ? reject(e2) : resolve())),
                        ),
                    ),
                close: () => new Promise<void>((resolve) => port.close(() => resolve())),
            };
        }

        this.emit("connected");
    }

    async send(command: string): Promise<void> {
        if (!this.backend?.isOpen) throw new Error("ESP32 não conectado");
        await this.backend.write(`${command}\n`);
    }

    async disconnect(): Promise<void> {
        if (!this.backend) return;
        await this.backend.close();
        this.backend = undefined;
        this.emit("disconnected");
    }
}
