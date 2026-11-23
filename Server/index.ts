import net from "node:net";
import readline from "node:readline";
import crypto from "node:crypto";

// =====================================================
// CONFIGURAÇÃO
// =====================================================

const HOST = process.env.HOST ?? "0.0.0.0";
const PORT = Number(process.env.PORT ?? 9090);

const AUTH_TIMEOUT_MS = 10_000;
const MAX_BUFFER = 4096;
const MAX_FAILURES = 5;
const LOCKOUT_MS = 60_000;

if (!Number.isInteger(PORT) || PORT < 1 || PORT > 65535) {
    console.error("Invalid PORT.");
    process.exit(1);
}

// O token tem de ser igual ao configurado no ESP32:
//   SET REMOTE TOKEN <token>   (32 a 128 caracteres)
let TOKEN = process.env.ENVCONTROL_TOKEN ?? "";

if (!TOKEN) {
    TOKEN = crypto.randomBytes(24).toString("hex"); // 48 caracteres

    console.log("[SETUP] ENVCONTROL_TOKEN is not set. Token for this run only:");
    console.log(TOKEN);
    console.log("[SETUP] Configure the ESP32 with: SET REMOTE TOKEN <token>");
    console.log("[SETUP] To keep it, set ENVCONTROL_TOKEN before starting.\n");
}

if (TOKEN.length < 32 || TOKEN.length > 128) {
    console.error("ENVCONTROL_TOKEN must have 32-128 characters.");
    process.exit(1);
}

// =====================================================
// UTILITÁRIOS
// =====================================================

// Comparação em tempo constante (hash para igualar os tamanhos)
function safeEqual(a: string, b: string): boolean {
    const ha = crypto.createHash("sha256").update(a).digest();
    const hb = crypto.createHash("sha256").update(b).digest();

    return crypto.timingSafeEqual(ha, hb);
}

type Failure = { count: number; lockedUntil: number };

const failures = new Map<string, Failure>();

function isLocked(ip: string): boolean {
    const entry = failures.get(ip);

    return !!entry && entry.lockedUntil > Date.now();
}

function registerFailure(ip: string): void {
    const entry = failures.get(ip) ?? { count: 0, lockedUntil: 0 };

    entry.count++;

    if (entry.count >= MAX_FAILURES) {
        entry.count = 0;
        entry.lockedUntil = Date.now() + LOCKOUT_MS;

        console.log(`[SECURITY] ${ip} locked for ${LOCKOUT_MS / 1000}s`);
    }

    failures.set(ip, entry);
}

// =====================================================
// DISPOSITIVOS
// =====================================================

type Device = {
    id: string;
    ip: string;
    socket: net.Socket;
    authenticated: boolean;
    connectedAt: Date;
};

const devices = new Map<string, Device>();
let counter = 1;

// Aceita "esp32-1" ou apenas "1"
function findDevice(ref: string | undefined): Device | undefined {
    if (!ref) return undefined;

    return devices.get(ref) ?? devices.get(`esp32-${ref}`);
}

function sendTo(device: Device, command: string): boolean {
    if (!device.authenticated || device.socket.destroyed) {
        return false;
    }

    device.socket.write(command + "\n");
    return true;
}

// =====================================================
// SERVIDOR TCP
// =====================================================

const server = net.createServer((socket) => {
    const ip = socket.remoteAddress ?? "unknown";

    if (isLocked(ip)) {
        console.log(`[REJECTED] ${ip} is locked`);
        socket.destroy();
        return;
    }

    const id = `esp32-${counter++}`;

    const device: Device = {
        id,
        ip,
        socket,
        authenticated: false,
        connectedAt: new Date(),
    };

    devices.set(id, device);

    console.log(`[CONNECTED] ${id}: ${ip} (waiting for authentication)`);

    socket.setNoDelay(true);
    socket.setKeepAlive(true, 30_000);

    // Fecha ligações que não se autenticam a tempo
    const authTimer = setTimeout(() => {
        if (!device.authenticated) {
            console.log(`[TIMEOUT] ${id} did not authenticate`);
            socket.destroy();
        }
    }, AUTH_TIMEOUT_MS);

    let buffer = "";

    socket.on("data", (data) => {
        buffer += data.toString();

        let index: number;

        while ((index = buffer.indexOf("\n")) !== -1) {
            const message = buffer.slice(0, index).trim();
            buffer = buffer.slice(index + 1);

            if (message) {
                handleMessage(device, message, authTimer);
            }

            if (socket.destroyed) return;
        }

        if (buffer.length > MAX_BUFFER) {
            console.log(`[ERROR] Message too large: ${id}`);
            socket.destroy();
        }
    });

    socket.on("close", () => {
        clearTimeout(authTimer);

        // Não apagar se já foi substituído
        if (devices.get(id) === device) {
            devices.delete(id);
        }

        console.log(`[DISCONNECTED] ${id}`);
    });

    socket.on("error", (error) => {
        console.error(`[ERROR] ${id}:`, error.message);
    });
});

