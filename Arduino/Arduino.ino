#include <ArduinoModbus.h>

#define AOUT_PIN   A0
#define RELAY_PIN  2

void setup() {
  pinMode(RELAY_PIN, OUTPUT);
  digitalWrite(RELAY_PIN, HIGH); // pump off at start

  // Start Modbus RTU slave with ID 1, on Serial, at 9600 baud
  if (!ModbusRTUServer.begin(1, 9600)) {
    Serial.println("Failed to start Modbus RTU Server!");
    while (1); // halt if Modbus fails
  }

  // Register map — tell the library how many registers to expose
  ModbusRTUServer.configureCoils(0, 1);              // 1 coil  starting at address 0 (pump command)
  ModbusRTUServer.configureInputRegisters(0, 1);     // 1 input register at address 0 (humidity)
}

void loop() {
  // Must call poll() every loop — handles incoming Modbus requests from master
  ModbusRTUServer.poll();

  ModbusRTUServer.inputRegisterWrite(0, analogRead(AOUT_PIN));
  digitalWrite(RELAY_PIN, ModbusRTUServer.coilRead(0) ? LOW : HIGH);

  delay(100); // short delay — keep loop fast so poll() runs frequently
}