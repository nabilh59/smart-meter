import 'package:logger/logger.dart';
import 'package:smart_meter_client/handler.dart';
import 'package:flutter_test/flutter_test.dart';

// mock logger to capture error messages
class MockLogger extends Logger {
  String? lastError;

  @override
  void e(dynamic message, {Object? error, DateTime? time, StackTrace? stackTrace}) {
    lastError = message.toString();
  }

}
void main() {
  group('Client API token tests', () {
    final mockLogger = MockLogger();    
    late ServerHandler serverHandler;

    setUp(() {
      serverHandler = ServerHandler();
      ServerHandler.logger = mockLogger;
    });

    // confirm clientAPIToken is correctly initialised to 'client-api-token'
    test('Verify Client API token is correctly assigned', () {
      expect(serverHandler.clientAPIToken, equals("client-api-token"));
    });
    
    // check accessTokenFactory returns the correct token when called
    // verifies token used for authentication matches token assigned to client instance
    test('Verify accessTokenFactory returns correct token', () async {
      final token = await serverHandler.httpConOptions.accessTokenFactory!();
      expect(token, equals("client-api-token"));
    });

  });
  
}