import 'package:flutter/material.dart';
import 'package:signalr_netcore/hub_connection.dart';
import 'package:signalr_netcore/hub_connection_builder.dart';

class SmartMeterAppPage extends StatefulWidget {
  const SmartMeterAppPage({super.key, required this.title});

  // This widget is the home page of your application. It is stateful, meaning
  // that it has a State object (defined below) that contains fields that affect
  // how it looks.

  // This class is the configuration for the state. It holds the values (in this
  // case the title) provided by the parent (in this case the App widget) and
  // used by the build method of the State. Fields in a Widget subclass are
  // always marked "final".

  final String title;

  @override
  State<SmartMeterAppPage> createState() => _SmartMeterAppPageState();
}

class _SmartMeterAppPageState extends State<SmartMeterAppPage> {
  late HubConnection hubConn;
  int viewCount = 0;
  
  void newConnectionReceived(){
    hubConn.on("updateTotalViews", (views) 
      {
        setState(() {
          viewCount = views?[0] as int;
        });        
      }
    );
  }

  initServerConnection() async
  {
    hubConn = HubConnectionBuilder().withUrl("http://localhost:5006/hubs/userCount").build();

    try {
      await hubConn.start();
    } catch (e) {
      print("Hit this error $e");
    }
    
    hubConn.invoke("NewWindowLoaded");
    
    newConnectionReceived();     
  } 

  @override
  void initState() {
    super.initState();
    initServerConnection();
  }

  @override
  Widget build(BuildContext context) {
    // This method is rerun every time setState is called
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        title: Text(widget.title),
      ),
      body: Center(
        // Center is a layout widget. It takes a single child and positions it
        // in the middle of the parent.
        child: Column(
          // Column is also a layout widget. It takes a list of children and
          // arranges them vertically. By default, it sizes itself to fit its
          // children horizontally, and tries to be as tall as its parent.
          //
          // Column has various properties to control how it sizes itself and
          // how it positions its children. Here we use mainAxisAlignment to
          // center the children vertically; the main axis here is the vertical
          // axis because Columns are vertical (the cross axis would be
          // horizontal).
          //
          // TRY THIS: Invoke "debug painting" (choose the "Toggle Debug Paint"
          // action in the IDE, or press "p" in the console), to see the
          // wireframe for each widget.
          mainAxisAlignment: MainAxisAlignment.center,
          children: <Widget>[
            const Text('Page Views:'),
            Text(
              '$viewCount',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
          ],
        ),
      ),
    );
  }

}
