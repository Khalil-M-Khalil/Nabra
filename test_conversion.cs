using System;
using Nabra.Core.Services;
using Nabra.Core.Models;

var layout = new KeyboardLayoutService();
var detector = new DirectionDetectionService();
var conv = new TextConversionService(layout, detector);

var res = conv.Convert(";dt;", ConversionMode.ToArabic);
Console.WriteLine(res.Text);
