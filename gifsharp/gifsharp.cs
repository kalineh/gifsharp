using System;
using System.Collections.Generic;

public class GifLoader
{
    public void Start()
    {
        var path = "C:\\temp\\rocklee.gif";
        var raw = System.IO.File.ReadAllBytes(path);
        var bytes = new List<byte>(raw);

        Gif gif = GifUtil.read_gif_file(path, bytes);
    }
}

public class ListCreate<T>
{
    public static List<T> Create(int size, T default_value)
    {
        var result = new List<T>(size);
        for (int i = 0; i < size; ++i)
            result.Add(default_value);
        return result;
    }
}

public class GifFILE
{
    public List<byte> bytes;
    public int cursor;

    public GifFILE(List<byte> bytes)
    {
        this.bytes = bytes;
        cursor = 0;
    }

    public bool is_eof()
    {
        return cursor >= bytes.Count;
    }

    public byte read_byte()
    {
        return bytes[cursor++];
    }

    public int read_gif_int()
    {
        byte b0 = read_byte();
        byte b1 = read_byte();

        int result = ((int)b1 << 8) | b0;

        return result;
    }

    public int read_bytes(List<byte> bytes, int count)
    {
        var remaining = count;
        while (remaining > 0)
        {
            if (is_eof())
                break;

            var b = read_byte();

            bytes.Add(b);
        }

        return count - remaining;
    }
}

public class GifColor
{
    public int r;
    public int g;
    public int b;

    public GifColor()
    {
    }

    public GifColor(int r, int g, int b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }
}

public class GifPalette
{
    public int length;
    public List<GifColor> colors;
}

public class GifScreen
{
    public int width;
    public int height;
    public int has_cmap;
    public int color_res;
    public int sorted;
    public int cmap_depth;
    public int bgcolor;
    public int aspect;
    public GifPalette cmap;
}

public class GifData
{
    public int byte_count;
    public List<byte> bytes;
}

public class GifExtension
{
    public int marker;
    public int data_count;
    public List<GifData> data; // GifData**
}

public class GifPicture
{
    public int left;
    public int top;
    public int width;
    public int height;
    public int has_cmap;
    public int interlace;
    public int sorted;
    public int reserved;
    public int cmap_depth;
    public GifPalette cmap;
    public List<byte> data; // byte**
}

public class GifBlock
{
    public int intro;
    public GifPicture pic; // GifPicture
    public GifExtension ext; // GifExtension
}

public class Gif
{
    public List<char> header;
    public GifScreen screen;
    public int block_count;
    public List<GifBlock> blocks; // GifBlock**
}

public class GIF
{
    public const int LZ_MAX_CODE = 4095;    /* Largest 12 bit code */
    public const int LZ_BITS = 12;

    public const int FLUSH_OUTPUT = 4096;    /* Impossible code = flush */
    public const int FIRST_CODE = 4097;    /* Impossible code = first */
    public const int NO_SUCH_CODE = 4098;    /* Impossible code = empty */

    public const int HT_SIZE = 8192;    /* 13 bit hash table size */
    public const int HT_KEY_MASK = 0x1FFF;  /* 13 bit key mask */

    public const int IMAGE_LOADING = 0;       /* file_state = processing */
    public const int IMAGE_SAVING = 0;       /* file_state = processing */
    public const int IMAGE_COMPLETE = 1;       /* finished reading or writing */
}

public class GifDecoder
{
    public List<byte> file;
    public int depth;
    public int clear_code;
    public int eof_code;
    public int running_code;
    public int running_bits;
    public int max_code_plus_one;
    public int prev_code;
    public int current_code;
    public int stack_ptr;
    public int shift_state;
    public UInt32 shift_data;
    public UInt32 pixel_count;
    public int file_state;
    public int position;
    public int bufsize;
    public List<byte> buf; // 256
    public List<byte> stack; // lz_max_code + 1
    public List<byte> suffix; // lz_max_code + 1
    public List<uint> prefix;  // lz_max_code + 1
}

public struct GifUtil
{
    public static GifColor rgb(int r, int g, int b)
    {
        return new GifColor(r, g, b);
    }

    public static void init_gif_decoder(GifFILE file, GifDecoder decoder)
    {
        int lzw_min = file.read_byte();
        int depth = lzw_min;

        decoder.file_state = GIF.IMAGE_LOADING;
        decoder.position = 0;
        decoder.bufsize = 0;
        decoder.buf = ListCreate<byte>.Create(256, 0);
        decoder.depth = depth;
        decoder.clear_code = (1 << depth);
        decoder.eof_code = decoder.clear_code + 1;
        decoder.running_code = decoder.eof_code + 1;
        decoder.running_bits = depth + 1;
        decoder.max_code_plus_one = 1 << decoder.running_bits;
        decoder.stack_ptr = 0;
        decoder.prev_code = GIF.NO_SUCH_CODE;
        decoder.shift_state = 0;
        decoder.shift_data = 0;

        decoder.stack = ListCreate<byte>.Create(GIF.LZ_MAX_CODE + 1, 0);
        decoder.suffix = ListCreate<byte>.Create(GIF.LZ_MAX_CODE + 1, 0);
        decoder.prefix = ListCreate<uint>.Create(GIF.LZ_MAX_CODE + 1, 0);

        for (int i = 0; i < decoder.prefix.Count; ++i)
            decoder.prefix[i] = GIF.NO_SUCH_CODE;
    }

