using System;
using OpenTK;
using UIKit;
using OpenTK.Graphics.ES20;
using CoreGraphics;
using Foundation;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OpenGLES20Example
{
	public class FontMetrics
	{
		public string family;
		public string style;
		public int buffer;
		public int fontSize = 24;
		public Dictionary<string, List<int>> chars;
	}

	public class GLViewController : UIViewController
	{
		string sampleText = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUu";
		string sdfFile = "verdana-ttf.sdf";

		string shader = "SDFShader";
		float fontSize = 18.0f;
		float gamma = 1.0f;

		Matrix4 modelViewMatrix;
		Matrix4 projectionMatrix;
		Matrix4 modelViewProjectionMatrix;

		GLProgram program;

		int posAttribute; // a_pos
		int texcoordAttribute; // a_texcoord
		int matrixUniform; // u_matrix
		int textureUniform; // u_texture
		int texsizeUniform; // u_texsize
		int colorUniform; // u_color
		int bufferUniform; // u_buffer
		int gammaUniform; // u_gamma
		int debugUniform; // u_debug

		nfloat viewWidth;
		nfloat viewHeight;

		FontMetrics fontMetrics;

		uint texture;
		nint textureWidth;
		nint textureHeight;

		List<Vector2> vertexList = new List<Vector2> ();
		List<Vector2> texcoordList = new List<Vector2> ();

		public GLViewController ()
		{
		}

		public void Setup ()
		{
			viewWidth = (float) View.Frame.Size.Width;
			viewHeight = (float) View.Frame.Size.Height;

			projectionMatrix = Matrix4.CreateOrthographic ((float) viewWidth, (float) viewHeight, 0, -1);

			if (!createShaderProgram ())
				throw new Exception ("Failed to load shader program.");

			program.Use ();

			var SDFFileContents = new {
				metrics = Path.Combine(sdfFile, "metrics.json"),
				texture0 = Path.Combine(sdfFile, "texture0.png")
			};

			if (!createFontMetrics (SDFFileContents.metrics))
				throw new Exception ("Failed to load SDF metrics.json file.");

			if (!createFontTexture (SDFFileContents.texture0))
				throw new Exception ("Failed to load SDF texture0.png file.");
		}

		private bool createShaderProgram ()
		{
			program = new GLProgram (shader, shader);

			program.AddAttribute ("a_pos");
			program.AddAttribute ("a_texcoord");

			if (!program.Link ()) {
				Console.WriteLine ("Link failed.");
				Console.WriteLine (String.Format ("Program Log: {0}", program.ProgramLog ()));
				Console.WriteLine (String.Format ("Fragment Log: {0}", program.FragmentShaderLog ()));
				Console.WriteLine (String.Format ("Vertex Log: {0}", program.VertexShaderLog ()));

				program = null;

				return false;
			}

			posAttribute = program.GetAttributeIndex ("a_pos");
			texcoordAttribute = program.GetAttributeIndex ("a_texcoord");
			matrixUniform = program.GetUniformIndex ("u_matrix");
			textureUniform = program.GetUniformIndex ("u_texture");
			texsizeUniform = program.GetUniformIndex ("u_texsize");
			colorUniform = program.GetUniformIndex ("u_color");
			bufferUniform = program.GetUniformIndex ("u_buffer");
			gammaUniform = program.GetUniformIndex ("u_gamma");
			debugUniform = program.GetUniformIndex ("u_debug");

			return true;
		}

		private bool createFontMetrics (string file)
		{
			string json = File.ReadAllText (file);
			if (null == json)
				return false;
			fontMetrics = JsonConvert.DeserializeObject<FontMetrics> (json);
			if (null == fontMetrics)
				return false;
			return true;
		}

		private bool createFontTexture (string file)
		{
			GL.Enable (EnableCap.Blend);
			GL.Enable (EnableCap.Texture2D);
			GL.GenTextures (1, out texture);
			GL.BindTexture (TextureTarget.Texture2D, texture);

			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);

			string extension = Path.GetExtension (file);
			string fileName = Path.GetFileNameWithoutExtension (file);
			string directoryName = Path.GetDirectoryName (file);
			string path = NSBundle.MainBundle.PathForResource (fileName, extension, directoryName);
			NSData texData = NSData.FromFile (path);

			UIImage image = UIImage.LoadFromData (texData);
			if (image == null)
				return false;

			textureWidth = image.CGImage.Width;
			textureHeight = image.CGImage.Height;

			GL.Uniform2 (texsizeUniform, (float) textureWidth, (float) textureHeight);

			CGColorSpace colorSpace = CGColorSpace.CreateDeviceGray ();
			byte [] imageData = new byte[textureWidth * textureHeight * 1];
			CGContext context = new CGBitmapContext  (imageData, textureWidth, textureHeight, 8, 1 * textureWidth, colorSpace,
				CGImageAlphaInfo.None);

			context.TranslateCTM (0, textureHeight);
			context.ScaleCTM (1, -1);
			colorSpace.Dispose ();
			context.ClearRect (new CGRect (0, 0, textureWidth, textureHeight));
			context.DrawImage (new CGRect (0, 0, textureWidth, textureHeight), image.CGImage);

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
			GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
			GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance, (int) textureWidth, (int) textureHeight, 0, PixelFormat.Luminance, PixelType.UnsignedByte, imageData);

			context.Dispose ();

			return true;
		}

		private float measureTextWidth (string text, float fontSize)
		{
			var width = 0;
			var scale = fontSize / fontMetrics.fontSize;
			for (var i = 0; i < text.Length; ++i) {
				var ch = (text [i]).ToString ();
				var glyphMetrics = fontMetrics.chars [ch];
				if (null == glyphMetrics)
					continue;
				
				var advance = glyphMetrics [4];
				width += advance;
			}
			return width * scale;
		}

		private void createText (string text, float fontSize)
		{
			vertexList.Clear ();
			texcoordList.Clear ();

			var scale = fontSize / fontMetrics.fontSize;
			var border = fontMetrics.buffer;
			var textWidth = measureTextWidth (text, fontSize);

			var cursorX = (float) -(textWidth / 2);
			var cursorY = (float) 0;

			for (var i = 0; i < text.Length; ++i) {
				var ch = (text [i]).ToString ();
				var glyph = fontMetrics.chars[ch];
				if (null == glyph)
					continue;
				
				if (glyph.Count < 7)
					continue;
				
				var width = glyph [0];
				var height = glyph [1];
				var left = glyph [2];
				var top = glyph [3];
				var advance = glyph [4];
				var x = glyph [5];
				var y = glyph [6];

				if (width <= 0 || height <= 0)
					continue;

				width += border * 2;
				height += border * 2;

				left -= border;
				top += border;

				var cx = cursorX;
				var cy = cursorY;
				var s = scale;

				vertexList.Add (new Vector2 (cx + s * (left), cy + s * (top - height)));
				vertexList.Add (new Vector2 (cx + s * (left + width), cy + s * (top - height)));
				vertexList.Add (new Vector2 (cx + s * (left), cy + s * (top)));

				vertexList.Add (new Vector2 (cx + s * (left + width), cy + s * (top - height)));
				vertexList.Add (new Vector2 (cx + s * (left), cy + s * (top)));
				vertexList.Add (new Vector2 (cx + s * (left + width), cy + s * (top)));

				var tx = x;
				var ty = textureHeight - (y + height);

				texcoordList.Add (new Vector2 (tx, ty));
				texcoordList.Add (new Vector2 (tx + width, ty));
				texcoordList.Add (new Vector2 (tx, ty + height));

				texcoordList.Add (new Vector2 (tx + width, ty));
				texcoordList.Add (new Vector2 (tx, ty + height));
				texcoordList.Add (new Vector2 (tx + width, ty + height));

				cursorX += s * advance;
			}
		}
		
		private void drawText()
		{
			program.Use ();

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texture);
			GL.Uniform1 (textureUniform, (int) 0);

			GL.Uniform1 (debugUniform, (float) 0.0f);

			var vertices = vertexList.ToArray ();
			GL.VertexAttribPointer (posAttribute, 2, VertexAttribPointerType.Float, false, 0, vertices);
			GL.EnableVertexAttribArray (posAttribute);

			var texcoords = texcoordList.ToArray ();
			GL.VertexAttribPointer (texcoordAttribute, 2, VertexAttribPointerType.Float, false, 0, texcoords);
			GL.EnableVertexAttribArray (texcoordAttribute);

			GL.Uniform1 (gammaUniform, (float) (gamma * 1.4142f / fontSize));

			var fromBuffer = (float) (48.0f / 256);
			GL.Uniform4 (colorUniform, 1.0f, 1.0f, 1.0f, 1.0f);
			GL.Uniform1 (bufferUniform, fromBuffer);
			GL.DrawArrays (BeginMode.Triangles, 0, vertices.Length);

			var toBuffer = (float) (192.0f / 256);
			GL.Uniform4 (colorUniform, 0.0f, 0.0f, 0.0f, 1.0f);
			GL.Uniform1 (bufferUniform, toBuffer);
			GL.DrawArrays (BeginMode.Triangles, 0, vertices.Length);
		}

		public void Draw ()
		{
			GL.ClearColor (0.9f, 0.9f, 0.9f, 1f);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.BlendFuncSeparate (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha,
				BlendingFactorSrc.One, BlendingFactorDest.One);

			createText (sampleText, fontSize);

			modelViewMatrix = Matrix4.Identity;
			modelViewProjectionMatrix = Matrix4.Mult (projectionMatrix, modelViewMatrix);
			GL.UniformMatrix4 (matrixUniform, false, ref modelViewProjectionMatrix);

			drawText ();
		}

		public override void LoadView ()
		{
			GLView view = new GLView ();
			view.Controller = this;
			View = view;
		}
	}
}
