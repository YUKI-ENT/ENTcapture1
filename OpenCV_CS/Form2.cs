using Accord.Video.DirectShow;  // AForge.NETライブラリから読込 
using System;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.RegularExpressions;


namespace ENTcapture
{
    public partial class Form2 : Form
    {
        int snapkeycode, startkeycode; //10-5桁：startkey
        private bool[] PresetReady = new bool[8];

        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "動画/静止画の保存先を選択してください";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowserDialog1.SelectedPath = Properties.Settings.Default.outdir;

            //ダイアログを表示する
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Properties.Settings.Default.outdir = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();
                this.textBox1.Text = Properties.Settings.Default.outdir;
            }

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.initForm();
        }

        private void initForm()
        {
            try
            {
                this.textBox1.Text = Properties.Settings.Default.outdir;
                this.textBox2.Text = Properties.Settings.Default.tmpdir;
                this.comboCodecs.Items.Clear();
                this.comboCodecs.Items.AddRange(new object[] { "raw", "MJPG", "XVID", "DIVX", "MP4V", "H264" });
                int index = comboCodecs.Items.IndexOf(Properties.Settings.Default.codec);
                comboCodecs.SelectedIndex = index;

                comboBoxReCodec.Items.Clear();
                comboBoxReCodec.Items.Add("libxvid");
                comboBoxReCodec.Items.Add("libx264");
                comboBoxReCodec.Items.Add("h264_qsv");
                comboBoxReCodec.Items.Add("h264_nvenc");
                comboBoxReCodec.Items.Add("h264_amf");
                comboBoxReCodec.Items.Add("hevc");
                comboBoxReCodec.Items.Add("hevc_qsv -load_plugin hevc_hw");
                comboBoxReCodec.Items.Add("hevc_nvenc");
                comboBoxReCodec.Items.Add("hevc_amf");

                int idx_recodec = comboBoxReCodec.Items.IndexOf(Properties.Settings.Default.recodec);
                if (idx_recodec >= 0) comboBoxReCodec.SelectedIndex = idx_recodec;

                this.textBoxThept.Text = Properties.Settings.Default.thept;

                this.textBoxTests.Text = Properties.Settings.Default.tests;
                SetTests(this.textBoxTests.Text);

                int k = Properties.Settings.Default.snapkey % 100000; //snapkey:10^4:ctrl,10^3:shift,mod1000:keyvalue
                if (k / 10000 > 0) this.checkBoxCtrl.Checked = true;
                if ((k % 10000) / 1000 > 0) this.checkShift.Checked = true;
                snapkeycode = k % 1000;
                textBoxChar.Text = ((Keys)snapkeycode).ToString();

                k = (int)Properties.Settings.Default.snapkey / 100000;
                if (k / 10000 > 0) this.checkBoxCtrl2.Checked = true;
                if ((k % 10000) / 1000 > 0) this.checkBoxShift2.Checked = true;
                startkeycode = k % 1000;
                if(startkeycode > 0) textBoxChar2.Text = ((Keys)startkeycode).ToString();

                textBoxJpegQuality.Text = Properties.Settings.Default.jpegquality.ToString();

                presetDevice1.Items.Clear();
                presetDevice2.Items.Clear();
                presetDevice3.Items.Clear();
                presetDevice4.Items.Clear();
                presetDevice5.Items.Clear();
                presetDevice6.Items.Clear();

                foreach (FilterInfo device in ((Form1)this.Owner).videoDevices)
                {
                    // カメラデバイスの一覧をコンボボックスに追加
                    presetDevice1.Items.Add(device.Name);
                    presetDevice2.Items.Add(device.Name);
                    presetDevice3.Items.Add(device.Name);
                    presetDevice4.Items.Add(device.Name);
                    presetDevice5.Items.Add(device.Name);
                    presetDevice6.Items.Add(device.Name);
                }
                this.presetDevice1.Text = Properties.Settings.Default.device1;
                this.presetDevice2.Text = Properties.Settings.Default.device2;
                this.presetDevice3.Text = Properties.Settings.Default.device3;
                this.presetDevice4.Text = Properties.Settings.Default.device4;
                this.presetDevice5.Text = Properties.Settings.Default.device5;
                this.presetDevice6.Text = Properties.Settings.Default.device6;

                this.presetRoi1.Text = Properties.Settings.Default.roi1;
                this.presetString1.Text = Properties.Settings.Default.string1;
                this.presetRoi2.Text = Properties.Settings.Default.roi2;
                this.presetString2.Text = Properties.Settings.Default.string2;
                this.presetRoi3.Text = Properties.Settings.Default.roi3;
                this.presetString3.Text = Properties.Settings.Default.string3;
                this.presetRoi4.Text = Properties.Settings.Default.roi4;
                this.presetString4.Text = Properties.Settings.Default.string4;
                this.presetRoi5.Text = Properties.Settings.Default.roi5;
                this.presetString5.Text = Properties.Settings.Default.string5;
                this.presetRoi6.Text = Properties.Settings.Default.roi6;
                this.presetString6.Text = Properties.Settings.Default.string6;

                this.presetVideo1.Checked = Properties.Settings.Default.video1;
                this.presetVideo2.Checked = Properties.Settings.Default.video2;
                this.presetVideo3.Checked = Properties.Settings.Default.video3;
                this.presetVideo4.Checked = Properties.Settings.Default.video4;
                this.presetVideo5.Checked = Properties.Settings.Default.video5;
                this.presetVideo6.Checked = Properties.Settings.Default.video6;

                //Fonts
                //InstalledFontCollectionオブジェクトの取得
                System.Drawing.Text.InstalledFontCollection ifc = new System.Drawing.Text.InstalledFontCollection();
                //インストールされているすべてのフォントファミリアを取得
                FontFamily[] ffs = ifc.Families;
                presetFonts1.Items.Clear();
                presetFonts2.Items.Clear();
                presetFonts3.Items.Clear();
                presetFonts4.Items.Clear();
                presetFonts5.Items.Clear();
                presetFonts6.Items.Clear();
                foreach (FontFamily f in ffs)
                {
                    presetFonts1.Items.Add(f.Name);
                    presetFonts2.Items.Add(f.Name);
                    presetFonts3.Items.Add(f.Name);
                    presetFonts4.Items.Add(f.Name);
                    presetFonts5.Items.Add(f.Name);
                    presetFonts6.Items.Add(f.Name);
                }
                presetFonts1.SelectedIndex = presetFonts1.Items.IndexOf(Properties.Settings.Default.font1);
                presetFonts2.SelectedIndex = presetFonts2.Items.IndexOf(Properties.Settings.Default.font2);
                presetFonts3.SelectedIndex = presetFonts3.Items.IndexOf(Properties.Settings.Default.font3);
                presetFonts4.SelectedIndex = presetFonts4.Items.IndexOf(Properties.Settings.Default.font4);
                presetFonts5.SelectedIndex = presetFonts5.Items.IndexOf(Properties.Settings.Default.font5);
                presetFonts6.SelectedIndex = presetFonts5.Items.IndexOf(Properties.Settings.Default.font6);


                //RSBASE
                checkBoxAutoFiling.Checked = Properties.Settings.Default.autofiling;
                if(checkBoxAutoFiling.Checked)
                {
                    labelRSBASEURL.Enabled = true;
                    textBoxRSBASEURL.Enabled = true;
                }
                else
                {
                    labelRSBASEURL.Enabled = false;
                    textBoxRSBASEURL.Enabled = false;
                }
                textBoxRSBASEURL.Text = Properties.Settings.Default.rsbaseurl;


                //if (Properties.Settings.Default.autodelete < 0) Properties.Settings.Default.autodelete = 0;
                textBoxAutoDelete.Text = Properties.Settings.Default.autodelete.ToString();

                textBoxFPS1.Text = Properties.Settings.Default.FPS1.ToString();
                textBoxFPS2.Text = Properties.Settings.Default.FPS2.ToString();
                textBoxFPS3.Text = Properties.Settings.Default.FPS3.ToString();
                textBoxFPS4.Text = Properties.Settings.Default.FPS4.ToString();
                textBoxFPS5.Text = Properties.Settings.Default.FPS5.ToString();
                textBoxFPS6.Text = Properties.Settings.Default.FPS6.ToString();

                textBoxEncode.Text = Properties.Settings.Default.encodesize.ToString();

                checkBoxRSBcamera.Checked = Properties.Settings.Default.rsbcamera;

                checkBoxTemp.Checked = Properties.Settings.Default.norec;

                numericUpDownTimeout.Value = Properties.Settings.Default.timeout;

                textBoxPGaddress.Text = Properties.Settings.Default.pgaddress;
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void SetTests(string strTests)
        {
            try
            {
                string[] tests = strTests.Split(',');

                this.presetName1.Text = Properties.Settings.Default.name1;
                this.presetTest1.Items.Clear();
                this.presetTest1.Items.AddRange(tests);
                this.presetTest1.Text = Properties.Settings.Default.test1;
                this.presetName2.Text = Properties.Settings.Default.name2;
                this.presetTest2.Items.Clear();
                this.presetTest2.Items.AddRange(tests);
                this.presetTest2.Text = Properties.Settings.Default.test2;
                this.presetName3.Text = Properties.Settings.Default.name3;
                this.presetTest3.Items.Clear();
                this.presetTest3.Items.AddRange(tests);
                this.presetTest3.Text = Properties.Settings.Default.test3;
                this.presetName4.Text = Properties.Settings.Default.name4;
                this.presetTest4.Items.Clear();
                this.presetTest4.Items.AddRange(tests);
                this.presetTest4.Text = Properties.Settings.Default.test4;
                this.presetName5.Text = Properties.Settings.Default.name5;
                this.presetTest5.Items.Clear();
                this.presetTest5.Items.AddRange(tests);
                this.presetTest5.Text = Properties.Settings.Default.test5;
                this.presetName6.Text = Properties.Settings.Default.name6;
                this.presetTest6.Items.Clear();
                this.presetTest6.Items.AddRange(tests);
                this.presetTest6.Text = Properties.Settings.Default.test6;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "一時録画ファイルの作成場所を選択してください（10Gb以上の空きがある高速なドライブをおすすめします）";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowserDialog1.SelectedPath = Properties.Settings.Default.tmpdir;

            //ダイアログを表示する
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Properties.Settings.Default.tmpdir = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();
                this.textBox2.Text = Properties.Settings.Default.tmpdir;
            }

        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            try
            {
                string stringError = "";
                if (presetString1.Text != "" && presetString1.Text.Split(',').Length % 5 != 0) stringError += "preset1 ";
                if (presetString2.Text != "" && presetString2.Text.Split(',').Length % 5 != 0) stringError += "preset2 ";
                if (presetString3.Text != "" && presetString3.Text.Split(',').Length % 5 != 0) stringError += "preset3 ";
                if (presetString4.Text != "" && presetString4.Text.Split(',').Length % 5 != 0) stringError += "preset4 ";
                if (presetString5.Text != "" && presetString5.Text.Split(',').Length % 5 != 0) stringError += "preset5";
                if (presetString6.Text != "" && presetString6.Text.Split(',').Length % 5 != 0) stringError += "preset6";


                if (stringError.Length > 0)
                {
                    MessageBox.Show(stringError + "の文字列指定が不正です。\r\n x,y,size,color,文字列 の並びになってるか確認してください", "書式エラー", MessageBoxButtons.OK);
                 }
                else
                {
                    setKey();

                    Properties.Settings.Default.name1 = presetName1.Text;
                    Properties.Settings.Default.test1 = presetTest1.Text;
                    
                    Properties.Settings.Default.roi1 = presetRoi1.Text;
                    Properties.Settings.Default.font1 = presetFonts1.Text;
                    Properties.Settings.Default.string1 = presetString1.Text;
                    Properties.Settings.Default.name2 = presetName2.Text;
                    Properties.Settings.Default.test2 = presetTest2.Text;
                    
                    Properties.Settings.Default.roi2 = presetRoi2.Text;
                    Properties.Settings.Default.font2 = presetFonts2.Text;
                    Properties.Settings.Default.string2 = presetString2.Text;
                    Properties.Settings.Default.name3 = presetName3.Text;
                    Properties.Settings.Default.test3 = presetTest3.Text;
                    
                    Properties.Settings.Default.roi3 = presetRoi3.Text;
                    Properties.Settings.Default.font3 = presetFonts3.Text;
                    Properties.Settings.Default.string3 = presetString3.Text;
                    Properties.Settings.Default.name4 = presetName4.Text;
                    Properties.Settings.Default.test4 = presetTest4.Text;
                    
                    Properties.Settings.Default.roi4 = presetRoi4.Text;
                    Properties.Settings.Default.font4 = presetFonts4.Text;
                    Properties.Settings.Default.string4 = presetString4.Text;

                    Properties.Settings.Default.name5 = presetName5.Text;
                    Properties.Settings.Default.test5 = presetTest5.Text;
                    Properties.Settings.Default.roi5 = presetRoi5.Text;
                    Properties.Settings.Default.font5 = presetFonts5.Text;
                    Properties.Settings.Default.string5 = presetString5.Text;

                    Properties.Settings.Default.name6 = presetName6.Text;
                    Properties.Settings.Default.test6 = presetTest6.Text;
                    Properties.Settings.Default.roi6 = presetRoi6.Text;
                    Properties.Settings.Default.font6 = presetFonts6.Text;
                    Properties.Settings.Default.string6 = presetString6.Text;

                    if (PresetReady[1])
                    {
                        Properties.Settings.Default.device1 = presetDevice1.Text;
                        Properties.Settings.Default.reso1 = presetReso1.SelectedIndex;
                    }
                    if (PresetReady[2])
                    {
                        Properties.Settings.Default.device2 = presetDevice2.Text;
                        Properties.Settings.Default.reso2 = presetReso2.SelectedIndex;
                    }
                    if (PresetReady[3])
                    {
                        Properties.Settings.Default.device3 = presetDevice3.Text;
                        Properties.Settings.Default.reso3 = presetReso3.SelectedIndex;
                    }
                    if (PresetReady[4])
                    {
                        Properties.Settings.Default.device4 = presetDevice4.Text;
                        Properties.Settings.Default.reso4 = presetReso4.SelectedIndex;
                    }
                    if (PresetReady[5])
                    {
                        Properties.Settings.Default.device5 = presetDevice5.Text;
                        Properties.Settings.Default.reso5 = presetReso5.SelectedIndex;
                    }
                    if (PresetReady[6])
                    {
                        Properties.Settings.Default.device6 = presetDevice6.Text;
                        Properties.Settings.Default.reso6 = presetReso6.SelectedIndex;
                    }

                    if (Properties.Settings.Default.reso1 < 0) Properties.Settings.Default.reso1 = 0;
                    if (Properties.Settings.Default.reso2 < 0) Properties.Settings.Default.reso2 = 0;
                    if (Properties.Settings.Default.reso3 < 0) Properties.Settings.Default.reso3 = 0;
                    if (Properties.Settings.Default.reso4 < 0) Properties.Settings.Default.reso4 = 0;
                    if (Properties.Settings.Default.reso5 < 0) Properties.Settings.Default.reso5 = 0;
                    if (Properties.Settings.Default.reso6 < 0) Properties.Settings.Default.reso6 = 0;

                    Properties.Settings.Default.video1 = this.presetVideo1.Checked;
                    Properties.Settings.Default.video2 = this.presetVideo2.Checked;
                    Properties.Settings.Default.video3 = this.presetVideo3.Checked;
                    Properties.Settings.Default.video4 = this.presetVideo4.Checked;
                    Properties.Settings.Default.video5 = this.presetVideo5.Checked;
                    Properties.Settings.Default.video6 = this.presetVideo6.Checked;

                    Properties.Settings.Default.jpegquality = int.Parse(textBoxJpegQuality.Text);

                    Properties.Settings.Default.autofiling = checkBoxAutoFiling.Checked;
                    Properties.Settings.Default.autodelete = int.Parse(textBoxAutoDelete.Text);
                    Properties.Settings.Default.rsbaseurl = textBoxRSBASEURL.Text;

                    Properties.Settings.Default.FPS1 = int.Parse(textBoxFPS1.Text);
                    Properties.Settings.Default.FPS2 = int.Parse(textBoxFPS2.Text);
                    Properties.Settings.Default.FPS3 = int.Parse(textBoxFPS3.Text);
                    Properties.Settings.Default.FPS4 = int.Parse(textBoxFPS4.Text);
                    Properties.Settings.Default.FPS5 = int.Parse(textBoxFPS5.Text);
                    Properties.Settings.Default.FPS6 = int.Parse(textBoxFPS6.Text);


                    Properties.Settings.Default.encodesize = int.Parse(textBoxEncode.Text);
                    Properties.Settings.Default.recodec = comboBoxReCodec.Text;
                    Properties.Settings.Default.IsUpgrade = true;

                    Properties.Settings.Default.rsbcamera = checkBoxRSBcamera.Checked;

                    Properties.Settings.Default.norec = checkBoxTemp.Checked;

                    Properties.Settings.Default.timeout = (int)numericUpDownTimeout.Value;

                    Properties.Settings.Default.pgaddress = textBoxPGaddress.Text;

                    Properties.Settings.Default.Save();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void comboCodecs_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.codec = comboCodecs.SelectedItem.ToString();
            Properties.Settings.Default.Save();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "RSBASEのthept.txtの場所（通常はc:\\common）";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowserDialog1.SelectedPath = Properties.Settings.Default.thept;

            //ダイアログを表示する
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Properties.Settings.Default.thept = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();
                this.textBoxThept.Text = Properties.Settings.Default.thept;
            }

        }

        private void textBoxTests_TextChanged(object sender, EventArgs e)
        {
            this.textBoxTests.Text.Replace("、", ",");
            Properties.Settings.Default.tests = this.textBoxTests.Text;

            SetTests(this.textBoxTests.Text);

        }

        private void buttonPresetPicture1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "参照画像を選択";
            //はじめに「ファイル名」で表示される文字列を指定する
            openFileDialog1.FileName = "default.html";
            //はじめに表示されるフォルダを指定する
            //指定しない（空の文字列）の時は、現在のディレクトリが表示される
            openFileDialog1.InitialDirectory = Properties.Settings.Default.outdir;
            //[ファイルの種類]に表示される選択肢を指定する
            //指定しないとすべてのファイルが表示される
            openFileDialog1.Filter = "jpegファイル(*.jpg;*.jpeg)|*.jpg;*.jpeg|すべてのファイル(*.*)|*.*";
            //[ファイルの種類]ではじめに選択されるものを指定する
            //2番目の「すべてのファイル」が選択されているようにする
            openFileDialog1.FilterIndex = 1;
            //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
            openFileDialog1.RestoreDirectory = true;
            //存在しないファイルの名前が指定されたとき警告を表示する
            //デフォルトでTrueなので指定する必要はない
            openFileDialog1.CheckFileExists = true;
            //存在しないパスが指定されたとき警告を表示する
            //デフォルトでTrueなので指定する必要はない
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.presetPictureBox1.ImageLocation = openFileDialog1.FileName;
            }

        }

        public static System.Drawing.Point ConvertCoordinates(System.Drawing.Point location, System.Windows.Forms.PictureBox pictureBox)
        {
            try
            {
                var x = location.X;
                var y = location.Y;
                var picH = pictureBox.ClientSize.Height;
                var picW = pictureBox.ClientSize.Width;
                var imgH = pictureBox.Image.Height;
                var imgW = pictureBox.Image.Width;

                int X0;
                int Y0;
                if (picW / (float)picH > imgW / (float)imgH)
                {
                    var scaledW = imgW * picH / (float)imgH;
                    var dx = (picW - scaledW) / 2;
                    X0 = (int)((x - dx) * imgH / picH);

                    Y0 = (int)(imgH * y / (float)picH);
                }
                else
                {
                    X0 = (int)(imgW * x / (float)picW);

                    var scaledH = imgH * picW / (float)imgW;
                    var dy = (picH - scaledH) / 2;
                    Y0 = (int)((y - dy) * imgW / picW);
                }

                if (X0 < 0 || imgW < X0 || Y0 < 0 || imgH < Y0)
                {
                    return new System.Drawing.Point(-1, -1); // 範囲外をどう表すのがいいか
                }

                return new System.Drawing.Point(X0, Y0);
            }
            catch (Exception)
            {
                return new System.Drawing.Point(-1, -1);
            }
        }

        private void presetPictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (presetPictureBox1.Image != null)
            {
                var p = ConvertCoordinates(e.Location, presetPictureBox1);
                this.presetArea1.Text = string.Format("{0},{1}", p.X, p.Y);
            }
        }


        private void textBoxChar_KeyDown(object sender, KeyEventArgs e)
        {
            this.textBoxChar.Text = e.KeyCode.ToString();
            snapkeycode = e.KeyValue;
        }

        private void setKey()
        {
            int k = 0;
            if (checkBoxCtrl.Checked) k += 10000;
            if (checkShift.Checked) k += 1000;
            k += snapkeycode;

            if (checkBoxCtrl2.Checked)  k += 1000000000;
            if (checkBoxShift2.Checked) k +=  100000000;
            k += startkeycode * 100000;
            
            Properties.Settings.Default.snapkey = k;
        }


        private void presetFonts1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.font1 = presetFonts1.Text;
        }

        private void presetbutton2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "参照画像を選択";
            openFileDialog1.FileName = "default.html";
            openFileDialog1.InitialDirectory = Properties.Settings.Default.outdir;
            openFileDialog1.Filter = "jpegファイル(*.jpg;*.jpeg)|*.jpg;*.jpeg|すべてのファイル(*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.presetpictureBox2.ImageLocation = openFileDialog1.FileName;
            }

        }

        private void presetbutton3_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "参照画像を選択";
            openFileDialog1.FileName = "default.html";
            openFileDialog1.InitialDirectory = Properties.Settings.Default.outdir;
            openFileDialog1.Filter = "jpegファイル(*.jpg;*.jpeg)|*.jpg;*.jpeg|すべてのファイル(*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.presetpictureBox3.ImageLocation = openFileDialog1.FileName;
            }

        }

        private void presetButton4_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "参照画像を選択";
            openFileDialog1.FileName = "default.html";
            openFileDialog1.InitialDirectory = Properties.Settings.Default.outdir;
            openFileDialog1.Filter = "jpegファイル(*.jpg;*.jpeg)|*.jpg;*.jpeg|すべてのファイル(*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.presetpictureBox4.ImageLocation = openFileDialog1.FileName;
            }

        }

        private void presetButton5_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "参照画像を選択";
            openFileDialog1.FileName = "default.html";
            openFileDialog1.InitialDirectory = Properties.Settings.Default.outdir;
            openFileDialog1.Filter = "jpegファイル(*.jpg;*.jpeg)|*.jpg;*.jpeg|すべてのファイル(*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.presetpictureBox5.ImageLocation = openFileDialog1.FileName;
            }

        }

        private void presetButton6_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "参照画像を選択";
            openFileDialog1.FileName = "default.html";
            openFileDialog1.InitialDirectory = Properties.Settings.Default.outdir;
            openFileDialog1.Filter = "jpegファイル(*.jpg;*.jpeg)|*.jpg;*.jpeg|すべてのファイル(*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.presetpictureBox6.ImageLocation = openFileDialog1.FileName;
            }

        }

        private void presetpictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (presetpictureBox2.Image != null)
            {
                var p = ConvertCoordinates(e.Location, presetpictureBox2);
                this.presetArea2.Text = string.Format("{0},{1}", p.X, p.Y);
            }

        }

        private void presetpictureBox3_MouseDown(object sender, MouseEventArgs e)
        {
            if (presetpictureBox3.Image != null)
            {
                var p = ConvertCoordinates(e.Location, presetpictureBox3);
                this.presetArea3.Text = string.Format("{0},{1}", p.X, p.Y);
            }
        }

        private void presetpictureBox4_MouseDown(object sender, MouseEventArgs e)
        {
            if (presetpictureBox4.Image != null)
            {
                var p = ConvertCoordinates(e.Location, presetpictureBox4);
                this.presetArea4.Text = string.Format("{0},{1}", p.X, p.Y);
            }
        }

        private void presetpictureBox5_MouseDown(object sender, MouseEventArgs e)
        {
            if (presetpictureBox5.Image != null)
            {
                var p = ConvertCoordinates(e.Location, presetpictureBox5);
                this.presetArea5.Text = string.Format("{0},{1}", p.X, p.Y);
            }
        }

        private void presetpictureBox6_MouseDown(object sender, MouseEventArgs e)
        {
            if (presetpictureBox6.Image != null)
            {
                var p = ConvertCoordinates(e.Location, presetpictureBox6);
                this.presetArea6.Text = string.Format("{0},{1}", p.X, p.Y);
            }
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            string settingsFilePath;

            SaveFileDialog op = new SaveFileDialog();
            op.Title = "設定の保存先";
            op.FileName = "entcapture.config";
            op.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            op.Filter = "configファイル(*.config)|*.config|すべてのファイル(*.*)|*.*";
            op.FilterIndex = 1;
            op.RestoreDirectory = true;
            op.CheckFileExists = false;
            op.CheckPathExists = true;

            if (op.ShowDialog(this) == DialogResult.OK)
            {
                settingsFilePath = op.FileName;
                Properties.Settings.Default.Save();
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                config.SaveAs(settingsFilePath);
                MessageBox.Show("設定を保存しました");
            }
            op.Dispose();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "設定ファイルの読込";
            op.FileName = "entcapture.config";
            op.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            op.Filter = "configファイル(*.config)|*.config|すべてのファイル(*.*)|*.*";
            op.FilterIndex = 1;
            op.RestoreDirectory = true;
            op.CheckFileExists = false;
            op.CheckPathExists = true;

            if (op.ShowDialog(this) == DialogResult.OK)
            {
                string settingsFilePath = op.FileName;
                Properties.Settings appSettings = Properties.Settings.Default;

                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

                    string appSettingsXmlName = Properties.Settings.Default.Context["GroupName"].ToString();
                    // returns "MyApplication.Properties.Settings";

                    // Open settings file as XML
                    var import = XDocument.Load(settingsFilePath);
                    // Get the whole XML inside the settings node
                    var settings = import.XPathSelectElements("//" + appSettingsXmlName);

                    config.GetSectionGroup("userSettings")
                        .Sections[appSettingsXmlName]
                        .SectionInformation
                        .SetRawXml(settings.Single().ToString());
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("userSettings");

                    appSettings.Reload();
                    initForm();

                    MessageBox.Show("設定を読み込みました");
                }
                catch (Exception ex) // Should make this more specific
                {
                    // Could not import settings.
                    appSettings.Reload(); // from last set saved, not defaults
                    MessageBox.Show(ex.ToString());
                }
            }

            op.Dispose();

        }

        private void presetDevice1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                presetReso1.Items.Clear();
                PresetReady[1] = false;

                int i = 0;
                do
                {
                    string r = ((Form1)this.Owner).resos[presetDevice1.SelectedIndex, i];
                    if (r == null) break;
                    presetReso1.Items.Add(r);
                    i++;
                } while (i < 127);

                if (i > 0)
                {
                    presetReso1.SelectedIndex = Properties.Settings.Default.reso1;
                    PresetReady[1] = true;
                }
                else
                {
                    MessageBox.Show("設定時に存在したデバイスが取り外されているようです。Preset1の設定はスキップされます。");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Preset1 デバイスエラー。設定時に存在したデバイスが取り外されている可能性があります");
            }
        }

        private void presetDevice2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                presetReso2.Items.Clear();
                PresetReady[2] = false;

                int i = 0;
                do
                {
                    string r = ((Form1)this.Owner).resos[presetDevice2.SelectedIndex, i];
                    if (r == null) break;
                    presetReso2.Items.Add(r);
                    i++;
                } while (i < 127);

                if (i > 0)
                {
                    presetReso2.SelectedIndex = Properties.Settings.Default.reso2;
                    PresetReady[2] = true;
                }
                else
                {
                    MessageBox.Show("設定時に存在したデバイスが取り外されているようです。Preset2の設定はスキップされます。");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Preset2 デバイスエラー。設定時に存在したデバイスが取り外されている可能性があります");
            }
        }

        private void presetDevice3_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                presetReso3.Items.Clear();
                PresetReady[3] = false;

                int i = 0;
                do
                {
                    string r = ((Form1)this.Owner).resos[presetDevice3.SelectedIndex, i];
                    if (r == null) break;
                    presetReso3.Items.Add(r);
                    i++;
                } while (i < 127);

                if (i > 0)
                {
                    presetReso3.SelectedIndex = Properties.Settings.Default.reso3;
                    PresetReady[3] = true;
                }
                else
                {
                    MessageBox.Show("設定時に存在したデバイスが取り外されているようです。Preset3の設定はスキップされます。");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Preset3 デバイスエラー。設定時に存在したデバイスが取り外されている可能性があります");
            }
        }

        private void presetDevice4_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                presetReso4.Items.Clear();
                PresetReady[4] = false;

                int i = 0;
                do
                {
                    string r = ((Form1)this.Owner).resos[presetDevice4.SelectedIndex, i];
                    if (r == null) break;
                    presetReso4.Items.Add(r);
                    i++;
                } while (i < 127);

                if (i > 0)
                {
                    presetReso4.SelectedIndex = Properties.Settings.Default.reso4;
                    PresetReady[4] = true;
                }
                else
                {
                    MessageBox.Show("設定時に存在したデバイスが取り外されているようです。Preset4の設定はスキップされます。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + "Preset4 デバイスエラー。設定時に存在したデバイスが取り外されている可能性があります");
            }
        }

        private void presetDevice5_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                presetReso5.Items.Clear();
                PresetReady[5] = false;

                int i = 0;
                do
                {
                    string r = ((Form1)this.Owner).resos[presetDevice5.SelectedIndex, i];
                    if (r == null) break;
                    presetReso5.Items.Add(r);
                    i++;
                } while (i < 127);

                if (i > 0)
                {
                    presetReso5.SelectedIndex = Properties.Settings.Default.reso5;
                    PresetReady[5] = true;
                }
                else
                {
                    MessageBox.Show("設定時に存在したデバイスが取り外されているようです。Preset5の設定はスキップされます。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + "Preset5 デバイスエラー。設定時に存在したデバイスが取り外されている可能性があります");
            }

        }

        private void checkBoxRSBcamera_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRSBcamera.Checked)
            {
                if(textBox1.Text.IndexOf("public_html\\camera") < 0)
                {
                    MessageBox.Show("静止画出力フォルダーには、(RSBaseインストールドライブ):\\Users\\rsn\\public_html\\cameraを指定してください");
                }
            }
        }

        private void checkBoxTemp_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTemp.Checked){
                textBox2.Enabled = false;
                comboCodecs.Enabled = false;
                textBoxAutoDelete.Enabled = false;
                textBoxEncode.Enabled = false;
                label2.Enabled = false;
                label3.Enabled = false;
                label41.Enabled = false;
                label42.Enabled = false;
                label59.Enabled = false;
                label60.Enabled = false;
                labelTimeout.Enabled = false;
                numericUpDownTimeout.Enabled = false;
            }
            else
            {
                textBox2.Enabled = true;
                comboCodecs.Enabled = true;
                textBoxAutoDelete.Enabled = true;
                textBoxEncode.Enabled = true;
                label2.Enabled = true;
                label3.Enabled = true;
                label41.Enabled = true;
                label42.Enabled = true;
                label59.Enabled = true;
                label60.Enabled = true;
                labelTimeout.Enabled = true;
                numericUpDownTimeout.Enabled = true;
            }
        }


        private void checkBoxAutoFiling_CheckedChanged(object sender, EventArgs e)
        {
                labelRSBASEURL.Enabled = checkBoxAutoFiling.Checked;
                textBoxRSBASEURL.Enabled = checkBoxAutoFiling.Checked;
        }

        
        private void textBoxPGaddress_Leave(object sender, EventArgs e)
        {
            string input = textBoxPGaddress.Text;

            if (input.Length >0 && !Regex.IsMatch(input, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$"))
            {
                MessageBox.Show("IPアドレスの形式が正しくありません。「192.168.0.1」等の形式で入力してください。");
                textBoxPGaddress.Text = "";
                textBoxPGaddress.Focus();
            }

        }

        private void presetDevice6_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                presetReso6.Items.Clear();
                PresetReady[6] = false;

                int i = 0;
                do
                {
                    string r = ((Form1)this.Owner).resos[presetDevice6.SelectedIndex, i];
                    if (r == null) break;
                    presetReso6.Items.Add(r);
                    i++;
                } while (i < 127);

                if (i > 0)
                {
                    presetReso6.SelectedIndex = Properties.Settings.Default.reso6;
                    PresetReady[6] = true;
                }
                else
                {
                    MessageBox.Show("設定時に存在したデバイスが取り外されているようです。Preset6の設定はスキップされます。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + "Preset6 デバイスエラー。設定時に存在したデバイスが取り外されている可能性があります");
            }
        }

        private void textBoxChar2_KeyDown(object sender, KeyEventArgs e)
        {
            this.textBoxChar2.Text = e.KeyCode.ToString();
            startkeycode = e.KeyValue;

        }
    }
}
