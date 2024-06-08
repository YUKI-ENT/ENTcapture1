using Accord;
//using System.Threading;
//using Point = System.Drawing.Point;
using Accord.Imaging.Filters;
using Accord.Video;
using Accord.Video.DirectShow;
using Npgsql;
using OpenCvSharp;
using OpenCvSharp.Extensions;
////using Brush = System.Drawing.Brush;
using Shell32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ENTcapture
{

    public partial class Form1 : Form
    {
        //       String moviePath = Properties.Settings.Default.outdir;
        //       String tmpavi = Properties.Settings.Default.tmpdir +  "temp.avi";
        string PtID = "", PtName = "";
        Dictionary<string, string> dicPtHisory = new Dictionary<string, string>();

        private const int orgWidth = 800, orgHeight = 818;
        private const string CaptureButtonOn = "取込開始";
        private const string CaptureButtonOff = "取込終了";
        private bool DeviceExist = false;                // デバイス有無
        public FilterInfoCollection videoDevices;       // カメラデバイスの一覧
        private VideoCaptureDevice videoSource = null;   // カメラデバイスから取得した映像
        private VideoCapabilities[] videoCapabilities;
        private string message;
        private int message_time = 5, t = 0;                              //Message 表示時間sec
        private int rec_start = 4, rec_count = 0; // rec_delay - rec_start間のfpsを測定
        private bool rec_state = false, video_open = false, keydown = false;
        private int jpeg_quality = 100;
        public Keys snapkey, startkey;
        private int vwidth, vheight;
        private byte playmode = 0, mode = 0;
        private int videoPosition = 0;
        private string testname = "";  //Videorecordスレッドからcomboboxtestが参照できないので
        private int[] rois = new int[4]; //x1,y1,x2,y2
        private string[,] ol_str = new string[20, 2]; // color, str 
        private int[,] ol_p = new int[200, 3]; // x,y,size
        private string ol_font;
        private string[] id_data = new string[2];

        public int frame_start = 0, frame_end = 0, total_frames = 0;
        private System.Drawing.Point MD = new System.Drawing.Point();
        private System.Drawing.Point MC = new System.Drawing.Point();
        private System.Drawing.Point MU = new System.Drawing.Point();
        private bool view = false;
        private int[] filterWB = new int[4] { 0, 255, 255, 255 }; // onoff, r,g,b
        private int[] filterGamma = new int[2] { 0, 10 }; //onoff, gamma*10 
        private int filterFlip = 10; //10:Off, 0:上下、1:左右、-1：上下左右
        private bool lockBmp = false;
        public bool lockOutbmp = false;
        private bool MouseUpFlag = false;
        private int TotalFrames = 0, SkippedFrames = 0;

        public bool SnapFlag = false, PauseReloadFlag = false;

        public string videofile;

        public string[,] resos = new string[8, 128]; //DeviceName, resos

        //private Accord.Video.FFMPEG.VideoFileWriter acout;

        private VideoWriter cvout = new VideoWriter();
        private int cvoutfps = 0, presetFPS = 0;
        //private Bitmap imgout;
        // ビットマップの配列を宣言
        private int BUFFERNUMBER = 4;
        private Bitmap[] bmpBuffer = new Bitmap[4];
        private int bufferIndex = 0;
        Stopwatch sw = new Stopwatch();

        Form5 formDisp;

        //キーボードGlobal Hook
        private GlobalKeyboardHook keyboardHook;
        private bool pressedCtrl=false, pressedShift=false;
        private Keys charSnapKey, charStartKey;

        //Log
        private string LogFile;

        static Semaphore semaphore = new Semaphore(2, 2); // 初期化時に2つのスロットを持つ Semaphore オブジェクトを作成します

        public Form1()
        {
            InitializeComponent();

            // 前バージョンからのUpgradeを実行していないときは、Upgradeを実施する
            if (Properties.Settings.Default.IsUpgrade == false)
            {
                // Upgradeを実行する
                Properties.Settings.Default.Upgrade();

                // 「Upgradeを実行した」という情報を設定する
                Properties.Settings.Default.IsUpgrade = true;

                // 現行バージョンの設定を保存する
                Properties.Settings.Default.Save();
            }

            formDisp = new Form5();
            formDisp.formMain = this;

            // グローバルキーボードフックの初期化
            keyboardHook = new GlobalKeyboardHook();
            keyboardHook.KeyDown += KeyboardHook_KeyDown;
            keyboardHook.KeyUp += KeyboardHook_KeyUp;

            
        }

        private async void button1_Click(object sender, EventArgs e) //再生
        {
            Debug.WriteLine("Button1 clicked");
            mode = 1;  //再生モード
            //Stop camera
            this.CloseVideoSource();

            if (playmode > 0) playmode = 0;


            //オープンファイルダイアログを表示する
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "録画ファイルを開く";
            op.Filter = "Video Files(*.avi; *.mp4)|*.avi; *.mp4|All files(*.*) | *.*";
            op.FilterIndex = 1;
            op.InitialDirectory = Properties.Settings.Default.tmpdir;
            //  if (this.checkBoxVideo.Checked) op.InitialDirectory = Properties.Settings.Default.outdir;

            //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
            op.RestoreDirectory = false;

            DialogResult result = op.ShowDialog();
            videofile = op.FileName;
            op.Dispose();
            if (result != DialogResult.OK)
            {
                playmode = 0; // 再生終了
                //this.button3.Text = "再生";
                drawBar(0, 0, Brushes.Gray);

                ctlLock(9);
                mode = 0;
                return;
            }

            //FilenameからID番号取得
            string[] ids = System.IO.Path.GetFileNameWithoutExtension(videofile).Split('~');
            if (ids[0].Length > 0)
            {
                PtID = ids[0];
                this.comboBoxID.Text = PtID;

                comboBoxIDUpdate();

                comboBoxNameUpdate();
            }
            //検査名取得、設定、プリセットモードも
            if(ids.Length > 3 && ids[3].Length > 0)
            {
                comboBoxTest.SelectedIndex = comboBoxTest.Items.IndexOf(ids[3]);
            }

            playmode = 3; //再生中 0:stop,1:recstandby, 2:rec, 3:play, 4:pause
            trackBar1.Value = 0;
            await playVideoAsync(videofile);

        }

        private async Task readVideoAsync(string file) //ファイル再生
        {
            Debug.WriteLine("readVideoAsync is called.");

            semaphore.WaitOne(); // semaphore から排他的なアクセス権を取得します

            //フィルタ値をデフォルトにする
            resetWB();
            resetGamma();
            filterFlip = 10;

            using (var vcap = new VideoCapture(file))
            using (var m = new Mat())
            {
                try
                {
                    int interval = (int)(1000 / vcap.Fps); //msec
                    total_frames = vcap.FrameCount; // 総フレーム数の取得
                    int current_frame = vcap.PosFrames;
                    this.trackBar1.Maximum = total_frames;

                    frame_end = total_frames;

                    Debug.WriteLine(string.Format("fps:{0},frames:{1},Pos:{2}", vcap.Fps, total_frames, current_frame));

                    System.Diagnostics.Stopwatch swplay = new System.Diagnostics.Stopwatch();

                    while (vcap.IsOpened())
                    {
                        swplay.Start(); // 計測開始

                        drawStatus(playmode);
                        if (playmode == 3) //再生中
                        {
                            if (vcap.Read(m))
                            {
                                if (lockBmp)
                                {
                                    message = string.Format("再生中フレームスキップしました");
                                    t = 0;
                                    LogEvents(message + " Frame:" + current_frame.ToString());
                                    return;
                                }

                                lockBmp = true;

                                current_frame = vcap.PosFrames;
                                this.trackBar1.Value = current_frame;
                                videoPosition = vcap.PosMsec;
                                this.labelTime.Text = ((float)videoPosition / 1000.0).ToString("F2") + "sec";
                                this.labelFPS.Text = vcap.Fps.ToString("F2") + "fps";
                                this.labelFrames.Text = current_frame.ToString() + "/" + total_frames.ToString();
                                
                                using (Bitmap ImgOrig = BitmapConverter.ToBitmap(m))
                                {
                                    await ProcessBitmapAsync(ImgOrig, false);
                                }
                                lockBmp = false;
                            }
                            else  //最後まで再生終了
                            {
                                playmode = 4;
                                drawStatus(4);
                                //this.button3.Text = "再開";
                                vcap.PosFrames = total_frames - 1;
                            }

                            //計測終了
                            swplay.Stop();
                            long elapsedMs = swplay.ElapsedMilliseconds;
                            if (elapsedMs > interval) elapsedMs = interval;
                            swplay.Reset();

                            //Debug.Print(string.Format("Elapsed time: {0} msec",t));

                            await Task.Delay((int)(interval - elapsedMs));
                        }
                        else if (playmode == 4) //一時停止中
                        {
                            int tr = this.trackBar1.Value;
                            if (tr != current_frame || checkBoxWB.Checked || SnapFlag || MouseUpFlag || PauseReloadFlag) //trackbarを操作した
                            {
                                current_frame = tr;
                                vcap.PosFrames = tr - 1;
                                if (vcap.Read(m))
                                {
                                    if (lockBmp)
                                    {
                                        message = string.Format("サーチ中フレームスキップしました");
                                        t = 0;
                                        LogEvents(message + " Frame:" + current_frame.ToString());
                                        return;
                                    }

                                    lockBmp = true;

                                    videoPosition = vcap.PosMsec;
                                    this.labelTime.Text = ((float)videoPosition / 1000.0).ToString("F2") + "sec";
                                    this.labelFPS.Text = vcap.Fps.ToString("F2") + "fps";
                                    this.labelFrames.Text = current_frame.ToString() + "/" + total_frames.ToString();

                                    using (Bitmap ImgOrig = BitmapConverter.ToBitmap(m))
                                    {
                                        await ProcessBitmapAsync(ImgOrig, false);
                                    }
                                    PauseReloadFlag = false;

                                    lockBmp = false;
                                }
                            }

                            //計測終了
                            swplay.Stop();
                            swplay.Reset();

                            await Task.Delay(interval);
                        }
                        else break; //停止コマンド
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    LogError(e);
                }
                finally
                {
                    Debug.WriteLine("readvideoAsync is finished.");
                    drawStatus(0);
                    vcap.Dispose();
                    m.Dispose();//Memory release
                    semaphore.Release(); // semaphore の排他的なアクセス権を解放します
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e) //開始終了ボタン
        {
            try
            {
                //キャプチャモード

                //再生中なら止める
                if (mode == 1)
                {
                    this.button4.PerformClick();
                    // 停止するまで待つ 
                    int i = 0;
                    while (mode == 1 && i < 500)
                    {
                        await Task.Delay(10);
                        i++;
                    }
                }

                testname = this.comboBoxTest.Text;

                if (button2.Text == CaptureButtonOn) // Off -> On
                {
                    if (DeviceExist)
                    {
                        TotalFrames = 0;
                        SkippedFrames = 0;

                        ctlLock(0);

                        drawStatus(1);

                        loadFilter(); //デバイスリストに存在すればここでフィルターがOnになる

                        connectVideo();
                        videoSource.VideoResolution = videoCapabilities[toolStripComboBoxResolution.SelectedIndex];

                        LogEvents("[録画開始] Device:" + videoDevices[toolStripComboDevices.SelectedIndex].Name + "/" + toolStripComboBoxResolution.Text);

                        videoSource.NewFrame += new NewFrameEventHandler(videoRendering);

                        videoSource.Start();

                        button2.Text = CaptureButtonOff;

                        message = "取り込み中";
                        if (checkBoxNorec.Checked)
                        {
                            message = "プレビュー中";
                        }

                        t = 0;
                        //timer1.Enabled = true;
                        video_open = true;
                        rec_count = 0;
                    }
                    else
                    {
                        message = "キャプチャデバイスが存在しません";
                        t = 0;
                    }
                }
                else  // 終了
                {
                    message = "ビデオデバイスを閉じました";
                    t = 0;
                    button2.Text = CaptureButtonOn;

                    if (videoSource.IsRunning)
                    {
                        drawStatus(0);
                        ctlLock(9);

                        //   timer1.Enabled = false;
                        video_open = false;

                        this.CloseVideoSource();
                        if (rec_state)
                        {
                            cvout.Release();
                            cvout.Dispose();
                            //acout.Close();
                            //acout.Dispose();
                            rec_state = false;

                            LogEvents("[録画終了] Drop frames:" + SkippedFrames.ToString() + "/Total frames:" + TotalFrames.ToString());

                            
                            // 複数の非同期処理を同時に開始
                            List<Task> tasks = new List<Task>();

                            // 動画保存の場合は出力フォルダーにコピー
                            if (this.checkBoxVideo.Checked)
                            {
                                Task encodeTask = EncodeAndRSB(videofile);
                                //await encodeTask;
                                tasks.Add(encodeTask);
                            }
                            else
                            {
                                Task rsbTask = Rsb_reload();
                                //await rsbTask;
                                tasks.Add(rsbTask);
                            }
                            
                            //録画したファイルを再生する
                            playmode = 3; //再生中 0:stop,1:recstandby, 2:rec, 3:play, 4:pause
                            trackBar1.Value = 0;

                            this.Activate();
                            this.BringToFront();

                            //フィルターを解除
                            checkBoxWB.Checked = false;

                            Task playTask = playVideoAsync(videofile);
                            tasks.Add(playTask);

                            // すべてのタスクが完了するのを待つ
                            await Task.WhenAll(tasks);
                        }
                        else //プレビューのみの場合
                        {
                            await Rsb_reload();
                        }
                    }
                }
            }
            catch (Exception other)
            {
                MessageBox.Show("キャプチャデバイス操作時にエラーが発生しました。" + other.Message);
                LogError(other);

                button2.Text = CaptureButtonOn;
                drawStatus(0);
                ctlLock(9);

                //   timer1.Enabled = false;
                video_open = false;

                this.CloseVideoSource();
            }

        }

        private async Task playVideoAsync(string videoFile)
        {
            mode = 1;
            ctlLock(1);

            frame_start = 0;
            drawBar(0, 100, Brushes.Turquoise);
            if (playmode == 4)
            { // 一時停止
                //this.button3.Text = "再生";
            }
            else
            {
                //this.button3.Text = "一時停止";
            }

            await readVideoAsync(videoFile);

            playmode = 0; // 再生終了
            //this.button3.Text = "再生";
            drawBar(0, 0, Brushes.Gray);

            ctlLock(9);
            mode = 0;
        }

        private async Task EncodeAndRSB(string videoFile)
        {
            var f = new FileInfo(videofile);
            string fn = f.Name;
            string outfile = Properties.Settings.Default.outdir + "\\" + fn;
            //Fileが大きければ再エンコード
            if (Properties.Settings.Default.encodesize > 0 && f.Length >= (Properties.Settings.Default.encodesize * 1000000))
            {
                LogEvents("ファイルが指定サイズより大きいので再エンコードを行います");

                await EncodeMovie(f.FullName, Properties.Settings.Default.outdir, Properties.Settings.Default.encodesize, Properties.Settings.Default.recodec);
                await Rsb_reload();
                //  this.buttonFFmpeg.PerformClick();
            }
            else
            {
                LogEvents("動画ファイルをRSBASEに出力しました");
                f.CopyTo(outfile, true);
                await Rsb_reload();
            }
            //MessageBox.Show("動画をファイリングします");
        }


        // カメラ情報の取得
        private void getCameraInfo()
        {
            try
            {
                // 端末で認識しているカメラデバイスの一覧を取得
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                toolStripComboDevices.Items.Clear();

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                int i = 0;
                foreach (FilterInfo device in videoDevices)
                {
                    // カメラデバイスの一覧をコンボボックスに追加
                    toolStripComboDevices.Items.Add(device.Name);
                    DeviceExist = true;
                    LogEvents($"ビデオデバイス{device.Name}を一覧に追加しました");

                    //VideoCapabilityの取得
                    videoSource = new VideoCaptureDevice(device.MonikerString);
                    videoCapabilities = videoSource.VideoCapabilities;

                    int j = 0;
                    foreach (VideoCapabilities c in videoCapabilities)
                    {
                        resos[i, j] = string.Format("{0}x{1}:{2}fps", c.FrameSize.Width, c.FrameSize.Height, c.AverageFrameRate);
                        j++;
                    }
                    videoSource = null;
                    i++;
                }
                toolStripComboDevices.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogError(ex);

                DeviceExist = false;
                toolStripComboDevices.Items.Add("ビデオデバイスがありません");
            }

        }

        //デバイス接続
        private void connectVideo()
        {
            videoSource = new VideoCaptureDevice(videoDevices[toolStripComboDevices.SelectedIndex].MonikerString);
            videoCapabilities = videoSource.VideoCapabilities;
        }

        // 描画処理
        private async void videoRendering(object sender, NewFrameEventArgs eventArgs)
        {
            // Frame -(clone)-> img -(WB)-(gamma)-->(clone)->imgout-(mouse rect)-(getAverage)->[描画]
            //                                 |
            //                               [Snap]->[Rec]
            TotalFrames++;

            if (lockBmp)
            {
                SkippedFrames++;
                message = string.Format("取込中フレームスキップしました");
                t = 0;
                LogEvents(message + " Frame:" + TotalFrames.ToString());
                return;
            }
            else
            {
                lockBmp = true;

                using (Bitmap ImgOriginal = (Bitmap)eventArgs.Frame.Clone())
                {
                    await ProcessBitmapAsync(ImgOriginal, true);
                }

                lockBmp = false;
            }

            labelFrames.Invoke((MethodInvoker)delegate
            {
                labelFrames.Text = "Drop:" + SkippedFrames.ToString() + "/" + TotalFrames.ToString();
            });
        }


        //録画

        private async Task videoRecord(Bitmap bmp)
        {
            try
            {
                // delay経過 fpsは取得できてるはず
                if (rec_count >= rec_start && cvoutfps > 0 && rec_state == false)
                {
                    drawStatus(2);

                    Debug.WriteLine("Rec start. FPS is " + cvoutfps.ToString());
                    vwidth = bmp.Width;
                    vheight = bmp.Height;

                    OpenCvSharp.Size dsize = new OpenCvSharp.Size(vwidth, vheight);

                    string s = Properties.Settings.Default.codec;
                    string ex = ".avi";
                    int fc;

                    if (s == "raw") fc = 0;
                    else if (s[0] == 'M' || s[0] == 'H')
                    {
                        ex = ".mp4";
                        FourCC fourcc = VideoWriter.FourCC(s[0], s[1], s[2], s[3]);
                        fc = fourcc;
                    }
                    else
                    {
                        FourCC fourcc = VideoWriter.FourCC(s[0], s[1], s[2], s[3]);
                        fc = fourcc;

                    }

                    string v = Properties.Settings.Default.tmpdir + "\\";

                    v += PtID + "~%%%%~" + DateTime.Now.ToString("yyyy_MM_dd") + "~" + testname + "~RSB" + ex;

                    for (int i = 1; i < 100; i++)
                    {
                        videofile = v.Replace("%%%%", i.ToString("D2"));
                        if (!File.Exists(videofile)) break;
                    }

                    Debug.WriteLine(string.Format("OpenCvSharp.Size:{0}x{1}, Codec:{2},file:{3}", vwidth, vheight, s, videofile));

                    int fps = cvoutfps;
                    if (presetFPS > 0) fps = presetFPS;

                    cvout = new VideoWriter(videofile, fc, fps, dsize);
                    Debug.WriteLine("cvout start.");
                    //writer.Open(fileName: videofile, width: 640, height: 480);
                    rec_state = true;

                    cvout.Set(VideoWriterProperties.Quality, 100);
                    message = string.Format("録画開始:file:{0}", videofile);
                    t = 0;
                }
                if (rec_state)
                {
                    await Task.Run(() =>
                    {
                        using (Mat m = bmp.ToMat())
                        {
                            cvout.Write(m);
                            //m.Dispose();
                        }
                    });

                }
            }
            catch (Exception e)
            {
                LogError(e);

                MessageBox.Show("録画処理中にエラーが発生しました\r\n" + e.ToString());

                drawStatus(0);
                cvout.Release();
                cvout.Dispose();
                rec_state = false;
                //bmp.Dispose();
            }

        }

        //画像表示関数
        private async Task ProcessBitmapAsync(Bitmap bitmap, bool Rec)
        {
            try
            {
                //Filter 適用

                if (checkBoxWB.Checked)
                {
                    await ApplyWBasync(bitmap, filterWB[1], filterWB[2], filterWB[3]);

                    if (filterGamma[1] != 10)
                    {
                        await ApplyGammaAsync(bitmap, (float)filterGamma[1] / 10);
                    }

                    if(filterFlip != 10) //Flip
                    {
                        bitmap.RotateFlip(OpenCVFilterMode2RotationFlipType(filterFlip));
                    }
                }
                if (MouseUpFlag) // フィルタ領域の設定完了→RBG平均を数値ボックスにセット
                {
                    await Task.Run(() =>
                    {
                        getRGBaverage(bitmap, Math.Min(MD.X, MU.X), Math.Min(MD.Y, MU.Y), Math.Max(MD.X, MU.X), Math.Max(MD.Y, MU.Y));
                    });
                    MouseUpFlag = false;
                }

                // 複数の非同期処理を同時に開始
                List<Task> tasks = new List<Task>();

                //Snap
                if (SnapFlag)
                {
                    Task SnapTask = SnapBmp(bitmap);
                    tasks.Add(SnapTask);
                    SnapFlag = false;
                }

                if (!lockOutbmp)
                {
                    bmpBuffer[bufferIndex]?.Dispose();
                    bmpBuffer[bufferIndex] = (Bitmap)bitmap.Clone();


                    //MOUSE 領域指定
                    if (view) //mouse drug 
                    {
                        DrawRegion(MD, MC, bmpBuffer[bufferIndex]);
                    }
                    else if (checkBoxWB.Checked) // mouse released
                    {
                        DrawRegion(MD, MU, bmpBuffer[bufferIndex]);
                    }

                    //描画
                    if (!formDisp.Created || formDisp == null)
                    {
                        Task DrawTask = Task.Run(() => SwapImage(pictureBox1, bmpBuffer[bufferIndex]));
                        tasks.Add(DrawTask);
                    }
                    else
                    {
                        if(pictureBox1.Image != null)  SwapImage(pictureBox1, null);

                        Task DrawForm5Task = Task.Run(() => formDisp.BeginInvoke(formDisp.dShowBmp, bmpBuffer[bufferIndex], vwidth, vheight));
                        tasks.Add(DrawForm5Task);
                    }
                   
                    //bmpBuffer[bufferIndex]?.Dispose();
                }

                //録画
                if (Rec && !checkBoxNorec.Checked)
                {
                    Task RecordTask = videoRecord(bitmap);
                    tasks.Add(RecordTask);
                }

                // すべてのタスクが完了するのを待つ
                await Task.WhenAll(tasks);

                bufferIndex++;
                if (bufferIndex >= BUFFERNUMBER) bufferIndex = 0;
            }
            catch (Exception e)
            {
                LogError(e);

                message = "ビデオフレーム処理中にエラーが発生しました";
                t = 0;
            }
        }

        // 停止の初期化
        private void CloseVideoSource()
        {
            try
            {
                if (!(videoSource == null))
                    if (videoSource.IsRunning)
                    {
                        videoSource.SignalToStop();
                        videoSource.WaitForStop();
                    }
                videoSource = null;
                pictureBox1.Image = null;
            } catch (Exception e){
                LogError(e);

                MessageBox.Show("終了時にエラーが発生しました。:" + e.ToString());            
            }
        }


        private async void Form1_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Formのロード開始");
            if (Properties.Settings.Default.F1size.Width == 0 || Properties.Settings.Default.F1size.Height == 0)
            {
                // 初回起動時にはここに来るので必要なら初期値を与えても良い。
                // 何も与えない場合には、デザイナーウインドウで指定されている大きさになる。
                this.Size = new System.Drawing.Size(orgWidth, orgHeight);
            }
            else
            {
                this.WindowState = Properties.Settings.Default.F1state;

                // もし前回終了時に最小化されていても、今回起動時にはNormal状態にしておく
                if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

                this.Location = Properties.Settings.Default.F1location;
                this.Size = new System.Drawing.Size(orgWidth, orgHeight);
            }

            this.initForm();

            loadFilter();

            this.timer1.Enabled = true;

            //古いログの削除
            DateTime cutoffDate = DateTime.Now.AddDays(-14);
            try
            {
                if (File.Exists(LogFile))
                {
                    await RemoveOldLogs(LogFile, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            
            LogEvents("[アプリケーションの開始] Entcaptureを起動しました");

        }

        private void initForm()
        {
            try
            {

                ctlLock(9);

                if (Directory.Exists(Properties.Settings.Default.thept))
                {
                    this.fileSystemWatcher1.Path = Properties.Settings.Default.thept;
                    this.fileSystemWatcher1.Filter = "thept.txt";
                    this.fileSystemWatcher1.EnableRaisingEvents = true;
                }
                else
                {
                    this.fileSystemWatcher1.EnableRaisingEvents = false;
                }

                LogFile = Properties.Settings.Default.tmpdir + "\\Entcapture.log";

                getThept();
                this.comboBoxID.Text = PtID;
                this.comboBoxName.Text = PtName;

                this.comboBoxTest.Items.Clear();
                this.comboBoxTest.Items.AddRange(Properties.Settings.Default.tests.Split(','));
                this.comboBoxTest.SelectedIndex = 0;

                this.button2.Text = CaptureButtonOn;

                int k = Properties.Settings.Default.snapkey % 100000;
                var ks = new int[3];
                if (k / 10000 > 0) ks[0] = (int)Keys.Control;
                if ((k % 10000) / 1000 > 0) ks[1] = (int)Keys.Shift;
                ks[2] = k % 1000;
                snapkey = (Keys)(ks[0]) | (Keys)(ks[1]) | (Keys)(ks[2]);
                charSnapKey = (Keys)(ks[2]);

                ks[0] = 0;
                ks[1] = 0;
                ks[2] = 0;
                k = Properties.Settings.Default.snapkey / 100000;
                if (k / 10000 > 0) ks[0] = (int)Keys.Control;
                if ((k % 10000) / 1000 > 0) ks[1] = (int)Keys.Shift;
                ks[2] = k % 1000;
                startkey = (Keys)(ks[0]) | (Keys)(ks[1]) | (Keys)(ks[2]);
                charStartKey = (Keys)(ks[2]);

                jpeg_quality = Properties.Settings.Default.jpegquality;

                this.getCameraInfo();

                //Shortcut buttons
                radioButton1.Text = Properties.Settings.Default.name1;
                radioButton2.Text = Properties.Settings.Default.name2;
                radioButton3.Text = Properties.Settings.Default.name3;
                radioButton4.Text = Properties.Settings.Default.name4;
                radioButton5.Text = Properties.Settings.Default.name5;
                radioButton6.Text = Properties.Settings.Default.name6;

                //comboDevices.SelectedIndex = comboDevices.Items.IndexOf(Properties.Settings.Default.device1);
                //            comboBoxResolution.SelectedIndex = comboBoxResolution.Items.IndexOf(Properties.Settings.Default.reso1);


                radioButton1.Checked = false;
                radioButton1.Checked = true;

                panelMenu.Visible = false;
                buttonMenu.Text = "<";
                this.Width = orgWidth;

                checkBoxNorec.Checked = Properties.Settings.Default.norec;

                
                drawStatus(0);

                if (!Directory.Exists(Properties.Settings.Default.outdir))
                {
                    MessageBox.Show("動画/静止画の保存先が設定されていません。設定画面で設定してください。");
                    this.toolStripButtonSettings.PerformClick();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);

                MessageBox.Show(ex.ToString());
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null)
            {
                // Form を閉じる際は映像データ取得をクローズ
                if (videoSource.IsRunning)
                {
                    this.CloseVideoSource();
                }
            }

            Properties.Settings.Default.F1state = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                // ウインドウステートがNormalな場合には位置（location）とサイズ（size）を記憶する。
                Properties.Settings.Default.F1location = this.Location;
                Properties.Settings.Default.F1size = this.Size;
            }
            else
            {
                // もし最小化（minimized）や最大化（maximized）の場合には、RestoreBoundsを記憶する。
                Properties.Settings.Default.F1location = this.RestoreBounds.Location;
                Properties.Settings.Default.F1size = this.RestoreBounds.Size;
            }

            // ここで設定を保存する
            Properties.Settings.Default.Save();

        }

        private void pictureBox2_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(Properties.Settings.Default.outdir);
        }

        private void pictureBox3_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(Properties.Settings.Default.outdir);
        }

        private void pictureBox4_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(Properties.Settings.Default.outdir);
        }

        private void pictureBox5_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(Properties.Settings.Default.outdir);
        }

        private void pictureBox6_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(Properties.Settings.Default.outdir);
        }

        private async void button4_Click(object sender, EventArgs e) // Stop
        {
            if (mode == 1)
            {
                playmode = 0;

                await Task.Run(() => SwapImage(pictureBox1, null));
                //if (this.pictureBox1.Image != null)
                //{
                //    this.pictureBox1.Image.Dispose();
                //    this.pictureBox1.Image = null;
                //}
                //if (imgorg != null) imgorg.Dispose();
                //if (img != null) img.Dispose();
                //if (imgout != null) imgout.Dispose();


                trackBar1.Value = 0;
                labelFPS.Text = "";
                labelTime.Text = "";

            }
        }

        private void fileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
        {
            bool successRead = false;

            for (int i = 0; i <= 5; i++)
            {
                try
                {
                    //MessageBox.Show("Thept changed!");
                    getThept();
                    // this.comboBoxID.Text = PtID;
                    // this.comboBoxName.Text = PtName;
                    this.comboBoxID.Text = PtID;
                    this.comboBoxName.Text = PtName;

                    //動画ロード中であれば停止しておく
                    this.button4.PerformClick();
                    successRead = true;
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }

            if (!successRead)
            {
                MessageBox.Show("RSBase患者情報ファイル(thept.txt)の読み込みに失敗したため、ID番号、名前をセットできませんでした。\r\n");
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void initDrawingParameters()
        {
            rois = new int[4];
            ol_p = new int[200, 3];
            ol_str = new string[200, 2];
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                setPreset(Properties.Settings.Default.device1, Properties.Settings.Default.reso1, Properties.Settings.Default.video1,
                    Properties.Settings.Default.test1, Properties.Settings.Default.roi1, Properties.Settings.Default.string1,
                    Properties.Settings.Default.font1, Properties.Settings.Default.FPS1);
            }

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                setPreset(Properties.Settings.Default.device2, Properties.Settings.Default.reso2, Properties.Settings.Default.video2,
                    Properties.Settings.Default.test2, Properties.Settings.Default.roi2, Properties.Settings.Default.string2,
                    Properties.Settings.Default.font2, Properties.Settings.Default.FPS2);
            }

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                setPreset(Properties.Settings.Default.device3, Properties.Settings.Default.reso3, Properties.Settings.Default.video3,
                    Properties.Settings.Default.test3, Properties.Settings.Default.roi3, Properties.Settings.Default.string3,
                    Properties.Settings.Default.font3, Properties.Settings.Default.FPS3);
            }

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                setPreset(Properties.Settings.Default.device4, Properties.Settings.Default.reso4, Properties.Settings.Default.video4,
                    Properties.Settings.Default.test4, Properties.Settings.Default.roi4, Properties.Settings.Default.string4,
                    Properties.Settings.Default.font4, Properties.Settings.Default.FPS4);
            }

        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                setPreset(Properties.Settings.Default.device5, Properties.Settings.Default.reso5, Properties.Settings.Default.video5,
                    Properties.Settings.Default.test5, Properties.Settings.Default.roi5, Properties.Settings.Default.string5,
                    Properties.Settings.Default.font5, Properties.Settings.Default.FPS5);
            }

        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
            {
                setPreset(Properties.Settings.Default.device6, Properties.Settings.Default.reso6, Properties.Settings.Default.video6,
                    Properties.Settings.Default.test6, Properties.Settings.Default.roi6, Properties.Settings.Default.string6,
                    Properties.Settings.Default.font6, Properties.Settings.Default.FPS6);
            }
        }


        private void setPreset(string presetDevice, int presetReso, bool presetVideo, string presetTest, string presetRoi, string presetString, string presetFont, int FPS)
        {
            try
            {
                initDrawingParameters();

                if (presetDevice.Length > 0 && toolStripComboDevices.Items.IndexOf(presetDevice) >= 0)
                {
                    toolStripComboDevices.SelectedIndex = toolStripComboDevices.Items.IndexOf(presetDevice);
                    toolStripComboBoxResolution.SelectedIndex = presetReso;
                }
                else
                {
                    MessageBox.Show("プリセットで指定されたデバイス" + presetDevice + "が見つかりません");
                    return;
                }

                checkBoxVideo.Checked = presetVideo;

                comboBoxTest.SelectedIndex = comboBoxTest.Items.IndexOf(presetTest);
                string[] r1 = presetRoi.Split(',');
                if (r1.Length >= 4)
                {
                    int i = 0;
                    foreach (string s in presetRoi.Split(','))
                    {
                        rois[i] = int.Parse(s);
                        i++;
                    }
                }
                string[] s1 = presetString.Split(',');
                if (s1.Length >= 5)
                {
                    for (int i = 0; i < s1.Length / 5; i++)
                    {
                        ol_p[i, 0] = int.Parse(s1[i * 5]); //x
                        ol_p[i, 1] = int.Parse(s1[i * 5 + 1]); //y
                        ol_p[i, 2] = int.Parse(s1[i * 5 + 2]); // font size
                        ol_str[i, 0] = s1[i * 5 + 3]; // font color
                        ol_str[i, 1] = s1[i * 5 + 4];
                    }
                }
                ol_font = presetFont;

                //FPS
                presetFPS = FPS;

                //フィルタをロード
                loadFilter();

                LogEvents("プリセットデバイス:" + presetDevice + "の設定をロードしました");
            }

            catch (Exception)
            {
                string mes = "設定で解像度が正しく設定されていません。デバイスの最大解像度に設定しました。";
                MessageBox.Show(mes);
            }

        }

        private void getThept()
        {
            string text = "";
            string ptfile = Properties.Settings.Default.thept + "\\thept.txt";

            if (File.Exists(ptfile))
            {
                using (StreamReader sr = new StreamReader(ptfile, Encoding.GetEncoding("Shift_JIS")))
                {
                    try
                    {
                        text = sr.ReadLine();
                        PtID = text.Split(',')[1];
                        PtName = text.Split(',')[2].Replace("\"", "");
                        dicPtHisory[PtID] = PtName;

                        if (comboBoxID.Items.IndexOf(PtID) < 0)
                        {
                            comboBoxID.Items.Add(PtID);
                            comboBoxName.Items.Add(PtName);
                        }
                    }
                    catch
                    {
                        PtID = "";
                        PtName = "";
                        message = "thept.txtを読み込めませんでした";
                        t = 0;
                    }
                }
                Debug.WriteLine(PtID, PtName);

            }
            else
            {
                message = "thept.txtが見つかりません";
                t = 0;
                PtID = "";
                PtName = "";
            }
        }

       

        private void button3_Click(object sender, EventArgs e)//再生・一時停止
        {
            if (mode == 1)
            {
                switch (playmode)
                {
                    case 0:
                        this.button1.PerformClick();
                        break;
                    case 3:
                        playmode = 4;
                        drawStatus(4);
                        //this.button3.Text = "再開";
                        break;
                    case 4: //一時停止中
                        playmode = 3;
                        drawStatus(3);
                        //this.button3.Text = "一時停止";
                        break;
                }
            }
        }

        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            //Play中ならpauseに
            if (mode == 1 && playmode == 3)
            {
                playmode = 4;
                drawStatus(4);
                //this.button3.Text = "再開";
            }

        }


        // フレームレートの取得
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (video_open)
            {
                int f = videoSource.FramesReceived;
                rec_count++;

                //最大録画時間超過
                if (rec_count > Properties.Settings.Default.timeout * 60 && !checkBoxNorec.Checked)
                {
                    this.button2.PerformClick();
                }

                if (rec_count >= rec_start)
                {
                    cvoutfps = f;
                }

                labelFPS.Text = f.ToString() + "fps";
                labelTime.Text = rec_count.ToString() + "sec";
            }


            if (keydown)
            {
                TimeSpan ts = sw.Elapsed;
                labelMessage.Text += ts.TotalMilliseconds.ToString();
                if (ts.TotalMilliseconds > 2000)
                {
                    //                  MessageBox.Show(ts.TotalMilliseconds.ToString());
                    this.button2.PerformClick();
                    sw.Stop();
                    sw.Reset();
                }
            }

            labelMessage.Text = message;
            t++;
            if (t > message_time)
            {
                message = "";
            }

        }

        private void toolStripComboDevices_TextChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("ToolComboDevice is called");
            updateResolution();

            loadFilter();
        }

        private void updateResolution()
        {
            try
            {
                int index = 0, res = 0, i = 0;
                connectVideo();

                toolStripComboBoxResolution.Items.Clear();
                foreach (VideoCapabilities capabilty in videoCapabilities)
                {
                    toolStripComboBoxResolution.Items.Add(string.Format("{0}x{1}:{2}fps", capabilty.FrameSize.Width, capabilty.FrameSize.Height, capabilty.AverageFrameRate));
                    if (capabilty.FrameSize.Width * capabilty.FrameSize.Height > res)
                    {
                        index = i;
                        res = capabilty.FrameSize.Width * capabilty.FrameSize.Height;
                    }
                    i++;
                }
                toolStripComboBoxResolution.SelectedIndex = index;

                this.CloseVideoSource();
            }
            catch (Exception)
            {
                string em = "デバイスから解像度情報が読み取れませんでした。デバイスの接続を確認してください。";
                MessageBox.Show(em);
            }

        }

        private void toolStripComboBoxResolution_TextChanged(object sender, EventArgs e)
        {
            connectVideo();

            videoSource.VideoResolution = videoCapabilities[toolStripComboBoxResolution.SelectedIndex];
            vwidth = videoCapabilities[toolStripComboBoxResolution.SelectedIndex].FrameSize.Width;
            vheight = videoCapabilities[toolStripComboBoxResolution.SelectedIndex].FrameSize.Height;
            Debug.WriteLine(string.Format("Frame size is set to: {0}x{1}", vwidth, vheight));

            this.CloseVideoSource();
        }

        private void toolStripButtonSettings_Click(object sender, EventArgs e)
        {
            //再生中なら止める
            this.button4.PerformClick();

            Form2 f = new Form2();
            //Form2を表示する
            f.StartPosition = FormStartPosition.CenterParent;

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                Debug.WriteLine("設定を再ロード");
                this.initForm();
            }
            //フォームが必要なくなったところで、Disposeを呼び出す
            f.Dispose();
        }

        private async void buttonExit_Click(object sender, EventArgs e)
        {
            try
            {
                this.CloseVideoSource();
                deleteOldFiles(Properties.Settings.Default.autodelete, Properties.Settings.Default.tmpdir);
            }
            catch (Exception ex)
            {
                LogError(ex);

                Debug.Write(ex.ToString());
            }
            finally
            {
                await LogEvents("[アプリケーションの終了] Entcaptureを終了しました");
                Application.Exit();
            }
        }

        private void buttonSnap_Click(object sender, EventArgs e)
        {
            SnapFlag = true;
        }

        private async Task SnapBmp(Bitmap bmp)  //Success :true
        {
            string error_message = "";

            lockBmp = true;

            using (Bitmap imgsnap = (Bitmap)bmp.Clone())
            {
                int w = imgsnap.Width;
                int h = imgsnap.Height;
                DateTime dt = new DateTime();
                Bitmap saveimg = null;
                string snapFile = "";

                using (var mat_back = new Mat(h, w, MatType.CV_8UC3, new Scalar(255, 255, 255)))
                {
                    //OpenCV 白のキャンバスを用意

                    try
                    {
                        //再生モードならファイル作成日時を基準とする
                        if (mode == 1) //再生
                        {
                            dt = System.IO.File.GetCreationTime(videofile);
                            dt = dt.AddMilliseconds(videoPosition);
                        }
                        else
                        {
                            dt = DateTime.Now;
                        }

                        string test = "", id = "";
                        this.Invoke((MethodInvoker)delegate ()
                        {
                            test = comboBoxTest.Text;
                            id = this.comboBoxID.Text;
                        });

                        string f = Properties.Settings.Default.outdir + "\\" + id + "~%%%%~" + dt.ToString("yyyy_MM_dd") + "~" + test + "~RSB.jpg";


                        for (int i = 1; i < 100; i++)
                        {
                            snapFile = f.Replace("%%%%", i.ToString("D2"));
                            if (!File.Exists(snapFile)) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        keydown = false;
                        sw.Stop();

                        LogError(ex);

                        MessageBox.Show("Snapファイルの作成時にエラーが発生しました。もう一度静止画取得を試みてください。");
                        lockBmp = false;
                        return;
                    }


                    try
                    {
                        //ROI
                        if (rois[2] > w || rois[3] > h)
                        {
                            error_message += "ROI切り取り領域が現在のビデオソースの範囲外です。プリセットの選択を確認してください。\r\n";
                            saveimg = imgsnap;
                        }
                        else if (rois.Sum() > 0)
                        {
                            using (Mat clippedMat = imgsnap.ToMat().Clone(new OpenCvSharp.Rect(rois[0], rois[1], rois[2] - rois[0], rois[3] - rois[1])))
                            {
                                var rect = new OpenCvSharp.Rect(rois[0], rois[1], rois[2] - rois[0], rois[3] - rois[1]);
                                //     Mat clippedMat = img.ToMat().Clone(new OpenCvSharp.Rect(rois[0], rois[1], rois[2] - rois[0], rois[3] - rois[1]));
                                mat_back[rect] = clippedMat;
                                saveimg = BitmapConverter.ToBitmap(mat_back);
                                clippedMat.Dispose();
                            }
                        }
                        else
                        {
                            saveimg = imgsnap;
                        }

                        //Text Overlay
                        if (ol_str[0, 0] != null)
                        {
                            using (Graphics g = Graphics.FromImage(saveimg))
                            {
                                for (int i = 0; i <= ol_str.Length; i++)
                                {
                                    if (ol_p[i, 0] > w || ol_p[i, 1] > h)
                                    {
                                        error_message += "文字列書き込み位置が、ビデオソースの範囲外です。プリセットの選択を確認してください。\r\n";
                                    }
                                    else if (ol_str[i, 0] == null)
                                    {
                                        break;
                                    }

                                    string strColor = ol_str[i, 0];
                                    Brush myBrush = new SolidBrush(System.Drawing.ColorTranslator.FromHtml("0x" + strColor));
                                    Font fnt = new Font(ol_font, ol_p[i, 2]);
                                    string s = ol_str[i, 1];

                                    s = s.Replace("$d", dt.ToString("yyyy/MM/dd"));
                                    s = s.Replace("$t", dt.ToString("HH:mm:ss"));
                                    s = s.Replace("$i", PtID);
                                    s = s.Replace("$n", PtName);

                                    g.DrawString(s, fnt, myBrush, ol_p[i, 0], ol_p[i, 1]);
                                    fnt.Dispose();
                                    myBrush.Dispose();
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        keydown = false;
                        sw.Stop();
                        
                        LogError(ex);

                        MessageBox.Show("画像切り出し、文字埋込みでエラーが発生しました: " + ex.ToString());
                        lockBmp = false;
                        return;
                    }

                    try
                    {
                        await Task.Run(() => SaveImage(saveimg, snapFile, jpeg_quality));
                    }
                    catch (Exception ex)
                    {
                        keydown = false;
                        sw.Stop();

                        LogError(ex);

                        MessageBox.Show("画像保存でエラーが発生しました");
                        lockBmp = false;
                        return;
                    }

                    try
                    {
                        if (pictureBox6.Image != null) await Task.Run(() => SwapImage(pictureBox7, (Image)pictureBox6.Image.Clone()));
                        if (pictureBox5.Image != null) await Task.Run(() => SwapImage(pictureBox6, (Image)pictureBox5.Image.Clone()));
                        if (pictureBox4.Image != null) await Task.Run(() => SwapImage(pictureBox5, (Image)pictureBox4.Image.Clone()));
                        if (pictureBox3.Image != null) await Task.Run(() => SwapImage(pictureBox4, (Image)pictureBox3.Image.Clone()));
                        if (pictureBox2.Image != null) await Task.Run(() => SwapImage(pictureBox3, (Image)pictureBox2.Image.Clone()));
                    }
                    catch (Exception ex)
                    {
                        keydown = false;
                        sw.Stop();

                        LogError(ex);

                        MessageBox.Show("PictureboxのDispose処理でエラーが発生しました");
                        lockBmp = false;
                        return;
                    }

                    try
                    {
                        await Task.Run(() => SwapImage(pictureBox2, (Image)saveimg.Clone()));
                        //pictureBox2.Image = (Image)saveimg.Clone();
                    }
                    catch (Exception ex)
                    {
                        keydown = false;
                        sw.Stop();

                        LogError(ex);

                        MessageBox.Show("Picturebox2の表示でエラーが発生しました");
                        lockBmp = false;
                        return;
                    }

                    try
                    {
                        saveimg.Dispose();
                        imgsnap.Dispose();
                    }
                    catch(Exception ex)
                    {
                        keydown = false;
                        sw.Stop();

                        LogError(ex);

                        MessageBox.Show("最終のDispose処理でエラーが発生しました");
                        lockBmp = false;
                        return;
                    }

                    System.Media.SystemSounds.Asterisk.Play();

                    message = "Snap is saved as " + snapFile;
                    t = 0;

                    LogEvents("静止画キャプチャしました。ファイル名:" + snapFile);
                }
            }
            lockBmp = false;
            
        }

        private void buttonFFmpeg_Click(object sender, EventArgs e)
        {
            //一時停止
            playmode = 4;

            //再生ロードされた動画変換
            using (Form4 f4 = new Form4())
            {
                f4.Owner = this;
                f4.StartPosition = FormStartPosition.CenterParent;

                f4.ShowDialog(this);

            }

        }

        private void toolStripButtonVersion_Click(object sender, EventArgs e)
        {
            Form3 f = new Form3();
            f.StartPosition = FormStartPosition.CenterParent;

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                Debug.WriteLine("Version closed");
            }
            //フォームが必要なくなったところで、Disposeを呼び出す
            f.Dispose();

        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            keydown = false;
            sw.Stop();
        }

        private void drawStatus(int mode) // ■、●等
        {
            //描画先とするImageオブジェクトを作成する
            Bitmap canvas = new Bitmap(pictureBoxState.Width, pictureBoxState.Height);
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g = Graphics.FromImage(canvas);
            switch (mode)
            {
                case 0: //stop
                    g.FillRectangle(System.Drawing.Brushes.Black, 8, 8, 20, 20);
                    break;
                case 1: //preview
                    g.FillEllipse(System.Drawing.Brushes.Yellow, 8, 8, 20, 20);
                    break;
                case 2: //Rec
                    g.FillEllipse(System.Drawing.Brushes.Red, 8, 8, 20, 20);
                    break;
                case 3:  //Play
                    System.Drawing.Point point1 = new System.Drawing.Point(8, 8);
                    System.Drawing.Point point2 = new System.Drawing.Point(8, 28);
                    System.Drawing.Point point3 = new System.Drawing.Point(28, 18);
                    System.Drawing.Point[] curvePoints = { point1, point2, point3 };

                    // Draw polygon to screen.
                    g.FillPolygon(System.Drawing.Brushes.Green, curvePoints);
                    break;
                case 4: //pause
                    g.FillRectangle(System.Drawing.Brushes.Black, 8, 8, 8, 20);
                    g.FillRectangle(System.Drawing.Brushes.Black, 20, 8, 8, 20);
                    break;
            }
            g.Dispose();
            //PictureBox1に表示する
            pictureBoxState.Image = canvas;

        }

        private void buttonEnd_Click(object sender, EventArgs e)
        {
            if (this.trackBar1.Value > frame_start)
            {
                frame_end = trackBar1.Value;
                drawBar(frame_start * 100 / trackBar1.Maximum, frame_end * 100 / trackBar1.Maximum, Brushes.Turquoise);
            }
            else
            {
                MessageBox.Show("終点が始点より前になっています。もう一度設定し直してください。");
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (this.trackBar1.Value < frame_end)
            {
                frame_start = trackBar1.Value;
                drawBar(frame_start * 100 / trackBar1.Maximum, frame_end * 100 / trackBar1.Maximum, Brushes.Turquoise);
            }
            else
            {
                MessageBox.Show("始点が終点より後になっています。もう一度設定し直してください。");
            }
        }



        private void pictureBox7_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(Properties.Settings.Default.outdir);
        }

        private void buttonMenu_Click(object sender, EventArgs e)
        {
            if (buttonMenu.Text == "<")
            {
                this.Width += 140;
                buttonMenu.Text = ">";
                panelMenu.Visible = true;
            }
            else
            {
                this.Width -= 140;
                buttonMenu.Text = "<";
                checkBoxWB.Checked = false;
                panelMenu.Visible = false;
            }

        }

        private void toolStripComboDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateResolution();
        }

        /// <summary>
        /// 指定された画像ファイルを、品質を指定してJPEGで保存する
        /// </summary>
        /// <param name="fileName">変換する画像ファイル名</param>
        /// <param name="quality">品質</param>
        private void SaveImage(Bitmap bmp, string fileName, int quality)
        {
            try
            {
                //EncoderParameterオブジェクトを1つ格納できる
                //EncoderParametersクラスの新しいインスタンスを初期化
                //ここでは品質のみ指定するため1つだけ用意する
                System.Drawing.Imaging.EncoderParameters eps =
                    new System.Drawing.Imaging.EncoderParameters(1);
                //品質を指定
                System.Drawing.Imaging.EncoderParameter ep =
                    new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality, (long)quality);
                //EncoderParametersにセットする
                eps.Param[0] = ep;

                //イメージエンコーダに関する情報を取得する
                System.Drawing.Imaging.ImageCodecInfo ici = GetEncoderInfo("image/jpeg");

                //保存する
                bmp.Save(fileName, ici, eps);

                eps.Dispose();
            }
            catch (Exception ex)
            {
                LogError(ex);

                MessageBox.Show("Jpegファイルの保存に失敗しました");

            }
        }

        //MimeTypeで指定されたImageCodecInfoを探して返す
        private static System.Drawing.Imaging.ImageCodecInfo
            GetEncoderInfo(string mineType)
        {
            //GDI+ に組み込まれたイメージ エンコーダに関する情報をすべて取得
            System.Drawing.Imaging.ImageCodecInfo[] encs =
                System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            //指定されたMimeTypeを探して見つかれば返す
            foreach (System.Drawing.Imaging.ImageCodecInfo enc in encs)
            {
                if (enc.MimeType == mineType)
                {
                    return enc;
                }
            }
            return null;
        }

        //ImageFormatで指定されたImageCodecInfoを探して返す
        private static System.Drawing.Imaging.ImageCodecInfo
            GetEncoderInfo(System.Drawing.Imaging.ImageFormat f)
        {
            System.Drawing.Imaging.ImageCodecInfo[] encs =
                System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            foreach (System.Drawing.Imaging.ImageCodecInfo enc in encs)
            {
                if (enc.FormatID == f.Guid)
                {
                    return enc;
                }
            }
            return null;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (checkBoxWB.Checked)
                {
                    MU = new System.Drawing.Point(0, 0);
                    // 描画フラグON
                    view = true;

                    // Mouseを押した座標を記録
                    MD = Form2.ConvertCoordinates(e.Location, this.pictureBox1);

                    if (MD.X < 0 || MD.Y < 0)
                    {
                        MessageBox.Show("画像範囲外を選択しています。画像内を指定してください。");
                        view = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("キャプチャ中か動画再生中のみ有効な操作です");
            }
        }

        private void trackBarGamma_Scroll(object sender, EventArgs e)
        {
            filterGamma[1] = trackBarGamma.Value;
            textBoxGamma.Text = ((float)filterGamma[1] / 10).ToString("f1");

            filterGamma[0] = 0;
            if (filterGamma[1] != 10) filterGamma[0] = 1;

        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && checkBoxWB.Checked && view)
            {
                MC = Form2.ConvertCoordinates(e.Location, pictureBox1);

            }
        }

        private void buttonGammaReset_Click(object sender, EventArgs e)
        {
            resetGamma();
        }

        private void resetGamma()
        {
            trackBarGamma.Value = 10;
            textBoxGamma.Text = "1";
            filterGamma[0] = 0;
            filterGamma[1] = 10;
        }

        private void resetWB()
        {
            filterWB[1] = 255;
            filterWB[2] = 255;
            filterWB[3] = 255;
            numericUpDownR.Value = 255;
            numericUpDownG.Value = 255;
            numericUpDownB.Value = 255;
        }

        private void buttonFilterAdd_Click(object sender, EventArgs e)
        {
            string strFilters = "";
            string devicename = toolStripComboDevices.Text;

            if (toolStripComboDevices.SelectedIndex >= 0 && checkBoxWB.Checked)
            {
                //Listboxから読込
                foreach (string filter in listBoxFilters.Items)
                {
                    if (filter.IndexOf(devicename) >= 0) //上書き確認
                    {
                        DialogResult re = MessageBox.Show("既存のフィルター設定を上書きしますか？", "上書きの確認", MessageBoxButtons.OKCancel);
                        if (re == DialogResult.Cancel) return;
                    }
                    else if (filter.Length > 0)
                    {
                        strFilters += filter + ";";
                    }
                }
                strFilters += devicename + ","
                           + numericUpDownR.Value.ToString() + ","
                           + numericUpDownG.Value.ToString() + ","
                           + numericUpDownB.Value.ToString() + ","
                           + filterGamma[1].ToString() + ","
                           + filterFlip.ToString();

                strFilters.Trim(';');

                LogEvents("Filter Add:" + strFilters);

                Properties.Settings.Default.filter = strFilters;
                Properties.Settings.Default.Save();

                loadFilter();
            }
            else
            {
                MessageBox.Show("ビデオデバイスが選択されていないか、フィルタ適用モードになっていません");
            }

        }

        private void loadFilter()
        {
            string[] filterlist = Properties.Settings.Default.filter.Split(';');
            filterWB[0] = 0;

            for (int i = 0; i < filterlist.Length; i++)
            {
                string[] strF = filterlist[i].Split(',');
               
                if (strF[0] == toolStripComboDevices.Text) //保存フィルタの中に現在のデバイス名があった場合
                {
                    LogEvents("フィルタを読み込みました：" + filterlist[i]);
                    if (strF.Length >= 5)
                    {
                        filterWB[1] = int.Parse(strF[1]);
                        filterWB[2] = int.Parse(strF[2]);
                        filterWB[3] = int.Parse(strF[3]);
                        filterGamma[1] = int.Parse(strF[4]);
                        filterWB[0] = 1;

                        // Flip added
                        if (strF.Length >= 6)
                        {
                            filterFlip = int.Parse(strF[5]);
                        }

                        //UI表示
                        checkBoxWB.Checked = (filterWB[0] == 1) ? true : false;
                        numericUpDownR.Value = filterWB[1];
                        numericUpDownG.Value = filterWB[2];
                        numericUpDownB.Value = filterWB[3];
                        trackBarGamma.Value = filterGamma[1];
                        textBoxGamma.Text = ((float)filterGamma[1] / 10).ToString("f1");
                        checkBoxFlipY.Checked = (filterFlip == 0 || filterFlip == -1) ? true : false;
                        checkBoxFlipX.Checked = (filterFlip == 1 || filterFlip == -1) ? true : false;

                    }
                } else
                {
                    checkBoxWB.Checked = false;
                }
            }

            listBoxFilters.Items.Clear();
            foreach (string filter in filterlist)
            {
                if (filter.Length > 0) listBoxFilters.Items.Add(filter);
            }
        }

        private void buttonFilterDelete_Click(object sender, EventArgs e)
        {
            string strFilters = "";

            for (int i = 0; i < listBoxFilters.Items.Count; i++)
            {
                if (i != listBoxFilters.SelectedIndex && listBoxFilters.Items[i].ToString().Length > 0)
                {
                    strFilters += listBoxFilters.Items[i] + ";";
                }
            }

            strFilters.Trim(';');

            Debug.WriteLine("Filter deleted: " + strFilters);

            Properties.Settings.Default.filter = strFilters;
            Properties.Settings.Default.Save();

            loadFilter();
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (pictureBox1.Image != null) // && mode == 1
                {
                    if (formDisp.IsDisposed)
                    {
                        formDisp = new Form5();
                        formDisp.formMain = this;
                    }

                    //formDisp.Size = new System.Drawing.Size(vwidth + 20, vheight + 30);
                    formDisp.pictureBox1.Size = new System.Drawing.Size(vwidth, vheight);


                    formDisp.Show(this);
                    if(playmode == 4) PauseReloadFlag = true; //一時停止時リロード
                }
                else
                {
                    MessageBox.Show("拡大表示は動画再生・画像表示時のみ有効です");
                }
            }
            catch (Exception ex)
            {
                formDisp.Close();

                LogError(ex);

                MessageBox.Show(ex.ToString());
            }
        }

        private void checkBoxNorec_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNorec.Checked)
            {
                checkBoxVideo.Checked = false;
                checkBoxVideo.Enabled = false;
            }
            else
            {
                checkBoxVideo.Enabled = true;
            }
        }



        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && checkBoxWB.Checked && view)
            {
                MU = Form2.ConvertCoordinates(e.Location, pictureBox1);
                // 描画フラグOFF
                view = false;

                if (MU.X > 0 && MU.Y > 0)
                {
                    //filterWB[0] = 1;
                    MouseUpFlag = true;
                    //getRGBaverage(imgorg, Math.Min(MD.X, MU.X), Math.Min(MD.Y, MU.Y), Math.Max(MD.X, MU.X), Math.Max(MD.Y, MU.Y));
                }
                else
                {
                    MessageBox.Show("描画範囲が適切ではありません。画像内を指定してください。");
                }
            }
        }

       
       

        private void comboBoxID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBoxID.SelectedIndex >= 0) comboBoxName.SelectedIndex = comboBoxID.SelectedIndex;
        }

        private void comboBoxName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxName.SelectedIndex >= 0) comboBoxID.SelectedIndex = comboBoxName.SelectedIndex;
        }

       
        private void comboBoxID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) {
                comboBoxIDUpdate();
            }
        }

       

        private void comboBoxID_Leave(object sender, EventArgs e)
        {
            comboBoxIDUpdate();
        }

        private void comboBoxNameUpdate()
        {
            int idx = comboBoxID.Items.IndexOf(comboBoxID.Text);
            if (idx < 0)
            {
                comboBoxName.Text = "";
            }
            else
            {
                comboBoxName.SelectedIndex = idx;
            }

            PtID = comboBoxID.Text;
            PtName = comboBoxName.Text;
        }

        private void comboBoxIDUpdate()
        {
            //int idx = comboBoxName.Items.IndexOf(comboBoxName.Text);
            //if (idx < 0) comboBoxID.Text = "";
            int idx = comboBoxID.FindStringExact(comboBoxID.Text);

            if(idx < 0) //新規手入力
            {
                comboBoxID.Items.Add(comboBoxID.Text);
                comboBoxName.Text = getNameFromPG(comboBoxID.Text.ToString());
                comboBoxName.Items.Add(comboBoxName.Text);

                comboBoxID.SelectedIndex = comboBoxID.Items.Count - 1;
            }
            else
            {
                comboBoxID.SelectedIndex = idx;
            }

            //comboBoxName.SelectedIndex = comboBoxID.SelectedIndex;

            PtID = comboBoxID.Text;
            PtName = comboBoxName.Text;
        }

        private string getNameFromPG(string id)
        {
            string pgserver = Properties.Settings.Default.pgaddress;
            string kanja_kanji = "";
            
            if (pgserver.Length > 6)
            {
                //接続文字列
                string conn_str = "Server=" + pgserver + @";Port=5432;User ID=postgres;Database=gazouDB;Password=medicalin;Enlist=true;Timeout=2";

                using (NpgsqlConnection conn = new NpgsqlConnection(conn_str))
                {
                    //PostgreSQLへ接続
                    try
                    {
                        conn.Open();
                        if (conn.State == System.Data.ConnectionState.Open)
                        {
                            int intid;
                            if (int.TryParse(id, out intid))
                            {
                                using (var command = new NpgsqlCommand(@"SELECT * FROM name_tbl WHERE id = " + intid.ToString(), conn))
                                {
                                    using (var dataReader = command.ExecuteReader())
                                    {
                                        while (dataReader.Read())
                                        {
                                            kanja_kanji = dataReader["kanja_kanji"].ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        conn.Close();
                        LogError(ex);

                        MessageBox.Show("PostgreSQLサーバーへの接続に失敗しました。PostgreSQLのサーバーアドレスを確認してください。\n");
                    }
                }
            }
            return kanja_kanji;
        }

        private void comboBoxName_Leave(object sender, EventArgs e)
        {
            PtName = comboBoxName.Text;
            comboBoxName.SelectedIndex = comboBoxName.Items.IndexOf(PtName);

            if (comboBoxName.SelectedIndex == -1 && comboBoxID.SelectedIndex >= 0)
            {
                comboBoxName.Items.RemoveAt(comboBoxID.SelectedIndex);
                comboBoxName.Items.Insert(comboBoxID.SelectedIndex, PtName);
                comboBoxName.SelectedIndex = comboBoxID.SelectedIndex;
            }
        }

        private void checkBoxWB_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxWB.Checked)
            {
                setFilterButtons(true);
            }
            else
            {
                setFilterButtons(false);
            }
            
        }

        private void setFilterButtons(bool buttonState)
        {
            //UI表示
            numericUpDownR.Value = filterWB[1];
            numericUpDownG.Value = filterWB[2];
            numericUpDownB.Value = filterWB[3];
            trackBarGamma.Value = filterGamma[1];
            textBoxGamma.Text = ((float)filterGamma[1] / 10).ToString("f1");
            checkBoxFlipY.Checked = (filterFlip == 0 || filterFlip == -1) ? true : false;
            checkBoxFlipX.Checked = (filterFlip == 1 || filterFlip == -1) ? true : false;


            labelR.Enabled = buttonState;
            labelG.Enabled = buttonState;
            labelB.Enabled = buttonState;
            numericUpDownR.Enabled = buttonState;
            numericUpDownG.Enabled = buttonState;
            numericUpDownB.Enabled = buttonState;

            trackBarGamma.Enabled = buttonState;
            textBoxGamma.Enabled = buttonState;
            buttonGammaReset.Enabled = buttonState;

            checkBoxFlipX.Enabled = buttonState;
            checkBoxFlipY.Enabled = buttonState;
        }
      
        private void ctlLock(int mod) // mode: 0 キャプチャ中、1:再生中、 待機状態
        {
            switch (mod)
            {
                case 0: //キャプチャ中
                    radioButton1.Enabled = false;
                    radioButton2.Enabled = false;
                    radioButton3.Enabled = false;
                    radioButton4.Enabled = false;
                    radioButton5.Enabled = false;
                    radioButton6.Enabled = false;

                    toolStripComboDevices.Enabled = false;
                    toolStripComboBoxResolution.Enabled = false;
                    checkBoxVideo.Enabled = false;
                    checkBoxNorec.Enabled = false;
                    this.buttonSnap.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    trackBar1.Enabled = false;
                    button1.Enabled = false;
                    button2.Enabled = true;
                    toolStripButtonSettings.Enabled = false;
                    buttonFFmpeg.Enabled = false;
                    buttonExit.Enabled = false;
                    buttonStart.Enabled = false;
                    buttonEnd.Enabled = false;
                    break;
                case 1: //再生中
                    radioButton1.Enabled = true;
                    radioButton2.Enabled = true;
                    radioButton3.Enabled = true;
                    radioButton4.Enabled = true;
                    radioButton5.Enabled = true;
                    radioButton6.Enabled = true;

                    toolStripComboDevices.Enabled = true;
                    buttonSnap.Enabled = true;
                    toolStripComboBoxResolution.Enabled = true;
                    checkBoxVideo.Enabled = true;
                    checkBoxNorec.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    trackBar1.Enabled = true;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    toolStripButtonSettings.Enabled = true;
                    buttonFFmpeg.Enabled = true;
                    buttonExit.Enabled = true;
                    buttonStart.Enabled = true;
                    buttonEnd.Enabled = true;
                    break;
                default: //初期状態（待機中）
                    radioButton1.Enabled = true;
                    radioButton2.Enabled = true;
                    radioButton3.Enabled = true;
                    radioButton4.Enabled = true;
                    radioButton5.Enabled = true;
                    radioButton6.Enabled = true;

                    toolStripComboDevices.Enabled = true;
                    buttonSnap.Enabled = false;
                    toolStripComboBoxResolution.Enabled = true;
                    checkBoxVideo.Enabled = true;
                    checkBoxNorec.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    trackBar1.Enabled = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    toolStripButtonSettings.Enabled = true;
                    buttonFFmpeg.Enabled = false;
                    buttonExit.Enabled = true;
                    buttonStart.Enabled = false;
                    buttonEnd.Enabled = false;
                    break;
            }
        }

        private void toolStripOpenLogFile_Click(object sender, EventArgs e)
        {
            // ファイルが存在するかどうかをチェックします
            if (File.Exists(LogFile))
            {
                try
                {
                    // ファイルを関連付けられたアプリケーションで開きます
                    Process.Start(LogFile);
                }
                catch (Exception)
                {
                    MessageBox.Show("ログファイルを開くのに失敗しました");
                }
            }

            else
            {
                // ファイルが存在しない場合はエラーメッセージを出力します
                MessageBox.Show("ログファイルが見つかりませんでした");
            }
        }

        private void deleteOldFiles(int days, string foldername)
        {
            if (days > 0)
            {
                try
                {
                    LogEvents("古い一時ファイルを削除します");

                    DirectoryInfo dyInfo = new DirectoryInfo(foldername);
                    // フォルダのファイルを取得
                    var target = DateTime.Today.AddDays(-days);
                    foreach (FileInfo fInfo in dyInfo.GetFiles())
                    {
                        // 日付の比較
                        if (fInfo.LastWriteTime < target && (fInfo.Extension == ".mp4" || fInfo.Extension == ".avi"))
                        {
                            LogEvents(string.Format("Deleted old file :{0}", fInfo.Name));
                            fInfo.Delete();
                        }
                    }
                }
                catch(Exception ex) {
                    LogError(ex);
                }
            }
        }

        public string GetMovieDurationText(string strMovPath)
        {
            FileInfo fi = new FileInfo(strMovPath);
            string strFileName = fi.FullName;
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(shellAppType);
            Folder objFolder = shell.NameSpace(Path.GetDirectoryName(strFileName));
            FolderItem folderItem = objFolder.ParseName(Path.GetFileName(strFileName));
            string strDuration = objFolder.GetDetailsOf(folderItem, 27);
            return strDuration;
        }

        private void toolStripReloadButton_Click(object sender, EventArgs e)
        {
            this.initForm();

            loadFilter();

            LogEvents("デバイスを再スキャンしました");
        }

        private void checkBoxFlipY_CheckedChanged(object sender, EventArgs e)
        {
            calcFilterFlip();
        }

        private void calcFilterFlip()
        {
            if (checkBoxFlipY.Checked && checkBoxFlipX.Checked)
            {
                filterFlip = -1;
            }
            else if (checkBoxFlipY.Checked)
            {
                filterFlip = 0;
            }
            else if (checkBoxFlipX.Checked)
            {
                filterFlip = 1;
            }
            else
            {
                filterFlip = 10;
            }
        }

        private System.Drawing.RotateFlipType  OpenCVFilterMode2RotationFlipType(int filterFlip)
        {
            switch (filterFlip)
            {
                case 0:
                    return RotateFlipType.RotateNoneFlipX;
                case 1:
                    return RotateFlipType.RotateNoneFlipY;
                case -1: 
                    return RotateFlipType.RotateNoneFlipXY;
                default:
                    return RotateFlipType.RotateNoneFlipNone;
            }
        }

        private void checkBoxFlipX_CheckedChanged(object sender, EventArgs e)
        {
            calcFilterFlip();
        }

        private async Task EncodeMovie(string inputFile, string outDir, int sizeMB, string codec)
        {
            semaphore.WaitOne(); // semaphore から排他的なアクセス権を取得します

            try
            {
                LogEvents("エンコード処理を開始します");
                //Stop camera
                this.CloseVideoSource();

                if (playmode > 0) playmode = 0;

                //ファイルサイズ長さ取得、ビットレート計算
                string strDuration = GetMovieDurationText(inputFile);
                // TimeSpanに変換
                TimeSpan ts = TimeSpan.Parse(strDuration);
                double bitrate = sizeMB * 1000 * 8 / ts.TotalSeconds;
                string arg, outFile;

                outFile = outDir + "\\" + System.IO.Path.GetFileNameWithoutExtension(inputFile);

                if (codec == "libxvid")
                {
                    outFile += ".avi";
                }else if (codec == "")
                {
                    outFile += System.IO.Path.GetExtension(inputFile);
                }else
                {
                    outFile += ".mp4";
                }

                if (codec == "")
                {
                    arg = string.Format(" -i \"{0}\" -filter:v yadif -b:v {1}k  \"{2}\"", inputFile, (int)bitrate, outFile);
                }
                else
                {
                    arg = string.Format(" -i \"{0}\" -filter:v yadif -b:v {1}k -c:v {3} \"{2}\"", inputFile, (int)bitrate, outFile, codec);
                }

                message = arg;
                t = 0;

       
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = arg,
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    ErrorDialog = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.EnableRaisingEvents = true;
                    process.SynchronizingObject = this;
                    var tcs = new TaskCompletionSource<object>();

                    process.Exited += (sender, args) =>
                    {
                        tcs.SetResult(null);
                    };

                    LogEvents("ffmpegのプロセスを開始します");
                    process.Start();
                    await tcs.Task; // プロセスの終了を非同期的に待機します
                }

                LogEvents("エンコード処理が終了しました");
                //Rsb_reload();

            }
            catch (Exception ex)
            {
                LogError(ex);

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                semaphore.Release(); // semaphore の排他的なアクセス権を解放します
            }
        }

        private  async Task Rsb_reload()
        {
            try
            {
                //RSB filing
                if (Properties.Settings.Default.autofiling && System.IO.Directory.EnumerateFileSystemEntries(Properties.Settings.Default.outdir, "*~*~*~*~RSB.*").Any())
                {
                    await LogEvents("RSBファイリングを開始しました");

                    string target = "";

                    if (Properties.Settings.Default.rsbcamera)
                    {
                        target = "http://localhost/~rsn/RSB_movie.cgi?sentaku";
                    }
                    else
                    {
                        target = Properties.Settings.Default.rsbaseurl;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = target,
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);
                    await LogEvents("RSBファイリングを完了しました");
                }
            }
            catch(Exception ex)
            {
                LogError(ex);
                MessageBox.Show("RSBaseファイリング時にエラーが発生しました");
            }

        }

        private void drawBar(int start, int end, Brush br)
        {
            Bitmap canvas = new Bitmap(pictureBoxBar.Width, pictureBoxBar.Height);
            Graphics g = Graphics.FromImage(canvas);
            int lmargin = 12;
            int rmargin = 12;
            int s = lmargin + ((pictureBoxBar.Width - lmargin - rmargin) * start / 100);
            int e = lmargin + ((pictureBoxBar.Width - lmargin - rmargin) * end / 100);
            g.FillRectangle(br, s, 0, e - s, pictureBoxBar.Height);
            pictureBoxBar.Image = canvas;

            g.Dispose();
        }

        private void getRGBaverage(Bitmap b, int x1, int y1, int x2, int y2)
        {
            try
            {
                int w = b.Width, h = b.Height;

                // Bitmapをロックし、BitmapDataを取得する
                BitmapData srcBitmapData = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, b.PixelFormat);
                //BitmapData srcBitmapData = b.LockBits(area, ImageLockMode.ReadOnly, b.PixelFormat);

                // 変換対象のカラー画像の情報をバイト列へ書き出す
                byte[] srcPixels = new byte[srcBitmapData.Stride * h];
                System.Runtime.InteropServices.Marshal.Copy(srcBitmapData.Scan0, srcPixels, 0, srcPixels.Length);

                b.UnlockBits(srcBitmapData);
                //b.Dispose();

                int R = 0, G = 0, B = 0, n = 0;
                for (int i = y1; i < y2; i++)
                {
                    for (int j = x1; j < x2; j++)
                    {
                        B += srcPixels[(w * i + j) * 3];
                        G += srcPixels[(w * i + j) * 3 + 1];
                        R += srcPixels[(w * i + j) * 3 + 2];
                        n++;
                    }
                }

                if (n == 0)
                {
                    B = 255;
                    G = 255;
                    R = 255;
                }
                else
                {
                    B = B / n;
                    G = G / n;
                    R = R / n;
                }

                // UIスレッドにマーシャリングしてテキストボックスに書き込み
                numericUpDownB.Invoke((MethodInvoker)delegate
                {
                    numericUpDownB.Value = B;
                });
                numericUpDownG.Invoke((MethodInvoker)delegate
                {
                    numericUpDownG.Value = G;
                });
                numericUpDownR.Invoke((MethodInvoker)delegate
                {
                    numericUpDownR.Value = R;
                });


                int targetAve = Math.Max(Math.Max(B, G), R);
                filterWB[1] = 255 * R / targetAve;
                filterWB[2] = 255 * G / targetAve;
                filterWB[3] = 255 * B / targetAve;
            }
            catch (Exception ex)
            {
                LogError(ex);

                MessageBox.Show(ex.ToString());
            }
        }

        private async Task ApplyWBasync(Bitmap originalBitmap, int filR, int filG, int filB)
        {
            if (originalBitmap != null)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        // Clone original bitmap to avoid modifying the original
                        //Bitmap adjustedBitmap = (Bitmap)originalBitmap.Clone();

                        // Apply LevelsLinear filter for white balance adjustment
                        LevelsLinear filter = new LevelsLinear();

                        // set ranges
                        filter.InRed = new IntRange(0, filR);
                        filter.InGreen = new IntRange(0, filG);
                        filter.InBlue = new IntRange(0, filB);
                        // apply the filter
                        filter.ApplyInPlace(originalBitmap);

                        //return originalBitmap;
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);

                        MessageBox.Show("Error in ApplyWBasync:" + ex.ToString());
                    }
                });
            }
        }

        private async Task ApplyGammaAsync(Bitmap originalBitmap, float gamma)
        {
            if (originalBitmap != null)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        GammaCorrection filter = new GammaCorrection(gamma);
                        filter.ApplyInPlace(originalBitmap);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);

                        MessageBox.Show("Error in ApplyGammaAsync:" + ex.ToString());
                    }
                });
            }
        }

        private void DrawRegion(System.Drawing.Point start, System.Drawing.Point end, Bitmap bmp)
        {
            try
            {
                Pen blackPen = new Pen(Color.Red);

                Graphics g = Graphics.FromImage(bmp);

                int x1 = Math.Min(start.X, end.X);
                int x2 = Math.Max(start.X, end.X);
                int y1 = Math.Min(start.Y, end.Y);
                int y2 = Math.Max(start.Y, end.Y);

                // 描画する線を点線に設定
                blackPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                // 領域を描画
                g.DrawRectangle(blackPen, x1, y1, x2 - x1, y2 - y1);

                g.Dispose();
            }
            catch (Exception ex)
            {
                LogError(ex);

                MessageBox.Show("DrawResionでエラーが発生しました");
            }
        }

        private Bitmap CopyBitmap(Bitmap srcbmp)
        {
            BitmapData imgData = srcbmp.LockBits(new Rectangle(0, 0, srcbmp.Width, srcbmp.Height), ImageLockMode.ReadOnly, srcbmp.PixelFormat);
            using (Bitmap copyImg = new Bitmap(srcbmp.Width, srcbmp.Height, imgData.Stride, srcbmp.PixelFormat, imgData.Scan0))
            {
                srcbmp.UnlockBits(imgData);
                return copyImg;
            }
        }

        private void SwapImage(PictureBox pictureBox, Image newImage)
        {
            if (pictureBox == null)
            {
                throw new ArgumentNullException("pictureBox");
            }
            try
            {
                var oldImg = pictureBox.Image;
                pictureBox.Image = newImage;
                oldImg?.Dispose();
            } 
            catch (Exception ex)
            {
                LogError(ex);
                //MessageBox.Show(ex.ToString());
                message = "描画中にエラーが発生しました";
                t = 0;
            }
        }

        private void KeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            //MessageBox.Show("Keyboard down event:" + e.KeyData.ToString());
            Keys currentKey = e.KeyData;

            chechKeyStatus(currentKey, true);

            if (pressedCtrl) currentKey |= Keys.Control;
            if(pressedShift) currentKey |= Keys.Shift;

            Debug.Write("KeyDown:" + e.KeyCode);
            Debug.Write(" pressedCtrl:" + pressedCtrl.ToString());
            Debug.WriteLine(" pressedShift:" + pressedShift.ToString());
           
            //MessageBox.Show("Keyboard down event:" + currentKey.ToString());

            if (currentKey == snapkey)
            {
                if (!keydown)
                {
                    keydown = true;

                    message = "Foot switch is pushed";
                    t = message_time - 1;
                    sw.Restart();

                    this.buttonSnap.PerformClick();
                    this.ActiveControl = this.buttonSnap;
                    //this.buttonSnap.Focus();
                }
            } else if(currentKey == startkey)
            {
                if (!keydown)
                {
                    keydown = true;

                    message = "Start key is received";
                    t = message_time - 1;
                    sw.Restart();

                    this.button2.PerformClick();
                    this.ActiveControl = this.button2;
                }
            }
        }

        private void KeyboardHook_KeyUp(object sender, KeyEventArgs e)
        {
            //Debug.WriteLine("KeyUp:" +  e.KeyData);

            chechKeyStatus(e.KeyData, false);

            keydown = false;
            sw.Stop();
        }

        private void chechKeyStatus(Keys inputKey, bool Down)
        {
            if(inputKey == Keys.LControlKey || inputKey == Keys.RControlKey || inputKey == Keys.Control ) pressedCtrl  = Down;
            if(inputKey == Keys.LShiftKey || inputKey == Keys.RShiftKey || inputKey == Keys.ShiftKey)   pressedShift = Down;
        }

        public async void LogError(Exception ex)
        {
            try
            {
                // エラーログを書き込む
                using (StreamWriter writer = new StreamWriter(LogFile, append: true))
                {
                    await writer.WriteLineAsync($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}-[Error]:{ex.Message}");
                    await writer.WriteLineAsync($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}-[StackTrace]: {ex.StackTrace}");
                    //await writer.WriteLineAsync(); // 空行を追加して区切ります
                }

                Console.WriteLine("エラーが発生しました。ログファイルに書き込まれました。");
            }
            catch (Exception logEx)
            {
                // ログの書き込み中にエラーが発生した場合の処理
                Console.WriteLine("ログの書き込み中にエラーが発生しました。");
                Console.WriteLine($"ログエラー: {logEx.Message}");
            }
        }

        public async Task LogEvents(string logMessage)
        {
            try
            {
                // ログを書き込む
                using (StreamWriter writer = new StreamWriter(LogFile, append: true))
                {
                    await writer.WriteLineAsync($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}-[Events]:{logMessage}");
                }
            }
            catch (Exception logEx)
            {
                // ログの書き込み中にエラーが発生した場合の処理
                Console.WriteLine("ログの書き込み中にエラーが発生しました。");
                Console.WriteLine($"ログエラー: {logEx.Message}");
            }
        }

        private async Task RemoveOldLogs(string filePath, DateTime cutoffDate)
        {
            string tempFilePath = Properties.Settings.Default.tmpdir + "\\temp.log";

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                using (StreamWriter writer = new StreamWriter(tempFilePath))
                {
                    string line;
                    bool pastCutoffDate = false;

                    // ログファイルを上から順に非同期で処理し、日付の古い順に並んでいることを利用する
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        // 行が空かどうかをチェックします
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue; // 空行の場合はスキップします
                        }

                        // カットオフ日よりも新しい行を見つけた後の行はすべて書き込みます
                        if (pastCutoffDate)
                        {
                            await writer.WriteLineAsync(line);
                        }
                        else
                        {
                            // 行の長さが19字未満の場合はスキップします
                            if (line.Length < 19)
                            {
                                continue; // 行が短すぎる場合はスキップします
                            }

                            // ログの日付部分を抽出してDateTimeに変換します
                            string[] parts = line.Split('-');

                            // スプリットできた場合は、1つ目の要素を取得します
                            string datePart = parts.Length > 0 ? parts[0] : null;

                            if (datePart != null)
                            {
                                if (!DateTime.TryParseExact(datePart, "yyyy/MM/dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime logDate))
                                {
                                    continue; // 日付がパースできない場合はスキップします
                                }

                                // ログの日付が指定されたカットオフ日よりも新しい場合
                                else if (logDate >= cutoffDate)
                                {
                                    pastCutoffDate = true; // カットオフ日よりも新しい行を見つけたのでフラグを立てる
                                    await writer.WriteLineAsync(line);
                                }
                            }

                        }
                    }

                }

                
                // 一時ファイルからログファイルに非同期でコピーします
                await CopyFileAsync(tempFilePath, filePath);
            }
            finally
            {
                // 一時ファイルを削除します
                File.Delete(tempFilePath);
            }
        }

        private async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using (FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            using (FileStream destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
    }
}


