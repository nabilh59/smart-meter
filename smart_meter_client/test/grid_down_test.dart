// test/grid_down_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:smart_meter_client/handler.dart';
import 'mock_hubConn_test.mocks.dart';

void main() {
  group('Grid status tests', () {
    // mock signlr hubconnection so that we dont connect to a real server
    late MockHubConnection mockConn;
    late ServerHandler handler;
    late void Function(List<Object?>? args) gridStatusCallback;

    setUp(() {
      mockConn = MockHubConnection();

      // intercept the callback and store it for later use
      when(mockConn.on(any, any)).thenAnswer((invocation) {
        final method = invocation.positionalArguments[0] as String;
        final cb = invocation.positionalArguments[1] as void Function(List<Object?>?);
        if (method == "gridStatus") {
          gridStatusCallback = cb;
        }
      });

      when(mockConn.onreconnected(any)).thenReturn(null);
      handler = ServerHandler(injected: mockConn);
    });

    test('Grid DOWN pauses readings', () {
      // used to check if the banner callback is triggered
      bool bannerShown = false;
      handler.showBanner = (_, __) => bannerShown = true;
      // simulate the server sending a grid DOWN message
      gridStatusCallback([
        {"status": "DOWN", "title": "Grid down", "message": "Power outage"}
      ]);
      // the handler should now enter the paused state
      expect(handler.state, TelemetryState.paused);
      // the banner should be shown
      expect(bannerShown, isTrue);
    });

    test('Grid UP resumes readings', () {
      bool bannerHidden = false;
      handler.hideBanner = () => bannerHidden = true;
      // simulate a grid up message from the server
      gridStatusCallback([
        {"status": "UP", "title": "Grid up", "message": "Restored"}
      ]);
      // the handler should now be in normal state
      expect(handler.state, TelemetryState.normal);
      // the banner should be hidden
      expect(bannerHidden, isTrue);
    });
  });
}