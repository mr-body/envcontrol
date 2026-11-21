# envcontrol

CLI (Bun + TypeScript) para o firmware ESP32 EnvControl, via serial e TCP/IP.

## Instalar

```bash
bun install
bun link          # opcional: cria o comando global `envcontrol`
```

## Serial (comando unico)

```bash
envcontrol ping
envcontrol on
envcontrol off
envcontrol status
envcontrol config
envcontrol restart
envcontrol wifi on          # off | reconnect
envcontrol wifi set MinhaRede MinhaSenha
envcontrol -p COM7 ping     # porta manual (ou ENVCONTROL_PORT=COM7)
envcontrol ports            # lista portas seriais
envcontrol shell            # shell interativo pela serial
```

## TCP/IP (loop estilo nc)

```bash
envcontrol 192.168.1.50 8080
```

Tudo que for digitado vai para o ESP32; tudo que chegar e impresso.
Saia com Ctrl+C ou Ctrl+D.

## Notas

- A porta e detectada automaticamente (CP210x, CH340, FTDI, Espressif); fallback `COM7`.
- No WSL1, `COMn` e convertido para `/dev/ttyS<n>` automaticamente.
- DTR/RTS sao desligados ao abrir a porta para o ESP32 nao reiniciar a cada comando.
- Binario unico (Windows): `bun run build` -> `envcontrol.exe`
  (modulos nativos do `serialport` podem exigir `bun link` em vez do binario compilado).
