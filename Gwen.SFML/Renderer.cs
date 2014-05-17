using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SFML;
using SFML.Graphics;
using SFML.Window;
using Tao.OpenGl;
using Color = SFML.Graphics.Color;
using Image = SFML.Graphics.Image;
using SFMLFont = SFML.Graphics.Font;
using SFMLTexture = SFML.Graphics.Texture;

namespace Gwen.Renderer.SFML
{
	/// <summary>
	/// SFML renderer.
	/// </summary>
	public class GwenRenderer : Base, ICacheToTexture
	{
		private RenderTarget _target;
		private Color _color;
		private Vector2f _viewScale;
		private RenderStates _renderState;
		private uint _cacheSize;
		private readonly Vertex[] _vertices;

		//Some simple caching for text to draw
		private Text _str;

		public const uint CacheSize = 1024;

		/// <summary>
		/// Initializes a new instance of the <see cref="SFML"/> class.
		/// </summary>
		/// <param name="target">SFML render target.</param>
		public GwenRenderer(RenderTarget target)
		{
			_target = target;
			_vertices = new Vertex[CacheSize];
			_renderState = RenderStates.Default;
		}

		public override void Begin()
		{
			base.Begin();
			var port = _target.GetViewport(_target.GetView());
			var scaled = _target.MapPixelToCoords(new Vector2i(port.Width, port.Height));
			_viewScale.X = (port.Width/scaled.X)*Scale;
			_viewScale.Y = (port.Height/scaled.Y)*Scale;
			_str = new Text ();
		}

		public override void End()
		{
			FlushCache();
			_str.Dispose ();
			base.End();
		}

		/// <summary>
		/// Cache to texture provider.
		/// </summary>
		public override ICacheToTexture CTT
		{
			get { return this; }
		}

		/// <summary>
		/// Gets or sets the current drawing color.
		/// </summary>
		public override System.Drawing.Color DrawColor
		{
			get { return System.Drawing.Color.FromArgb(_color.A, _color.R, _color.G, _color.B); }
			set { _color = new Color(value.R, value.G, value.B, value.A); }
		}

		public override System.Drawing.Color PixelColor(Texture texture, uint x, uint y, System.Drawing.Color defaultColor)
		{
			var tex = texture.RendererData as SFMLTexture;
			if (tex == null)
				return defaultColor;
			var img = tex.CopyToImage();
			var pixel = img.GetPixel(x, y);
			return System.Drawing.Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
		}

		/// <summary>
		/// Loads the specified font.
		/// </summary>
		/// <param name="font">Font to load.</param>
		/// <returns>True if succeeded.</returns>
		public override bool LoadFont(Font font)
		{
			font.RealSize = font.Size * Scale;

			var sfFont = new SFMLFont(font.FaceName);
			font.RendererData = sfFont;

			return true;
		}

		/// <summary>
		/// Frees the specified font.
		/// </summary>
		/// <param name="font">Font to free.</param>
		public override void FreeFont(Font font)
		{
			if ( font.RendererData == null )
				return;

			var sfFont = font.RendererData as SFMLFont;
		    if (sfFont != null) sfFont.Dispose();
		    font.RendererData = null;
		}

		/// <summary>
		/// Returns dimensions of the text using specified font.
		/// </summary>
		/// <param name="font">Font to use.</param>
		/// <param name="text">Text to measure.</param>
		/// <returns>
		/// Width and height of the rendered text.
		/// </returns>
		public override Point MeasureText(Font font, string text)
		{
			var sfFont = font.RendererData as SFMLFont;

			// If the font doesn't exist, or the font size should be changed
			if (sfFont == null || Math.Abs(font.RealSize - font.Size * Scale) > 2)
			{
				FreeFont(font);
				LoadFont(font);
			}

			sfFont = font.RendererData as SFMLFont;

			// todo: this is workaround for SFML.Net bug under mono
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				if (text[text.Length - 1] != '\0')
					text += '\0';
			}

			var extents = new Point(0, sfFont.GetLineSpacing((uint)font.RealSize));
			var prev = '\0';

			foreach (var cur in text)
			{
			    prev = cur;
			    if (cur == '\n' || cur == '\v')
			        continue;
			    extents.X += sfFont.GetGlyph(cur, (uint) font.RealSize, false).Advance;
			}

			return extents;
		}

