export interface PortInfo {
    path: string;
    description: string;
}

/** Lista portas seriais. No Windows usa PowerShell (sem modulo nativo). */
export async function listSerialPorts(): Promise<PortInfo[]> {
    if (process.platform === "win32") {
        const script =
            "Get-CimInstance Win32_PnPEntity | " +
            "Where-Object { $_.Name -match '\\(COM\\d+\\)' } | " +
            "ForEach-Object { $_.Name }";

        try {
            const proc = Bun.spawn(["powershell", "-NoProfile", "-NonInteractive", "-Command", script], {
                stdout: "pipe",
                stderr: "ignore",
            });
            const text = await new Response(proc.stdout).text();

            const ports: PortInfo[] = [];
            for (const line of text.split(/\r?\n/)) {
                const m = /\((COM\d+)\)/i.exec(line);
                if (m) ports.push({ path: m[1]!.toUpperCase(), description: line.trim() });
            }
            return ports;
        } catch {
            return [];
        }
    }

    const moduleName = "serialport";
    const { SerialPort } = await import(moduleName);
    const list = await SerialPort.list();
    return list.map((p: any) => ({
        path: p.path as string,
        description: `${p.manufacturer ?? "-"} VID:${p.vendorId ?? "-"} PID:${p.productId ?? "-"}`,
    }));
}

const ESP_BRIDGE = /CP210|CH34|CH91|FTDI|USB.?SERIAL|Silicon Labs|Espressif|USB JTAG|UART/i;

/** Escolhe a porta que parece ser do ESP32 (ignora Bluetooth). */
export function pickEsp(ports: PortInfo[]): string | undefined {
    const real = ports.filter((p) => !/bluetooth/i.test(p.description));
    const bridges = real.filter((p) => ESP_BRIDGE.test(p.description));
    if (bridges.length === 1) return bridges[0]!.path;
    if (real.length === 1) return real[0]!.path;
    return undefined;
}
