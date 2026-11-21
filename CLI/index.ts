#!/usr/bin/env bun
import net from "node:net";
import { createInterface } from "node:readline";
import { ESP32Controller } from "./ESP32Controller";
import { listSerialPorts, pickEsp } from "./ports";

const IDLE_MS = 600; // sem dados por este tempo => resposta terminou
const DEFAULT_TIMEOUT_MS = 3000;
const FALLBACK_PORT = "COM7";

const HELP = `
envcontrol - CLI do ESP32 EnvControl

SERIAL (comando unico):
  envcontrol ping
  envcontrol on
  envcontrol off
  envcontrol status
  envcontrol config
  envcontrol restart
  envcontrol wifi on | off | reconnect
  envcontrol wifi set <SSID> [SENHA]
  envcontrol <qualquer outro comando do firmware>

SERIAL (shell interativo):
  envcontrol shell

TCP/IP (loop interativo estilo nc):
  envcontrol <IP> <PORTA>        ex.: envcontrol 192.168.1.50 8080

Outros:
  envcontrol ports               lista as portas seriais

Opcoes:
  -p, --port <COMx>              porta serial (ou variavel ENVCONTROL_PORT)
  -t, --timeout <ms>             espera maxima pela resposta (padrao ${DEFAULT_TIMEOUT_MS})
  -h, --help

Variaveis: ENVCONTROL_DTR=1 liga o DTR (placas com USB nativo: ESP32-S2/S3/C3)
`;

// ----------------------------------------------------------------- utils ---
function parseArgs(argv: string[]) {
    let port = process.env.ENVCONTROL_PORT;
    let timeout = DEFAULT_TIMEOUT_MS;
    const rest: string[] = [];

    for (let i = 0; i < argv.length; i++) {
        const a = argv[i]!;
        if (a === "-p" || a === "--port") port = argv[++i];
        else if (a === "-t" || a === "--timeout") timeout = Number(argv[++i]) || timeout;
        else if (a === "-h" || a === "--help") rest.unshift("--help");
        else rest.push(a);
    }
    return { port, timeout, rest };
}

/** Converte argumentos amigaveis em comandos do firmware. */
function translate(args: string[]): string {
    const low = args.map((a) => a.toLowerCase());
    if (low[0] === "wifi" && args.length >= 2) {
        const sub = low[1];
        if (sub === "on") return "WIFI ON";
        if (sub === "off") return "WIFI OFF";
        if (sub === "reconnect" || sub === "reconect") return "WIFI RECONNECT";
        if (sub === "set" && args.length >= 3) return "SET WIFI " + args.slice(2).join(" ");
    }
    return args.join(" ");
}

function isTcpMode(rest: string[]): boolean {
    if (rest.length !== 2) return false;
    const [host, p] = rest as [string, string];
    if (["wifi", "set"].includes(host.toLowerCase())) return false;
    const n = Number(p);
    return /^\d+$/.test(p) && n > 0 && n < 65536;
}

/** No WSL1, COMn do Windows = /dev/ttyS(n). */
function normalizePort(p: string): string {
    const m = /^COM(\d+)$/i.exec(p);
    return m && process.platform === "linux" ? `/dev/ttyS${m[1]}` : p;
}

async function detectPort(explicit?: string): Promise<string> {
    if (explicit) return normalizePort(explicit);

    const found = pickEsp(await listSerialPorts());
    if (found) return found;

    console.error(`[envcontrol] porta do ESP32 nao detectada, usando ${FALLBACK_PORT} (use -p para mudar)`);
    return normalizePort(FALLBACK_PORT);
}

/** Envia um comando e junta as linhas de resposta ate ficar ocioso. */
function request(esp: ESP32Controller, command: string, timeoutMs: number): Promise<string[]> {
    return new Promise((resolve) => {
        const lines: string[] = [];
        const stopToken = command.trim().toUpperCase() === "CONFIG" ? "CONFIG_END" : null;
        let idleTimer: ReturnType<typeof setTimeout> | undefined;
        let done = false;

        const finish = () => {
            if (done) return;
            done = true;
            clearTimeout(idleTimer);
            clearTimeout(hardTimer);
            esp.off("message", onMessage);
            resolve(lines);
        };

        const onMessage = (msg: string) => {
            lines.push(msg);
            if (stopToken && msg === stopToken) {
                setTimeout(finish, 150); // deixa o INFO final chegar
                return;
            }
            clearTimeout(idleTimer);
            idleTimer = setTimeout(finish, IDLE_MS);
        };

        const hardTimer = setTimeout(finish, timeoutMs);
        esp.on("message", onMessage);
        esp.send(command).catch((err) => {
            console.error("[ERRO]", err.message);
            finish();
        });
    });
}

