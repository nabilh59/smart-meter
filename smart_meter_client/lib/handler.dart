import 'dart:async';
import 'dart:math';
// incompatible with macOS
// import 'dart:nativewrappers/_internal/vm/lib/ffi_allocation_patch.dart';

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

  bool skipInitialBill = false;

  String lastBill = "";

  bool isReconnecting = false;

  ServerHandler() {
    setupConnection();
    registerInitialHandler(); // handlers must be registered before starting connection
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
        .withUrl("https://localhost:5001/hubs/connect", options: httpConOptions)
        .withAutomaticReconnect()
        .build(); 

    // handles disconnection events
    hubConn.onclose(({error}) {
      initServerConnection();
    });

    // handles reconnection events
    hubConn.onreconnected(({connectionId}) {
      skipInitialBill = true;
      if (lastBill.isEmpty) {
        lastBill = "0.00";
      }
      billReactable.value = lastBill;
      logger.i("Reconnected to server. Keeping last valid bill: $lastBill");
    });
  }

  // validate client's token for authentication
  bool validateToken(String token) {
    return token == clientAPIToken;
  }

  HttpConnectionOptions get httpConOptions => _httpConOpts;

  // sends a new reading to the server every 15 to 60 seconds
  sendReadings() {
    int duration = 2; // random number between 15 and 60

    Future.delayed(Duration(seconds: duration), () {
      double reading = Random().nextDouble(); // random number between 0.0 and 1.0
    
      // skip sending reading if server is disconnected or telemetry is paused
      if (!isReconnecting && (hubConn.state != HubConnectionState.Connected || state != TelemetryState.normal)) {
        showBanner?.call("Server/Telemetry Issue", "Waiting for server to reconnect or telemetry to resume...");
        logger.w("Skipped reading: server disconnected or telemetry paused.");
      }
      // send reading if server is connected and telemetry is normal
      if (hubConn.state== HubConnectionState.Connected && state == TelemetryState.normal) {
        sendReading(reading);
      }
      sendReadings(); // recursive so that a new random reading and random delay is generated each time 
    });
    
  }

  sendReading(double reading) async {
    // if grid is down, drop the reading and do not send it to the server
    if (state == TelemetryState.paused) return;

    // skip reading if not connected
    if (hubConn.state != HubConnectionState.Connected) {
      logger.e("Server disconnected.");
      return;
    }
    if (reading.isNaN || reading.isInfinite || reading < 0) {
      logger.e("Client-side validation failed: Invalid reading- Must be a positive decimal.");
      return;
    }

    try {
      DateTime nowDate = DateTime.now().toUtc();
      int nowEpoch = nowDate.millisecondsSinceEpoch;

      // ensure bill value is not empty
      final currentBill = (billReactable.value.isEmpty) ? "0.00" : billReactable.value;
      await hubConn.send("CalculateNewBill", args: [currentBill, reading, nowEpoch]);
      // await hubConn.send("CalculateNewBill", args: [billReactable.value, reading, nowEpoch]);
      logger.i("Sent new reading to server: $reading with current bill: $currentBill");
    } catch (e) {
      logger.e("Failed to send reading: $e");
    }
  }

  setGuiValues(List? result) {
    // skip the initial bill update after reconnecting
    if (skipInitialBill) {
      skipInitialBill = false;
      return;
    }

    // only update bill if new value is not 0 or not empty
    if (result != null && result.isNotEmpty) {
      final newBill = result[0] ?? "";
      final newDate = result[1] ?? "";

      if (newBill.isNotEmpty && newBill != "0" && newBill != "0.00") {
        billReactable.value = newBill;
        lastBill = newBill;
      } else {
        if (lastBill.isNotEmpty) {
          billReactable.value = lastBill;
          logger.w("Received invalid bill value from server, keeping previous value: $lastBill");
        } else {
          logger.w("Received invalid bill value from server, keeping current value: ${billReactable.value}");
        }
      }
      if (newDate.isNotEmpty) {
        billDateReactable.value = newDate;
      }
    }

  }

  registerInitialHandler() {
    logger.i("Registering initial handlers...");
    hubConn.on("receiveInitialBill", (args) {
      logger.i ("receiveInitialBill triggered with args: $args.");
      setGuiValues(args);
    });
    hubConn.on("calculateBill", (args) {
      logger.i ("calculateBill triggered with args: $args.");
      setGuiValues(args);
    });

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
  }

  initServerConnection() async {
    // starts the connection to the server (single hub only)
    logger.i("Setting up server connection to https://localhost:5001...");

    // attempt to start connection and retry if initial connection fails
    try {
      await hubConn.start();
      isReconnecting = false;
      hideBanner?.call();
      logger.i("Server connection setup complete.");
      sendReadings();
    } catch (e) {
      isReconnecting = true;
      showBanner?.call("Server is unavailable", "Retrying connection...");
      logger.e("Failed to connect to server: $e. Retrying connection...");
      Future.delayed(Duration(seconds: 3), initServerConnection);
    }
  }
}
