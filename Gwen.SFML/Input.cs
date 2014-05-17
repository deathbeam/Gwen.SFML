using System;
using Gwen.Control;
using SFML.Graphics;
using SFML.Window;

namespace Gwen.Input.SFML
{
	/// <summary>
	/// Helper class to avoid modifying SFML itself.
	/// Build it in SFML::RenderWindow.MouseButtonPressed/Released and pass to ProcessMessage.
	/// </summary>
	public class GwenMouseButtonEventArgs : EventArgs
	{
		/// <summary>
		/// SFML event args.
		/// </summary>
		public readonly MouseButtonEventArgs Args;

		/// <summary>
		/// Indicates whether the button is pressed (true) or released (false).
		/// </summary>
		public readonly bool Down;

		public GwenMouseButtonEventArgs(MouseButtonEventArgs args, bool down)
		{
			Args = args;
			Down = down;
		}
	}

	/// <summary>
	/// Helper class to avoid modifying SFML itself.
	/// Build it in SFML::RenderWindow.KeyPressed/Released and pass to ProcessMessage.
	/// </summary>
	public class GwenKeyEventArgs : EventArgs
	{
		public readonly KeyEventArgs Args;

		/// <summary>
		/// Indicates whether the key is pressed (true) or released (false).
		/// </summary>
		public readonly bool Down;

		public GwenKeyEventArgs(KeyEventArgs args, bool down)
		{
			Args = args;
			Down = down;
		}
	}

	/// <summary>
	/// SFML input handler.
	/// </summary>
	public class GwenInput
	{
		private Canvas _canvas;
		private int _mouseX;
		private int _mouseY;
		private RenderTarget _target;

		/// <summary>
		/// Creates new instance of GuiIpnut class.
		/// </summary>
		/// <param name="canvas">Canvas to use.</param>
		/// <param name="target">Rander target (needed for scaling).</param>
		public GwenInput(RenderTarget target, Canvas canvas)
		{
			_canvas = canvas;
			_target = target;
		}

		/// <summary>
		/// Translates control key's SFML key code to GWEN's code.
		/// </summary>
		/// <param name="sfKey">SFML key code.</param>
		/// <returns>GWEN key code.</returns>
		private static Key TranslateKeyCode(Keyboard.Key sfKey)
		{
			switch (sfKey)
			{
			case Keyboard.Key.Back: return Key.Backspace;
			case Keyboard.Key.Return: return Key.Return;
			case Keyboard.Key.Escape: return Key.Escape;
			case Keyboard.Key.Tab: return Key.Tab;
			case Keyboard.Key.Space: return Key.Space;
			case Keyboard.Key.Up: return Key.Up;
			case Keyboard.Key.Down: return Key.Down;
			case Keyboard.Key.Left: return Key.Left;
			case Keyboard.Key.Right: return Key.Right;
			case Keyboard.Key.Home: return Key.Home;
			case Keyboard.Key.End: return Key.End;
			case Keyboard.Key.Delete: return Key.Delete;
			case Keyboard.Key.LControl: return Key.Control;
			case Keyboard.Key.LAlt: return Key.Alt;
			case Keyboard.Key.LShift: return Key.Shift;
			case Keyboard.Key.RControl: return Key.Control;
			case Keyboard.Key.RAlt: return Key.Alt;
			case Keyboard.Key.RShift: return Key.Shift;
			}
			return Key.Invalid;
		}

		/// <summary>
		/// Translates alphanumeric SFML key code to character value.
		/// </summary>
		/// <param name="sfKey">SFML key code.</param>
		/// <returns>Translated character.</returns>
		private static char TranslateChar(Keyboard.Key sfKey)
		{
			if (sfKey >= Keyboard.Key.A && sfKey <= Keyboard.Key.Z)
				return (char)('A' + (int)sfKey);
			return ' ';
		}

		/// <summary>
		/// Main entrypoint for processing input events. Call from your RenderWindow's event handlers.
		/// </summary>
		/// <param name="args">SFML input event args: can be MouseMoveEventArgs, SFMLMouseButtonEventArgs, MouseWheelEventArgs, TextEventArgs, SFMLKeyEventArgs.</param>
		/// <returns>True if the event was handled.</returns>
		public bool ProcessMessage(EventArgs args)
		{
			if (null == _canvas) return false;

			if (args is MouseMoveEventArgs)
			{
				var ev = args as MouseMoveEventArgs;

				if (_target != null)
				{
					Vector2f coord = _target.MapPixelToCoords(new Vector2i(ev.X, ev.Y));
					ev.X = (int)Math.Floor(coord.X);
					ev.Y = (int)Math.Floor(coord.Y);
				}

				int dx = ev.X - _mouseX;
				int dy = ev.Y - _mouseY;

				_mouseX = ev.X;
				_mouseY = ev.Y;

				return _canvas.Input_MouseMoved(_mouseX, _mouseY, dx, dy);
			}

			if (args is GwenMouseButtonEventArgs)
			{
				var ev = args as GwenMouseButtonEventArgs;
				return _canvas.Input_MouseButton((int)ev.Args.Button, ev.Down);
			}

			if (args is MouseWheelEventArgs)
			{
				var ev = args as MouseWheelEventArgs;
				return _canvas.Input_MouseWheel(ev.Delta * 60);
			}

			if (args is TextEventArgs)
			{
				var ev = args as TextEventArgs;
				// [omeg] following may not fit in 1 char in theory
				return _canvas.Input_Character(ev.Unicode[0]);
			}

			if (args is GwenKeyEventArgs)
			{
				var ev = args as GwenKeyEventArgs;

				if (ev.Args.Control && ev.Args.Alt && ev.Args.Code == Keyboard.Key.LControl)
					return false; // this is AltGr

				char ch = TranslateChar(ev.Args.Code);
				if (ev.Down && InputHandler.DoSpecialKeys(_canvas, ch))
					return false;

				Key key = TranslateKeyCode(ev.Args.Code);
				if (key == Key.Invalid && !ev.Down) // it's not special char and it's been released
					return InputHandler.HandleAccelerator(_canvas, ch);
				//return _canvas.Input_Character(ch);)

				return _canvas.Input_Key(key, ev.Down);
			}

			throw new ArgumentException("Invalid event args", "args");
		}
	}
}
