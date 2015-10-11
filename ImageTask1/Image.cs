﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageTask1
{
    class Image
    {

        private byte[] m_buffer;
        private uint m_width, m_height;

        private bool m_needFlush;

        private uint m_components;

        private Bitmap m_bitmap;

        public uint Width
        {
            get { return m_width; }
        }

        public uint Height
        {
            get{return m_height;}
        }

        public byte[] Buffer
        {
            get { return m_buffer; }
        }

        public byte this[uint key]
        {
            get { return m_buffer[key]; }
            set { 
                m_buffer[key] = value;
                m_needFlush = true;
            }
        }

        public uint Components
        {
            get { return m_components; }
        }

        public Bitmap bitmap
        {
            get
            {
                if (m_needFlush)
                    flush();
                return m_bitmap;
            }
        }

        public Image(byte[] bytes, uint width, uint height, uint components)
        {
            m_buffer = bytes;
            m_width = width;
            m_height = height;
            //create bitmap
            m_bitmap = new Bitmap((int)m_width, (int)m_height);
            m_needFlush = true;
            m_components = components;
        }

        public Image(uint width, uint height, uint components)
        {
            m_bitmap = new Bitmap((int)width,(int)height,PixelFormat.Format24bppRgb);
            m_width = width;
            m_height = height;
            m_buffer = new byte[width*height*components];
            m_needFlush = true;
            m_components = components;
        }

        public Image(Bitmap bmp)
        {
            //load code
            m_width = (uint)bmp.Width;
            m_height = (uint)bmp.Height;
            m_needFlush = false;
            m_bitmap = bmp;

            //load bytes
            unsafe
            {
                BitmapData data = m_bitmap.LockBits(new Rectangle(0, 0, (int)m_width, (int)m_height), ImageLockMode.ReadWrite, bmp.PixelFormat);

                if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    m_components = 3;
                }
                else if (bmp.PixelFormat == PixelFormat.Format32bppArgb || bmp.PixelFormat == PixelFormat.Format32bppRgb || bmp.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    m_components = 4;
                }

                m_buffer = new byte[m_width*m_height*m_components];

                Marshal.Copy(data.Scan0,m_buffer,0,m_buffer.Length);
                m_bitmap.UnlockBits(data);
            }
        }

        public Pixel getPixel(uint x, uint y)
        {
            Pixel result = new Pixel();

            uint index = (y*m_width) + x;

            index *= m_components;


            result.R = m_buffer[index];
            result.G = m_buffer[index+1];
            result.B = m_buffer[index+2];

            if(m_components == 4)
                result.A = m_buffer[index+3];
            else
                result.A = 255;

            return result;
        }

        public void setPixel(uint x, uint y,Pixel p)
        {

            uint index = (y * m_width) + x;

            index *= m_components;

            m_buffer[index] = p.R;
            m_buffer[index+1] = p.G;
            m_buffer[index+2] = p.B;

            if (m_components == 4)
                m_buffer[index + 3] = p.A;

            m_needFlush = true;
        }

        public void flush()
        {
            //flush code
            unsafe
            {
                if (m_width*m_components%4 == 0)
                {
                    BitmapData data = m_bitmap.LockBits(new Rectangle(0, 0, (int) m_width, (int) m_height),
                        ImageLockMode.WriteOnly, m_bitmap.PixelFormat);
                    Marshal.Copy(m_buffer, 0, data.Scan0, m_buffer.Length);
                    m_bitmap.UnlockBits(data);
                }
                else
                {
                    Color c = new Color();
                    int r, g, b, a;
                    int index;
                    for (int x = 0; x < m_width; x++)
                    {
                        for (int y = 0; y < m_height; y++)
                        {
                            index = x + (int)(y*m_width);
                            index *= (int)m_components;
                            b = m_buffer[index];
                            g = m_buffer[index+1];
                            r = m_buffer[index + 2];
                            c = Color.FromArgb(r, g, b);

                            m_bitmap.SetPixel(x,y,c);
                        }
                    }
                }
            }
            m_needFlush = false;
        }








        public Point ShearXY(Point source, double shearX, double shearY)
        {
            Point result = new Point();

            result.X = (int)(Math.Round(source.X + shearX * source.Y));
            result.X -= (int)Math.Round(m_width * shearX / 2.0); ;

            result.Y = (int)(Math.Round(source.Y + shearY * source.X));
            result.Y -= (int)Math.Round(m_height * shearY / 2.0);

            return result;
        }

        public Bitmap ShearImage(double shearX, double shearY)
        {


            BitmapData sourceData = m_bitmap.LockBits(new Rectangle(0, 0, (int)m_width, (int)m_height),
                                          ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];

            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

            m_bitmap.UnlockBits(sourceData);



            int sourceXY = 0;
            int resultXY = 0;

            Point sourcePoint = new Point();
            Point resultPoint = new Point();


            Rectangle imageBounds = new Rectangle(0, 0, (int)m_width, (int)m_height);

            for (int row = 0; row < (int)m_height; row++)
            {
                for (int col = 0; col < (int)m_width; col++)
                {

                    /*
                        int bitsPerPixel = ((int)format & 0xff00) >> 8;
                        int bytesPerPixel = (bitsPerPixel + 7) / 8;
                        int stride = 4 * ((width * bytesPerPixel + 3) / 4);
                     */


                    sourceXY = row * sourceData.Stride + col * 4;

                    sourcePoint.X = col;
                    sourcePoint.Y = row;

                    if (sourceXY >= 0 && sourceXY + 3 < pixelBuffer.Length)
                    {


                        resultPoint = ShearXY(sourcePoint, shearX, shearY);


                        resultXY = resultPoint.Y * sourceData.Stride + resultPoint.X * 4;



                        if (imageBounds.Contains(resultPoint) && resultXY >= 0)
                        {



                            if (resultXY + 6 <= resultBuffer.Length)
                            {
                                resultBuffer[resultXY + 4] =
                                     pixelBuffer[sourceXY];

                                resultBuffer[resultXY + 5] =
                                     pixelBuffer[sourceXY + 1];

                                resultBuffer[resultXY + 6] =
                                     pixelBuffer[sourceXY + 2];

                                resultBuffer[resultXY + 7] = 255;
                            }



                            if (resultXY - 3 >= 0)
                            {
                                resultBuffer[resultXY - 4] =
                                     pixelBuffer[sourceXY];

                                resultBuffer[resultXY - 3] =
                                     pixelBuffer[sourceXY + 1];

                                resultBuffer[resultXY - 2] =
                                     pixelBuffer[sourceXY + 2];

                                resultBuffer[resultXY - 1] = 255;
                            }



                            if (resultXY + 3 < resultBuffer.Length)
                            {
                                resultBuffer[resultXY] =
                                 pixelBuffer[sourceXY];

                                resultBuffer[resultXY + 1] =
                                 pixelBuffer[sourceXY + 1];

                                resultBuffer[resultXY + 2] =
                                 pixelBuffer[sourceXY + 2];

                                resultBuffer[resultXY + 3] = 255;
                            }

                        }

                    }
                }
            }



            BitmapData resultData =
                     m_bitmap.LockBits(new Rectangle(0, 0, m_bitmap.Width, m_bitmap.Height),
                      ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);

            m_bitmap.UnlockBits(resultData);


            return null;
        }






        public Point RotateXY(Point source, double degrees)
        {
            Point result = new Point();

            result.X = (int)(Math.Round((source.X - (int)(m_width / 2.0)) *
                       Math.Cos(degrees) - (source.Y - (int)(m_height / 2.0)) *
                       Math.Sin(degrees))) + (int)(m_width / 2.0);

            result.Y = (int)(Math.Round((source.X - (int)(m_width / 2.0)) *
                       Math.Sin(degrees) + (source.Y - (int)(m_width / 2.0)) *
                       Math.Cos(degrees))) + (int)(m_height / 2.0);

            return result;
        }

        public Bitmap RotateImage(double degree)
        {
            BitmapData sourceData =
                       m_bitmap.LockBits(new Rectangle(0, 0, (int)m_width, (int)m_height),
                       ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];

            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

            m_bitmap.UnlockBits(sourceData);


            //Convert to Radians
            degree = degree * Math.PI / 180.0;

            int sourceXY = 0;
            int resultXY = 0;

            Point sourcePoint = new Point();
            Point resultPoint = new Point();

            Rectangle imageBounds = new Rectangle(0, 0, (int)m_width, (int)m_height);

            for (int row = 0; row < m_height; row++)
            {
                for (int col = 0; col < m_width; col++)
                {
                    sourceXY = row * sourceData.Stride + col * 4;

                    sourcePoint.X = col;
                    sourcePoint.Y = row;

                    if (sourceXY >= 0 && sourceXY + 3 < pixelBuffer.Length)
                    {
                        //Calculate Blue Rotation

                        resultPoint = RotateXY(sourcePoint, degree);

                        resultXY = (int)(Math.Round((resultPoint.Y * sourceData.Stride) + (resultPoint.X * 4.0)));

                        if (imageBounds.Contains(resultPoint) && resultXY >= 0)
                        {
                            if (resultXY + 6 < resultBuffer.Length)
                            {
                                resultBuffer[resultXY + 4] = pixelBuffer[sourceXY];

                                resultBuffer[resultXY + 7] = 255;
                            }

                            if (resultXY + 3 < resultBuffer.Length)
                            {
                                resultBuffer[resultXY] = pixelBuffer[sourceXY];

                                resultBuffer[resultXY + 3] = 255;
                            }
                        }

                        //Calculate Green Rotation

                        resultPoint = RotateXY(sourcePoint, degree);

                        resultXY = (int)(Math.Round((resultPoint.Y * sourceData.Stride) + (resultPoint.X * 4.0)));

                        if (imageBounds.Contains(resultPoint) && resultXY >= 0)
                        {
                            if (resultXY + 6 < resultBuffer.Length)
                            {
                                resultBuffer[resultXY + 5] =
                                 pixelBuffer[sourceXY + 1];

                                resultBuffer[resultXY + 7] = 255;
                            }

                            if (resultXY + 3 < resultBuffer.Length)
                            {
                                resultBuffer[resultXY + 1] =
                                 pixelBuffer[sourceXY + 1];

                                resultBuffer[resultXY + 3] = 255;
                            }
                        }

                        //Calculate Red Rotation

                        resultPoint = RotateXY(sourcePoint, degree);

                        resultXY = (int)(Math.Round((resultPoint.Y * sourceData.Stride) + (resultPoint.X * 4.0)));

                        if (imageBounds.Contains(resultPoint) && resultXY >= 0)
                        {
                            if (resultXY + 6 < resultBuffer.Length)
                            {
                                resultBuffer[resultXY + 6] =
                                 pixelBuffer[sourceXY + 2];

                                resultBuffer[resultXY + 7] = 255;
                            }

                            if (resultXY + 3 < resultBuffer.Length)
                            {
                                resultBuffer[resultXY + 2] =
                                 pixelBuffer[sourceXY + 2];

                                resultBuffer[resultXY + 3] = 255;
                            }
                        }
                    }
                }
            }


            BitmapData resultData = m_bitmap.LockBits(new Rectangle(0, 0, (int)m_width, (int)m_height),
                       ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);

            m_bitmap.UnlockBits(resultData);

            return null;
        }







    }
}

