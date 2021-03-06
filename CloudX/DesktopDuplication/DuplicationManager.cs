﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Rectangle = SharpDX.Rectangle;
using Resource = SharpDX.DXGI.Resource;
using ResultCode = SharpDX.DXGI.ResultCode;

namespace CloudX.DesktopDuplication
{
    public class DuplicationManager
    {
        /// <summary>
        ///     If the caller specifies a zero time-out interval in the TimeoutInMilliseconds parameter, AcquireNextFrame verifies
        ///     whether there is a new desktop image available, returns immediately, and indicates its outcome with the return
        ///     value. If the caller specifies an INFINITE time-out interval in the TimeoutInMilliseconds parameter, the time-out
        ///     interval never elapses
        /// </summary>
        private const int TIME_OUT = 130;

        private static int counter;
        private static readonly DuplicationManager instance = new DuplicationManager();

        private Device device;

        private OutputDuplication duplicatedOutput;
        private int Height;
        private OutputDescription outputDescription;
        private Texture2D screenTexture;
        private Texture2DDescription textureDesc;
        private int Width;
        //private Texture2D acquiredDesktopImage = null;

        private DuplicationManager()
        {
            Init();
        }

        public static DuplicationManager GetInstance()
        {
            return instance;
        }


        private void Init()
        {
            // # of graphics card adapter
            const int numAdapter = 0;

            // # of output device (i.e. monitor)
            const int numOutput = 0;

            // Create DXGI Factory1
            var factory = new Factory1();
            Adapter1 adapter = factory.GetAdapter1(numAdapter);

            // Create device from Adapter
            device = new Device(adapter);

            // Get DXGI.Output
            Output output = adapter.GetOutput(numOutput);
            var output1 = output.QueryInterface<Output1>();

            // Width/Height of desktop to capture
            Width = output.Description.DesktopBounds.Width;
            Height = output.Description.DesktopBounds.Height;

            // Create Staging screenTexture CPU-accessible
            textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = Width,
                Height = Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = {Count = 1, Quality = 0},
                Usage = ResourceUsage.Staging
            };

            outputDescription = output.Description;

            // Demo the output
            duplicatedOutput = output1.DuplicateOutput(device);

            screenTexture = new Texture2D(device, textureDesc);

            FrameData.Device = device;
        }

        private void GetDirtyAndMoveRects(ref FrameData data, Resource screenResource,
            OutputDuplicateFrameInformation duplicateFrameInformation)
        {
            //copy resource into memory that can be accessed by the CPU
            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
            screenResource.Dispose();

            int bufSize = duplicateFrameInformation.TotalMetadataBufferSize;

            if (bufSize <= 0) return;

            int moveCount = (int)
                Math.Ceiling((double) bufSize/
                             Marshal.SizeOf(typeof (OutputDuplicateMoveRectangle)));
            var moveRectangles =
                new OutputDuplicateMoveRectangle
                    [
                    moveCount
                    ];

            Console.WriteLine("Move : {0}  {1}  {2}  {3}", moveRectangles.Length, bufSize,
                Marshal.SizeOf(typeof (OutputDuplicateMoveRectangle)),
                bufSize/Marshal.SizeOf(typeof (OutputDuplicateMoveRectangle)));

            //get move rects
            if (moveRectangles.Length > 0)
                duplicatedOutput.GetFrameMoveRects(bufSize, moveRectangles, out bufSize);
            
            data.MoveRectangles = moveRectangles;
            data.MoveCount = moveCount;

            bufSize = duplicateFrameInformation.TotalMetadataBufferSize - bufSize;
            int dirtyCount = bufSize/Marshal.SizeOf(typeof (System.Drawing.Rectangle));
            var dirtyRectangles = new Rectangle[dirtyCount];
            Console.WriteLine("Dirty : {0}  {1}  {2}  {3}", dirtyRectangles.Length, bufSize,
                Marshal.SizeOf(typeof (System.Drawing.Rectangle)),
                bufSize/Marshal.SizeOf(typeof (System.Drawing.Rectangle)));
            //get dirty rects
            if (dirtyRectangles.Length > 0)
                duplicatedOutput.GetFrameDirtyRects(bufSize, dirtyRectangles, out bufSize);
            data.DirtyRectangles = dirtyRectangles;
            data.DirtyCount = dirtyCount;

            data.Frame = screenTexture;

            data.FrameInfo = duplicateFrameInformation;
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        public void GetChangedRects(ref FrameData data)
        {
            if (data == null)
                data = new FrameData();

            bool captured = false;
            //lock (duplicatedOutput)
                for (int i = 0; !captured; i++)
                {
                    try
                    {
                        Resource screenResource;
                        OutputDuplicateFrameInformation duplicateFrameInformation;

                        //try to get duplicated frame within given time
                        duplicatedOutput.AcquireNextFrame(TIME_OUT, out duplicateFrameInformation, out screenResource);

                        if (i > 0)
                        {
                            GetDirtyAndMoveRects(ref data, screenResource, duplicateFrameInformation);

                            captured = true;
                        }
                        duplicatedOutput.ReleaseFrame();
                    }
                    catch (SharpDXException e)
                    {
                        if (e.ResultCode.Code != ResultCode.WaitTimeout.Result.Code)
                            Console.WriteLine("GetChangedRects : {0}", e.Message);
                    }
                }
        }

        public void GetFrame(out FrameData data)
        {
            data = new FrameData();
            bool captureDone = false;
            //lock (duplicatedOutput)
                for (int i = 0; !captureDone; i++)
                    try
                    {
                        Resource screenResource;
                        OutputDuplicateFrameInformation duplicateFrameInformation;

                        // Try to get duplicated frame within given time
                        duplicatedOutput.AcquireNextFrame(TIME_OUT, out duplicateFrameInformation, out screenResource);

                        if (i > 0)
                        {
                            // copy resource into memory that can be accessed by the CPU
                            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                                device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                            screenResource.Dispose();

                            data.Frame = screenTexture;
                            data.Width = Width;
                            data.Height = Height;

                            Texture2DToBitmap().Save("save.bmp");

                            // Capture done
                            captureDone = true;
                        }

                        duplicatedOutput.ReleaseFrame();
                    }
                    catch (SharpDXException e)
                    {
                        if (e.ResultCode.Code != ResultCode.WaitTimeout.Result.Code)
                        {
                            Console.WriteLine("GetFrame : {0}", e.Descriptor);
                        }
                    }
        }

        public Bitmap Texture2DToBitmap()
        {
            return ExtractRect(0, 0, Width, Height);
        }

        //copy bitmap的指定区域
        public Bitmap ExtractRect(int originX, int originY, int width, int height)
        {
            // Get the desktop capture screenTexture
            DataBox mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read,
                MapFlags.None);

            // Create Drawing.Bitmap

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb); //不能是ARGB
            var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

            // Copy pixels from screen capture Texture to GDI bitmap
            BitmapData mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            IntPtr sourcePtr = mapSource.DataPointer;
            IntPtr destPtr = mapDest.Scan0;

            sourcePtr = IntPtr.Add(sourcePtr, originY*mapSource.RowPitch + originX*4);
            for (int y = 0; y < height; y++)
            {
                // Copy a single line 

                Utilities.CopyMemory(destPtr, sourcePtr, width*4);

                // Advance pointers
                if (y != height - 1)
                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            // Release source and dest locks
            bitmap.UnlockBits(mapDest);
            device.ImmediateContext.UnmapSubresource(screenTexture, 0);

            return bitmap;
        }
    }
}