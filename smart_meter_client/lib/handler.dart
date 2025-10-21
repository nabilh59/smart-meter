import 'dart:developer';

import 'package:reactable/reactable.dart';
import 'package:signalr_netcore/hub_connection.dart';
import 'package:signalr_netcore/hub_connection_builder.dart';

class ServerHandler {
  HubConnection hubConn = HubConnectionBuilder().withUrl("http://localhost:5006/hubs/connect").build();   
  Reactable<double> billReactable = 0.0.asReactable;

  ServerHandler() {
    registerInitialHandler();
  }  

  sendReading(double reading) async {
    await hubConn.send("CalculateNewBill", args:[billReactable.value, reading]);
      // server responds with the new bill based on the new reading just sent      
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
    await hubConn.start();
  } 
}