function handleMessage(
    device: Device,
    message: string,
    authTimer: NodeJS.Timeout
): void {
    // ----- Autenticação -----
    if (!device.authenticated) {
        if (!message.startsWith("AUTH:")) {
            device.socket.write("AUTH_REQUIRED\n");
            return;
        }

        const token = message.slice(5).trim();

        if (safeEqual(token, TOKEN)) {
            device.authenticated = true;
            clearTimeout(authTimer);
            failures.delete(device.ip);

            // Remove ligações antigas do mesmo IP (ESP32 que reconectou)
            for (const other of devices.values()) {
                if (other !== device && other.ip === device.ip) {
                    console.log(`[REPLACED] ${other.id} by ${device.id}`);
                    devices.delete(other.id);
                    other.socket.destroy();
                }
            }

            device.socket.write("AUTH_OK:REMOTE\n");
            console.log(`[AUTHENTICATED] ${device.id}`);
        } else {
            registerFailure(device.ip);

            console.log(`[AUTH FAILED] ${device.id}: ${device.ip}`);

            device.socket.write("AUTH_FAILED\n");
            device.socket.end();
        }

        return;
    }

    // ----- Mensagens do dispositivo autenticado -----
    console.log(`[${device.id}] ${message}`);
}

server.on("error", (error) => {
    console.error("[SERVER ERROR]", error.message);
    process.exit(1);
});

server.listen(PORT, HOST, () => {
    console.log(`EnvControl Server running on ${HOST}:${PORT}`);
});

// =====================================================
// TERMINAL
// =====================================================

const terminal = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: "envcontrol> ",
});

terminal.prompt();

terminal.on("line", (input) => {
    const [action, ...args] = input.trim().split(/\s+/);

    switch (action?.toLowerCase()) {
        case "":
        case undefined:
            break;

        case "devices": {
            if (devices.size === 0) {
                console.log("No devices connected.");
                break;
            }

            for (const d of devices.values()) {
                console.log(
                    `${d.id}  ${d.ip}  ${d.authenticated ? "AUTHENTICATED" : "PENDING"}  since ${d.connectedAt.toLocaleTimeString()}`
                );
            }
            break;
        }

        case "send": {
            const device = findDevice(args.shift());
            const command = args.join(" ");

            if (!device || !command) {
                console.log("Usage: send <id> <command>");
                break;
            }

            if (!sendTo(device, command)) {
                console.log(`${device.id} is not authenticated.`);
                break;
            }

            console.log(`[SEND] ${device.id}: ${command}`);
            break;
        }

        case "all": {
            const command = args.join(" ");

            if (!command) {
                console.log("Usage: all <command>");
                break;
            }

            let sent = 0;

            for (const device of devices.values()) {
                if (sendTo(device, command)) sent++;
            }

            console.log(`[BROADCAST] ${command} (${sent} device(s))`);
            break;
        }

        case "kick": {
            const device = findDevice(args[0]);

            if (!device) {
                console.log("Usage: kick <id>");
                break;
            }

            device.socket.destroy();
            break;
        }

        case "help":
            console.log(`
devices                  list connected devices
send <id> <command>      send to one device (id or number, e.g. send 1 STATUS)
all <command>            send to all authenticated devices
kick <id>                disconnect a device
help
exit
`);
            break;

        case "exit":
            for (const device of devices.values()) {
                device.socket.destroy();
            }

            terminal.close();
            server.close(() => process.exit(0));
            setTimeout(() => process.exit(0), 1000).unref();
            return;

        default:
            console.log("Unknown command. Type help.");
    }

    terminal.prompt();
});