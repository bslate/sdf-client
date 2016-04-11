// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace SDFExample
{
	[Register ("FontSettingsViewController")]
	partial class FontSettingsViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UISlider angleSlider { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UISlider borderSlider { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UISlider gammaSlider { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UISlider sizeSlider { get; set; }

		void ReleaseDesignerOutlets ()
		{
			if (angleSlider != null) {
				angleSlider.Dispose ();
				angleSlider = null;
			}
			if (borderSlider != null) {
				borderSlider.Dispose ();
				borderSlider = null;
			}
			if (gammaSlider != null) {
				gammaSlider.Dispose ();
				gammaSlider = null;
			}
			if (sizeSlider != null) {
				sizeSlider.Dispose ();
				sizeSlider = null;
			}
		}
	}
}
