using SharpDX;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftEngine
{
    /// <summary>
    /// Device Class
    /// </summary>
    public class Device
    {
        /// <summary>
        /// The back buffer
        /// </summary>
        private byte[] _backBuffer;

        /// <summary>
        /// The Bitmap source
        /// </summary>
        private WriteableBitmap _bmp;

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        /// <param name="bmp">The BMP.</param>
        public Device(WriteableBitmap bmp)
        {
            _bmp = bmp;

            // the back buffer size is equal to the number of pixels to draw
            // on screen (width*height) * 4 (R,G,B & Alpha values).
            _backBuffer = new byte[_bmp.PixelWidth * _bmp.PixelHeight * 4];
        }

        /// <summary>
        /// This method is called to clear the back buffer with a specific color
        /// </summary>
        /// <param name="r">The red.</param>
        /// <param name="g">The green.</param>
        /// <param name="b">The blue.</param>
        /// <param name="a">The alpha.</param>
        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (var index = 0; index < _backBuffer.Length; index += 4)
            {
                _backBuffer[index] = b;
                _backBuffer[index + 1] = g;
                _backBuffer[index + 2] = r;
                _backBuffer[index + 3] = a;
            }
        }

        /// <summary>
        /// Once everything is ready , this flush the back buffer
        /// </summary>
        public void Present()
        {
            using (var stream = _bmp.PixelBuffer.AsStream())
            {
                // writing our byte[] back buffer into our WriteableBitmap stream
                stream.Write(_backBuffer, 0, _backBuffer.Length);
            }

            // request a redraw of the entire bitmap
            _bmp.Invalidate();
        }

        /// <summary>
        /// Puts the pixel on the screen at a specific X,Y Coordinates
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="color">The color.</param>
        public void PutPixel(int x, int y, Color4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen
            var index = (x + y * _bmp.PixelWidth) * 4;

            _backBuffer[index] = (byte)(color.Blue * 255);
            _backBuffer[index + 1] = (byte)(color.Green * 255);
            _backBuffer[index + 2] = (byte)(color.Red * 255);
            _backBuffer[index + 3] = (byte)(color.Alpha * 255);
        }

        /// <summary>
        /// Project takes some 3D coordinates and transform them
        /// in 2D coordinates using the transformation matrix
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="transformationMatrix">The transformation matrix.</param>
        /// <returns></returns>
        public Vector2 Project(Vector3 coordinates, Matrix transformationMatrix)
        {
            var point = Vector3.TransformCoordinate(coordinates, transformationMatrix);

            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point.X * _bmp.PixelWidth + _bmp.PixelWidth / 2.0f;
            var y = -point.Y * _bmp.PixelHeight + _bmp.PixelHeight / 2.0f;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Draws the point.
        /// </summary>
        /// <param name="point">The point.</param>
        public void DrawPoint(Vector2 point)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < _bmp.PixelWidth && point.Y < _bmp.PixelHeight)
            {
                PutPixel((int)point.X, (int)point.Y, Color.Yellow);
            }
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovRH(
                0.78f,
                (float)_bmp.PixelWidth / _bmp.PixelHeight,
                0.01f, 1.0f);

            foreach (Mesh mesh in meshes)
            {
                // Beware to apply rotation before translation
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y,
                                                              mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                foreach (var vertex in mesh.Vertices)
                {
                    // First, we project the 3D coordinates into the 2D space
                    var point = Project(vertex, transformMatrix);
                    // Then we can draw on screen
                    DrawPoint(point);
                }
            }
        }
    }
}