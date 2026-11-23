#include <WiFi.h>
#include <Preferences.h>

Preferences preferences;

#define LED_PIN 2

// =====================================================
// CONFIGURAÇÕES PADRÃO
// =====================================================

const uint16_t DEFAULT_LOCAL_PORT = 8080;
const char* DEFAULT_LOCAL_PASSWORD = "envcontrol123";

const unsigned long WIFI_RETRY_INTERVAL_MS = 10000;
const unsigned long REMOTE_RETRY_INTERVAL_MS = 10000;

const size_t MAX_TOKEN_LENGTH = 128;
const size_t MAX_PASSWORD_LENGTH = 64;

// =====================================================
// CONFIGURAÇÕES EM RAM
// =====================================================

String wifiSSID = "";
String wifiPassword = "";
bool wifiEnabled = false;

String remoteServerHost = "";
uint16_t remoteServerPort = 9090;
String remoteServerToken = "";

uint16_t localServerPort = DEFAULT_LOCAL_PORT;
String localServerPassword = "";

bool previousWiFiState = false;

unsigned long lastWiFiAttempt = 0;
unsigned long lastRemoteAttempt = 0;

// =====================================================
// SERVIDOR TCP LOCAL
// =====================================================

WiFiServer* localServer = nullptr;
WiFiClient localClient;

bool localServerRunning = false;
bool localClientAuthenticated = false;

// =====================================================
// CLIENTE TCP REMOTO
// =====================================================

WiFiClient remoteServerClient;

bool remoteConnected = false;
bool remoteAuthenticated = false;
bool remoteAuthSent = false;

// =====================================================
// DECLARAÇÕES
// =====================================================

void processCommand(String command, Stream& output);
void alertSystems(String message);
void startWiFiConnection();
void monitorWiFiConnection();
void startLocalServer();
void stopLocalServer();
void handleLocalServer();
void connectRemoteServer();
void handleRemoteServer();
void stopRemoteConnection();
void saveConfig();
void loadConfig();
void printConfig(Stream& output);

// =====================================================
// PERSISTÊNCIA
// =====================================================

void saveConfig() {
    preferences.begin("envcontrol", false);

    preferences.putString("wifi_ssid", wifiSSID);
    preferences.putString("wifi_pass", wifiPassword);
    preferences.putBool("wifi_enabled", wifiEnabled);

    preferences.putString("remote_host", remoteServerHost);
    preferences.putUShort("remote_port", remoteServerPort);
    preferences.putString("remote_token", remoteServerToken);

    preferences.putUShort("local_port", localServerPort);
    preferences.putString("local_pass", localServerPassword);

    preferences.end();

    Serial.println("[CONFIG] Configuration saved.");
}

void loadConfig() {
    preferences.begin("envcontrol", true);

    wifiSSID = preferences.getString("wifi_ssid", "");
    wifiPassword = preferences.getString("wifi_pass", "");
    wifiEnabled = preferences.getBool("wifi_enabled", false);

    remoteServerHost = preferences.getString("remote_host", "");
    remoteServerPort = preferences.getUShort("remote_port", 9090);
    remoteServerToken = preferences.getString("remote_token", "");

    localServerPort = preferences.getUShort(
        "local_port",
        DEFAULT_LOCAL_PORT
    );

    localServerPassword = preferences.getString("local_pass", DEFAULT_LOCAL_PASSWORD);

    preferences.end();

    if (localServerPort == 0) {
        localServerPort = DEFAULT_LOCAL_PORT;
    }

    if (localServerPassword.length() < 8) {
        localServerPassword = DEFAULT_LOCAL_PASSWORD;
        Serial.println("[CONFIG] Using default local password.");
    }
}

// =====================================================
// ENCERRAR SERVIDOR LOCAL
// =====================================================

void stopLocalServer() {
    if (localClient) {
        localClient.stop();
    }

    localClientAuthenticated = false;

    if (localServer != nullptr) {
        localServer->end();
        delete localServer;
        localServer = nullptr;
    }

    localServerRunning = false;

    Serial.println("[LOCAL] Server stopped.");
}

// =====================================================
// INICIAR SERVIDOR LOCAL
// =====================================================

