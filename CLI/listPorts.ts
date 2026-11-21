import { SerialPort } from "serialport";

async function listPorts() {
    const ports = await SerialPort.list();

    console.log("Portas disponíveis:");

    for (const port of ports) {
        console.log({
            path: port.path,
            manufacturer: port.manufacturer,
            serialNumber: port.serialNumber,
            vendorId: port.vendorId,
            productId: port.productId
        });
    }
}

listPorts();
