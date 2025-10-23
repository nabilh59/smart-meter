import 'dart:async';
import 'dart:math';

import 'package:reactable/reactable.dart';
import 'package:signalr_netcore/hub_connection.dart';
import 'package:signalr_netcore/hub_connection_builder.dart';

class ServerHandler {
  // handles connection to server and communication with it (/hubs/connect matches what is in the server code) 
  HubConnection hubConn = HubConnectionBuilder().withUrl("http://localhost:5006/hubs/connect").build();  

  // Reactable to hold the current bill value 
  Reactable<double> billReactable = 0.0.asReactable;

  ServerHandler() {
    registerInitialHandler(); // handlers must be registered before starting connection
  }

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
    await hubConn.send("CalculateNewBill", args:[billReactable.value, reading]);   
  }  

  setBill(List? result){
    billReactable.value = result?[0].toDouble();
  }

  registerInitialHandler(){    
    hubConn.on("receiveInitialBill", setBill);

    hubConn.on("calculateBill", setBill); 
  }

  initServerConnection() async
  {
    // starts the connection to the server
    await hubConn.start();
  } 
}