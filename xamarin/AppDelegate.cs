using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UIKit;

namespace OpenGLES20Example
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			window = new UIWindow ();

			GLViewController root = new GLViewController ();

			window.RootViewController = root;

			window.MakeKeyAndVisible ();

			(root.View as GLView).StartAnimation ();

			return true;
		}
	}
}