void startLocalServer() {
    if (WiFi.status() != WL_CONNECTED) {
        return;
    }

    if (localServerPassword.length() < 8) {
        Serial.println("[LOCAL] Password must have at least 8 characters.");
        return;
    }

    stopLocalServer();

    localServer = new WiFiServer(localServerPort);

    if (localServer == nullptr) {
        Serial.println("[LOCAL] Failed to allocate server.");
        return;
    }

    localServer->begin();
    localServer->setNoDelay(true);

    localServerRunning = true;

    Serial.printf(
        "[LOCAL] Listening on %s:%u\n",
        WiFi.localIP().toString().c_str(),
        localServerPort
    );
}

// =====================================================
// ENCERRAR CONEXÃO REMOTA
// =====================================================

void stopRemoteConnection() {
    if (remoteServerClient) {
        remoteServerClient.stop();
    }

    remoteConnected = false;
    remoteAuthenticated = false;
    remoteAuthSent = false;
}

// =====================================================
// WI-FI
// =====================================================

void startWiFiConnection() {
    if (!wifiEnabled || wifiSSID.length() == 0) {
        stopLocalServer();
        stopRemoteConnection();

        WiFi.disconnect(true);
        WiFi.mode(WIFI_OFF);

        previousWiFiState = false;

        Serial.println("[WIFI] Disabled or SSID not configured.");
        return;
    }

    stopLocalServer();
    stopRemoteConnection();

    WiFi.mode(WIFI_STA);
    WiFi.setAutoReconnect(true);
    WiFi.disconnect();

    delay(100);

    if (wifiPassword.length() > 0) {
        WiFi.begin(wifiSSID.c_str(), wifiPassword.c_str());
    } else {
        WiFi.begin(wifiSSID.c_str());
    }

    lastWiFiAttempt = millis();

    Serial.printf(
        "[WIFI] Connecting to %s...\n",
        wifiSSID.c_str()
    );
}

void monitorWiFiConnection() {
    bool connected = wifiEnabled &&
        WiFi.status() == WL_CONNECTED;

    if (connected != previousWiFiState) {
        previousWiFiState = connected;

        if (connected) {
            Serial.printf(
                "[WIFI] Connected. IP: %s\n",
                WiFi.localIP().toString().c_str()
            );

            alertSystems("WIFI CONNECTED");

            startLocalServer();
        } else {
            Serial.println("[WIFI] Disconnected.");

            stopLocalServer();
            stopRemoteConnection();
        }
    }

    if (
        wifiEnabled &&
        wifiSSID.length() > 0 &&
        !connected &&
        millis() - lastWiFiAttempt >= WIFI_RETRY_INTERVAL_MS
    ) {
        startWiFiConnection();
    }
}

// =====================================================
// SERVIDOR TCP LOCAL
// =====================================================

void handleLocalServer() {
    if (!wifiEnabled || WiFi.status() != WL_CONNECTED) {
        return;
    }

    if (!localServerRunning) {
        static unsigned long lastStartAttempt = 0;

        if (millis() - lastStartAttempt < 10000) {
            return;
        }

        lastStartAttempt = millis();
        startLocalServer();
        return;
    }

    // Aceitar cliente novo
    if (!localClient || !localClient.connected()) {
        localClientAuthenticated = false;

        WiFiClient incoming = localServer->available();

        if (incoming) {
            localClient = incoming;
            localClient.setNoDelay(true);
            localClient.setTimeout(50);

            localClient.println("AUTH_REQUIRED");

            Serial.println("[LOCAL] Client connected.");
        }
    }

    if (!localClient || !localClient.connected()) {
        return;
    }

    if (localClient.available() <= 0) {
        return;
    }

    String command = localClient.readStringUntil('\n');
    command.trim();

    if (command.length() == 0) {
        return;
    }

    // Autenticação obrigatória
    if (!localClientAuthenticated) {
        if (command.startsWith("AUTH:")) {
            String password = command.substring(5);
            password.trim();

            if (
                localServerPassword.length() >= 8 &&
                password == localServerPassword
            ) {
                localClientAuthenticated = true;
                localClient.println("AUTH_OK:LOCAL");

                Serial.println("[LOCAL] Client authenticated.");
            } else {
                localClient.println("AUTH_FAILED");
                localClient.stop();

                Serial.println("[LOCAL] Authentication failed.");
            }
        } else {
            localClient.println("AUTH_REQUIRED");
        }

        return;
    }

    processCommand(command, localClient);
}

// =====================================================
// CONEXÃO AO SERVIDOR REMOTO
// =====================================================

