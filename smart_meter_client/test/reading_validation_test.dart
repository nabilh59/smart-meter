import 'package:logger/logger.dart';
import 'package:mockito/mockito.dart';
import 'package:smart_meter_client/handler.dart';
import 'package:flutter_test/flutter_test.dart';
import 'mock_hubConn_test.mocks.dart';

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
    late ServerHandler serverHandler;
    late MockLogger mockLogger;
    late MockHubConnection mockConn;

    setUp(() {
      mockLogger = MockLogger();
      serverHandler = ServerHandler(logger: mockLogger);
      mockConn = MockHubConnection();
      serverHandler.hubConn = mockConn;
    });

    test('Valid reading is sent to server', () async {
      when(mockConn.send(any, args: anyNamed('args'))).thenAnswer((_) async => null);      
      await serverHandler.sendReading(22.5);

      verify(mockConn.send("CalculateNewBill", args: [0.0, 22.5])).called(1);
      expect(mockLogger.logs.isEmpty, true);
    });

    test('Verify negative reading triggers validation error', () async {
      double reading = -5.0;
      await serverHandler.sendReading(reading);

      expect(mockLogger.logs.contains("Client-side validation failed: Invalid reading- Must be a positive decimal."), true);
    });

    test('Verify NaN/NotaNumber reading triggers validation error', () async {
      double reading = double.nan;
      await serverHandler.sendReading(reading);

      expect(mockLogger.logs.contains("Client-side validation failed: Invalid reading- Must be a positive decimal."), true);
    });

    test('Verify infinite reading triggers validation error', () async {
      double reading = double.infinity;
      await serverHandler.sendReading(reading);

      expect(mockLogger.logs.contains("Client-side validation failed: Invalid reading- Must be a positive decimal."), true);
    });

  });

}