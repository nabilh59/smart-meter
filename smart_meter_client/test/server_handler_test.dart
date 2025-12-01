import 'package:flutter_test/flutter_test.dart';
import 'package:smart_meter_client/handler.dart';
import 'package:logger/logger.dart';
import 'package:mockito/mockito.dart';
import 'mock_hub_connection_test.mocks.dart';

// mock logger to capture error messages
class MockLogger extends Logger {
  String? lastError;

  @override
  void e(dynamic message, {Object? error, DateTime? time, StackTrace? stackTrace}) {
    lastError = message.toString();
  }

}

void main() {
  test('Server error message is received by client and logged correctly', () {
    final mockLogger = MockLogger();
    ServerHandler.logger = mockLogger;
    final mockConn = MockHubConnection();

    // mock the server sending an error message
    when(mockConn.on(any, any)).thenAnswer((invocation) {
      final procedureName = invocation.positionalArguments[0];
      final callback = invocation.positionalArguments[1];

      // if error event is registered then trigger it with a mock error message
      if (procedureName == "error") {
        callback(["Invalid reading- Must be a positive decimal."]);
      }
    });
    when(mockConn.onreconnected(any)).thenReturn(null);

    final handler = ServerHandler();
    handler.hubConn = mockConn;
    handler.registerInitialHandler();

    // verify error message was logged correctly
    expect(mockLogger.lastError, "Server error: Invalid reading- Must be a positive decimal.");

  });

}