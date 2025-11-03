import 'dart:async';
import 'dart:math';

import 'package:reactable/reactable.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'package:logger/logger.dart';

enum TelemetryState { normal, paused }

final logger = Logger();

class ServerHandler {
  late HubConnection hubConn;
  late HubConnection alertsHubConn; // keep a field so it persists

  // assign token for each client instance
  final String clientAPIToken = "client-api-token";

  // Reactable to hold the current bill value
  Reactable<double> billReactable = 0.0.asReactable;

  // state to track whether readings should be paused or active
  TelemetryState state = TelemetryState.normal;

  // optional UI callbacks (used by the home_page widget)
  void Function(String title, String body)? showBanner;
  void Function()? hideBanner;

  ServerHandler() {
    setupConnection();
    registerInitialHandler(); // handlers must be registered before starting connection
  }

  void setupConnection() {
    // create HTTPConnectionOptions with accessTokenFactory
    final httpConOptions = HttpConnectionOptions(
      accessTokenFactory: () async => clientAPIToken,
      transport: HttpTransportType.WebSockets,
    );

    // handles connection to server and communication with it (/hubs/connect matches what is in the server code)
    // changed http to https for TLS encryption
    hubConn = HubConnectionBuilder()
        .withUrl("https://localhost:5001/hubs/connect", options: httpConOptions)
        .build();

    // connect to alerts hub to listen for grid status changes (DOWN / UP)
    alertsHubConn = HubConnectionBuilder()
        .withUrl("https://localhost:5001/hubs/alerts", options: httpConOptions)
        .withAutomaticReconnect()
        .build();

    // listen for grid status messages from the server
    alertsHubConn.on("gridStatus", (args) {
      if (args == null || args.isEmpty) return;
      final msg = args.first as Map<dynamic, dynamic>;
      final status = (msg["status"] as String?) ?? "";
      final title = (msg["title"] as String?) ?? "Status update";
      final body = (msg["message"] as String?) ?? "";

      if (status == "DOWN") {
        state = TelemetryState.paused;
        showBanner?.call(title, body);
        logger.w("Grid DOWN: pausing telemetry and dropping readings.");
      } else if (status == "UP") {
        state = TelemetryState.normal;
        hideBanner?.call();
        logger.i("Grid UP: resuming telemetry.");
      }
    });
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
    // if grid is down, drop the reading and do not send it to the server
    if (state == TelemetryState.paused) {
      logger.d("Telemetry paused: dropping reading $reading");
      return;
    }

    // validate reading is a positive decimal
    if (reading.isNaN || reading.isInfinite || reading < 0) {
      logger.e("Client-side validation failed: Invalid reading- Must be a positive decimal.");
      return;
    }

    await hubConn.send("CalculateNewBill", args: [billReactable.value, reading]);
  }

  setBill(List? result) {
    billReactable.value = result?[0].toDouble();
  }

  registerInitialHandler() {
    hubConn.on("receiveInitialBill", setBill);

    hubConn.on("calculateBill", setBill);

    // listen for error messages from the server and log them
    hubConn.on("error", (args) {
      if (args != null && args.isNotEmpty) {
        logger.e("Server error: ${args[0]}");
      } else {
        logger.e("Unknown server error");
      }
    });
  }

  initServerConnection() async {
    // starts the connection to the server
    await hubConn.start();

    // starts the alerts hub connection (await so DOWN/UP works immediately)
    await alertsHubConn.start();
  }
}
