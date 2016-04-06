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
		float fontSize = 26.0f;
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

		List<Vector2> vertexList = new List<Vector2> ();
		List<Vector2> texcoordList = new List<Vector2> ();

		public GLViewController ()
		{
		}

		public void Setup ()
		{
			viewWidth = View.Frame.Size.Width;
			viewHeight = View.Frame.Size.Height;

			projectionMatrix = Matrix4.CreateOrthographic ((float) viewWidth, (float) viewHeight, 0, -1);

			if (!createShaderProgram ())
				throw new Exception ("Failed to load shader program.");

			var SDFFile = new {
				metrics = "arial-ttf.sdf/metrics.json",
				texture0 = "arial-ttf.sdf/texture0.png"
			};

			if (!createFontMetrics (SDFFile.metrics))
				throw new Exception ("Failed to load SDF metrics.json file.");

			if (!createFontTexture (SDFFile.texture0))
				throw new Exception ("Failed to load SDF texture0.png file.");
		}

		private bool createShaderProgram ()
		{
			program = new GLProgram ("SDFShader", "SDFShader");

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
			GL.BlendFuncSeparate (
				BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha,
				BlendingFactorSrc.One, BlendingFactorDest.One);

			GL.Enable (EnableCap.Texture2D);
			GL.GenTextures (1, out texture);

			string extension = Path.GetExtension (file);
			string fileName = Path.GetFileNameWithoutExtension (file);
			string directoryName = Path.GetDirectoryName (file);
			string path = NSBundle.MainBundle.PathForResource (fileName, extension, directoryName);
			NSData texData = NSData.FromFile (path);

			UIImage image = UIImage.LoadFromData (texData);
			if (image == null)
				return false;

			nint width = image.CGImage.Width;
			nint height = image.CGImage.Height;

			CGColorSpace colorSpace = CGColorSpace.CreateGenericGray ();
			byte [] imageData = new byte[height * width];
			CGContext context = new CGBitmapContext  (imageData, width, height, 8, 1 * width, colorSpace,
				CGBitmapFlags.None);

			context.TranslateCTM (0, height);
			context.ScaleCTM (1, -1);
			colorSpace.Dispose ();
			context.ClearRect (new CGRect (0, 0, width, height));
			context.DrawImage (new CGRect (0, 0, width, height), image.CGImage);

			GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance, (int) width, (int) height, 0, PixelFormat.Luminance, PixelType.UnsignedByte, imageData);

			context.Dispose ();

			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);

			GL.Uniform2 (texsizeUniform, width, height);

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
			var padding = fontMetrics.buffer;
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
				
				width += padding * 2;
				height += padding * 2;
				left -= padding;

				var cx = cursorX;
				var cy = cursorY;

				vertexList.Add (new Vector2 (cx + scale * (left), cy - scale * (top)));
				vertexList.Add (new Vector2 (cx + scale * (left + width), cy - scale * (top)));
				vertexList.Add (new Vector2 (cx + scale * (left), cy - scale * (height - top)));

				vertexList.Add (new Vector2 (cx + scale * (left + width), cy - scale * (top)));
				vertexList.Add (new Vector2 (cx + scale * (left), cy - scale * (height - top)));
				vertexList.Add (new Vector2 (cx + scale * (left + width), cy - scale * (height - top)));

				texcoordList.Add (new Vector2 (cx, cy));
				texcoordList.Add (new Vector2 (cx + width, cy));
				texcoordList.Add (new Vector2 (cx, cy + height));

				texcoordList.Add (new Vector2 (cx + width, cy));
				texcoordList.Add (new Vector2 (cx, cy + height));
				texcoordList.Add (new Vector2 (cx + width, cy + height));

				cursorX += advance;
			}

//			vertexList.Add (new Vector2 (0, 0));
//			vertexList.Add (new Vector2 (50, 0));
//			vertexList.Add (new Vector2 (0, 50));
//
//			vertexList.Add (new Vector2 (50, 0));
//			vertexList.Add (new Vector2 (0, 50));
//			vertexList.Add (new Vector2 (50, 50));
//
//			texcoordList.Add (new Vector2 (0, 0));
//			texcoordList.Add (new Vector2 (50, 0));
//			texcoordList.Add (new Vector2 (0, 50));
//
//			texcoordList.Add (new Vector2 (50, 0));
//			texcoordList.Add (new Vector2 (0, 50));
//			texcoordList.Add (new Vector2 (50, 50));

			for (var i = 0; i < vertexList.Count; ++i) {
				Console.WriteLine ("Vertex: {0}, {1}", vertexList [i].X, vertexList[i].Y);
			}
		}
		
		private void drawText()
		{
			program.Use ();

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texture);
			GL.Uniform1 (textureUniform, (int) 0);

			GL.Uniform1 (debugUniform, (float) 1);
			GL.Uniform1 (gammaUniform, (float) (gamma * 1.4142 / fontSize));

			var vertices = vertexList.ToArray ();
			GL.EnableVertexAttribArray (posAttribute);
			GL.VertexAttribPointer (posAttribute, 2, VertexAttribPointerType.Float, false, 0, vertices);

			var texcoords = texcoordList.ToArray ();
			GL.EnableVertexAttribArray (texcoordAttribute);
			GL.VertexAttribPointer (texcoordAttribute, 2, VertexAttribPointerType.Float, false, 0, texcoords);

			var fromBuffer = (float) (48 / 256);
			GL.Uniform4 (colorUniform, 1, 1, 1, 1);
			GL.Uniform1 (bufferUniform, fromBuffer);
			GL.DrawArrays (BeginMode.Triangles, 0, vertices.Length);

			var toBuffer = (float) (192 / 256);
			GL.Uniform4 (colorUniform, 0, 0, 0, 1);
			GL.Uniform1 (bufferUniform, toBuffer);
			GL.DrawArrays (BeginMode.Triangles, 0, vertices.Length);
		}

		public void Draw ()
		{
			GL.ClearColor (0.9f, 0.9f, 0.9f, 1f);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

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
