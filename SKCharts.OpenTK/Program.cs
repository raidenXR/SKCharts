using System;
using System.Numerics;
using System.Threading;



namespace SKCharts.OpenTK;


public class Program
{
	public static void Main()
	{
		var window = new Window("renderer", 800, 600);
		var thread = new Thread(window.Run);
		thread.Start();

		Console.ReadKey();
	}
}
