using System;
using System.Collections.Generic;
using System.Drawing;
using AVT.VmbAPINET;

namespace CameraControl
{
    public delegate void FrameReceivedHandler(Bitmap frame, double fps);

    public class CameraController
    {
        private enum SelectedCamera
        {
            Left,
            Right
        }
        
        private Vimba _vimba;
        private Camera _rightCamera;
        private Camera _leftCamera;

        // Time for FPS calculation
        private int _lastTicksLeft;
        private int _lastTicksRight;

        public event FrameReceivedHandler OnRightBitmapReceived;
        public event FrameReceivedHandler OnLeftBitmapReceived;

        public void Startup()
        {
            _vimba = new Vimba();
            _vimba.Startup();
        }

        public void StartAutomaticRecording()
        {
            List<Camera> cameras = CameraList;
            
            StartRecording(cameras[0].Id, cameras[1].Id);
        }

        public void StartRecording(string idLeft, string idRight)
        {
            #region Basic error checking

            // Check if API has been started up at all
            if (null == _vimba)
            {
                throw new Exception("Vimba is not started.");
            }

            if (null != _rightCamera && null != _leftCamera)
            {
                throw new Exception("A camera is already open.");
            }


            #endregion

            // open cameras
            _rightCamera = _vimba.OpenCameraByID(idRight, VmbAccessModeType.VmbAccessModeFull);
            _leftCamera = _vimba.OpenCameraByID(idLeft, VmbAccessModeType.VmbAccessModeFull);

            #region GeV packet size
            
            if (null == _rightCamera || null == _leftCamera)
            {
                throw new NullReferenceException("one or all cameras not retrieved.");
            }

            // Set the GeV packet size to the highest possible value (right camera)
            try
            {
                _rightCamera.Features["GVSPAdjustPacketSize"].RunCommand();
                while (false == _rightCamera.Features["GVSPAdjustPacketSize"].IsCommandDone())
                {
                    // Do nothing
                }
            }
            catch
            {
                // Do nothing
            }

            // Set the GeV packet size to the highest possible value (left camera)
            try
            {
                _leftCamera.Features["GVSPAdjustPacketSize"].RunCommand();
                while (false == _leftCamera.Features["GVSPAdjustPacketSize"].IsCommandDone())
                {
                    // Do nothing
                }
            }
            catch
            {
                // Do nothing
            }

            #endregion
            
            StartImageAcquisition(_rightCamera, OnRightBitmapReceived, SelectedCamera.Right);
            StartImageAcquisition(_leftCamera, OnLeftBitmapReceived, SelectedCamera.Left);
        }

        /// <summary>
        /// Gets the current camera list
        /// </summary>
        public List<Camera> CameraList
        {
            get
            {
                // Check if API has been started up at all
                if (null == _vimba)
                {
                    throw new Exception("Vimba is not started.");
                }

                List<Camera> cameraList = new List<Camera>();
                CameraCollection cameras = _vimba.Cameras;
                foreach (Camera camera in cameras)
                {
                    cameraList.Add(camera);
                }

                return cameraList;
            }
        }

        private void StartImageAcquisition(Camera c, FrameReceivedHandler receivedHandler, SelectedCamera selectedCamera)
        {
            // Register frame callback
            c.OnFrameReceived += frame =>
            {
                c.QueueFrame(frame); // inorder to keep everything continious
                CallImageEvent(frame, receivedHandler, selectedCamera);
            };

            // Start synchronous image acquisition (grab)
            c.StartContinuousImageAcquisition(30);
        }
        
        private void CallImageEvent(Frame frame, FrameReceivedHandler e, SelectedCamera selectedCamera)
        {
            Bitmap bmp = null;
            frame.Fill(ref bmp);
            bmp.RotateFlip(RotateFlipType.Rotate90FlipXY); // rotate 90° to the left
            
            if(selectedCamera == SelectedCamera.Left)
                e?.Invoke(bmp, CalcFPS(ref  _lastTicksLeft));
            else
                e?.Invoke(bmp, CalcFPS(ref  _lastTicksRight));
        }
        
        private double CalcFPS(ref int lastTicks)
        {
            int sytemTimeLocal = System.Environment.TickCount;

            double fps = 1000 / (double)(sytemTimeLocal - lastTicks);

            lastTicks = sytemTimeLocal;

            return fps;
        }
    }
}