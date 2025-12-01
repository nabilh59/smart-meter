import 'package:flutter_test/flutter_test.dart';
import 'package:smart_meter_client/handler.dart';
import 'package:logger/logger.dart';
import 'package:mockito/mockito.dart';
import 'mock_hub_connection_test.mocks.dart';
import 'package:signalr_netcore/signalr_client.dart';

// mock logger to capture error messages
class MockLogger extends Logger {
  String? lastError;

  @override
  void e(dynamic message, {Object? error, DateTime? time, StackTrace? stackTrace}) {
    lastError = message.toString();
  }

}

void main() {
  group('sendReading', () {
    late MockLogger mockLogger;
    late MockHubConnection mockConn;
    late String? bannerTitle;
    late String? bannerMessage;
    late ServerHandler handler;

    setUp(() {
      mockLogger = MockLogger();      
      mockConn = MockHubConnection();
      bannerTitle = null;
      bannerMessage = null;

      handler = ServerHandler();
      handler.hubConn = mockConn;
      ServerHandler.logger = mockLogger;
      handler.showBanner = (title, message) {
        bannerTitle = title;
        bannerMessage = message;
      };
    });

    test('Verify banner is displayed when server is disconnected', () async {
      when(mockConn.state).thenReturn(HubConnectionState.Disconnected);

      await handler.sendReading(5.0);

      expect(bannerTitle, equals("Server Issue"));
      expect(bannerMessage, equals("The server is down. Please wait for reconnection..."));
      expect(handler.lastReadingTotal, equals(5.0));
      expect(mockLogger.lastError, equals("Server disconnected."));
    });

    test('Verify no banner is displayed when server is connected', () async {
      when(mockConn.state).thenReturn(HubConnectionState.Connected);

      await handler.sendReading(5.0);

      expect(bannerTitle, isNull);
      expect(bannerMessage, isNull);
      expect(handler.lastReadingTotal, equals(0.0));
      expect(mockLogger.lastError, isNull);
    });

  });

}