import 'dart:ffi';

import 'package:flutter/material.dart';
import 'package:reactable/reactable.dart';
import 'package:signalr_netcore/hub_connection.dart';
import 'package:signalr_netcore/hub_connection_builder.dart';
import 'package:smart_meter_client/handler.dart';

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

class _SmartMeterConnectingPage extends StatelessWidget {
  final String text;
  final double totalBill;
  final Widget icon;
  const _SmartMeterConnectingPage({
    required this.text,
    required this.totalBill,
    required this.icon,
    Key? key,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          icon,
          Text(text, style: const TextStyle(fontSize: 20)),
          const Text('Total Bill:'),
          const SizedBox(height: 20),
          Text("$totalBill", style: const TextStyle(fontSize: 20)),
        ],
      ),
    );
  }
}

class _SmartMeterAppPageState extends State<SmartMeterAppPage> {
  ServerHandler handler = ServerHandler(); 
  double bill = 0.0;

  void sendReading() async{
      //bill = await handler.sendReading(bill, 10.0);
      //print("new reading sent");
      //setState(() {});     
  }

  void getFirstBill() async{
    //double firstBill = await handler.initServerConnection();
    //print("connected");
      //setState(() {bill = firstBill;});     
  }

  @override
  void initState() {
    super.initState();
    getFirstBill();
  }

  @override
  Widget build(BuildContext context) {
    // This method is rerun every time setState is called
    return  Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        title: Text(widget.title),
      ),
      body: FutureBuilder(future: handler.initServerConnection(), builder: (context, snapshot) {
        if (snapshot.hasError) {
          return _SmartMeterConnectingPage(
            text: snapshot.error.toString(),
            totalBill: 0.0,
            icon: const Icon(Icons.error, color: Colors.red),
          );
        }

        if (snapshot.connectionState == ConnectionState.done) {
          // Future.delayed(const Duration(seconds: 15), () {
          //   handler.sendReading(bill, 10.0);
          // });
          // return _SmartMeterConnectingPage(
          //   text: "Connected",
          //   totalBill: handler.bill,
          //   icon: const Icon(Icons.done, color: Colors.green),
          // );
          Future.delayed(const Duration(seconds: 1), () {
            Navigator.of(context).pushReplacement(
              MaterialPageRoute(builder: (_) => _SmartMeterHomePage(handler: handler),)
              );
          });
        }

        return _SmartMeterConnectingPage(
            text: "Loading",
            totalBill: 0.0,
            icon: const Icon(Icons.screenshot_monitor_rounded, color: Colors.grey),
          );
      }
      ),
      //  Center(
      //   // Center is a layout widget. It takes a single child and positions it
      //   // in the middle of the parent.
      //   child: Column(
      //     // Column is also a layout widget. It takes a list of children and
      //     // arranges them vertically. By default, it sizes itself to fit its
      //     // children horizontally, and tries to be as tall as its parent.
      //     //
      //     // Column has various properties to control how it sizes itself and
      //     // how it positions its children. Here we use mainAxisAlignment to
      //     // center the children vertically; the main axis here is the vertical
      //     // axis because Columns are vertical (the cross axis would be
      //     // horizontal).
      //     //
      //     // TRY THIS: Invoke "debug painting" (choose the "Toggle Debug Paint"
      //     // action in the IDE, or press "p" in the console), to see the
      //     // wireframe for each widget.
      //     mainAxisAlignment: MainAxisAlignment.center,
      //     children: <Widget>[
      //       const Text('New Bill:'),
      //       Text(
      //         '$bill',
      //         style: Theme.of(context).textTheme.headlineMedium,
      //       ),
      //       TextButton(
      //         style: ButtonStyle(
      //           foregroundColor: WidgetStateProperty.all<Color>(Colors.blue),
      //         ),
      //         onPressed: sendReading,
      //         child: Text('Make new reading'),
      //       )
      //     ],
      //   ),
      // ),
    );
  }
}

class _SmartMeterHomePage extends StatelessWidget {
  final ServerHandler handler;
  const _SmartMeterHomePage({Key? key, required this.handler}) : super(key: key);
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        title: Text("Smart Meter"),
      ),
      body: Center( 
            child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Text('Total Bill:'),
              const SizedBox(height: 20),
              Scope(
                builder: (_) => Text("${handler.billReactable.value}", style: const TextStyle(fontSize: 20)),
                ),
              TextButton(
                style: ButtonStyle(
                  foregroundColor: WidgetStateProperty.all<Color>(Colors.blue),
                ),
                onPressed: () async{ await handler.sendReading(10);} ,
                child: Text('Make new reading'),
              )
            ],
          ),
          ),
        );
  }
}
