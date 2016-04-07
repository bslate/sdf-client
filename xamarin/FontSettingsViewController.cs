using System;
using UIKit;
using Foundation;

namespace OpenGLES20Example
{
	public partial class FontSettingsViewController : UIViewController
	{
		public FontSettingsViewController () : base ("FontSettingsViewController", null)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			// Perform any additional setup after loading the view, typically from a nib.
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}

		public float Size
		{
			get { return sizeSlider.Value; }
			set { sizeSlider.Value = value; }
		}

		public float Border
		{
			get { return borderSlider.Value; }
			set { borderSlider.Value = value; }
		}

		public float Angle
		{
			get { return angleSlider.Value; }
			set { angleSlider.Value = value; }
		}

		public float Gamma
		{
			get { return gammaSlider.Value; }
			set { gammaSlider.Value = value; }
		}

		public Action<float> SizeChanged;
		public Action<float> BorderChanged;
		public Action<float> AngleChanged;
		public Action<float> GammaChanged;

		[Export("sizeSliderChanged:")]
		void SizeSliderChanged(UISlider slider)
		{
			if (null != SizeChanged) {
				SizeChanged (slider.Value);
			}
		}

		[Export("borderSliderChanged:")]
		void BorderSliderChanged(UISlider slider)
		{
			if (null != BorderChanged) {
				BorderChanged (slider.Value);
			}
		}

		[Export("angleSliderChanged:")]
		void AngleSliderChanged(UISlider slider)
		{
			if (null != AngleChanged) {
				AngleChanged (slider.Value);
			}
		}

		[Export("gammaSliderChanged:")]
		void GammaSliderChanged(UISlider slider)
		{
			if (null != GammaChanged) {
				GammaChanged (slider.Value);
			}
		}
	}
}
