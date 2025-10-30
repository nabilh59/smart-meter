import 'package:logger/logger.dart';
import 'package:smart_meter_client/handler.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Client API token tests', () {
    late ServerHandler serverHandler;
    final logger = Logger();
    setUp(() {
      serverHandler = ServerHandler(logger: logger);
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