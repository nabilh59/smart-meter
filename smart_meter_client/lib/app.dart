import 'package:flutter/material.dart';
import 'package:smart_meter_client/home_page.dart';

// stateless meaning it holds no data, just puts elements to the screen
class MyApp extends StatelessWidget {
  const MyApp({super.key});

  // This widget is the root of your application.
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Smart Meter App',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.lightGreen),
      ),
      home: const SmartMeterAppPage(title: 'Smart Meter App'),
    );
  }
}