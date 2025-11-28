import 'dart:async';
import 'dart:io';
import 'dart:math';
import 'package:reactable/reactable.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'package:logger/logger.dart';

enum TelemetryState { normal, paused }

class ServerHandler {
  static Logger logger = Logger(
    level: Level.all, // change as needed to control log output
    printer: PrettyPrinter(
      methodCount: 0,
      printEmojis: false,
    ),
  );

  late HubConnection hubConn;
  late HttpConnectionOptions _httpConOpts;

  // assign token for each client instance
  final String clientAPIToken = "client-api-token";

  // Reactable to hold the current bill value
  Reactable<String> billReactable = Reactable("");

  // Reactable to hold the current bill date
  Reactable<String> billDateReactable = Reactable("");

  // state to track whether readings should be paused or active
  TelemetryState state = TelemetryState.normal;

  // optional UI callbacks (used by the home_page widget)
  void Function(String title, String body)? showBanner;
  void Function()? hideBanner;

  double lastReadingTotal = 0.0;

  ServerHandler({HubConnection? injected}) {
    // allows injection of a mock HubConnection for testing purposes
    if (injected != null) {
      hubConn = injected;
    } else {
      setupConnection();
    }
    registerInitialHandler();
  }


  void setupConnection() {    
    // create HTTPConnectionOptions with accessTokenFactory
    _httpConOpts = HttpConnectionOptions(
      accessTokenFactory: () async => clientAPIToken,
      transport: HttpTransportType.WebSockets,
      skipNegotiation: true,
    );

    // handles connection to server and communication with it (/hubs/connect matches what is in the server code)
    // changed http to https for TLS encryption (switch to http://localhost:5000 if using HTTP)
    hubConn = HubConnectionBuilder()
        .withUrl(
          "https://localhost:5001/hubs/connect",
          options: httpConOptions,
          httpClientFactory: () {
            // Accept self-signed certificates only for localhost (safe for local development)
            final client = HttpClient();
            client.badCertificateCallback = (cert, host, port) {
              if (host == 'localhost' || host == '127.0.0.1') return true;
              return false;
            };
            return client;
          },
        )
        .withAutomaticReconnect(
          // explicit reconnect delays to make behavior consistent across builds
          retryDelays: const [
            Duration(seconds: 0),
            Duration(seconds: 2),
            Duration(seconds: 5),
            Duration(seconds: 10),
          ],
        )
        .build(); 
  }

  // validate client's token for authentication
  bool validateToken(String token) {
    return token == clientAPIToken;
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
    // if grid is down or the server is down, locally store the reading (until things are back up)
    if (state == TelemetryState.paused) {
      lastReadingTotal += reading;
      showBanner?.call("Temporary grid interruption", "We can’t calculate your bill right now due to a grid issue. No action is needed.");
      return;
    } else if (hubConn.state != HubConnectionState.Connected) {
      showBanner?.call("Server Issue", "The server is down. Please wait for reconnection...");
      lastReadingTotal += reading;
      logger.e("Server disconnected.");
      return;
    }

    // client-side validation of reading
    if (reading.isNaN || reading.isInfinite || reading < 0) {
      logger.e("Client-side validation failed: Invalid reading- Must be a positive decimal.");
      return;
    }

    try {
      DateTime nowDate = DateTime.now().toUtc();
      int nowEpoch = nowDate.millisecondsSinceEpoch;

      await hubConn.send("CalculateNewBill", args: [billReactable.value, reading, nowEpoch]);
      logger.i("Sent new reading to server: $reading with current bill: ${billReactable.value}");
    } catch (e) {
      logger.e("Failed to send reading: $e");
    }
  }

  setGuiValues(List? result) {
    billReactable.value = result?[0];
    billDateReactable.value = result?[1];
  }

  registerInitialHandler() {
    logger.i("Registering initial handlers...");
    hubConn.on("receiveInitialBill", setGuiValues);
    hubConn.on("calculateBill", setGuiValues);

    // listen for error messages from the server and log them
    hubConn.on("error", (args) {
      if (args != null && args.isNotEmpty) {
        logger.e("Server error: ${args[0]}");
      } else {
        logger.e("Unknown server error");
      }
    });

    hubConn.on("gridStatus", (args) {
      if (args == null || args.isEmpty) return;
      final msg = args.first as Map<dynamic, dynamic>;
      final status = (msg["status"] as String?) ?? "";
      final title = (msg["title"] as String?) ?? "Status update";
      final body = (msg["message"] as String?) ?? "";

      if (status == "DOWN") {
        state = TelemetryState.paused;
        showBanner?.call(title, body);
      } else if (status == "UP") {
        state = TelemetryState.normal;
        hideBanner?.call();
      }
    });

    // handles reconnection events
    hubConn.onreconnected(({connectionId}) {
      logger.i("SignalR: onreconnected (connectionId=$connectionId)");
      // attempt to send any accumulated readings
      if (lastReadingTotal > 0) {
        sendReading(lastReadingTotal);
        lastReadingTotal = 0.0;
      }
      hideBanner?.call();
      logger.i("Reconnected to server. Keeping last valid bill: ${billReactable.value}");
    });

    // when starting to reconnect
    hubConn.onreconnecting((error) {
      logger.w("SignalR: onreconnecting: $error");
      showBanner?.call("Reconnecting", "Attempting to reconnect to server...");
    });

    // when the connection is closed
    hubConn.onclose(({error}) {
      logger.e("SignalR: onclose: $error");
      showBanner?.call("Server disconnected", "Connection to server was lost. Will retry automatically.");
    });
  }

  initServerConnection() async {
    // starts the connection to the server (single hub only)
    logger.i("Setting up server connection to https://localhost:5001...");
    try {
      await hubConn.start().timeout(const Duration(seconds: 15));
      logger.i("Server connection setup complete.");
      hideBanner?.call();
    } catch (e) {
      logger.e("Failed to start HubConnection: $e");
      showBanner?.call("Error connecting to server", "$e");
      rethrow;
    }
  }
}
