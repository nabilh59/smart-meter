import 'package:flutter/material.dart';
import 'package:smart_meter_client/home_page.dart';


class MyApp extends StatelessWidget {
  const MyApp({super.key});

  // This widget is the root of your application. Change the colour of the entire app here.
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