		public override void RenderText(Font font, Point pos, string text)
		{
			pos = Translate(pos);
			var sfFont = font.RendererData as SFMLFont;

			// If the font doesn't exist, or the font size should be changed
			if (sfFont == null || Math.Abs(font.RealSize - font.Size * Scale) > 2)
			{
				FreeFont(font);
				LoadFont(font);
			}

			// todo: this is workaround for SFML.Net bug under mono
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				if (text[text.Length - 1] != '\0')
					text += '\0';
			}

			_str.DisplayedString = text;
			_str.Font = sfFont;
			_str.Position = new Vector2f (pos.X, pos.Y);
			_str.CharacterSize = (uint) font.RealSize;
			_str.Color = _color;
			_target.Draw(_str);
		}
		
        public override void DrawLine(int x1, int y1, int x2, int y2)
        {
			Translate(ref x1, ref y1);
            Translate(ref x2, ref y2);

            Vertex[] line = {
				new Vertex(new Vector2f(x1, y1), _color),
				new Vertex(new Vector2f(x2, y2), _color)};

			_target.Draw(line, PrimitiveType.Lines, _renderState);
        }

		public override void DrawFilledRect(Rectangle rect)
		{
			rect = Translate(rect);

			if (_renderState.Texture != null || _cacheSize + 4 >= CacheSize)
			{
				FlushCache();
				_renderState.Texture = null;
			}

			var right = rect.X + rect.Width;
			var bottom = rect.Y + rect.Height;

			_vertices[_cacheSize++] = new Vertex(new Vector2f(rect.X, rect.Y), _color);
			_vertices[_cacheSize++] = new Vertex(new Vector2f(right, rect.Y), _color);
			_vertices[_cacheSize++] = new Vertex(new Vector2f(right, bottom), _color);
			_vertices[_cacheSize++] = new Vertex(new Vector2f(rect.X, bottom), _color);  
		}

		public override void DrawTexturedRect(Texture t, Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
		{
			var tex = t.RendererData as SFMLTexture;
			if (tex == null)
			{
				DrawMissingImage(rect);
				return;
			}

			DrawTexturedRect(tex, rect, u1, v1, u2, v2);
		}

		protected void DrawTexturedRect(SFMLTexture tex, Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
		{
			rect = Translate(rect);

			u1 *= tex.Size.X;
			v1 *= tex.Size.Y;
			u2 *= tex.Size.X;
			v2 *= tex.Size.Y;

			if (_renderState.Texture != tex || _cacheSize + 4 >= CacheSize)
			{
				FlushCache();

				// enable the new texture
				_renderState.Texture = tex;
			}

			var right = rect.X + rect.Width;
			var bottom = rect.Y + rect.Height;

			_vertices[_cacheSize++] = new Vertex(new Vector2f(rect.X, rect.Y), new Vector2f(u1, v1));
			_vertices[_cacheSize++] = new Vertex(new Vector2f(right, rect.Y), new Vector2f(u2, v1));
			_vertices[_cacheSize++] = new Vertex(new Vector2f(right, bottom), new Vector2f(u2, v2));
			_vertices[_cacheSize++] = new Vertex(new Vector2f(rect.X, bottom), new Vector2f(u1, v2));
		}

		public override void LoadTexture(Texture texture)
		{
			if (texture == null) return;

			if (texture.RendererData != null) 
				FreeTexture(texture);

			try
			{
				var sfTexture = new SFMLTexture(texture.Name) {Smooth = true};
			    texture.Width = (int)sfTexture.Size.X;
				texture.Height = (int)sfTexture.Size.Y;
				texture.RendererData = sfTexture;
				texture.Failed = false;
			}
			catch (LoadingFailedException)
			{
				Debug.Print("LoadTexture: failed");
				texture.Failed = true;
			}
		}

		/// <summary>
		/// Initializes texture from image file data.
		/// </summary>
		/// <param name="texture">Texture to initialize.</param>
		/// <param name="data">Image file as stream.</param>
		public override void LoadTextureStream(Texture texture, System.IO.Stream data)
		{
			if (null == texture) return;

			Debug.Print("LoadTextureStream: {0} {1}", texture.Name, texture.RendererData);

			if (texture.RendererData != null)
				FreeTexture(texture);

			try
			{
				var sfTexture = new SFMLTexture(data) {Smooth = true};
			    texture.Width = (int)sfTexture.Size.X;
				texture.Height = (int)sfTexture.Size.Y;
				texture.RendererData = sfTexture;
				texture.Failed = false;
			}
			catch (LoadingFailedException)
			{
				Debug.Print("LoadTextureStream: failed");
				texture.Failed = true;
			}
		}

		// [omeg] added, pixelData are in RGBA format
		public override void LoadTextureRaw(Texture texture, byte[] pixelData)
		{
			if (null == texture) return;

			Debug.Print("LoadTextureRaw: {0}", texture.RendererData);

			if (texture.RendererData != null) 
				FreeTexture(texture);

			try
			{
				var sfTexture = new SFMLTexture(new Image((uint)texture.Width, (uint)texture.Height, pixelData)) {Smooth = true};
			    texture.RendererData = sfTexture;
				texture.Failed = false;
			}
			catch (LoadingFailedException)
			{
				Debug.Print("LoadTextureRaw: failed");
				texture.Failed = true;
			}
		}

		public override void FreeTexture(Texture texture)
		{
			var tex = texture.RendererData as SFMLTexture;

			if (tex != null)
				tex.Dispose();

			Debug.Print("FreeTexture: {0}", texture.Name);

			texture.RendererData = null;
		}

		public override void StartClip()
		{
			FlushCache();
			var clip = ClipRegion;
			clip.X = (int) Math.Round(clip.X*_viewScale.X);
			clip.Y = (int) Math.Round(clip.Y*_viewScale.Y);
			clip.Width = (int) Math.Round(clip.Width*_viewScale.X);
			clip.Height = (int) Math.Round(clip.Height*_viewScale.Y);

			var view = _target.GetView();
			var v = _target.GetViewport(view);
			view.Dispose();
			clip.Y = v.Height - (clip.Y + clip.Height);

			Gl.glScissor(clip.X, clip.Y, clip.Width, clip.Height);
			Gl.glEnable(Gl.GL_SCISSOR_TEST);
		}

		public override void EndClip()
		{
			FlushCache();
			Gl.glDisable(Gl.GL_SCISSOR_TEST);
		}

		private void FlushCache()
		{
			Debug.Assert(_cacheSize % 4 == 0);
			if (_cacheSize > 0)
			{
				_target.Draw(_vertices, 0, _cacheSize, PrimitiveType.Quads, _renderState);
				_cacheSize = 0;
			}
		}

		#region Implementation of ICacheToTexture

		private Dictionary<Control.Base, RenderTexture> _rt;
		private Stack<RenderTarget> _stack;
		private RenderTarget _realRt;

		public void Initialize()
		{
			_rt = new Dictionary<Control.Base, RenderTexture>();
			_stack = new Stack<RenderTarget>();
		}

		public void ShutDown()
		{
			_rt.Clear();
			if (_stack.Count > 0)
				throw new InvalidOperationException("Render stack not empty");
		}

		/// <summary>
		/// Called to set the target up for rendering.
		/// </summary>
		/// <param name="control">Control to be rendered.</param>
		public void SetupCacheTexture(Control.Base control)
		{
			_realRt = _target;
			_stack.Push(_target); // save current RT
			_target = _rt[control]; // make cache current RT
		}

		/// <summary>
		/// Called when cached rendering is done.
		/// </summary>
		/// <param name="control">Control to be rendered.</param>
		public void FinishCacheTexture(Control.Base control)
		{
			_target = _stack.Pop();
		}

		/// <summary>
		/// Called when gwen wants to draw the cached version of the control. 
		/// </summary>
		/// <param name="control">Control to be rendered.</param>
		public void DrawCachedControlTexture(Control.Base control)
		{
			var temp = _target;
			_target = _realRt;
			DrawTexturedRect(_rt[control].Texture, control.Bounds);
			_target = temp;
		}

		/// <summary>
		/// Called to actually create a cached texture. 
		/// </summary>
		/// <param name="control">Control to be rendered.</param>
		public void CreateControlCacheTexture(Control.Base control)
		{
			// initialize cache RT
			if (!_rt.ContainsKey(control))
			{
				var view = new View(new FloatRect(0, 0, control.Width, control.Height));
				_rt[control] = new RenderTexture((uint)control.Width, (uint)control.Height);
				_rt[control].SetView(view);
			}

			_rt[control].Display();
		}

		public void UpdateControlCacheTexture(Control.Base control)
		{
			throw new NotImplementedException();
		}

		public void SetRenderer(Base renderer)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
