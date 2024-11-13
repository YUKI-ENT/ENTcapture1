using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ENTcapture
{
    public partial class Form5 : Form
    {
        public Form1 formMain;
        //private Bitmap bmpOrg; //受け取ったbmp
        private int picWidth, picHeight, zoom = 8; //zoom 1-16
        
        public delegate void DelegateShowBmp(Bitmap bmp, int bmpWidth, int bmpHeight);
        public DelegateShowBmp dShowBmp;

        public Form5()
        {
            InitializeComponent();
            //ハンドルが作成されていなければ作成。Invokeする際に必要です。
            if (!IsHandleCreated) CreateHandle();
            //デリゲートに委任する処理(関数)を定義。
            dShowBmp = ShowBmp;

            // ホイールイベントの追加  
            this.MouseWheel
                += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseWheel);
        }

        private async void ShowBmp(Bitmap bmp, int bmpWidth, int bmpHeight)
        {
            try
            {
                formMain.lockOutbmp = true;

                //this.Size = new Size(bmp.Width, bmp.Height);
                picWidth = bmpWidth;
                picHeight = bmpHeight;

                if (bmp != null)
                {
                    Bitmap resizedBmp = await Task.Run(() => ProcessBmp(bmp));
                    //Bitmap resizedBmp = ProcessBmp(bmp);
                    await UpdatePictureBox(resizedBmp);
                    //Task.Run(() =>
                    //{
                    //    DrawBmp(bmp, zoom);
                    //});

                }
            }
            catch (Exception ex)
            {
                formMain.LogError(ex);
                //MessageBox.Show(ex.ToString());
                formMain.PauseReloadFlag = true;
            }
            finally
            {
                formMain.lockOutbmp = false;
            }
        }

        // マウスホイールイベント  
        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if(e.Delta > 0)
            {
                zoom--;
            }
            else
            {
                zoom++;
            }
            if (zoom > 16) zoom = 16;
            if (zoom < 1) zoom = 1;

            this.Text = String.Format("Zoom:{0:0.0}x", (float)zoom / 8.0);

            formMain.PauseReloadFlag = true;
        }

        private async Task UpdatePictureBox(Bitmap resizedBmp)
        {
            if (resizedBmp == null) return;

            var oldimg = pictureBox1.Image;
            pictureBox1.BeginInvoke((Action)(() =>
            {
                pictureBox1.Image = resizedBmp;
                oldimg?.Dispose();

            }));
            // メモリ解放のチェック
            await Task.Delay(1);  // 必要に応じて間隔を調整
        }

        //private void DrawBmp(Bitmap bmp, int zoom) //Zoom:mouse wheel delta
        //{
        //    Bitmap resizedBmp = null;
        //    try
        //    {
        //        if (bmp != null)
        //        {
        //            formMain.lockOutbmp = true;

        //            using (Mat srcmat = bmp.ToMat())
        //            using (Mat dstmat = new Mat())
        //            {
        //                var dstSize = new OpenCvSharp.Size(picWidth * zoom / 8, picHeight * zoom / 8);

        //                Cv2.Resize(srcmat, dstmat, dstSize, 0, 0, InterpolationFlags.Lanczos4);
        //                resizedBmp = dstmat.ToBitmap();
        //            }

        //            var oldimg = pictureBox1.Image;
        //            // UIスレッドに戻す
        //            this.BeginInvoke((Action)(() =>
        //            {
        //                // UIスレッドでpictureBox1.Imageにアクセス
        //                pictureBox1.Image = resizedBmp;
        //                oldimg?.Dispose();
        //            }));

        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString());
        //        //MessageBox.Show("拡大表示時にエラーが発生しました");
        //    }
        //    finally { 
        //        formMain.lockOutbmp = false;

        //    }
        //}

        private Bitmap ProcessBmp(Bitmap bmp)
        {
            try
            {
                Bitmap resizedBmp;
                if (bmp == null)
                {
                    formMain.PauseReloadFlag = true;
                    resizedBmp = null;
                }
                else
                {
                    using (Mat srcmat = bmp.ToMat())
                    using (Mat dstmat = new Mat())
                    {
                        var dstSize = new OpenCvSharp.Size(picWidth * zoom / 8, picHeight * zoom / 8);
                        Cv2.Resize(srcmat, dstmat, dstSize, 0, 0, InterpolationFlags.Lanczos4);
                        resizedBmp = dstmat.ToBitmap();
                    }
                }
                return resizedBmp;
            }
            catch (Exception ex)
            {
                formMain.LogError(ex);
                formMain.PauseReloadFlag = true;
                return null;
            }
        }

        private void Form5_FormClosed(object sender, FormClosedEventArgs e)
        {
            formMain.PauseReloadFlag = true;
        }

        private void Form5_KeyDown(object sender, KeyEventArgs e)
        {
            //try
            //{
            //    if (e.KeyData == formMain.snapkey)
            //    {
            //        formMain.SnapFlag = true;
            //    }
            //}
            //catch (Exception)
            //{

            //}
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            this.Location = new System.Drawing.Point(this.Owner.Location.X, this.Owner.Location.Y + 220);

            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.TopMost = true;

            this.Text = "マウスホイールで拡大縮小ができます";

        }
    }
}
