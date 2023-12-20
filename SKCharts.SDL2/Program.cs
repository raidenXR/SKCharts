using System;
using static SDL2.SDL;

namespace SKCharts.SDL2;

public class Program
{
	public static void Main()
	{
		var window = SDL_CreateWindow("SDL2 window", 0, 0, 800, 600, SDL_WindowFlags.SDL_WINDOW_OPENGL);
		SDL_Init(SDL_INIT_VIDEO);
	}
}
