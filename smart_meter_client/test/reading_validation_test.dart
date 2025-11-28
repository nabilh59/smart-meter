import 'package:logger/logger.dart';
import 'package:mockito/mockito.dart';
import 'package:smart_meter_client/handler.dart';
import 'package:flutter_test/flutter_test.dart';
import 'mock_hubConn_test.mocks.dart';
import 'package:signalr_netcore/signalr_client.dart';

class MockLogger extends Logger {
  final List<String> logs = [];
  MockLogger() : super(printer: PrettyPrinter());

  @override
  void e(dynamic message, {Object? error, DateTime? time, StackTrace? stackTrace}) {
    logs.add(message.toString());
    super.e(message, error: error, time: time, stackTrace: stackTrace);
  }
}

void main() {
  group('Reading validation tests', () {
    late MockLogger mockLogger;
    late ServerHandler serverHandler;
    late MockHubConnection mockConn;

    setUp(() {
      mockLogger = MockLogger();
      ServerHandler.logger = mockLogger;
      mockConn = MockHubConnection();
      serverHandler = ServerHandler();
      serverHandler.hubConn = mockConn;

      when(mockConn.on(any, any)).thenAnswer((invocation) {});

      when(mockConn.onreconnected(any)).thenReturn(null);
      when(mockConn.state).thenReturn(HubConnectionState.Connected);
      serverHandler.state = TelemetryState.normal;
    });

    test('Verify valid reading is sent to server', () async {
      when(mockConn.send(any, args: anyNamed('args'))).thenAnswer((_) async => {});
      serverHandler.billReactable.value = "0.0";
      await serverHandler.sendReading(22.5);
      verify(mockConn.send("CalculateNewBill", args: anyNamed('args'))).called(1);
      expect(mockLogger.logs.isEmpty, true);
    });

    test('Verify negative reading triggers validation error', () async {
      await serverHandler.sendReading(-5.0);
      expect(mockLogger.logs.any((log) => log.contains("Client-side validation failed")), true);
    });

    test('Verify NaN/NotaNumber reading triggers validation error', () async {
      await serverHandler.sendReading(double.nan);
      expect(mockLogger.logs.any((log) => log.contains("Client-side validation failed")), true);
    });

    test('Verify infinite reading triggers validation error', () async {
      await serverHandler.sendReading(double.infinity);
      expect(mockLogger.logs.any((log) => log.contains("Client-side validation failed")), true);
    });

  });

}