    public static void read_gif_picture(GifFILE file, GifPicture picture)
    {
        picture.left = file.read_gif_int();
        picture.top = file.read_gif_int();
        picture.width = file.read_gif_int();
        picture.height = file.read_gif_int();

        byte info = file.read_byte();

        picture.has_cmap = (info & 0x80) >> 7;
        picture.interlace = (info & 0x40) >> 6;
        picture.sorted = (info & 0x20) >> 5;
        picture.reserved = (info & 0x18) >> 3;

        if (picture.has_cmap != 0)
        {
            picture.cmap = new GifPalette();

            picture.cmap_depth = (info & 0x07) + 1;
            picture.cmap.length = 1 << picture.cmap_depth;

            read_gif_palette(file, picture.cmap, picture.cmap.length);
        }

        read_gif_picture_data(file, picture);
    }

    public static void read_gif_picture_data(GifFILE file, GifPicture picture)
    {
        int w = picture.width;
        int h = picture.height;
        int bytes = w * h;

        picture.data = ListCreate<byte>.Create(bytes, 0);

        GifDecoder decoder = new GifDecoder();

        init_gif_decoder(file, decoder);

        if (picture.interlace != 0)
        {
            int[] interlace_start = { 0, 4, 2, 1 };
            int[] interlace_step = { 8, 8, 4, 2 };

            for (int scan_pass = 0; scan_pass < 4; ++scan_pass)
            {
                int row = interlace_start[scan_pass];
                while (row < h)
                {
                    read_gif_line(file, decoder, picture.data, row * w, w);
                    row += interlace_step[scan_pass];
                }
            }
        }
        else
        {
            int row = 0;
            while (row < h)
            {
                read_gif_line(file, decoder, picture.data, row * w, w);
                row += 1;
            }
        }

        finish_gif_picture(file, decoder);
    }

    public static int read_gif_code(GifFILE file, GifDecoder decoder)
    {
        return 0;
    }

    public static int trace_prefix(List<uint> prefix, int code, int clear_code)
    {
        int i = 0;

        while (code > clear_code && i++ <= GIF.LZ_MAX_CODE)
            code = (int)prefix[code];

        return code;
    }

