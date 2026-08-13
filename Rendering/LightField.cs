using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DotGame.Rendering;

/// <summary>
/// Shape of a light splat's radial falloff.
/// </summary>
public enum Falloff
{
    /// <summary>Broad, gentle shoulder - haloes and auras.</summary>
    Soft,
    /// <summary>Concentrated centre that drops away fast - hot cores and sparks.</summary>
    Tight
}

/// <summary>
/// A high-dynamic-range additive light buffer.
///
/// Light is accumulated as unbounded floating-point energy and only converted to displayable
/// bytes at the very end, through an exposure curve. That ordering is the whole point: two
/// overlapping lights *sum*, so their overlap is genuinely brighter than either alone and
/// saturates to white, exactly as real emitters do. Drawing translucent ellipses instead
/// would alpha-blend - the nearer disc would partly hide the further one, and no combination
/// of them could ever be brighter than the brightest single source.
///
/// Nothing here has an edge. Every primitive is a falloff that reaches zero at its radius,
/// so particles fade into the background rather than being cut out of it.
/// </summary>
public sealed class LightField
{
    // Cap on buffer pixels. Beyond this the field renders at reduced scale and is stretched
    // back up on display - which costs almost nothing visually, because a glow is
    // low-frequency and bilinear upscaling of one is indistinguishable from rendering it
    // large. It costs a great deal in time, however: clearing, tone-mapping and uploading
    // are all per-pixel and independent of particle count, so at full resolution they, not
    // the lights, dominate the frame. Measured on a ~870k-pixel canvas, dropping to this cap
    // took the frame from ~12.9ms to comfortably inside the 16.7ms budget.
    private const int MAX_BUFFER_PIXELS = 480_000;

    // Tone-mapping lookup. Transcendental maths per channel per pixel would dominate the
    // frame; a quantised curve is visually identical and a single array read.
    private const int TONE_LUT_SIZE = 4096;
    private const float TONE_LUT_MAX = 24f;

    private float[] _energy = Array.Empty<float>();   // interleaved RGB, linear light
    private byte[] _pixels = Array.Empty<byte>();     // BGRA output
    private byte[] _toneLut = Array.Empty<byte>();
    private float _lutExposure = float.NaN;
    private float _lutGamma = float.NaN;

    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>Buffer pixels per canvas pixel (&lt;= 1).</summary>
    public double Scale { get; private set; } = 1.0;

    public WriteableBitmap? Bitmap { get; private set; }

    /// <summary>
    /// Sizes the field to a canvas. Returns true if the backing bitmap was recreated.
    /// </summary>
    public bool Resize(double canvasWidth, double canvasHeight)
    {
        if (canvasWidth < 1 || canvasHeight < 1) return false;

        double scale = 1.0;
        double pixels = canvasWidth * canvasHeight;
        if (pixels > MAX_BUFFER_PIXELS)
            scale = Math.Sqrt(MAX_BUFFER_PIXELS / pixels);

        int w = Math.Max(1, (int)(canvasWidth * scale));
        int h = Math.Max(1, (int)(canvasHeight * scale));

        if (w == Width && h == Height && Bitmap != null) return false;

        Width = w;
        Height = h;
        Scale = scale;
        _energy = new float[w * h * 3];
        _pixels = new byte[w * h * 4];
        Bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        return true;
    }

    public void Clear() => Array.Clear(_energy, 0, _energy.Length);

    /// <summary>
    /// Scales all accumulated light down. Running this instead of a full clear leaves a
    /// decaying imprint of previous frames, which is what produces motion trails made of
    /// light rather than of drawn line segments.
    /// </summary>
    public void Decay(float keep)
    {
        if (keep <= 0f) { Clear(); return; }
        if (keep >= 1f) return;

        var buf = _energy;
        Parallel.For(0, Height, y =>
        {
            int start = y * Width * 3;
            int end = start + Width * 3;
            for (int i = start; i < end; i++) buf[i] *= keep;
        });
    }

