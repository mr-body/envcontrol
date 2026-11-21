# EnvControl

> Aplicação para configurar, monitorizar e controlar um ESP32 através de comunicação série, Wi-Fi e Bluetooth Classic.

O projeto combina uma aplicação gráfica para Windows em C#/.NET 8, uma CLI em TypeScript/Bun e dois exemplos de firmware Arduino para ESP32:

- **Wi-Fi:** conecta o ESP32 a uma rede, guarda as credenciais na memória NVS e inicia um servidor TCP na porta `8080`.
- **Bluetooth:** inicia o `BluetoothSerial`, permite configurar nome e PIN e aceita comandos por Bluetooth Classic.

## Funcionalidades

- Interface gráfica Windows Forms
- CLI para comunicação série e TCP/IP
- Controlo de um LED no GPIO 2
- Consulta do estado com `PING` e `STATUS`
- Configuração persistente do Wi-Fi
- Configuração persistente do Bluetooth
- Servidor TCP na porta `8080`
- Reconexão Wi-Fi automática
- Reinício do ESP32 por comando
- Detecção automática de portas seriais
- Suporte a Windows, Linux, macOS e WSL1

## Arquitetura

```text
┌────────────────────────────────────────┐
│ Aplicação Windows ou CLI TypeScript    │
│ C# WinForms / Bun                      │
└──────────────────┬─────────────────────┘
                   │ USB serial @ 115200 baud
                   ▼
┌────────────────────────────────────────┐
│              Firmware ESP32            │
│  Wi-Fi ou Bluetooth Classic            │
└──────────────────┬─────────────────────┘
                   │
        ┌──────────┴──────────┐
        ▼                     ▼
Servidor TCP:8080      Serial Bluetooth
```

## Estrutura do repositório

```text
envcontrol/
├── envcontrol/
│   ├── module/
│   │   ├── SerialCommunication.cs
│   │   ├── SerialPortManager.cs
│   │   └── WifiScanner.cs
│   ├── Form1.cs
│   ├── WifiConfig.cs
│   ├── BluetoothConfig.cs
│   └── envcontrol.csproj
├── envcontrol_wifiConfig/
│   └── envcontrol_wifiConfig.ino
├── envcontrol_bluetoothConfig/
│   └── envcontrol_bluetoothConfig.ino
├── CLI/
│   ├── index.ts
│   ├── ESP32Controller.ts
│   ├── WinSerial.ts
│   ├── ports.ts
│   └── package.json
├── envcontrol.sln
└── README.md
```

## Tecnologias

| Componente | Tecnologia |
|---|---|
| Interface gráfica | C# e Windows Forms |
| Plataforma desktop | .NET 8 Windows |
| Pesquisa Wi-Fi no Windows | API nativa `wlanapi.dll` |
| Comunicação série C# | `System.IO.Ports` |
| CLI | TypeScript e Bun |
| Comunicação série CLI | `serialport` ou API Windows Kernel32 |
| Firmware | C++ com Arduino Core para ESP32 |
| Wi-Fi | `WiFi.h` |
| Bluetooth Classic | `BluetoothSerial.h` |
| Configuração persistente | `Preferences.h` / NVS |

## Hardware

- ESP32
- Cabo USB com transferência de dados
- LED ou carga ligada ao GPIO `2`
- Rede Wi-Fi de 2,4 GHz para o firmware Wi-Fi

A comunicação é feita a `115200 baud`.

## Firmware

Os firmwares Wi-Fi e Bluetooth são independentes. Grave apenas a versão pretendida no ESP32.

### Firmware Wi-Fi

Abra:

```text
envcontrol_wifiConfig/envcontrol_wifiConfig.ino
```

O firmware:

- Guarda SSID, password e estado na NVS
- Ativa ou desativa o Wi-Fi
- Tenta reconectar a cada 10 segundos
- Inicia o servidor TCP na porta `8080` após obter um IP
- Controla o LED pelo GPIO 2
- Aceita comandos pela porta USB ou pelo socket TCP

### Firmware Bluetooth

Abra:

```text
envcontrol_bluetoothConfig/envcontrol_bluetoothConfig.ino
```

Valores padrão:

| Configuração | Valor |
|---|---|
| Nome | `ESP32_ENVCONTROL` |
| PIN | `123456` |
| Estado | Ativado |

O nome e o PIN são guardados na NVS. Depois de alterar, envie `RESTART` para reiniciar o ESP32 e aplicar completamente a configuração.

## Aplicação Windows

### Requisitos

- Windows 10 ou posterior
- .NET 8 SDK
- Visual Studio 2022
- Firmware carregado no ESP32
- Driver USB correspondente ao conversor serial

### Compilar

```powershell
dotnet restore
dotnet build envcontrol.sln
```

A solução também pode ser aberta diretamente no Visual Studio:

```text
envcontrol.sln
```

### Executar

1. Conecte o ESP32 ao computador.
2. Selecione a porta COM.
3. Clique no botão de conexão.
4. Utilize ON/OFF para controlar o LED.
5. Utilize PING para testar a comunicação.
6. Abra a configuração Wi-Fi para pesquisar redes e introduzir credenciais.
7. Abra a configuração Bluetooth para alterar nome, PIN ou estado.

A comunicação série utiliza `115200 baud`, `DtrEnable = true` e `RtsEnable = false`.

## CLI TypeScript

### Requisitos

- Bun
- Firmware compatível carregado no ESP32
- Porta série disponível

### Instalar

```bash
cd CLI
bun install
bun link
```

Para executar sem criar um comando global:

```bash
bun run index.ts --help
```

