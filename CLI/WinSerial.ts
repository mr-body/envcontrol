import { dlopen, FFIType } from "bun:ffi";

// Acesso direto a porta serial do Windows (kernel32), sem modulo nativo.
const { u32, u64, ptr, i32 } = FFIType;

const k32 = dlopen("kernel32.dll", {
    CreateFileA: { args: [ptr, u32, u32, ptr, u32, u32, u64], returns: u64 },
    CloseHandle: { args: [u64], returns: i32 },
    GetCommState: { args: [u64, ptr], returns: i32 },
    SetCommState: { args: [u64, ptr], returns: i32 },
    SetCommTimeouts: { args: [u64, ptr], returns: i32 },
    ReadFile: { args: [u64, ptr, u32, ptr, ptr], returns: i32 },
    WriteFile: { args: [u64, ptr, u32, ptr, ptr], returns: i32 },
    EscapeCommFunction: { args: [u64, u32], returns: i32 },
    PurgeComm: { args: [u64, u32], returns: i32 },
    GetLastError: { args: [], returns: u32 },
}).symbols;

const GENERIC_READ_WRITE = 0xc0000000;
const OPEN_EXISTING = 3;
const INVALID_HANDLE = 0xffffffffffffffffn;
const SETRTS = 3, CLRRTS = 4, SETDTR = 5, CLRDTR = 6;
const DEBUG = process.env.ENVCONTROL_DEBUG === "1";
let dbgCount = 0;

function winError(code: number, port: string): Error {
    if (code === 2) return new Error(`A porta ${port} nao existe (use 'envcontrol ports')`);
    if (code === 5) return new Error(`Acesso negado em ${port}: a porta esta em uso por outro programa`);
    return new Error(`Falha na porta ${port} (codigo Win32 ${code})`);
}

export class WinSerial {
    isOpen = false;
    private handle = 0n;
    private timer?: ReturnType<typeof setInterval>;
    private readBuf = new Uint8Array(4096);
    private nRead = new Uint32Array(1);
    private nWritten = new Uint32Array(1);

    constructor(
        private port: string,
        private baud: number,
        private onData: (chunk: Buffer) => void,
        private onError: (e: Error) => void,
    ) {}

    open(): void {
        const name = Buffer.from(`\\\\.\\${this.port}\0`, "latin1"); // suporta COM10+
        const h = BigInt(k32.CreateFileA(name, GENERIC_READ_WRITE, 0, null, OPEN_EXISTING, 0, 0n));
        if (h === INVALID_HANDLE || h === 0n) throw winError(k32.GetLastError(), this.port);
        this.handle = h;

        try {
            const dcb = new Uint8Array(28);
            const dv = new DataView(dcb.buffer);
            dv.setUint32(0, 28, true);
            if (!k32.GetCommState(h, dcb)) throw winError(k32.GetLastError(), this.port);
            dv.setUint32(4, this.baud, true);
            dv.setUint32(8, 1, true); // fBinary=1, sem controle de fluxo, DTR/RTS desligados
            dcb[18] = 8; // 8 bits
            dcb[19] = 0; // sem paridade
            dcb[20] = 0; // 1 stop bit
            if (!k32.SetCommState(h, dcb)) throw winError(k32.GetLastError(), this.port);

            // leitura nao bloqueante: ReadFile volta na hora com o que houver
            const timeouts = new Uint32Array([0xffffffff, 0, 0, 0, 1000]);
            k32.SetCommTimeouts(h, timeouts);

            // DTR/RTS desligados (evita reset da placa). ENVCONTROL_DTR=1 liga o DTR
            // (necessario em placas com USB nativo: ESP32-S2/S3/C3).
            // Ordem importa no circuito de auto-reset (DTR->GPIO0, RTS->EN):
            // soltar o RTS primeiro e o DTR depois evita o pulso de reset.
            k32.EscapeCommFunction(h, process.env.ENVCONTROL_RTS === "1" ? SETRTS : CLRRTS);
            k32.EscapeCommFunction(h, process.env.ENVCONTROL_DTR === "1" ? SETDTR : CLRDTR);

            k32.PurgeComm(h, 0x0f);
        } catch (e) {
            k32.CloseHandle(h);
            this.handle = 0n;
            throw e;
        }

        this.isOpen = true;
        this.timer = setInterval(() => this.poll(), 15);
    }

    private poll(): void {
        if (!this.isOpen) return;
        for (;;) {
            this.nRead[0] = 0;
            const ok = k32.ReadFile(this.handle, this.readBuf, this.readBuf.length, this.nRead, null);
            if (!ok) {
                this.onError(winError(k32.GetLastError(), this.port));
                this.close();
                return;
            }
            const n = this.nRead[0]!;
            if (n === 0) return;
            if (DEBUG && dbgCount++ < 15) console.error(`[dbg] read n=${n} t=${Date.now() % 100000}`);
            this.onData(Buffer.from(this.readBuf.subarray(0, n)));
            if (n < this.readBuf.length) return;
        }
    }

    write(data: string): void {
        if (!this.isOpen) throw new Error("ESP32 nao conectado");
        const buf = Buffer.from(data, "utf8");
        if (DEBUG) console.error(`[dbg] write ${JSON.stringify(data)} t=${Date.now() % 100000}`);
        this.nWritten[0] = 0;
        const ok = k32.WriteFile(this.handle, buf, buf.length, this.nWritten, null);
        if (!ok || this.nWritten[0] !== buf.length) throw winError(k32.GetLastError(), this.port);
    }

    close(): void {
        if (this.timer) clearInterval(this.timer);
        this.timer = undefined;
        if (this.isOpen) {
            this.isOpen = false;
            k32.CloseHandle(this.handle);
            this.handle = 0n;
        }
    }
}
