#include <Preferences.h>
#include <WiFi.h>

Preferences preferences;

#define LED_PIN 2

// ======================================
// CONFIGURAÇÕES PADRÃO - WI-FI
// ======================================

const char* DEFAULT_WIFI_SSID    = "";
const char* DEFAULT_WIFI_PASS    = "";
const bool  DEFAULT_WIFI_ENABLED = false;

String wifiSSID;
String wifiPassword;
bool   wifiEnabled             = DEFAULT_WIFI_ENABLED;
bool   previousWiFiState       = false;
unsigned long lastWiFiAttempt  = 0;
const unsigned long WIFI_RETRY_INTERVAL_MS = 10000;

// ======================================
// SERVIDOR TCP (SOCKET)
// ======================================

WiFiServer tcpServer(8080);
WiFiClient tcpClient;
bool tcpStarted = false;

// Declaração antecipada
void alertSystems(String message);

// ======================================
// SALVAR / CARREGAR CONFIGURAÇÕES WI-FI
// ======================================

void saveWiFiConfig() {
    preferences.begin("envcontrol", false);
    preferences.putString("wifi_ssid", wifiSSID);
    preferences.putString("wifi_pass", wifiPassword);
    preferences.putBool("wifi_enabled", wifiEnabled);
    preferences.end();
    Serial.println("[WIFI] Credentials saved to NVS.");
}

void loadWiFiConfig() {
    preferences.begin("envcontrol", true); // true = apenas leitura
    wifiSSID     = preferences.getString("wifi_ssid", DEFAULT_WIFI_SSID);
    wifiPassword = preferences.getString("wifi_pass", DEFAULT_WIFI_PASS);
    wifiEnabled  = preferences.getBool("wifi_enabled", DEFAULT_WIFI_ENABLED);
    preferences.end();
}

// ======================================
// CONTROLE WI-FI
// ======================================

void startWiFiConnection() {
    if (!wifiEnabled || wifiSSID.length() == 0) {
        if (tcpClient && tcpClient.connected()) {
            tcpClient.stop();
        }
        tcpStarted = false;
        WiFi.disconnect(true);
        WiFi.mode(WIFI_OFF);
        Serial.println("[WIFI] Disabled or empty SSID.");
        return;
    }

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
    Serial.printf("[WIFI] Connecting to \"%s\"...\n", wifiSSID.c_str());
}

void monitorWiFiConnection() {
    if (!wifiEnabled) {
        if (previousWiFiState) {
            previousWiFiState = false;
            Serial.println("[WIFI] DISCONNECTED (DISABLED)");
        }
        return;
    }

    bool isConnected = (WiFi.status() == WL_CONNECTED);

    if (isConnected != previousWiFiState) {
        if (isConnected) {
            // Garante que SSID, password e estado habilitado ficam salvos ao conectar
            saveWiFiConfig();
            Serial.printf("[WIFI] CONNECTED! IP: %s\n", WiFi.localIP().toString().c_str());
            alertSystems("WIFI CONNECTED (" + WiFi.localIP().toString() + ")");
        } else {
            Serial.println("[WIFI] DISCONNECTED!");
            alertSystems("WIFI DISCONNECTED");
        }
        previousWiFiState = isConnected;
    }

    if (!isConnected && wifiSSID.length() > 0) {
        if (millis() - lastWiFiAttempt >= WIFI_RETRY_INTERVAL_MS) {
            startWiFiConnection();
        }
    }
}

void handleTcpServer() {
    if (!wifiEnabled || WiFi.status() != WL_CONNECTED) {
        if (tcpClient && tcpClient.connected()) {
            tcpClient.stop();
        }
        return;
    }

    if (!tcpStarted) {
        tcpServer.begin();
        tcpStarted = true;
        Serial.printf("[SOCKET] TCP Server listening on %s:8080\n", WiFi.localIP().toString().c_str());
    }

    // Aceita novo cliente se não houver um ativo ou se o anterior desconectou
    if (!tcpClient || !tcpClient.connected()) {
        WiFiClient incoming = tcpServer.available();
        if (incoming) {
            tcpClient = incoming;
            Serial.println("[SOCKET] Client connected!");
            tcpClient.println("INFO:SOCKET CONNECTED");
        }
    }

    // Lê comandos enviados pelo socket
    if (tcpClient && tcpClient.connected() && tcpClient.available() > 0) {
        String command = tcpClient.readStringUntil('\n');
        processCommand(command, tcpClient);
    }
}

// ======================================
// NOTIFICAR SISTEMAS
// ======================================

void alertSystems(String message) {
    Serial.println("INFO:" + message);
    if (tcpClient && tcpClient.connected()) {
        tcpClient.println("INFO:" + message);
    }
}

// ======================================
// PROCESSAR COMANDOS
// ======================================

