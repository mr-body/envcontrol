#include "BluetoothSerial.h"
#include <Preferences.h>

BluetoothSerial SerialBT;
Preferences preferences;

#define LED_PIN 2

const char* DEFAULT_BT_NAME    = "ESP32_ENVCONTROL";
const char* DEFAULT_BT_PIN     = "123456";
const bool  DEFAULT_BT_ENABLED = true;

String bluetoothName;
String bluetoothPassword;
bool   bluetoothEnabled = DEFAULT_BT_ENABLED;
bool   btActive         = false;

bool previousBTState = false;

// ======================================
// SALVAR CONFIGURAÇÕES
// ======================================

void saveBluetoothConfig() {
    preferences.begin("envcontrol", false);
    preferences.putString("bt_name", bluetoothName);
    preferences.putString("bt_pin", bluetoothPassword);
    preferences.putBool("bt_enabled", bluetoothEnabled);
    preferences.end();
}

// ======================================
// NOTIFICAR SISTEMAS
// ======================================

void alertSystems(String message, Stream* excludeStream = nullptr) {
    if (&Serial != excludeStream) {
        Serial.println("INFO:" + message);
    }

    if (bluetoothEnabled && btActive && SerialBT.hasClient() && &SerialBT != excludeStream) {
        SerialBT.println("INFO:" + message);
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
        alertSystems("LED ON", &output);
    }
    else if (upperCmd == "OFF") {
        digitalWrite(LED_PIN, LOW);
        output.println("LED OFF");
        alertSystems("LED OFF", &output);
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
        String ledState = digitalRead(LED_PIN) ? "ON" : "OFF";
        String btEnStr  = bluetoothEnabled ? "ON" : "OFF";
        String btActStr = (bluetoothEnabled && btActive) ? "YES" : "NO";

        output.println("CONFIG_START");
        output.println("LED:" + ledState);
        output.println("BT_NAME:" + bluetoothName);
        output.println("BT_PIN:" + bluetoothPassword);
        output.println("BT_ENABLED:" + btEnStr);
        output.println("BT_ACTIVE:" + btActStr);
        output.println("CONFIG_END");
    }
    else if (upperCmd.startsWith("SET BT NAME ") || upperCmd.startsWith("SET BUETHOOTH NAME ")) {
        int spaceIdx = rawCmd.lastIndexOf(' ');
        String newName = rawCmd.substring(spaceIdx + 1);
        newName.trim();

        if (newName.length() > 0) {
            bluetoothName = newName;
            saveBluetoothConfig();
            output.println("NAME SET: " + bluetoothName + " (RESTART ESP32 TO APPLY)");
        } else {
            output.println("ERROR: EMPTY NAME");
        }
    }
    else if (upperCmd.startsWith("SET BT PIN ") || upperCmd.startsWith("SET BUETHOOTH PASSWORD ")) {
        int spaceIdx = rawCmd.lastIndexOf(' ');
        String newPin = rawCmd.substring(spaceIdx + 1);
        newPin.trim();

        // PIN numérico de 4 a 16 dígitos é recomendado no Bluetooth clássico
        if (newPin.length() >= 4 && newPin.length() <= 16) {
            bluetoothPassword = newPin;
            saveBluetoothConfig();
            output.println("PIN SET (RESTART ESP32 TO APPLY)");
        } else {
            output.println("ERROR: PIN MUST BE 4-16 CHARS");
        }
    }
    else if (upperCmd == "SET BT ENABLED ON" || upperCmd == "SET BT ENABLED TRUE" || upperCmd == "BT ON") {
        bluetoothEnabled = true;
        saveBluetoothConfig();
        output.println("BT ENABLED: ON (RESTART ESP32 TO APPLY)");
    }
    else if (upperCmd == "SET BT ENABLED OFF" || upperCmd == "SET BT ENABLED FALSE" || upperCmd == "BT OFF") {
        bluetoothEnabled = false;
        saveBluetoothConfig();
        output.println("BT ENABLED: OFF (RESTART ESP32 TO APPLY)");
    }
    else if (upperCmd == "GET BT CONFIG" || upperCmd == "GET_BUETHOOTH_CONFIG") {
        output.print("NAME:" + bluetoothName + "\nPIN:" + bluetoothPassword + "\nENABLED:");
        output.println(bluetoothEnabled ? "ON" : "OFF");
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
// MONITORIZAR BLUETOOTH
// ======================================

void checkBluetoothConnection() {
    if (!bluetoothEnabled || !btActive) return;

    bool currentState = SerialBT.hasClient();
    if (currentState != previousBTState) {
        if (currentState) {
            Serial.println("[BLUETOOTH] NOVA CONEXAO!");
            alertSystems("BLUETOOTH CONNECTED", &SerialBT);
        }
        else {
            Serial.println("[BLUETOOTH] DISPOSITIVO DESCONECTADO!");
            Serial.println("INFO:BLUETOOTH DISCONNECTED");
        }
        previousBTState = currentState;
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

    // Inicializar armazenamento e carregar configurações
    preferences.begin("envcontrol", false);
    bluetoothName     = preferences.getString("bt_name", DEFAULT_BT_NAME);
    bluetoothPassword = preferences.getString("bt_pin", DEFAULT_BT_PIN);
    bluetoothEnabled  = preferences.getBool("bt_enabled", DEFAULT_BT_ENABLED);
    preferences.end();

    // Garantir valores válidos
    if (bluetoothName.length() == 0) bluetoothName = DEFAULT_BT_NAME;
    if (bluetoothPassword.length() < 4) bluetoothPassword = DEFAULT_BT_PIN;

    Serial.println("[SYSTEM] ESP32 READY");
    Serial.println("[USB] Serial initialized");
    Serial.printf("[BLUETOOTH] Enabled: %s\n", bluetoothEnabled ? "YES" : "NO");

    if (bluetoothEnabled) {
        SerialBT.setPin(bluetoothPassword.c_str());

        if (!SerialBT.begin(bluetoothName.c_str())) {
            Serial.println("[ERROR] Bluetooth initialization failed!");
            while (true) {
                delay(1000);
            }
        }

        btActive = true;
        Serial.printf("[BLUETOOTH] Name: %s\n", bluetoothName.c_str());
        Serial.println("[BLUETOOTH] PIN: ******");
        Serial.println("[BLUETOOTH] Configuration loaded!");
        Serial.println("[BLUETOOTH] Waiting for connections...");
    } else {
        Serial.println("[BLUETOOTH] Disabled by configuration.");
    }
}

// ======================================
// LOOP
// ======================================

void loop() {
    checkBluetoothConnection();

    // USB SERIAL
    if (Serial.available() > 0) {
        String command = Serial.readStringUntil('\n');
        processCommand(command, Serial);
    }

    // BLUETOOTH SERIAL
    if (bluetoothEnabled && btActive && SerialBT.available() > 0) {
        String command = SerialBT.readStringUntil('\n');
        processCommand(command, SerialBT);
    }
}