    public static void read_gif_line(GifFILE file, GifDecoder decoder, List<byte> line, int offset, int length)
    {
        int current_prefix;
        int current_code;

        List<uint> prefix = decoder.prefix;
        List<byte> suffix = decoder.suffix;
        List<byte> stack = decoder.stack;

        int stack_ptr = decoder.stack_ptr;
        int eof_code = decoder.eof_code;
        int clear_code = decoder.clear_code;
        int prev_code = decoder.prev_code;

        int i = 0;

        if (stack_ptr != 0)
        {
            while (stack_ptr != 0 && i < length)
                line[offset + i++] = stack[--stack_ptr];
        }

        while (i < length)
        {
            current_code = read_gif_code(file, decoder);

            if (current_code == eof_code)
            {
                // unexpected eof
                if (i != length - 1 || decoder.pixel_count != 0)
                    return;
                i++;
            }
            else if (current_code == clear_code)
            {
                for (int j = 0; j < GIF.LZ_MAX_CODE; ++j)
                    prefix[j] = GIF.NO_SUCH_CODE;

                decoder.running_code = decoder.eof_code + 1;
                decoder.running_bits = decoder.depth + 1;
                decoder.max_code_plus_one = 1 << decoder.running_bits;
                prev_code = decoder.prev_code = GIF.NO_SUCH_CODE;
            }
            else
            {
                if (current_code < clear_code)
                {
                    line[offset + i++] = (byte)current_code;
                }
                else
                {
                    if ((current_code < 0) || (current_code > GIF.LZ_MAX_CODE))
                        return;

                    if (prefix[current_code] == GIF.NO_SUCH_CODE)
                    {
                        if (current_code == decoder.running_code - 2)
                        {
                            current_prefix = prev_code;
                            suffix[decoder.running_code - 2]
                                = stack[stack_ptr++]
                                = (byte)trace_prefix(prefix, prev_code, clear_code);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        current_prefix = current_code;
                    }

                    int j = 0;
                    while (j++ <= GIF.LZ_MAX_CODE
                        && current_prefix > clear_code
                        && current_prefix <= GIF.LZ_MAX_CODE)
                    {
                        stack[stack_ptr++] = suffix[current_prefix];
                        current_prefix = (int)prefix[current_prefix];
                    }

                    if (j >= GIF.LZ_MAX_CODE || current_prefix > GIF.LZ_MAX_CODE)
                        return;

                    stack[stack_ptr++] = (byte)current_prefix;

                    while (stack_ptr != 0 && i < length)
                        line[offset + i++] = stack[--stack_ptr];
                }

                if (prev_code != GIF.NO_SUCH_CODE)
                {
                    if ((decoder.running_code < 2) ||
                        (decoder.running_code > GIF.LZ_MAX_CODE + 2))
                        return;
                    prefix[decoder.running_code - 2] = (uint)prev_code;

                    if (current_code == decoder.running_code - 2)
                    {
                        suffix[decoder.running_code - 2] =
                            (byte)trace_prefix(prefix, prev_code, clear_code);
                    }
                    else
                    {
                        suffix[decoder.running_code - 2] =
                            (byte)trace_prefix(prefix, current_code, clear_code);
                    }
                }

                prev_code = current_code;
            }
        }

        decoder.prev_code = prev_code;
        decoder.stack_ptr = stack_ptr;
    }

    public static void finish_gif_picture(GifFILE file, GifDecoder decoder)
    {
        List<byte> buf = decoder.buf;

        while (decoder.bufsize != 0)
        {
            decoder.bufsize = file.read_byte();
            if (decoder.bufsize == 0)
            {
                decoder.file_state = GIF.IMAGE_COMPLETE;
                break;
            }

            // should this be loading a new buf each time?
            read_stream(file, buf, decoder.bufsize);
        }
    }

    public static GifData new_gif_data(int size)
    {
        return new GifData()
        {
            bytes = new List<byte>(),
            byte_count = size,
        };
    }

    public static GifData read_gif_data(GifFILE file)
    {
        int size = file.read_byte();
        if (size <= 0)
            return null;

        var data = new_gif_data(size);

        read_stream(file, data.bytes, size);

        return new GifData();
    }

    public static int read_stream(GifFILE file, List<byte> bytes, int size)
    {
        for (int i = 0; i < bytes.Count; ++i)
            bytes[i] = 0;

        int read = file.read_bytes(bytes, size);

        return read;
    }

    public static void read_gif_extension(GifFILE file, GifExtension extension)
    {
        extension.marker = file.read_byte();

        var data = read_gif_data(file);
        while (data != null)
        {
            extension.data.Add(data);
            data = read_gif_data(file);
        }

        extension.data_count = extension.data.Count;
    }

    public static void read_gif_palette(GifFILE file, GifPalette palette, int length)
    {
        palette.colors = new List<GifColor>(length);

        for (int i = 0; i < length; ++i)
        {
            var r = file.read_byte();
            var g = file.read_byte();
            var b = file.read_byte();

            var color = rgb(r, g, b);

            palette.colors.Add(color);
        }
    }

    public static void read_gif_screen(GifFILE file, GifScreen screen)
    {
        screen.width = file.read_gif_int();
        screen.height = file.read_gif_int();

        byte info = file.read_byte();

        screen.has_cmap = (info & 0x80) >> 7;
        screen.color_res = ((info & 0x70) >> 4) + 1;
        screen.sorted = (info & 0x08) >> 3;
        screen.cmap_depth = (info & 0x07) + 1;

        screen.bgcolor = file.read_byte();
        screen.aspect = file.read_byte();

        if (screen.has_cmap != 0)
        {
            screen.cmap = new GifPalette();

            var length = 1 << screen.cmap_depth;
            read_gif_palette(file, screen.cmap, length);
        }
    }

    public static void read_gif_block(GifFILE file, GifBlock block)
    {
        block.intro = file.read_byte();

        if (block.intro == 0x2C)
        {
            block.pic = new GifPicture();
            read_gif_picture(file, block.pic);
            return;
        }

        if (block.intro == 0x21)
        {
            block.ext = new GifExtension();
            read_gif_extension(file, block.ext);
            return;
        }
    }

    public static void read_gif(GifFILE file, Gif gif)
    {
        gif.header = ListCreate<char>.Create(8, (char)0);

        for (int i = 0; i < 6; ++i)
        {
            gif.header[i] = (char)file.read_byte();
        }

        if (gif.header[0] != 'G' || gif.header[1] != 'I' || gif.header[2] != 'F')
            return;

        gif.screen = new GifScreen();
        read_gif_screen(file, gif.screen);

        gif.blocks = new List<GifBlock>();

        while (true)
        {
            GifBlock block = new GifBlock();

            read_gif_block(file, block);

            // terminator
            if (block.intro == 0x3B)
                break;

            // image
            if (block.intro == 0x2C)
            {
                gif.block_count++;
                gif.blocks.Add(block);
                continue;
            }

            // extension
            if (block.intro == 0x21)
            {
                gif.block_count++;
                gif.blocks.Add(block);
                continue;
            }

            // error
            break;
        }
    }

    public static Gif read_gif_file(string filename, List<byte> bytes)
    {
        GifFILE file = new GifFILE(bytes);
        Gif gif = new Gif();

        read_gif(file, gif);

        if (gif.header[0] != 'G' || gif.header[1] != 'I' || gif.header[2] != 'F')
            return null;

        return gif;
    }
}
