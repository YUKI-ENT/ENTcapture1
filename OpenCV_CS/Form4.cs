using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ENTcapture
{
    public partial class Form4 : Form
    {
        private string inputFile, outputFile;
        private long targetSize, orgSize;
        TimeSpan videoLength;
        private int totalFrames, startFlame, endFrame;

        public Form4()
        {
            InitializeComponent();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            inputFile = ((Form1)this.Owner).videofile;
            this.labelFilename.Text = inputFile;
            FileInfo f = new FileInfo(inputFile);
            totalFrames = ((Form1)this.Owner).total_frames;
            startFlame = ((Form1)this.Owner).frame_start;
            endFrame = ((Form1)this.Owner).frame_end;

            orgSize = f.Length;
            //ファイルサイズ長さ取得、ビットレート計算
            string strDuration = ((Form1)this.Owner).GetMovieDurationText(inputFile);
            // TimeSpanに変換
            videoLength = TimeSpan.Parse(strDuration);

            labelOrgSize.Text = (orgSize / 1024 / 1024).ToString();

            trackBarComp.Value = 50;
            numericUpDownComp.Value = 50;

            this.textBoxCommandline.Text = makeArgs();

            comboBoxCodec.Items.Clear();
            comboBoxCodec.Items.Add("libxvid");
            comboBoxCodec.Items.Add("libx264");
            comboBoxCodec.Items.Add("h264_qsv"); 
            comboBoxCodec.Items.Add("h264_nvenc");
            comboBoxCodec.Items.Add("h264_amf");
            comboBoxCodec.Items.Add("hevc");
            comboBoxCodec.Items.Add("hevc_qsv -load_plugin hevc_hw");
            comboBoxCodec.Items.Add("hevc_venc");

            comboBoxCodec.SelectedIndex = comboBoxCodec.FindStringExact(Properties.Settings.Default.recodec);
        }

        private void textBoxFPS_TextChanged(object sender, EventArgs e)
        {
            this.textBoxCommandline.Text = makeArgs();
        }

        private void comboBoxCodec_TextChanged(object sender, EventArgs e)
        {
            this.textBoxCommandline.Text = makeArgs();
        }

        private void trackBarComp_MouseUp(object sender, MouseEventArgs e)
        {
            this.textBoxCommandline.Text = makeArgs();
        }

        private void numericUpDownComp_Scroll(object sender, ScrollEventArgs e)
        {
            this.textBoxCommandline.Text = makeArgs();
        }

        private void numericUpDownComp_ValueChanged(object sender, EventArgs e)
        {
            targetSize = (long)(orgSize * numericUpDownComp.Value / 100);

            this.trackBarComp.Value = (int)numericUpDownComp.Value;
            this.textBoxSize.Text = ((float)targetSize * (endFrame - startFlame) / (float)totalFrames / 1024 / 1024).ToString("f1");
            this.textBoxBR.Text = (targetSize * 8 / 1024 / videoLength.TotalSeconds).ToString("0");
        }

        private void trackBarComp_Scroll(object sender, EventArgs e)
        {
            this.numericUpDownComp.Value = trackBarComp.Value;
        }

        private void buttonCompress_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("下記コマンドで動画圧縮を行います。\r\n " + "ffmpeg.exe " +  textBoxCommandline.Text, "動画圧縮", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                if (result == DialogResult.OK)
                {
                    //var process = Process.Start(new ProcessStartInfo("ffmpeg.exe", textBoxCommandline.Text)
                    //{
                    //    CreateNoWindow = false,
                    //    UseShellExecute = true,
                    //});

                    //process.WaitForExit();
                    ////MessageBox.Show("変換作業が終了しました");

                    var p = new Process();
                    p.StartInfo.FileName = "ffmpeg.exe";
                    p.StartInfo.Arguments = textBoxCommandline.Text;
                    p.StartInfo.CreateNoWindow = false;
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    //               p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.ErrorDialog = true;

                    p.SynchronizingObject = this;
                    p.Exited += (s, args) =>
                    {
                        MessageBox.Show("エンコード終了しました");
                        p.Dispose();
                        //rsb_reload();
                        
                    };
                    p.EnableRaisingEvents = true;
                    p.Start();

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private string makeArgs()
        {
            string arg;
            string Codec;
            string r = "";

            //ID欄の値に変更
            outputFile = System.Text.RegularExpressions.Regex.Replace(Path.GetFileName(inputFile), "^\\d+", ((Form1)this.Owner).comboBoxID.Text);
            outputFile = Properties.Settings.Default.outdir + "\\" + outputFile;

            if (comboBoxCodec.SelectedIndex == -1)
            {
                Codec = comboBoxCodec.Text;
            }
            else
            {
                Codec = comboBoxCodec.SelectedItem.ToString();
                switch (Codec)
                {
                    case "libxvid":
                        outputFile = Path.ChangeExtension(outputFile, ".avi");
                        break;
                    default:
                        outputFile = Path.ChangeExtension(outputFile, ".mp4");
                        break;
                }
            }
            if (Codec != "") Codec = "-c:v " + Codec;
            if (textBoxFPS.Text.Length > 0)
            {
                r = "-r " + textBoxFPS.Text;
            }

            arg = string.Format(" {6} -i \"{0}\" -filter:v trim=start_frame={4}:end_frame={5},setpts=PTS-STARTPTS,yadif -b:v {1}k {3} \"{2}\"", inputFile, textBoxBR.Text, outputFile, Codec, startFlame, endFrame, r);

            return (arg);
        }
    }
}
