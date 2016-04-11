# Signed Distance Field for OpenGL ES 2.0 Text

This project shows how to use signed distance fields (SDFs) to use text in OpenGL. It is written in C# using Xamarin to build a Xamarin.iOS demo. It should be easily adaptable to Android or other platforms.

This project expects an SDF texture with a JSON descriptor. Refer to the [`sdf-cartographer`](https://github.com/zotebook/sdf-cartographer) project for tooling to convert TTF and OTF fonts into the format this example can use.

![Screenshot](https://raw.githubusercontent.com/zotebook/sdf-client/master/xamarin/screenshot.png)

This project is based on [Mapbox's signed distance field shader](https://www.mapbox.com/blog/text-signed-distance-fields/).