void processCommand(String rawCmd, Stream &output) {
    rawCmd.trim();
    if (rawCmd.length() == 0) return;

    String upperCmd = rawCmd;
    upperCmd.toUpperCase();

    if (upperCmd == "ON") {
        digitalWrite(LED_PIN, HIGH);
        output.println("LED ON");
        alertSystems("LED ON");
    }
    else if (upperCmd == "OFF") {
        digitalWrite(LED_PIN, LOW);
        output.println("LED OFF");
        alertSystems("LED OFF");
    }
    else if (upperCmd == "PING") {
        output.print("ESP32 READY | LED: ");
        output.println(digitalRead(LED_PIN) ? "ON" : "OFF");
    }
    else if (upperCmd == "STATUS") {
        output.print("STATUS:");
        output.println(digitalRead(LED_PIN) ? "ON" : "OFF");
    }
    else if (upperCmd == "CONFIG") {
        String ledState   = digitalRead(LED_PIN) ? "ON" : "OFF";
        String wifiEnStr  = wifiEnabled ? "ON" : "OFF";
        String wifiStatus = !wifiEnabled ? "DISABLED" : (WiFi.status() == WL_CONNECTED ? "CONNECTED" : "DISCONNECTED");
        String wifiIp     = (WiFi.status() == WL_CONNECTED) ? WiFi.localIP().toString() : "0.0.0.0";

        output.println("CONFIG_START");
        output.println("LED:" + ledState);
        output.println("WIFI_ENABLED:" + wifiEnStr);
        output.println("WIFI_SSID:" + wifiSSID);
        output.println("WIFI_STATUS:" + wifiStatus);
        output.println("WIFI_IP:" + wifiIp);
        output.println("CONFIG_END");
    }
    // --- WI-FI (COMANDO ÚNICO: SET WIFI <SSID> <PASS>) ---
    else if (upperCmd.startsWith("SET WIFI SSID PASS ")) {
        String payload = rawCmd.substring(19);
        payload.trim();

        int lastSpace = payload.lastIndexOf(' ');
        String newSSID = (lastSpace != -1) ? payload.substring(0, lastSpace) : payload;
        String newPass = (lastSpace != -1) ? payload.substring(lastSpace + 1) : "";
        newSSID.trim();
        newPass.trim();

        if (newSSID.length() > 0) {
            wifiSSID = newSSID;
            wifiPassword = newPass;
            wifiEnabled = true;
            saveWiFiConfig();
            output.println("WIFI SET: " + wifiSSID + " | CONNECTING...");
            startWiFiConnection();
        } else {
            output.println("ERROR: EMPTY WIFI SSID");
        }
    }
    else if (upperCmd == "SET WIFI ENABLED ON" || upperCmd == "SET WIFI ENABLED TRUE" || upperCmd == "WIFI ON") {
        wifiEnabled = true;
        saveWiFiConfig();
        output.println("WIFI_ENABLED: ON");
        startWiFiConnection();
    }
    else if (upperCmd == "SET WIFI ENABLED OFF" || upperCmd == "SET WIFI ENABLED FALSE" || upperCmd == "WIFI OFF") {
        wifiEnabled = false;
        saveWiFiConfig();
        output.println("WIFI_ENABLED: OFF");
        startWiFiConnection();
    }
    else if (upperCmd.startsWith("SET WIFI ")) {
        String payload = rawCmd.substring(9);
        payload.trim();

        int lastSpace = payload.lastIndexOf(' ');
        String newSSID = (lastSpace != -1) ? payload.substring(0, lastSpace) : payload;
        String newPass = (lastSpace != -1) ? payload.substring(lastSpace + 1) : "";
        newSSID.trim();
        newPass.trim();

        if (newSSID.length() > 0) {
            wifiSSID = newSSID;
            wifiPassword = newPass;
            wifiEnabled = true;
            saveWiFiConfig();
            output.println("WIFI SET: " + wifiSSID + " | CONNECTING...");
            startWiFiConnection();
        } else {
            output.println("ERROR: EMPTY WIFI SSID");
        }
    }
    else if (upperCmd == "WIFI RECONECT" || upperCmd == "WIFI RECONNECT") {
        output.println("WIFI RECONNECTING...");
        startWiFiConnection();
    }
    else if (upperCmd == "RESTART") {
        output.println("RESTARTING...");
        delay(200);
        ESP.restart();
    }
    else {
        output.println("INVALID COMMAND");
    }
}

// ======================================
// SETUP
// ======================================

void setup() {
    Serial.begin(115200);

    pinMode(LED_PIN, OUTPUT);
    digitalWrite(LED_PIN, LOW);

    Serial.println("[SYSTEM] Starting ESP32...");

    // Carregar configurações salvas do Wi-Fi (SSID, password e estado)
    loadWiFiConfig();

    Serial.println("[SYSTEM] ESP32 READY");
    Serial.println("[USB] Serial initialized");

    // Inicializar Wi-Fi se habilitado e com SSID salvo
    Serial.printf("[WIFI] Enabled: %s | SSID: \"%s\"\n", wifiEnabled ? "YES" : "NO", wifiSSID.c_str());
    if (wifiEnabled && wifiSSID.length() > 0) {
        startWiFiConnection();
    } else {
        WiFi.mode(WIFI_OFF);
    }
}

// ======================================
// LOOP
// ======================================

void loop() {
    monitorWiFiConnection();
    handleTcpServer();

    // USB SERIAL
    if (Serial.available() > 0) {
        String command = Serial.readStringUntil('\n');
        processCommand(command, Serial);
    }
}