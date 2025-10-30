import 'dart:async';
import 'dart:math';

import 'package:reactable/reactable.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'package:logger/logger.dart';

class ServerHandler {
  final Logger logger;
  late HubConnection hubConn;
  late HttpConnectionOptions _httpConOpts;

  // assign token for each client instance
  final String clientAPIToken = "client-api-token";

  // Reactable to hold the current bill value
  Reactable<double> billReactable = 0.0.asReactable;

  ServerHandler({required this.logger}) {
    setupConnection();
    registerInitialHandler(); // handlers must be registered before starting connection
  }

  void setupConnection() {
    // create HTTPConnectionOptions with accessTokenFactory
    _httpConOpts = HttpConnectionOptions(
      accessTokenFactory: () async => clientAPIToken,
      transport: HttpTransportType.WebSockets,
    );

    // handles connection to server and communication with it (/hubs/connect matches what is in the server code)
    // changed http to https for TLS encryption
    hubConn = HubConnectionBuilder()
    .withUrl("https://localhost:5001/hubs/connect", options: _httpConOpts)
    .build();
  }

  HttpConnectionOptions get httpConOptions => _httpConOpts;

  // sends a new reading to the server every 15 to 60 seconds
  sendReadings() {
    int duration = Random().nextInt(45) + 15; // random number between 15 and 60

    Future.delayed(Duration(seconds: duration), () {
      double reading = Random().nextDouble(); // random number between 0.0 and 1.0
      sendReading(reading);
      sendReadings(); // recursive so that a new random reading and random delay is generated each time
    });
  }

  sendReading(double reading) async {
    // validate reading is a positive decimal
    if (reading.isNaN || reading.isInfinite || reading < 0) {
      logger.e("Client-side validation failed: Invalid reading- Must be a positive decimal.");
      return;
    }

    await hubConn.send("CalculateNewBill", args:[billReactable.value, reading]);   
  }  

  setBill(List? result){
    billReactable.value = result?[0].toDouble();
  }

  registerInitialHandler(){    
    hubConn.on("receiveInitialBill", setBill);

    hubConn.on("calculateBill", setBill);

    // listen for error messages from the server and log them
    hubConn.on("error", (args) {
      if (args !=null && args.isNotEmpty) {
        logger.e("Server error: ${args[0]}");
      } else {
        logger.e("Unknown server error");
      }
    });
  }

  initServerConnection() async
  {
    // starts the connection to the server
    await hubConn.start();
  } 
}