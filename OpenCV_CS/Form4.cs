using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ENTcapture
{
    public partial class Form4 : Form
    {
        private string inputFile, outputFile;
        private long targetSize, orgSize;
        TimeSpan videoLength;
        private int totalFrames, startFlame, endFrame;
        private Form1 form1;

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
            form1 = (Form1)this.Owner;
            inputFile = form1.videofile;
            this.labelFilename.Text = inputFile;
            FileInfo f = new FileInfo(inputFile);
            totalFrames = form1.total_frames;
            startFlame = form1.frame_start;
            endFrame = form1.frame_end;

            orgSize = f.Length;
            //ファイルサイズ長さ取得、ビットレート計算
            string strDuration = form1.GetMovieDurationText(inputFile);
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

        private async void buttonCompress_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("下記コマンドで動画圧縮を行います。\r\n " + "ffmpeg.exe " +  textBoxCommandline.Text, "動画圧縮", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                if (result == DialogResult.OK)
                {
                   
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg.exe",
                        Arguments = textBoxCommandline.Text,
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

                        process.Exited += (s, args) =>
                        {
                            tcs.SetResult(null);
                            this.Close();
                        };

                        
                        process.Start();
                        await tcs.Task; // プロセスの終了を非同期的に待機します
                    }

                    MessageBox.Show("エンコード終了しました");

                    if (form1 != null)
                    {
                        // Form1をアクティブにし、最前面に表示します
                        form1.Activate();
                        form1.TopMost = true;
                        form1.TopMost = false; // 最前面表示を解除することで、フォーカスを移動させる
                    }
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
