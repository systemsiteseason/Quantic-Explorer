using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.ComponentModel;

namespace Quantic_Explorer
{
    public partial class Form1 : Form
    {
        private static GCHandle handle;
        Image imgOriginal;

        public Form1()
        {
            InitializeComponent();

            /*byte[] b = new byte[] { 0xd0 };
            ByteViewer bv = new ByteViewer();
            bv.SetBytes(b);
            Controls.Add(bv);*/
        }
        public Dictionary<int, byte[]> dic = new Dictionary<int, byte[]>();
        public Dictionary<int, byte[]> dicblock = new Dictionary<int, byte[]>();
        public string path;
        public string filename;
        public byte[] hxd;
        public string nameoff;
        public byte[] sound;
        public WaveStream mainOutputStream;
        WaveChannel32 volumeStream;
        WaveOutEvent player = new WaveOutEvent();
        public int version;
        public int lengsegs = 0;
        List<string> addr = new List<string>();
        public string exte = string.Empty;
        public byte[] savef = null;
        OpenFileDialog openFile = new OpenFileDialog();

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile.Filter = "IDX File|*.idx";
            if(openFile.ShowDialog() == DialogResult.OK)
            {
                dic.Clear();
                dicblock.Clear();
                if (!string.IsNullOrEmpty(openFile.FileName))
                {
                    
                    List<string> offset_data = new List<string>();
                    path = Path.GetDirectoryName(openFile.FileName);
                    filename = Path.GetFileNameWithoutExtension(openFile.FileName);
                    var ext = Path.GetExtension(openFile.FileName);
                    BinaryReader rd = new BinaryReader(File.OpenRead(openFile.FileName));
                    rd.BaseStream.Seek(20, SeekOrigin.Begin);
                    version = Big(rd.ReadBytes(4));
                    if (version == 18)
                        rd.BaseStream.Seek(101, SeekOrigin.Begin);
                    else
                        rd.BaseStream.Seek(100, SeekOrigin.Begin);
                    int num = Big(rd.ReadBytes(4));
                    for(int i = 0; i < num; i++)
                    {
                        int typetide = Big(rd.ReadBytes(4));
                        rd.ReadBytes(4);
                        byte[] bt = rd.ReadBytes(20);
                        var rdm = new BinaryReader(new MemoryStream(bt));
                        if (typetide == 4091 || typetide == 1010)
                            offset_data.Add("0x" + Big(rdm.ReadBytes(4)).ToString("x8") + "-Audio");
                        else if( typetide == 2137)
                            offset_data.Add("0x" + Big(rdm.ReadBytes(4)).ToString("x8")+"-Texture");
                        else if (typetide == 29)
                            offset_data.Add("0x" + Big(rdm.ReadBytes(4)).ToString("x8") + "-Segs*Qzip");
                        else if (typetide == 2199)
                            offset_data.Add("0x" + Big(rdm.ReadBytes(4)).ToString("x8") + "-Video");
                        else
                            offset_data.Add("0x" + Big(rdm.ReadBytes(4)).ToString("x8"));

                        rdm.Close();
                        dic.Add(i, bt);
                    }
                    listBox1.DataSource = offset_data;
                    rd.Close();
                    //bytehex.SetFile(openFile.FileName);2199
                    Console.WriteLine("Open file {0} version {1}", Path.GetFileName(openFile.FileName), version);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        public static int Big( byte[] input)
        {
            Array.Reverse(input);
            return BitConverter.ToInt32(input, 0);
        }

        public static int Big16(byte[] input)
        {
            Array.Reverse(input);
            return BitConverter.ToInt16(input, 0);
        }

        private void listBox1_MouseClick(object sender, MouseEventArgs e)
        {
            int index = this.listBox1.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                btnExportR.Enabled = true;
                bytehex.SetBytes(new byte[] { });
                byte[] value = dic[index];
                var rdk = new BinaryReader(new MemoryStream(value));
                rdk.ReadBytes(4);//num_object
                int offs = Big(rdk.ReadBytes(4));
                nameoff = "0x" + offs.ToString("X8");
                int len = Big(rdk.ReadBytes(4));
                rdk.ReadBytes(4);
                int file = Big(rdk.ReadBytes(4));
                string bf = string.Empty;
                if (file == 0)
                    bf = ".dat";
                else
                    bf = ".d" + file.ToString("d2");
                try
                {
                    var rdbig = new BinaryReader(new FileStream(path + "\\" + filename + bf, FileMode.Open, FileAccess.Read, FileShare.Read));
                    rdbig.BaseStream.Seek(offs, SeekOrigin.Begin);
                    byte[] hex = rdbig.ReadBytes(len);
                    hxd = hex;
                    bytehex.SetBytes(hex);
                    namefile.Text = filename + bf;
                    this.offs.Text = offs.ToString();
                    this.len.Text = len.ToString();
                    player.Stop();
                    using (var datab = new BinaryReader(new MemoryStream(hex)))
                    {
                        switch (datab.ReadInt32())
                        {
                            case 1936156019: //segs
                                typeb.Text = "Compress SEGS";
                                SEGSDATA();
                                if (version == 13)
                                {
                                    lengsegs = 0;
                                    dicblock.Clear();
                                    datab.ReadInt16();
                                    int numf = Big16(datab.ReadBytes(2));
                                    datab.ReadBytes(8);
                                    int[] bit1 = new int[numf];
                                    int[] bit2 = new int[numf];
                                    int[] bit3 = new int[numf];
                                    for (int i = 0; i < numf; i++)
                                    {
                                        bit1[i] = (UInt16)Big16(datab.ReadBytes(2));
                                        byte[] tmp = datab.ReadBytes(2);
                                        bit2[i] = Big16(tmp);
                                        if(tmp[0] == 0 && tmp[1] == 0)
                                        {
                                            bit2[i] = 65536;
                                        }
                                        bit3[i] = Big(datab.ReadBytes(4));
                                    }
                                    if(numf % 2 != 0)
                                    {
                                        datab.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                    }
                                    else
                                    {
                                        datab.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                    }
                                    int pos = (int)datab.BaseStream.Position;
                                    for(int i = 0; i < numf; i++)
                                    {
                                        datab.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                        addr.Add("0x" + datab.BaseStream.Position.ToString("X8").ToUpper());
                                        byte[] blk = datab.ReadBytes(bit1[i]);
                                        lengsegs += Decompress(blk).Length;
                                        dicblock.Add(i, Decompress(blk));
                                    }
                                    listBox2.DataSource = addr;
                                } 
                                else if(version == 17 || version == 18)
                                {
                                    lengsegs = 0;
                                    dicblock.Clear();
                                    datab.ReadInt16();
                                    int numf = datab.ReadInt16();
                                    datab.ReadBytes(8);
                                    int[] bit1 = new int[numf];
                                    int[] bit2 = new int[numf];
                                    int[] bit3 = new int[numf];
                                    for (int i = 0; i < numf; i++)
                                    {
                                        bit1[i] = datab.ReadUInt16();
                                        byte[] tmp = datab.ReadBytes(2);
                                        bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                        if (tmp[0] == 0 && tmp[1] == 0)
                                        {
                                            bit2[i] = 65536;
                                        }
                                        bit3[i] = datab.ReadInt32();
                                    }
                                    if (numf % 2 != 0)
                                    {
                                        datab.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                    }
                                    else
                                    {
                                        datab.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                    }
                                    int pos = (int)datab.BaseStream.Position;
                                    for (int i = 0; i < numf; i++)
                                    {
                                        datab.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                        datab.ReadBytes(2);
                                        addr.Add("0x" + datab.BaseStream.Position.ToString("X8").ToUpper());
                                        byte[] blk = datab.ReadBytes(bit1[i]-2);
                                        lengsegs += Decompress(blk).Length;
                                        dicblock.Add(i, Decompress(blk));
                                    }
                                    listBox2.DataSource = addr;
                                }
                                break;
                            case 1346984529: //qzi
                                datab.ReadBytes(1);
                                string next = Encoding.ASCII.GetString(datab.ReadBytes(8));
                                if(next == "COM_CONT")
                                {
                                    datab.BaseStream.Seek(41, SeekOrigin.Begin);
                                    if(Encoding.ASCII.GetString(datab.ReadBytes(8)) == "FILETEXT")
                                    {
                                        typeb.Text = "Texture";
                                        TexData();
                                        datab.BaseStream.Seek(61, SeekOrigin.Begin);
                                        byte[] head = new byte[]{ 0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x00, 0x00};
                                        int chk = datab.ReadByte();
                                        int width = Big(datab.ReadBytes(4));
                                        int height = Big(datab.ReadBytes(4));
                                        int lck = Big(datab.ReadBytes(4));
                                        datab.ReadBytes(lck * 5 + (15 - lck));
                                        long avhead = datab.BaseStream.Position;
                                        if (chk == 0)
                                        {
                                            int lengtext = len - (int)avhead;
                                            byte[] textureb = datab.ReadBytes(lengtext);
                                            byte[] done = new byte[128 + textureb.Length];
                                            Buffer.BlockCopy(head, 0, done, 0, head.Length);
                                            Buffer.BlockCopy(BitConverter.GetBytes(height), 0, done, head.Length, 4);
                                            Buffer.BlockCopy(BitConverter.GetBytes(width), 0, done, head.Length + 4, 4);
                                            Buffer.BlockCopy(BitConverter.GetBytes(lengtext), 0, done, head.Length + 8, 4);
                                            Buffer.BlockCopy(argb, 0, done, head.Length + 12, argb.Length);
                                            Buffer.BlockCopy(textureb, 0, done, head.Length + 12 + argb.Length, textureb.Length);
                                            exte = ".dds";
                                            savef = done;
                                            TexDDS(done);
                                        }
                                        else if(chk == 7)
                                        {
                                            int lengtext = len - (int)avhead;
                                            byte[] textureb = datab.ReadBytes(lengtext);
                                            byte[] done = new byte[128 + textureb.Length];
                                            Buffer.BlockCopy(head, 0, done, 0, head.Length);
                                            Buffer.BlockCopy(BitConverter.GetBytes(height), 0, done, head.Length, 4);
                                            Buffer.BlockCopy(BitConverter.GetBytes(width), 0, done, head.Length + 4, 4);
                                            Buffer.BlockCopy(BitConverter.GetBytes(lengtext), 0, done, head.Length + 8, 4);
                                            Buffer.BlockCopy(bc3, 0, done, head.Length + 12, bc3.Length);
                                            Buffer.BlockCopy(textureb, 0, done, head.Length + 12 + bc3.Length, textureb.Length);
                                            exte = ".dds";
                                            savef = done;
                                            TexDDS(done);
                                        }
                                        

                                        //TexDDS(textureout);
                                    }
                                    else
                                    {
                                        typeb.Text = "Unknown";
                                        unkdata();
                                    }
                                }
                                else if(next == "VIDEDATA")
                                {
                                    typeb.Text = "Video BIK";
                                    viddata();
                                    datab.BaseStream.Seek(281, SeekOrigin.Begin);
                                    exte = ".bik";
                                    savef = datab.ReadBytes(len - 281);
                                }
                                else
                                {
                                    typeb.Text = "Unknown";
                                    unkdata();
                                }
                                break;
                            case 1230979908: //dat
                                typeb.Text = "DC_INFO";
                                unkdata();
                                break;
                            case 1414676816: //par
                                typeb.Text = "Sound";
                                SoundData();
                                exte = ".mp3";
                                savef = hex;
                                try
                                {
                                    mainOutputStream = new Mp3FileReader(new MemoryStream(hex));
                                    volumeStream = new WaveChannel32(mainOutputStream);
                                    player.Init(volumeStream);
                                }
                                catch(Exception ex)
                                {
                                    playbt.Enabled = false;
                                    stopbtn.Enabled = false;
                                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                break;
                            case 1598902083: // tex
                                datab.BaseStream.Seek(36, SeekOrigin.Begin);
                                if (Encoding.ASCII.GetString(datab.ReadBytes(8)) == "FILETEXT")
                                {
                                    typeb.Text = "Texture";
                                    TexData();
                                    datab.BaseStream.Seek(52, SeekOrigin.Begin);
                                    byte[] head = new byte[] { 0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x00, 0x00 };
                                    int chk = datab.ReadByte();
                                    int width = datab.ReadInt16();
                                    int height = datab.ReadInt16();
                                    datab.ReadBytes(104);
                                    long avhead = datab.BaseStream.Position;
                                    if (chk == 0)
                                    {
                                        int lengtext = len - (int)avhead;
                                        byte[] textureb = datab.ReadBytes(lengtext);
                                        byte[] done = new byte[128 + textureb.Length];
                                        Buffer.BlockCopy(head, 0, done, 0, head.Length);
                                        Buffer.BlockCopy(BitConverter.GetBytes(height), 0, done, head.Length, 4);
                                        Buffer.BlockCopy(BitConverter.GetBytes(width), 0, done, head.Length + 4, 4);
                                        Buffer.BlockCopy(BitConverter.GetBytes(lengtext), 0, done, head.Length + 8, 4);
                                        Buffer.BlockCopy(argb, 0, done, head.Length + 12, argb.Length);
                                        Buffer.BlockCopy(textureb, 0, done, head.Length + 12 + argb.Length, textureb.Length);
                                        exte = ".dds";
                                        savef = done;
                                        TexDDS(done);
                                    }
                                    else if (chk == 7)
                                    {
                                        int lengtext = len - (int)avhead;
                                        byte[] textureb = datab.ReadBytes(lengtext);
                                        byte[] done = new byte[128 + textureb.Length];
                                        Buffer.BlockCopy(head, 0, done, 0, head.Length);
                                        Buffer.BlockCopy(BitConverter.GetBytes(height), 0, done, head.Length, 4);
                                        Buffer.BlockCopy(BitConverter.GetBytes(width), 0, done, head.Length + 4, 4);
                                        Buffer.BlockCopy(BitConverter.GetBytes(lengtext), 0, done, head.Length + 8, 4);
                                        Buffer.BlockCopy(bc3, 0, done, head.Length + 12, bc3.Length);
                                        Buffer.BlockCopy(textureb, 0, done, head.Length + 12 + bc3.Length, textureb.Length);
                                        exte = ".dds";
                                        savef = done;
                                        TexDDS(done);
                                    }


                                    //TexDDS(textureout);
                                }
                                else
                                {
                                    typeb.Text = "Unknown";
                                    unkdata();
                                }
                                break;
                        }
                    }
                    
                    
                    rdk.Close();
                    rdbig.Close();
                }
                catch
                {
                    MessageBox.Show("Error!");
                }
            }
        }

        public void TexDDS(byte[] data)
        {
            var image = Pfim.Pfim.FromStream(new MemoryStream(data));

            PixelFormat format;
            switch (image.Format)
            {
                case Pfim.ImageFormat.Rgb24:
                    format = PixelFormat.Format24bppRgb;
                    break;

                case Pfim.ImageFormat.Rgba32:
                    format = PixelFormat.Format32bppArgb;
                    break;

                case Pfim.ImageFormat.R5g5b5:
                    format = PixelFormat.Format16bppRgb555;
                    break;

                case Pfim.ImageFormat.R5g6b5:
                    format = PixelFormat.Format16bppRgb565;
                    break;

                case Pfim.ImageFormat.R5g5b5a1:
                    format = PixelFormat.Format16bppArgb1555;
                    break;

                case Pfim.ImageFormat.Rgb8:
                    format = PixelFormat.Format8bppIndexed;
                    break;

                default:
                    var msg = $"{image.Format} is not recognized for Bitmap on Windows Forms. " +
                               "You'd need to write a conversion function to convert the data to known format";
                    var caption = "Unrecognized format";
                    MessageBox.Show(msg, caption, MessageBoxButtons.OK);
                    return;
            }
            if (handle.IsAllocated)
            {
                handle.Free();
            }

            // Pin image data as the picture box can outlive the Pfim Image
            // object, which, unless pinned, will garbage collect the data
            // array causing image corruption.
            handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
            var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, ptr);

            // While frameworks like WPF and ImageSharp natively understand 8bit gray values.
            // WinForms can only work with an 8bit palette that we construct of gray values.
            if (format == PixelFormat.Format8bppIndexed)
            {
                var palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb((byte)i, (byte)i, (byte)i);
                }
                bitmap.Palette = palette;
            }

            pictureBox2.Image = bitmap;
            //flowLayoutPanel1.Controls.Add(pictureBox1);
            imgOriginal = pictureBox2.Image;
        }
        SaveFileDialog svf = new SaveFileDialog();
        private void btnExport_Click(object sender, EventArgs e)
        {
            svf.FileName = nameoff + exte;
            if (svf.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(svf.FileName))
                {
                    using (var wt = new BinaryWriter(new FileStream(svf.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        wt.Write(savef);
                    }
                    savef = null;
                    exte = string.Empty;
                    Console.WriteLine("Export file {0}", Path.GetFileName(svf.FileName));
                }
            }
        }

        private void btnExportR_Click(object sender, EventArgs e)
        {
            svf.FileName = nameoff;
            svf.Filter = "DAT File|*.rdat";
            if (svf.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(svf.FileName))
                {
                    using (var wt = new BinaryWriter(new FileStream(svf.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        wt.Write(hxd);
                    }
                    Console.WriteLine("Export Raw file {0}", Path.GetFileName(svf.FileName));
                }
            }
        }

        public byte[] argb = new byte[]
        {
            0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x20, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00,
            0x00, 0x00, 0x00, 0xFF, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public byte[] bc3 = new byte[]
        {
            0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x44, 0x58, 0x54, 0x35,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private void playbt_Click(object sender, EventArgs e)
        {
            player.Play();
        }

        private void stopbtn_Click(object sender, EventArgs e)
        {
            player.Stop();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            if (trackBar2.Value > 0)
            {
                pictureBox2.Image = Zoom(imgOriginal, new Size(trackBar2.Value, trackBar2.Value));
            }
        }

        Image Zoom(Image img, Size size)
        {
            Bitmap bmp = new Bitmap(img, img.Width + (img.Width * size.Width / 100), img.Height + (img.Height * size.Height / 100));
            Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            return bmp;
        }

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (System.IO.Compression.DeflateStream dstream = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        private void listBox2_MouseClick(object sender, MouseEventArgs e)
        {
            int index2 = this.listBox2.IndexFromPoint(e.Location);
            if (index2 != ListBox.NoMatches)
            {
                bytehex.SetBytes(new byte[] { });
                bytehex.SetBytes(dicblock[index2]);
            }
        }

        private void btnDecompress_Click(object sender, EventArgs e)
        {
            btnExport.Enabled = true;
            bytehex.SetBytes(new byte[] { });
            byte[] allone = new byte[lengsegs];
            int pivot = 0;
            for (int i = 0; i < dicblock.Count; i++)
            {
                Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                pivot += dicblock[i].Length;
            }
            exte = ".dc_segs";
            savef = allone;
            bytehex.SetBytes(allone);
        }
        #region ui
        public void SEGSDATA()
        {
            if (addr.Count > 0)
                addr.Clear();
            btnDecompress.Enabled = true;
            btnExport.Enabled = false;
            pictureBox2.Image = null;
            playbt.Enabled = false;
            stopbtn.Enabled = false;
            btnExpTex.Enabled = false;
            trackBar2.Value = 0;
            trackBar2.Enabled = false;
            listBox2.DataSource = null;
            listBox2.Items.Clear();
        }

        public void unkdata()
        {
            btnDecompress.Enabled = false;
            btnExport.Enabled = false;
            pictureBox2.Image = null;
            playbt.Enabled = false;
            stopbtn.Enabled = false;
            btnExpTex.Enabled = false;
            listBox2.DataSource = null;
            listBox2.Items.Clear();
            trackBar2.Value = 0;
            trackBar2.Enabled = false;
            dicblock.Clear();
            if (addr.Count > 0)
                addr.Clear();
        }

        public void viddata()
        {
            btnDecompress.Enabled = false;
            btnExport.Enabled = true;
            pictureBox2.Image = null;
            playbt.Enabled = false;
            stopbtn.Enabled = false;
            btnExpTex.Enabled = false;
            listBox2.DataSource = null;
            listBox2.Items.Clear();
            trackBar2.Value = 0;
            trackBar2.Enabled = false;
            dicblock.Clear();
            if (addr.Count > 0)
                addr.Clear();
        }

        public void TexData()
        {
            btnDecompress.Enabled = false;
            btnExport.Enabled = false;
            playbt.Enabled = false;
            stopbtn.Enabled = false;
            btnExpTex.Enabled = true;
            listBox2.DataSource = null;
            listBox2.Items.Clear();
            trackBar2.Value = 0;
            trackBar2.Enabled = true;
            dicblock.Clear();
            if (addr.Count > 0)
                addr.Clear();
        }

        public void SoundData()
        {
            btnDecompress.Enabled = false;
            btnExport.Enabled = true;
            playbt.Enabled = true;
            stopbtn.Enabled = true;
            btnExpTex.Enabled = false;
            listBox2.DataSource = null;
            listBox2.Items.Clear();
            trackBar2.Value = 0;
            trackBar2.Enabled = false;
            dicblock.Clear();
            if (addr.Count > 0)
                addr.Clear();
        }
        #endregion

        private void allWithConvertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StreamWriter filesize = new StreamWriter(path + "\\" + filename + ".FileSizeTable");
            filesize.WriteLine("version " + version);
            for (int j = 0; j < dic.Count; j++)
            {
                byte[] section = dic[j];
                var rbex = new BinaryReader(new MemoryStream(section));
                var num_object = rbex.ReadUInt32();
                int eoffs = Big(rbex.ReadBytes(4));
                int elen = Big(rbex.ReadBytes(4));
                rbex.ReadBytes(4);
                int elemente = Big(rbex.ReadBytes(4));
                string ebf = string.Empty;
                if (elemente == 0)
                    ebf = ".dat";
                else
                    ebf = ".d" + elemente.ToString("d2");
                try
                {
                    var erbf = new BinaryReader(new FileStream(path + "\\" + filename + ebf, FileMode.Open, FileAccess.Read, FileShare.Read));
                    erbf.BaseStream.Seek(eoffs, SeekOrigin.Begin);
                    byte[] getdata = erbf.ReadBytes(elen);
                    using (var data_ar = new BinaryReader(new MemoryStream(getdata)))
                    { 
                        if(data_ar.ReadInt32() == 1936156019)
                        {
                            if (version == 13)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = Big16(data_ar.ReadBytes(2));
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = (UInt16)Big16(data_ar.ReadBytes(2));
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = Big16(tmp);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = Big(data_ar.ReadBytes(4));
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    byte[] blk = data_ar.ReadBytes(bit1[i]);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var bgw = new BinaryReader(new MemoryStream(allone));
                                List<string> lstxt = new List<string>();
                                bool flags = false;
                                if(bgw.ReadInt64() == 2328156834426209092)
                                {
                                    bgw.ReadInt32();
                                    int tobe = Big(bgw.ReadBytes(4)) - 4;
                                    int wnum = Big(bgw.ReadBytes(4));
                                    bgw.ReadBytes(tobe);
                                    bgw.ReadBytes(8);
                                    bgw.ReadInt32();
                                    bgw.ReadInt32(); //length
                                    for(int i = 0; i < wnum; i++)
                                    {
                                        try
                                        {
                                            int nextleg = Big(bgw.ReadBytes(4));
                                            bgw.ReadBytes(6);
                                            byte[] ardata = bgw.ReadBytes(nextleg);
                                            var adatarb = new BinaryReader(new MemoryStream(ardata));
                                            adatarb.ReadInt32();
                                            if (adatarb.ReadInt64() == 6074880098149683011)
                                            {
                                                adatarb.ReadInt32();
                                                int lnum = Big(adatarb.ReadBytes(4));
                                                adatarb.ReadBytes(lnum * 17);
                                                adatarb.ReadBytes(12);
                                                if (adatarb.ReadInt64() == 6870884773368385356)
                                                {
                                                    flags = true;
                                                    adatarb.ReadInt64();
                                                    int cnum = adatarb.ReadByte();
                                                    for (int c = 0; c < cnum; c++)
                                                    {
                                                        adatarb.ReadBytes(4);
                                                        if (adatarb.ReadInt32() == 1196311811)
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            List<int> por = new List<int>();
                                                            for(int g = 0; g < knum; g++)
                                                            {
                                                                adatarb.ReadInt32();//null
                                                                adatarb.ReadInt32();//position
                                                                int addpor = Big(adatarb.ReadBytes(4)) * 2;
                                                                if(addpor != 0)
                                                                    por.Add(addpor);
                                                                adatarb.ReadBytes(16);
                                                            }
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum == 0)
                                                            {
                                                                lstxt.Add("[0]");
                                                            }
                                                            else
                                                            {
                                                                MemoryStream intextms = new MemoryStream(adatarb.ReadBytes(mnum));
                                                                BinaryReader rdmsintext = new BinaryReader(intextms);
                                                                string txtout = "[" + por.Count + "]\n";
                                                                for (int g = 0; g < por.Count; g++)
                                                                {
                                                                    if(g == por.Count - 1)
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString();
                                                                    } 
                                                                    else
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString() + "\n";
                                                                    }
                                                                }
                                                                lstxt.Add(txtout);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            adatarb.ReadBytes(knum * 28);
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum != 0)
                                                                adatarb.ReadBytes(mnum);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Break!");
                                        }
                                        
                                    }

                                }
                                if (flags)
                                {
                                    using (StreamWriter wttxt = new StreamWriter(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".txt"))
                                    {
                                        foreach (String s in lstxt)
                                            wttxt.WriteLine(s);
                                    }
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    filesize.WriteLine(num_object.ToString("x8").ToUpper() + "=" + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                    Console.WriteLine("Done!");
                                }
                                else
                                {
                                    Console.WriteLine("Break!");
                                }
                                
                            }
                            else if (version == 17)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2); 
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var bgw = new BinaryReader(new MemoryStream(allone));
                                List<string> lstxt = new List<string>();
                                bool flags = false;
                                //bool flagsmenus = false;
                                Int64 hbgw = bgw.ReadInt64();
                                if (hbgw == 2328156834426209092)
                                {
                                    bgw.ReadInt32();
                                    int tobe = bgw.ReadInt32() - 4;
                                    int wnum = bgw.ReadInt32();
                                    bgw.ReadBytes(tobe);
                                    bgw.ReadBytes(8);
                                    bgw.ReadInt32();
                                    bgw.ReadInt32(); //length
                                    for (int i = 0; i < wnum; i++)
                                    {
                                        try
                                        {
                                            int nextleg = bgw.ReadInt32();
                                            bgw.ReadBytes(10);
                                            byte ckcs1 = bgw.ReadByte();
                                            if (ckcs1 != 0)
                                                bgw.BaseStream.Seek(bgw.BaseStream.Position - 1, SeekOrigin.Begin);
                                            byte[] ardata = bgw.ReadBytes(nextleg);
                                            var adatarb = new BinaryReader(new MemoryStream(ardata));
                                            adatarb.ReadInt32();
                                            Int64 hadatarb = adatarb.ReadInt64();
                                            if (hadatarb == 6074880098149683011)
                                            {
                                                adatarb.ReadInt32();
                                                int lnum = adatarb.ReadInt32(); ;
                                                adatarb.ReadBytes(lnum * 9);
                                                adatarb.ReadBytes(12);
                                                Int64 hlocal = adatarb.ReadInt64();
                                                if (hlocal == 6870884773368385356)
                                                {
                                                    flags = true;
                                                    adatarb.ReadBytes(5);
                                                    int cnum = adatarb.ReadInt32();
                                                    for (int c = 0; c < cnum; c++)
                                                    {
                                                        adatarb.ReadBytes(4);
                                                        int heng = adatarb.ReadInt32();
                                                        if (heng == 1196311808)
                                                        {
                                                            int knum = adatarb.ReadInt32();
                                                            bool flagdata = false;
                                                            for (int g = 0; g < knum; g++)
                                                            {
                                                                int count = adatarb.ReadInt32();//null
                                                                byte[] txtid = adatarb.ReadBytes(count);
                                                                count = adatarb.ReadInt32();
                                                                byte[] txtout = adatarb.ReadBytes(count);
                                                                string converted = Encoding.Unicode.GetString(txtout);
                                                                StringBuilder builder = new StringBuilder(converted);
                                                                builder.Replace("\r\n", "[rn]");
                                                                builder.Replace("\n\r", "[nr]");
                                                                builder.Replace("\r", "[r]");
                                                                builder.Replace("\n", "[n]");
                                                                builder.Replace("=", "[p]");
                                                                builder.Replace("Ư" ,"[q]");
                                                                builder.Replace("ư", "[w]");
                                                                builder.Replace("Ơ", "[e]");
                                                                builder.Replace("ơ", "[t]");
                                                                if (builder.Length == 0)
                                                                    lstxt.Add(Encoding.ASCII.GetString(txtid) + "=0");
                                                                else
                                                                    lstxt.Add(Encoding.ASCII.GetString(txtid) + "=" + builder.ToString());
                                                                if (adatarb.ReadInt32() == 1)
                                                                {
                                                                    flagdata = true;
                                                                }
                                                            }
                                                            if(flagdata == true)
                                                            {
                                                                int num = adatarb.ReadInt32();
                                                                for (int g = 0; g < num; g++)
                                                                {
                                                                    int count = adatarb.ReadInt32();
                                                                    adatarb.ReadBytes(count);
                                                                    byte ckck = adatarb.ReadByte();
                                                                    if (ckck == 1 && g == num - 1)
                                                                    {
                                                                        adatarb.ReadBytes(5);
                                                                    }
                                                                    else if(ckck == 1)
                                                                    {
                                                                        adatarb.ReadBytes(9);
                                                                    }
                                                                    else if(ckck == 0 && g == num -1)
                                                                    {
                                                                        goto END;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int knum = adatarb.ReadInt32();
                                                            bool flagdata = false;
                                                            for (int g = 0; g < knum; g++)
                                                            {
                                                                int count = adatarb.ReadInt32();//null
                                                                adatarb.ReadBytes(count);
                                                                count = adatarb.ReadInt32();
                                                                adatarb.ReadBytes(count);
                                                                if (adatarb.ReadInt32() == 1)
                                                                {
                                                                    flagdata = true;
                                                                }
                                                            }
                                                            if (flagdata == true)
                                                            {
                                                                int num = adatarb.ReadInt32();
                                                                for (int g = 0; g < num; g++)
                                                                {
                                                                    int count = adatarb.ReadInt32();
                                                                    Debug.WriteLine(Encoding.ASCII.GetString(adatarb.ReadBytes(count)));
                                                                    byte ckck = adatarb.ReadByte();
                                                                    if (ckck == 1 && g == (num - 1))
                                                                    {
                                                                        adatarb.ReadBytes(5);
                                                                    }
                                                                    else if (ckck == 1)
                                                                    {
                                                                        adatarb.ReadBytes(9);
                                                                    }
                                                                    else if(ckck == 0 && g == num -1)
                                                                    {
                                                                        goto END;
                                                                    }
                                                                }
                                                            }
                                                            
                                                        }
                                                        adatarb.ReadBytes(4);
                                                        END:;
                                                    }
                                                }
                                                /*else if(hlocal == 6076285342561748301)
                                                {
                                                    flagsmenus = true;
                                                }*/
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Break!");
                                        }

                                    }

                                }
                                if (flags)
                                {
                                    using (StreamWriter wttxt = new StreamWriter(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".txt"))
                                    {
                                        foreach (String s in lstxt)
                                            wttxt.WriteLine(s);
                                    }
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    filesize.WriteLine(num_object.ToString("x8").ToUpper() + " " + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                    Console.WriteLine("Done!");
                                }
                                else
                                {
                                    Console.WriteLine("Break!");
                                }
                                /*if (flagsmenus)
                                {
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    Console.WriteLine("Done!");
                                }*/

                            }
                            else if (version == 18)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2);
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\exp_data\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\exp_data\\" + ebf + "\\");
                                }
                                Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                var wt2 = new BinaryWriter(new FileStream(path + "\\exp_data\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                wt2.Write(allone);
                                wt2.Flush();
                                wt2.Close();
                                Console.WriteLine(" Done!");
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Null Lenght Byte");
                }
            }
            filesize.Close();
        }

        private void btnExpTex_Click(object sender, EventArgs e)
        {
            svf.FileName = nameoff;
            svf.Filter = "DAT File|*.dds";
            if (svf.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(svf.FileName))
                {
                    using (var wt = new BinaryWriter(new FileStream(svf.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        wt.Write(savef);
                        Console.WriteLine("Save file {0} done!", Path.GetFileName(svf.FileName));
                    }
                    savef = null;
                }
            }
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void tableSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile.Filter = "File Size Table| *.FileSizeTable";
            if(openFile.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(openFile.FileName))
                {
                    Console.WriteLine("Open file {0}", Path.GetFileName(openFile.FileName));
                    Dictionary<string, string> keyfile = new Dictionary<string, string>();
                    path = Path.GetDirectoryName(openFile.FileName);
                    filename = Path.GetFileNameWithoutExtension(openFile.FileName);
                    string[] sizetable = File.ReadAllLines(openFile.FileName);
                    string bigfnew = string.Empty;
                    int filechk = 0;
                    for(int i = 0; i < 50; i++)
                    {
                        if(i != 0)
                        {
                            if(!File.Exists(path + "\\" + filename + ".d" + i.ToString("d2")))
                            {
                                bigfnew = path + "\\" + filename + ".d" + i.ToString("d2");
                                File.Create(path + "\\" + filename + ".d" + i.ToString("d2") + "new");
                                filechk = i;
                                break;
                            }
                            else if (File.Exists(path + "\\" + filename + ".d" + i.ToString("d2")+ "new"))
                            {
                                bigfnew = path + "\\" + filename + ".d" + i.ToString("d2");
                                filechk = i;
                                break;
                            }
                        }
                    }
                    BinaryWriter bigfile = new BinaryWriter(new FileStream(bigfnew, FileMode.Create, FileAccess.Write, FileShare.Read));
                    bigfile.Write(Encoding.ASCII.GetBytes("QUANTICDREAMTABINDEX"));
                    bigfile.Write(285212672);
                    for (int i = 0; i < 2048; i++)
                    {
                        if (bigfile.BaseStream.Position == 2048)
                        {
                            break;
                        }
                        else
                        {
                            bigfile.Write((byte)0x2D);
                        }
                    }
                    foreach (string addrkey in sizetable)
                    {
                        var data = addrkey.Split(' ');
                        if(data[0] == "version")
                        {
                            version = int.Parse(data[1]);
                            Console.WriteLine("Version {0}", version);
                        }
                        else
                        {
                            keyfile.Add(data[0], data[1]);
                        }
                    }
                    if(version == 13)
                    {
                        
                    }
                    else if(version == 17)
                    {
                        foreach(var pair in keyfile)
                        {
                            if(!File.Exists(path + pair.Value + "_bk"))
                            {
                                File.Copy(path + pair.Value, path + pair.Value + "_bk");
                                Console.WriteLine("Backup file done!");
                            }
                            if(File.Exists(Path.GetDirectoryName(path + pair.Value) + "\\" + Path.GetFileNameWithoutExtension(path + pair.Value) + ".txt"))
                            {
                                Console.WriteLine("Load file {0}", Path.GetFileNameWithoutExtension(path + pair.Value) + ".txt");
                                using (var wt = new BinaryWriter(new FileStream(path + pair.Value, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                                {
                                    using(var rd = new BinaryReader(new FileStream(path + pair.Value + "_bk", FileMode.Open, FileAccess.Read, FileShare.Read)))
                                    {
                                        Dictionary<string, string> textline = new Dictionary<string, string>();
                                        string[] text = File.ReadAllLines(Path.GetDirectoryName(path + pair.Value) + "\\" + Path.GetFileNameWithoutExtension(path + pair.Value) + ".txt");
                                        foreach(string line in text)
                                        {
                                            var dataline = line.Split('=');
                                            textline.Add(dataline[0], dataline[1]);
                                        }
                                        wt.Write(rd.ReadBytes(12));
                                        int tobe = rd.ReadInt32();
                                        int wnum = rd.ReadInt32();
                                        wt.Write(tobe);
                                        wt.Write(wnum);
                                        wt.Write(rd.ReadBytes(tobe + 8));
                                        //long porlen = wt.BaseStream.Position; //lengpor
                                        int lenb = rd.ReadInt32();
                                        byte[] datasheet = rd.ReadBytes(lenb);
                                        BinaryReader msrd = new BinaryReader(new MemoryStream(datasheet));
                                        BinaryWriter tmp = new BinaryWriter(new FileStream("dump.tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
                                        for (int i = 0; i < wnum; i++)
                                        {
                                            var dumpfs = new BinaryWriter(new FileStream(i + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
                                            int knum = msrd.ReadInt32();
                                            byte[] nullh = msrd.ReadBytes(10);
                                            byte ckscks = msrd.ReadByte();
                                            if (ckscks != 0)
                                                msrd.BaseStream.Seek(msrd.BaseStream.Position - 1, SeekOrigin.Begin);
                                            byte[] aldata = msrd.ReadBytes(knum);
                                            var msrdm = new BinaryReader(new MemoryStream(aldata));
                                            dumpfs.Write(msrdm.ReadInt32());
                                            Int64 hadatarb = msrdm.ReadInt64();
                                            dumpfs.Write(hadatarb);
                                            try
                                            {
                                                if (hadatarb == 6074880098149683011)
                                                {
                                                    dumpfs.Write(msrdm.ReadInt32());
                                                    int lnum = msrdm.ReadInt32();
                                                    dumpfs.Write(lnum);
                                                    dumpfs.Write(msrdm.ReadBytes(lnum * 9));
                                                    dumpfs.Write(msrdm.ReadBytes(12));
                                                    Int64 hlocal = msrdm.ReadInt64();
                                                    dumpfs.Write(hlocal);
                                                    if (hlocal == 6870884773368385356)
                                                    {
                                                        dumpfs.Write(msrdm.ReadBytes(5));
                                                        int cnum = msrdm.ReadInt32();
                                                        dumpfs.Write(cnum);
                                                        for (int c = 0; c < cnum; c++)
                                                        {
                                                            dumpfs.Write(msrdm.ReadBytes(4));
                                                            int heng = msrdm.ReadInt32();
                                                            dumpfs.Write(heng);
                                                            if (heng == 1196311808)
                                                            {
                                                                int count = msrdm.ReadInt32();
                                                                dumpfs.Write(count);
                                                                bool flagdata = false;
                                                                for (int j = 0; j < count; j++)
                                                                {
                                                                    int lenid = msrdm.ReadInt32();
                                                                    dumpfs.Write(lenid);
                                                                    byte[] idsubb = msrdm.ReadBytes(lenid);
                                                                    string idsub = Encoding.ASCII.GetString(idsubb);
                                                                    dumpfs.Write(idsubb);
                                                                    lenid = msrdm.ReadInt32();
                                                                    msrdm.ReadBytes(lenid);
                                                                    string sub = textline[idsub];
                                                                    StringBuilder builder = new StringBuilder(sub);
                                                                    builder.Replace("[rn]", "\r\n");
                                                                    builder.Replace("[nr]", "\n\r");
                                                                    builder.Replace("[r]", "\r");
                                                                    builder.Replace("[n]", "\n");
                                                                    builder.Replace("[p]", "=");
                                                                    builder.Replace("Ư", "Ū");
                                                                    builder.Replace("ư", "ū");
                                                                    builder.Replace("Ơ", "Ō");
                                                                    builder.Replace("ơ", "ō");
                                                                    builder.Replace("[q]", "Ư");
                                                                    builder.Replace("[w]", "ư");
                                                                    builder.Replace("[e]", "Ơ");
                                                                    builder.Replace("[t]", "ơ");
                                                                    if (sub != "0")
                                                                    {
                                                                        byte[] chargesub = Encoding.Unicode.GetBytes(builder.ToString());
                                                                        dumpfs.Write(chargesub.Length);
                                                                        dumpfs.Write(chargesub);
                                                                    }
                                                                    else
                                                                    {
                                                                        dumpfs.Write(0);
                                                                        //msrdm.ReadInt32();
                                                                    }
                                                                    int ckmk = msrdm.ReadInt32();
                                                                    dumpfs.Write(ckmk);
                                                                    if (ckmk == 1)
                                                                        flagdata = true;
                                                                }
                                                                if (flagdata == true)
                                                                {
                                                                    int pnum = msrdm.ReadInt32();
                                                                    dumpfs.Write(pnum);
                                                                    for (int g = 0; g < pnum; g++)
                                                                    {
                                                                        int nm = msrdm.ReadInt32();
                                                                        dumpfs.Write(nm);
                                                                        dumpfs.Write(msrdm.ReadBytes(nm));
                                                                        byte cck = msrdm.ReadByte();
                                                                        dumpfs.Write(cck);
                                                                        if (cck == 1 && g == pnum - 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(5));
                                                                        }
                                                                        else if (cck == 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(9));
                                                                        }
                                                                        else
                                                                        {
                                                                            goto ENDCK;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                int count = msrdm.ReadInt32();
                                                                dumpfs.Write(count);
                                                                bool flagdata = false;
                                                                for (int j = 0; j < count; j++)
                                                                {
                                                                    int lenid = msrdm.ReadInt32();
                                                                    dumpfs.Write(lenid);
                                                                    dumpfs.Write(msrdm.ReadBytes(lenid));
                                                                    lenid = msrdm.ReadInt32();
                                                                    if(lenid != 0)
                                                                    {
                                                                        dumpfs.Write(lenid);
                                                                        dumpfs.Write(msrdm.ReadBytes(lenid));
                                                                    }
                                                                    else
                                                                    {
                                                                        dumpfs.Write(lenid);
                                                                    }
                                                                    int ckmk = msrdm.ReadInt32();
                                                                    dumpfs.Write(ckmk);
                                                                    if (ckmk == 1)
                                                                        flagdata = true;
                                                                }
                                                                if (flagdata == true)
                                                                {
                                                                    int pnum = msrdm.ReadInt32();
                                                                    dumpfs.Write(pnum);
                                                                    for (int g = 0; g < pnum; g++)
                                                                    {
                                                                        int nm = msrdm.ReadInt32();
                                                                        dumpfs.Write(nm);
                                                                        dumpfs.Write(msrdm.ReadBytes(nm));
                                                                        byte cck = msrdm.ReadByte();
                                                                        dumpfs.Write(cck);
                                                                        if (cck == 1 && g == pnum - 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(5));
                                                                        }
                                                                        else if (cck == 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(9));
                                                                        }
                                                                        else if (cck == 0 && g == pnum - 1)
                                                                        {
                                                                            goto ENDCK;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            dumpfs.Write(msrdm.ReadBytes(4));
                                                            Debug.WriteLine(msrdm.BaseStream.Position);
                                                            ENDCK:;
                                                        }
                                                        dumpfs.Close();
                                                        byte[] dumpall = File.ReadAllBytes(i + ".tmp");
                                                        tmp.Write(dumpall.Length);
                                                        tmp.Write(nullh);
                                                        if (ckscks != 0)
                                                            tmp.Write(dumpall);
                                                        else
                                                        {
                                                            tmp.Write(ckscks);
                                                            tmp.Write(dumpall);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        tmp.Write(knum);
                                                        tmp.Write(nullh);
                                                        if (ckscks != 0)
                                                            tmp.Write(aldata);
                                                        else
                                                        {
                                                            tmp.Write(ckscks);
                                                            tmp.Write(aldata);
                                                        }
                                                        dumpfs.Close();
                                                        File.Delete(i + ".tmp");
                                                    }
                                                }
                                                else
                                                {
                                                    tmp.Write(knum);
                                                    tmp.Write(nullh);
                                                    if (ckscks != 0)
                                                        tmp.Write(aldata);
                                                    else
                                                    {
                                                        tmp.Write(ckscks);
                                                        tmp.Write(aldata);
                                                    }
                                                    dumpfs.Close();
                                                    File.Delete(i + ".tmp");
                                                }
                                                dumpfs.Close();
                                                File.Delete(i + ".tmp");
                                            }
                                            catch
                                            {
                                                tmp.Write(knum);
                                                tmp.Write(nullh);
                                                if (ckscks != 0)
                                                    tmp.Write(aldata);
                                                else
                                                {
                                                    tmp.Write(ckscks);
                                                    tmp.Write(aldata);
                                                }
                                                dumpfs.Close();
                                                File.Delete(i + ".tmp");
                                            }
                                            
                                        }
                                        tmp.Close();
                                        byte[] ftmp = File.ReadAllBytes("dump.tmp");
                                        wt.Write(ftmp.Length);
                                        wt.Write(ftmp);
                                        wt.Flush();
                                        wt.Close();
                                        File.Delete("dump.tmp");
                                    }
                                }
                                Console.WriteLine("Import file {0} done!", Path.GetFileName(path + pair.Value));
                                
                            }
                            
                            for (int i = 0; i < 64; i++)
                            {
                                if (bigfile.BaseStream.Position % 64 != 0)
                                {
                                    bigfile.Write((byte)0x2D);
                                }
                                else
                                {
                                    byte[] dcsegs = File.ReadAllBytes(path + pair.Value);
                                    if(!File.Exists(path + "\\" + filename + ".idm_bk"))
                                    {
                                        File.Copy(path + "\\" + filename + ".idm", path + "\\" + filename + ".idm_bk");
                                    }
                                    using(var idmapwt = new BinaryWriter(new FileStream(path + "\\" + filename + ".idm", FileMode.Open, FileAccess.Write, FileShare.ReadWrite)))
                                    {
                                        byte[] raidm = File.ReadAllBytes(path + "\\" + filename + ".idm_bk");
                                        using (var idmaprd = new BinaryReader(new MemoryStream(raidm)))
                                        {
                                            while (idmaprd.BaseStream.Position != idmaprd.BaseStream.Length)
                                            {
                                                int ckpair = Big(idmaprd.ReadBytes(4));
                                                if (ckpair == Int32.Parse(pair.Key, System.Globalization.NumberStyles.HexNumber))
                                                {
                                                    long porsave = idmaprd.BaseStream.Position;
                                                    if (idmaprd.ReadInt32() == (int)new Int32Converter().ConvertFromString(Path.GetFileNameWithoutExtension(pair.Value)))
                                                    {
                                                        idmapwt.BaseStream.Seek(porsave, SeekOrigin.Begin);
                                                        idmapwt.Write((int)bigfile.BaseStream.Position);
                                                        idmapwt.Write(dcsegs.Length);
                                                        idmapwt.BaseStream.Seek(idmapwt.BaseStream.Position + 4, SeekOrigin.Begin);
                                                        idmapwt.Write(filechk);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    bigfile.Write(dcsegs);
                                    break;
                                }
                            }
                        }
                    }
                    

                    bigfile.Flush();
                    bigfile.Close();
                }
            }
        }

        private void allSegsMENUSMSTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StreamWriter filesize = new StreamWriter(path + "\\" + filename + ".FileSizeTable");
            filesize.WriteLine("version " + version);
            for (int j = 0; j < dic.Count; j++)
            {
                byte[] section = dic[j];
                var rbex = new BinaryReader(new MemoryStream(section));
                var num_object = rbex.ReadUInt32();
                int eoffs = Big(rbex.ReadBytes(4));
                int elen = Big(rbex.ReadBytes(4));
                rbex.ReadBytes(4);
                int elemente = Big(rbex.ReadBytes(4));
                string ebf = string.Empty;
                if (elemente == 0)
                    ebf = ".dat";
                else
                    ebf = ".d" + elemente.ToString("d2");
                try
                {
                    var erbf = new BinaryReader(new FileStream(path + "\\" + filename + ebf, FileMode.Open, FileAccess.Read, FileShare.Read));
                    erbf.BaseStream.Seek(eoffs, SeekOrigin.Begin);
                    byte[] getdata = erbf.ReadBytes(elen);
                    using (var data_ar = new BinaryReader(new MemoryStream(getdata)))
                    {
                        if (data_ar.ReadInt32() == 1936156019)
                        {
                            if (version == 13)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = Big16(data_ar.ReadBytes(2));
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = (UInt16)Big16(data_ar.ReadBytes(2));
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = Big16(tmp);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = Big(data_ar.ReadBytes(4));
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    byte[] blk = data_ar.ReadBytes(bit1[i]);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var bgw = new BinaryReader(new MemoryStream(allone));
                                List<string> lstxt = new List<string>();
                                bool flags = false;
                                if (bgw.ReadInt64() == 2328156834426209092)
                                {
                                    bgw.ReadInt32();
                                    int tobe = Big(bgw.ReadBytes(4)) - 4;
                                    int wnum = Big(bgw.ReadBytes(4));
                                    bgw.ReadBytes(tobe);
                                    bgw.ReadBytes(8);
                                    bgw.ReadInt32();
                                    bgw.ReadInt32(); //length
                                    for (int i = 0; i < wnum; i++)
                                    {
                                        try
                                        {
                                            int nextleg = Big(bgw.ReadBytes(4));
                                            bgw.ReadBytes(6);
                                            byte[] ardata = bgw.ReadBytes(nextleg);
                                            var adatarb = new BinaryReader(new MemoryStream(ardata));
                                            adatarb.ReadInt32();
                                            if (adatarb.ReadInt64() == 6074880098149683011)
                                            {
                                                adatarb.ReadInt32();
                                                int lnum = Big(adatarb.ReadBytes(4));
                                                adatarb.ReadBytes(lnum * 17);
                                                adatarb.ReadBytes(12);
                                                if (adatarb.ReadInt64() == 6870884773368385356)
                                                {
                                                    flags = true;
                                                    adatarb.ReadInt64();
                                                    int cnum = adatarb.ReadByte();
                                                    for (int c = 0; c < cnum; c++)
                                                    {
                                                        adatarb.ReadBytes(4);
                                                        if (adatarb.ReadInt32() == 1196311811)
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            List<int> por = new List<int>();
                                                            for (int g = 0; g < knum; g++)
                                                            {
                                                                adatarb.ReadInt32();//null
                                                                adatarb.ReadInt32();//position
                                                                int addpor = Big(adatarb.ReadBytes(4)) * 2;
                                                                if (addpor != 0)
                                                                    por.Add(addpor);
                                                                adatarb.ReadBytes(16);
                                                            }
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum == 0)
                                                            {
                                                                lstxt.Add("[0]");
                                                            }
                                                            else
                                                            {
                                                                MemoryStream intextms = new MemoryStream(adatarb.ReadBytes(mnum));
                                                                BinaryReader rdmsintext = new BinaryReader(intextms);
                                                                string txtout = "[" + por.Count + "]\n";
                                                                for (int g = 0; g < por.Count; g++)
                                                                {
                                                                    if (g == por.Count - 1)
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString();
                                                                    }
                                                                    else
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString() + "\n";
                                                                    }
                                                                }
                                                                lstxt.Add(txtout);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            adatarb.ReadBytes(knum * 28);
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum != 0)
                                                                adatarb.ReadBytes(mnum);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Break!");
                                        }

                                    }

                                }
                                if (flags)
                                {
                                    using (StreamWriter wttxt = new StreamWriter(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".txt"))
                                    {
                                        foreach (String s in lstxt)
                                            wttxt.WriteLine(s);
                                    }
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    filesize.WriteLine(num_object.ToString("x8").ToUpper() + "=" + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                    Console.WriteLine("Done!");
                                }
                                else
                                {
                                    Console.WriteLine("Break!");
                                }

                            }
                            else if (version == 17)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2);
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var bgw = new BinaryReader(new MemoryStream(allone));
                                List<string> lstxt = new List<string>();
                                //bool flags = false;
                                bool flagsmenus = false;
                                Int64 hbgw = bgw.ReadInt64();
                                if (hbgw == 2328156834426209092)
                                {
                                    bgw.ReadInt32();
                                    int tobe = bgw.ReadInt32() - 4;
                                    int wnum = bgw.ReadInt32();
                                    bgw.ReadBytes(tobe);
                                    bgw.ReadBytes(8);
                                    bgw.ReadInt32();
                                    bgw.ReadInt32(); //length
                                    for (int i = 0; i < wnum; i++)
                                    {
                                        try
                                        {
                                            int nextleg = bgw.ReadInt32();
                                            bgw.ReadBytes(10);
                                            byte ckcs1 = bgw.ReadByte();
                                            if (ckcs1 != 0)
                                                bgw.BaseStream.Seek(bgw.BaseStream.Position - 1, SeekOrigin.Begin);
                                            byte[] ardata = bgw.ReadBytes(nextleg);
                                            var adatarb = new BinaryReader(new MemoryStream(ardata));
                                            adatarb.ReadInt32();
                                            Int64 hadatarb = adatarb.ReadInt64();
                                            if (hadatarb == 6074880098149683011)
                                            {
                                                adatarb.ReadInt32();
                                                int lnum = adatarb.ReadInt32(); ;
                                                adatarb.ReadBytes(lnum * 9);
                                                adatarb.ReadBytes(12);
                                                Int64 hlocal = adatarb.ReadInt64();
                                                if(hlocal == 6076285342561748301)
                                                {
                                                    flagsmenus = true;
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Break!");
                                        }

                                    }

                                }
                                if (flagsmenus)
                                {
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    filesize.WriteLine(num_object.ToString("x8").ToUpper() + " " + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                    Console.WriteLine("Done!");
                                }

                            }
                            else if (version == 18)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2);
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\exp_data\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\exp_data\\" + ebf + "\\");
                                }
                                Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                var wt2 = new BinaryWriter(new FileStream(path + "\\exp_data\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                wt2.Write(allone);
                                wt2.Flush();
                                wt2.Close();
                                Console.WriteLine(" Done!");
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Null Lenght Byte");
                }
            }
            filesize.Close();
        }

        private void allQZIPNoDecompressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StreamWriter filesize = new StreamWriter(path + "\\" + filename + ".FileSizeTable");
            filesize.WriteLine("version " + version);
            for (int j = 0; j < dic.Count; j++)
            {
                byte[] section = dic[j];
                var rbex = new BinaryReader(new MemoryStream(section));
                var num_object = rbex.ReadUInt32();
                int eoffs = Big(rbex.ReadBytes(4));
                int elen = Big(rbex.ReadBytes(4));
                rbex.ReadBytes(4);
                int elemente = Big(rbex.ReadBytes(4));
                string ebf = string.Empty;
                if (elemente == 0)
                    ebf = ".dat";
                else
                    ebf = ".d" + elemente.ToString("d2");
                try
                {
                    var erbf = new BinaryReader(new FileStream(path + "\\" + filename + ebf, FileMode.Open, FileAccess.Read, FileShare.Read));
                    erbf.BaseStream.Seek(eoffs, SeekOrigin.Begin);
                    byte[] allone = erbf.ReadBytes(elen);
                    using (var data_ar = new BinaryReader(new MemoryStream(allone)))
                    {
                        if (version == 17)
                        {
                            int pivot = 0;
                            for (int i = 0; i < dicblock.Count; i++)
                            {
                                Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                pivot += dicblock[i].Length;
                            }
                            if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                            {
                                Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                            }
                            var bgw = new BinaryReader(new MemoryStream(allone));
                            List<string> lstxt = new List<string>();
                            bool flags = false;
                            //bool flagsmenus = false;
                            bgw.ReadBytes(5);
                            Int64 hbgw = bgw.ReadInt64();
                            if (hbgw == 2328156834426209092)
                            {
                                bgw.ReadInt32();
                                int tobe = bgw.ReadInt32() - 4;
                                int wnum = bgw.ReadInt32();
                                bgw.ReadBytes(tobe);
                                bgw.ReadBytes(8);
                                bgw.ReadInt32();
                                bgw.ReadInt32(); //length
                                for (int i = 0; i < wnum; i++)
                                {
                                    try
                                    {
                                        int nextleg = bgw.ReadInt32();
                                        bgw.ReadBytes(10);
                                        byte ckcs1 = bgw.ReadByte();
                                        if (ckcs1 != 0)
                                            bgw.BaseStream.Seek(bgw.BaseStream.Position - 1, SeekOrigin.Begin);
                                        byte[] ardata = bgw.ReadBytes(nextleg);
                                        var adatarb = new BinaryReader(new MemoryStream(ardata));
                                        adatarb.ReadInt32();
                                        Int64 hadatarb = adatarb.ReadInt64();
                                        if (hadatarb == 6074880098149683011)
                                        {
                                            adatarb.ReadInt32();
                                            int lnum = adatarb.ReadInt32(); ;
                                            adatarb.ReadBytes(lnum * 9);
                                            adatarb.ReadBytes(12);
                                            Int64 hlocal = adatarb.ReadInt64();
                                            if (hlocal == 6870884773368385356)
                                            {
                                                flags = true;
                                                adatarb.ReadBytes(5);
                                                int cnum = adatarb.ReadInt32();
                                                for (int c = 0; c < cnum; c++)
                                                {
                                                    adatarb.ReadBytes(4);
                                                    int heng = adatarb.ReadInt32();
                                                    if (heng == 1196311808)
                                                    {
                                                        int knum = adatarb.ReadInt32();
                                                        bool flagdata = false;
                                                        for (int g = 0; g < knum; g++)
                                                        {
                                                            int count = adatarb.ReadInt32();//null
                                                            byte[] txtid = adatarb.ReadBytes(count);
                                                            count = adatarb.ReadInt32();
                                                            byte[] txtout = adatarb.ReadBytes(count);
                                                            string converted = Encoding.Unicode.GetString(txtout);
                                                            StringBuilder builder = new StringBuilder(converted);
                                                            builder.Replace("\r\n", "[rn]");
                                                            builder.Replace("\n\r", "[nr]");
                                                            builder.Replace("\r", "[r]");
                                                            builder.Replace("\n", "[n]");
                                                            builder.Replace("=", "[p]");
                                                            builder.Replace("Ư", "[q]");
                                                            builder.Replace("ư", "[w]");
                                                            builder.Replace("Ơ", "[e]");
                                                            builder.Replace("ơ", "[t]");
                                                            if (builder.Length == 0)
                                                                lstxt.Add(Encoding.ASCII.GetString(txtid) + "=0");
                                                            else
                                                                lstxt.Add(Encoding.ASCII.GetString(txtid) + "=" + builder.ToString());
                                                            if (adatarb.ReadInt32() == 1)
                                                            {
                                                                flagdata = true;
                                                            }
                                                        }
                                                        if (flagdata == true)
                                                        {
                                                            int num = adatarb.ReadInt32();
                                                            for (int g = 0; g < num; g++)
                                                            {
                                                                int count = adatarb.ReadInt32();
                                                                adatarb.ReadBytes(count);
                                                                byte ckck = adatarb.ReadByte();
                                                                if (ckck == 1 && g == num - 1)
                                                                {
                                                                    adatarb.ReadBytes(5);
                                                                }
                                                                else if (ckck == 1)
                                                                {
                                                                    adatarb.ReadBytes(9);
                                                                }
                                                                else if (ckck == 0 && g == num - 1)
                                                                {
                                                                    goto END;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        int knum = adatarb.ReadInt32();
                                                        bool flagdata = false;
                                                        for (int g = 0; g < knum; g++)
                                                        {
                                                            int count = adatarb.ReadInt32();//null
                                                            adatarb.ReadBytes(count);
                                                            count = adatarb.ReadInt32();
                                                            adatarb.ReadBytes(count);
                                                            if (adatarb.ReadInt32() == 1)
                                                            {
                                                                flagdata = true;
                                                            }
                                                        }
                                                        if (flagdata == true)
                                                        {
                                                            int num = adatarb.ReadInt32();
                                                            for (int g = 0; g < num; g++)
                                                            {
                                                                int count = adatarb.ReadInt32();
                                                                Debug.WriteLine(Encoding.ASCII.GetString(adatarb.ReadBytes(count)));
                                                                byte ckck = adatarb.ReadByte();
                                                                if (ckck == 1 && g == (num - 1))
                                                                {
                                                                    adatarb.ReadBytes(5);
                                                                }
                                                                else if (ckck == 1)
                                                                {
                                                                    adatarb.ReadBytes(9);
                                                                }
                                                                else if (ckck == 0 && g == num - 1)
                                                                {
                                                                    goto END;
                                                                }
                                                            }
                                                        }

                                                    }
                                                    adatarb.ReadBytes(4);
                                                END:;
                                                }
                                            }
                                            /*else if(hlocal == 6076285342561748301)
                                            {
                                                flagsmenus = true;
                                            }*/
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Break!");
                                    }

                                }

                            }
                            if (flags)
                            {
                                using (StreamWriter wttxt = new StreamWriter(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".txt"))
                                {
                                    foreach (String s in lstxt)
                                        wttxt.WriteLine(s);
                                }
                                Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                wt2.Write(allone);
                                wt2.Flush();
                                wt2.Close();
                                filesize.WriteLine(num_object.ToString("x8").ToUpper() + " " + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                Console.WriteLine("Done!");
                            }
                            else
                            {
                                Console.WriteLine("Break!");
                            }
                            /*if (flagsmenus)
                            {
                                Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                wt2.Write(allone);
                                wt2.Flush();
                                wt2.Close();
                                Console.WriteLine("Done!");
                            }*/

                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Null Lenght Byte");
                }
            }
            filesize.Close();
        }

        private void allQZIPCompressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StreamWriter filesize = new StreamWriter(path + "\\" + filename + ".FileSizeTable");
            filesize.WriteLine("version " + version);
            for (int j = 0; j < dic.Count; j++)
            {
                byte[] section = dic[j];
                var rbex = new BinaryReader(new MemoryStream(section));
                var num_object = rbex.ReadUInt32();
                int eoffs = Big(rbex.ReadBytes(4));
                int elen = Big(rbex.ReadBytes(4));
                rbex.ReadBytes(4);
                int elemente = Big(rbex.ReadBytes(4));
                string ebf = string.Empty;
                if (elemente == 0)
                    ebf = ".dat";
                else
                    ebf = ".d" + elemente.ToString("d2");
                try
                {
                    var erbf = new BinaryReader(new FileStream(path + "\\" + filename + ebf, FileMode.Open, FileAccess.Read, FileShare.Read));
                    erbf.BaseStream.Seek(eoffs, SeekOrigin.Begin);
                    byte[] getdata = erbf.ReadBytes(elen);
                    using (var data_ar = new BinaryReader(new MemoryStream(getdata)))
                    { 
                        if(data_ar.ReadInt32() == 1936156019)
                        {
                            if (version == 13)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = Big16(data_ar.ReadBytes(2));
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = (UInt16)Big16(data_ar.ReadBytes(2));
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = Big16(tmp);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = Big(data_ar.ReadBytes(4));
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    byte[] blk = data_ar.ReadBytes(bit1[i]);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var bgw = new BinaryReader(new MemoryStream(allone));
                                List<string> lstxt = new List<string>();
                                bool flags = false;
                                if(bgw.ReadInt64() == 2328156834426209092)
                                {
                                    bgw.ReadInt32();
                                    int tobe = Big(bgw.ReadBytes(4)) - 4;
                                    int wnum = Big(bgw.ReadBytes(4));
                                    bgw.ReadBytes(tobe);
                                    bgw.ReadBytes(8);
                                    bgw.ReadInt32();
                                    bgw.ReadInt32(); //length
                                    for(int i = 0; i < wnum; i++)
                                    {
                                        try
                                        {
                                            int nextleg = Big(bgw.ReadBytes(4));
                                            bgw.ReadBytes(6);
                                            byte[] ardata = bgw.ReadBytes(nextleg);
                                            var adatarb = new BinaryReader(new MemoryStream(ardata));
                                            adatarb.ReadInt32();
                                            if (adatarb.ReadInt64() == 6074880098149683011)
                                            {
                                                adatarb.ReadInt32();
                                                int lnum = Big(adatarb.ReadBytes(4));
                                                adatarb.ReadBytes(lnum * 17);
                                                adatarb.ReadBytes(12);
                                                if (adatarb.ReadInt64() == 6870884773368385356)
                                                {
                                                    flags = true;
                                                    adatarb.ReadInt64();
                                                    int cnum = adatarb.ReadByte();
                                                    for (int c = 0; c < cnum; c++)
                                                    {
                                                        adatarb.ReadBytes(4);
                                                        if (adatarb.ReadInt32() == 1196311811)
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            List<int> por = new List<int>();
                                                            for(int g = 0; g < knum; g++)
                                                            {
                                                                adatarb.ReadInt32();//null
                                                                adatarb.ReadInt32();//position
                                                                int addpor = Big(adatarb.ReadBytes(4)) * 2;
                                                                if(addpor != 0)
                                                                    por.Add(addpor);
                                                                adatarb.ReadBytes(16);
                                                            }
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum == 0)
                                                            {
                                                                lstxt.Add("[0]");
                                                            }
                                                            else
                                                            {
                                                                MemoryStream intextms = new MemoryStream(adatarb.ReadBytes(mnum));
                                                                BinaryReader rdmsintext = new BinaryReader(intextms);
                                                                string txtout = "[" + por.Count + "]\n";
                                                                for (int g = 0; g < por.Count; g++)
                                                                {
                                                                    if(g == por.Count - 1)
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString();
                                                                    } 
                                                                    else
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString() + "\n";
                                                                    }
                                                                }
                                                                lstxt.Add(txtout);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            adatarb.ReadBytes(knum * 28);
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum != 0)
                                                                adatarb.ReadBytes(mnum);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Break!");
                                        }
                                        
                                    }

                                }
                                if (flags)
                                {
                                    using (StreamWriter wttxt = new StreamWriter(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".txt"))
                                    {
                                        foreach (String s in lstxt)
                                            wttxt.WriteLine(s);
                                    }
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    filesize.WriteLine(num_object.ToString("x8").ToUpper() + "=" + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                    Console.WriteLine("Done!");
                                }
                                else
                                {
                                    Console.WriteLine("Break!");
                                }
                                
                            }
                            else if (version == 17)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2); 
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var bgw = new BinaryReader(new MemoryStream(allone));
                                List<string> lstxt = new List<string>();
                                bool flags = false;
                                //bool flagsmenus = false;
                                Int64 hbgw = bgw.ReadInt64();
                                if (hbgw == 6864405025180441169)
                                {
                                    bgw.ReadInt32();
                                    int tobe = bgw.ReadInt32() - 4;
                                    int wnum = bgw.ReadInt32();
                                    bgw.ReadBytes(tobe);
                                    bgw.ReadBytes(8);
                                    bgw.ReadInt32();
                                    bgw.ReadInt32(); //length
                                    for (int i = 0; i < wnum; i++)
                                    {
                                        try
                                        {
                                            int nextleg = bgw.ReadInt32();
                                            bgw.ReadBytes(10);
                                            byte ckcs1 = bgw.ReadByte();
                                            if (ckcs1 != 0)
                                                bgw.BaseStream.Seek(bgw.BaseStream.Position - 1, SeekOrigin.Begin);
                                            byte[] ardata = bgw.ReadBytes(nextleg);
                                            var adatarb = new BinaryReader(new MemoryStream(ardata));
                                            adatarb.ReadInt32();
                                            Int64 hadatarb = adatarb.ReadInt64();
                                            if (hadatarb == 6074880098149683011)
                                            {
                                                adatarb.ReadInt32();
                                                int lnum = adatarb.ReadInt32(); ;
                                                adatarb.ReadBytes(lnum * 9);
                                                adatarb.ReadBytes(12);
                                                Int64 hlocal = adatarb.ReadInt64();
                                                if (hlocal == 6870884773368385356)
                                                {
                                                    flags = true;
                                                    adatarb.ReadBytes(5);
                                                    int cnum = adatarb.ReadInt32();
                                                    for (int c = 0; c < cnum; c++)
                                                    {
                                                        adatarb.ReadBytes(4);
                                                        int heng = adatarb.ReadInt32();
                                                        if (heng == 1196311808)
                                                        {
                                                            int knum = adatarb.ReadInt32();
                                                            bool flagdata = false;
                                                            for (int g = 0; g < knum; g++)
                                                            {
                                                                int count = adatarb.ReadInt32();//null
                                                                byte[] txtid = adatarb.ReadBytes(count);
                                                                count = adatarb.ReadInt32();
                                                                byte[] txtout = adatarb.ReadBytes(count);
                                                                string converted = Encoding.Unicode.GetString(txtout);
                                                                StringBuilder builder = new StringBuilder(converted);
                                                                builder.Replace("\r\n", "[rn]");
                                                                builder.Replace("\n\r", "[nr]");
                                                                builder.Replace("\r", "[r]");
                                                                builder.Replace("\n", "[n]");
                                                                builder.Replace("=", "[p]");
                                                                builder.Replace("Ư" ,"[q]");
                                                                builder.Replace("ư", "[w]");
                                                                builder.Replace("Ơ", "[e]");
                                                                builder.Replace("ơ", "[t]");
                                                                if (builder.Length == 0)
                                                                    lstxt.Add(Encoding.ASCII.GetString(txtid) + "=0");
                                                                else
                                                                    lstxt.Add(Encoding.ASCII.GetString(txtid) + "=" + builder.ToString());
                                                                if (adatarb.ReadInt32() == 1)
                                                                {
                                                                    flagdata = true;
                                                                }
                                                            }
                                                            if(flagdata == true)
                                                            {
                                                                int num = adatarb.ReadInt32();
                                                                for (int g = 0; g < num; g++)
                                                                {
                                                                    int count = adatarb.ReadInt32();
                                                                    adatarb.ReadBytes(count);
                                                                    byte ckck = adatarb.ReadByte();
                                                                    if (ckck == 1 && g == num - 1)
                                                                    {
                                                                        adatarb.ReadBytes(5);
                                                                    }
                                                                    else if(ckck == 1)
                                                                    {
                                                                        adatarb.ReadBytes(9);
                                                                    }
                                                                    else if(ckck == 0 && g == num -1)
                                                                    {
                                                                        goto END;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int knum = adatarb.ReadInt32();
                                                            bool flagdata = false;
                                                            for (int g = 0; g < knum; g++)
                                                            {
                                                                int count = adatarb.ReadInt32();//null
                                                                adatarb.ReadBytes(count);
                                                                count = adatarb.ReadInt32();
                                                                adatarb.ReadBytes(count);
                                                                if (adatarb.ReadInt32() == 1)
                                                                {
                                                                    flagdata = true;
                                                                }
                                                            }
                                                            if (flagdata == true)
                                                            {
                                                                int num = adatarb.ReadInt32();
                                                                for (int g = 0; g < num; g++)
                                                                {
                                                                    int count = adatarb.ReadInt32();
                                                                    Debug.WriteLine(Encoding.ASCII.GetString(adatarb.ReadBytes(count)));
                                                                    byte ckck = adatarb.ReadByte();
                                                                    if (ckck == 1 && g == (num - 1))
                                                                    {
                                                                        adatarb.ReadBytes(5);
                                                                    }
                                                                    else if (ckck == 1)
                                                                    {
                                                                        adatarb.ReadBytes(9);
                                                                    }
                                                                    else if(ckck == 0 && g == num -1)
                                                                    {
                                                                        goto END;
                                                                    }
                                                                }
                                                            }
                                                            
                                                        }
                                                        adatarb.ReadBytes(4);
                                                        END:;
                                                    }
                                                }
                                                /*else if(hlocal == 6076285342561748301)
                                                {
                                                    flagsmenus = true;
                                                }*/
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Break!");
                                        }

                                    }

                                }
                                if (flags)
                                {
                                    using (StreamWriter wttxt = new StreamWriter(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".txt"))
                                    {
                                        foreach (String s in lstxt)
                                            wttxt.WriteLine(s);
                                    }
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    filesize.WriteLine(num_object.ToString("x8").ToUpper() + " " + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                    Console.WriteLine("Done!");
                                }
                                else
                                {
                                    Console.WriteLine("Break!");
                                }
                                /*if (flagsmenus)
                                {
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    Console.WriteLine("Done!");
                                }*/

                            }
                            else if (version == 18)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2);
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\exp_data\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\exp_data\\" + ebf + "\\");
                                }
                                Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                var wt2 = new BinaryWriter(new FileStream(path + "\\exp_data\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                wt2.Write(allone);
                                wt2.Flush();
                                wt2.Close();
                                Console.WriteLine(" Done!");
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Null Lenght Byte");
                }
            }
            filesize.Close();
        }

        private void allSegsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StreamWriter filesize = new StreamWriter(path + "\\" + filename + ".FileSizeTable");
            filesize.WriteLine("version " + version);
            for (int j = 0; j < dic.Count; j++)
            {
                byte[] section = dic[j];
                var rbex = new BinaryReader(new MemoryStream(section));
                var num_object = rbex.ReadUInt32();
                int eoffs = Big(rbex.ReadBytes(4));
                int elen = Big(rbex.ReadBytes(4));
                rbex.ReadBytes(4);
                int elemente = Big(rbex.ReadBytes(4));
                string ebf = string.Empty;
                if (elemente == 0)
                    ebf = ".dat";
                else
                    ebf = ".d" + elemente.ToString("d2");
                try
                {
                    var erbf = new BinaryReader(new FileStream(path + "\\" + filename + ebf, FileMode.Open, FileAccess.Read, FileShare.Read));
                    erbf.BaseStream.Seek(eoffs, SeekOrigin.Begin);
                    byte[] getdata = erbf.ReadBytes(elen);
                    using (var data_ar = new BinaryReader(new MemoryStream(getdata)))
                    {
                        if (data_ar.ReadInt32() == 1936156019)
                        {
                            if (version == 13)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = Big16(data_ar.ReadBytes(2));
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = (UInt16)Big16(data_ar.ReadBytes(2));
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = Big16(tmp);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = Big(data_ar.ReadBytes(4));
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    byte[] blk = data_ar.ReadBytes(bit1[i]);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var bgw = new BinaryReader(new MemoryStream(allone));
                                List<string> lstxt = new List<string>();
                                bool flags = false;
                                if (bgw.ReadInt64() == 2328156834426209092)
                                {
                                    bgw.ReadInt32();
                                    int tobe = Big(bgw.ReadBytes(4)) - 4;
                                    int wnum = Big(bgw.ReadBytes(4));
                                    bgw.ReadBytes(tobe);
                                    bgw.ReadBytes(8);
                                    bgw.ReadInt32();
                                    bgw.ReadInt32(); //length
                                    for (int i = 0; i < wnum; i++)
                                    {
                                        try
                                        {
                                            int nextleg = Big(bgw.ReadBytes(4));
                                            bgw.ReadBytes(6);
                                            byte[] ardata = bgw.ReadBytes(nextleg);
                                            var adatarb = new BinaryReader(new MemoryStream(ardata));
                                            adatarb.ReadInt32();
                                            if (adatarb.ReadInt64() == 6074880098149683011)
                                            {
                                                adatarb.ReadInt32();
                                                int lnum = Big(adatarb.ReadBytes(4));
                                                adatarb.ReadBytes(lnum * 17);
                                                adatarb.ReadBytes(12);
                                                if (adatarb.ReadInt64() == 6870884773368385356)
                                                {
                                                    flags = true;
                                                    adatarb.ReadInt64();
                                                    int cnum = adatarb.ReadByte();
                                                    for (int c = 0; c < cnum; c++)
                                                    {
                                                        adatarb.ReadBytes(4);
                                                        if (adatarb.ReadInt32() == 1196311811)
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            List<int> por = new List<int>();
                                                            for (int g = 0; g < knum; g++)
                                                            {
                                                                adatarb.ReadInt32();//null
                                                                adatarb.ReadInt32();//position
                                                                int addpor = Big(adatarb.ReadBytes(4)) * 2;
                                                                if (addpor != 0)
                                                                    por.Add(addpor);
                                                                adatarb.ReadBytes(16);
                                                            }
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum == 0)
                                                            {
                                                                lstxt.Add("[0]");
                                                            }
                                                            else
                                                            {
                                                                MemoryStream intextms = new MemoryStream(adatarb.ReadBytes(mnum));
                                                                BinaryReader rdmsintext = new BinaryReader(intextms);
                                                                string txtout = "[" + por.Count + "]\n";
                                                                for (int g = 0; g < por.Count; g++)
                                                                {
                                                                    if (g == por.Count - 1)
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString();
                                                                    }
                                                                    else
                                                                    {
                                                                        string converted = Encoding.BigEndianUnicode.GetString(rdmsintext.ReadBytes(por[g]));
                                                                        StringBuilder builder = new StringBuilder(converted);
                                                                        builder.Replace("\r\n", "[rn]");
                                                                        builder.Replace("\n\r", "[nr]");
                                                                        builder.Replace("\r", "[r]");
                                                                        builder.Replace("\n", "[n]");
                                                                        txtout += builder.ToString() + "\n";
                                                                    }
                                                                }
                                                                lstxt.Add(txtout);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int knum = Big(adatarb.ReadBytes(4));
                                                            adatarb.ReadBytes(knum * 28);
                                                            int mnum = Big(adatarb.ReadBytes(4));
                                                            if (mnum != 0)
                                                                adatarb.ReadBytes(mnum);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Break!");
                                        }

                                    }

                                }
                                if (flags)
                                {
                                    using (StreamWriter wttxt = new StreamWriter(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".txt"))
                                    {
                                        foreach (String s in lstxt)
                                            wttxt.WriteLine(s);
                                    }
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    filesize.WriteLine(num_object.ToString("x8").ToUpper() + "=" + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs");
                                    Console.WriteLine("Done!");
                                }
                                else
                                {
                                    Console.WriteLine("Break!");
                                }

                            }
                            else if (version == 17)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2);
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\" + filename + "_exp\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\" + filename + "_exp\\" + ebf + "\\");
                                }
                                var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                wt2.Write(allone);
                                wt2.Flush();
                                wt2.Close();
                                /*if (flagsmenus)
                                {
                                    Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                    var wt2 = new BinaryWriter(new FileStream(path + "\\" + filename + "_exp\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                    wt2.Write(allone);
                                    wt2.Flush();
                                    wt2.Close();
                                    Console.WriteLine("Done!");
                                }*/

                            }
                            else if (version == 18)
                            {
                                lengsegs = 0;
                                dicblock.Clear();
                                data_ar.ReadInt16();
                                int numf = data_ar.ReadInt16();
                                data_ar.ReadBytes(8);
                                int[] bit1 = new int[numf];
                                int[] bit2 = new int[numf];
                                int[] bit3 = new int[numf];
                                for (int i = 0; i < numf; i++)
                                {
                                    bit1[i] = data_ar.ReadUInt16();
                                    byte[] tmp = data_ar.ReadBytes(2);
                                    bit2[i] = BitConverter.ToUInt16(tmp, 0);
                                    if (tmp[0] == 0 && tmp[1] == 0)
                                    {
                                        bit2[i] = 65536;
                                    }
                                    bit3[i] = data_ar.ReadInt32();
                                }
                                if (numf % 2 != 0)
                                {
                                    data_ar.BaseStream.Seek((numf + 1) * 8 + 16, SeekOrigin.Begin);
                                }
                                else
                                {
                                    data_ar.BaseStream.Seek(numf * 8 + 16, SeekOrigin.Begin);
                                }
                                int pos = (int)data_ar.BaseStream.Position;
                                for (int i = 0; i < numf; i++)
                                {
                                    data_ar.BaseStream.Seek(pos + bit3[i] - 1, SeekOrigin.Begin);
                                    data_ar.ReadBytes(2);
                                    byte[] blk = data_ar.ReadBytes(bit1[i] - 2);
                                    lengsegs += Decompress(blk).Length;
                                    dicblock.Add(i, Decompress(blk));
                                }
                                byte[] allone = new byte[lengsegs];
                                int pivot = 0;
                                for (int i = 0; i < dicblock.Count; i++)
                                {
                                    Buffer.BlockCopy(dicblock[i], 0, allone, pivot, dicblock[i].Length);
                                    pivot += dicblock[i].Length;
                                }
                                if (!Directory.Exists(path + "\\exp_data\\" + ebf + "\\"))
                                {
                                    Directory.CreateDirectory(path + "\\exp_data\\" + ebf + "\\");
                                }
                                Console.WriteLine(eoffs.ToString("x8").ToUpper() + ".dc_segs " + allone.Length);
                                var wt2 = new BinaryWriter(new FileStream(path + "\\exp_data\\" + ebf + "\\0x" + eoffs.ToString("x8").ToUpper() + ".dc_segs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                                wt2.Write(allone);
                                wt2.Flush();
                                wt2.Close();
                                Console.WriteLine(" Done!");
                            }
                        }
                    }
                }
                catch
                {
                    Debug.WriteLine(eoffs + " " + elen);
                    Console.WriteLine("Null Lenght Byte");
                }
            }
            filesize.Close();
        }

        private void tableSizeQZIPTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile.Filter = "File Size Table| *.FileSizeTable";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(openFile.FileName))
                {
                    Console.WriteLine("Open file {0}", Path.GetFileName(openFile.FileName));
                    Dictionary<string, string> keyfile = new Dictionary<string, string>();
                    path = Path.GetDirectoryName(openFile.FileName);
                    filename = Path.GetFileNameWithoutExtension(openFile.FileName);
                    string[] sizetable = File.ReadAllLines(openFile.FileName);
                    string bigfnew = string.Empty;
                    int filechk = 0;
                    for (int i = 0; i < 50; i++)
                    {
                        if (i != 0)
                        {
                            if (!File.Exists(path + "\\" + filename + ".d" + i.ToString("d2")))
                            {
                                bigfnew = path + "\\" + filename + ".d" + i.ToString("d2");
                                File.Create(path + "\\" + filename + ".d" + i.ToString("d2") + "new");
                                filechk = i;
                                break;
                            }
                            else if (File.Exists(path + "\\" + filename + ".d" + i.ToString("d2") + "new"))
                            {
                                bigfnew = path + "\\" + filename + ".d" + i.ToString("d2");
                                filechk = i;
                                break;
                            }
                        }
                    }
                    BinaryWriter bigfile = new BinaryWriter(new FileStream(bigfnew, FileMode.Create, FileAccess.Write, FileShare.Read));
                    bigfile.Write(Encoding.ASCII.GetBytes("QUANTICDREAMTABINDEX"));
                    bigfile.Write(285212672);
                    for (int i = 0; i < 2048; i++)
                    {
                        if (bigfile.BaseStream.Position == 2048)
                        {
                            break;
                        }
                        else
                        {
                            bigfile.Write((byte)0x2D);
                        }
                    }
                    foreach (string addrkey in sizetable)
                    {
                        var data = addrkey.Split(' ');
                        if (data[0] == "version")
                        {
                            version = int.Parse(data[1]);
                            Console.WriteLine("Version {0}", version);
                        }
                        else
                        {
                            keyfile.Add(data[0], data[1]);
                        }
                    }
                    if (version == 13)
                    {

                    }
                    else if (version == 17)
                    {
                        foreach (var pair in keyfile)
                        {
                            if (!File.Exists(path + pair.Value + "_bk"))
                            {
                                File.Copy(path + pair.Value, path + pair.Value + "_bk");
                                Console.WriteLine("Backup file done!");
                            }
                            if (File.Exists(Path.GetDirectoryName(path + pair.Value) + "\\" + Path.GetFileNameWithoutExtension(path + pair.Value) + ".txt"))
                            {
                                Console.WriteLine("Load file {0}", Path.GetFileNameWithoutExtension(path + pair.Value) + ".txt");
                                using (var wt = new BinaryWriter(new FileStream(path + pair.Value, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                                {
                                    using (var rd = new BinaryReader(new FileStream(path + pair.Value + "_bk", FileMode.Open, FileAccess.Read, FileShare.Read)))
                                    {
                                        Dictionary<string, string> textline = new Dictionary<string, string>();
                                        string[] text = File.ReadAllLines(Path.GetDirectoryName(path + pair.Value) + "\\" + Path.GetFileNameWithoutExtension(path + pair.Value) + ".txt");
                                        foreach (string line in text)
                                        {
                                            var dataline = line.Split('=');
                                            textline.Add(dataline[0], dataline[1]);
                                        }
                                        wt.Write(rd.ReadBytes(17));
                                        int tobe = rd.ReadInt32();
                                        int wnum = rd.ReadInt32();
                                        wt.Write(tobe);
                                        wt.Write(wnum);
                                        wt.Write(rd.ReadBytes(tobe + 8));
                                        //long porlen = wt.BaseStream.Position; //lengpor
                                        int lenb = rd.ReadInt32();
                                        byte[] datasheet = rd.ReadBytes(lenb);
                                        BinaryReader msrd = new BinaryReader(new MemoryStream(datasheet));
                                        BinaryWriter tmp = new BinaryWriter(new FileStream("dump.tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
                                        for (int i = 0; i < wnum; i++)
                                        {
                                            var dumpfs = new BinaryWriter(new FileStream(i + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
                                            int knum = msrd.ReadInt32();
                                            byte[] nullh = msrd.ReadBytes(10);
                                            byte ckscks = msrd.ReadByte();
                                            if (ckscks != 0)
                                                msrd.BaseStream.Seek(msrd.BaseStream.Position - 1, SeekOrigin.Begin);
                                            byte[] aldata = msrd.ReadBytes(knum);
                                            var msrdm = new BinaryReader(new MemoryStream(aldata));
                                            dumpfs.Write(msrdm.ReadInt32());
                                            Int64 hadatarb = msrdm.ReadInt64();
                                            dumpfs.Write(hadatarb);
                                            try
                                            {
                                                if (hadatarb == 6074880098149683011)
                                                {
                                                    dumpfs.Write(msrdm.ReadInt32());
                                                    int lnum = msrdm.ReadInt32();
                                                    dumpfs.Write(lnum);
                                                    dumpfs.Write(msrdm.ReadBytes(lnum * 9));
                                                    dumpfs.Write(msrdm.ReadBytes(12));
                                                    Int64 hlocal = msrdm.ReadInt64();
                                                    dumpfs.Write(hlocal);
                                                    if (hlocal == 6870884773368385356)
                                                    {
                                                        dumpfs.Write(msrdm.ReadBytes(5));
                                                        int cnum = msrdm.ReadInt32();
                                                        dumpfs.Write(cnum);
                                                        for (int c = 0; c < cnum; c++)
                                                        {
                                                            dumpfs.Write(msrdm.ReadBytes(4));
                                                            int heng = msrdm.ReadInt32();
                                                            dumpfs.Write(heng);
                                                            if (heng == 1196311808)
                                                            {
                                                                int count = msrdm.ReadInt32();
                                                                dumpfs.Write(count);
                                                                bool flagdata = false;
                                                                for (int j = 0; j < count; j++)
                                                                {
                                                                    int lenid = msrdm.ReadInt32();
                                                                    dumpfs.Write(lenid);
                                                                    byte[] idsubb = msrdm.ReadBytes(lenid);
                                                                    string idsub = Encoding.ASCII.GetString(idsubb);
                                                                    dumpfs.Write(idsubb);
                                                                    lenid = msrdm.ReadInt32();
                                                                    msrdm.ReadBytes(lenid);
                                                                    string sub = textline[idsub];
                                                                    StringBuilder builder = new StringBuilder(sub);
                                                                    builder.Replace("[rn]", "\r\n");
                                                                    builder.Replace("[nr]", "\n\r");
                                                                    builder.Replace("[r]", "\r");
                                                                    builder.Replace("[n]", "\n");
                                                                    builder.Replace("[p]", "=");
                                                                    builder.Replace("Ư", "Ū");
                                                                    builder.Replace("ư", "ū");
                                                                    builder.Replace("Ơ", "Ō");
                                                                    builder.Replace("ơ", "ō");
                                                                    builder.Replace("[q]", "Ư");
                                                                    builder.Replace("[w]", "ư");
                                                                    builder.Replace("[e]", "Ơ");
                                                                    builder.Replace("[t]", "ơ");
                                                                    if (sub != "0")
                                                                    {
                                                                        byte[] chargesub = Encoding.Unicode.GetBytes(builder.ToString());
                                                                        dumpfs.Write(chargesub.Length);
                                                                        dumpfs.Write(chargesub);
                                                                    }
                                                                    else
                                                                    {
                                                                        dumpfs.Write(0);
                                                                        //msrdm.ReadInt32();
                                                                    }
                                                                    int ckmk = msrdm.ReadInt32();
                                                                    dumpfs.Write(ckmk);
                                                                    if (ckmk == 1)
                                                                        flagdata = true;
                                                                }
                                                                if (flagdata == true)
                                                                {
                                                                    int pnum = msrdm.ReadInt32();
                                                                    dumpfs.Write(pnum);
                                                                    for (int g = 0; g < pnum; g++)
                                                                    {
                                                                        int nm = msrdm.ReadInt32();
                                                                        dumpfs.Write(nm);
                                                                        dumpfs.Write(msrdm.ReadBytes(nm));
                                                                        byte cck = msrdm.ReadByte();
                                                                        dumpfs.Write(cck);
                                                                        if (cck == 1 && g == pnum - 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(5));
                                                                        }
                                                                        else if (cck == 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(9));
                                                                        }
                                                                        else
                                                                        {
                                                                            goto ENDCK;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                int count = msrdm.ReadInt32();
                                                                dumpfs.Write(count);
                                                                bool flagdata = false;
                                                                for (int j = 0; j < count; j++)
                                                                {
                                                                    int lenid = msrdm.ReadInt32();
                                                                    dumpfs.Write(lenid);
                                                                    dumpfs.Write(msrdm.ReadBytes(lenid));
                                                                    lenid = msrdm.ReadInt32();
                                                                    if (lenid != 0)
                                                                    {
                                                                        dumpfs.Write(lenid);
                                                                        dumpfs.Write(msrdm.ReadBytes(lenid));
                                                                    }
                                                                    else
                                                                    {
                                                                        dumpfs.Write(lenid);
                                                                    }
                                                                    int ckmk = msrdm.ReadInt32();
                                                                    dumpfs.Write(ckmk);
                                                                    if (ckmk == 1)
                                                                        flagdata = true;
                                                                }
                                                                if (flagdata == true)
                                                                {
                                                                    int pnum = msrdm.ReadInt32();
                                                                    dumpfs.Write(pnum);
                                                                    for (int g = 0; g < pnum; g++)
                                                                    {
                                                                        int nm = msrdm.ReadInt32();
                                                                        dumpfs.Write(nm);
                                                                        dumpfs.Write(msrdm.ReadBytes(nm));
                                                                        byte cck = msrdm.ReadByte();
                                                                        dumpfs.Write(cck);
                                                                        if (cck == 1 && g == pnum - 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(5));
                                                                        }
                                                                        else if (cck == 1)
                                                                        {
                                                                            dumpfs.Write(msrdm.ReadBytes(9));
                                                                        }
                                                                        else if (cck == 0 && g == pnum - 1)
                                                                        {
                                                                            goto ENDCK;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            dumpfs.Write(msrdm.ReadBytes(4));
                                                            Debug.WriteLine(msrdm.BaseStream.Position);
                                                        ENDCK:;
                                                        }
                                                        dumpfs.Close();
                                                        byte[] dumpall = File.ReadAllBytes(i + ".tmp");
                                                        tmp.Write(dumpall.Length);
                                                        tmp.Write(nullh);
                                                        if (ckscks != 0)
                                                            tmp.Write(dumpall);
                                                        else
                                                        {
                                                            tmp.Write(ckscks);
                                                            tmp.Write(dumpall);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        tmp.Write(knum);
                                                        tmp.Write(nullh);
                                                        if (ckscks != 0)
                                                            tmp.Write(aldata);
                                                        else
                                                        {
                                                            tmp.Write(ckscks);
                                                            tmp.Write(aldata);
                                                        }
                                                        dumpfs.Close();
                                                        File.Delete(i + ".tmp");
                                                    }
                                                }
                                                else
                                                {
                                                    tmp.Write(knum);
                                                    tmp.Write(nullh);
                                                    if (ckscks != 0)
                                                        tmp.Write(aldata);
                                                    else
                                                    {
                                                        tmp.Write(ckscks);
                                                        tmp.Write(aldata);
                                                    }
                                                    dumpfs.Close();
                                                    File.Delete(i + ".tmp");
                                                }
                                                dumpfs.Close();
                                                File.Delete(i + ".tmp");
                                            }
                                            catch
                                            {
                                                tmp.Write(knum);
                                                tmp.Write(nullh);
                                                if (ckscks != 0)
                                                    tmp.Write(aldata);
                                                else
                                                {
                                                    tmp.Write(ckscks);
                                                    tmp.Write(aldata);
                                                }
                                                dumpfs.Close();
                                                File.Delete(i + ".tmp");
                                            }

                                        }
                                        tmp.Close();
                                        byte[] ftmp = File.ReadAllBytes("dump.tmp");
                                        wt.Write(ftmp.Length);
                                        wt.Write(ftmp);
                                        wt.Flush();
                                        wt.Close();
                                        File.Delete("dump.tmp");
                                    }
                                }
                                Console.WriteLine("Import file {0} done!", Path.GetFileName(path + pair.Value));

                            }

                            for (int i = 0; i < 64; i++)
                            {
                                if (bigfile.BaseStream.Position % 64 != 0)
                                {
                                    bigfile.Write((byte)0x2D);
                                }
                                else
                                {
                                    byte[] dcsegs = File.ReadAllBytes(path + pair.Value);
                                    if (!File.Exists(path + "\\" + filename + ".idm_bk"))
                                    {
                                        File.Copy(path + "\\" + filename + ".idm", path + "\\" + filename + ".idm_bk");
                                    }
                                    using (var idmapwt = new BinaryWriter(new FileStream(path + "\\" + filename + ".idm", FileMode.Open, FileAccess.Write, FileShare.ReadWrite)))
                                    {
                                        byte[] raidm = File.ReadAllBytes(path + "\\" + filename + ".idm_bk");
                                        using (var idmaprd = new BinaryReader(new MemoryStream(raidm)))
                                        {
                                            while (idmaprd.BaseStream.Position != idmaprd.BaseStream.Length)
                                            {
                                                int ckpair = Big(idmaprd.ReadBytes(4));
                                                if (ckpair == Int32.Parse(pair.Key, System.Globalization.NumberStyles.HexNumber))
                                                {
                                                    long porsave = idmaprd.BaseStream.Position;
                                                    if (idmaprd.ReadInt32() == (int)new Int32Converter().ConvertFromString(Path.GetFileNameWithoutExtension(pair.Value)))
                                                    {
                                                        idmapwt.BaseStream.Seek(porsave, SeekOrigin.Begin);
                                                        idmapwt.Write((int)bigfile.BaseStream.Position);
                                                        idmapwt.Write(dcsegs.Length);
                                                        idmapwt.BaseStream.Seek(idmapwt.BaseStream.Position + 4, SeekOrigin.Begin);
                                                        idmapwt.Write(filechk);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    bigfile.Write(dcsegs);
                                    break;
                                }
                            }
                        }
                    }


                    bigfile.Flush();
                    bigfile.Close();
                }
            }
        }
    }
}