### Comandos

```bash
envcontrol ports
envcontrol ping
envcontrol on
envcontrol off
envcontrol status
envcontrol config
envcontrol restart
envcontrol shell
```

Definir manualmente a porta:

```bash
envcontrol -p COM7 ping
```

Ou configurar uma variável de ambiente no Windows:

```powershell
$env:ENVCONTROL_PORT = "COM7"
envcontrol status
```

No WSL1, `COMn` é convertido automaticamente para `/dev/ttyS<n>`.

### Wi-Fi pela CLI

```bash
envcontrol wifi on
envcontrol wifi off
envcontrol wifi reconnect
envcontrol wifi set MinhaRede MinhaSenha
```

No protocolo atual, SSID e password não devem conter espaços, porque o parser separa os argumentos pelo último espaço.

### Shell interativo

```bash
envcontrol shell
```

Comandos disponíveis:

```text
ping
on
off
status
config
wifi on
wifi off
wifi reconnect
wifi set <SSID> <SENHA>
restart
exit
```

### Comunicação TCP

Depois de o ESP32 obter um endereço IP:

```bash
envcontrol <IP_DO_ESP32> 8080
```

Exemplo:

```bash
envcontrol 192.168.1.50 8080
```

Pressione `Ctrl+C` ou `Ctrl+D` para terminar.

### Compilar a CLI

```bash
cd CLI
bun run build
```

O ficheiro será criado em:

```text
CLI/dist/index.js
```

### Criar executável no Windows

```bash
cd CLI
bun run compling
```

O resultado é:

```text
CLI/envcontrol.exe
```

## Protocolo de comunicação

Todas as mensagens são terminadas por nova linha (`\n`).

### Comandos comuns

| Comando | Resultado |
|---|---|
| `ON` | Liga o LED no GPIO 2 |
| `OFF` | Desliga o LED no GPIO 2 |
| `PING` | Verifica se o ESP32 está disponível |
| `STATUS` | Devolve `STATUS:ON` ou `STATUS:OFF` |
| `CONFIG` | Devolve o bloco completo de configuração |
| `RESTART` | Reinicia o ESP32 |

### Comandos Wi-Fi

| Comando | Descrição |
|---|---|
| `CONFIG` | Lista estado, SSID, IP e configuração Wi-Fi |
| `WIFI ON` | Ativa o Wi-Fi |
| `WIFI OFF` | Desativa o Wi-Fi |
| `WIFI RECONNECT` | Solicita uma nova ligação |
| `SET WIFI SSID PASS <SSID> <PASSWORD>` | Guarda e aplica as credenciais |
| `SET WIFI <SSID> <PASSWORD>` | Formato alternativo |

### Comandos Bluetooth

| Comando | Descrição |
|---|---|
| `CONFIG` | Lista nome, PIN, estado e atividade |
| `GET BT CONFIG` | Devolve nome, PIN e estado |
| `SET BT NAME <NOME>` | Altera o nome Bluetooth |
| `SET BT PIN <PIN>` | Altera o PIN, entre 4 e 16 caracteres |
| `SET BT ENABLED ON` | Ativa o Bluetooth |
| `SET BT ENABLED OFF` | Desativa o Bluetooth |

### Resposta de exemplo

```text
CONFIG_START
LED:OFF
WIFI_ENABLED:ON
WIFI_SSID:MinhaRede
WIFI_STATUS:CONNECTED
WIFI_IP:192.168.1.50
CONFIG_END
```

## Solução de problemas

### Porta não encontrada

- Confirme que o cabo suporta transferência de dados.
- Instale o driver do conversor CH340, CP210x ou FTDI.
- Execute `envcontrol ports`.
- Feche o Monitor Serial ou o Arduino IDE.

### Acesso negado à porta

Feche outros programas que estejam utilizando o mesmo COM e tente novamente.

### O ESP32 não responde ao PING

- Confirme o baud rate `115200`.
- Confirme que gravou o firmware no modelo correto.
- Use uma porta explícita: `envcontrol -p COM7 ping`.
- Reinicie o ESP32.
- Em placas com USB nativo, tente:

```powershell
$env:ENVCONTROL_DTR = "1"
```

### Wi-Fi não conecta

- Utilize uma rede de 2,4 GHz.
- Confirme SSID e password.
- Evite espaços no SSID e na password.
- Execute `CONFIG` e verifique `WIFI_STATUS`.
- Reinicie o ESP32 após alterar a configuração.

### Servidor TCP indisponível

Confirme que:

- O Wi-Fi está ativo.
- O ESP32 obteve um endereço IP.
- O computador está na mesma rede.
- A porta `8080` não está bloqueada pelo firewall.

### Bluetooth não inicia

- Confirme que está usando o firmware Bluetooth.
- Confirme que `BT ENABLED` está ativo.
- Reinicie o ESP32 após alterar nome ou PIN.
- Remova emparelhamentos antigos e procure `ESP32_ENVCONTROL`.

## Limitações atuais

- Wi-Fi e Bluetooth estão separados em dois firmwares.
- O firmware Wi-Fi não cria um modo de configuração automática por ponto de acesso.
- O parser não suporta espaços em SSID ou password Wi-Fi.
- A aplicação Windows depende da API nativa do Windows para procurar redes Wi-Fi.
- Toda a comunicação é baseada em comandos textuais enviados por linha.
- A senha Wi-Fi e o PIN Bluetooth não devem ser considerados informações públicas.

## Licença

O repositório não contém atualmente um ficheiro de licença na raiz. Adicione uma licença antes de distribuir o projeto publicamente.