    /// <summary>
    /// Adds a radially fading light. Coordinates and radius are in canvas space.
    /// </summary>
    public void AddGlow(double cx, double cy, double radius, Color color, float intensity,
        Falloff falloff = Falloff.Soft)
    {
        if (intensity <= 0f || radius <= 0) return;

        double s = Scale;
        float fx = (float)(cx * s), fy = (float)(cy * s);
        float r = (float)(radius * s);
        if (r < 0.5f) r = 0.5f;

        int minX = Math.Max(0, (int)MathF.Floor(fx - r));
        int maxX = Math.Min(Width - 1, (int)MathF.Ceiling(fx + r));
        int minY = Math.Max(0, (int)MathF.Floor(fy - r));
        int maxY = Math.Min(Height - 1, (int)MathF.Ceiling(fy + r));
        if (minX > maxX || minY > maxY) return;

        float r2 = r * r;
        float cr = color.R * (1f / 255f) * intensity;
        float cg = color.G * (1f / 255f) * intensity;
        float cb = color.B * (1f / 255f) * intensity;
        bool tight = falloff == Falloff.Tight;

        var buf = _energy;
        for (int y = minY; y <= maxY; y++)
        {
            float dy = y - fy;
            float dy2 = dy * dy;
            int row = y * Width;
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - fx;
                float d2 = dx * dx + dy2;
                if (d2 >= r2) continue;

                // t falls linearly in squared distance and reaches exactly 0 at the rim,
                // so there is no discontinuity to read as an edge.
                float t = 1f - d2 / r2;
                float v = t * t;
                if (tight) v *= v;

                int i = (row + x) * 3;
                buf[i] += cr * v;
                buf[i + 1] += cg * v;
                buf[i + 2] += cb * v;
            }
        }
    }

    /// <summary>
    /// Adds a soft annulus - an aura that reads as a shell of light around a particle
    /// rather than a filled disc.
    /// </summary>
    public void AddRing(double cx, double cy, double radius, double thickness, Color color, float intensity)
    {
        if (intensity <= 0f || radius <= 0 || thickness <= 0) return;

        double s = Scale;
        float fx = (float)(cx * s), fy = (float)(cy * s);
        float rr = (float)(radius * s);
        float th = MathF.Max(0.75f, (float)(thickness * s));
        float outer = rr + th;

        int minX = Math.Max(0, (int)MathF.Floor(fx - outer));
        int maxX = Math.Min(Width - 1, (int)MathF.Ceiling(fx + outer));
        int minY = Math.Max(0, (int)MathF.Floor(fy - outer));
        int maxY = Math.Min(Height - 1, (int)MathF.Ceiling(fy + outer));
        if (minX > maxX || minY > maxY) return;

        float cr = color.R * (1f / 255f) * intensity;
        float cg = color.G * (1f / 255f) * intensity;
        float cb = color.B * (1f / 255f) * intensity;

        var buf = _energy;
        for (int y = minY; y <= maxY; y++)
        {
            float dy = y - fy;
            float dy2 = dy * dy;
            int row = y * Width;
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - fx;
                float d = MathF.Sqrt(dx * dx + dy2);
                float off = MathF.Abs(d - rr);
                if (off >= th) continue;

                float t = 1f - off / th;
                float v = t * t;

                int i = (row + x) * 3;
                buf[i] += cr * v;
                buf[i + 1] += cg * v;
                buf[i + 2] += cb * v;
            }
        }
    }

    /// <summary>
    /// Adds a tapering streak of light between two points, used for comet smears and the
    /// motion blur of fast movers. Drawn as overlapping glows so it has no outline.
    /// </summary>
    public void AddStreak(double x0, double y0, double x1, double y1, double radius,
        Color color, float intensity)
    {
        double dx = x1 - x0, dy = y1 - y0;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 0.01) { AddGlow(x0, y0, radius, color, intensity); return; }

        // One stamp per half-radius keeps the streak continuous without over-drawing
        int steps = Math.Clamp((int)(len / Math.Max(1.0, radius * 0.5)), 2, 48);
        for (int i = 0; i <= steps; i++)
        {
            double f = (double)i / steps;
            // Taper both brightness and width toward the tail
            float fade = (float)(1.0 - f);
            AddGlow(x0 + dx * f, y0 + dy * f, radius * (0.35 + 0.65 * fade),
                color, intensity * fade * fade);
        }
    }

    /// <summary>
    /// Converts accumulated light to displayable pixels through an exposure curve and
    /// uploads them to the bitmap.
    /// </summary>
    /// <param name="exposure">Multiplier applied before the curve - the "brightness" control.</param>
    /// <param name="gamma">Display gamma; 2.2 keeps midtones from looking muddy.</param>
    public void Resolve(float exposure, float gamma)
    {
        if (Bitmap == null) return;

        EnsureToneLut(exposure, gamma);

        var buf = _energy;
        var px = _pixels;
        var lut = _toneLut;
        const float lutScale = TONE_LUT_SIZE / TONE_LUT_MAX;
        int lastLut = TONE_LUT_SIZE - 1;
        int width = Width;

        Parallel.For(0, Height, y =>
        {
            int si = y * width * 3;
            int di = y * width * 4;
            for (int x = 0; x < width; x++)
            {
                int ri = (int)(buf[si] * lutScale);
                int gi = (int)(buf[si + 1] * lutScale);
                int bi = (int)(buf[si + 2] * lutScale);
                if (ri > lastLut) ri = lastLut; else if (ri < 0) ri = 0;
                if (gi > lastLut) gi = lastLut; else if (gi < 0) gi = 0;
                if (bi > lastLut) bi = lastLut; else if (bi < 0) bi = 0;

                px[di] = lut[bi];      // B
                px[di + 1] = lut[gi];  // G
                px[di + 2] = lut[ri];  // R
                px[di + 3] = 255;      // A
                si += 3;
                di += 4;
            }
        });

        Bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, Width, Height), px, Width * 4, 0);
    }

    private void EnsureToneLut(float exposure, float gamma)
    {
        if (_toneLut.Length == TONE_LUT_SIZE && exposure == _lutExposure && gamma == _lutGamma)
            return;

        _toneLut = new byte[TONE_LUT_SIZE];
        float invGamma = 1f / MathF.Max(0.1f, gamma);

        for (int i = 0; i < TONE_LUT_SIZE; i++)
        {
            float linear = i * (TONE_LUT_MAX / TONE_LUT_SIZE);

            // Exponential exposure curve: approaches 1 asymptotically, so bright overlaps
            // roll off into white instead of clipping into flat coloured plateaus.
            float mapped = 1f - MathF.Exp(-linear * exposure);
            float encoded = MathF.Pow(mapped, invGamma);
            _toneLut[i] = (byte)Math.Clamp((int)(encoded * 255f + 0.5f), 0, 255);
        }

        _lutExposure = exposure;
        _lutGamma = gamma;
    }
}