/** Faz PING ate o ESP32 responder (ele pode reiniciar ao abrir a porta). */
async function waitReady(esp: ESP32Controller, maxMs = 8000): Promise<string[] | null> {
    const start = Date.now();
    while (Date.now() - start < maxMs) {
        const lines = await request(esp, "PING", 800);
        if (lines.some((l) => l.startsWith("ESP32 READY"))) return lines;
    }
    return null;
}

async function openSerial(portName: string): Promise<{ esp: ESP32Controller; ready: string[] }> {
    console.error(`[envcontrol] porta: ${portName}`);

    const esp = new ESP32Controller(portName);
    esp.on("error", (e: Error) => console.error("[ERRO]", e.message));
    try {
        await esp.connect();
    } catch (e: any) {
        console.error(`Erro ao abrir ${portName}: ${e.message}`);
        process.exit(1);
    }

    const ready = await waitReady(esp);
    if (!ready) {
        await esp.disconnect().catch(() => {});
        console.error(`A porta ${portName} abriu, mas o ESP32 nao respondeu ao PING.`);
        console.error("- Confira a porta com: envcontrol ports   (e use -p COMx)");
        console.error("- Placa com USB nativo (S2/S3/C3)? Tente: set ENVCONTROL_DTR=1");
        console.error("- Confirme que o firmware gravado tem o comando PING na serial (115200)");
        process.exit(1);
    }
    return { esp, ready };
}

// ---------------------------------------------------------------- serial ---
async function serialOneShot(command: string, portArg: string | undefined, timeout: number) {
    const portName = await detectPort(portArg);
    const { esp, ready } = await openSerial(portName);

    // PING ja foi respondido durante o handshake
    const isPing = command.trim().toUpperCase() === "PING";
    const lines = isPing
        ? ready.filter((l) => l.startsWith("ESP32 READY"))
        : await request(esp, command, timeout);
    await esp.disconnect();

    if (lines.length === 0) {
        console.error("(sem resposta do ESP32)");
        process.exit(1);
    }
    console.log(lines.join("\n"));
    process.exit(0);
}

async function serialShell(portArg: string | undefined, timeout: number) {
    const portName = await detectPort(portArg);
    const { esp } = await openSerial(portName);

    console.log(`[envcontrol] shell serial em ${portName} @ 115200`);
    console.log("Comandos: ping, on, off, status, config, wifi on|off|reconnect,");
    console.log("          wifi set <ssid> [senha], restart  |  'exit' para sair\n");

    const rl = createInterface({ input: process.stdin, output: process.stdout });
    rl.setPrompt("envcontrol> ");
    rl.prompt();

    for await (const raw of rl) {
        const line = raw.trim();
        if (!line) { rl.prompt(); continue; }
        if (["exit", "quit", "q"].includes(line.toLowerCase())) break;

        const lines = await request(esp, translate(line.split(/\s+/)), timeout);
        console.log(lines.length ? lines.join("\n") : "(sem resposta)");
        rl.prompt();
    }

    rl.close();
    await esp.disconnect();
    process.exit(0);
}

async function showPorts() {
    const ports = await listSerialPorts();
    if (ports.length === 0) return console.log("Nenhuma porta serial encontrada.");
    for (const p of ports) console.log(`${p.path}\t${p.description}`);
}

// ------------------------------------------------------------------- tcp ---
function tcpLoop(host: string, port: number) {
    let connected = false;
    const sock = net.createConnection({ host, port });
    sock.setEncoding("utf8");

    const connectTimer = setTimeout(() => {
        if (!connected) {
            console.error(`Falha ao conectar em ${host}:${port} (timeout)`);
            process.exit(1);
        }
    }, 5000);

    sock.on("connect", () => {
        connected = true;
        clearTimeout(connectTimer);
        console.error(`[envcontrol] conectado a ${host}:${port}  (Ctrl+C ou Ctrl+D para sair)`);
    });

    sock.on("data", (d: string) => process.stdout.write(d));

    sock.on("close", () => {
        console.error("\n[envcontrol] conexao encerrada");
        process.exit(0);
    });

    sock.on("error", (e) => {
        console.error(`Falha: ${e.message}`);
        process.exit(1);
    });

    const rl = createInterface({ input: process.stdin });
    rl.on("line", (line) => sock.write(line + "\n"));
    rl.on("close", () => {
        sock.end();
        setTimeout(() => process.exit(0), 300);
    });
}

// ------------------------------------------------------------------ main ---
async function main() {
    const { port, timeout, rest } = parseArgs(process.argv.slice(2));

    if (rest.length === 0 || rest[0] === "--help") {
        console.log(HELP);
        process.exit(rest.length === 0 ? 1 : 0);
    }

    if (isTcpMode(rest)) return tcpLoop(rest[0]!, Number(rest[1]));

    const first = rest[0]!.toLowerCase();
    if (first === "ports") return showPorts();
    if (first === "shell") return serialShell(port, timeout);

    return serialOneShot(translate(rest), port, timeout);
}

main();
