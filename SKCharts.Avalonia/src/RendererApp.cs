using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.Themes.Fluent;
using SKCharts;

namespace SKCharts.Avalonia;



public class MainWindow : Window {
	public MainWindow(){}

	public void SetContentDispatch(Control control)	{
		Dispatcher.UIThread.Invoke((() => {Content = control;}));
	}	
}

public class App : Application {
	public override void Initialize() {
		this.Styles.Add(new FluentTheme());
	}	

    public override void OnFrameworkInitializationCompleted() {
		if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.MainWindow = new MainWindow();
		}
        base.OnFrameworkInitializationCompleted();
    }
}

public class MainWindow2 : Window {
	public MainWindow2() {
		this.Content = new SKChart2DView();
	}
	
}

public class App2 : Application {
    public override void Initialize() {
        this.Styles.Add(new FluentTheme());           		
		// base.Initialize();
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow2();
        }

        base.OnFrameworkInitializationCompleted();
    }

}

public class MainWindow3 : Window {
	public MainWindow3() {
		this.Content = new SKChart3DView();
	}
}

public class App3 : Application {
	public override void Initialize() {
		this.Styles.Add(new FluentTheme());
	}

    public override void OnFrameworkInitializationCompleted() {
		if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.MainWindow = new MainWindow3();
		}
        base.OnFrameworkInitializationCompleted();
    }
}


// https://github.com/mrakgr/Lithe-POC/blob/master/Lithe%20Avalonia%20Examples/00_HelloWorld.fs

public static class RendererApp
{
	public static Thread OpenWindow2D()	{
		var thread = new Thread(StartNewWindow2DView);
		return thread;
	}

	[STAThread]
	private static void StartNewWindow2DView()
	{
		var builder = AppBuilder.Configure<App2>().UsePlatformDetect().WithInterFont();
		builder.StartWithClassicDesktopLifetime(null);		
	}

	public static Thread OpenWindow3D() {
		var thread = new Thread(StartNewWindow3DView);
		return thread;
	}

	[STAThread]
	private static void StartNewWindow3DView()
	{
		var builder = AppBuilder.Configure<App3>().UsePlatformDetect().WithInterFont();
		builder.StartWithClassicDesktopLifetime(null);
	}

	/// <summary> will throw Exception of calling from an Invalid thread </summary>
	public static Window GetMainWindow()
	{
		if(Application.Current!.ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop) 
		{
			return desktop.MainWindow ?? throw new NullReferenceException("not classic desktop application?");
		} 
		else 
		{
			throw new NullReferenceException("main window is null");
		}
	}

}

public static class Renderer
{
	public static Window OpenWindow(UserControl content)
	{
		var builder = AppBuilder.Configure<Application>().UsePlatformDetect();
		builder.StartWithClassicDesktopLifetime(null, ShutdownMode.OnMainWindowClose);

		if(Application.Current.ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop)
		{
			var window = desktop.MainWindow ?? throw new NullReferenceException("not classic desktop application");
			window.Content = content;
			return window;
		}
		else
		{
			throw new NullReferenceException("main window is null");
		}
	}
}
