using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AsynchronousGrabConsole;
using AVT.VmbAPINET;

namespace FrameCollector
{
    public delegate void OnBitmapReceived(Bitmap bmp);

    /**
     * Delegate to help with receiving images from Prosilica cameras
     */
    public class FrameController
    {
        public event OnBitmapReceived LeftImageReceived;
        public event OnBitmapReceived RightImageReceived;

        private VimbaHelper _vimbaHelperCam1;
        private VimbaHelper _vimbaHelperCam2;

        private static FrameController _fc;

        public static FrameController Fc => _fc ?? (_fc = new FrameController());

        private FrameController()
        {
        }

        public void Start()
        {
            _vimbaHelperCam1 = new VimbaHelper();
            _vimbaHelperCam1.Startup();

            _vimbaHelperCam2 = new VimbaHelper();
            _vimbaHelperCam2.Startup();

            List<Camera> cameras = _vimbaHelperCam1.CameraList;
            //todo make camera acquisition more dynamic
            _vimbaHelperCam1.StartContinuousImageAcquisition(cameras[0].Id, VimbaHelper.FrameInfos.Off);
            _vimbaHelperCam2.StartContinuousImageAcquisition(cameras[1].Id, VimbaHelper.FrameInfos.Off);

            _vimbaHelperCam1.OnFrameReceivedEvent += frame => CallImageEvent(frame, LeftImageReceived);
            _vimbaHelperCam2.OnFrameReceivedEvent += frame => CallImageEvent(frame, RightImageReceived);
        }

        public void Stop()
        {
            _vimbaHelperCam1?.StopContinuousImageAcquisition();
            _vimbaHelperCam2?.StopContinuousImageAcquisition();

            _vimbaHelperCam1?.Shutdown();
            _vimbaHelperCam2?.Shutdown();
        }


        private void CallImageEvent(Frame frame, OnBitmapReceived e)
        {
            Bitmap bmp = null;
            frame.Fill(ref bmp);
            bmp.RotateFlip(RotateFlipType.Rotate90FlipXY); // rotate 90° to the left
            e?.Invoke(bmp);
        }
    }
}