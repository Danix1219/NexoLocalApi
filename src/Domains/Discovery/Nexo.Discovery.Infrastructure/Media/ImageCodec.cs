using SkiaSharp;
namespace Nexo.Discovery.Infrastructure.Media;
public sealed record EncodedImage(byte[] Full, byte[] Thumb, byte[] Moderation, int Width, int Height);
public static class ImageCodec
{
    public static EncodedImage Encode(byte[] bytes, string contentType)
    {
        if (bytes.Length is < 1 or > 5242880) throw new InvalidDataException("upload.too_large");
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("upload.content_type_invalid");
        var expected = contentType switch { "image/jpeg" => SKEncodedImageFormat.Jpeg, "image/png" => SKEncodedImageFormat.Png, "image/webp" => SKEncodedImageFormat.Webp, _ => (SKEncodedImageFormat)(-1) };
        if (codec.EncodedFormat != expected || codec.FrameCount > 1) throw new InvalidDataException("upload.content_type_invalid");
        if (codec.Info.Width is < 1 or > 4096 || codec.Info.Height is < 1 or > 4096) throw new InvalidDataException("upload.dimensions_too_large");
        using var decoded = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success) throw new InvalidDataException("upload.content_type_invalid");
        var rotated = (int)codec.EncodedOrigin >= 5;
        var w = rotated ? decoded.Height : decoded.Width; var h = rotated ? decoded.Width : decoded.Height;
        using var oriented = new SKBitmap(w,h);
        using (var c = new SKCanvas(oriented))
        {
            switch (codec.EncodedOrigin)
            {
                case SKEncodedOrigin.TopRight: c.Translate(w,0); c.Scale(-1,1); break;
                case SKEncodedOrigin.BottomRight: c.Translate(w,h); c.RotateDegrees(180); break;
                case SKEncodedOrigin.BottomLeft: c.Translate(0,h); c.Scale(1,-1); break;
                case SKEncodedOrigin.LeftTop: c.RotateDegrees(90); c.Scale(1,-1); break;
                case SKEncodedOrigin.RightTop: c.Translate(w,0); c.RotateDegrees(90); break;
                case SKEncodedOrigin.RightBottom: c.Translate(w,h); c.RotateDegrees(90); c.Scale(-1,1); break;
                case SKEncodedOrigin.LeftBottom: c.Translate(0,h); c.RotateDegrees(-90); break;
            }
            c.DrawBitmap(decoded,0,0);
        }
        var scale = Math.Min(1,1600d/Math.Max(w,h)); var width=Math.Max(1,(int)(w*scale)); var height=Math.Max(1,(int)(h*scale));
        using var full = new SKBitmap(width,height);
        using (var c=new SKCanvas(full)) { c.Clear(SKColors.White); c.DrawBitmap(oriented,new SKRect(0,0,width,height)); }
        using var thumb = new SKBitmap(400,400);
        using (var c=new SKCanvas(thumb)) { var side=Math.Min(width,height); c.Clear(SKColors.White); c.DrawBitmap(full,new SKRect((width-side)/2f,(height-side)/2f,(width+side)/2f,(height+side)/2f),new SKRect(0,0,400,400)); }
        static byte[] Bytes(SKBitmap b,SKEncodedImageFormat format) { using var i=SKImage.FromBitmap(b); using var d=i.Encode(format,82); return d.ToArray(); }
        return new(Bytes(full,SKEncodedImageFormat.Webp),Bytes(thumb,SKEncodedImageFormat.Webp),Bytes(full,SKEncodedImageFormat.Jpeg),width,height);
    }
}
