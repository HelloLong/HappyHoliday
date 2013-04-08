using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Media;
using Toolkit;
using System.IO;

namespace 节日快乐
{
    public partial class Form1 : Form
    {
        private LeafLabel[] textLabels;//显示文字的label
        private Queue<LeafLabel> imgLabelQueue;//显示图片的label
        private LeafLabel[] imgLabelsNotInQueue;
        public int imgLabelsOnScreenCount = 3;//同一时间只有3个图片label显示，太多不好看
        
        public readonly int maxWindChangeInterval = 300;
        public readonly int minWindChangeInterval = 200;
        public readonly int sleep = 40;//label下落速度有关
        private Random rnd;
        private Thread thread;
        private Font font;
        public readonly Rectangle screen; 

        public Form1()
        {
            InitializeComponent();
            
            screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;//得到屏幕的大小

            initializeResources();//载入资源文件
            play();//开始
        }

        void initializeResources()
        {
            Form.CheckForIllegalCrossThreadCalls = false;//取消跨线程访问同一个控件的限制
            rnd = new Random();//随机数和华文云彩的字体后面程序中用到
            font = new Font("华文彩云", 50, FontStyle.Bold);
            // 得到exe的文件名
            string moduleName = Assembly.GetExecutingAssembly().GetModules()[0].Name;
            string text = moduleName.Substring(0, moduleName.Length - 4);//剪去“.exe”

            PutTextToLabels(text);

            PutImageToLabels();
        }

        private void PutImageToLabels()
        {
            // 取得images文件夹中的图片文件
            DirectoryInfo imagedir = new DirectoryInfo(@".\images");
            if (imagedir.Exists)
            {
                FileInfo[] imagefiles = imagedir.GetFiles();

                //同一时间只有 imgLabelsOnScreenCount 个图片显示在屏幕内           
                if (imgLabelsOnScreenCount > imagefiles.Length)
                    imgLabelsOnScreenCount = imagefiles.Length;

                imgLabelsNotInQueue = new LeafLabel[imgLabelsOnScreenCount];
                imgLabelQueue = new Queue<LeafLabel>();

                for (int i = 0; i < imagefiles.Length; i++)
                {
                    Bitmap bmp = new Bitmap(imagefiles[i].FullName);
                    LeafLabel label = new LeafLabel(rnd.Next(LeafLabel.MinGravity, LeafLabel.MaxGravity + 1));
                    label.AutoSize = true;
                    label.MinimumSize = bmp.Size;//这句要加，不然label的Size默认是0
                    label.Image = bmp;// 设置label的图片
                    label.Region = FormWizard.Image2Region(bmp);// 设置label的轮廓为图片轮廓
                    if (i < imgLabelsOnScreenCount)
                    {
                        // 这些label显示在屏幕内
                        label.Location = new Point(rnd.Next(screen.Width), rnd.Next(screen.Height));
                        imgLabelsNotInQueue[i] = label;
                    }
                    else
                    {
                        //剩下的图片放在先进先出的列队里面，待以后循环显示
                        label.Location = new Point(0, -label.Height);
                        imgLabelQueue.Enqueue(label);
                    }
                    this.Controls.Add(label);

                }
            }
        }

        /// <summary>
        /// 把程序名分成一个一个字分别放到label里
        /// </summary>
        /// <param name="text">程序的名称</param>
        private void PutTextToLabels(string text)
        {
            // 将这些文件名中的文字放在label中
            textLabels = new LeafLabel[text.Length];
            for (int i = 0; i < textLabels.Length; i++)
            {
                // 设置每一个label的字体，随机前景色等
                char ch = text[i];
                textLabels[i] = new LeafLabel(rnd.Next(LeafLabel.MinGravity, LeafLabel.MaxGravity + 1));
                textLabels[i].AutoSize = true;
                Color color = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                Bitmap bmp = FormWizard.Text2Bitmap(ch + "", font, color);
                textLabels[i].Text = ch + "";
                textLabels[i].ForeColor = color;
                textLabels[i].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;//文字放在label中间
                textLabels[i].Font = font;
                textLabels[i].MinimumSize = bmp.Size;
                textLabels[i].Region = FormWizard.Image2Region(bmp);//label的轮廓设置成文字的轮廓
                textLabels[i].Location = new Point(rnd.Next(screen.Width - textLabels[i].Width), rnd.Next(screen.Height));
                this.Controls.Add(textLabels[i]);// 将label添加到透明窗口中
            }
        }

