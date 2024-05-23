using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using GxIAPINET;

namespace DahengSdkDev3
{
    class Program
    {
        static void Main(string[] args)
        {
            #region 相机初始化与开始采集
            // 0 全局资源初始化
            IGXFactory.GetInstance().Init();

            // 1 查找相机
            List<IGXDeviceInfo> iGXDeviceInfos = new List<IGXDeviceInfo>(); // 创建列表，存放相机
            IGXFactory.GetInstance().UpdateDeviceList(1000, iGXDeviceInfos);

            // 2 获取相机信息，如SN
            string Sn = iGXDeviceInfos[0].GetSN();
            Console.WriteLine("设备SN码:{0}", Sn);

            // 3 打开设备。一般网口相机使用Ip，usb3相机使用SN
            IGXDevice cam = IGXFactory.GetInstance().OpenDeviceBySN(Sn, GX_ACCESS_MODE.GX_ACCESS_CONTROL);

            // 4 开始采集图像
            IGXStream cam_stream = cam.OpenStream(0); //打开采集流通道，通常打开第一个

            cam_stream.RegisterCaptureCallback(cam, _CallCaptureBack); // 注册回调函数

            cam_stream.StartGrab();

            // 打开远端控制器，发送采集命令
            IGXFeatureControl cam_remote_control = cam.GetRemoteFeatureControl();
            cam_remote_control.GetCommandFeature("AcquisitionStart").Execute();

            // 抓取一张图像
            //IImageData imageData = cam_stream.GetImage(1000); // 超时时间

            //ulong height = imageData.GetHeight();
            //ulong width  = imageData.GetWidth();

            //Console.WriteLine("图像宽:{0}, 高:{1}", width, height);
            //Console.ReadKey();
            #endregion


            #region 关闭相机
            //cam_stream.StopGrab();
            //cam_stream.UnregisterCaptureCallback();
            //cam.Close();
            //IGXFactory.GetInstance().Uninit();
            #endregion

        }

        #region 回调函数
        // 要实现回调采集，先创建回调函数。回调函数参数包含，一个自定义变量，一个图像数据
        // 回调函数可以理解为连续采集
        public static void _CallCaptureBack(object userParam, IFrameData framData)
        {
            ulong height = framData.GetHeight();
            ulong width = framData.GetWidth();

            Console.WriteLine("图像宽:{0}, 高:{1}", width, height);
        }
        #endregion
    }
}
