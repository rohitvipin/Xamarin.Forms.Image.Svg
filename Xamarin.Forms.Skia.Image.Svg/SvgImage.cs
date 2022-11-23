using System;
using System.Linq;
using System.Reflection;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using SKSvg = SkiaSharp.Extended.Svg.SKSvg;

namespace Xamarin.Forms.Image.Svg
{
    public class SvgImage : Frame
    {
        #region Private Members

        private readonly SKCanvasView _canvasView = new SKCanvasView();
        private static Assembly _assemblyCache;

        #endregion

        #region Bindable Properties

        public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(string), typeof(SvgImage), default(string), propertyChanged: RedrawCanvas);

        /// <summary>
        /// Filename of the Source SVG Image
        /// </summary>
        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly BindableProperty TintColorProperty = BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(SvgImage), Color.Transparent, propertyChanged: RedrawCanvas);

        /// <summary>
        /// Color the SVG should be rendered in, set to Transparent for no no alterations.
        /// </summary>
        public Color TintColor
        {
            get => (Color)GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }
        
        #endregion

        #region Constructor

        /// <summary>
        /// Ctor
        /// </summary>
        public SvgImage()
        {
            Padding = new Thickness(0);
            BackgroundColor = Color.Transparent;
            HasShadow = false;
            Content = _canvasView;
            _canvasView.PaintSurface += CanvasView_PaintSurface;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers the assembly.
        /// </summary>
        /// <param name="typeHavingResource">Type having resource.</param>
        public static void RegisterAssembly(Type typeHavingResource = null)
        {
            if (typeHavingResource == null)
            {
#if NETSTANDARD2_0
                _assemblyCache = Assembly.GetCallingAssembly();
#else
                MethodInfo callingAssemblyMethod = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
                if (callingAssemblyMethod != null)
                {
                    AssemblyCache = (Assembly)callingAssemblyMethod.Invoke(null, new object[0]);
                }
#endif
            }
            else
            {
                _assemblyCache = typeHavingResource.GetTypeInfo().Assembly;
            }
        }

        #endregion

        #region Private Methods

        private static string GetRealResource(string resource)
        {
            return _assemblyCache.GetManifestResourceNames()
                              .FirstOrDefault(x => x.EndsWith(resource, StringComparison.CurrentCultureIgnoreCase));
        }

        private static void RedrawCanvas(BindableObject bindable, object oldvalue, object newvalue)
        {
            var svgIcon = bindable as SvgImage;
            svgIcon?._canvasView.InvalidateSurface();
        }

        private void CanvasView_PaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            var canvas = args.Surface.Canvas;
            canvas.Clear();

            if (string.IsNullOrEmpty(Source))
            {
                return;
            }

            var resourceId = GetRealResource(Source);

            try
            {
                using (var stream = _assemblyCache.GetManifestResourceStream(resourceId))
                {
                    var svg = new SKSvg();
                    svg.Load(stream);

                    var info = args.Info;
                    canvas.Translate(info.Width / 2f, info.Height / 2f);

                    var bounds = svg.Picture.CullRect;
                    var xRatio = info.Width / bounds.Width;
                    var yRatio = info.Height / bounds.Height;

                    var ratio = Math.Min(xRatio, yRatio);

                    canvas.Scale(ratio);
                    canvas.Translate(-bounds.MidX, -bounds.MidY);

                    if (TintColor == Color.Transparent)
                    {
                        canvas.DrawPicture(svg.Picture);
                    }
                    else
                    {
                        using (var paint = new SKPaint())
                        {
                            var color = TintColor.ToSKColor();

                            paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.SrcIn);

                            canvas.DrawPicture(svg.Picture, paint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error while trying to load Resource Id {resourceId}.", ex);
            }
        }

        #endregion
    }
}