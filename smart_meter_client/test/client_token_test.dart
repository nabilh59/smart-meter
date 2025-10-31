import 'package:smart_meter_client/handler.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Client API token tests', () {
    late ServerHandler serverHandler;

    setUp(() {
      serverHandler = ServerHandler();
    });

    // confirm clientAPIToken is correctly initialised to 'client-api-token'
    test('Verify Client API token is correctly assigned', () {
      expect(serverHandler.clientAPIToken, equals("client-api-token"));
    });
    
    // check accessTokenFactory returns the correct token used by the client for authentication
    // verify token used for authentication matches token assigned to client instance
    test('Verify accessTokenFactory returns correct token', () async {
      final goodToken = await serverHandler.httpConOptions.accessTokenFactory!();
      final isValidToken = serverHandler.validateToken(goodToken);
      expect(isValidToken, isTrue);
    });

    test('Verify invalid token fails authentication', () async {
      final isValidToken = serverHandler.validateToken("invalid-token");
      expect(isValidToken, isFalse);
    });

  });
  
}