void connectRemoteServer() {
    if (
        !wifiEnabled ||
        WiFi.status() != WL_CONNECTED ||
        remoteServerHost.length() == 0 ||
        remoteServerPort == 0 ||
        remoteServerToken.length() == 0
    ) {
        return;
    }

    if (remoteServerClient.connected()) {
        return;
    }

    if (
        lastRemoteAttempt != 0 &&
        millis() - lastRemoteAttempt < REMOTE_RETRY_INTERVAL_MS
    ) {
        return;
    }

    lastRemoteAttempt = millis();

    stopRemoteConnection();

    Serial.printf(
        "[REMOTE] Connecting to %s:%u...\n",
        remoteServerHost.c_str(),
        remoteServerPort
    );

    remoteServerClient.setTimeout(50);

    if (
        remoteServerClient.connect(
            remoteServerHost.c_str(),
            remoteServerPort
        )
    ) {
        remoteConnected = true;
        remoteAuthenticated = false;
        remoteAuthSent = false;

        remoteServerClient.setNoDelay(true);

        remoteServerClient.println(
            "AUTH:" + remoteServerToken
        );

        remoteAuthSent = true;

        Serial.println("[REMOTE] TCP connected. Authenticating...");
    } else {
        Serial.println("[REMOTE] Connection failed.");
        stopRemoteConnection();
    }
}

// =====================================================
// MONITORIZAR SERVIDOR REMOTO
// =====================================================

void handleRemoteServer() {
    if (
        !wifiEnabled ||
        WiFi.status() != WL_CONNECTED
    ) {
        stopRemoteConnection();
        return;
    }

    if (!remoteServerClient.connected()) {
        if (remoteConnected) {
            Serial.println("[REMOTE] Server disconnected.");
        }

        stopRemoteConnection();
        connectRemoteServer();
        return;
    }

    remoteConnected = true;

    if (remoteServerClient.available() <= 0) {
        return;
    }

    String message = remoteServerClient.readStringUntil('\n');
    message.trim();

    if (message.length() == 0) {
        return;
    }

    // Resposta de autenticação
    if (!remoteAuthenticated) {
        if (message.startsWith("AUTH_OK:")) {
            remoteAuthenticated = true;

            Serial.println("[REMOTE] Authentication successful.");

            alertSystems("REMOTE AUTHENTICATED");
        } else if (message == "AUTH_FAILED") {
            Serial.println("[REMOTE] Authentication failed.");
            stopRemoteConnection();
        }

        return;
    }

    // Só aceita comandos depois da autenticação
    Serial.println("[REMOTE] Command: " + message);

    processCommand(message, remoteServerClient);
}

// =====================================================
// NOTIFICAR SISTEMAS
// =====================================================

void alertSystems(String message) {
    Serial.println("INFO:" + message);

    if (
        localClient &&
        localClient.connected() &&
        localClientAuthenticated
    ) {
        localClient.println("INFO:" + message);
    }

    if (
        remoteServerClient &&
        remoteServerClient.connected() &&
        remoteAuthenticated
    ) {
        remoteServerClient.println("INFO:" + message);
    }
}

// =====================================================
// CONFIGURAÇÃO / ESTADO
// =====================================================

void printConfig(Stream& output) {
    bool wifiConnected = WiFi.status() == WL_CONNECTED;

    output.println("CONFIG_START");

    // LED
    output.println(
        String("LED_STATUS:") +
        (digitalRead(LED_PIN) ? "ON" : "OFF")
    );

    // Wi-Fi
    output.println(
        String("WIFI_ENABLED:") +
        (wifiEnabled ? "YES" : "NO")
    );

    output.println("WIFI_SSID:" + wifiSSID);

    output.println(
        String("WIFI_STATUS:") +
        (wifiConnected ? "CONNECTED" : "DISCONNECTED")
    );

    output.println(
        String("WIFI_IP:") +
        (wifiConnected ? WiFi.localIP().toString() : "0.0.0.0")
    );

    output.println(
        String("WIFI_RSSI:") +
        (wifiConnected ? String(WiFi.RSSI()) : "0")
    );

    // Servidor local
    output.println(
        "LOCAL_SERVER_PORT:" + String(localServerPort)
    );

    output.println(
        String("LOCAL_SERVER_RUNNING:") +
        (localServerRunning ? "YES" : "NO")
    );

    output.println(
        String("LOCAL_PASSWORD:") +
        (localServerPassword)
    );

    output.println(
        String("LOCAL_CLIENT_CONNECTED:") +
        (localClient && localClient.connected() ? "YES" : "NO")
    );

    output.println(
        String("LOCAL_CLIENT_AUTHENTICATED:") +
        (localClientAuthenticated ? "YES" : "NO")
    );

    // Servidor remoto
    output.println("REMOTE_HOST:" + remoteServerHost);
    output.println(
        "REMOTE_PORT:" + String(remoteServerPort)
    );

    output.println(
        String("REMOTE_TOKEN:") +
        (remoteServerToken)
    );

    output.println(
        String("REMOTE_CONNECTED:") +
        (remoteConnected ? "YES" : "NO")
    );

    output.println(
        String("REMOTE_AUTHENTICATED:") +
        (remoteAuthenticated ? "YES" : "NO")
    );

    output.println("CONFIG_END");
}