        /// <summary>
        /// 图片和文字开始飘落，并开始播放音乐
        /// </summary>
        void play()
        {
            thread = new Thread(
                () =>
                {
                    LeafLabel.Wind = rnd.Next(-1, 2);
                    while (true)
                    {
                        Thread.Sleep(sleep);

                        ChangeWind();

                        DisplayText();

                        DisplayImg();
                    }
                }
            );
            thread.IsBackground = true;//设置为backGround线程，会随着主程序退出而退出
            thread.Start();

            PlayMusic();
        }

        private void PlayMusic()
        {
            // 取得music文件夹中的音乐文件放到播放类表中
            DirectoryInfo musicdir = new DirectoryInfo(@".\music");
            if (musicdir.Exists)
            {
                FileInfo[] musicfiles = musicdir.GetFiles();
                if (musicfiles.Length > 0)
                {
                    axWindowsMediaPlayer1.currentPlaylist = axWindowsMediaPlayer1.newPlaylist("myList", "");
                    foreach (FileInfo file in musicfiles)
                    {
                        axWindowsMediaPlayer1.currentPlaylist.appendItem(axWindowsMediaPlayer1.newMedia(file.FullName));
                    }
                    axWindowsMediaPlayer1.settings.setMode("Loop", true);//循环播放
                    axWindowsMediaPlayer1.Ctlcontrols.play();//开始播放
                }
            }
        }

        private void DisplayImg()
        {
            for (int i = 0; i < imgLabelsNotInQueue.Length; i++)
            {
                if (imgLabelsNotInQueue[i].Location.Y > screen.Height || imgLabelsNotInQueue[i].Location.X < -imgLabelsNotInQueue[i].Width || imgLabelsNotInQueue[i].Location.X > screen.Width)
                {
                    //对于图片label,飘出屏幕范围的先放入列队尾，从列队头去处一个不同的图片放到屏幕上方开始飘落
                    imgLabelQueue.Enqueue(imgLabelsNotInQueue[i]);
                    imgLabelsNotInQueue[i] = imgLabelQueue.Dequeue();
                    imgLabelsNotInQueue[i].Gravity = rnd.Next(LeafLabel.MinGravity, LeafLabel.MaxGravity + 1);
                    imgLabelsNotInQueue[i].Location = new Point(rnd.Next(screen.Width - imgLabelsNotInQueue[i].Width), -imgLabelsNotInQueue[i].Height);
                }
                else
                {
                    // 屏幕范围内的label在风力的影响下飘落
                    imgLabelsNotInQueue[i].Location = new Point(imgLabelsNotInQueue[i].Location.X + LeafLabel.Wind, imgLabelsNotInQueue[i].Location.Y + imgLabelsNotInQueue[i].Gravity);
                }
            }
        }

        private void DisplayText()
        {
            foreach (LeafLabel label in textLabels)
            {
                if (label.Location.Y > screen.Height || label.Location.X < -label.Width || label.Location.X > screen.Width)
                {
                    //如果显示文字的label飘出屏幕范围，就重新设置它的飘落速度和颜色，再移动到屏幕上方
                    label.Location = new Point(rnd.Next(screen.Width - label.Width), -label.Height);
                    label.Gravity = rnd.Next(LeafLabel.MinGravity, LeafLabel.MaxGravity + 1);
                    label.ForeColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                }
                else
                {
                    // 屏幕范围内的label在风力的影响下飘落
                    label.Location = new Point(label.Location.X + LeafLabel.Wind, label.Location.Y + label.Gravity);
                }
            }
        }

        private void ChangeWind()
        {
            int k=0;
            int windChangeInterval = 200;//每隔一定时间改变风向
            // 每隔一定时间改变风向
            if (k++ > windChangeInterval)
            {
                windChangeInterval = rnd.Next(minWindChangeInterval, maxWindChangeInterval);
                k = 0;
                LeafLabel.Wind += rnd.Next(-1, 2);
                this.Invalidate();
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //axWindowsMediaPlayer1.Ctlcontrols.stop();
            if (thread != null)
                thread.Abort();
            Application.Exit();
        }

        /// <summary>
        /// 显示飘落的文字和图片的Label类
        /// </summary>
        public class LeafLabel : Label
        {
            public static readonly int MaxWind = 2;
            public static readonly int MaxGravity = 3;
            public static readonly int MinGravity = 1;

            public int Gravity;//重力，越重落得越快
            private static int _Wind = 0;//风力
            public static int Wind 
            { 
                get { return _Wind; } 
                set 
                { 
                    if (value <= MaxWind && value >= -MaxWind)
                        _Wind = value; 
                } 
            }

            public LeafLabel(int g)
                : base()
            {
                Gravity = g;
            }

        }
    }
}
