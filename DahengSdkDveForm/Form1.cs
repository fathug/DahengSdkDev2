using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GxIAPINET;
using GxIAPINET.Sample.Common;

namespace DahengSdkDveForm
{
    public partial class Form1 : Form
    {
        IGXDevice cam; //相机对象
        IGXStream cam_stream; //流对象。全局，button1_Click和button3_Click都要用
        IGXFeatureControl cam_remote_control;

        bool colorcam = false;
        static bool colorflag = false; //回调中需要是静态

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IGXFactory.GetInstance().Init();

            List<IGXDeviceInfo> iGXDeviceInfos = new List<IGXDeviceInfo>();
            IGXFactory.GetInstance().UpdateDeviceList(1000, iGXDeviceInfos);

            if(iGXDeviceInfos.Count == 0)
            {
                MessageBox.Show("无相机连接");
                return;
            }

            // sub相机没有IP信息
            string sN = iGXDeviceInfos[0].GetSN();
            textBox1.AppendText(sN+"\r\n");
            string name = iGXDeviceInfos[0].GetDisplayName();
            textBox1.AppendText(name+"\r\n");

            cam = IGXFactory.GetInstance().OpenDeviceBySN(sN, GX_ACCESS_MODE.GX_ACCESS_CONTROL);

            cam_remote_control = cam.GetRemoteFeatureControl(); // 打开相机的时候，就把远端属性控制器获取到

            uint cam_stream_num = cam.GetStreamCount(); // 流通道数量
            IsSupportColor(ref colorcam, cam); //彩色黑白判断
            IsSupportColor(ref colorflag, cam); //彩色黑白判断

            cam_stream = cam.OpenStream(0);

        }

        // 单帧采集，使用GetImage函数
        private void button3_Click(object sender, EventArgs e)
        {
            // 流通道开采
            cam_stream.StartGrab();
            cam_remote_control.GetCommandFeature("AcquisitionStart").Execute(); //通过属性控制器发送开采命令

            IImageData imgData = cam_stream.GetImage(1000);

            int width = (int)imgData.GetWidth();
            int height = (int)imgData.GetHeight();

            // 显示到界面
            // 注意，彩色相机与黑白相机的显示是不同的，需要先判断一下

            if (colorcam)
            {
                // 获取buffer
                IntPtr buffer = imgData.ConvertToRGB24(GX_VALID_BIT_LIST.GX_BIT_0_7,
                    GX_BAYER_CONVERT_TYPE_LIST.GX_RAW2RGB_NEIGHBOUR, false);
                // 把buffer转换为BMP格式
                Bitmap bitMap = new Bitmap(width, height, width*3, 
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb, buffer);
                // 显示
                pictureBox1.Image = bitMap;
            }
            else // 黑白，不需要对buffer转换，直接转为bmp
            {
                Bitmap bitMap = new Bitmap(width, height, width,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed, imgData.GetBuffer());
                // 黑白需要用调色盘进行像素翻转
                ColorPalette colorPalette = bitMap.Palette;
                for (int i = 0; i < 255; i++)
                {
                    colorPalette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitMap.Palette = colorPalette;
                // 显示
                pictureBox1.Image = bitMap;
            }
            button4.Enabled = false;
        }


        /// <summary>
        /// 是否支持彩色
        /// </summary>
        /// <param name="bIsColorFilter">是否支持彩色</param>
        public void IsSupportColor(ref bool bIsColorFilter, IGXDevice cam)
        {
            bool bIsImplemented = false;
            bool bIsMono = false;
            string strPixelFormat = "";
            uint nPixelFormatValue = 0;

            strPixelFormat = cam.GetRemoteFeatureControl().GetEnumFeature("PixelFormat").GetValue();
            if (0 == string.Compare(strPixelFormat, 0, "Mono", 0, 4))
            {
                bIsMono = true;
            }
            else
            {
                bIsMono = false;
            }


            // 通过当前像素格式判断是否为彩色相机
            CDecide.GetConvertPixelFormat(strPixelFormat, ref nPixelFormatValue);
            bIsImplemented = CDecide.GetIsGray(nPixelFormatValue);
            if ((!bIsMono) && (!bIsImplemented))
            {
                bIsColorFilter = true;
            }
            else
            {
                bIsColorFilter = false;
            }
        }


        // 要实现回调采集，先创建回调函数。回调函数参数包含，一个自定义变量，一个图像数据
        // 回调函数可以理解为连续采集
        /// <summary>
        /// 回调函数
        /// </summary>
        /// <param name="userParam"></param>
        /// <param name="framData"></param>
        public static void _CallCaptureBack(object userParam, IFrameData imgData)
        {
            // 使用 用户自定义变量 把picturebox传递进来
            PictureBox pictureBox = userParam as PictureBox; //这句的作用是，把userParm转换为PictureBox类型

            int width = (int)imgData.GetWidth();
            int height = (int)imgData.GetHeight();

            if (colorflag)
            {
                // 获取buffer
                IntPtr buffer = imgData.ConvertToRGB24(GX_VALID_BIT_LIST.GX_BIT_0_7,
                    GX_BAYER_CONVERT_TYPE_LIST.GX_RAW2RGB_NEIGHBOUR, false);
                // 把buffer转换为BMP格式
                Bitmap bitMap = new Bitmap(width, height, width * 3,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb, buffer);
                // 显示
                Action action = () =>
                {
                    pictureBox.Image = bitMap;
                };
                pictureBox.Invoke(action);
            }
            else // 黑白，不需要对buffer转换，直接转为bmp
            {
                Bitmap bitMap = new Bitmap(width, height, width,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed, imgData.GetBuffer());
                // 黑白需要用调色盘进行像素翻转
                ColorPalette colorPalette = bitMap.Palette;
                for (int i = 0; i < 255; i++)
                {
                    colorPalette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitMap.Palette = colorPalette;

                // 由于回调函数是在线程中采集，另开一个现场。而pictureBox是主函数中的一个变量，所以需要使用委托的方式对picturebox进行操作
                Action action = () =>
                {
                    pictureBox.Image = bitMap;
                };
                pictureBox.Invoke(action);
            }

        }



        // 连续采集，使用回调方式
        private void button4_Click(object sender, EventArgs e)
        {
            // 注册回调函数
            cam_stream.RegisterCaptureCallback(pictureBox1, _CallCaptureBack);

            cam_stream.StartGrab();

            cam_remote_control.GetCommandFeature("AcquisitionStart").Execute(); //发送开采命令

            button3.Enabled = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            cam_stream.StopGrab();
            cam_remote_control.GetCommandFeature("AcquisitionStop").Execute();

            button3.Enabled = true;
            button4.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cam_remote_control.GetCommandFeature("AcquisitionStop").Execute();
            cam_stream.StopGrab();
            cam_stream.UnregisterCaptureCallback();
            cam.Close();
            IGXFactory.GetInstance().Uninit();
        }
    }
}
