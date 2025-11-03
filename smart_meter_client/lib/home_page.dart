import 'package:flutter/material.dart';
import 'package:reactable/reactable.dart';
import 'package:smart_meter_client/handler.dart';

class SmartMeterAppPage extends StatefulWidget {
  const SmartMeterAppPage({super.key, required this.title});

  final String title;

  @override
  State<SmartMeterAppPage> createState() => _SmartMeterAppPageState();
}

class _SmartMeterConnectingPage extends StatelessWidget {
  final String text;
  final Widget icon;
  const _SmartMeterConnectingPage({
    required this.text,
    required this.icon,
    super.key,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          icon,
          Text(text, style: const TextStyle(fontSize: 20)),          
        ],
      ),
    );
  }
}


class _SmartMeterAppPageState extends State<SmartMeterAppPage> {
  ServerHandler handler = ServerHandler(); // how this interacts with the server

  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return  Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        title: Text(widget.title),
      ),
      // The client connects to the server using handler.initServerConnection()
      body: FutureBuilder(future: handler.initServerConnection(), builder: (context, snapshot) {
        
        // if there was an issue connecting to the server (i.e. an error in handler.initServerConnection()), 
        //  show an error version of the _SmartMeterConnectingPage
        if (snapshot.hasError) { 
          return _SmartMeterConnectingPage(
            text: snapshot.error.toString(),
            icon: const Icon(Icons.error, color: Colors.red),
          );
        }

        // if the connection to the server was successful, 
        //  wait 1 second, show the _SmartMeterHomePage and start sending readings with handler.sendReadings()
        if (snapshot.connectionState == ConnectionState.done) {          
          Future.delayed(const Duration(seconds: 1), () {
            Navigator.of(context).pushReplacement(
              MaterialPageRoute(builder: (_) => 
                _SmartMeterHomePage(handler: handler, title: widget.title),
              )
            );
            handler.sendReadings();
          });
        }

        // in every other situation, 
        //  show a loading version of the _SmartMeterConnectingPage
        return _SmartMeterConnectingPage(
            text: "Loading...",
            icon: const Icon(Icons.wifi_2_bar_outlined , color: Colors.grey),
          );
      }
      ),
    );
  }
}

class _SmartMeterHomePage extends StatefulWidget {
  final ServerHandler handler;
  final String title;
  const _SmartMeterHomePage({
    super.key,
    required this.handler,
    required this.title
  });

  @override
  State<_SmartMeterHomePage> createState() => _SmartMeterHomePageState();
}

class _SmartMeterHomePageState extends State<_SmartMeterHomePage> {
  String? _bannerTitle;
  String? _bannerBody;

  @override
  void initState() {
    super.initState();

    // show a calm, inline banner when grid is DOWN
    widget.handler.showBanner = (title, body) {
      setState(() {
        _bannerTitle = title;
        _bannerBody = body;
      });
    };

    // hide the banner when grid is UP
    widget.handler.hideBanner = () {
      setState(() {
        _bannerTitle = null;
        _bannerBody = null;
      });
    };
  }

  @override
  void dispose() {
    widget.handler.showBanner = null;
    widget.handler.hideBanner = null;
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        title: Text(widget.title),
      ),
      body: Column(
        children: [
          if (_bannerTitle != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              margin: const EdgeInsets.only(bottom: 8),
              decoration: BoxDecoration(
                color: Colors.blue.shade50, // soft info color
                border: Border(
                  left: BorderSide(color: Colors.blue.shade300, width: 4),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(_bannerTitle!, style: const TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 4),
                  Text(_bannerBody!),
                ],
              ),
            ),

          // main content
          Expanded(
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Text('Total Bill:', style: TextStyle(fontSize: 20)),
                  const SizedBox(height: 20),
                  Scope(
                    builder: (_) => Text(
                      "${widget.handler.billReactable.value}",
                      style: const TextStyle(fontSize: 20),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