// =====================================================
// PROCESSAR COMANDOS
// =====================================================

void processCommand(String rawCommand, Stream& output) {
    rawCommand.trim();

    if (rawCommand.length() == 0) {
        return;
    }

    String command = rawCommand;
    command.toUpperCase();

    // -------------------------------------------------
    // LED
    // -------------------------------------------------

    if (command == "ON") {
        digitalWrite(LED_PIN, HIGH);
        output.println("LED ON");
        alertSystems("LED ON");
        return;
    }

    if (command == "OFF") {
        digitalWrite(LED_PIN, LOW);
        output.println("LED OFF");
        alertSystems("LED OFF");
        return;
    }

    // -------------------------------------------------
    // ESTADO E CONFIGURAÇÃO
    // -------------------------------------------------

    if (command == "PING") {
        output.print("ESP32 READY | LED: ");
        output.println(digitalRead(LED_PIN) ? "ON" : "OFF");
        return;
    }

    if (command == "STATUS") {
        output.println(
            String("LED:") +
            (digitalRead(LED_PIN) ? "ON" : "OFF")
        );

        output.println(
            String("WIFI:") +
            (WiFi.status() == WL_CONNECTED ? "CONNECTED" : "DISCONNECTED")
        );

        output.println(
            String("REMOTE:") +
            (remoteAuthenticated ? "AUTHENTICATED" : "DISCONNECTED")
        );

        return;
    }

    if (command == "CONFIG") {
        printConfig(output);
        return;
    }

    // -------------------------------------------------
    // WI-FI: SET WIFI SSID PASS <ssid>|<password>
    // Também aceita o formato antigo:
    // SET WIFI <ssid> <password>
    // -------------------------------------------------

    if (command.startsWith("SET WIFI SSID PASS ")) {
        String payload = rawCommand.substring(19);
        payload.trim();

        int separator = payload.indexOf('|');

        if (separator < 1) {
            output.println(
                "ERROR: USE SET WIFI SSID PASS <ssid>|<password>"
            );
            return;
        }

        String newSSID = payload.substring(0, separator);
        String newPassword = payload.substring(separator + 1);

        newSSID.trim();
        newPassword.trim();

        if (newSSID.length() == 0) {
            output.println("ERROR: EMPTY SSID");
            return;
        }

        wifiSSID = newSSID;
        wifiPassword = newPassword;
        wifiEnabled = true;

        saveConfig();

        output.println("WIFI CONFIGURATION SAVED");
        startWiFiConnection();
        return;
    }

    if (command.startsWith("SET WIFI ")) {
        String payload = rawCommand.substring(9);
        payload.trim();

        int separator = payload.lastIndexOf(' ');

        if (separator < 1) {
            output.println(
                "ERROR: USE SET WIFI <ssid> <password>"
            );
            return;
        }

        wifiSSID = payload.substring(0, separator);
        wifiPassword = payload.substring(separator + 1);
        wifiEnabled = true;

        wifiSSID.trim();
        wifiPassword.trim();

        if (wifiSSID.length() == 0) {
            output.println("ERROR: EMPTY SSID");
            return;
        }

        saveConfig();

        output.println("WIFI CONFIGURATION SAVED");
        startWiFiConnection();
        return;
    }

    if (
        command == "SET WIFI ENABLED ON" ||
        command == "SET WIFI ENABLED TRUE" ||
        command == "WIFI ON"
    ) {
        wifiEnabled = true;
        saveConfig();

        output.println("WIFI ENABLED");
        startWiFiConnection();
        return;
    }

    if (
        command == "SET WIFI ENABLED OFF" ||
        command == "SET WIFI ENABLED FALSE" ||
        command == "WIFI OFF"
    ) {
        wifiEnabled = false;
        saveConfig();

        output.println("WIFI DISABLED");
        startWiFiConnection();
        return;
    }

    if (
        command == "WIFI RECONNECT" ||
        command == "WIFI RECONECT"
    ) {
        output.println("WIFI RECONNECTING");
        startWiFiConnection();
        return;
    }

    // -------------------------------------------------
    // SERVIDOR REMOTO
    // -------------------------------------------------

    if (command.startsWith("SET REMOTE HOST ")) {
        String value = rawCommand.substring(16);
        value.trim();

        if (value.length() == 0) {
            output.println("ERROR: EMPTY HOST");
            return;
        }

        remoteServerHost = value;

        saveConfig();
        stopRemoteConnection();

        lastRemoteAttempt = 0;

        output.println("REMOTE HOST SAVED");
        return;
    }

    if (command.startsWith("SET REMOTE PORT ")) {
        String value = rawCommand.substring(16);
        value.trim();

        long port = value.toInt();

        if (port < 1 || port > 65535) {
            output.println("ERROR: INVALID PORT");
            return;
        }

        remoteServerPort = (uint16_t)port;

        saveConfig();
        stopRemoteConnection();

        lastRemoteAttempt = 0;

        output.println("REMOTE PORT SAVED");
        return;
    }

    if (command.startsWith("SET REMOTE TOKEN ")) {
        String value = rawCommand.substring(17);
        value.trim();

        if (
            value.length() < 32 ||
            value.length() > MAX_TOKEN_LENGTH
        ) {
            output.println("ERROR: TOKEN MUST HAVE 32-128 CHARACTERS");
            return;
        }

        remoteServerToken = value;

        saveConfig();
        stopRemoteConnection();

        lastRemoteAttempt = 0;

        output.println("REMOTE TOKEN SAVED");
        return;
    }

    // -------------------------------------------------
    // SERVIDOR LOCAL
    // -------------------------------------------------

    if (command.startsWith("SET LOCAL PORT ")) {
        String value = rawCommand.substring(15);
        value.trim();

        long port = value.toInt();

        if (port < 1 || port > 65535) {
            output.println("ERROR: INVALID PORT");
            return;
        }

        localServerPort = (uint16_t)port;

        saveConfig();
        stopLocalServer();

        output.println("LOCAL PORT SAVED");

        if (WiFi.status() == WL_CONNECTED) {
            startLocalServer();
        }

        return;
    }

    if (command.startsWith("SET LOCAL PASSWORD ")) {
        String value = rawCommand.substring(19);
        value.trim();

        if (
            value.length() < 8 ||
            value.length() > MAX_PASSWORD_LENGTH
        ) {
            output.println("ERROR: PASSWORD MUST HAVE 8-64 CHARACTERS");
            return;
        }

        localServerPassword = value;

        saveConfig();
        stopLocalServer();

        output.println("LOCAL PASSWORD SAVED");

        if (WiFi.status() == WL_CONNECTED) {
            startLocalServer();
        }

        return;
    }

    // -------------------------------------------------
    // REINICIAR
    // -------------------------------------------------

    if (command == "RESTART") {
        output.println("RESTARTING");
        delay(200);
        ESP.restart();
        return;
    }

    output.println("INVALID COMMAND");
}

// =====================================================
// SETUP
// =====================================================

void setup() {
    Serial.begin(115200);
    Serial.setTimeout(50);

    pinMode(LED_PIN, OUTPUT);
    digitalWrite(LED_PIN, LOW);

    Serial.println();
    Serial.println("================================");
    Serial.println("       ENVCONTROL ESP32");
    Serial.println("================================");

    loadConfig();

    Serial.println("[SYSTEM] Ready.");

    if (wifiEnabled && wifiSSID.length() > 0) {
        startWiFiConnection();
    } else {
        WiFi.mode(WIFI_OFF);
    }
}

// =====================================================
// LOOP
// =====================================================

void loop() {
    monitorWiFiConnection();

    // Comandos pela USB Serial
    if (Serial.available() > 0) {
        String command = Serial.readStringUntil('\n');
        processCommand(command, Serial);
    }
    handleLocalServer();

    handleRemoteServer();

    delay(2);
}
